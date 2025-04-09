using LMPWebService.Services;
using LMPWebService.Configuration;
using LMPWebService.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using AutoMapper;
using LMPWebService.DTO;
using System.Text.Json;
using LMPWebService.Services.Interfaces;
using LeadsSaverRabbitMQ.MessageModels;

namespace LMPWebService.Controllers
{
    [ApiController]
    [Route("api/document")]
    public class StatusController : ControllerBase
    {
        private readonly IMassTransitPublisher _massTransitPublisher;
        private readonly IOuterMessageService _messageService;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly ILeadProcessingService _leadProcessingService;

        public StatusController(IMassTransitPublisher massTransitPublisher, IHttpClientLeadService httpClientLeadService, IOuterMessageService messageService,
                              ILeadProcessingService leadProcessingService)
        {
            _massTransitPublisher = massTransitPublisher;
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _leadProcessingService = leadProcessingService;
        }

        [HttpPost("status_changed")]
        public async Task<IActionResult> DocumentStatusChanged([FromBody] RabbitMQStatusMessage_LMP request)
        {

            if (request?.astra_document_id == Guid.Empty || request?.astra_document_id == null || request?.astra_document_type_id == Guid.Empty || request?.astra_document_type_id == null)
                return BadRequest("Некорректные данные");

            await _massTransitPublisher.SendLeadStatusMessage(request);
            return Ok();
        }
    }
}
