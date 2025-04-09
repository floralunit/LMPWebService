using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using System.Transactions;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace LMPWebService.Services
{
    public class LeadProcessingService : ILeadProcessingService
    {
        private readonly AstraDbContext _dbContext;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;
        private readonly IMassTransitPublisher _massTransitPublisher;

        public LeadProcessingService(
            IHttpClientLeadService httpClientLeadService,
            IOuterMessageService messageService,
            IMassTransitPublisher massTransitPublisher,
            AstraDbContext dbContext
            )
        {
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _massTransitPublisher = massTransitPublisher;
            _dbContext = dbContext;
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

                    var statusID = jsonObject?.lead_info?.status_id.ToString();
                    if (statusID == "40") // когда пришел спам и надо удалить созданное обращение
                    {
                        if (!exist)
                        {
                            return ProcessingResult.Failure("Данный лид не поступал в обработку. Обработка статуса 40 невозможна."); ;
                        }
                        var messageFound = await _messageService.FindMessageAsync(leadId);
                        var eMessage = await _dbContext.EMessage.FirstOrDefaultAsync(x => x.OuterMessage_ID == messageFound.OuterMessage_ID);
                        if (eMessage == null)
                        {
                            return ProcessingResult.Success();
                        }

                        var docState = eMessage.DocumentAllowedState_ID;
                        var docID = eMessage.DocumentBase_ID;
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

                        return ProcessingResult.Success();
                    }

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
                        InsDate = DateTime.Now.AddHours(3),
                        UpdDate = DateTime.Now.AddHours(3)
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
