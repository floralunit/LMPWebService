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
            _httpClientLeadService = httpClientLeadService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var pendingMessages = await GetPendingMessagesAsync();
                if (!pendingMessages.Any())
                {
                    _logger.LogDebug("Нет сообщений для обработки");
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
                .ToListAsync();
        }

        private async Task ProcessMessagesAsync(List<FieldsToTrackForStatus_LMP> pendingMessages)
        {
            foreach (var message in pendingMessages)
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var result = await ProcessSingleMessageAsync(message);
                    if (result)
                    {
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Ошибка обработки сообщения {message.OuterMessage_ID}");
                    await UpdateLeadErrorStatusAsync(message.OuterMessage_ID.Value, ex.Message);
                }
            }
        }

        private async Task<bool> ProcessSingleMessageAsync(FieldsToTrackForStatus_LMP message)
        {
            var lead = await _dbContext.OuterMessage
                .FirstOrDefaultAsync(x => x.OuterMessage_ID == message.OuterMessage_ID);

            if (lead == null)
            {
                _logger.LogError($"Не найдена запись OuterMessage с ID {message.OuterMessage_ID}");
                return false;
            }

            var outletCode = await GetOutletCodeAsync(lead.OuterMessageReader_ID);
            if (string.IsNullOrEmpty(outletCode))
            {
                _logger.LogError($"Не удалось извлечь outlet_code для сообщения {message.OuterMessage_ID}");
                return false;
            }

            bool processingResult = message.FieldName switch
            {
                "ResponsibleUserName" => await ProcessResponsibleUserUpdateAsync(lead, outletCode, message.FieldContent),
                "DateReception" => await ProcessDateReceptionUpdateAsync(lead, outletCode, message.FieldContent),
                _ => HandleUnknownFieldType(message.FieldName)
            };

            if (processingResult)
            {
                message.SendStatus = true;
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }

        private bool HandleUnknownFieldType(string fieldName)
        {
            _logger.LogWarning($"Неизвестное поле для обновления: {fieldName}");
            return false;
        }

        private async Task<string> GetOutletCodeAsync(int? outerMessageReaderId)
        {
            if (!outerMessageReaderId.HasValue) return null;

            return await _dbContext.OuterMessageReader
                .Where(x => x.OuterMessageReader_ID == outerMessageReaderId)
                .Select(x => x.OuterMessageReaderName)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> ProcessResponsibleUserUpdateAsync(OuterMessage lead, string outletCode, string responsibleName)
        {
            var statusResponse = await _httpClientLeadService.SendStatusResponsibleAsync(
                lead.OuterMessage_ID.ToString(),
                outletCode,
                responsibleName);

            if (statusResponse == null || !statusResponse.is_success)
            {
                throw new Exception($"Ошибка отправки статуса ResponsibleUserName: {statusResponse?.error}");
            }

            UpdateLeadSuccessStatus(lead);
            _logger.LogInformation($"[ResponsibleUserName] Сообщение {lead.OuterMessage_ID} успешно обработано");
            return true;
        }

        private async Task<bool> ProcessDateReceptionUpdateAsync(OuterMessage lead, string outletCode, string dateString)
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
                throw new Exception($"Ошибка отправки статуса DateReception: {statusResponse?.error}");
            }

            UpdateLeadSuccessStatus(lead);
            _logger.LogInformation($"[DateReception] Сообщение {lead.OuterMessage_ID} успешно обработано");
            return true;
        }

        private void UpdateLeadSuccessStatus(OuterMessage lead)
        {
            lead.ProcessingStatus = 2;
            lead.ErrorCode = 0;
            lead.ErrorMessage = string.Empty;
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
    }
}