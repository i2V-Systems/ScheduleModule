
using Scheduling.Contracts.Schedule.Enums;

namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher
{

    public class CronExpressionBuilder
    {
        public static string BuildCronExpression(List<Days> selectedDays, DateTime timeString)
        {
            // Convert days to cron format
            var cronDays = ConvertDaysToCron(selectedDays);
        
            // Build cron expression: minute hour day month dayOfWeek
            return $"{timeString.Minute} {timeString.Hour} * * {cronDays}";
        }
        
        private static string ConvertDaysToCron(List<Days>selectedDays)
        {
            if (selectedDays == null || selectedDays.Count == 0)
            {
                throw new ArgumentException("No days selected");
            }
         
            // Sort and remove duplicates
            var cronDayNumbers = selectedDays.Distinct().OrderBy(x => x).ToList();
            // Join with commas for multiple days
            return string.Join(",", cronDayNumbers);
        }
    }
}
    