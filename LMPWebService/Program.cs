using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography.X509Certificates;
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


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));


builder.Services.AddDatabase(configuration);
//builder.Services.AddQuartzJobs();
builder.Services.AddScoped<IOuterMessageRepository, OuterMessageRepository>();
builder.Services.AddScoped<IOuterMessageService, OuterMessageService>();
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration);
builder.Services.AddScoped<IMassTransitPublisher, MassTransitPublisher>();
builder.Services.AddScoped<ISendEndpointProvider>(sp => sp.GetRequiredService<IBus>());
builder.Services.AddHttpClient<IHttpClientLeadService, HttpClientLeadService>();
builder.Services.AddSingleton<IMessageQueueService, RabbitMqService>();
builder.Services.AddScoped<ILeadProcessingService, LeadProcessingService>();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.WebHost.ConfigureKestrel(options =>
{
    //var certPath = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Path");
    //var certPassword = Environment.GetEnvironmentVariable("ASPNETCORE_Kestrel__Certificates__Default__Password");
    var certPath = "./Certificates/localhost_cert.pfx";
    var certPassword = "password";
    var certificate = new X509Certificate2(certPath, certPassword);
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ServerCertificate = certificate;
    });
});

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
