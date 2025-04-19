using LeadsSaverRabbitMQ.MessageModels;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Data;
using Newtonsoft.Json;
using LMPWebService.Services;
using System.Net.WebSockets;

namespace LeadsSaver_RabbitMQ.Jobs
{
    [DisallowConcurrentExecution]
    public class CheckFieldsToTrackForStatusLMPJob : IJob
    {
        private readonly ILogger<CheckFieldsToTrackForStatusLMPJob> _logger;
        private readonly AstraDbContext _dbContext;

        private readonly ISendStatusService _sendStatusService;

        public CheckFieldsToTrackForStatusLMPJob(
                                ILogger<CheckFieldsToTrackForStatusLMPJob> logger,
                                AstraDbContext dbContext,
                                ISendStatusService sendStatusService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _sendStatusService = sendStatusService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            //_logger.LogInformation("");
            //_logger.LogInformation("");
            //_logger.LogInformation($"Job {context.JobDetail.Key.Name} started", DateTimeOffset.Now);

            try
            {
                var pendingMessages = await _dbContext.FieldsToTrackForStatus_LMP
                    .Where(x => !x.SendStatus)
                    .OrderByDescending(x => x.InsDate)
                    .ToListAsync();

                if (!pendingMessages.Any())
                {
                    //_logger.LogInformation("[CheckFieldsToTrackForStatusLMPJob] Нет сообщений со статусом 0 для обработки.");
                    return;
                }

                foreach (var pendingMessage in pendingMessages)
                {
                    try
                    {
                        var lead = await _dbContext.OuterMessage
                            .FirstOrDefaultAsync(x => x.OuterMessage_ID == pendingMessage.OuterMessage_ID);

                        if (lead == null)
                        {
                            _logger.LogWarning($"[CheckFieldsToTrackForStatusLMPJob] Не найдена запись OuterMessage с ID {pendingMessage.OuterMessage_ID}");
                            continue;
                        }

                        var reader = await _dbContext.OuterMessageReader.FirstOrDefaultAsync(x => x.OuterMessageReader_ID == lead.OuterMessageReader_ID);
                        var outlet_code = reader?.OuterMessageSourceName;

                        if (string.IsNullOrEmpty(outlet_code))
                        {
                            _logger.LogWarning($"[CheckFieldsToTrackForStatusLMPJob] Не удалось извлечь outlet_code для сообщения {pendingMessage.OuterMessage_ID}");
                            continue;
                        }

                        if (pendingMessage.FieldName == "ResponsibleUserName")
                        {
                            var isSuccess = await _sendStatusService.SendStatusResponsibleAsync(lead.OuterMessage_ID, outletCode: outlet_code, responsibleName: pendingMessage?.FieldContent);
                            if (isSuccess)
                            {
                                pendingMessage.SendStatus = true;
                                await _dbContext.SaveChangesAsync();
                                _logger.LogInformation($"[CheckFieldsToTrackForStatusLMPJob] Сообщение ({lead.OuterMessage_ID}) с outlet_code {outlet_code} и responsibleUser_ID {pendingMessage?.FieldContent} было успешно получено и передано на отправку статуса взятие в работу");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[CheckFieldsToTrackForStatusLMPJob] Ошибка обработки сообщения {pendingMessage.OuterMessage_ID}: {ex.Message}");

                        var lead = await _dbContext.OuterMessage
                            .FirstOrDefaultAsync(x => x.OuterMessage_ID == pendingMessage.OuterMessage_ID);

                        if (lead != null)
                        {
                            lead.ErrorCode = 1;
                            lead.ErrorMessage = ex.Message;
                            lead.ProcessingStatus = 4;
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка в CheckFieldsToTrackForStatusLMPJob: {ex.Message}");
                throw;
            }




            //var leadsLMP = await _dbContext.OuterMessage.Where(x => (x.ProcessingStatus == 1 || x.ProcessingStatus == 4) && x.OuterMessageReader_ID == 25).ToListAsync();


            //foreach (var lead in leadsLMP)
            //{
            //    var query = "Exec [dbo].[PR_EMessage_Find]  @OuterMessage_ID = {0}, @DocumentSubtype_ID = 140, @DocumentAllowedState = '<root><Al Id=\"4d60972e-4c3c-41e7-a604-168056e1ef01\" /><Al Id=\"d1c0e8ef-260c-4594-88b7-75275a7a7ddc\" /><Al Id=\"ae562511-a904-423a-85c0-d1ec5cfad4d4\" /></root>', @DocumentBaseNumber = NULL, @DocumentBaseDateFrom = NULL, @DocumentBaseDateTo = NULL, @TopRows = NULL, @Department = NULL, @CFR = NULL, @Project = '<root><OS Id=\"840739be-b80f-4d4a-b7d5-97495e14559e\" /><OS Id=\"fa5b703e-fb5e-4f7b-abb6-01f8a8fd3a6f\" /><OS Id=\"62404504-abeb-44a4-b9eb-cf55730ca26c\" /><OS Id=\"e17915cd-0613-424f-9623-d7c4d8498a10\" /><OS Id=\"68fe6544-205e-40d3-a18b-87f7b40cdf47\" /><OS Id=\"2d673fec-bf76-4258-8c20-33dc74fe8206\" /><OS Id=\"479b4017-3076-445b-83f8-25505f6d4af2\" /><OS Id=\"6199c7c3-c1d4-4e47-ac28-590db4c5f8ce\" /><OS Id=\"6b612741-c395-4c13-b9ac-60577591f03d\" /><OS Id=\"0bd38f42-f175-4850-af6a-f52079b6eafb\" /><OS Id=\"31a9b7ae-d9d0-4921-9dda-b7723c1e762d\" /><OS Id=\"069a7ec0-f094-4994-b151-0f99dd69e3c6\" /><OS Id=\"f172f6fe-de58-4469-bea0-fab1911fd148\" /><OS Id=\"1cc1b143-c569-443d-9412-01b15a7bdc2c\" /><OS Id=\"25723626-ff7b-4f77-be27-4233f2df0ce8\" /><OS Id=\"67257504-9fc9-4728-900e-ceaa4a326f32\" /><OS Id=\"a96301ec-ac40-435b-aa6f-d3da67b066e8\" /><OS Id=\"516b8bfe-699e-4f36-aa57-3227f433ae86\" /><OS Id=\"6ba19a34-5144-4734-9bd2-df85520c8c94\" /><OS Id=\"5659f27b-5722-445c-a071-decb0207ad70\" /><OS Id=\"0749585b-eb14-4d03-b57a-96aeea3db8d4\" /><OS Id=\"f6227bf4-a966-4ab6-95e7-69333c107d69\" /><OS Id=\"a5372887-4779-42c4-a088-ba81a3d6c799\" /><OS Id=\"e655034f-5462-4ae9-965c-546573db33b1\" /><OS Id=\"8c5cfc78-a6dd-4005-a303-5c79843ce87a\" /><OS Id=\"9ac6d1a1-d83d-4787-944a-6a4289c2bba1\" /><OS Id=\"261f0768-dab2-451d-8694-66d8303b1950\" /><OS Id=\"6f6ab29e-da83-4f41-9dde-e7fe21790280\" /></root>', @Center = '<root><OS Id=\"82a032ba-47ca-11ee-b807-00505601166d\" /><OS Id=\"f17ec854-66cd-11ee-b809-00505601166d\" /><OS Id=\"eee651de-6157-11ef-b807-0050560128bc\" /><OS Id=\"66b82a01-4622-4b45-8ab4-0f5935a022c5\" /><OS Id=\"c87295f3-313c-4253-8c37-ac2ea6a8244c\" /><OS Id=\"f375845e-e139-4048-8b52-2023f006719c\" /><OS Id=\"28d71ec9-2a4d-4fac-9681-3e22b31b84a1\" /><OS Id=\"9cd09cb6-3f81-4dc5-b1e3-30d0ce2c4990\" /><OS Id=\"8f534b8e-a7ed-44ea-b30e-dbc53f8ea08b\" /><OS Id=\"8719816a-29a7-4e25-ab18-81cbaf28f95f\" /><OS Id=\"ce847da9-51e0-40bb-a76a-90c5e8e5ea7d\" /><OS Id=\"97b6b063-319c-4020-9710-ca577aef18bc\" /><OS Id=\"91e1e5d4-a3f3-4927-9d16-8bb4e4ede41a\" /><OS Id=\"24774de6-fdf3-44e1-af56-d41ae1add566\" /><OS Id=\"a6c88432-e250-4d4f-aefb-0ba8ce1fe4fb\" /><OS Id=\"53e571c6-8b39-479e-b130-4852fd47a74e\" /><OS Id=\"c84196e4-9890-405e-a6c8-888de9289d4d\" /><OS Id=\"61568afe-81fe-4b55-b2bd-453bb6e51fd9\" /><OS Id=\"a1dfedf2-ab79-460c-886e-1d0fd604f058\" /><OS Id=\"c8b88810-6386-11ee-b809-00505601166d\" /><OS Id=\"32dbcada-7faf-4747-94b3-7560fc3223e3\" /><OS Id=\"12221ec9-6d7e-11ee-b809-00505601166d\" /><OS Id=\"0477fac2-6387-11ee-b809-00505601166d\" /><OS Id=\"2b28ba0b-6d7e-11ee-b809-00505601166d\" /><OS Id=\"01e6ca47-7e3f-4ea2-b96d-32080d60d8df\" /><OS Id=\"20944c37-9db1-11ee-b805-0050560128bc\" /><OS Id=\"dd50debb-9dd0-11ee-b805-0050560128bc\" /><OS Id=\"e5cb55cf-24b5-11ef-b807-0050560128bc\" /><OS Id=\"a702037c-3539-11ef-b807-0050560128bc\" /><OS Id=\"8126043a-59c5-4d9b-8acd-2d9371d498e1\" /><OS Id=\"91072718-517c-46f6-91ef-2c6aafdb2282\" /><OS Id=\"abf51256-6468-4869-81bd-8854c54284d3\" /><OS Id=\"13552947-d88d-4405-a4b4-95fc266949fa\" /><OS Id=\"57c67a65-0feb-43db-8b0c-e7de74c52a46\" /><OS Id=\"dce76dcb-a8c8-11ef-9672-b0f98a38e7fe\" /><OS Id=\"3a947e29-113e-423c-be2b-4a35d3209ce4\" /><OS Id=\"3a71dedd-526f-4d81-9a3e-5e06867cf06a\" /><OS Id=\"4e12e816-9606-11ef-9672-b0f98a38e7fe\" /><OS Id=\"773de224-bafd-4bc0-839d-685ff61e4414\" /><OS Id=\"460a10f0-6395-4b32-ac4a-7794485e5dba\" /><OS Id=\"dd90b188-52fd-460c-881c-0ef724112a2b\" /><OS Id=\"b97ae7d5-6789-4f40-a72e-b70bd5da0569\" /><OS Id=\"3e025a23-0296-4955-97ad-bddecccb1a34\" /><OS Id=\"2e09d725-5bda-4cb1-9f5d-2c19b607754e\" /><OS Id=\"f9691e3c-7efd-4586-9441-29aa1ea801d3\" /><OS Id=\"774404f3-a69b-48bc-ae36-2345ee8b276a\" /><OS Id=\"6e415cc7-0106-441d-8be4-7fcc0f34dbfa\" /><OS Id=\"007d2f06-8705-4eec-9d8b-30f2382e7a6c\" /><OS Id=\"a6bb0deb-8fa4-4808-bd23-fb0682ff683d\" /><OS Id=\"6ca58c61-c025-4d58-a57b-986dbaa4f7a3\" /><OS Id=\"7e0e1a77-6ef4-4ec8-9884-38916b9d69bb\" /><OS Id=\"b09ab96a-b5f4-4e9b-8376-656f13b1ba55\" /><OS Id=\"31b10060-f531-4675-9568-5689507675e2\" /><OS Id=\"ffcfb6c7-abc4-11ef-9673-863c01d83a86\" /><OS Id=\"ba66be25-1e95-11ef-b807-0050560128bc\" /><OS Id=\"6a82cf85-6d7e-11ee-b809-00505601166d\" /><OS Id=\"d3b8b1cf-9a46-4647-be57-9efae9ee0d73\" /><OS Id=\"e0c3c4fc-5e12-11ef-b807-0050560128bc\" /><OS Id=\"9030afea-865b-11ef-966f-a4c7c49d4351\" /><OS Id=\"8d829228-394b-11ef-b807-0050560128bc\" /><OS Id=\"29c9727d-8613-11ef-966f-a4c7c49d4351\" /><OS Id=\"07641d34-698c-49fc-bdfc-2fb0d0d97552\" /></root>', @VisitAim = '<root><OS Id=\"52cbac59-e526-4bce-9252-cf0cf7305363\" /><OS Id=\"1e64a761-306d-46dd-98c0-5696395df71a\" /><OS Id=\"51925334-249f-4072-90e0-91ceae1f24d9\" /><OS Id=\"b688a9c7-cbf8-43c2-9b15-76e2f3e98bb2\" /><OS Id=\"83afa901-636b-4b19-bfed-3bcb92c9f3b8\" /><OS Id=\"108e76d7-bce3-4a27-8b44-bba52d768e6e\" /><OS Id=\"7834a5c9-dd15-456d-95fd-d33af57fce6a\" /><OS Id=\"cdd39f3f-b3fb-463b-8284-2f44e8c2bc51\" /><OS Id=\"5ceaabbe-5f28-4f46-93a6-61fdadbfa905\" /><OS Id=\"2a20d8b0-c7f8-43bd-b085-c0281816cf13\" /><OS Id=\"de1463f7-8e61-48b9-b005-88b5d4e843dc\" /><OS Id=\"7c63e047-b34f-4b13-9904-ab52c0610169\" /></root>', @Responsible_ID = NULL, @EMessageSubject = NULL, @ProActivity = '<root><OS Id=\"39\" /><OS Id=\"40\" /></root>'";

            //    var eMessage = await _dbContext.EMessage
            //            .FromSqlRaw(query, lead.OuterMessage_ID)
            //            .FirstOrDefaultAsync();

            //    if (eMessage != null && !string.IsNullOrEmpty(eMessage?.ResponsibleName))
            //    {
            //var jsonRecord = lead.MessageText;
            //var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonRecord);
            //string outlet_code;

            //var leadinfo = jsonObject?.lead_info;
            //if (leadinfo != null)
            //{
            //    outlet_code = leadinfo.outlet_code.ToString();
            //}
            //else
            //{
            //    outlet_code = jsonObject?.outlet_code?.ToString();
            //}

            //var message = new RabbitMQStatusMessage_LMP
            //{
            //    Message_ID = lead.OuterMessage_ID,
            //    Outlet_Code = outlet_code.Substring(0, 5),
            //    ResponsibleName = eMessage?.ResponsibleName
            //};
            //try
            //{
            //    await _publishEndpoint.Publish(message);
            //    _logger.LogInformation($"Сообщение для изменения статуса {lead.MessageOuter_ID} (outlet_code: {outlet_code.Substring(0, 5)}, responsibleName:{eMessage?.ResponsibleName}) успешно добавлено в очередь RabbitMQ LMP status queue", DateTimeOffset.Now);
            //}
            //catch (Exception ex)
            //{
            //    lead.ErrorCode = 1;
            //    lead.ErrorMessage = ex.Message;
            //    lead.ProcessingStatus = 4;
            //    await _dbContext.SaveChangesAsync();
            //    _logger.LogError($"Ошибка отправки LMP сообщения в RabbitMQ для {lead.OuterMessage_ID}: {ex.Message}", DateTimeOffset.Now);
            //}
            //    }
            //}

        }
    }


}
