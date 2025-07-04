using Scheduling.Contracts.AttachedResources.Enums;

namespace Domain.AttachedResources;


public class ScheduleResourceMapping :BaseEntity
{
    public Guid ScheduleId { get; private set; }
    public Guid ResourceId { get; private set; }
    
    public Resources ResourceType { get; private set; }


    public ScheduleResourceMapping(Guid resId, Guid schId, Resources type)
    {
        this.ScheduleId = schId;
        this.ResourceId = resId;
        this.ResourceType = type;
    }
}