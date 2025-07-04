using Application.Schedule;
using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts.Schedule.Enums;

namespace Infrastructure.Schedule;
public class ScheduleRepository :IScheduleRepository
{
    private readonly ScheduleDbContext _context;
    private IScheduleRepository _scheduleRepositoryImplementation;

    public ScheduleRepository(ScheduleDbContext context)
    {
        _context = context;
    }
    public async Task<Domain.Schedule.Schedule?> GetByIdAsync(Guid id)
        => await _context.Schedules.FindAsync(id);

    public async Task<IEnumerable<Domain.Schedule.Schedule>> GetAllAsync()
        => await _context.Schedules.ToListAsync();

    public async Task<IEnumerable<Domain.Schedule.Schedule>> SearchAsync(string searchTerm)
        => await _context.Schedules.Where(b => b.Name.Contains(searchTerm) || b.Details.Contains(searchTerm)).ToListAsync();

    public async Task<IEnumerable<Domain.Schedule.Schedule>> GetByStatusAsync(ScheduleStatus status)
        => await _context.Schedules.Where(b => b.Status == status).ToListAsync();

    public async Task<Domain.Schedule.Schedule> AddAsync(Domain.Schedule.Schedule book)
    {
        _context.Schedules.Add(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<Domain.Schedule.Schedule> UpdateAsync(Domain.Schedule.Schedule book)
    {
        _context.Schedules.Update(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task DeleteAsync(Guid id)
    {
        var book = await _context.Schedules.FindAsync(id);
        if (book != null)
        {
            _context.Schedules.Remove(book);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await _context.Schedules.AnyAsync(b => b.Id == id);

  
}

