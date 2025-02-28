using LMPWebService.DTO;
using LMPWebService.Models;
using System.Threading.Tasks;

namespace LMPWebService.Services.Interfaces
{
    public interface IOuterMessageService
    {
        Task<Guid> SaveMessageAsync(OuterMessage message);
    }
}
