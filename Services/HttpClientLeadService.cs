using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LMPWebService.Configuration;
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

        public HttpClientLeadService(HttpClient httpClient, ILogger<HttpClientLeadService> logger, IOptions<AuthSettings> authSettings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _authSettings = authSettings.Value;
        }

        public async Task<string> GetLeadDataAsync(string leadId, string outlet_code)
        {
            try
            {
                var requestBody = new { lead_id = leadId };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await GetAccessTokenAsync(leadId, outlet_code);
                if (token == null)
                {
                    _logger.LogError("Токен пустой");
                    return null;
                }
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);

                var response = await _httpClient.PostAsync($"{_authSettings.BaseUrl}/v1/crm/lead", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при запросе данных лида {leadId}");
                throw;
            }
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string lead_id, string outlet_code)
        {
            try
            {
                var authString = $"{outlet_code}\\{_authSettings.Username}:{_authSettings.Password}";
                var base64AuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64AuthString);

                // Формируем тело запроса
                var requestBody = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "int_leadcrm")
        });

                // Отправляем POST-запрос
                var response = await _httpClient.PostAsync($"{_authSettings.BaseUrl}/identity/connect/token", requestBody);

                // Проверяем успешность запроса
                response.EnsureSuccessStatusCode();

                // Читаем ответ и десериализуем JSON
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                if (tokenResponse?.access_token != null)
                    _logger.LogInformation("Токен успешно получен");

                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении токена");
                throw;
            }
        }

        public async Task<LeadStatusResponseDto> SendStatusResponsibleAsync(string lead_id, string outlet_code, string responsibleName)
        {
            try
            {
                var requestBody = new { lead_id = lead_id, responsible_user = responsibleName };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await GetAccessTokenAsync(lead_id, outlet_code);
                if (token == null)
                {
                    _logger.LogError("Токен пустой");
                    return null;
                }
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);

                //var response = await _httpClient.PostAsync($"{_authSettings.BaseUrl}/v1/crm/lead_distributed", content);
                //response.EnsureSuccessStatusCode();

                //var responseContent = await response.Content.ReadAsStringAsync();
                //var statusResponse = JsonSerializer.Deserialize<LeadStatusResponseDto>(responseContent);

                //return statusResponse;

                return new LeadStatusResponseDto() { is_success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке статуса о взятии в работу лида {lead_id}");
                throw;
            }
        }

        public async Task<LeadStatusResponseDto> SendStatusAsync(LeadStatusRequestDto request, string outlet_code)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await GetAccessTokenAsync(request.lead_id, outlet_code);
                if (token == null)
                {
                    _logger.LogError("Токен пустой");
                    return null;
                }
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);

                //var response = await _httpClient.PostAsync($"{_authSettings.BaseUrl}/v1/crm/status", content);
                //response.EnsureSuccessStatusCode();

                //var responseContent = await response.Content.ReadAsStringAsync();
                //var statusResponse = JsonSerializer.Deserialize<LeadStatusResponseDto>(responseContent);

                //return statusResponse;

                return new LeadStatusResponseDto() { is_success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отправке статуса {request.status} лида {request.lead_id}");
                throw;
            }
        }

        /// <summary>
        /// Модель для десериализации ответа с токеном.
        /// </summary>
        public class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }

        /// <summary>
        /// Модель для десериализации ответа со статусом.
        /// </summary>
        public class LeadStatusResponseDto
        {
            public string error { get; set; }
            public bool is_success { get; set; }
        }

    }
}
