using Domain.AttachedResources;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public DbSet<JobExecutionLog> JobExecutionLogs { get; set; }

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

            // Value comparer for StartDays collection
            var startDaysComparer = new ValueComparer<List<Days>>(
                (c1, c2) => JsonConvert.SerializeObject(c1) == JsonConvert.SerializeObject(c2),
                c => c == null ? 0 : JsonConvert.SerializeObject(c).GetHashCode(),
                c => JsonConvert.DeserializeObject<List<Days>>(JsonConvert.SerializeObject(c)) ?? new List<Days>());

            modelBuilder.Entity<Domain.Schedule.Schedule>()
                .Property(e => e.StartDays)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v ?? new List<Days>()),
                    v => string.IsNullOrEmpty(v) ? new List<Days>() : JsonConvert.DeserializeObject<List<Days>>(v) ?? new List<Days>()
                )
                .Metadata.SetValueComparer(startDaysComparer);
            modelBuilder.Entity<ScheduleResourceMapping>()
                .Property(e => e.ResourceType)
                .HasConversion(new EnumToStringConverter<Resources>());

            modelBuilder
                .Entity<ScheduleResourceMapping>()
                .HasKey(pvs =>   pvs.Id);

            modelBuilder.Entity<JobExecutionLog>()
                .Property(e => e.Status)
                .HasConversion(new EnumToStringConverter<JobExecutionStatus>());

            modelBuilder.Entity<JobExecutionLog>(entity =>
            {
              entity.ToTable("job_execution_logs", schema: "scheduler");

              entity.Property(e => e.Id).HasColumnName("id");
              entity.Property(e => e.JobName).HasColumnName("job_name");
              entity.Property(e => e.JobGroup).HasColumnName("job_group");
              entity.Property(e => e.TriggerName).HasColumnName("trigger_name");
              entity.Property(e => e.TriggerGroup).HasColumnName("trigger_group");
              entity.Property(e => e.TriggerDescription).HasColumnName("trigger_description");
              entity.Property(e => e.FiredAt).HasColumnName("fired_at");
              entity.Property(e => e.ScheduledFireTime).HasColumnName("scheduled_fire_time");
              entity.Property(e => e.NextFireTime).HasColumnName("next_fire_time");
              entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
              entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion(new EnumToStringConverter<JobExecutionStatus>());
              entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
              entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            });

            base.OnModelCreating(modelBuilder);
        }
    }

}
