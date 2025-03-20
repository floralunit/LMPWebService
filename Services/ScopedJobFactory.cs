using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;

namespace LMPWebService.Services
{
    public class ScopedJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ScopedJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            // Создаем новую область (scope)
            var scope = _serviceProvider.CreateScope();

            try
            {
                // Разрешаем задачу из области
                var job = scope.ServiceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
                return new ScopedJob(scope, job);
            }
            catch
            {
                // Если что-то пошло не так, освобождаем область
                scope.Dispose();
                throw;
            }
        }

        public void ReturnJob(IJob job)
        {
            // Освобождаем область после выполнения задачи
            if (job is ScopedJob scopedJob)
            {
                scopedJob.Scope.Dispose();
            }
        }

        private class ScopedJob : IJob
        {
            public IServiceScope Scope { get; }
            private readonly IJob _innerJob;

            public ScopedJob(IServiceScope scope, IJob innerJob)
            {
                Scope = scope;
                _innerJob = innerJob;
            }

            public Task Execute(IJobExecutionContext context)
            {
                return _innerJob.Execute(context);
            }
        }
    }
}
