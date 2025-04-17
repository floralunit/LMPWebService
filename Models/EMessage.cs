
namespace LMPWebService.Models
{
    public class EMessage
    {
        public Guid EMessage_ID { get; set; }
        public Guid? VisitAim_ID { get; set; }
        public Guid? EMessageSubject_ID { get; set; }
        //public string? ResponsibleName { get; set; }
        public Guid? OuterMessage_ID { get; set; }

    }
}
