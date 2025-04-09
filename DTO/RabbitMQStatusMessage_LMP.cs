namespace LeadsSaverRabbitMQ.MessageModels;

public class RabbitMQStatusMessage_LMP

{
    public Guid? astra_document_id { get; set; }
    public Guid? astra_document_status_id { get; set; }
    public Guid? astra_document_type_id { get; set; }
}
