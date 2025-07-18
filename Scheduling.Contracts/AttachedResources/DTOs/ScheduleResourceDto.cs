
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.AttachedResources.DTOs;

public record ScheduleResourceDto(Guid ScheduleId, Guid ResourceId, Resources ResourceType);

public record DetachScheduleRequest(List<Guid> Ids, ScheduleAllDetails Schedule);
