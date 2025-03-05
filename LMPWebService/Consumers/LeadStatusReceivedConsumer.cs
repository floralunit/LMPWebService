using MassTransit;
using System;
using System.Threading.Tasks;
using LMPWebService.DTO;
using LeadsSaverRabbitMQ.MessageModels;

namespace LMPWebService.Consumers
{
    public class LeadStatusReceivedConsumer : IConsumer<RabbitMQStatusMessage_LMP>
    {
        private readonly ILogger<LeadStatusReceivedConsumer> _logger;
        public LeadStatusReceivedConsumer(ILogger<LeadStatusReceivedConsumer> logger)
        {
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<RabbitMQStatusMessage_LMP> context)
        {
            Console.WriteLine($"Получено сообщение об изменении статуса: {context.Message.Message_ID} - {context.Message.Lead_Id}");
            await Task.CompletedTask;
        }
    }
}
