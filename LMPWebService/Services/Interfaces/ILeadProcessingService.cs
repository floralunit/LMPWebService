using LMPWebService.Models;

namespace LMPWebService.Services.Interfaces
{
    public interface ILeadProcessingService
    {
        Task<ProcessingResult> ProcessLeadAsync(string leadId);
    }

}
