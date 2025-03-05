using LeadsSaverRabbitMQ.MessageModels;
using LMPWebService.Models;

namespace LMPWebService.Services.Interfaces
{
    public interface IMassTransitPublisher
    {
        Task SendLeadReceivedMessage(RabbitMQLeadMessage_LMP message);
        Task SendLeadStatusMessage(Guid leadId, string status);
    }
}
