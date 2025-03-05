using MassTransit;
using LMPWebService.Consumers;
using LMPWebService.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMPWebService.Extensions
{
    public static class MassTransitExtension
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(
            this IServiceCollection services, IConfiguration configuration)
        {
            var rabbitMqSettings = configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();

            if (rabbitMqSettings == null)
                throw new InvalidOperationException("RabbitMQ configuration is missing!");

            services.AddMassTransit(x =>
            {
                x.AddConsumer<LeadStatusReceivedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
                    {
                        h.Username(rabbitMqSettings.Username);
                        h.Password(rabbitMqSettings.Password);
                    });

                    cfg.ReceiveEndpoint(rabbitMqSettings.QueueName_SendStatus_LMP, e =>
                    {
                        e.ConfigureConsumer<LeadStatusReceivedConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}
