using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host;
using Lexichord.Host.Services;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for <see cref="HostServices"/> DI registration.
/// </summary>
public class HostServicesTests
{
    /// <summary>
    /// Verifies that IThemeManager is registered in the DI container.
    /// </summary>
    /// <remarks>
    /// LOGIC: We skip this test because ThemeManager requires an Avalonia Application,
    /// which cannot be instantiated in a pure unit test without the Avalonia runtime.
    /// Integration tests or manual verification cover this scenario.
    /// </remarks>
    [Fact(Skip = "ThemeManager requires Avalonia Application which cannot be mocked in unit tests")]
    public void ConfigureServices_RegistersThemeManager()
    {
        // This test is skipped because ThemeManager has a hard dependency on Application
    }

    /// <summary>
    /// Verifies that IWindowStateService is registered in the DI container.
    /// </summary>
    [Fact]
    public void ConfigureServices_RegistersWindowStateService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();
        var result = provider.GetService<IWindowStateService>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<WindowStateService>();
    }

    /// <summary>
    /// Verifies that singleton services return the same instance.
    /// </summary>
    [Fact]
    public void ConfigureServices_WindowStateService_SingletonsReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<IWindowStateService>();
        var instance2 = provider.GetService<IWindowStateService>();

        // Assert
        instance1.Should().BeSameAs(instance2, 
            because: "Singleton services should return the same instance");
    }

    /// <summary>
    /// Verifies that IServiceLocator is registered in the DI container.
    /// </summary>
    [Fact]
    public void ConfigureServices_RegistersServiceLocator()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();

        #pragma warning disable CS0618 // Testing obsolete interface intentionally
        var result = provider.GetService<IServiceLocator>();
        #pragma warning restore CS0618

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ServiceLocator>();
    }
}
