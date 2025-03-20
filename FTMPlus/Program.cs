using FTMPlus.Common.MainService;
using FTMPlus.Common.Processor;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using FTMPlus.Common.Db;
using FTMCommon.Common.MainService;


public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
         

        //var x=new SFTPProccesor(new FTMPlus.Entities.Flow { 
        //    Procces=new List<FTMPlus.Entities.FlowProcess>() { 
        //        new FTMPlus.Entities.FlowProcess { 
        //            Order=1, SourceServerCode="1", SourceDir="/kubs/", SoruceFilePatern="*.*" , TargetDir="/kubs/", TargetFilePatern="*.*", TargetServerCode="1"
        //        } 
        //    } 
        //},1);




        //var x = new S3Adaptor(new FTMPlus.Entities.Flow
        //{
        //    Procces = new List<FTMPlus.Entities.FlowProcess>() {
        //        new FTMPlus.Entities.FlowProcess {
        //            Order=1, SourceServerCode="2", SourceDir="/test1", SoruceFilePatern="*.*" , TargetDir="/", TargetFilePatern="*.*", TargetServerCode="1"
        //        }
        //    }
        //}, 1);

        //x.ConnectAsync().Wait();



        //x.FindFilesAsync().Wait();

        // Add services to the container.


        var applicationUrl = builder.Configuration["applicationUrl"] ?? "http://localhost:5215"; // Varsayılan değer 

        Console.WriteLine($"url => ${applicationUrl}"); 


        builder.Services.AddSingleton(ConnectionMultiplexer.Connect("127.0.0.1:6379"));

        builder.Services.AddDbContext<ListenerContext>(options =>
            options.UseSqlServer("Data Source=10.0.11.175;Initial Catalog=Test_FTM;Persist Security Info=True;User ID=sa;Password=1qazxsw2;Encrypt=False").UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        builder.Services.AddTransient<RabbitMQManager>();

        builder.Services.AddSingleton<ServerService>(); 
        builder.Services.AddScoped<ExpressionServices>();
        builder.Services.AddScoped<DistributedLockService>();



        builder.Services.AddSingleton<DistributedLockMasterService>();
        builder.Services.AddHostedService<ListenerIsMaster>();
        builder.Services.AddHostedService<MasterRunningService>();
        builder.Services.AddHostedService<ListenerRunningService>();
         

        builder.Services.AddTransient<FileAdaptor>();
        builder.Services.AddTransient<FTPAdaptor>();
        builder.Services.AddTransient<S3Adaptor>();
        builder.Services.AddTransient<SFTPAdaptor>();



        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();

        app.MapControllers(); 
        app.Run(applicationUrl);
    }
}