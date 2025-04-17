namespace LMPWebService.Models
{
    public class DocumentBase
    {
        public Guid DocumentBase_ID { get; set; }
        public Guid DocumentParent_ID { get; set; }
        public int? DocumentType_ID { get; set; }
        public int? DocumentSubtype_ID { get; set; }
        public Guid DocumentAllowedState_ID { get; set; }

    }
}
