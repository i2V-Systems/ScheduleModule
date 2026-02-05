using CommonUtilityModule.CrudUtilities;
using CommonUtilityModule.Manager;
using Scheduling.Contracts;

namespace Infrastructure;

internal class NotificationManager: INotificationManager
{
    public async Task SendCrudDataToClientAsync(CrudMethodType method, Dictionary<string, dynamic> resources,
      List<string>? skipUserIds = null,
      List<string>? targetUserIds = null)
    {
        await CrudManager.SendCrudDataToClient(
          CrudRelatedEntity.Schedule,
          method,
          resources,
          skipUserIds,
          targetUserIds
        );
    }
}
