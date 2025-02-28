using LMPWebService.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LMPWebService.Data.Repositories
{
    public class OuterMessageRepository : IOuterMessageRepository
    {
        private readonly AstraDbContext _dbContext;

        public OuterMessageRepository(AstraDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(OuterMessage message)
        {
            await _dbContext.OuterMessage.AddAsync(message);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
