using System;
using System.Collections.Generic;

namespace LoggingModule.Enums
{
    public enum UserActivityTypeEnum
    {
        Get,
        Post,
        Put,
        Delete
    }
}

namespace LoggingModule.Managers
{
    public static class LoggingManager
    {
        public static void NotifyLogger<T>(Guid entityId, string tablename, Enums.UserActivityTypeEnum activityType, Guid userId)
        {
            // Stubbed
        }

        public static void NotifyLogger<T>(Guid entityId, string tablename, Enums.UserActivityTypeEnum activityType, Guid userId, IEnumerable<dynamic> prevEntity)
        {
            // Stubbed
        }

        public static IEnumerable<dynamic> getPreviousEntity(string tablename, string entityId)
        {
            return Array.Empty<dynamic>();
        }
    }
}

namespace CommonUtilityModule.Models
{
    public static class ExceptionHandler
    {
        public static void ThrowDatabaseExceptions<T>(Exception ex, Infrastructure.Schedule.OperationType opType)
        {
            throw ex;
        }
    }
}

namespace Infrastructure.Schedule
{
    public enum OperationType
    {
        Add,
        Update,
        Delete,
        Read
    }
}
