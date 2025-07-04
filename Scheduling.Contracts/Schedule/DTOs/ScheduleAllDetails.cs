namespace Scheduling.Contracts.Schedule.DTOs
{ 
    public class ScheduleAllDetails
    {
        public ScheduleDto schedules { get; set; }

        public HashSet<string> AttachedResources { get; set; } = new HashSet<string>();
        
    }
    
}


  
