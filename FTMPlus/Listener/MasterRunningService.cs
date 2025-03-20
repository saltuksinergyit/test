using FTM.Common.Entities;
using FTMPlus.Entities;
using System.Text.Json;
using System.Threading.Tasks;

namespace FTMPlus.Common.MainService
{

    public class MasterRunningService(ILogger<MasterRunningService> _logger, RabbitMQManager rabbit) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested && ListenerIsMaster.IsMaster)
                    {
                        await this.SendExecuteAsync(stoppingToken);
                    }
                    _logger.LogInformation($"[SLAVE] SLAVE olarak devam ediliyor");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MasterRunnigMode");
                }
                await Task.Delay(5 * 1000, stoppingToken); // TTL kadar saniye sonra tekrar dene
            }
        }

        private async Task SendExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"[MASTER] MASTER Listener Defination gonderiyor");

            var groupedAndChunkedFlows = Flows.Where(t => t.FlowId == 1)
                .GroupBy(f => f.Procces.FirstOrDefault()?.SourceServerCode) // İlk process'in SourceServerCode'una göre gruplama
                .Select(group => new
                {
                    ServerCode = group.Key,
                    Chunks = group.Select((flow, index) => new { flow, index })
                                  .GroupBy(x => x.index / 100) // 100'lük parçalara bölme
                                  .Select(chunk => chunk.Select(x => x.flow).ToList())
                                  .ToList()
                })
                .ToList();

            foreach (var item in groupedAndChunkedFlows)
            {
                foreach (var chunk in item.Chunks)
                {
                    var send = new FlowsChunks
                    {
                        ServerCode = item.ServerCode,
                        Flows = chunk
                    };
                    await rabbit.PublishMessageAsync(nameof(ListenerRunningService), JsonSerializer.Serialize(send));

                }

            }



            await Task.Delay(1000 * 59);

        }


        public List<Flow> Flows { get; set; } = new List<Flow>()
        {
            new Flow
            {
                FlowId = 100,
                FlowDescription = "Flow 1",
                FlowName= "Flow 1",
                FlowStatus="A",
                FlowType="F",
                Procces=new List<FlowProcess>()
                {
                    /// file system to s3
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=1,
                        SourceServerCode="3",
                        SourceDirectoryName="C:\\TEST\\FTPTOFILESYSTEM",
                        SourceFileName="*.txt",
                        SourceResultAction="R",
                        SourceNewFileExtension=".p1",
                        TargetDirectoryName="/s3test/b",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="2"
                    } 
                }
            },
            new Flow
            {
                FlowId = 100,
                FlowDescription = "Flow 1",
                FlowName= "Flow 1",
                FlowStatus="A",
                FlowType="F",
                Procces=new List<FlowProcess>()
                {
                    ///ftp to file system
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=1,
                        SourceServerCode="4",
                        SourceDirectoryName="/test/",
                        SourceFileName="*.txt",
                        TargetDirectoryName="C:\\TEST\\FTPTOFILESYSTEM",
                        SourceResultAction="R",
                        SourceNewFileExtension=".p1",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="3"
                    },
                    ///file system to s3 system
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=2,
                        SourceServerCode="3",
                        SourceDirectoryName="C:\\TEST\\FTPTOFILESYSTEM",
                        SourceFileName="$.Id+'.txt'",
                        TargetDirectoryName="/tmp/test/",
                        //SourceResultAction="D",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="2"
                    }
                }
            },
            new Flow
            {
                FlowId = 1,
                FlowDescription = "Flow 1",
                FlowName= "Flow 1",
                FlowStatus="A",
                FlowType="F",
                //pstart=
                //pend?=
                Procces=new List<FlowProcess>()
                {
                    ///ftp to file system
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=1,
                        SourceServerCode="4",
                        SourceDirectoryName="/test/",
                        SourceFileName="*.txt",
                        TargetDirectoryName="C:\\TEST\\FTPTOFILESYSTEM",
                        SourceResultAction="R",
                        SourceNewFileExtension=".p2",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="3",
                //           pstart=
                //pend?=
                    },
                    ///file system to sftp system
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=2,
                        SourceServerCode="3",
                        SourceDirectoryName="C:\\TEST\\FTPTOFILESYSTEM",
                        SourceFileName="$.Id+'.txt'",
                        TargetDirectoryName="/tmp/test/",
                        //SourceResultAction="D",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="1",
                //           pstart=
                //pend?=
                //        lastqueueduration=pold.start-start
                    },
                    ///sftp to ftp
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=3,
                        SourceServerCode="1",
                        SourceDirectoryName="/tmp/test/",
                        SourceFileName="$.Id+'.txt'",
                        TargetDirectoryName="/test2/",
                        //SourceResultAction="D",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="4"
                    },
                    ///ftp to s3
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=4,
                        SourceServerCode="4",
                        SourceDirectoryName="/test2/",
                        SourceFileName="$.Id+'.txt'",
                        TargetDirectoryName="/test2/",
                        //SourceResultAction="D",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="2"
                    },
                    ///s3 to s3
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=5,
                        SourceServerCode="2",
                        SourceDirectoryName="/test2/",
                        SourceFileName="$.Id+'.txt'",
                        TargetDirectoryName="/test3/",
                        //SourceResultAction="D",
                        TargetFileName="$.Id+'.txt'",
                        TargetServerCode="2"
                    }
                }
            },
            new Flow
            {
                FlowId = 2,
                FlowDescription = "Flow 2",
                FlowName= "Flow 2",
                FlowStatus="A",
                FlowType="F",
                Procces=new List<FlowProcess>()
                {
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=1,
                        SourceResultAction="D",
                        SourceServerCode="3",
                        SourceDirectoryName="c:\\TEST\\1",
                        SourceFileName="*.*",
                        TargetDirectoryName="/testZ/x",
                        TargetFileName="$.Id + '.txt'",
                        TargetServerCode="2"
                    },
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=2,
                        SourceServerCode="2",
                        SourceDirectoryName="/testZ/x",
                        SourceResultAction="D",
                        SourceFileName="$.Id + '.txt'",
                        TargetDirectoryName="/testZ/x2",
                        TargetFileName="$.Id +'_1.txt'",
                        TargetServerCode="2"
                    } ,
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=3,
                        SourceServerCode="2",
                        SourceDirectoryName="/testZ/x2",
                        SourceResultAction="D",
                        SourceFileName="$.Id +'_1.txt'",
                        TargetDirectoryName="/test/",
                        TargetFileName="$.Id +'_2.txt'",
                        TargetServerCode="4"
                    }
                }
            },
            new Flow
            {
                FlowId = 3,
                FlowDescription = "Flow 3",
                FlowName= "Flow 3",
                FlowStatus="A",
                FlowType="F",
                Procces=new List<FlowProcess>()
                {
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=1,
                        SourceResultAction="D",
                        SourceServerCode="3",
                        SourceDirectoryName="c:\\TEST\\1",
                        SourceFileName="*.*",
                        TargetDirectoryName="c:\\TEST\\2",
                        TargetFileName="$.Id + '.txt'",
                        TargetServerCode="3"
                    },
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=2,
                        SourceServerCode="3",
                        SourceDirectoryName="c:\\TEST\\2",
                        SourceResultAction="D",
                        SourceFileName="$.Id + '.txt'",
                        TargetDirectoryName="c:\\TEST\\3",
                        TargetFileName="$.Id +'_1.txt'",
                        TargetServerCode="3"
                    },
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=3,
                        SourceServerCode="3",
                        SourceDirectoryName="c:\\TEST\\3",
                        SourceResultAction="D",
                        SourceFileName="$.Id +'_1.txt'",
                        TargetDirectoryName="/tmp/test",
                        TargetFileName="$.Id +'-1.txt'",
                        TargetServerCode="1"
                    },
                    new FlowProcess
                    {
                        ProccesType=ProccesType.Copy,
                        Order=4,
                        SourceServerCode="1",
                        SourceDirectoryName="/tmp/test",
                        SourceResultAction="D",
                        SourceFileName="$.Id +'-1.txt'",
                        TargetDirectoryName="c:\\TEST\\4",
                        TargetFileName="$.Id +'_' + Now()+ '.txt'",
                        TargetServerCode="3"
                    }
                }
            }
        };
    }
}

