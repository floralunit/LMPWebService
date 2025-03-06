namespace LeadsSaverRabbitMQ.MessageModels;

public class RabbitMQStatusMessage_LMP

{
    public Guid Message_ID { get; set; }
    public string Lead_Id { get; set; }
}
