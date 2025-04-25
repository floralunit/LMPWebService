using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using System.Transactions;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace LMPWebService.Services
{
    public class LeadProcessingService : ILeadProcessingService
    {
        private readonly AstraDbContext _dbContext;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;
        private readonly IMassTransitPublisher _massTransitPublisher;

        private readonly ILogger<LeadProcessingService> _logger;

        public LeadProcessingService(
            IHttpClientLeadService httpClientLeadService,
            IOuterMessageService messageService,
            IMassTransitPublisher massTransitPublisher,
            AstraDbContext dbContext,
            ILogger<LeadProcessingService> logger
            )
        {
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _massTransitPublisher = massTransitPublisher;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessLeadAsync(Guid leadId, string outlet_code)
        {
            var leadData = await _httpClientLeadService.GetLeadDataAsync(leadId.ToString(), outlet_code);
            if (string.IsNullOrWhiteSpace(leadData))
            {
                _logger.LogError($"Не удалось выполнить запрос на поиск данных лида");
                return ProcessingResult.Failure("Не удалось выполнить запрос на поиск данных лида");
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var outlet_code5 = outlet_code.Length >= 5 ? outlet_code.Substring(0, 5) : outlet_code;
                    var outerReader = await _messageService.FindReaderByOutletCodeAsync(outlet_code5);
                    if (outerReader == null)
                    {
                        _logger.LogError($"[LeadProcessingService] для outlet_code {outlet_code5} не найден readerID");
                        return ProcessingResult.Failure("Внутрення ошибка обработки лида");
                    }

                    var jsonRecord = leadData.ToString();
                    var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonRecord);

                    if (jsonObject?.error != null)
                    {
                        _logger.LogError($"[LeadProcessingService] ошибка обработи лида {leadId} {outlet_code} {jsonObject?.error}");
                        return ProcessingResult.Failure("Внутрення ошибка обработки лида");
                    }

                    string? public_id = jsonObject?.lead_info?.public_id.ToString();
                    var exist = await _messageService.CheckMessageExistAsync(public_id, outerReader.OuterMessageReader_ID);

                    var statusID = jsonObject?.lead_info?.status_id.ToString();
                    if (statusID == "40") // когда пришел спам и надо удалить созданное обращение
                    {
                        _logger.LogInformation($"[LeadProcessingService] Поступил лид на дисквалификацию {leadId} {outlet_code}");
                        if (!exist)
                        {
                            _logger.LogError($"[LeadProcessingService] Лид {leadId} не поступал в обработку. Обработка статуса 40 невозможна.");
                            return ProcessingResult.Failure("Данный лид не поступал в обработку. Обработка статуса 40 невозможна."); ;
                        }
                        var messageFound = await _messageService.FindMessageAsync(leadId);
                        var eMessage = await _dbContext.EMessage.FirstOrDefaultAsync(x => x.OuterMessage_ID == messageFound.OuterMessage_ID);
                        if (eMessage == null)
                        {
                            return ProcessingResult.Success();
                        }

                        var docBase = await _dbContext.DocumentBase.Where(x => x.DocumentBase_ID == eMessage.EMessage_ID).FirstOrDefaultAsync();
                        var docState = docBase.DocumentAllowedState_ID;
                        var docID = docBase.DocumentBase_ID;
                        Guid.TryParse("1E835730-9CB3-4C47-8397-B7BF7CF0231F", out var userID); // Импорт лидов

                        // Если документ уже в состоянии "Удалено" или "Отработано" - ничего не делаем
                        if (docState == Guid.Parse("9df040fa-6543-42bd-b952-63da4d4601d6") || // Удалено
                            docState == Guid.Parse("d1c0e8ef-260c-4594-88b7-75275a7a7ddc"))  // Отработано
                        {
                            return ProcessingResult.Success();
                        }

                        if (docState == Guid.Parse("4d60972e-4c3c-41e7-a604-168056e1ef01"))
                        {
                            // Сначала выполняем переход "Назначено -> Создано"
                            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                                        $@"EXEC [dbo].[PR_DocumentBaseTransition_Set] 
                                        @DB_Id = {docID},
                                        @DAT_ID = 'BD6DCD53-152D-4053-802D-B029083DE7B0',
                                        @User_Id = {userID}");

                            // Затем выполняем переход "Создано -> Удалено"
                            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                                        $@"EXEC [dbo].[PR_DocumentBaseTransition_Set] 
                                        @DB_Id = {docID},
                                        @DAT_ID = 'E918B3EC-351B-42E5-8855-BC575B95451F',
                                        @User_Id = {userID}");
                        }
                        // Если документ в состоянии "Создано"
                        else if (docState == Guid.Parse("ae562511-a904-423a-85c0-d1ec5cfad4d4"))
                        {
                            // Выполняем переход "Создано -> Удалено"
                            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                                        $@"EXEC [dbo].[PR_DocumentBaseTransition_Set] 
                                        @DB_Id = {docID},
                                        @DAT_ID = 'E918B3EC-351B-42E5-8855-BC575B95451F',
                                        @User_Id = {userID}");
                        }
                        _logger.LogInformation($"[LeadProcessingService] Лид {leadId} успешно дисквалифицирован");
                        return ProcessingResult.Success();
                    }

                    if (exist)
                    {
                        _logger.LogError($"Данный лид уже был передан в обработку");
                        return ProcessingResult.Failure("Данный лид уже был передан в обработку");
                    }

                    string? messageText = jsonObject?.lead_info?.ToString();

                    var dbRecord = new OuterMessage
                    {
                        OuterMessage_ID = leadId,
                        OuterMessageReader_ID = outerReader.OuterMessageReader_ID,
                        MessageOuter_ID = public_id,
                        ProcessingStatus = 0,
                        MessageText = messageText,
                        InsDate = DateTime.Now.AddHours(3),
                        UpdDate = DateTime.Now.AddHours(3)
                    };

                    await _messageService.SaveMessageAsync(dbRecord);

                    var message = new RabbitMQLeadMessage_LMP
                    {
                        OuterMessage_ID = leadId,
                        OuterMessageReader_ID = outerReader.OuterMessageReader_ID,
                        OutletCode = outlet_code5
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

                    _logger.LogError(errorMes);

                    return ProcessingResult.Failure($"Внутренняя ошибка обработки лида: {errorMes}");
                }
            }

        }
    }
}
