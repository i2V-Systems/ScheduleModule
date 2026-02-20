using Common.Infrastructure.Data.Factories;

namespace Infrastructure.Schedule;

public class ScheduleDbContextFactory : DbContextFactoryBase<ScheduleDbContext>
{
    protected override string MigrationsAssembly => "Scheduling.Infrastructure";
    protected override string MigrationHistorySchema => Schemas.Schedule;
}
