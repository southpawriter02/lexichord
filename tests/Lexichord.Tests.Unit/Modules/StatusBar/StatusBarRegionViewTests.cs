using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.StatusBar;
using Lexichord.Modules.StatusBar.Services;
using Lexichord.Modules.StatusBar.ViewModels;
using Lexichord.Modules.StatusBar.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for StatusBarRegionView.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the IShellRegionView contract:
/// - TargetRegion returns Shell.Bottom
/// - Order returns 100
/// - ViewContent lazily creates the view (skipped - requires Avalonia runtime)
/// </remarks>
public class StatusBarRegionViewTests
{
    [Fact]
    public void TargetRegion_ReturnsBottom()
    {
        // Arrange
        var provider = CreateMockServiceProvider();
        var regionView = new StatusBarRegionView(provider);

        // Act
        var result = regionView.TargetRegion;

        // Assert
        result.Should().Be(ShellRegion.Bottom);
    }

    [Fact]
    public void Order_Returns100()
    {
        // Arrange
        var provider = CreateMockServiceProvider();
        var regionView = new StatusBarRegionView(provider);

        // Act
        var result = regionView.Order;

        // Assert
        result.Should().Be(100);
    }

    // NOTE: ViewContent tests are skipped because they require Avalonia runtime.
    // The view instantiation tests would need a full Avalonia app context
    // which is not available in unit tests. These are verified by integration tests
    // and manual testing instead.

    private static IServiceProvider CreateMockServiceProvider()
    {
        return Substitute.For<IServiceProvider>();
    }
}
