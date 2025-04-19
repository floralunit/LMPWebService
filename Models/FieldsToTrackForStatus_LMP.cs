using System;

namespace LMPWebService.Models
{
    public class FieldsToTrackForStatus_LMP
    {
        public Guid? OuterMessage_ID { get; set; }
        public string FieldName { get; set; }
        public Guid? EMessage_ID { get; set; }
        public string FieldContent { get; set; }
        public DateTime InsDate { get; set; }
        public bool SendStatus { get; set; }
    }
}
