namespace FreshTrack;

public interface IReminderService
{
    void SetReminder(DateTime triggerTime, string title, string message, int listId = 0);
    Task<bool> RequestNotificationPermissionAsync();
}