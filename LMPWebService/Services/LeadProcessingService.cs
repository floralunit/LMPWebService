using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Configuration;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using static MassTransit.Monitoring.Performance.BuiltInCounters;
using System.Text.Json.Nodes;
using System.Transactions;
using Newtonsoft.Json;

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

        public async Task<ProcessingResult> ProcessLeadAsync(Guid leadId, string outlet_code)
        {
            var leadData = await _httpClientLeadService.GetLeadDataAsync(leadId.ToString(), outlet_code);
            if (string.IsNullOrWhiteSpace(leadData))
                return ProcessingResult.Failure("Не удалось выполнить запрос на поиск данных лида");

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var jsonRecord = leadData.ToString();
                    var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonRecord);

                    if (jsonObject?.error != null)
                    {
                        return ProcessingResult.Failure(jsonObject?.error.ToString());
                    }

                    string? public_id = jsonObject?.lead_info?.public_id.ToString();


                    var exist = await _messageService.CheckMessageExistAsync(public_id, 25);

                    if (exist)
                    {
                        return ProcessingResult.Failure("Данный лид уже был передан в обработку");
                    }

                    string? messageText = jsonObject?.lead_info?.ToString();

                    var dbRecord = new OuterMessage
                    {
                        OuterMessage_ID = leadId,
                        OuterMessageReader_ID = 25,
                        MessageOuter_ID = public_id,
                        ProcessingStatus = 0,
                        MessageText = messageText,
                        InsDate = DateTime.Now,
                        UpdDate = DateTime.Now
                    };

                    await _messageService.SaveMessageAsync(dbRecord);

                    var message = new RabbitMQLeadMessage_LMP
                    {
                        Message_ID = leadId,
                        OutletCode = outlet_code
                    };

                    //throw new RabbitMqConnectionException("Искусственная ошибка при публикации в RabbitMQ");

                    await _massTransitPublisher.SendLeadReceivedMessage(message);
                    scope.Complete();
                    return ProcessingResult.Success();
                }
                catch (Exception ex)
                {
                    var errorMes = $"Error while saving lead and pushing to RabbitMQ";
                    if (ex.InnerException != null)
                        errorMes += $"\nInnerException: {ex.InnerException.Message}";

                    return ProcessingResult.Failure($"Внутренняя ошибка обработки лида: {errorMes}");
                }
            }

        }
    }
}
