using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using YourNamespace.Dtos;

namespace LMPWebService.Services
{
    public interface ISendStatusService
    {
        Task SendStatusAsync(RabbitMQStatusMessage_LMP message);
    }

    public class SendStatusService : ISendStatusService
    {
        private readonly ILogger<SendStatusService> _logger;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly IOuterMessageService _messageService;
        private readonly AstraDbContext _dbContext;

        private static readonly HashSet<int> AllowedDocTypeIds = new() { 140, 57, 56, 1 };

        public SendStatusService(
            ILogger<SendStatusService> logger,
            IHttpClientLeadService httpClientLeadService,
            IOuterMessageService messageService,
            AstraDbContext dbContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientLeadService = httpClientLeadService ?? throw new ArgumentNullException(nameof(httpClientLeadService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        }

        public async Task SendStatusAsync(RabbitMQStatusMessage_LMP message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var docID = message.astra_document_id;
            var statusID = message.astra_document_status_id;
            var docTypeID = message.astra_document_subtype_id;
            var responsibleUserID = message.upd_application_user_id;

            _logger.LogInformation($"[SendStatusService] Получено сообщение на обработку статуса emessage = {docID}, docTypeID={docTypeID}, statusID={statusID}");

            // Проверка типа документа
            if (docTypeID != null &&
                !AllowedDocTypeIds.Contains(docTypeID.Value))
            {
                return;
            }

            var statusDTO = new LeadStatusRequestDto();
            int statusToSend = -1;
            Models.OuterMessage lead = null;

            try
            {
                if (docTypeID == null) // Потребность
                {
                    if (ClientRefusalStatuses.Contains(statusID.Value))
                    {
                        statusToSend = 7;
                        statusDTO.refuse_reason_comment = "Клиент отказался от дальнейшего общения";
                        statusDTO.dealer_refuse_reason_id = 27;

                    }
                }
                else if (docTypeID == 140) // Электронное обращение
                {
                    if (DeletedStatuses.Contains(statusID.Value))
                    {
                        var eMessage = await _dbContext.EMessage
                            .FirstOrDefaultAsync(x => x.EMessage_ID == docID);

                        if (eMessage == null) return;

                        lead = await _dbContext.OuterMessage
                            .FirstOrDefaultAsync(x => x.OuterMessage_ID == eMessage.OuterMessage_ID);

                        if (lead == null)
                        {
                            _logger.LogError(
                                "[SendStatusService] Для emessage = {EMessageId} не найден соотвествующий лид в OuterMessage. Обработка статуса невозможна",
                                eMessage.EMessage_ID);
                            return;
                        }

                        statusToSend = 40;
                        statusDTO.status_comment = "Лид дисквалифицирован";
                    }
                }
                else if (docTypeID == 56) // Договор на продажу
                {
                    _logger.LogInformation(
                        "[SendStatusService] Начата обработка статуса для договора на продажу, docID = {DocumentId}",
                        docID);

                    var eMessage = await GetEMessageBySalesContractId(docID.Value);
                    if (eMessage == null) return;

                    lead = await _dbContext.OuterMessage
                        .FirstOrDefaultAsync(x => x.OuterMessage_ID == eMessage.OuterMessage_ID);

                    if (lead == null)
                    {
                        _logger.LogError(
                            "[SendStatusService] Для emessage = {EMessageId} не найден соотвествующий лид в OuterMessage. Обработка статуса невозможна",
                            eMessage.EMessage_ID);
                        return;
                    }

                    if (statusID == Guid.Parse("355ABE35-B336-4302-80D1-9862AABDF3EB"))//Заявка на резерв -> Резерв согласован
                    {
                        statusToSend = 39;
                        statusDTO.status_comment = "Коммерческое предложение";
                    }
                    else if (statusID == Guid.Parse("13BF47E4-5265-4686-AF75-C8EE0EF1DF64"))//Резерв согласован -> Подписан
                    {
                        statusToSend = 12;
                        statusDTO.status_comment = "Контракт заключен";
                    }
                }
                else if (docTypeID == 1) // Контакт с клиентом
                {
                    _logger.LogInformation(
                        "[SendStatusService] Начата обработка статуса для контакта с клиентом, docID = {DocumentId}",
                        docID);

                    var contact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == docID);

                    if (contact == null)
                    {
                        _logger.LogError(
                            "[SendStatusService] Для contact = {ContactId} не найден соотвествующий Contact",
                            docID);
                        return;
                    }

                    var docParents = await _dbContext.DocumentBaseParent
                        .Where(x => x.DocumentBase_ID == docID)
                        .ToListAsync();

                    if (!docParents.Any()) return;

                    var docChildren = await _dbContext.DocumentBaseParent
                        .Where(x => x.ParentDocumentBase_ID == docID)
                        .ToListAsync();

                    var eMessage = await _dbContext.EMessage
                        .FirstOrDefaultAsync(x => docParents.Select(y => y.ParentDocumentBase_ID).Contains(x.EMessage_ID));

                    if (eMessage == null) return;

                    lead = await _dbContext.OuterMessage
                        .FirstOrDefaultAsync(x => x.OuterMessage_ID == eMessage.OuterMessage_ID);

                    if (lead == null)
                    {
                        _logger.LogError(
                            "[SendStatusService] Для emessage = {EMessageId} не найден соотвествующий лид в OuterMessage. Обработка статуса невозможна",
                            eMessage.EMessage_ID);
                        return;
                    }

                    var interest = await _dbContext.Interest
                        .FirstOrDefaultAsync(x => docParents.Select(y => y.ParentDocumentBase_ID).Contains(x.Interest_ID));

                    if (interest != null)
                    {
                        var result = await ProcessContactWithInterest(contact, statusID.Value, docParents, docChildren);
                        statusToSend = result.statusToSend;
                        statusDTO = result.statusDTO;
                        var docBase = await _dbContext.DocumentBase.FirstOrDefaultAsync(x => x.DocumentBase_ID == interest.Interest_ID);
                        statusDTO.client_dms_id = docBase.DocumentBaseNumber;
                    }
                    else
                    {
                        var result = await ProcessContactWithoutInterest(contact, statusID.Value, docParents, docChildren);
                        statusToSend = result.statusToSend;
                        statusDTO = result.statusDTO;
                    }
                }
                else if (docTypeID == 57) // Заявка на ремонт
                {
                    _logger.LogInformation(
                        "[SendStatusService] Начата обработка статуса для заявки на ремонт, docID = {DocumentId}",
                        docID);

                    var docParent = await _dbContext.DocumentBaseParent
                        .FirstOrDefaultAsync(x => x.DocumentBase_ID == docID);

                    if (docParent == null) return;

                    var eMessage = await _dbContext.EMessage
                        .FirstOrDefaultAsync(x => x.EMessage_ID == docParent.ParentDocumentBase_ID);

                    if (eMessage == null) return;

                    lead = await _dbContext.OuterMessage
                        .FirstOrDefaultAsync(x => x.OuterMessage_ID == eMessage.OuterMessage_ID);

                    if (lead == null)
                    {
                        _logger.LogError(
                            "[SendStatusService] Для emessage = {EMessageId} не найден соотвествующий лид в OuterMessage. Обработка статуса невозможна",
                            eMessage.EMessage_ID);
                        return;
                    }

                    var workOrder = await _dbContext.WorkOrder
                        .FirstOrDefaultAsync(x => x.WorkOrder_ID == docID);

                    var result = await ProcessWorkOrderStatus(statusID.Value, workOrder, statusDTO);
                    statusToSend = result.statusToSend;
                    statusDTO = result.statusDTO;
                }

                if (statusToSend != -1 && lead != null)
                {
                    var responsible_user = await _dbContext.DictBase.FirstOrDefaultAsync(x => x.DictBase_ID == responsibleUserID);
                    statusDTO.responsible_user = responsible_user?.DictBaseName;
                    await SendLeadStatus(lead, statusToSend, statusDTO, docID, docTypeID, statusID);
                }
            }
            catch (Exception e)
            {
                var errorMes = $"[SendStatusService] Error processing message: {e.Message}";
                if (e.InnerException != null)
                    errorMes += $"\nInnerException: {e.InnerException.Message}";

                _logger.LogError(errorMes);

                if (lead != null)
                {
                    await UpdateMessageErrorStatus(lead, errorMes);
                }
            }
        }

        private async Task<(int statusToSend, LeadStatusRequestDto statusDTO)> ProcessContactWithInterest(
            Contact contact,
            Guid statusId,
            List<DocumentBaseParent> docParents,
            List<DocumentBaseParent> docChildren)
        {
            int statusToSend = -1;
            var statusDTO = new LeadStatusRequestDto();

            if (NotCompletedStatuses.Contains(statusId) &&
                contact.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86") && //встреча
                docChildren.Count > 0)
            {
                foreach (var childDoc in docChildren)
                {
                    var childContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == childDoc.DocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86")); //встреча

                    if (childContact != null)
                    {
                        var childDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == childContact.Contact_ID);

                        if (childDocumentBase.DocumentAllowedState_ID == Guid.Parse("b00fb2fd-589e-447e-ad3e-1616445dc747")) //запланирован
                        {
                            statusToSend = 10;
                            var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == childDocumentBase.DocumentBase_ID && x.DocumentCommentType_ID == 3);
                            statusDTO.status_comment = comment?.Comment;
                            statusDTO.dealer_visit_planned_date = childContact.PlanDate;
                        }
                    }
                }
            }

