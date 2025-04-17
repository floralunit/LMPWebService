using LMPWebService.DTO;
using LMPWebService.Models;
using System.Threading.Tasks;

namespace LMPWebService.Services.Interfaces
{
    public interface IOuterMessageService
    {
        Task<Guid> SaveMessageAsync(OuterMessage message);
        Task<bool> CheckMessageExistAsync(string leadId, int readerID);
        Task UpdateMessageAsync(OuterMessage message);
        Task<OuterMessage> FindMessageAsync(Guid outerMessage_ID);
        Task<OuterMessageReader?> FindReaderByOutletCodeAsync(string outlet_code);
        Task<List<OuterMessage>> FindMessagesByStatusAsync(int status);
    }
}
