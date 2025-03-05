namespace LeadsSaverRabbitMQ.MessageModels;

public class RabbitMQLeadMessage_LMP
{
    public Guid Message_ID { get; set; }
    public string OutletCode { get; set; }
}
