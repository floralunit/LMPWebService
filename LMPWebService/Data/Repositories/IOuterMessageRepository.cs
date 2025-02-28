using LMPWebService.Models;
using System.Threading.Tasks;

namespace LMPWebService.Data.Repositories
{
    public interface IOuterMessageRepository
    {
        Task AddAsync(OuterMessage message);
        Task SaveChangesAsync();
    }
}
