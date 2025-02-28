using LMPWebService.Models;
using LMPWebService.Services.Interfaces;

namespace LMPWebService.Services
{
    public class LeadProcessingService : ILeadProcessingService
    {
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;
        private readonly IMassTransitPublisher _massTransitPublisher;

        public LeadProcessingService(
            IHttpClientLeadService httpClientLeadService,
            IOuterMessageService messageService,
            IMassTransitPublisher massTransitPublisher)
        {
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _massTransitPublisher = massTransitPublisher;
        }

        public async Task<ProcessingResult> ProcessLeadAsync(string leadId)
        {
            //var leadData = await _httpClientLeadService.GetLeadDataAsync(leadId.ToString());
            var leadData = "1212";
            if (string.IsNullOrWhiteSpace(leadData))
                return ProcessingResult.Failure("Некорректные данные");

            var leadDataOuterMessage = new OuterMessage
            {
                MessageOuter_ID = leadId,
                MessageText = leadData
            };

            var messageId = await _messageService.SaveMessageAsync(leadDataOuterMessage);

            var message = new RabbitMQLeadMessageLmp
            {
                Message_ID = messageId,
                Center_ID = new Guid("6DDE8DAB-D21C-4CB2-8355-A9525BBA5EFF"),
                BrandName = "BMW"
            };

            await _massTransitPublisher.SendLeadReceivedMessage(message);

            return ProcessingResult.Success();
        }
    }
}
