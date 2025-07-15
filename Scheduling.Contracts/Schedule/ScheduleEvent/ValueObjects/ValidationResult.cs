namespace Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

public class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = new List<string>();

    public static ValidationResult Valid() => new() { IsValid = true };
    
    public static ValidationResult Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}