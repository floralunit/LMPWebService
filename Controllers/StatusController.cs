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
        [ProducesResponseType(typeof(PostStatusResponse), 200)]
        [ProducesResponseType(typeof(PostStatusResponse), 400)]
        public async Task<ActionResult<PostStatusResponse>> DocumentStatusChanged([FromBody] RabbitMQStatusMessage_LMP request)
        {
            var response = new PostStatusResponse();

            if (request?.astra_document_id == Guid.Empty ||
                request?.astra_document_id == null)
            {
                response.success = false;
                response.type = "BadRequest";
                response.title = "Некорректные данные";
                response.errors = new Dictionary<string, string>
                    {
                        {"astra_document_id", "Неверный идентификатор документа"}
                    };
                return BadRequest(response);
            }


            //if (request?.astra_document_subtype_id == null)
            //{
            //    response.success = false;
            //    response.type = "BadRequest";
            //    response.title = "Некорректные данные";
            //    response.errors = new Dictionary<string, string>
            //        {
            //            {"astra_document_subtype_id", "Неверный тип документа"}
            //        };
            //    return BadRequest(response);
            //}


            if (request?.astra_document_status_id == Guid.Empty ||
                request?.astra_document_status_id == null)
            {
                response.success = false;
                response.type = "BadRequest";
                response.title = "Некорректные данные";
                response.errors = new Dictionary<string, string>
                    {
                        {"astra_document_status_id", "Неверный статус документа"}
                    };
                return BadRequest(response);
            }

            try
            {
                await _massTransitPublisher.SendLeadStatusMessage(request);

                response.success = true;
                response.type = "Success";
                response.title = "Статус успешно обновлен";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.success = false;
                response.type = "InternalServerError";
                response.title = "Ошибка при обработке запроса";
                response.errors = new Dictionary<string, string>
                    {
                        {"ServerError", ex.Message}
                    };
                return StatusCode(500, response);
            }
        }
    }
}
