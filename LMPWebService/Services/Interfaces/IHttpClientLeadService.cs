using System.Threading.Tasks;

namespace LMPWebService.Services.Interfaces
{
    public interface IHttpClientLeadService
    {
        Task<string> GetLeadDataAsync(string leadId, string outlet_code);
    }
}
