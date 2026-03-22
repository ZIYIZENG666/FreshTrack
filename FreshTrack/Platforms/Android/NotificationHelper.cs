using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

// 抑制版本兼容性警告（仅当前文件）
#pragma warning disable CS0618
#pragma warning disable CA1416

namespace FreshTrack.Platforms.Android;

public static class NotificationHelper
{
    private const string CHANNEL_ID = "GroceryReminderChannel";
    private const string CHANNEL_NAME = "Grocery List Reminders";
    private const string CHANNEL_DESCRIPTION = "Reminders for grocery list items";

    /// <summary>
    /// 创建通知渠道（Android 8.0+ 必需，无空引用警告）
    /// </summary>
    public static void CreateNotificationChannel()
    {
        // 1. 版本检查：仅Android 8.0+执行
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        // 2. 获取应用上下文（强制非空 + 编译器宽恕）
        var androidAppContext = global::Android.App.Application.Context;
        if (androidAppContext is null) return;

        // 3. 获取NotificationService（拆分步骤 + 显式非空断言）
        var notificationService = androidAppContext.GetSystemService(Context.NotificationService);
        if (notificationService is null) return;

        // 4. 转换为NotificationManager（非空断言 + 检查）
        var notificationManager = notificationService as NotificationManager;
        if (notificationManager is null) return;

        // 5. 安全创建通知渠道（try-catch + 非空初始化）
        NotificationChannel channel;
        try
        {
            // 显式初始化，编译器确认非空
            channel = new NotificationChannel(
                CHANNEL_ID,
                CHANNEL_NAME,
                NotificationImportance.Default)
            {
                Description = CHANNEL_DESCRIPTION ?? string.Empty // 兜底空字符串
            };
        }
        catch
        {
            return;
        }

        // 最终调用：所有变量均非空，无警告
        notificationManager.CreateNotificationChannel(channel!); // 非空断言，消除最后疑虑
    }

    /// <summary>
    /// 显示通知（兼容所有Android版本，彻底无空引用警告）
    /// </summary>
    /// <param name="context">上下文</param>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="notificationId">唯一通知ID</param>
    public static void ShowNotification(Context? context, string title, string message, int notificationId)
    {
        // 1. 根上下文非空检查（最外层防御）
        if (context is null) return;

        // 2. 处理PackageName（显式非空 + 兜底）
        var packageName = context.PackageName;
        if (string.IsNullOrEmpty(packageName))
        {
            packageName = context.PackageName!; // 编译器宽恕（上下文非空则PackageName必存在）
            if (string.IsNullOrEmpty(packageName)) return;
        }

        // 3. 处理PackageManager（非空断言 + 检查）
        var packageManager = context.PackageManager;
        if (packageManager is null) return;

        // 4. 获取启动Intent（兜底空Intent，消除后续警告）
        var intent = packageManager.GetLaunchIntentForPackage(packageName);
        intent ??= new Intent(); // 空合并：null则创建空Intent
        intent.AddFlags(ActivityFlags.ClearTop); // 此时intent必非空

        // 5. 构建PendingIntent Flags（版本兼容）
        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        // 6. 安全创建PendingIntent（try-catch + 非空检查）
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

        // 7. 构建通知（拆分链式调用 + 全链路非空确认，彻底消除警告）
        var safeTitle = title ?? "Grocery List Reminder";
        var safeMessage = message ?? "It's time to check your grocery list!";
        var safeChannelId = CHANNEL_ID ?? "default_channel"; // 常量兜底（仅为编译器安心）

        // 步骤1：显式确认context非空（前面已检查，此处断言消除编译器疑虑）
        var nonNullContext = context!;

        // 步骤2：提取系统图标ID并显式断言非空
        var notificationIcon = global::Android.Resource.Drawable.IcDialogAlert!;

        // 步骤3：拆分Builder链式调用，逐个步骤确认非空
        NotificationCompat.Builder builder = new NotificationCompat.Builder(nonNullContext, safeChannelId);

        // 逐个设置属性，避免链式调用导致的警告
        builder = builder.SetContentTitle(safeTitle)!; // 断言返回值非空
        builder = builder.SetContentText(safeMessage)!; // 断言返回值非空
        builder = builder.SetSmallIcon(notificationIcon)!; // 图标ID非空，返回值断言
        builder = builder.SetAutoCancel(true)!; // 断言返回值非空
        builder = builder.SetPriority(NotificationCompat.PriorityDefault)!; // 断言返回值非空

        // 最终确认builder非空（双重保险）
        if (builder is null) return;

        // 8. 设置PendingIntent（仅非空时）
        if (pendingIntent is not null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        // 9. 显示通知（终极非空检查 + 断言）
        var notificationManager = NotificationManagerCompat.From(context);
        if (notificationManager is null) return;

        var notification = builder.Build();
        if (notification is not null)
        {
            notificationManager.Notify(notificationId, notification!); // 非空断言
        }
    }
}

// 恢复警告检查
#pragma warning restore CS0618
#pragma warning restore CA1416