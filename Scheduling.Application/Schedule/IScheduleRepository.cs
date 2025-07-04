

using Scheduling.Contracts.Schedule.Enums;

namespace Application.Schedule;

public interface IScheduleRepository
{
    Task<Domain.Schedule.Schedule?> GetByIdAsync(Guid id);
    Task<IEnumerable<Domain.Schedule.Schedule>> GetAllAsync();
    Task<IEnumerable<Domain.Schedule.Schedule>> SearchAsync(string searchTerm);
    Task<IEnumerable<Domain.Schedule.Schedule>> GetByStatusAsync(ScheduleStatus status);
    Task<Domain.Schedule.Schedule> AddAsync(Domain.Schedule.Schedule Schedule);
    Task<Domain.Schedule.Schedule> UpdateAsync(Domain.Schedule.Schedule Schedule);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}