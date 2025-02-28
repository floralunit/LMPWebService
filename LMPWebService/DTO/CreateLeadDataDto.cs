using System.ComponentModel.DataAnnotations;

namespace LMPWebService.DTO
{
    public class CreateLeadDataDto
    {
        public int? OuterMessageReader_ID { get; set; }
        public string? MessageOuter_ID { get; set; }
        public byte? ProcessingStatus { get; set; }
        public string? MessageText { get; set; }
        public int? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}