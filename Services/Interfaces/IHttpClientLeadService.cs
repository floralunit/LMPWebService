using System.Threading.Tasks;
using static LMPWebService.Services.HttpClientLeadService;

namespace LMPWebService.Services.Interfaces
{
    public interface IHttpClientLeadService
    {
        Task<string> GetLeadDataAsync(string leadId, string outlet_code);
        Task<LeadStatusResponseDto> SendStatusAsync(string lead_id, string outlet_code);
    }
}
