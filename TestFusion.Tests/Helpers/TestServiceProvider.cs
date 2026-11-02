using Microsoft.Extensions.DependencyInjection;
using TestFusion.Core.Interfaces;

namespace TestFusion.Tests.Helpers;

public class TestServiceProvider : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public IServiceScopeFactory ScopeFactory =>
        _serviceProvider.GetRequiredService<IServiceScopeFactory>();

    public TestServiceProvider(TestSettingsService? settingsService = null)
    {
        settingsService ??= new TestSettingsService();

        var services = new ServiceCollection();

        services.AddScoped<ISettingsService>(_ => settingsService);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}