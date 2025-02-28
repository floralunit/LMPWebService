using MassTransit;
using System;
using System.Threading.Tasks;
using LMPWebService.DTO;

namespace LMPWebService.Consumers
{
    public class LeadStatusReceivedConsumer : IConsumer<LeadReceivedMessage>
    {
        private readonly ILogger<LeadReceivedMessage> _logger;
        public LeadStatusReceivedConsumer(ILogger<LeadReceivedMessage> logger)
        {
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<LeadReceivedMessage> context)
        {
            Console.WriteLine($"Получено сообщение: {context.Message.LeadId} - {context.Message.Event}");
            await Task.CompletedTask;
        }
    }
}
