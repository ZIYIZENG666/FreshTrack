using Microsoft.Extensions.Logging;

namespace FreshTrack;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
#if ANDROID

        builder.Services.AddSingleton<IReminderService, FreshTrack.Platforms.Android.ReminderService>();
#endif

        builder.Services.AddSingleton<IShoppingListRepository, JsonShoppingListRepository>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
