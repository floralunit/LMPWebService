using NLog.Web;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text;
using Quartz;
using MassTransit;
using LMPWebService.Services;
using LMPWebService.Data.Repositories;
using LMPWebService.Configuration;
using LMPWebService.Extensions;
using LMPWebService.Services.Interfaces;
using LMPWebService.Jobs;
using Quartz.Impl;
using Quartz.Spi;
using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Consumers;
using LeadsSaver_RabbitMQ.Jobs;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
builder.Services.Configure<AuthSettings>(configuration.GetSection("AuthSettings"));


builder.Services.AddDatabase(configuration);
//builder.Services.AddQuartzJobs();
builder.Services.AddScoped<IOuterMessageRepository, OuterMessageRepository>();
builder.Services.AddScoped<IOuterMessageService, OuterMessageService>();
builder.Services.AddScoped<IMassTransitPublisher, MassTransitPublisher>();
builder.Services.AddScoped<ISendEndpointProvider>(sp => sp.GetRequiredService<IBus>());
builder.Services.AddScoped<IBmwIntegrationLogger, BmwIntegrationLogger>();
builder.Services.AddHttpClient<IHttpClientLeadService, HttpClientLeadService>();
builder.Services.AddTransient<IHttpClientLeadService, HttpClientLeadService>();


builder.Services.AddSingleton<IMessageQueueService, RabbitMqService>();
builder.Services.AddScoped<ILeadProcessingService, LeadProcessingService>();
builder.Services.AddScoped<ISendStatusService, SendStatusService>();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumer<LeadStatusReceivedConsumer>();

    cfg.UsingRabbitMq((context, busCfg) =>
    {
        var rabbitMqSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

        busCfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);
        });

        busCfg.ReceiveEndpoint(rabbitMqSettings.QueueName_SendStatus_LMP, e =>
        {
            e.PrefetchCount = 16;
            e.UseMessageRetry(x => x.Interval(2, 100));

            e.ConfigureConsumer<LeadStatusReceivedConsumer>(context);
        });

        // Указываем, что сообщения типа LeadMessage будут отправляться в конкретную очередь
        busCfg.Message<RabbitMQStatusMessage_LMP>(x => x.SetEntityName(rabbitMqSettings.QueueName_SendStatus_LMP));
        busCfg.Publish<RabbitMQStatusMessage_LMP>(x =>
        {
            x.ExchangeType = "fanout";
            x.Durable = true;
        });

        // Отключаем телеметрию
        busCfg.ConfigureEndpoints(context);
    });

    // Отключаем телеметрию глобально
    cfg.AddTelemetryListener(false);
});
builder.Services.AddHostedService<BusService>();

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});
builder.Services.AddScoped<CheckErrorLeadsJob>();
builder.Services.AddScoped<CheckFieldsToTrackForStatusLMPJob>();


builder.Services.AddSingleton<IJobFactory, ScopedJobFactory>();
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

builder.Services.AddSingleton(provider =>
{
    var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
    var scheduler = schedulerFactory.GetScheduler().Result;
    scheduler.JobFactory = provider.GetRequiredService<IJobFactory>();
    return scheduler;
});

builder.Services.AddHostedService<LeadsServiceJobScheduler>();

builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.WebHost.ConfigureKestrel(options =>
{
    //var certPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
    //var certPassword = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password");
    //var certPath = "./Certificates/localhost_cert.pfx";
    //var certPath = "/app/certificates/localhost_cert.pfx";
    //var certPassword = "password";
    //var certificate = new X509Certificate2(certPath, certPassword);
    //options.ConfigureHttpsDefaults(httpsOptions =>
    //{
    //    httpsOptions.ServerCertificate = certificate;
    //});
    options.ListenAnyIP(17171);
    options.ListenAnyIP(1717, listenOptions =>
     {
         listenOptions.UseHttps("/app/certificates/localhost_cert.pfx", "password");

     });
});

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
//app.UseHttpsRedirection();
app.MapControllers();

app.Run();


