using LMPWebService.Data;
using LMPWebService.DTO;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task<Guid> SaveMessageAsync(OuterMessage message)
        {
            await _dbContext.OuterMessage.AddAsync(message);
            await _dbContext.SaveChangesAsync();

            return message.OuterMessage_ID;
        }

        public async Task<bool> CheckMessageExistAsync(string leadId, int readerId)
        {
            var lead = await _dbContext.OuterMessage.FirstOrDefaultAsync(x => x.MessageOuter_ID == leadId && x.OuterMessageReader_ID == readerId);

            if (lead != null)
            {
                _logger.LogInformation($"Лид {leadId} уже существует в таблице OuterMessage c ProcessingStatus={lead.ProcessingStatus}", DateTimeOffset.Now);
                return true;
            }

            return false;
        }
    }
}
