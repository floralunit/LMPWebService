using LMPWebService.Services.Interfaces;

namespace LMPWebService.Services
{
    public interface ISendStatusService
    {
        Task SendStatusAsync(Guid messageID, string outletCode);
    }
    public class SendStatusService : ISendStatusService
    {

        private readonly ILogger<SendStatusService> _logger;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;

        public SendStatusService(ILogger<SendStatusService> logger,
                                IHttpClientLeadService httpClientLeadService,
                                IOuterMessageService messageService)
        {
            _logger = logger;
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
        }

        public async Task SendStatusAsync(Guid messageID, string outletCode)
        {
            var entityMessage = await _messageService.FindMessageAsync(messageID);

            if (entityMessage == null)
            {
                _logger.LogError($"[SendStatusService] Message ({messageID}) was not found in OuterMessage table", DateTimeOffset.Now);
                return;
            }
            try
            {

                var statusResponse = await _httpClientLeadService.SendStatusAsync(messageID.ToString(), outletCode);

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
    }
}
