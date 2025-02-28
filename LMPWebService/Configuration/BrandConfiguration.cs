namespace LMPWebService.Configuration
{
    public class BrandConfiguration
    {
        public string BrandName { get; set; }
        public Guid ProjectGuid { get; set; }
        public string Url { get; set; }
        public List<CenterConfiguration> Centers { get; set; }
    }
}
