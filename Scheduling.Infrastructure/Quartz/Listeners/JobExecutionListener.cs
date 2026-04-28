using Application.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.Quartz.Listeners;

public class JobExecutionListener : IJobListener
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobExecutionListener> _logger;

    public string Name => "JobExecutionListener";

    public JobExecutionListener(IServiceScopeFactory scopeFactory, ILogger<JobExecutionListener> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct = default)
    {
        // store fired time in context for duration calc later
        context.Put("firedAt", DateTime.UtcNow);

        await SaveLogAsync(new JobExecutionLog
        {
            Id = Guid.NewGuid(),
            JobName = context.JobDetail.Key.Name,
            JobGroup = context.JobDetail.Key.Group,
            FiredAt = DateTime.UtcNow,
            Status = JobExecutionStatus.Started
        }, ct);
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken ct = default)
    {
        var firedAt = (DateTime)context.Get("firedAt");
        var duration = (long)(DateTime.UtcNow - firedAt).TotalMilliseconds;

        var log = new JobExecutionLog
        {
            Id = Guid.NewGuid(),
            JobName = context.JobDetail.Key.Name,
            JobGroup = context.JobDetail.Key.Group,
            FiredAt = firedAt,
            CompletedAt = DateTime.UtcNow,
            DurationMs = duration,
            Status = jobException is null ? JobExecutionStatus.Completed : JobExecutionStatus.Failed,
            ErrorMessage = jobException?.Message
        };

        await SaveLogAsync(log, ct);
        _logger.LogInformation("Job {JobName} {Status} in {Duration}ms", log.JobName, log.Status, duration);
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    // IJobListener is a singleton — use scope to resolve scoped DbContext
    private async Task SaveLogAsync(JobExecutionLog log, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IJobExecutionLogRepository>();
        await repo.AddAsync(log, ct);
    }
}
