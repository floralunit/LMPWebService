namespace LeadsSaverRabbitMQ.MessageModels;

public class RabbitMQLeadMessage_LMP
{
    public Guid OuterMessage_ID { get; set; }
    public int OuterMessageReader_ID { get; set; }
    public string OutletCode { get; set; }
}
