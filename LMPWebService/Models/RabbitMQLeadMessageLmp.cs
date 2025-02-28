namespace LMPWebService.Models
{
    public class RabbitMQLeadMessageLmp
    {
        public Guid Message_ID { get; set; }
        public Guid Center_ID { get; set; }
        public string BrandName { get; set; }
    }
}
