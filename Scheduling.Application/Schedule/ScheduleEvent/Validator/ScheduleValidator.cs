using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleEvent.Validator;

// [TransientService]
public class ScheduleValidator : IScheduleValidator
{
    public ValidationResult ValidateSchedule(ScheduleDto schedule)
    {
        var errors = new List<string>();

        if (schedule == null)
            errors.Add("Schedule cannot be null");
        else
        {
            if (schedule.Id==Guid.Empty)
                errors.Add("Schedule ID is required");

            if (schedule.StartDateTime >= schedule.EndDateTime)
                errors.Add("Start date must be before end date");
        }

        return errors.Any() ? ValidationResult.Invalid(errors.ToArray()) : ValidationResult.Valid();
    }

    public ValidationResult ValidateTimeRange(DateTime currentTime, DateTime startTime, DateTime endTime)
    {
        if (currentTime < startTime)
            return ValidationResult.Invalid("Current time is before schedule start time");

        if (currentTime > endTime)
            return ValidationResult.Invalid("Current time is after schedule end time");

        return ValidationResult.Valid();
    }
}