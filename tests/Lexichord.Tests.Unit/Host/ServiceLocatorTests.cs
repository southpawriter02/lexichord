using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for <see cref="ServiceLocator"/>.
/// </summary>
public class ServiceLocatorTests
{
    /// <summary>
    /// Verifies that GetRequiredService returns a registered service.
    /// </summary>
    [Fact]
    public void GetRequiredService_ReturnsRegisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for WindowStateService
        services.AddSingleton<IWindowStateService, WindowStateService>();
        var provider = services.BuildServiceProvider();

        #pragma warning disable CS0618 // Testing obsolete interface intentionally
        var sut = new ServiceLocator(provider);

        // Act
        var result = sut.GetRequiredService<IWindowStateService>();

        // Assert
        result.Should().NotBeNull();
        #pragma warning restore CS0618
    }

    /// <summary>
    /// Verifies that GetRequiredService throws for unregistered services.
    /// </summary>
    [Fact]
    public void GetRequiredService_ThrowsForUnregisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        #pragma warning disable CS0618 // Testing obsolete interface intentionally
        var sut = new ServiceLocator(provider);

        // Act & Assert
        var action = () => sut.GetRequiredService<IThemeManager>();
        action.Should().Throw<InvalidOperationException>();
        #pragma warning restore CS0618
    }

    /// <summary>
    /// Verifies that GetService returns null for unregistered services.
    /// </summary>
    [Fact]
    public void GetService_ReturnsNullForUnregisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        #pragma warning disable CS0618 // Testing obsolete interface intentionally
        var sut = new ServiceLocator(provider);

        // Act
        var result = sut.GetService<IThemeManager>();

        // Assert
        result.Should().BeNull();
        #pragma warning restore CS0618
    }

    /// <summary>
    /// Verifies that GetService returns a registered service.
    /// </summary>
    [Fact]
    public void GetService_ReturnsRegisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for WindowStateService
        services.AddSingleton<IWindowStateService, WindowStateService>();
        var provider = services.BuildServiceProvider();

        #pragma warning disable CS0618 // Testing obsolete interface intentionally
        var sut = new ServiceLocator(provider);

        // Act
        var result = sut.GetService<IWindowStateService>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<WindowStateService>();
        #pragma warning restore CS0618
    }
}
