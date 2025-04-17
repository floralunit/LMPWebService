using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using YourNamespace.Dtos;

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

        // Статические коллекции для хранения GUID-ов статусов
        private static readonly HashSet<Guid> ClientRefusalStatuses = new()
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

        public async Task SendStatusResponsibleAsync(Guid messageID, string outletCode, string responsibleName)
        {
            if (string.IsNullOrWhiteSpace(outletCode))
                throw new ArgumentException("Outlet code cannot be null or empty", nameof(outletCode));

            if (string.IsNullOrWhiteSpace(responsibleName))
                throw new ArgumentException("Responsible name cannot be null or empty", nameof(responsibleName));

            var entityMessage = await _messageService.FindMessageAsync(messageID);
            if (entityMessage == null)
            {
                _logger.LogError($"[SendStatusService] Message ({messageID}) was not found in OuterMessage table", messageID);
                return;
            }

            try
            {
                var statusResponse = await _httpClientLeadService.SendStatusResponsibleAsync(
                    messageID.ToString(),
                    outletCode,
                    responsibleName);

                if (statusResponse == null || !statusResponse.is_success)
                {
                    LogAndUpdateErrorStatus(entityMessage, statusResponse?.error ?? "Unknown error");
                    return;
                }

                await UpdateMessageSuccessStatus(entityMessage);
                _logger.LogInformation("Для лида id {MessageId} успешно обновлен статус Взят в работу", entityMessage.MessageOuter_ID);
            }
            catch (Exception e)
            {
                var errorMes = $"[SendStatusService] Error processing messageId={messageID}: {e.Message}";
                if (e.InnerException != null)
                    errorMes += $"\nInnerException: {e.InnerException.Message}";

                _logger.LogError(errorMes);
                await UpdateMessageErrorStatus(entityMessage, errorMes);
            }
        }

        public async Task SendStatusAsync(RabbitMQStatusMessage_LMP message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var docID = message.astra_document_id;
            var statusID = message.astra_document_status_id;
            var docTypeID = message.astra_document_subtype_id;

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
                            "[SendStatusService] Для contact = {ContactId} не найден соотвествующий Contract",
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

                    statusToSend = ProcessWorkOrderStatus(statusID.Value, workOrder, statusDTO);
                }

                if (statusToSend != -1 && lead != null)
                {
                    statusDTO.responsible_user = message.responsible_user;
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
                contact.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86") &&
                docChildren.Count > 0)
            {
                foreach (var childDoc in docChildren)
                {
                    var childContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == childDoc.DocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86"));

                    if (childContact != null)
                    {
                        var childDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == childContact.Contact_ID);

                        if (childDocumentBase.DocumentAllowedState_ID == Guid.Parse("b00fb2fd-589e-447e-ad3e-1616445dc747"))
                        {
                            statusToSend = 10;
                            statusDTO.status_comment = "Содержание контакта (факт) \"невыполненного\" документа КсК";
                            statusDTO.dealer_visit_planned_date = childContact.PlanDate;
                        }
                    }
                }
            }

            if (CompletedStatuses.Contains(statusId))
            {
                if (contact.ContactType_ID == Guid.Parse("bdece6ef-3a4f-45cc-b6ef-cb9e8e6d038c"))
                {
                    statusToSend = 44;
                    statusDTO.status_comment = "Содержание контакта (факт) документа";
                }
                else if (contact.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86"))
                {
                    statusToSend = 9;
                    statusDTO.status_comment = "Содержание контакта (факт) документа";
                }
                else if (contact.ContactType_ID == Guid.Parse("09eafe1d-e316-46ea-a0b8-58cd2152c4d2") ||
                         contact.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"))
                {
                    statusToSend = 36;
                    statusDTO.status_comment = "Содержание контакта (факт) документа";
                }
            }

            if (contact.ContactType_ID == Guid.Parse("f49301fc-9b95-4b2b-953c-3b02f58aaf86") &&
                PlannedStatuses.Contains(statusId))
            {
                statusToSend = 8;
                statusDTO.status_comment = "Содержание контакта (план)";
                statusDTO.dealer_visit_planned_date = contact.PlanDate;
            }

            if (contact.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"))
            {
                var outgoingCallResult = await ProcessOutgoingCall(contact, statusId, docParents, docChildren);
                if (outgoingCallResult.statusToSend != -1)
                {
                    statusToSend = outgoingCallResult.statusToSend;
                    statusDTO = outgoingCallResult.statusDTO;
                }
            }

            if (contact.ContactType_ID == Guid.Parse("c25a8615-efdd-43e9-a4ec-38c5ecaa354d"))
            {
                if (PlannedStatuses.Contains(statusId))
                {
                    statusToSend = 37;
                    statusDTO.status_comment = "Содержание контакта (план)";
                    statusDTO.dealer_visit_planned_date = contact.PlanDate;
                }
                else if (statusId == Guid.Parse("331F0156-81CA-427F-B048-5C0EF177FBEF") ||
                         statusId == Guid.Parse("A4DBB71A-4A40-4C1B-9C67-E85225A5B2CB"))
                {
                    statusToSend = 38;
                    statusDTO.status_comment = "Содержание контакта (факт)";
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
                foreach (var parentDoc in docParents)
                {
                    var parentContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc.ParentDocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"));

                    if (parentContact != null)
                    {
                        var parentContactDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact.Contact_ID);

                        if (parentContactDocumentBase.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2"))
                        {
                            var docParents2 = await _dbContext.DocumentBaseParent
                                .Where(x => x.DocumentBase_ID == parentContactDocumentBase.DocumentBase_ID)
                                .ToListAsync();

                            foreach (var parentDoc2 in docParents2)
                            {
                                var parentContact2 = await _dbContext.Contact
                                    .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc2.ParentDocumentBase_ID &&
                                                          x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"));

                                if (parentContact2 != null)
                                {
                                    var parentContactDocumentBase2 = await _dbContext.DocumentBase
                                        .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact2.Contact_ID);

                                    if (parentContactDocumentBase2.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2"))
                                    {
                                        statusToSend = 4;
                                        statusDTO.status_comment = "Содержание контакта (факт)";
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
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"));

                    if (childContact != null)
                    {
                        var childDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == childContact.Contact_ID);

                        if (childDocumentBase.DocumentAllowedState_ID == Guid.Parse("b00fb2fd-589e-447e-ad3e-1616445dc747"))
                        {
                            statusToSend = 5;
                            statusDTO.dealer_recall_date = childContact.PlanDate;
                            statusDTO.status_comment = "Содержание контакта (факт) \"невыполненного\" документа КсК";
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
            if (NotCompletedStatuses.Contains(statusId))
            {
                foreach (var parentDoc in docParents)
                {
                    var parentContact = await _dbContext.Contact
                        .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc.ParentDocumentBase_ID &&
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"));

                    if (parentContact != null)
                    {
                        var parentContactDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact.Contact_ID);

                        if (parentContactDocumentBase.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2"))
                        {
                            var docParents2 = await _dbContext.DocumentBaseParent
                                .Where(x => x.DocumentBase_ID == parentContactDocumentBase.DocumentBase_ID)
                                .ToListAsync();

                            foreach (var parentDoc2 in docParents2)
                            {
                                var parentContact2 = await _dbContext.Contact
                                    .FirstOrDefaultAsync(x => x.Contact_ID == parentDoc2.ParentDocumentBase_ID &&
                                                          x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"));

                                if (parentContact2 != null)
                                {
                                    var parentContactDocumentBase2 = await _dbContext.DocumentBase
                                        .FirstOrDefaultAsync(x => x.DocumentBase_ID == parentContact2.Contact_ID);

                                    if (parentContactDocumentBase2.DocumentAllowedState_ID == Guid.Parse("a28e950d-88fb-4769-a917-610eca7138e2"))
                                    {
                                        statusToSend = 4;
                                        statusDTO.status_comment = "Содержание контакта (факт)";
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
                                              x.ContactType_ID == Guid.Parse("a8cd493d-daa6-4af1-8c2d-6070334ea75a"));

                    if (childContact != null)
                    {
                        var childDocumentBase = await _dbContext.DocumentBase
                            .FirstOrDefaultAsync(x => x.DocumentBase_ID == childContact.Contact_ID);

                        if (childDocumentBase.DocumentAllowedState_ID == Guid.Parse("b00fb2fd-589e-447e-ad3e-1616445dc747"))
                        {
                            statusToSend = 5;
                            statusDTO.dealer_recall_date = childContact.PlanDate;
                            statusDTO.status_comment = "Содержание контакта (факт) \"невыполненного\" документа КсК";
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

        private int ProcessWorkOrderStatus(Guid statusId, WorkOrder workOrder, LeadStatusRequestDto statusDTO)
        {
            var statusToSend = -1;

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
                statusId == Guid.Parse("34C40DEE-47D3-4EF9-8F9E-F73FEB460E67"))
            {
                statusToSend = 9;
                statusDTO.status_comment = "Автомобиль поступил";
            }
            else if (statusId == Guid.Parse("AEFD9D1C-FF5D-469C-8A09-9C1C45987D43") ||
                     statusId == Guid.Parse("6AF78A95-68E0-44E3-B258-AEFAC1F5516B") ||
                     statusId == Guid.Parse("84166577-1670-4FDE-B4EB-E15B7D891F84") ||
                     statusId == Guid.Parse("8CE97943-D26A-44AC-84FB-FB3222B3058D"))
            {
                statusToSend = 41;
                statusDTO.status_comment = "Консультация проведена";
            }
            else if (statusId == Guid.Parse("479B4D5B-9C76-43A7-A1EB-8AF851C407BE"))
            {
                statusToSend = 8;
                statusDTO.dealer_visit_planned_date = workOrder?.ReceptionDate;
            }
            else if (ClientRefusalStatuses.Contains(statusId))
            {
                statusToSend = 7;
            }
            else if (statusId == Guid.Parse("93FE5BEC-2D58-4C67-8C44-CD6E838086AB"))
            {
                statusToSend = 11;
                statusDTO.status_comment = "Ремонт выполнен";
            }

            return statusToSend;
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
    }
}