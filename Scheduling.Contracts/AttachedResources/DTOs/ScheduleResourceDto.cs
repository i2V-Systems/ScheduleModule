
using Scheduling.Contracts.AttachedResources.Enums;

namespace Scheduling.Contracts.AttachedResources.DTOs;

public record ScheduleResourceDto(Guid ScheduleId, Guid ResourceId, Resources ResourceType);
