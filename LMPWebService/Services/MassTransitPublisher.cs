using MassTransit;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using LMPWebService.Configuration;
using Microsoft.Extensions.Options;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;

namespace LMPWebService.Services
{
    public class MassTransitPublisher : IMassTransitPublisher
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly RabbitMqSettings _settings;

        public MassTransitPublisher(ISendEndpointProvider sendEndpointProvider, IOptions<RabbitMqSettings> options)
        {
            _sendEndpointProvider = sendEndpointProvider;
            _settings = options.Value;
        }

        public async Task SendLeadReceivedMessage(RabbitMQLeadMessageLmp message)
        {
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{_settings.SendLeadsLmpQueueName}"));

            await sendEndpoint.Send(message);
        }

        public async Task SendLeadStatusMessage(Guid leadId, string status)
        {
            var message = new { LeadId = leadId, Status = status };
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{_settings.SendStatusLmpQueueName}"));

            await sendEndpoint.Send(message);
        }
    }
}
