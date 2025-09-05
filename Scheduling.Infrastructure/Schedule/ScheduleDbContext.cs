using Domain.AttachedResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
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
            base.OnModelCreating(modelBuilder);

            // Configure Schedule entity
            modelBuilder.Entity<Domain.Schedule.Schedule>(entity =>
            {
                entity.Property(e => e.Type)
                    .IsRequired();
            
                entity.Property(e => e.SubType)
                    .IsRequired();
            
                entity.Property(e => e.Status)
                    .IsRequired();
            
                entity.Property(e => e.StartDays)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v ?? new List<Days>()),
                        v => string.IsNullOrEmpty(v)
                            ? new List<Days>()
                            : JsonConvert.DeserializeObject<List<Days>>(v) ?? new List<Days>()
                    ) .Metadata.SetValueComparer(new ValueComparer<List<Days>>(
                        (c1, c2) => c1.SequenceEqual(c2), // Equality check
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), // Hash code
                        c => c.ToList() // Snapshot for EF tracking
                    ));
            });
            
            // Configure ScheduleResourceMapping entity
            modelBuilder.Entity<ScheduleResourceMapping>(entity =>
            {
                entity.Property(e => e.ResourceType)
                    .IsRequired();
            
                // Uncomment if composite key is needed
                // entity.HasKey(pvs => new { pvs.ScheduleId, pvs.ResourceId });
            });
        }
    }
}