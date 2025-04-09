using MassTransit;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using LMPWebService.Configuration;
using Microsoft.Extensions.Options;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using LeadsSaverRabbitMQ.MessageModels;

namespace LMPWebService.Services
{
    public class MassTransitPublisher : IMassTransitPublisher
    {
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        private readonly RabbitMqSettings _settings;

        public MassTransitPublisher(ISendEndpointProvider sendEndpointProvider, IOptions<RabbitMqSettings> options, IPublishEndpoint publishEndpoint)
        {
            _sendEndpointProvider = sendEndpointProvider;
            _settings = options.Value;
            _publishEndpoint = publishEndpoint;
        }

        public async Task SendLeadReceivedMessage(RabbitMQLeadMessage_LMP message)
        {
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{_settings.QueueName_SendLeads_LMP}"));

            await sendEndpoint.Send(message);
        }

        public async Task SendLeadStatusMessage(RabbitMQStatusMessage_LMP message)
        {
            //var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{_settings.QueueName_SendStatus_LMP}"));

            //await sendEndpoint.Send(message);
            await _publishEndpoint.Publish<RabbitMQStatusMessage_LMP>(message);
        }
    }
}
