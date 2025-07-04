using Domain.AttachedResources;
using Riok.Mapperly.Abstractions;
using Scheduling.Contracts.AttachedResources.DTOs;

namespace Application.AttachedResources;
[Mapper]
public static partial class ResourceMappingExtension
{
    public static ScheduleResourceDto ToDto(this ScheduleResourceMapping entity)
    {
        if (entity == null) return null;
        return new ScheduleResourceDto(entity.Id, entity.ScheduleId, entity.ResourceId, entity.ResourceType);
    }

    public static ScheduleResourceMapping ToDomain(this ScheduleResourceDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        // Create a new ScheduleResource entity
        var scheduleResource = new ScheduleResourceMapping(dto.ScheduleId, dto.ResourceId, dto.ResourceType);
        return scheduleResource;
    }

}