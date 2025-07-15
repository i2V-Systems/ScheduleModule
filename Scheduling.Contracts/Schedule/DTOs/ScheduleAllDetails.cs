using Scheduling.Contracts.AttachedResources.DTOs;

namespace Scheduling.Contracts.Schedule.DTOs
{ 
    public class ScheduleAllDetails
    {
        public ScheduleDto schedules { get; set; }

        public List<ScheduleResourceDto>? AttachedResources { get; set; } = new List<ScheduleResourceDto>();

    }
    
}


  
