using LMPWebService.DTO;
using LMPWebService.Models;
using System.Threading.Tasks;

namespace LMPWebService.Services.Interfaces
{
    public interface IOuterMessageService
    {
        Task<Guid> SaveMessageAsync(OuterMessage message);
        Task<bool> CheckMessageExistAsync(string leadId, int readerId);
        Task UpdateMessageAsync(OuterMessage message);
        Task<OuterMessage> FindMessageAsync(Guid outerMessage_ID);
        Task<List<OuterMessage>> FindMessagesByStatusAsync(int status);
    }
}
