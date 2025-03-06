namespace LMPWebService.Services.Interfaces
{
    public interface IMessageQueueService
    {
        Task SendMessageAsync(string queueName, string message);
    }
}
