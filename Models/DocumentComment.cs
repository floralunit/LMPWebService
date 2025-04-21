namespace LMPWebService.Models
{
    public class DocumentComment
    {
        public Guid DocumentComment_ID { get; set; }
        public Guid DocumentBase_ID { get; set; }
        public short DocumentCommentType_ID { get; set; }
        public string Comment { get; set; }

    }
}
