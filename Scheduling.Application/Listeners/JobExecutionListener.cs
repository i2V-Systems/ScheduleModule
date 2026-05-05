using Application.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Application.Listeners;

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
        var logId = Guid.NewGuid();
        var firedAt = DateTime.UtcNow;
        await SaveLogAsync(new JobExecutionLog
        {
          Id = logId,
          JobName = context.JobDetail.Key.Name,
          JobGroup = context.JobDetail.Key.Group,
          TriggerName = context.Trigger.Key.Name,
          TriggerGroup = context.Trigger.Key.Group,
          TriggerDescription = context.Trigger.Description,
          ScheduledFireTime = context.ScheduledFireTimeUtc?.UtcDateTime,
          NextFireTime = context.NextFireTimeUtc?.UtcDateTime,
          FiredAt = firedAt,
          Status = JobExecutionStatus.Started
        }, ct);
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken ct = default)
    {
      var logId = (Guid)context.Get("logId");
      var firedAt = (DateTime)context.Get("firedAt");
      var completedAt = DateTime.UtcNow;
      var duration = (long)(completedAt - firedAt).TotalMilliseconds;

      await using var scope = _scopeFactory.CreateAsyncScope();
      var repo = scope.ServiceProvider.GetRequiredService<IJobExecutionLogRepository>();

      var log = await repo.GetByIdAsync(logId, ct);   // fetch the Started row
      if (log is not null)
      {
        log.CompletedAt = completedAt;
        log.DurationMs = duration;
        log.Status = jobException is null ? JobExecutionStatus.Completed : JobExecutionStatus.Failed;
        log.ErrorMessage = jobException?.Message;
        await repo.UpdateAsync(log, ct);
      }

      _logger.LogInformation("Job {JobName} {Status} in {Duration}ms",
        context.JobDetail.Key.Name,
        jobException is null ? JobExecutionStatus.Completed : JobExecutionStatus.Failed,
        duration);
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
