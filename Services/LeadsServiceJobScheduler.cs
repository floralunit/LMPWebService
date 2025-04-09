using LeadsSaver_RabbitMQ.Jobs;
using LMPWebService.Jobs;
using Quartz;

public class LeadsServiceJobScheduler : IHostedService
{
    private readonly IScheduler _scheduler;

    public LeadsServiceJobScheduler(
        IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _scheduler.Start(cancellationToken);

        var job2 = JobBuilder.Create<CheckErrorLeadsJob>()
            .WithIdentity($"checkErrorLeadsJob", "groupCheckErrorLeads")
            .UsingJobData("test", "test")
            .Build();

        var trigger2 = TriggerBuilder.Create()
            .WithIdentity($"checkErrorLeadsTrigger", "groupCheckErrorLeads")
            .StartNow()
            //.WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMinutes(35)).RepeatForever())
            .WithCronSchedule("0 0/35 6-21 ? * * *", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"))) // Запуск каждые 35 минут с 6 утра до 9 вечера
            .Build();

        await _scheduler.ScheduleJob(job2, trigger2, cancellationToken);

        var job1 = JobBuilder.Create<CheckResponsibleJob>()
            .WithIdentity($"checkResponsibleJob", "groupCheckResponsibleJob")
            .UsingJobData("test", "test")
            .Build();

        var trigger1 = TriggerBuilder.Create()
            .WithIdentity($"checkResponsibleJob", "groupCheckResponsibleJob")
            .StartNow()
            //.WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMinutes(30)).RepeatForever())
            .WithCronSchedule("0 0 6-21 ? * * *", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")))
            .Build();

        await _scheduler.ScheduleJob(job1, trigger1, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
    }
}
