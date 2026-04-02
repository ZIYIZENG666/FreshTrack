using Microsoft.Extensions.DependencyInjection;

namespace FreshTrack;

public static class ServiceHelper
{
    private static IServiceProvider? _services;

    public static void Initialize(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        if (_services is null)
        {
            throw new InvalidOperationException("The application services are not ready yet.");
        }

        return _services.GetRequiredService<T>();
    }

    public static T? GetService<T>() where T : class
    {
        return _services?.GetService<T>();
    }
}
