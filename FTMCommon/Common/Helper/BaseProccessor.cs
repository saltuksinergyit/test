using FTMCommon.Common.MainService;
using FTMCommon.Entities;
using FTMPlus.Common.MainService;
using FTMPlus.Entities;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FTMPlus.Common.Helper
{
    public abstract class BaseProccessor
    {
        public FlowService FlowService { get; set; } = default!;
        public Flow Flow { get { return FlowService.Flow; } }
        public int Order { get { return FlowService.Flow.ProccessOrder; } }
        public IServiceScope ServiceScope { get; private set; } = default!;
        public ServerService ServerCodes { get; private set; } = default!;

        public FlowStatus FlowStatus { get; set; } = new FlowStatus();

        public FileServer GetServerAsync(string serverCode)
        {
            return this.ServerCodes.GetFileServer(serverCode);
        }


        public FlowProcess Process { get { return this.Flow.Procces.FirstOrDefault(t => t.Order == this.Order)!; } }
        /// <summary>
        /// Dosyaları bulur.
        /// </summary>
        /// <returns></returns>
        public abstract Task DoProccessAsync();

        public void Set(IServiceScope ctx)
        { 
            this.ServiceScope = ctx; 
            this.FlowService = this.ServiceScope.ServiceProvider.GetRequiredService<FlowService>();
            this.ServerCodes = this.ServiceScope.ServiceProvider.GetRequiredService<ServerService>(); 
        }

        /// <summary>
        /// Dosya Varsayimsal Logic Hesaplar 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetExpresionAsync(string value)
        {
            return value;
        }



        public async Task<FlowStatus> ExecuteAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (this.Process == null)
            {
                throw new Exception("Proccess not found");
            }
            try
            {
                await this.DoProccessAsync();
                return FlowStatus;
            }
            catch (Exception ex)
            {
                FlowStatus.IsError = true;
                FlowStatus.ErrorMessages.Add(new FlowError { Message = ex.Message, StackTrace = ex.StackTrace ?? "" });
                return FlowStatus;

            }
            finally
            {
                stopwatch.Stop();
                this.SendMetricAsync(stopwatch);
            }

        }


        /// <summary>
        /// Metrik verilerini gönderir.
        /// </summary>
        /// <param name="stopwatch"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SendMetricAsync(Stopwatch stopwatch)
        {
            //throw new NotImplementedException();
        }
    }


}
