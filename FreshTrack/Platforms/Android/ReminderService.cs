// 补充完整的Android原生命名空间
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FreshTrack;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;

// 抑制当前文件的Android版本兼容性警告（仅影响此文件，不全局生效）
#pragma warning disable CA1416 // 抑制版本兼容性警告
#pragma warning disable CS0618 // 抑制过时API警告（如有）

namespace FreshTrack.Platforms.Android;

public class ReminderService : IReminderService
{
    private const string REMINDER_ACTION = "com.freshtrack.REMINDER_ACTION";

    public void SetReminder(DateTime triggerTime, string title, string message, int listId = 0)
    {
        // 初始化通知渠道
        NotificationHelper.CreateNotificationChannel();

        // 将UTC时间转换为Android的Epoch毫秒（增加边界检查，避免负数）
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        if (triggerTime < epochStart) triggerTime = epochStart.AddMinutes(1);
        var triggerTimeMs = (long)(triggerTime - epochStart).TotalMilliseconds;

        // 获取Android原生上下文（严格空值检查）
        var androidAppContext = global::Android.App.Application.Context;
        if (androidAppContext == null) return;

        // 创建广播意图（空值保护）
        var intent = new Intent(androidAppContext, typeof(ReminderBroadcastReceiver));
        intent.SetAction(REMINDER_ACTION);
        intent.PutExtra("Title", title ?? "Grocery Reminder");
        intent.PutExtra("Message", message ?? "Check your grocery list!");
        intent.PutExtra("NotificationId", listId);

        // 创建PendingIntent Flags（强化版本检查，仅在支持的版本添加Immutable）
        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        // PendingIntentFlags.Immutable 仅在Android 6.0+（API23）及更高版本支持
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        // 安全创建PendingIntent（异常捕获）
        PendingIntent? pendingIntent = null;
        try
        {
            pendingIntent = PendingIntent.GetBroadcast(
                androidAppContext,
                listId,
                intent,
                pendingIntentFlags);
        }
        catch (Exception)
        {
            return;
        }

        // 获取AlarmManager并设置定时（严格空值 + 版本检查）
        var alarmManager = androidAppContext.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager != null && pendingIntent != null)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M) // Android 6.0+（API23）
            {
                // SetExactAndAllowWhileIdle 仅在API23+支持，版本检查已覆盖
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat) // Android 4.4+（API19）
            {
                alarmManager.SetExact(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
            }
            else // 更低版本
            {
                alarmManager.Set(AlarmType.RtcWakeup, triggerTimeMs, pendingIntent);
            }
        }
    }

    public async Task<bool> RequestNotificationPermissionAsync()
    {
        // POST_NOTIFICATIONS权限仅在Android 13+（API33/Tiramisu）需要
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var androidAppContext = global::Android.App.Application.Context;
            if (androidAppContext == null) return false;

            // 明确引用Android 13+的通知权限（版本检查已覆盖）
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

        // 低于Android 13无需权限
        return true;
    }
}

// 恢复警告检查（仅当前文件）
#pragma warning restore CA1416
#pragma warning restore CS0618