            if (CompletedStatuses.Contains(statusId))
            {
                if (contact.ContactType_ID == Guid.Parse("bdece6ef-3a4f-45cc-b6ef-cb9e8e6d038c")) // выдача
                {
                    statusToSend = 44;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                    statusDTO.status_comment = comment?.Comment;
                }
                else if (contact.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86")) //встреча
                {
                    statusToSend = 9;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                    statusDTO.status_comment = comment?.Comment;
                }
                else if (contact.ContactType_ID == Guid.Parse("09eafe1d-e316-46ea-a0b8-58cd2152c4d2") || // звонок входящий
                         contact.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")) // звонок исходящий
                {
                    statusToSend = 36;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                    statusDTO.status_comment = comment?.Comment;
                }
            }

            if (contact.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86") && //встреча
                PlannedStatuses.Contains(statusId))
            {
                statusToSend = 8;
                var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 2);
                statusDTO.status_comment = comment?.Comment;
                statusDTO.dealer_visit_planned_date = contact.PlanDate;
            }

            if (contact.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")) // звонок исходящий
            {
                var outgoingCallResult = await ProcessOutgoingCall(contact, statusId, docParents, docChildren);
                if (outgoingCallResult.statusToSend != -1)
                {
                    statusToSend = outgoingCallResult.statusToSend;
                    statusDTO = outgoingCallResult.statusDTO;
                }
            }

            if (contact.ContactType_ID == Guid.Parse("c25a8615-efdd-43e9-a4ec-38c5ecaa354d")) // тест-драйв
            {
                if (PlannedStatuses.Contains(statusId))
                {
                    statusToSend = 37;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 2);
                    statusDTO.status_comment = comment?.Comment;
                    statusDTO.dealer_visit_planned_date = contact.PlanDate;
                }
                else if (statusId == Guid.Parse("331F0156-81CA-427F-B048-5C0EF177FBEF") ||
                         statusId == Guid.Parse("A4DBB71A-4A40-4C1B-9C67-E85225A5B2CB")) //Запланирован -> Выполнен
                {
                    statusToSend = 38;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                    statusDTO.status_comment = comment?.Comment;
                }
            }

            return (statusToSend, statusDTO);
        }

