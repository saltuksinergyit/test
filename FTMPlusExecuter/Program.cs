using FTMCommon.Common.MainService;
using FTMPlus.Common.Db;
using FTMPlus.Common.MainService;
using FTMPlus.Common.Processor;
using FTMPlus.Executer.MainService;
using FTMPlusExecuter.Executer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);




builder.Services.AddSingleton<ServerService>();
builder.Services.AddSingleton(ConnectionMultiplexer.Connect("127.0.0.1:6379"));

//builder.Services.AddDbContext<TransactionContext>(options =>
//    options.UseSqlServer("Server=sql.in.sinergyit.com;Database=Test_FTM;User Id=sa;Password=1qazxsw2;Encrypt=False;"));
builder.Services.AddDbContext<ListenerContext>(options =>
    options.UseSqlServer("Server=sql.in.sinergyit.com;Database=Test_FTM;User Id=sa;Password=1qazxsw2;Encrypt=False;").UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddTransient<RabbitMQManager>();

builder.Services.AddSingleton<ServerService>(); 


builder.Services.AddScoped<DistributedLockService>();
builder.Services.AddSingleton<DistributedLockMasterService>();
builder.Services.AddHostedService<ExecuterRunningService>();



builder.Services.AddTransient<FileAdaptor>();
builder.Services.AddTransient<FTPAdaptor>();
builder.Services.AddTransient<S3Adaptor>();
builder.Services.AddTransient<SFTPAdaptor>();


builder.Services.AddScoped<FlowService>();
builder.Services.AddScoped<CopyProcessor>();
builder.Services.AddScoped<MergeProcessor>();
builder.Services.AddScoped<RequestProcessor>();
builder.Services.AddScoped<TrasformProcessor>();
builder.Services.AddScoped<ExpressionServices>();


builder.Services.AddScoped<ExecuterService>();




// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
