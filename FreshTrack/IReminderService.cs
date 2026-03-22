using System;
using System.Threading.Tasks;

namespace FreshTrack; // 命名空间必须和ReminderService的外层命名空间一致

public interface IReminderService
{
    // 方法签名要和ReminderService完全匹配（参数、返回值、名称）
    void SetReminder(DateTime triggerTime, string title, string message, int listId = 0);
    Task<bool> RequestNotificationPermissionAsync();
}