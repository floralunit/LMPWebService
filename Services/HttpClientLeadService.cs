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

                var token = await GetAccessTokenAsync(outlet_code);
                if (token == null)
                {
                    _logger.LogError("Токен пустой");
                    return null;
                }
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.token_type, token.access_token);

                //var response = await _httpClient.PostAsync($"{_authSettings.BaseUrl}/v1/crm/lead", content);
                //response.EnsureSuccessStatusCode();

                //var result = await response.Content.ReadAsStringAsync();

                string result = @"
                {
                    ""error"": null,
                    ""lead_info"": {
                        ""lead_id"": ""58007b35-bd36-45d1-8158-7f74e22c8529"",
                        ""type_id"": 2,
                        ""type_name"": ""RFO"",
                        ""temperature_id"": 2,
                        ""temperature_name"": ""Горячий"",
                        ""source_id"": 2,
                        ""source_name"": ""DWS"",
                        ""custom_properties_groups"": [
                            {
                                ""name"": ""Другое"",
                                ""sequence"": 100,
                                ""custom_properties"": [
                                    {
                                        ""code"": ""external_id"",
                                        ""name"": ""external_id"",
                                        ""type"": 3,
                                        ""type_name"": ""Int"",
                                        ""value"": ""10002"",
                                        ""sequence"": 2
                                    },
                                    {
                                        ""code"": ""Комменатрий"",
                                        ""name"": ""Комменатрий"",
                                        ""type"": 1,
                                        ""type_name"": ""String"",
                                        ""value"": ""комментарий"",
                                        ""sequence"": 3
                                    },
                                    {
                                        ""code"": ""flow_id"",
                                        ""name"": ""flow_id"",
                                        ""type"": 3,
                                        ""type_name"": ""Int"",
                                        ""value"": ""1"",
                                        ""sequence"": 1
                                    }
                                ]
                            }
                        ],
                        ""contact"": {
                            ""first_name"": ""Иван"",
                            ""last_name"": ""Иванов"",
                            ""middle_name"": """",
                            ""contect_phone"": ""+79441111234"",
                            ""email"": ""test@test.com"",
                            ""address"": null,
                            ""brand"": null,
                            ""car_model"": """",
                            ""car_model_year"": null,
                            ""gender"": null,
                            ""date_of_birth"": null,
                            ""vin"": null
                        },
                        ""status_id"": 6,
                        ""status_name"": ""ДЦ - Передан"",
                        ""dealer_status_id"": 0,
                        ""dealer_status_name"": ""Новый"",
                        ""dealer_status_crm_name"": null,
                        ""public_id"": 34511,
                        ""expiration_datetime"": null,
                        ""dealer_call_date"": null,
                        ""dealer_comment"": null,
                        ""dealer_receive_date"": ""2021-06-07T13:25:00.367"",
                        ""dealer_refuse_comment"": null,
                        ""client_dms_id"": null,
                        ""dealer_refuse_reason_id"": null,
                        ""dealer_visit_date"": null,
                        ""dealer_visit_planned_date"": null,
                        ""dealer_recall_date"": null,
                        ""visit_planned_date"": null,
                        ""lead_group_id"": ""255313f3-7761-42c2-8b11-2d35b411009b"",
                        ""dealer_refuse_reason_name"": null,
                        ""qulification_required"": true,
                        ""responsible_user"": null,
                        ""source_channel_detail"": ""DWS RFO stock"",
                        ""communication_target"": ""Продажа новых автомобилей/мотоциклов"",
                        ""source_campaign"": ""test_campaign_name""
                    }
                }";


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при запросе данных лида {leadId}");
                throw;
            }
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string outlet_code)
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

        /// <summary>
        /// Модель для десериализации ответа с токеном.
        /// </summary>
        public class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }

    }
}
