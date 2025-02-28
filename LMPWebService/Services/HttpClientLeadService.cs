using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LMPWebService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LMPWebService.Services
{
    public class HttpClientLeadService : IHttpClientLeadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpClientLeadService> _logger;

        public HttpClientLeadService(HttpClient httpClient, ILogger<HttpClientLeadService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GetLeadDataAsync(string leadId)
        {
            try
            {
                var requestBody = new { lead_id = leadId };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://lmp.bmw.ru/api/1/crm/lead", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе данных лида");
                throw;
            }
        }
    }
}
