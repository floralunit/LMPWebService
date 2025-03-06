using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using MassTransit;
using LMPWebService.Consumers;

namespace LMPWebService.Extensions
{
    public static class QuartzExtension
    {
        public static IServiceCollection AddQuartzJobs(this IServiceCollection serviceCollection)
        {
            var jobKey = new JobKey("HttpClientLeadJob");

            serviceCollection.AddQuartz(q =>
            {
                q.AddJob<HttpClientLeadJob>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("HttpClientLeadTrigger")
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever()));
            });

            serviceCollection.AddQuartzHostedService();
            return serviceCollection;
        }
    }
}