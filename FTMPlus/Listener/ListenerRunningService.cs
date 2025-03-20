using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Common.Processor;
using FTMPlus.Entities;
using System.Text.Json;

namespace FTMPlus.Common.MainService
{

    public class ListenerRunningService(ILogger<ListenerRunningService> _logger, IServiceScopeFactory service, ServerService server, RabbitMQManager rabbit, IServiceScopeFactory serviceScopeFactory) : BackgroundService
    {
        private DistributedLockService lockService { get; set; } = null!;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var lockService = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<DistributedLockService>();


            this.lockService = lockService;

            await rabbit.StartConsumerAsync(nameof(ListenerRunningService), async (message) =>
            {
                try
                {
                    _logger.LogInformation($"[SLAVE] ListenerRunningService");

                    var row = JsonSerializer.Deserialize<FlowsChunks>(message)!;
                    await SearchFlows(row);
                    _logger.LogInformation($"[SLAVE] ListenerRunningService");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ListenerRunningService");
                }
            });

            Console.ReadLine();
        }

        private async Task SearchFlows(FlowsChunks flowsChunks)
        {
            IServiceScope? ctx = null;
            ISearchAdaptor? searchAdaptor = null;
            try
            {
                ctx = service.CreateScope();
                var fileServer = server.GetFileServer(flowsChunks.ServerCode);

                searchAdaptor = getSearchAdaptor(fileServer.ServerType, ctx);



                if (searchAdaptor == null)
                {
                    _logger.LogError($"SearchAdaptor not found {flowsChunks.ServerCode}");
                    return;
                }
                await searchAdaptor.ConnectAsync(fileServer);
                flowsChunks.Flows.ForEach(async x =>
                {
                    try
                    {
                        var procces = x.Procces.FirstOrDefault();
                        await searchAdaptor.Set(ctx, x, 0); // ilk proccess 
                        var data = await searchAdaptor.FindFilesAsync(fileServer, procces!);
                        if (data.Count > 0)
                        {
                            await SendStartProccessAsync(x, data);
                            _logger.LogInformation($"SearchFlow {x.FlowId}-{x.FlowName} Found {data.Count} files");
                        }
                        else
                        {
                            _logger.LogInformation($"SearchFlow {x.FlowId}-{x.FlowName} Not Found");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"SearchFlow {x.FlowId}-{x.FlowName}");
                    }
                });
            }
            finally
            {
                searchAdaptor?.Disconnect();
                ctx?.Dispose();
            }

        }

        private async Task SendStartProccessAsync(Flow x, List<FileMeta> data)
        {
            _logger.LogInformation($"finded\n{JsonSerializer.Serialize(data)}");
            Flow flow = JsonSerializer.Deserialize<Flow>(JsonSerializer.Serialize(x))!;
            var proccess = flow.Procces.First();
            flow.ProccessOrder = 1;


            List<FileMeta> sendData = new List<FileMeta>();

            foreach (var item in data)
            {
                try
                {
                    string hash = lockService.getLockHash(item);
                    item.lockKey = lockService.getLockBase(hash);
                    //neden boyle yapildi eger source islemden sonra kayboluyorsa db bakmaya gerek yok redis master olarak kullanilabilir.
                    var status = await lockService.GetExistingLockValueAsync(hash, proccess.SourceResultAction == "D" || proccess.SourceResultAction == "R");
                    if (status == LockValue.None)
                    {
                        sendData.Add(item);
                        await lockService.AcquireLockAsync(hash, 600, LockValue.Start, proccess.SourceResultAction == "D" || proccess.SourceResultAction == "R");
                    }
                }
                catch (Exception)
                {

                }

            }

            data = sendData;



            if (data.Count == 0)
                return;

            if (proccess.MultipleFiles)
            {
                proccess.SourceFiles = data;
                flow.Id = Guid.NewGuid();

                await rabbit.PublishMessageAsync("ExecuterRunningService", JsonSerializer.Serialize(flow));
            }
            else
                foreach (var item in data)
                {
                    flow.Id = Guid.NewGuid();
                    proccess.SourceFiles = new List<FileMeta> { item };
                    await rabbit.PublishMessageAsync("ExecuterRunningService", JsonSerializer.Serialize(flow));
                }
        }

        private ISearchAdaptor? getSearchAdaptor(ServerType serverType, IServiceScope ctx)
        {
            try
            {
                switch (serverType)
                {
                    case Entities.ServerType.FTP:
                        return ctx.ServiceProvider.GetRequiredService<FTPAdaptor>();
                    case Entities.ServerType.SFTP:
                        return ctx.ServiceProvider.GetRequiredService<SFTPAdaptor>();
                    case Entities.ServerType.S3:
                        return ctx.ServiceProvider.GetRequiredService<S3Adaptor>();
                    //case Entities.ServerType.Mail:
                    //    break;
                    case Entities.ServerType.Filesystem:
                    default:
                        return ctx.ServiceProvider.GetRequiredService<FileAdaptor>();
                }
                return null;
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }

}
