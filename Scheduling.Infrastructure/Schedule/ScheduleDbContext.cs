using Domain.AttachedResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.Enums;

namespace Infrastructure.Schedule
{
    public class ScheduleDbContext : DbContext
    {
        public ScheduleDbContext(DbContextOptions<ScheduleDbContext> options)
            : base(options)
        {
        }
        //public DbSet<ActionData> ActionData { get; set; }

        public DbSet<Domain.Schedule.Schedule> Schedule { get; set; }
        public DbSet<ScheduleResourceMapping> ScheduleResourceMapping { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Domain.Schedule.Schedule>()
                .Property(e => e.Type)
                .HasConversion(
                    new EnumToStringConverter<ScheduleType>()
                );

            // Configure the SubType property to convert Enum_ScheduleSubType? to string in the database
            modelBuilder.Entity<Domain.Schedule.Schedule>()
                .Property(e => e.SubType)
                .HasConversion(
                    new EnumToStringConverter<ScheduleSubType>()
                );
            
            modelBuilder.Entity<Domain.Schedule.Schedule>()
                .Property(e => e.Status)
                .HasConversion(new EnumToStringConverter<ScheduleStatus>());
            
            modelBuilder.Entity<Domain.Schedule.Schedule>()
                .Property(e => e.StartDays)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v ?? new List<Days>()),
                    v => string.IsNullOrEmpty(v) ? new List<Days>() : JsonConvert.DeserializeObject<List<Days>>(v) ?? new List<Days>()
                );
            modelBuilder.Entity<ScheduleResourceMapping>()
                .Property(e => e.ResourceType)
                .HasConversion(new EnumToStringConverter<Resources>());
            
            modelBuilder
                .Entity<ScheduleResourceMapping>()
                .HasKey(pvs => new { pvs.ScheduleId, pvs.ResourceId });

            base.OnModelCreating(modelBuilder);
        }
    }

}
