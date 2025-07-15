namespace Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

public class ScheduleResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> ScheduledJobIds { get; init; } = new List<string>();
    public Exception? Exception { get; init; }

    public static ScheduleResult Success(IReadOnlyList<string> jobIds) =>
        new() { IsSuccess = true, ScheduledJobIds = jobIds };

    public static ScheduleResult Failure(string errorMessage, Exception? exception = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}