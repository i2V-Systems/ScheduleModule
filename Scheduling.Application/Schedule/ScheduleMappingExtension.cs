using Application.AttachedResources;
using Domain.AttachedResources;
using Riok.Mapperly.Abstractions;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.Schedule;

[Mapper]
public static partial class ScheduleMappingExtension
{
    #region Schedule Mappings

    public static partial ScheduleDto ToDto(this Domain.Schedule.Schedule entity);
   

    public static partial Domain.Schedule.Schedule ToDomain(this ScheduleDto dto);

    public static void UpdateFromDto(this Domain.Schedule.Schedule entity, ScheduleDto dto)
    {
        // Call the UpdateDetails method on the existing entity
        entity.UpdateDetails(
            dto.Name,
            dto.Type,
            dto.SubType,
            dto.Details,
            dto.NoOfDays,
            dto.StartDays , 
            dto.StartDateTime,
            dto.EndDateTime,
            dto.RecurringTime
        );
        // Update status if provided
        if (dto.Status != null)
        {
            entity.UpdateStatus(dto.Status);
        }
    }

    #endregion
    

    #region Batch Operations

    public static IEnumerable<ScheduleDto> ToDto(this IEnumerable<Domain.Schedule.Schedule> entities)
    {
        return entities?.Select(e => e.ToDto()) ?? Enumerable.Empty<ScheduleDto>();
    }

    public static IEnumerable<ScheduleResourceDto> ToDto(this IEnumerable<ScheduleResourceMapping> entities)
    {
        return entities.Select(e => e.ToDto()) ?? Enumerable.Empty<ScheduleResourceDto>();
    }

    public static IEnumerable<Domain.Schedule.Schedule> ToDomain(this IEnumerable<ScheduleDto> dtos)
    {
        return dtos?.Select(dto => dto.ToDomain()) ?? Enumerable.Empty<Domain.Schedule.Schedule>();
    }

    public static IEnumerable<ScheduleResourceMapping> ToDomain(this IEnumerable<ScheduleResourceDto> dtos)
    {
        return dtos?.Select(dto => dto.ToDomain()) ?? Enumerable.Empty<ScheduleResourceMapping>();
    }

    #endregion
}