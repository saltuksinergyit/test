using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Common.MainService;
using FTMPlus.Common.Processor;
using FTMPlus.Entities;
using FTMPlusExecuter.Executer;
using System.Text.Json;

namespace FTMPlus.Executer.MainService
{

    public class ExecuterRunningService(ILogger<ExecuterRunningService> _logger, IServiceScopeFactory service, ServerService server, RabbitMQManager rabbit) : BackgroundService
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            await rabbit.StartConsumerAsync(nameof(ExecuterRunningService), async (message) =>
            {
                try
                {
                    _logger.LogInformation($"[EXECUTER] ListenerRunningService");

                    var row = JsonSerializer.Deserialize<Flow>(message);
                    await Executer(row);
                    _logger.LogInformation($"[EXECUTER] ListenerRunningService");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ListenerRunningService");
                }
            });

            Console.ReadLine();
        }

        private async Task Executer(Flow flow)
        {
            IServiceScope? ctx = null;

            try
            {
                ctx = service.CreateScope();
                await ctx.ServiceProvider.GetRequiredService<ExecuterService>().ExecuterAsync(flow, ctx, rabbit); 

            }
            finally
            {
                ctx?.Dispose();
            }

        }

    }

}
