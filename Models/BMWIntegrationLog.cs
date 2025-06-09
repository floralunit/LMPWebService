using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMPWebService.Models
{
    [Table("BMWIntegrationLogs")]
    public class BMWIntegrationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        [Required]
        public DateTime LogDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string OperationType { get; set; } // 'IncomingRequest', 'GetToken', 'GetLeadData', 'SendStatus' и т.д.

        [StringLength(50)]
        public string LeadId { get; set; }

        [StringLength(20)]
        public string OutletCode { get; set; }

        [StringLength(500)]
        public string RequestUrl { get; set; }

        public string RequestBody { get; set; }

        public string ResponseBody { get; set; }

        public int? ResponseStatus { get; set; }

        public bool? IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public Guid? CorrelationId { get; set; }

        public string AdditionalInfo { get; set; }

        public int? ProcessingTimeMs { get; set; }
    }
}