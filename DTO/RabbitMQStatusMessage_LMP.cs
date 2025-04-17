namespace LeadsSaverRabbitMQ.MessageModels;

public class RabbitMQStatusMessage_LMP

{
    public Guid? astra_document_id { get; set; }
    public Guid? astra_document_status_id { get; set; }
    public int? astra_document_subtype_id { get; set; }
    public string responsible_user { get; set; }
}
