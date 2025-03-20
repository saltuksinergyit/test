using FTMCommon.Common.MainService;
using FTMCommon.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Common.MainService;
using FTMPlus.Common.Processor;
using FTMPlus.Entities;
using FTMPlus.Executer.MainService;
using System.Text.Json;

namespace FTMPlusExecuter.Executer
{
    public class ExecuterService
    { 
        public IServiceScope Scope { get; private set; } = default!;
        public RabbitMQManager Rabbit { get; private set; } = default!;
        public FlowService FlowService { get; private set; }
        public ILogger<ExecuterService> Logger { get; }
        public ServerService Server { get; } 
        public ExecuterService(ILogger<ExecuterService> logger, ServerService server,FlowService flowService)
        {
            this.FlowService = flowService;
            this.Logger = logger;
            this.Server = server;
        }

        public async Task ExecuterAsync(Flow flow, IServiceScope ctx, RabbitMQManager rabbit)
        {

            this.FlowService.Set(flow);
            //this.Flow = flow;
            this.Scope = ctx;
            this.Rabbit = rabbit; 
            if (this.FlowService.Process == null)
            {
                throw new Exception("NotFound");
            }
            await FlowExecuter();
        } 
        private async Task FlowExecuter()
        { 
            BaseProccessor baseProccessor = GetProccessor();
            baseProccessor.Set(Scope);
            var status= await baseProccessor.ExecuteAsync();
            await NextProccessorAsync(status);
        }

        private async Task NextProccessorAsync(FlowStatus status)
        {
            if(status.IsError)
            { 
                this.FlowService.Flow.TryCount++; 
            }
            else
            {
                this.FlowService.Flow.ProccessOrder++;
                this.FlowService.Flow.TryCount = 0;
            } 

            await UpdateFlowProcessStatusAsync(status); 
            //next var mi
            if (this.FlowService.Flow.ProccessOrder <= this.FlowService.Flow.Procces.Max(t => t.Order) &&  this.FlowService.Flow.TryCount < 3)
            {             
                await Rabbit.PublishMessageAsync(nameof(ExecuterRunningService), JsonSerializer.Serialize(this.FlowService.Flow)!);
            }
            else
            {
                await CompleteProccessorAsync();
            }

        }
        /// <summary>
        /// db git durum guncelle
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns> 
        private async Task UpdateFlowProcessStatusAsync(FlowStatus status)
        { 
            await Task.CompletedTask;
        }

        private async Task CompleteProccessorAsync()
        {
            await Task.CompletedTask;
        }


        /// <summary>
        /// hangi proccessor calisacak
        /// </summary>
        /// <returns></returns>
        private BaseProccessor GetProccessor()
        {
            switch (this.FlowService.Process.ProccesType)
            {
                case ProccesType.Copy:
                    return this.Scope.ServiceProvider.GetRequiredService<CopyProcessor>();
                case ProccesType.Transform:
                    break;
                case ProccesType.Merge:
                    break;
                case ProccesType.Notify:
                    break;
                case ProccesType.Check:
                    break;
                default:
                    break;
            }
            return null;
        }



    }
}
