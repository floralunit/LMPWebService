using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LMPWebService.Configuration;
using LMPWebService.Models;
using LMPWebService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YourNamespace.Dtos;

namespace LMPWebService.Services
{
    public class HttpClientLeadService : IHttpClientLeadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientLeadService> _logger;
        private readonly AuthSettings _authSettings;
        private readonly IBmwIntegrationLogger _bmwLogger;

        public HttpClientLeadService(
            HttpClient httpClient,
            ILogger<HttpClientLeadService> logger,
            IOptions<AuthSettings> authSettings,
            IBmwIntegrationLogger bmwLogger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authSettings = authSettings.Value;
            _bmwLogger = bmwLogger;
        }

        public async Task<string> GetLeadDataAsync(string leadId, string outlet_code)
        {
            var correlationId = _bmwLogger.GenerateCorrelationId();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var requestBody = new { lead_id = leadId };
                var json = JsonSerializer.Serialize(requestBody);

                await _bmwLogger.LogIncomingRequestAsync("GetLeadData", leadId, outlet_code, json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await GetAccessTokenAsync(leadId, outlet_code);
                if (token == null)
                {
                    const string error = "Токен пустой";
                    await _bmwLogger.LogOperationAsync("GetLeadData", leadId, outlet_code,
                        requestBody, null, false, error);
                    _logger.LogError(error);
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(token.token_type, token.access_token);

                var url = $"{_authSettings.BaseUrl}/v1/crm/lead";
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                await _bmwLogger.LogApiCallAsync(
                    "BMWApiCall",
                    leadId,
                    outlet_code,
                    url,
                    json,
                    responseContent,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    null,
                    stopwatch.Elapsed);

                response.EnsureSuccessStatusCode();
                return responseContent;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _bmwLogger.LogOperationAsync(
                    "GetLeadData",
                    leadId,
                    outlet_code,
                    null,
                    null,
                    false,
                    ex.Message);

                _logger.LogError(ex, $"Ошибка при запросе данных лида {leadId}");
                throw;
            }
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string lead_id, string outlet_code)
        {
            var correlationId = _bmwLogger.GenerateCorrelationId();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var authString = $"{outlet_code}\\{_authSettings.Username}:{_authSettings.Password}";
                var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", base64AuthString);

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "int_leadcrm")
                });

                var url = $"{_authSettings.BaseUrl}/identity/connect/token";
                var response = await _httpClient.PostAsync(url, requestBody);
                var responseContent = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                await _bmwLogger.LogApiCallAsync(
                    "GetToken",
                    lead_id,
                    outlet_code,
                    url,
                    "grant_type=client_credentials&scope=int_leadcrm",
                    responseContent,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    null,
                    stopwatch.Elapsed);

                response.EnsureSuccessStatusCode();

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                if (tokenResponse?.access_token != null)
                {
                    await _bmwLogger.LogOperationAsync(
                        "GetToken",
                        lead_id,
                        outlet_code,
                        null,
                        new { token_type = tokenResponse.token_type, expires_in = tokenResponse.expires_in },
                        true,
                        null);

                    _logger.LogInformation("Токен успешно получен");
                }

                return tokenResponse;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _bmwLogger.LogOperationAsync(
                    "GetToken",
                    lead_id,
                    outlet_code,
                    null,
                    null,
                    false,
                    ex.Message);

                _logger.LogError(ex, "Ошибка при получении токена");
                throw;
            }
        }

        public async Task<LeadStatusResponseDto> SendStatusResponsibleAsync(string lead_id, string outlet_code, string responsibleName)
        {
            var correlationId = _bmwLogger.GenerateCorrelationId();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var requestBody = new { lead_id, responsible_user = responsibleName };
                var json = JsonSerializer.Serialize(requestBody);

                await _bmwLogger.LogIncomingRequestAsync("SendStatusResponsible", lead_id, outlet_code, json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await GetAccessTokenAsync(lead_id, outlet_code);
                if (token == null)
                {
                    const string error = "Токен пустой";
                    await _bmwLogger.LogOperationAsync(
                        "SendStatusResponsible",
                        lead_id,
                        outlet_code,
                        requestBody,
                        null,
                        false,
                        error);

                    _logger.LogError(error);
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(token.token_type, token.access_token);

                var url = $"{_authSettings.BaseUrl}/v1/crm/lead_distributed";
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                await _bmwLogger.LogApiCallAsync(
                    "BMWApiCall",
                    lead_id,
                    outlet_code,
                    url,
                    json,
                    responseContent,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    null,
                    stopwatch.Elapsed);

                response.EnsureSuccessStatusCode();

                var statusResponse = JsonSerializer.Deserialize<LeadStatusResponseDto>(responseContent);

                await _bmwLogger.LogOperationAsync(
                    "SendStatusResponsible",
                    lead_id,
                    outlet_code,
                    requestBody,
                    statusResponse,
                    true,
                    null);

                return statusResponse;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _bmwLogger.LogOperationAsync(
                    "SendStatusResponsible",
                    lead_id,
                    outlet_code,
                    null,
                    null,
                    false,
                    ex.Message);

                _logger.LogError(ex, $"Ошибка при отправке статуса о взятии в работу лида {lead_id}");
                throw;
            }
        }

        public async Task<LeadStatusResponseDto> SendStatusAsync(LeadStatusRequestDto request, string outlet_code)
        {
            var correlationId = _bmwLogger.GenerateCorrelationId();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var json = JsonSerializer.Serialize(request);

                await _bmwLogger.LogIncomingRequestAsync("SendStatus", request.lead_id, outlet_code, json);
                _logger.LogInformation($"[SendStatusAsync] Попытка отправки статуса json: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await GetAccessTokenAsync(request.lead_id, outlet_code);
                if (token == null)
                {
                    const string error = "Токен пустой";
                    await _bmwLogger.LogOperationAsync(
                        "SendStatus",
                        request.lead_id,
                        outlet_code,
                        request,
                        null,
                        false,
                        error);

                    _logger.LogError(error);
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(token.token_type, token.access_token);

                var url = $"{_authSettings.BaseUrl}/v1/crm/status";
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                await _bmwLogger.LogApiCallAsync(
                    "BMWApiCall",
                    request.lead_id,
                    outlet_code,
                    url,
                    json,
                    responseContent,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    null,
                    stopwatch.Elapsed);

                response.EnsureSuccessStatusCode();

                var statusResponse = JsonSerializer.Deserialize<LeadStatusResponseDto>(responseContent);

                await _bmwLogger.LogOperationAsync(
                    "SendStatus",
                    request.lead_id,
                    outlet_code,
                    request,
                    statusResponse,
                    true,
                    null);

                return statusResponse;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _bmwLogger.LogOperationAsync(
                    "SendStatus",
                    request.lead_id,
                    outlet_code,
                    request,
                    null,
                    false,
                    ex.Message);

                _logger.LogError(ex, $"Ошибка при отправке статуса {request.status} лида {request.lead_id}");
                throw;
            }
        }

        public class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }

        public class LeadStatusResponseDto
        {
            public string error { get; set; }
            public bool is_success { get; set; }
        }
    }
}