        private async Task<(int statusToSend, LeadStatusRequestDto statusDTO)> ProcessOutgoingCall(
            Contact contact,
            Guid statusId,
            List<DocumentBaseParent> docParents,
            List<DocumentBaseParent> docChildren)
        {
            int statusToSend = -1;
            var statusDTO = new LeadStatusRequestDto();

            if (NotCompletedStatuses.Contains(statusId))
            {
                if (contact.ContactFailureReason_ID == Guid.Parse("9735665F-902F-420C-A0A3-9AEA04DDE471"))//неверный номер
                {
                    statusToSend = 17;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                    statusDTO.status_comment = comment?.Comment;
                }

                foreach (var parentDoc in docParents)
                {
                    var parentContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc.ParentDocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")); // звонок исходящий

                    if (parentContact != null)
                    {
                        var parentContactDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact.Contact_ID);

                        if (parentContactDocumentBase.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2")) // не выполнен
                        {
                            var docParents2 = await _dbContext.DocumentBaseParent
                                .Where(x => x.DocumentBase_ID == parentContactDocumentBase.DocumentBase_ID)
                                .ToListAsync();

                            foreach (var parentDoc2 in docParents2)
                            {
                                var parentContact2 = await _dbContext.Contact
                                    .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc2.ParentDocumentBase_ID &&
                                                          x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")); // звонок исходящий

                                if (parentContact2 != null)
                                {
                                    var parentContactDocumentBase2 = await _dbContext.DocumentBase
                                        .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact2.Contact_ID);

                                    if (parentContactDocumentBase2.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2")) // не выполнен
                                    {
                                        statusToSend = 4;
                                        var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                                        statusDTO.status_comment = comment?.Comment;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (CompletedStatuses.Contains(statusId))
            {
                foreach (var childDoc in docChildren)
                {
                    var childContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == childDoc.DocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")); // звонок исходящий

                    if (childContact != null)
                    {
                        var childDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == childContact.Contact_ID);

                        if (childDocumentBase.DocumentAllowedState_ID == Guid.Parse("b00fb2fd-589e-447e-ad3e-1616445dc747")) // запланирован
                        {
                            statusToSend = 5;
                            statusDTO.dealer_recall_date = childContact.PlanDate;
                            var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                            statusDTO.status_comment = comment?.Comment;
                        }
                    }
                }
            }

            return (statusToSend, statusDTO);
        }

        private async Task<(int statusToSend, LeadStatusRequestDto statusDTO)> ProcessContactWithoutInterest(
                Contact contact,
                Guid statusId,
                List<DocumentBaseParent> docParents,
                List<DocumentBaseParent> docChildren)
        {
            int statusToSend = -1;
            var statusDTO = new LeadStatusRequestDto();
            var docBase = await _dbContext.DocumentBase.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID);
            statusDTO.client_dms_id = docBase.DocumentBaseNumber;

            if (NotCompletedStatuses.Contains(statusId))
            {
                if (contact.ContactFailureReason_ID == Guid.Parse("9735665F-902F-420C-A0A3-9AEA04DDE471"))//неверный номер
                {
                    statusToSend = 17;
                    statusDTO.status_comment = "Неверный номер телефона";
                }

                if (contact.ContactFailureReason_ID == Guid.Parse("A7E1A10C-9FED-4BD3-861E-38DD6348FED1") || // Недоступен/Заблокирован
                    contact.ContactFailureReason_ID == Guid.Parse("0602B2CA-3B88-406E-A0A4-3D381EA19E0D") || // Сбросил
                    contact.ContactFailureReason_ID == Guid.Parse("3A7690B1-FFE1-46CE-8707-FD6EEF4010ED")) //Не отвечает
                {

                    foreach (var parentDoc in docParents)
                    {
                        var parentContact = await _dbContext.Contact
                            .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc.ParentDocumentBase_ID &&
                                                  x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")); // звонок исходящий

                        if (parentContact != null)
                        {
                            var parentContactDocumentBase = await _dbContext.DocumentBase
                                .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact.Contact_ID);

                            if (parentContactDocumentBase.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2")) // не выполнен
                            {
                                var docParents2 = await _dbContext.DocumentBaseParent
                                    .Where(x => x.DocumentBase_ID == parentContactDocumentBase.DocumentBase_ID)
                                    .ToListAsync();

                                foreach (var parentDoc2 in docParents2)
                                {
                                    var parentContact2 = await _dbContext.Contact
                                        .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc2.ParentDocumentBase_ID &&
                                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")); // звонок исходящий

                                    if (parentContact2 != null)
                                    {
                                        var parentContactDocumentBase2 = await _dbContext.DocumentBase
                                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact2.Contact_ID);

                                        if (parentContactDocumentBase2.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2")) // не выполнен
                                        {
                                            statusToSend = 4;
                                            var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                                            statusDTO.status_comment = comment?.Comment;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    statusToSend = 7;
                    var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                    statusDTO.status_comment = comment?.Comment;
                    statusDTO.refuse_reason_comment = "Прочее";
                    statusDTO.dealer_refuse_reason_id = 4;
                }
            }

            if (CompletedStatuses.Contains(statusId))
            {
                foreach (var childDoc in docChildren)
                {
                    var childContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == childDoc.DocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a")); // звонок исходящий

                    if (childContact != null)
                    {
                        var childDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == childContact.Contact_ID);

                        if (childDocumentBase.DocumentAllowedState_ID == Guid.Parse("b00fb2fd-589e-447e-ad3e-1616445dc747")) //запланирован
                        {
                            statusToSend = 5;
                            statusDTO.dealer_recall_date = childContact.PlanDate;
                            var comment = await _dbContext.DocumentComment.FirstOrDefaultAsync(x => x.DocumentBase_ID == contact.Contact_ID && x.DocumentCommentType_ID == 3);
                            statusDTO.status_comment = comment?.Comment;
                        }
                    }
                }

                if (docChildren.Count == 0)
                {
                    statusToSend = 35;
                    statusDTO.status_comment = "Заявка обработана";
                }
            }
            return (statusToSend, statusDTO);
        }

        private async Task<(int statusToSend, LeadStatusRequestDto statusDTO)> ProcessWorkOrderStatus(Guid statusId, WorkOrder workOrder, LeadStatusRequestDto statusDTO)
        {
            var statusToSend = -1;
            var docBase = await _dbContext.DocumentBase.FirstOrDefaultAsync(x => x.DocumentBase_ID == workOrder.WorkOrder_ID);
            statusDTO.client_dms_id = docBase.DocumentBaseNumber;

            if (statusId == Guid.Parse("BE8D9882-5A81-4007-970F-8CACDC401E82") ||
                statusId == Guid.Parse("E43B0048-1CBA-4681-946C-AD231192A222") ||
                statusId == Guid.Parse("A13FAACA-1D0F-4F6C-A265-E2A9C57C307D") ||
                statusId == Guid.Parse("1D068EA0-67BE-44AB-A61A-32F71F3F1747") ||
                statusId == Guid.Parse("7CBCE66F-2C83-4600-93A4-9CBB6F470F5D") ||
                statusId == Guid.Parse("58F3BFF1-DF86-462A-8C58-90F7BD103E2E") ||
                statusId == Guid.Parse("C1B294B1-AE5D-48F2-A58F-A6573A0F0060") ||
                statusId == Guid.Parse("25D383C8-AE56-48F1-9B86-E071F0D557A5") ||
                statusId == Guid.Parse("9D0A7044-3C9D-4BA7-AF37-3949D4D4ACC0") ||
                statusId == Guid.Parse("B1FB64F0-9181-4BE4-A43A-F8D36CEABE0B") ||
                statusId == Guid.Parse("10E3FA11-8ADF-452A-93E6-C5B80C46184A") ||
                statusId == Guid.Parse("BB4AE67D-AC49-4F19-97A1-25AF8DDD0069") ||
                statusId == Guid.Parse("098C117F-30A0-4506-A170-84806CCFC150") ||
                statusId == Guid.Parse("34C40DEE-47D3-4EF9-8F9E-F73FEB460E67")) //-> Автомобиль поступил
            {
                statusToSend = 9;
                statusDTO.status_comment = "Автомобиль поступил";
            }
            else if (statusId == Guid.Parse("AEFD9D1C-FF5D-469C-8A09-9C1C45987D43") ||
                     statusId == Guid.Parse("6AF78A95-68E0-44E3-B258-AEFAC1F5516B") ||
                     statusId == Guid.Parse("84166577-1670-4FDE-B4EB-E15B7D891F84") ||
                     statusId == Guid.Parse("8CE97943-D26A-44AC-84FB-FB3222B3058D")) //Создана -> Удалена
            {
                statusToSend = 41;
                statusDTO.status_comment = "Консультация проведена";
            }
            else if (statusId == Guid.Parse("479B4D5B-9C76-43A7-A1EB-8AF851C407BE"))//Создана -> Предварительная запись
            {
                statusToSend = 8;
                statusDTO.dealer_visit_planned_date = workOrder?.ReceptionDate;
            }
            else if (ClientRefusalStatuses.Contains(statusId) || AnnulledStatuses.Contains(statusId))
            {
                statusToSend = 7;

                var docChild = await _dbContext.DocumentBaseParent
                    .Where(x => x.ParentDocumentBase_ID == workOrder.WorkOrder_ID)
                    .FirstOrDefaultAsync();
                var childContact = await _dbContext.Contact.FirstOrDefaultAsync(x => x.ContactWorkOrderRefuseReason_ID != null && x.Contact_ID == docChild.DocumentBase_ID);
                if (childContact != null)
                {
                    var refuseID = GetRefuseReasonId(childContact.ContactWorkOrderRefuseReason_ID.Value);
                    statusDTO.dealer_refuse_reason_id = refuseID;
                    statusDTO.refuse_reason_comment = GetRefuseReasonName(refuseID);

                }
            }
            else if (statusId == Guid.Parse("93FE5BEC-2D58-4C67-8C44-CD6E838086AB")) //Автомобиль принят -> Автомобиль выдан
            {
                statusToSend = 11;
                statusDTO.status_comment = "Ремонт выполнен";
            }
            else if (ClientRefusalStatuses.Contains(statusId))
            {
                statusToSend = 7;
            }

            return (statusToSend, statusDTO);
        }

        private async Task SendLeadStatus(
            Models.OuterMessage lead,
            int statusToSend,
            LeadStatusRequestDto statusDTO,
            Guid? docID,
            int? docTypeID,
            Guid? statusID)
        {
            var outerReader = await _dbContext.OuterMessageReader
                .FirstOrDefaultAsync(x => x.OuterMessageReader_ID == lead.OuterMessageReader_ID);

            var outletCode = outerReader?.OuterMessageReaderName;

            statusDTO.lead_id = lead.OuterMessage_ID.ToString();
            statusDTO.status = statusToSend.ToString();

            var statusResponse = await _httpClientLeadService.SendStatusAsync(statusDTO, outletCode);

            if (statusResponse == null || !statusResponse.is_success)
            {
                LogAndUpdateErrorStatus(
                    lead,
                    statusResponse?.error ?? "Unknown error",
                    docID, docTypeID, statusID, statusDTO.status);
                return;
            }

            await UpdateMessageSuccessStatus(lead);
            _logger.LogInformation(
                "Для лида id {MessageId}, docID={DocId}, docTypeID={DocTypeId}, docStatusID={StatusId} успешно обновлен статус {Status}",
                lead.MessageOuter_ID, docID, docTypeID, statusID, statusDTO.status);
        }

        private async Task UpdateMessageSuccessStatus(Models.OuterMessage message)
        {
            message.ProcessingStatus = 2;
            message.ErrorCode = 0;
            message.ErrorMessage = "";
            message.UpdDate = DateTime.Now;
            await _messageService.UpdateMessageAsync(message);
        }

        private async Task UpdateMessageErrorStatus(Models.OuterMessage message, string errorMessage)
        {
            message.ErrorCode = 1;
            message.ErrorMessage = errorMessage;
            message.ProcessingStatus = 4;
            message.UpdDate = DateTime.Now.AddHours(3);
            await _messageService.UpdateMessageAsync(message);
        }

        private void LogAndUpdateErrorStatus(
            Models.OuterMessage message,
            string error,
            Guid? docId = null,
            int? docTypeId = null,
            Guid? statusId = null,
            string status = null)
        {
            _logger.LogError(error);

            if (docId != null && docTypeId != null && statusId != null && status != null)
            {
                _logger.LogError(
                    "Для лида id {MessageId}, docID={DocId}, docTypeID={DocTypeId}, docStatusID={StatusId} ошибка обновления статуса {Status}: {Error}",
                    message.MessageOuter_ID, docId, docTypeId, statusId, status, error);
            }
        }

        public async Task<EMessage?> GetEMessageBySalesContractId(Guid salesContractId)
        {
            return await (from db in _dbContext.DocumentBase
                          join dbp1 in _dbContext.DocumentBaseParent on db.DocumentBase_ID equals dbp1.DocumentBase_ID
                          join i in _dbContext.Interest on dbp1.ParentDocumentBase_ID equals i.Interest_ID
                          join dbp2 in _dbContext.DocumentBaseParent on i.Interest_ID equals dbp2.ParentDocumentBase_ID
                          join c in _dbContext.Contact on dbp2.DocumentBase_ID equals c.Contact_ID
                          join dbp3 in _dbContext.DocumentBaseParent on c.Contact_ID equals dbp3.DocumentBase_ID
                          join e in _dbContext.EMessage on dbp3.ParentDocumentBase_ID equals e.EMessage_ID
                          where db.DocumentBase_ID == salesContractId
                          select e)
                   .FirstOrDefaultAsync();
        }

        // Статические коллекции для хранения GUID-ов статусов
        private static readonly HashSet<Guid> ClientRefusalStatuses = new() // -> отказ
        {
            Guid.Parse("00067AD7-0C2C-4E12-9E93-0AF4209C6560"),
            Guid.Parse("6E2943A8-E39E-46D5-B3DC-48C0660C34E1"),
            Guid.Parse("5DEF2C8A-5368-46A0-AFCB-B52CB9469AED"),
            Guid.Parse("435AD73D-EEA4-40A4-8661-C3221A15838C"),
            Guid.Parse("9B0ECD9A-6C49-4E0D-AB24-1BA63DCD61B1"),
            Guid.Parse("A991C140-FC9D-469F-8762-251BD3F39DE9"),
            Guid.Parse("C7F889BF-B2F3-4465-9C82-52879B2E3DCD"),
            Guid.Parse("1DB9E974-CE6E-4B68-8F0D-C7622A97C4D7"),
            Guid.Parse("6F506864-84B0-4034-A2DB-8FD45DEEA03B"),
            Guid.Parse("D4DAF335-A207-4A49-9565-4B9FCB31ACA6"),
            Guid.Parse("E3C13503-C8A5-4784-857A-BD2BCE234C2F"),
            Guid.Parse("85B5ECB3-2486-4C2F-9DB6-E59DDBED1FE9"),
            Guid.Parse("10079B2B-6F64-44C5-B1F4-2530BBA29E79"),
            Guid.Parse("BF0D002A-3A58-4F3D-B310-8320BD767B48"),
            Guid.Parse("94D68B48-B848-4039-BC0E-AE263D770FB6"),
            Guid.Parse("0F1D0775-D275-4186-B860-3B180749C580")
        };

        private static readonly HashSet<Guid> AnnulledStatuses = new()
        {
            // Аннулирован <- Автомобиль выдан
            Guid.Parse("1E1109C7-0764-4D75-83D4-28C1F1451A87"),
    
            // Аннулирован <- Автомобиль поступил
            Guid.Parse("F998B2F1-0027-458E-B7A0-0AEB43C7EF12"),
            Guid.Parse("676C59B4-8D5B-4668-8515-42E349D62654"),
            Guid.Parse("3FFADC68-27BF-4D33-B335-683FDE910816"),
    
            // Аннулирован <- В работе
            Guid.Parse("984F222A-A445-4385-A66E-217BB37BF8ED"),
            Guid.Parse("4950C80D-6188-43B7-9F5B-44C8E97D75AB"),
            Guid.Parse("E3C1C618-84E8-4DC8-B2A1-6E45E947A42E"),
            Guid.Parse("4F032059-4B82-4C77-A142-71597378A28F"),
            Guid.Parse("7308DAAF-7120-42BC-BDA6-80A08594E4CA"),
            Guid.Parse("3CC8E7D2-5867-410F-A45C-89CB5A48B12E"),
            Guid.Parse("AA04FB96-5AE5-492D-94DD-F19EE50B3F31"),
    
            // Аннулирован <- Выполнен
            Guid.Parse("8BD9BDCC-C355-4A0D-A599-96E875989EB6"),
    
            // Аннулирован <- Выставлен
            Guid.Parse("E63E9A5C-469E-4B6C-B3A4-2BF416EF3D9E"),
            Guid.Parse("CC9FF717-9260-45AB-BC19-C04F21B78F8F"),
            Guid.Parse("92D6A3B0-3BCC-4D8E-82D5-5D935DA5F3C9"),
            Guid.Parse("9393F860-036F-4FFC-A364-B00425B5350B"),
    
            // Аннулирован <- Закрыт
            Guid.Parse("279177A1-5F9B-442D-B1F9-195481E6ED42"),
            Guid.Parse("75218A09-4594-445B-896E-4AFD037A0EAE"),
            Guid.Parse("05544E2B-97EE-4EDC-9679-6F08416135D4"),
            Guid.Parse("38BEF845-2602-45D6-8DCF-A44ABFB130C1"),
            Guid.Parse("F7BE6EA4-0334-4791-B600-BB02F3013B78"),
            Guid.Parse("E3D1FB28-DB6F-4EDD-A453-D7EE12373CCC"),
            Guid.Parse("2FCC8A6D-A675-413F-9272-EAC27976105E"),
    
            // Аннулирован <- Зафиксирована цена
            Guid.Parse("C46AE575-B884-47E1-99AD-AFD38B54894A"),
    
            // Аннулирован <- Отработан
            Guid.Parse("6A9467FA-BF83-4476-99D1-59ECDFF797C4"),
    
            // Аннулирован <- Оформлен
            Guid.Parse("76C303D2-379B-4549-9F52-0D3EB5751DE3"),
            Guid.Parse("6A9B350B-7BDF-47D0-847F-13F32FF4C90D"),
            Guid.Parse("A6FAF297-1615-4EE6-AD1E-1DEDA6A110FF"),
            Guid.Parse("9C001B31-A471-4E59-91D7-318DD54E9CD5"),
            Guid.Parse("73560350-DFAF-420F-B434-85A18AEB0BB2"),
            Guid.Parse("9084AA64-6C62-4536-B709-9BB2763D09D5"),
            Guid.Parse("D169E719-27CA-4C33-BA05-CF6DAEBD7F7B"),
            Guid.Parse("1A256850-40CC-4DD2-ADBF-EF1A7A6E9BAE"),
            Guid.Parse("37992B3F-CF03-4220-ABA0-FCABD05C7758"),
    
            // Аннулирован <- Оформление
            Guid.Parse("F434BE9B-AED3-4613-ABE0-91DAF17319DA"),
    
            // Аннулирован <- Оформление 50/100
            Guid.Parse("E9FB0AE7-AE86-4490-AE5E-59D1B80B3D63"),
    
            // Аннулирован <- Передан клиенту
            Guid.Parse("92AA592E-520E-4DC8-8B90-670F53FED0A5"),
    
            // Аннулирован <- Переоценка
            Guid.Parse("12261FA9-C6F2-4DAF-8DA6-4D20F6AB66E2"),
            Guid.Parse("87C00FE0-10DF-498C-AECE-E2D6DE30C631"),
    
            // Аннулирован <- Подписан
            Guid.Parse("945821C2-CA85-4279-A0D8-F10757AFCD74"),
    
            // Аннулирован <- Подтвержден
            Guid.Parse("0647FD63-A798-4561-8680-9B009A7A648C"),
            Guid.Parse("AA58E47E-3197-423D-9655-C97E72C8B79C"),
            Guid.Parse("3A400D9F-0D5C-45B4-BCF5-1A51D5B88B18"),
            Guid.Parse("4DBD17C6-093D-49E8-9883-1DF1CCDF01FE"),
            Guid.Parse("8C0D3C4D-C572-4F95-8C23-2884140F62EF"),
            Guid.Parse("15977D90-328A-4923-8BBB-40E27200923B"),
            Guid.Parse("B985A910-07EB-4A97-B0E1-650E2359B39A"),
            Guid.Parse("50164E83-3400-4000-B1AF-6A2522E071A3"),
            Guid.Parse("33612BE2-5692-4E6E-A83E-6F2E63AB367B"),
            Guid.Parse("86F7098B-16D2-4CD3-8608-799FEC2B21F8"),
            Guid.Parse("E0DC2DA2-FFA1-4D3B-A792-943006A61503"),
            Guid.Parse("7DFD08C9-7BBE-48C1-AC78-9882F745E7CE"),
            Guid.Parse("6F821C2A-1A9B-433F-8691-A9AAC258253B"),
            Guid.Parse("5CDEBE68-75EB-4F13-9D23-AE4E8F183D37"),
            Guid.Parse("CB80BD35-4841-4178-BA78-E12AAEF0BAED"),
    
            // Аннулирован <- Принят
            Guid.Parse("96CD2BAC-33C9-4ABE-B202-4A30D672E768"),
            Guid.Parse("46ACACFC-D403-42B6-BC4C-94B36F19FD7A"),
    
            // Аннулирован <- Реализация
            Guid.Parse("9FFDE2C0-241F-45AE-8BAA-681C3A4B952C"),
    
            // Аннулирован <- Резерв
            Guid.Parse("3834141A-0FB3-479B-AC8E-0D637348A214"),
    
            // Аннулирован <- Согласован
            Guid.Parse("A9C07BC9-7525-44F5-A76E-C86E61CB4EA7"),
    
            // Аннулирован <- Создан
            Guid.Parse("89782A16-F4C2-4FDE-90CF-04740BCB051A"),
            Guid.Parse("889BDAF3-F899-4E4E-ACEE-250BD41CA6D3"),
            Guid.Parse("1529D877-34B9-47D1-9BD4-2C059FC8F9D5"),
            Guid.Parse("8DDC6D6A-E9DC-440C-8CF0-38FB9369A737"),
            Guid.Parse("F75E34DF-35E5-48E1-94F2-3E220187DC60"),
            Guid.Parse("C11CB943-0584-45AD-B31F-40A23FCD8BDB"),
            Guid.Parse("D0ED3145-3EE4-4BAE-AFC3-47799BB16259"),
            Guid.Parse("AB4E9D18-A5A4-40B3-B66F-645032A62555"),
            Guid.Parse("2FE66275-1F11-405F-966C-6B5447B01EB0"),
            Guid.Parse("26414FE8-6BD3-4FD2-A037-72CED005CD76"),
            Guid.Parse("B83050A3-10C1-47EC-B913-846FCC3197D4"),
            Guid.Parse("9736F959-E56A-4FF6-9B2E-85DDE78823C7"),
            Guid.Parse("8ED97D2C-567C-4A53-A1D8-8B05DAD756A7"),
            Guid.Parse("BAEDD4D2-9200-4DB0-927B-8D8AC7421314"),
            Guid.Parse("FD5DC105-B9FB-42DE-9039-8FF58DA97134"),
            Guid.Parse("7AA56A7A-0566-4049-99C5-914EE78A9D4F"),
            Guid.Parse("CD9202A6-CD62-4E0C-BE30-99185B0A6892"),
            Guid.Parse("EC5AED25-50FA-4DDB-9B00-A8666855169D"),
            Guid.Parse("7051A2D9-74B0-4C10-87D2-DA868C0273BD"),
            Guid.Parse("A2256FFD-C07A-4101-85C2-E596C93B480C")
        };

        private static readonly HashSet<Guid> DeletedStatuses = new()
        {
            Guid.Parse("3BA296B9-B730-4A63-9A42-240DB794BC27"),
            Guid.Parse("3CAF1D89-BD6A-4E8E-BE74-2F0A85A377AF"),
            Guid.Parse("34694844-1D0E-49A5-89CD-83C2585249BC"),
            Guid.Parse("DABE815D-ECA0-4C7D-87C8-AE482485893F"),
            Guid.Parse("F7F9ADEE-D71B-42E2-8EAB-EFADE1A47ECC"),
            Guid.Parse("AEFD9D1C-FF5D-469C-8A09-9C1C45987D43"),
            Guid.Parse("6AF78A95-68E0-44E3-B258-AEFAC1F5516B"),
            Guid.Parse("84166577-1670-4FDE-B4EB-E15B7D891F84"),
            Guid.Parse("8CE97943-D26A-44AC-84FB-FB3222B3058D"),
            Guid.Parse("0019DF69-F055-46A0-A5E1-ECF324044743")
        };

        private static readonly HashSet<Guid> CompletedStatuses = new()
        {
            Guid.Parse("331F0156-81CA-427F-B048-5C0EF177FBEF"),
            Guid.Parse("A4DBB71A-4A40-4C1B-9C67-E85225A5B2CB"),
            Guid.Parse("7E8AFF0A-ABA6-4020-A6E5-0A15BF16013A"),
            Guid.Parse("C43A12F8-3DDB-478B-8704-2521AA8AEABA"),
            Guid.Parse("77634514-E9F8-489C-B263-41E80145576B"),
            Guid.Parse("9A40C4D0-BDB7-410E-B8C6-4D80D11F280E"),
            Guid.Parse("68A3FCDC-BFFA-48D7-B6A9-76A63E09E8A8"),
            Guid.Parse("481F78A6-C6FF-4C8B-9D27-DF01A32D2747"),
            Guid.Parse("5824B6EF-1F7B-4D13-BBC3-EB9AAA29A99D"),
            Guid.Parse("994D1BE2-6017-4710-82E9-FAE34113B987"),
            Guid.Parse("479C8007-CFB3-4BD5-8091-0FFFFAF4EF26"),
            Guid.Parse("9D0737F2-2C5E-427B-8A23-38D268B7D098"),
            Guid.Parse("E78CD57D-EFEB-4528-80D3-3D5AF3E19E40"),
            Guid.Parse("68CB0EAE-FCC3-4F66-AF79-868AEDA93FE8"),
            Guid.Parse("B1E3783E-D623-4C80-A605-085943CCD66F"),
            Guid.Parse("5D939990-17EF-4156-8B0B-0BAC40620CCA"),
            Guid.Parse("AFCA17A0-4B37-4547-A682-29C824565547"),
            Guid.Parse("046D890C-868C-42F6-B4B6-4B7B3783E845"),
            Guid.Parse("0CE8F7ED-E2F0-4631-945C-522C4D72F18E"),
            Guid.Parse("1CCC4606-658F-4E3E-8751-59167F8B6ACC"),
            Guid.Parse("E708E919-4F91-46C9-ADE9-67F6796A00D6"),
            Guid.Parse("92C54053-0231-4BA5-8834-70995027EAA3"),
            Guid.Parse("253E70B3-D19D-4F9B-A16C-734F0E9E63DA"),
            Guid.Parse("C3B1C515-822C-4BBB-9D1F-9C9B9A49291D"),
            Guid.Parse("7528EB63-B2C1-4362-A2E7-FF7BE63E85F6"),
            Guid.Parse("953E0C1F-72DF-48CA-A0FF-41381560D779"),
            Guid.Parse("EC7CF217-59D1-4DEB-A0F7-46E7DC11CF4D"),
            Guid.Parse("6FEFC6F4-3040-4088-B979-6A56E955F21E"),
            Guid.Parse("96EB5686-46F2-4FF3-98D2-B474AC87B12A"),
            Guid.Parse("64255ABD-A089-4DC5-ACBA-4EBA6757F962"),
            Guid.Parse("8E22840E-D345-4B34-8D97-120DD143296D"),
            Guid.Parse("5D42CBE4-CD8D-4102-9194-FB71766A4D43")
        };

        private static readonly HashSet<Guid> NotCompletedStatuses = new()
        {
            Guid.Parse("BDA4231D-6488-407A-8FDB-0EDBD9DEBBE7"),
            Guid.Parse("91A20A66-EC6D-4DCB-A199-E250604753B6"),
            Guid.Parse("B5D9C721-71F8-4F79-840C-7915AF250A10"),
            Guid.Parse("F70A167C-43AA-49EB-BF75-B06225322630")
        };

        private static readonly HashSet<Guid> PlannedStatuses = new()
        {
            Guid.Parse("7CC17A21-2E18-4442-B971-028A5F73172B"),
            Guid.Parse("FD5248E1-5014-4ABF-9F61-60B247495909"),
            Guid.Parse("D0CDF996-D3AF-4A0F-A353-EB7BCA19F7C6"),
            Guid.Parse("8391C0AF-F11E-4CA3-A6E3-03DA4C07CB46"),
            Guid.Parse("575AC02F-E1BF-4C49-B57D-94BEB03E2232")
        };

        public static int GetRefuseReasonId(Guid reasonGuid)
        {
            if (GuidToReasonIdMap.TryGetValue(reasonGuid, out int reasonId))
            {
                return reasonId;
            }
            return 4;
        }

        public static string GetRefuseReasonName(int reasonId)
        {
            if (ReasonIdToNameMap.TryGetValue(reasonId, out string reasonName))
            {
                return reasonName;
            }
            return string.Empty;
        }

        private static readonly Dictionary<int, string> ReasonIdToNameMap = new Dictionary<int, string>
        {
            { 4, "Прочее" },
            { 34, "Отказ клиента" },
            { 6, "Высокая стоимость" },
            { 7, "Нет времени/занят" },
            { 1, "Обратится самостоятельно" },
            { 31, "Не устроили условия" }
        };

        private static readonly Dictionary<Guid, int> GuidToReasonIdMap = new Dictionary<Guid, int>
        {
            { Guid.Parse("ED93265C-9EF5-4F01-AF31-04DE036A17C8"), 4 },   // Услуга не предоставляется
            { Guid.Parse("28CC55E6-82CC-45D8-998D-63E1473B13BE"), 1 },   // Клиент не определился
            { Guid.Parse("B91854BD-2859-47CA-A0B3-D0138F151D60"), 4 },   // Неинтересное предложение (Прочее)
            { Guid.Parse("E921CCF1-18E8-49A5-A580-F17D737E6289"), 4 },   // Продал авто, новый не купил (Прочее)
            { Guid.Parse("1CB95F2B-E551-4372-A592-20F7C7F72B55"), 6 },   // Дорого
            { Guid.Parse("CB41C518-493E-4F84-9827-7275A0CA279A"), 4 },   // Не обслуживается у дилера (Прочее)
            { Guid.Parse("7D2D5E14-4B2B-4583-BC25-A18CAE786232"), 1 },   // Сравнивает цены
            { Guid.Parse("FAD6B10C-692C-4839-BC77-DC9156FD43D3"), 4 },   // Продал авто, купил непрофильный (Прочее)
            { Guid.Parse("B604CD40-4F5D-4636-ABEA-0544DE1CA663"), 34 },  // Неудобное расположение
            { Guid.Parse("A5A5C969-EF13-478E-B236-33C0046C8329"), 7 },   // Неудобное время визита
            { Guid.Parse("2E1DF5CE-3CF2-468A-886C-5D270D52723C"), 1 },   // Запишется сам
            { Guid.Parse("AC4865F3-8823-4AC1-B3F3-D7AFA77C3161"), 31 },  // Отсутствуют ЗЧ
            { Guid.Parse("7B59D17B-41DD-4842-995E-EFFA14A94E7D"), 4 },   // Обслуживается у другого дилера (Прочее)
            { Guid.Parse("3064D75D-410E-41A4-99EE-0437F2A04201"), 4 },   // Нарекания на качество выполненных работ (Прочее)
            { Guid.Parse("C22F51DE-F351-401E-BF76-809109C20E92"), -1 },  // Клиент записан (не является отказом)
            { Guid.Parse("00869C39-6DCC-49A2-B9D1-9CAF59C6CC85"), 4 },   // Обслуживается в другом ДЦ ГК (Прочее)
            { Guid.Parse("8D12B6DA-1955-46D0-9C85-C80CC0681DF2"), 4 },   // Продал авто, купил профильный (Прочее)
            { Guid.Parse("BF513583-0A9D-486B-993A-E6C53DA8B4BC"), 34 }   // Отказался через ЛК
        };
    }
}