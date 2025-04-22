namespace LMPWebService.Models
{
    public class DocumentBase
    {
        public Guid DocumentBase_ID { get; set; }
        public Guid? DocumentParent_ID { get; set; }
        public short DocumentType_ID { get; set; }
        public short DocumentSubtype_ID { get; set; }
        public Guid DocumentAllowedState_ID { get; set; }
        public string? DocumentBaseNumber { get; set; }

    }
}
