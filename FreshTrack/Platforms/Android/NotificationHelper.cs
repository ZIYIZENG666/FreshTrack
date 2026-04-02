using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

#pragma warning disable CS0618
#pragma warning disable CA1416

namespace FreshTrack.Platforms.Android;

public static class NotificationHelper
{
    private const string ChannelId = "GroceryReminderChannel";
    private const string ChannelName = "Grocery List Reminders";
    private const string ChannelDescription = "Reminders for grocery list items";
    private const string DefaultTitle = "Grocery List Reminder";
    private const string DefaultMessage = "It's time to check your grocery list!";

    public static void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var context = global::Android.App.Application.Context;
        var notificationManager = context?.GetSystemService(Context.NotificationService) as NotificationManager;
        if (notificationManager is null)
        {
            return;
        }

        try
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Default)
            {
                Description = ChannelDescription
            };

            notificationManager.CreateNotificationChannel(channel);
        }
        catch
        {
        }
    }

    public static void ShowNotification(Context? context, string title, string message, int notificationId)
    {
        if (context is null)
        {
            return;
        }

        var launchIntent = GetLaunchIntent(context);
        if (launchIntent is null)
        {
            return;
        }

        var builder = new NotificationCompat.Builder(context, ChannelId);
        builder.SetContentTitle(string.IsNullOrWhiteSpace(title) ? DefaultTitle : title);
        builder.SetContentText(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message);
        builder.SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert);
        builder.SetAutoCancel(true);
        builder.SetPriority(NotificationCompat.PriorityDefault);

        var pendingIntent = CreateContentIntent(context, launchIntent, notificationId);
        if (pendingIntent is not null)
        {
            _ = builder.SetContentIntent(pendingIntent);
        }

        var notificationManager = NotificationManagerCompat.From(context);
        var notification = builder.Build();
        if (notificationManager is null || notification is null)
        {
            return;
        }

        notificationManager.Notify(notificationId, notification);
    }

    private static Intent? GetLaunchIntent(Context context)
    {
        if (context.PackageManager is null || string.IsNullOrWhiteSpace(context.PackageName))
        {
            return null;
        }

        var launchIntent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName) ?? new Intent();
        launchIntent.AddFlags(ActivityFlags.ClearTop);

        return launchIntent;
    }

    private static PendingIntent? CreateContentIntent(Context context, Intent intent, int notificationId)
    {
        try
        {
            return PendingIntent.GetActivity(
                context,
                notificationId,
                intent,
                GetPendingIntentFlags());
        }
        catch
        {
            return null;
        }
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
}

#pragma warning restore CS0618
#pragma warning restore CA1416
