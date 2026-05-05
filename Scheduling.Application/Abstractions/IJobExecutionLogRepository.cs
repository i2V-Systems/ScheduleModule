using Domain.Entities;

namespace Application.Abstractions;

public interface IJobExecutionLogRepository
{
    Task AddAsync(JobExecutionLog log, CancellationToken ct = default);
    Task<IReadOnlyList<JobExecutionLog>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<JobExecutionLog>> GetByJobNameAsync(string jobName, CancellationToken ct = default);

      Task<JobExecutionLog?> GetByIdAsync(Guid id, CancellationToken ct);
      Task UpdateAsync(JobExecutionLog log, CancellationToken ct);

}
