using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LMPWebService.Configuration;
using LMPWebService.Services.Interfaces;

namespace LMPWebService.Services
{
    public class RabbitMqService : IMessageQueueService
    {
        private readonly string _hostName;
        private readonly string _sendLeadQueueName;
        private readonly string _statusLeadQueueName;
        private readonly string _virtualhost;
        private readonly string _username;
        private readonly string _password;
        private readonly ILogger<RabbitMqService> _logger;

        public RabbitMqService(IOptions<RabbitMqSettings> options, ILogger<RabbitMqService> logger)
        {
            _hostName = options.Value.Host;
            _virtualhost = options.Value.VirtualHost;
            _username = options.Value.Username;
            _password = options.Value.Password;
            _sendLeadQueueName = options.Value.SendLeadsLmpQueueName;
            _statusLeadQueueName = options.Value.SendStatusLmpQueueName;
            _logger = logger;
        }

        public async Task SendMessageAsync(string queueName, string message)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    VirtualHost = _virtualhost,
                    UserName = _username,
                    Password = _password
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                var properties = new BasicProperties
                {
                    Persistent = true
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    mandatory: false,
                    body: body);

                _logger.LogInformation($"[RabbitMQ] Сообщение отправлено в очередь {queueName}: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения в RabbitMQ");
                throw;
            }
        }
    }
}
