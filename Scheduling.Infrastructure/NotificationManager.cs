using CommonUtilityModule.CrudUtilities;
using CommonUtilityModule.Manager;
using Scheduling.Contracts;

namespace Infrastructure;

internal class NotificationManager: INotificationManager
{
    private readonly ICrudNotifier _crudNotifier;

    public NotificationManager(ICrudNotifier crudNotifier)
    {
        _crudNotifier = crudNotifier;
    }

    public async Task SendCrudDataToClientAsync(CrudMethodType method, Dictionary<string, dynamic> resources,
      List<string> skipUserIds = null,
      List<string> targetUserIds = null)
    {
        await _crudNotifier.SendCrudDataToClient(
          CrudRelatedEntity.Schedule,
          method,
          resources,
          skipUserIds,
          targetUserIds
        );
    }
}
