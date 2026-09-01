namespace Scheduling.Contracts.Schedule;

public interface IScheduleAuditLogger
{
    void LogActivity(string action, string entityName, string userName, string message = "");
}
