using MassTransit;
using System;
using System.Threading.Tasks;
using LMPWebService.DTO;
using LeadsSaverRabbitMQ.MessageModels;
using Microsoft.Extensions.Options;
using LMPWebService.Services;

namespace LMPWebService.Consumers
{
    public class LeadStatusReceivedConsumer : IConsumer<RabbitMQStatusMessage_LMP>
    {
        private readonly ILogger<LeadStatusReceivedConsumer> _logger;

        private readonly ISendStatusService _sendStatusService;


        public LeadStatusReceivedConsumer(
                                ILogger<LeadStatusReceivedConsumer> logger,
                                ISendStatusService sendStatusService)
        {
            _logger = logger;
            _sendStatusService = sendStatusService;
        }
        public async Task Consume(ConsumeContext<RabbitMQStatusMessage_LMP> context)
        {
            _logger.LogInformation($"NEW LMP STATUS MESSAGE Received: LMP Status Message ({context.Message.Message_ID}))");
            await _sendStatusService.SendStatusAsync(context.Message.Message_ID, context.Message.Outlet_Code);
        }
    }
}
