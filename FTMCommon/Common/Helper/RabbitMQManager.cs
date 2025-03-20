using System;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events; // AsyncEventingBasicConsumer vb. için

public class RabbitMQManager : IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IChannel _channel; // Yeni nesil API'de IModel yerine IChannel türü olabilir.
    private readonly ILogger<RabbitMQManager> _logger;
    protected bool isStart = false;

    public RabbitMQManager(
        IConfiguration configuration,
        ILogger<RabbitMQManager> logger = null)
    {
        _logger = logger;


        string hostName = configuration["RabbitMQ:HostName"];
        int.TryParse(configuration["RabbitMQ:Port"], out int port);
        port = port == 0 ? 5672 : port;
        string userName = configuration["RabbitMQ:UserName"];
        string password = configuration["RabbitMQ:Password"];

        // Bağlantı ayarları – gerekirse TLS, virtual host vb. eklenebilir
        _connectionFactory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            
        };
    }

    /// <summary>
    /// Asenkron olarak RabbitMQ'ya bağlantı açar ve kanal oluşturur.
    /// Artık CreateModel() yerine CreateChannelAsync() kullanıyoruz.
    /// </summary>
    public async Task InitializeAsync(string queueName)
    {
        try
        {
            _connection = await _connectionFactory.CreateConnectionAsync();

            // Yeni API'de kanal oluşturmak için CreateChannelAsync gibi bir metot
            // (Gerçek sürüm ve dokümantasyon değişebilir)
            _channel = await _connection.CreateChannelAsync();


            await DeclareQueueAsync(queueName);

            _logger?.LogInformation("RabbitMQ bağlantısı kuruldu ve kanal oluşturuldu (async).");
            isStart = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RabbitMQ bağlantısı ya da kanal oluşturulurken hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Kuyruk tanımlar (Declare). Örneğin dosya işlem kuyruğu gibi.
    /// </summary>
    private async Task DeclareQueueAsync(string queueName)
    {

        if (_channel == null)
            throw new InvalidOperationException("Önce InitializeAsync() ile bağlantı kurulmalıdır.");

        try
        {
            // Bazı sürümlerde QueueDeclare yerine QueueDeclareAsync kullanılabilir.
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger?.LogInformation($"{queueName} kuyruğu tanımlandı.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"{queueName} kuyruğu tanımlanırken hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Mesajları dinler (consume). Yeni API’de BasicConsumeAsync / AsyncEventingBasicConsumer vb.
    /// </summary>
    public async Task StartConsumerAsync(string queueName, Func<string, Task> onMessageReceivedAsync)
    {

        if (!isStart)
            await this.InitializeAsync(queueName);
        if (_channel == null)
            throw new InvalidOperationException("Önce InitializeAsync() ile bağlantı kurulmalıdır.");

        try
        {
            // Eğer kuyruğu daha önce declare etmediyseniz, burada da declare edebilirsiniz:
            // await DeclareQueueAsync(queueName);
            await _channel.BasicQosAsync(0, 20, false);
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger?.LogInformation($"[{queueName}] mesaj alındı: {message}");

                    // İş mantığınızı asenkron çağırabilirsiniz
                    await onMessageReceivedAsync?.Invoke(message);

                    // Manuel ack (autoAck = false kullanıyorsanız)
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"[{queueName}] mesaj işlenirken hata oluştu.");
                    // Gerekirse BasicNack veya BasicReject ile yeniden kuyruğa koyma (requeue) yapılabilir
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // autoAck=false → manuel BasicAck
            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger?.LogInformation($"{queueName} kuyruğu için consumer başlatıldı.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"{queueName} kuyruğunda consumer başlatılırken hata oluştu.");
            throw;
        }
    }

    /// <summary>
    /// Mesaj yayınlama (publish). Örnek: SagaEvent'leri ya da FTM dosya işleme bildirimlerini iletmek.
    /// </summary>
    public async Task PublishMessageAsync(string queueName, string message,string messageId=null)
    {
        if (!isStart)
            await this.InitializeAsync(queueName);



        if (_connection == null)
            throw new InvalidOperationException("Önce InitializeAsync() ile bağlantı kurulmalıdır.");

        try
        {
            using var channel = await _connection.CreateChannelAsync();

            // Kuyruk tanımlama (Eğer önceden tanımlanmadıysa)
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            var props = new BasicProperties
            {
                Persistent = true,
                MessageId= messageId,
                
            };

            // Exchange kullanmadan, kuyruğa doğrudan mesaj bırak
            await channel.BasicPublishAsync("", queueName, false, props, body);

            _logger?.LogInformation($"[Queue={queueName}] Mesaj bırakıldı: {message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Mesaj kuyruğa bırakılırken hata oluştu.");
            throw;
        }


    }

    public async ValueTask DisposeAsync()
    {
        // Asenkron dispose örneği
        try
        {
            if (_channel != null)
                await _channel.CloseAsync();
            if (_connection != null)
                await _connection.CloseAsync();
        }
        catch
        {
            // ignore or log
        }
    }
}
