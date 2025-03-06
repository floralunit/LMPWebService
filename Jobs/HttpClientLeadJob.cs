using Quartz;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LMPWebService.Services.Interfaces;

public class HttpClientLeadJob : IJob
{
    private readonly IHttpClientLeadService _httpClientLeadService;
    private readonly ILogger<HttpClientLeadJob> _logger;

    public HttpClientLeadJob(IHttpClientLeadService httpClientLeadService, ILogger<HttpClientLeadJob> logger)
    {
        _httpClientLeadService = httpClientLeadService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Выполняем HTTP-запрос для Lead API...");
        //var result = await _httpClientLeadService.GetLeadDataAsync();
        //_logger.LogInformation("Результат запроса: {Result}", result);
    }
}
