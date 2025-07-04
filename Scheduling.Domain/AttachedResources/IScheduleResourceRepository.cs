
using Domain.AttachedResources;

namespace Domain.AttachedResources;
public interface IScheduleResourceRepository
{
    Task<ScheduleResourceMapping?> GetByIdAsync(Guid id);
    Task<IEnumerable<ScheduleResourceMapping>> GetAllAsync();
    Task<IEnumerable<ScheduleResourceMapping>> SearchAsync(string searchTerm);
    Task<ScheduleResourceMapping> AddAsync(ScheduleResourceMapping Schedule);
    Task<ScheduleResourceMapping> UpdateAsync(ScheduleResourceMapping Schedule);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}