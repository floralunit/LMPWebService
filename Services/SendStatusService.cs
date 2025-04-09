using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Dtos;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace LMPWebService.Services
{
    public interface ISendStatusService
    {
        Task SendStatusResponsibleAsync(Guid messageID, string outletCode, string responsibleName);
        Task SendStatusAsync(RabbitMQStatusMessage_LMP message);
    }
    public class SendStatusService : ISendStatusService
    {

        private readonly ILogger<SendStatusService> _logger;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;
        private readonly AstraDbContext _dbContext;

        public SendStatusService(ILogger<SendStatusService> logger,
                                IHttpClientLeadService httpClientLeadService,
                                IOuterMessageService messageService,
                                AstraDbContext dbContext)
        {
            _logger = logger;
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _dbContext = dbContext;
        }

        public async Task SendStatusResponsibleAsync(Guid messageID, string outletCode, string responsibleName)
        {
            var entityMessage = await _messageService.FindMessageAsync(messageID);

            if (entityMessage == null)
            {
                _logger.LogError($"[SendStatusService] Message ({messageID}) was not found in OuterMessage table", DateTimeOffset.Now);
                return;
            }
            try
            {

                var statusResponse = await _httpClientLeadService.SendStatusResponsibleAsync(messageID.ToString(), outletCode, responsibleName);

                if (!statusResponse.is_success || statusResponse == null)
                {
                    _logger.LogError(statusResponse?.error, DateTimeOffset.Now);
                    entityMessage.ErrorCode = 1;
                    entityMessage.ErrorMessage = statusResponse?.error;
                    entityMessage.ProcessingStatus = 4;
                    entityMessage.UpdDate = DateTime.Now.AddHours(3);
                    await _messageService.UpdateMessageAsync(entityMessage);
                    return;
                }

                entityMessage.ProcessingStatus = 2;
                entityMessage.ErrorCode = 0;
                entityMessage.ErrorMessage = "";
                entityMessage.UpdDate = DateTime.Now;
                await _messageService.UpdateMessageAsync(entityMessage);
                _logger.LogInformation($"Для лида id {entityMessage.MessageOuter_ID} успешно обновлен статус Взят в работу", DateTimeOffset.Now);

            }
            catch (Exception e)
            {
                var errorMes = $"[SendStatusService] Error processing messageId={messageID}: {e.Message}";
                if (e.InnerException != null)
                    errorMes += $"\nInnerException: {e.InnerException.Message}";

                _logger.LogError(errorMes, DateTimeOffset.Now);

                entityMessage.ErrorCode = 1;
                entityMessage.ErrorMessage = errorMes;
                entityMessage.ProcessingStatus = 4;
                entityMessage.UpdDate = DateTime.Now.AddHours(3);
                await _messageService.UpdateMessageAsync(entityMessage);
            }

        }
        public async Task SendStatusAsync(RabbitMQStatusMessage_LMP message)
        {
            _logger.LogInformation($"[SendStatusService] Получено сообщение на обработку статуса emessage = {message.astra_document_id}", DateTimeOffset.Now);

            var eMessage = await _dbContext.EMessage.FirstOrDefaultAsync(x => x.EMessage_ID == message.astra_document_id);

            if (eMessage == null)
            {
                await Task.CompletedTask;
            }

            var lead = await _dbContext.OuterMessage.FirstOrDefaultAsync(x => x.OuterMessage_ID == eMessage.OuterMessage_ID);

            if (lead == null)
            {
                await Task.CompletedTask;
            }
            _logger.LogInformation($"[SendStatusService] SendStatus started for emessage = {eMessage.DocumentBase_ID}, lead = {lead.MessageOuter_ID}", DateTimeOffset.Now);

            try
            {
                var outletCode = "";
                var statusDTO = new LeadStatusRequestDto()
                {
                    lead_id = lead.OuterMessage_ID.ToString()
                };
                var statusID = 0;

                //if (message.astra_document_status_id == "") //заявка на ремонт
                //{
                //    statusID = 40;
                //}



                var statusResponse = await _httpClientLeadService.SendStatusAsync(statusDTO, outletCode);

                if (!statusResponse.is_success || statusResponse == null)
                {
                    _logger.LogError(statusResponse?.error, DateTimeOffset.Now);
                    lead.ErrorCode = 1;
                    lead.ErrorMessage = statusResponse?.error;
                    lead.ProcessingStatus = 4;
                    lead.UpdDate = DateTime.Now.AddHours(3);
                    await _messageService.UpdateMessageAsync(lead);
                    return;
                }

                lead.ProcessingStatus = 2;
                lead.ErrorCode = 0;
                lead.ErrorMessage = "";
                lead.UpdDate = DateTime.Now;
                await _messageService.UpdateMessageAsync(lead);
                _logger.LogInformation($"Для лида id {lead.MessageOuter_ID} успешно обновлен статус {statusDTO.status}", DateTimeOffset.Now);

            }
            catch (Exception e)
            {
                var errorMes = $"[SendStatusService] Error processing messageId={lead.MessageOuter_ID}: {e.Message}";
                if (e.InnerException != null)
                    errorMes += $"\nInnerException: {e.InnerException.Message}";

                _logger.LogError(errorMes, DateTimeOffset.Now);

                lead.ErrorCode = 1;
                lead.ErrorMessage = errorMes;
                lead.ProcessingStatus = 4;
                lead.UpdDate = DateTime.Now.AddHours(3);
                await _messageService.UpdateMessageAsync(lead);
            }

        }
    }
}
