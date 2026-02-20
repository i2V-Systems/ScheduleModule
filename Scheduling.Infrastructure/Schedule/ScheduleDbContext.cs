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

        public DbSet<Domain.Scheduling.Schedule> Schedule { get; set; }
        public DbSet<ScheduleResourceMapping> ScheduleResourceMapping { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder.Entity<Domain.Schedule.Schedule>()
            //     .Property(e => e.Type)
            //     .HasConversion(
            //         new EnumToStringConverter<ScheduleType>()
            //     );

            // Configure the SubType property to convert Enum_ScheduleSubType? to string in the database
            // modelBuilder.Entity<Domain.Schedule.Schedule>()
            //     .Property(e => e.SubType)
            //     .HasConversion(
            //         new EnumToStringConverter<ScheduleSubType>()
            //     );

            // modelBuilder.Entity<Domain.Schedule.Schedule>()
            //     .Property(e => e.Status)
            //     .HasConversion(new EnumToStringConverter<ScheduleStatus>());

            modelBuilder.Entity<Domain.Scheduling.Schedule>()
                .Property(schedule => schedule.StartDays)
                .HasConversion(
                    value => JsonConvert.SerializeObject(value ?? new List<Days>()),
                    value => string.IsNullOrEmpty(value) ? new List<Days>() : JsonConvert.DeserializeObject<List<Days>>(value) ?? new List<Days>()
                );

            EnumToStringConverter<Resources> enumToStringConverter = new EnumToStringConverter<Resources>();
            modelBuilder.Entity<ScheduleResourceMapping>()
                .Property(scheduleResourceMapping => scheduleResourceMapping.ResourceType)
                .HasConversion(enumToStringConverter);

            modelBuilder
                .Entity<ScheduleResourceMapping>()
                .HasKey(pvs =>   pvs.Id);

            base.OnModelCreating(modelBuilder);
        }
    }

}
