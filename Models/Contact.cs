namespace LMPWebService.Models
{
    public class Contact
    {
        public Guid Contact_ID { get; set; }
        public Guid? ContactType_ID { get; set; }
        public Guid? ContactSubject_ID { get; set; }
        public Guid? ContactFailureReason_ID { get; set; }
        public Guid? ContactWorkOrderRefuseReason_ID { get; set; }
        public DateTime? PlanDate { get; set; }

    }
}
