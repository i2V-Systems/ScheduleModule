using System.Diagnostics;
using Newtonsoft.Json;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;

namespace Domain.AttachedResources;


public class ScheduleResourceMapping :BaseEntity
{

    public Guid ScheduleId { get; set; }
    public string ResourceId { get; set; } = string.Empty;

    public Resources ResourceType { get; set; }

    public string? metaData { get; set; }

    public ScheduleResourceMapping()
    {
    }

    public ScheduleResourceMapping(string resId, Guid schId, Resources type, string? data)
    {
        ScheduleId = schId;
        ResourceId = resId;
        ResourceType = type;
        metaData = data;
    }
}
