using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Schedule;

internal class JobExecutionLogRepository : IJobExecutionLogRepository
{
    private readonly ScheduleDbContext _context;

    public JobExecutionLogRepository(ScheduleDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(JobExecutionLog log, CancellationToken ct = default)
    {
        await _context.JobExecutionLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<JobExecutionLog>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.JobExecutionLogs
            .OrderByDescending(x => x.FiredAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JobExecutionLog>> GetByJobNameAsync(string jobName, CancellationToken ct = default)
    {
        return await _context.JobExecutionLogs
            .Where(x => x.JobName == jobName)
            .OrderByDescending(x => x.FiredAt)
            .ToListAsync(ct);
    }
}
