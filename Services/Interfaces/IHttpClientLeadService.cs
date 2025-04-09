using System.Threading.Tasks;
using YourNamespace.Dtos;
using static LMPWebService.Services.HttpClientLeadService;

namespace LMPWebService.Services.Interfaces
{
    public interface IHttpClientLeadService
    {
        Task<string> GetLeadDataAsync(string leadId, string outlet_code);
        Task<LeadStatusResponseDto> SendStatusResponsibleAsync(string lead_id, string outlet_code, string responsibleNam);
        Task<LeadStatusResponseDto> SendStatusAsync(LeadStatusRequestDto request, string outlet_code);
    }
}
