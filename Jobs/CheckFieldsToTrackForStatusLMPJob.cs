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
                var pendingMessages = await _dbContext.FieldsToTrackForStatus_LMP
                                            .Where(x => !x.SendStatus)
                                            .OrderByDescending(x => x.InsDate)
                                            .ToListAsync();
                //if (!pendingMessages.Any())
                //{
                //    _logger.LogInformation("[CheckFieldsToTrackForStatusLMPJob] Нет сообщений для обработки");
                //    return;
                //}

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

                        bool isProcessed = false;

                        if (pendingMessage.FieldName == "ResponsibleUserName")
                        {
                            isProcessed = await ProcessResponsibleUserUpdateAsync(lead, outletCode, pendingMessage.FieldContent);
                        }
                        else if (pendingMessage.FieldName == "DateReception")
                        {
                            isProcessed = await ProcessDateReceptionUpdateAsync(lead, outletCode, pendingMessage.FieldContent);
                        }
                        else
                        {
                            _logger.LogWarning($"Неизвестное поле для обновления: {pendingMessage.FieldName}");
                        }

                        if (isProcessed)
                        {
                            pendingMessage.SendStatus = true;
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Ошибка обработки сообщения {pendingMessage.OuterMessage_ID}");
                        await UpdateLeadErrorStatusAsync(pendingMessage.OuterMessage_ID.Value, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в CheckFieldsToTrackForStatusLMPJob");
                throw;
            }
        }

        private async Task<string> GetOutletCodeAsync(int? outerMessageReaderId)
        {
            if (!outerMessageReaderId.HasValue) return null;

            var reader = await _dbContext.OuterMessageReader
                .Where(x => x.OuterMessageReader_ID == outerMessageReaderId)
                .Select(x => x.OuterMessageReaderName)
                .FirstOrDefaultAsync();

            return reader;
        }

        private async Task<bool> ProcessResponsibleUserUpdateAsync(OuterMessage lead, string outletCode, string responsibleName)
        {
            try
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
                _logger.LogInformation($"[ResponsibleUserName] Сообщение ({lead.OuterMessage_ID}) с outlet_code {outletCode} успешно обработано");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ResponsibleUserName] Ошибка для сообщения {lead.OuterMessage_ID}");
                await UpdateLeadErrorStatusAsync(lead.OuterMessage_ID, ex.Message);
                return false;
            }
        }

        private async Task<bool> ProcessDateReceptionUpdateAsync(OuterMessage lead, string outletCode, string dateString)
        {
            try
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
                _logger.LogInformation($"[DateReception] Сообщение ({lead.OuterMessage_ID}) с outlet_code {outletCode} успешно обработано");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DateReception] Ошибка для сообщения {lead.OuterMessage_ID}");
                await UpdateLeadErrorStatusAsync(lead.OuterMessage_ID, ex.Message);
                return false;
            }
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