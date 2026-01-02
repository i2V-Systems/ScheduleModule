using CommonUtilityModule.CrudUtilities;

namespace Scheduling.Contracts;

public  interface INotificationManager
{
    Task  SendCrudDataToClientAsync(CrudMethodType method, Dictionary<string, dynamic> resources, List<string> skipUserIds = null, List<string> targetUserIds = null);
}
