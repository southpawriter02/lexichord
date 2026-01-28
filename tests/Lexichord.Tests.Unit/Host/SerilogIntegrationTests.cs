using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Lexichord.Host;
using Lexichord.Host.Services;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for Serilog integration.
/// </summary>
public class SerilogIntegrationTests
{
    /// <summary>
    /// Verifies that ILogger<T> is registered and resolvable via DI.
    /// </summary>
    [Fact]
    public void ConfigureServices_RegistersILoggerFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();
        var result = provider.GetService<ILoggerFactory>();

        // Assert
        result.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ILogger<T> can be resolved for WindowStateService.
    /// </summary>
    [Fact]
    public void ConfigureServices_CanResolveILogger_ForWindowStateService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();
        var logger = provider.GetService<ILogger<WindowStateService>>();

        // Assert
        logger.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that WindowStateService is created with logger injection.
    /// </summary>
    [Fact]
    public void WindowStateService_CanBeResolved_WithLogger()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IWindowStateService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<WindowStateService>();
    }
}
