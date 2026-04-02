using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FreshTrack;
using Microsoft.Maui.ApplicationModel;

#pragma warning disable CA1416
#pragma warning disable CS0618

namespace FreshTrack.Platforms.Android;

public class ReminderService : IReminderService
{
    private const string ReminderAction = "com.freshtrack.REMINDER_ACTION";
    private const int NotificationPermissionRequestCode = 1001;

    public void SetReminder(DateTime triggerTime, string title, string message, int listId = 0)
    {
        NotificationHelper.CreateNotificationChannel();

        var context = global::Android.App.Application.Context;
        if (context is null)
        {
            return;
        }

        var pendingIntent = CreatePendingIntent(context, title, message, listId);
        if (pendingIntent is null)
        {
            return;
        }

        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        var localTime = triggerTime.Kind == DateTimeKind.Utc
            ? triggerTime.ToLocalTime()
            : triggerTime;
        var triggerTimeMs = ToUnixTimeMilliseconds(localTime);

        try
        {
            ScheduleAlarm(alarmManager, triggerTimeMs, pendingIntent);
            System.Diagnostics.Debug.WriteLine("Reminder scheduled successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to schedule reminder: {ex.Message}");
        }
    }

    public async Task<bool> RequestNotificationPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return true;
        }

        var context = global::Android.App.Application.Context;
        if (context is null)
        {
            return false;
        }

        var notificationPermission = global::Android.Manifest.Permission.PostNotifications;
        if (HasPermission(context, notificationPermission))
        {
            return true;
        }

        var currentActivity = Platform.CurrentActivity;
        if (currentActivity is null)
        {
            return false;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ActivityCompat.RequestPermissions(
                currentActivity,
                new[] { notificationPermission },
                NotificationPermissionRequestCode);
        });

        await Task.Delay(1500);
        return HasPermission(context, notificationPermission);
    }

    private static PendingIntent? CreatePendingIntent(Context context, string title, string message, int listId)
    {
        try
        {
            var intent = new Intent(context, typeof(ReminderBroadcastReceiver));
            intent.SetAction(ReminderAction);
            intent.PutExtra("Title", title);
            intent.PutExtra("Message", message);
            intent.PutExtra("NotificationId", listId);

            return PendingIntent.GetBroadcast(
                context,
                listId,
                intent,
                GetPendingIntentFlags());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create pending intent: {ex.Message}");
            return null;
        }
    }

    private static long ToUnixTimeMilliseconds(DateTime localTime)
    {
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(localTime.ToUniversalTime() - epochStart).TotalMilliseconds;
    }

    private static void ScheduleAlarm(AlarmManager alarmManager, long triggerTimeMs, PendingIntent pendingIntent)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            if (alarmManager.CanScheduleExactAlarms())
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                return;
            }

            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
            return;
        }

        alarmManager.SetExact(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
    }

    private static PendingIntentFlags GetPendingIntentFlags()
    {
        var flags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            flags |= PendingIntentFlags.Immutable;
        }

        return flags;
    }

    private static bool HasPermission(Context context, string permission)
    {
        return ActivityCompat.CheckSelfPermission(context, permission) == Permission.Granted;
    }
}

#pragma warning restore CS0618
#pragma warning restore CA1416
