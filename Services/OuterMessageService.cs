using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LMPWebService.Services
{
    public class OuterMessageService : IOuterMessageService
    {
        private readonly AstraDbContext _dbContext;
        private readonly IMessageQueueService _queueService;
        private readonly ILogger<OuterMessageService> _logger;

        public OuterMessageService(AstraDbContext dbContext, IMessageQueueService queueService, ILogger<OuterMessageService> logger)
        {
            _dbContext = dbContext;
            _queueService = queueService;
            _logger = logger;
        }

        public async Task<List<OuterMessage>> FindMessagesByStatusAsync(int status)
        {
            var messageList = await _dbContext.OuterMessage.Where(x => x.ProcessingStatus == status 
                                && new int[] {24, 25, 26, 27, 28, 29}.Contains(x.OuterMessageReader_ID) ).ToListAsync();

            return messageList;
        }

        public async Task<OuterMessage> FindMessageAsync(Guid outerMessage_ID)
        {
            var messageDB = await _dbContext.OuterMessage.FirstOrDefaultAsync(x => x.OuterMessage_ID == outerMessage_ID);

            return messageDB;
        }


        public async Task UpdateMessageAsync(OuterMessage message)
        {
            _dbContext.OuterMessage.Update(message);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Guid> SaveMessageAsync(OuterMessage message)
        {
            await _dbContext.OuterMessage.AddAsync(message);
            await _dbContext.SaveChangesAsync();

            return message.OuterMessage_ID;
        }

        public async Task<bool> CheckMessageExistAsync(string leadId, int readerID)
        {
            var lead = await _dbContext.OuterMessage.FirstOrDefaultAsync(x => x.MessageOuter_ID == leadId && x.OuterMessageReader_ID == readerID);

            return lead != null;
        }
        public async Task<OuterMessageReader?> FindReaderByOutletCodeAsync(string outlet_code)
        {
            return await _dbContext.OuterMessageReader.FirstOrDefaultAsync(x => x.OuterMessageReaderName.Contains(outlet_code));
        }

    }
}
