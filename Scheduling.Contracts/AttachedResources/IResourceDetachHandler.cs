using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;

namespace Scheduling.Contracts.AttachedResources;

public interface IResourceDetachHandler
{
  Resources ResourceType { get; }
  Task OnDetachedAsync(ScheduleResourceDto resource);
}
