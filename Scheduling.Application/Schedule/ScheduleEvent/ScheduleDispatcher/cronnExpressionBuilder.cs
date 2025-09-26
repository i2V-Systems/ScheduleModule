
using Scheduling.Contracts.Schedule.Enums;

namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher
{

    public static class CronExpressionBuilder
    {
        public static string BuildCronExpression(List<Days> selectedDays, DateTime timeString)
        {
            // Convert days to cron format
            var cronDays = ConvertDaysToCron(selectedDays);
            var cronExpression = $"0 {timeString.Minute} {timeString.Hour} ? * {cronDays}";
            // Validate the expression
            try
            {
                Quartz.CronExpression.ValidateExpression(cronExpression);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Generated invalid cron expression: {cronExpression}", ex);
            }
            return cronExpression;
        }

        private static string ConvertDaysToCron(List<Days> selectedDays)
        {
            if (selectedDays == null || selectedDays.Count == 0)
            {
                throw new ArgumentException("No days selected");
            }

            // Sort and remove duplicates
            var cronDayStrings = selectedDays.Distinct().Select(ConvertDayEnumToCronDay).OrderBy(x => x);
            // Join with commas for multiple days
            return string.Join(",", cronDayStrings);
        }
        private static string ConvertDayEnumToCronDay(Days day)
        {
            // Assuming your Days enum maps like this - adjust based on your actual enum
            return day switch
            {
                Days.Sunday => "SUN",
                Days.Monday => "MON",
                Days.Tuesday => "TUE", 
                Days.Wednesday => "WED",
                Days.Thursday => "THU",
                Days.Friday => "FRI",
                Days.Saturday => "SAT",
                _ => throw new ArgumentException($"Invalid day: {day}")
            };
        }
        private static int ConvertDayEnumToNumber(Days day)
        {
            return day switch
            {
                Days.Sunday => 0,
                Days.Monday => 1,
                Days.Tuesday => 2,
                Days.Wednesday => 3,
                Days.Thursday => 4,
                Days.Friday => 5,
                Days.Saturday => 6,
                _ => throw new ArgumentException($"Invalid day: {day}")
            };
        }
        
        public static string BuildDailyCronExpression(DateTime time)
        {
            return $"0 {time.Minute} {time.Hour} * * ?";
        }

        public static string BuildWeekdaysCronExpression(DateTime time)
        {
            return $"0 {time.Minute} {time.Hour} ? * MON-FRI"; // Monday to Friday
        }

        public static string BuildWeekendsCronExpression(DateTime time)
        {
            return $"0 {time.Minute} {time.Hour} ? * SUN,SAT"; // Sunday and Saturday
        }

        public static string BuildMonthlyCronExpression(DateTime time, int dayOfMonth)
        {
            return $"0 {time.Minute} {time.Hour} {dayOfMonth} * ?";
        }
    }
}
    