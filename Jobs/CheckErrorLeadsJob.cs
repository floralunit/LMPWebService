using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Consumers;
using LMPWebService.Services;
using LMPWebService.Services.Interfaces;
using Newtonsoft.Json;
using Quartz;

namespace LMPWebService.Jobs
{
    public class CheckErrorLeadsJob : IJob
    {
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;
        private readonly IMassTransitPublisher _massTransitPublisher;
        private readonly ILogger<CheckErrorLeadsJob> _logger;

        //private readonly ISendStatusService _sendStatusService;

        public CheckErrorLeadsJob(
                                IHttpClientLeadService httpClientLeadService,
                                IOuterMessageService messageService,
                                IMassTransitPublisher massTransitPublisher,
                                ILogger<CheckErrorLeadsJob> logger
                                //ISendStatusService sendStatusService

            )
        {
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _massTransitPublisher = massTransitPublisher;
            _logger = logger;
            //_sendStatusService = sendStatusService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("");
            _logger.LogInformation("");
            _logger.LogInformation($"Job {context.JobDetail.Key.Name} started", DateTimeOffset.Now);

            var errorLeads3A = await _messageService.FindMessagesByStatusAsync(3); //не создались обращения
            var errorLeads4A = await _messageService.FindMessagesByStatusAsync(4); //не отправился статус
            _logger.LogInformation($"В базе ASTRA_AUDI найдено {errorLeads3A.Count()} лидов с ProcessingStatus=3 (не создались обращения) и {errorLeads4A.Count} лидов с ProcessingStatus=4 (не отправился статус)", DateTimeOffset.Now);

            foreach (var lead3A in errorLeads3A)
            {
                try
                {
                    var jsonRecord = lead3A.MessageText;
                    var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonRecord);
                    string outlet_code;

                    var leadinfo = jsonObject?.lead_info;
                    if (leadinfo != null)
                    {
                        outlet_code = leadinfo.outlet_code.ToString();
                    }
                    else
                    {
                        outlet_code = jsonObject?.outlet_code?.ToString();
                    }
                    var message = new RabbitMQLeadMessage_LMP
                    {
                        Message_ID = lead3A.OuterMessage_ID,
                        OutletCode = outlet_code.Substring(0, 5)
                    };
                    await _massTransitPublisher.SendLeadReceivedMessage(message);
                    _logger.LogInformation($"Сообщение {lead3A.OuterMessage_ID} было повторно отправлено в очередь на создание обращений", DateTimeOffset.Now);
                }
                catch (Exception ex)
                {
                    var errorMes = $"Error while pushing to RabbitMQ";
                    if (ex.InnerException != null)
                        errorMes += $"\nInnerException: {ex.InnerException.Message}";

                    _logger.LogError(errorMes, DateTimeOffset.Now);
                }
            }

            //foreach (var lead4A in errorLeads4A)
            //{
            //    var jsonRecord = lead4A.MessageText;
            //    var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonRecord);
            //    string outlet_code;

            //    var leadinfo = jsonObject?.lead_info;
            //    if (leadinfo != null)
            //    {
            //        outlet_code = leadinfo.outlet_code.ToString();
            //    }
            //    else
            //    {
            //        outlet_code = jsonObject?.outlet_code?.ToString();
            //    }
            //    await _sendStatusService.SendStatusResponsibleAsync(lead4A.OuterMessage_ID, outlet_code.Substring(0, 5));
            //    _logger.LogInformation($"Сообщение {lead4A.OuterMessage_ID} было повторно отправлено на обработку статуса подтверждения", DateTimeOffset.Now);
            //}

        }
    }


}
