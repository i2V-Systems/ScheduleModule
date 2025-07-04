
using Domain.AttachedResources;
using Infrastructure.Schedule;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AttachedResources;

public class ScheduleResourceRepository :IScheduleResourceRepository
{
    private readonly ScheduleDbContext _context;
    private IScheduleResourceRepository _scheduleRepositoryImplementation;

    public ScheduleResourceRepository(ScheduleDbContext context)
    {
        _context = context;
    }
    public async Task<ScheduleResourceMapping?> GetByIdAsync(Guid id)
        => await _context.ScheduleResourceMapping.FindAsync(id);

    public async Task<IEnumerable<ScheduleResourceMapping>> GetAllAsync()
        => await _context.ScheduleResourceMapping.ToListAsync();

    public Task<IEnumerable<ScheduleResourceMapping>> SearchAsync(string searchTerm)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<ScheduleResourceMapping>> SearchAsync(Guid searchTerm)
        => await _context.ScheduleResourceMapping.Where(b => b.ScheduleId== searchTerm).ToListAsync();
    

    public async Task<ScheduleResourceMapping> AddAsync(ScheduleResourceMapping resourceMapping)
    {
        _context.ScheduleResourceMapping.Add(resourceMapping);
        await _context.SaveChangesAsync();
        return resourceMapping;
    }

    public async Task<ScheduleResourceMapping> UpdateAsync(ScheduleResourceMapping book)
    {
        _context.ScheduleResourceMapping.Update(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task DeleteAsync(Guid id)
    {
        var book = await _context.ScheduleResourceMapping.FindAsync(id);
        if (book != null)
        {
            _context.ScheduleResourceMapping.Remove(book);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
        => await _context.ScheduleResourceMapping.AnyAsync(b => b.Id == id);

  
}
