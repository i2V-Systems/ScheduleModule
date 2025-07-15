using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

namespace Application.Schedule.ScheduleEvent.Validator;

// Schedule validation interface
public interface IScheduleValidator
{
    ValidationResult ValidateSchedule(ScheduleDto schedule);
    ValidationResult ValidateTimeRange(DateTime currentTime, DateTime startTime, DateTime endTime);
}