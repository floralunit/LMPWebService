using LMPWebService.Models;

namespace LMPWebService.Services.Interfaces
{
    public interface IMassTransitPublisher
    {
        Task SendLeadReceivedMessage(RabbitMQLeadMessageLmp message);
        Task SendLeadStatusMessage(Guid leadId, string status);
    }
}
