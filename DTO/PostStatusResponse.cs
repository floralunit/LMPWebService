using System.Runtime.Serialization;

namespace LMPWebService.DTO
{
    public class PostStatusResponse
    {
        public bool success { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public Dictionary<string, string> errors { get; set; }
    }
}
