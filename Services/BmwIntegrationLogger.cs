using LMPWebService.Models;
using System.Text.Json;

public interface IBmwIntegrationLogger
{
    Task LogIncomingRequestAsync(string operationType, string leadId, string outletCode, string requestBody);
    Task LogApiCallAsync(string operationType, string leadId, string outletCode, string requestUrl,
        string requestBody, string responseBody, int? responseStatus, bool isSuccess,
        string errorMessage, TimeSpan? duration);
    Task LogOperationAsync(string operationType, string leadId, string outletCode,
        object requestData, object responseData, bool isSuccess, string errorMessage);
    Guid GenerateCorrelationId();
}

public class BmwIntegrationLogger : IBmwIntegrationLogger
{
    private readonly AstraDbContext _dbContext;
    private readonly ILogger<BmwIntegrationLogger> _logger;

    public BmwIntegrationLogger(AstraDbContext dbContext, ILogger<BmwIntegrationLogger> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Guid GenerateCorrelationId() => Guid.NewGuid();

    public async Task LogIncomingRequestAsync(string operationType, string leadId, string outletCode, string requestBody)
    {
        try
        {
            var log = new BMWIntegrationLogs
            {
                LogDate = DateTime.UtcNow,
                OperationType = operationType,
                LeadId = leadId,
                OutletCode = outletCode,
                RequestBody = requestBody,
                CorrelationId = GenerateCorrelationId()
            };

            await _dbContext.BMWIntegrationLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при логировании входящего запроса");
        }
    }

    public async Task LogApiCallAsync(string operationType, string leadId, string outletCode,
        string requestUrl, string requestBody, string responseBody, int? responseStatus,
        bool isSuccess, string errorMessage, TimeSpan? duration = null)
    {
        try
        {
            var log = new BMWIntegrationLogs
            {
                LogDate = DateTime.UtcNow,
                OperationType = operationType,
                LeadId = leadId,
                OutletCode = outletCode,
                RequestUrl = requestUrl,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                ResponseStatus = responseStatus,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                ProcessingTimeMs = duration.HasValue ? (int)duration.Value.TotalMilliseconds : null,
                CorrelationId = GenerateCorrelationId()
            };

            await _dbContext.BMWIntegrationLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при логировании вызова API");
        }
    }

    public async Task LogOperationAsync(string operationType, string leadId, string outletCode,
        object requestData, object responseData, bool isSuccess, string errorMessage)
    {
        try
        {
            var log = new BMWIntegrationLogs
            {
                LogDate = DateTime.UtcNow,
                OperationType = operationType,
                LeadId = leadId,
                OutletCode = outletCode,
                RequestBody = requestData != null ? JsonSerializer.Serialize(requestData) : null,
                ResponseBody = responseData != null ? JsonSerializer.Serialize(responseData) : null,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CorrelationId = GenerateCorrelationId()
            };

            await _dbContext.BMWIntegrationLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при логировании операции");
        }
    }
}