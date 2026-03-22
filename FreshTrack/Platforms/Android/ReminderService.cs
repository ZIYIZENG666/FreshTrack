
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FreshTrack;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;

#pragma warning disable CA1416
#pragma warning disable CS0618 

namespace FreshTrack.Platforms.Android;

public class ReminderService : IReminderService
{
    private const string REMINDER_ACTION = "com.freshtrack.REMINDER_ACTION";

    public void SetReminder(DateTime triggerTime, string title, string message, int listId = 0)
    {
        NotificationHelper.CreateNotificationChannel();

        DateTime localTime = triggerTime.Kind == DateTimeKind.Utc
            ? triggerTime.ToLocalTime()
            : triggerTime;

        System.Diagnostics.Debug.WriteLine($"===== 闹钟设置 =====");
        System.Diagnostics.Debug.WriteLine($"传入时间: {triggerTime} (Kind: {triggerTime.Kind})");
        System.Diagnostics.Debug.WriteLine($"本地时间: {localTime}");
        System.Diagnostics.Debug.WriteLine($"====================");

        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        long triggerTimeMs = (long)(localTime.ToUniversalTime() - epochStart).TotalMilliseconds;

        var androidAppContext = global::Android.App.Application.Context;
        if (androidAppContext == null) return;

        var intent = new Intent(androidAppContext, typeof(ReminderBroadcastReceiver));
        intent.SetAction(REMINDER_ACTION);
        intent.PutExtra("Title", title);
        intent.PutExtra("Message", message);
        intent.PutExtra("NotificationId", listId);

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            pendingIntentFlags |= PendingIntentFlags.Immutable;

        PendingIntent? pendingIntent = null;
        try
        {
            pendingIntent = PendingIntent.GetBroadcast(
                androidAppContext,
                listId,
                intent,
                pendingIntentFlags);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PendingIntent 创建失败: {ex.Message}");
            return;
        }

        var alarmManager = androidAppContext.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager != null && pendingIntent != null)
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    if (alarmManager.CanScheduleExactAlarms())
                    {
                        alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                    }
                    else
                    {
                        alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                    }
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                }
                else
                {
                    alarmManager.SetExact(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
                }
                System.Diagnostics.Debug.WriteLine("✅ 闹钟已设置到系统");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 设置闹钟失败: {ex.Message}");
            }
        }
    }

    public async Task<bool> RequestNotificationPermissionAsync()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var androidAppContext = global::Android.App.Application.Context;
            if (androidAppContext == null) return false;

            string notificationPermission = global::Android.Manifest.Permission.PostNotifications;
            Permission currentPermission = ActivityCompat.CheckSelfPermission(androidAppContext, notificationPermission);

            if (currentPermission != Permission.Granted)
            {
                var currentActivity = Platform.CurrentActivity;
                if (currentActivity == null) return false;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ActivityCompat.RequestPermissions(
                        currentActivity,
                        new[] { notificationPermission },
                        1001);
                });

                await Task.Delay(1500);
                currentPermission = ActivityCompat.CheckSelfPermission(androidAppContext, notificationPermission);
            }

            return currentPermission == Permission.Granted;
        }

        return true;
    }
}

#pragma warning restore CA1416
#pragma warning restore CS0618