using LeadsSaverRabbitMQ.MessageModels;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Globalization;
using YourNamespace.Dtos;
using Microsoft.Extensions.Logging;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;

namespace LeadsSaver_RabbitMQ.Jobs
{
    [DisallowConcurrentExecution]
    public class CheckFieldsToTrackForStatusLMPJob : IJob
    {
        private readonly ILogger<CheckFieldsToTrackForStatusLMPJob> _logger;
        private readonly AstraDbContext _dbContext;
        private readonly IHttpClientLeadService _httpClientLeadService;

        public CheckFieldsToTrackForStatusLMPJob(
            ILogger<CheckFieldsToTrackForStatusLMPJob> logger,
            AstraDbContext dbContext,
            IHttpClientLeadService httpClientLeadService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _httpClientLeadService = httpClientLeadService ?? throw new ArgumentNullException(nameof(httpClientLeadService));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var pendingMessages = await GetPendingMessagesAsync();
                if (!pendingMessages.Any())
                {
                    _logger.LogInformation("[CheckFieldsToTrackForStatusLMPJob] Нет сообщений для обработки");
                    return;
                }

                await ProcessMessagesAsync(pendingMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в CheckFieldsToTrackForStatusLMPJob");
                throw;
            }
        }

        private async Task<List<FieldsToTrackForStatus_LMP>> GetPendingMessagesAsync()
        {
            return await _dbContext.FieldsToTrackForStatus_LMP
                .Where(x => !x.SendStatus)
                .OrderByDescending(x => x.InsDate)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task ProcessMessagesAsync(List<FieldsToTrackForStatus_LMP> pendingMessages)
        {
            foreach (var pendingMessage in pendingMessages)
            {
                try
                {
                    var lead = await _dbContext.OuterMessage
                        .FirstOrDefaultAsync(x => x.OuterMessage_ID == pendingMessage.OuterMessage_ID);

                    if (lead == null)
                    {
                        LogErrorAndContinue($"Не найдена запись OuterMessage с ID {pendingMessage.OuterMessage_ID}");
                        continue;
                    }

                    var outletCode = await GetOutletCodeAsync(lead.OuterMessageReader_ID);
                    if (string.IsNullOrEmpty(outletCode))
                    {
                        LogErrorAndContinue($"Не удалось извлечь outlet_code для сообщения {pendingMessage.OuterMessage_ID}");
                        continue;
                    }

                    await ProcessFieldUpdateAsync(pendingMessage, lead, outletCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка обработки сообщения {pendingMessage.OuterMessage_ID}");
                    await UpdateLeadErrorStatusAsync(pendingMessage.OuterMessage_ID.Value, ex.Message);
                }
            }
        }

        private async Task<string> GetOutletCodeAsync(int? outerMessageReaderId)
        {
            if (!outerMessageReaderId.HasValue) return null;

            var reader = await _dbContext.OuterMessageReader
                .Where(x => x.OuterMessageReader_ID == outerMessageReaderId)
                .Select(x => x.OuterMessageSourceName)
                .FirstOrDefaultAsync();

            return reader;
        }

        private async Task ProcessFieldUpdateAsync(FieldsToTrackForStatus_LMP pendingMessage, OuterMessage lead, string outletCode)
        {
            switch (pendingMessage.FieldName)
            {
                case "ResponsibleUserName":
                    await ProcessResponsibleUserUpdateAsync(lead, outletCode, pendingMessage.FieldContent);
                    break;

                case "DateReception":
                    await ProcessDateReceptionUpdateAsync(lead, outletCode, pendingMessage.FieldContent);
                    break;

                default:
                    _logger.LogWarning($"Неизвестное поле для обновления: {pendingMessage.FieldName}");
                    break;
            }

            pendingMessage.SendStatus = true;
            await _dbContext.SaveChangesAsync();
        }

        private async Task ProcessResponsibleUserUpdateAsync(OuterMessage lead, string outletCode, string responsibleName)
        {
            var statusResponse = await _httpClientLeadService.SendStatusResponsibleAsync(
                lead.OuterMessage_ID.ToString(),
                outletCode,
                responsibleName);

            if (statusResponse == null || !statusResponse.is_success)
            {
                var errorMessage = $"Ошибка отправки статуса о взятии в работу для ({lead.OuterMessage_ID}) с outlet_code {outletCode}: {statusResponse?.error}";
                throw new Exception(errorMessage);
            }

            UpdateLeadSuccessStatus(lead);
            _logger.LogInformation($"Сообщение ({lead.OuterMessage_ID}) с outlet_code {outletCode} было успешно обработано (ResponsibleUserName)");
        }

        private async Task ProcessDateReceptionUpdateAsync(OuterMessage lead, string outletCode, string dateString)
        {
            if (!DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                throw new Exception($"Неверный формат даты: {dateString}");
            }

            var statusDTO = new LeadStatusRequestDto()
            {
                lead_id = lead.OuterMessage_ID.ToString(),
                status = "10",
                dealer_visit_planned_date = date,
                status_comment = "Запись перенесена"
            };

            var statusResponse = await _httpClientLeadService.SendStatusAsync(statusDTO, outletCode);

            if (statusResponse == null || !statusResponse.is_success)
            {
                var errorMessage = $"Ошибка отправки статуса о переносе даты для ({lead.OuterMessage_ID}) с outlet_code {outletCode}: {statusResponse?.error}";
                throw new Exception(errorMessage);
            }

            UpdateLeadSuccessStatus(lead);
            _logger.LogInformation($"Сообщение ({lead.OuterMessage_ID}) с outlet_code {outletCode} было успешно обработано (DateReception)");
        }

        private void UpdateLeadSuccessStatus(OuterMessage lead)
        {
            lead.ProcessingStatus = 2;
            lead.ErrorCode = 0;
            lead.ErrorMessage = "";
            lead.UpdDate = DateTime.Now;
        }

        private async Task UpdateLeadErrorStatusAsync(Guid outerMessageId, string errorMessage)
        {
            var lead = await _dbContext.OuterMessage
                .FirstOrDefaultAsync(x => x.OuterMessage_ID == outerMessageId);

            if (lead != null)
            {
                lead.ErrorCode = 1;
                lead.ErrorMessage = errorMessage;
                lead.ProcessingStatus = 4;
                lead.UpdDate = DateTime.Now.AddHours(3);
                await _dbContext.SaveChangesAsync();
            }
        }

        private void LogErrorAndContinue(string message)
        {
            _logger.LogError($"[CheckFieldsToTrackForStatusLMPJob] {message}");
        }
    }
}