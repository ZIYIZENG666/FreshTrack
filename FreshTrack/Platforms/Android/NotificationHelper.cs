using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

#pragma warning disable CS0618
#pragma warning disable CA1416

namespace FreshTrack.Platforms.Android;

public static class NotificationHelper
{
    private const string CHANNEL_ID = "GroceryReminderChannel";
    private const string CHANNEL_NAME = "Grocery List Reminders";
    private const string CHANNEL_DESCRIPTION = "Reminders for grocery list items";


    public static void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var androidAppContext = global::Android.App.Application.Context;
        if (androidAppContext is null) return;

        var notificationService = androidAppContext.GetSystemService(Context.NotificationService);
        if (notificationService is null) return;

        var notificationManager = notificationService as NotificationManager;
        if (notificationManager is null) return;

        NotificationChannel channel;
        try
        {
            channel = new NotificationChannel(
                CHANNEL_ID,
                CHANNEL_NAME,
                NotificationImportance.Default)
            {
                Description = CHANNEL_DESCRIPTION ?? string.Empty 
            };
        }
        catch
        {
            return;
        }

        notificationManager.CreateNotificationChannel(channel!); 
    }


    /// <param name="context">上下文</param>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="notificationId">唯一通知ID</param>
    public static void ShowNotification(Context? context, string title, string message, int notificationId)
    {
        if (context is null) return;

        var packageName = context.PackageName;
        if (string.IsNullOrEmpty(packageName))
        {
            packageName = context.PackageName!; 
            if (string.IsNullOrEmpty(packageName)) return;
        }

        var packageManager = context.PackageManager;
        if (packageManager is null) return;

        var intent = packageManager.GetLaunchIntentForPackage(packageName);
        intent ??= new Intent(); 
        intent.AddFlags(ActivityFlags.ClearTop); 

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        PendingIntent? pendingIntent = null;
        try
        {
            pendingIntent = PendingIntent.GetActivity(
                context,
                notificationId,
                intent,
                pendingIntentFlags);
        }
        catch
        {
            pendingIntent = null;
        }

        var safeTitle = title ?? "Grocery List Reminder";
        var safeMessage = message ?? "It's time to check your grocery list!";
        var safeChannelId = CHANNEL_ID ?? "default_channel"; 

        var nonNullContext = context!;

        var notificationIcon = global::Android.Resource.Drawable.IcDialogAlert!;

        NotificationCompat.Builder builder = new NotificationCompat.Builder(nonNullContext, safeChannelId);

        builder = builder.SetContentTitle(safeTitle)!; 
        builder = builder.SetContentText(safeMessage)!; 
        builder = builder.SetSmallIcon(notificationIcon)!; 
        builder = builder.SetAutoCancel(true)!; 
        builder = builder.SetPriority(NotificationCompat.PriorityDefault)!;

        if (builder is null) return;

        if (pendingIntent is not null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        var notificationManager = NotificationManagerCompat.From(context);
        if (notificationManager is null) return;

        var notification = builder.Build();
        if (notification is not null)
        {
            notificationManager.Notify(notificationId, notification!); // 非空断言
        }
    }
}

#pragma warning restore CS0618
#pragma warning restore CA1416