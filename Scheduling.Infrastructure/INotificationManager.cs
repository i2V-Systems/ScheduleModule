using CommonUtilityModule.CrudUtilities;

namespace Infrastructure;

public  interface INotificationManager
{
    Task  SendCrudDataToClientAsync(CrudMethodType method, Dictionary<string, dynamic> resources, List<string> skipUserIds = null, List<string> targetUserIds = null);
}
