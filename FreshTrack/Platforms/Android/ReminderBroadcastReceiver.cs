using Android.App;
using Android.Content;
using FreshTrack.Platforms.Android;

namespace FreshTrack.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { "com.freshtrack.REMINDER_ACTION" })]
public class ReminderBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        var title = intent.GetStringExtra("Title") ?? "Grocery Reminder";
        var message = intent.GetStringExtra("Message") ?? "It's time to check your grocery list!";
        var notificationId = intent.GetIntExtra("NotificationId", 0);

        NotificationHelper.ShowNotification(context, title, message, notificationId);
    }
}