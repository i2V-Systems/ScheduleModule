
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.AttachedResources.DTOs;

public record ScheduleResourceDto(
    Guid Id, Guid ScheduleId, Guid ResourceId, Resources ResourceType,string? metaData);

public record DetachScheduleRequest(List<Guid> Ids, ScheduleAllDetails Schedule);

// public record AttachUpdateDto(
//     Guid ResourceId,
//     Resources ResourceType,
//     List<Guid> ScheduleIds
// );

