namespace FTMPlus.Common.MainService
{

    public class ListenerIsMaster : BackgroundService
    {

        public static bool IsMaster { get; set; } = false;


        private readonly DistributedLockMasterService _lockService;
        private readonly ILogger<ListenerIsMaster> _logger;
        private readonly string _instanceId;
        private const string MasterKey = "01_FTM:Master";
        private const int MasterTTL = 60; // Master süresi (saniye cinsinden)

        public ListenerIsMaster(DistributedLockMasterService lockService, ILogger<ListenerIsMaster> logger)
        {
            _lockService = lockService;
            _logger = logger;
            _instanceId = Environment.MachineName;  //Guid.NewGuid().ToString(); // Her instance için benzersiz ID oluşturulur
            //_instanceId = Guid.NewGuid().ToString(); // Her instance için benzersiz ID oluşturulur

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"ListenerIsMaster servisi başlatıldı: {_instanceId}  {DateTime.Now.ToString()}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Master Lock'u almayı dene
                    string? isMaster = await _lockService.GetExistingLockValueAsync(MasterKey);

                    if (isMaster == _instanceId || isMaster == null)
                    {
                        _logger.LogInformation($"[MASTER] Bu instance master oldu: {_instanceId}");
                        isMaster = _instanceId;
                        await _lockService.AcquireLockAsync(MasterKey, MasterTTL, _instanceId);
                        // Master süresi boyunca işlemi yürüt
                        while (!stoppingToken.IsCancellationRequested && isMaster == _instanceId)
                        {
                            
                            // Süreyi güncelle
                            await _lockService.AcquireLockAsync(MasterKey, MasterTTL, _instanceId);
                            _logger.LogInformation($"[MASTER] Master olarak devam ediliyor: {_instanceId}");
                            IsMaster = true;
                            await Task.Delay((MasterTTL/5)*1000, stoppingToken); // 3 saniye bekleyip tekrar kontrol et
                            isMaster = await _lockService.GetExistingLockValueAsync(MasterKey);

                        }
                        _logger.LogInformation($"Master Değişti");
                    }
                    else
                    {
                        _logger.LogInformation($"Başka bir instance master.");
                    }
                    IsMaster = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Master kontrol hatası: {ex.Message}");
                }
                finally
                {
                    // Master Lock'u kaldır
                    if (IsMaster)
                        await _lockService.ReleaseLockAsync(MasterKey);
                }

                await Task.Delay(MasterTTL * 1000, stoppingToken); // TTL kadar saniye sonra tekrar dene
            }
        }
    }

}
