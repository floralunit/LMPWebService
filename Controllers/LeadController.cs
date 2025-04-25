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

namespace LMPWebService.Controllers
{
    [ApiController]
    [Route("api/leads")]
    public class LeadController : ControllerBase
    {
        private readonly IMassTransitPublisher _massTransitPublisher;
        private readonly IOuterMessageService _messageService;
        private readonly IHttpClientLeadService _httpClientLeadService;
        private readonly ILeadProcessingService _leadProcessingService;

        private readonly ILogger<LeadController> _logger;

        public LeadController(IMassTransitPublisher massTransitPublisher, IHttpClientLeadService httpClientLeadService, IOuterMessageService messageService,
                              ILeadProcessingService leadProcessingService, ILogger<LeadController> logger)
        {
            _massTransitPublisher = massTransitPublisher;
            _httpClientLeadService = httpClientLeadService;
            _messageService = messageService;
            _leadProcessingService = leadProcessingService;
            _logger = logger;
        }
        [HttpGet("receive")]
        public async Task<IActionResult> ReceiveLead([FromQuery] string outlet_code, [FromBody] LeadReceivedRequest request)
        {
            _logger.LogInformation($"[ReceiveLeadPost] Поступил лид на обработку lead_id={request.lead_id}, outlet_code={outlet_code}, action={request.action}");

            Guid.TryParse(request?.lead_id, out var leadIdGuid);

            if (leadIdGuid == Guid.Empty || request?.lead_id == null || string.IsNullOrEmpty(outlet_code))
            {
                _logger.LogError($"[ReceiveLeadPost] Поступил лид с некорректными данными lead_id={request.lead_id}, outlet_code={outlet_code}, action={request.action}");
                return BadRequest("Некорректные данные");
            }

            var result = await _leadProcessingService.ProcessLeadAsync(leadIdGuid, outlet_code);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(new { message = "Лид успешно получен и отправлен в работу" });
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveLeadPost([FromQuery] string outlet_code, [FromBody] LeadReceivedRequest request)
        {
            _logger.LogInformation($"[ReceiveLeadPost] Поступил лид на обработку lead_id={request.lead_id}, outlet_code={outlet_code}, action={request.action}");

            Guid.TryParse(request?.lead_id, out var leadIdGuid);

            if (leadIdGuid == Guid.Empty || request?.lead_id == null || string.IsNullOrEmpty(outlet_code))
            {
                _logger.LogError($"[ReceiveLeadPost] Поступил лид с некорректными данными lead_id={request.lead_id}, outlet_code={outlet_code}, action={request.action}");
                return BadRequest("Некорректные данные");
            }

            var result = await _leadProcessingService.ProcessLeadAsync(leadIdGuid, outlet_code);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(new { message = "Лид успешно получен и отправлен в работу" });
        }
    }
}
