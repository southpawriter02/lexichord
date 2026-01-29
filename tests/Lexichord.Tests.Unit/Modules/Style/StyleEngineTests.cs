using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for StyleEngine.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the StyleEngine contract:
/// - Active style sheet management
/// - Event raising on sheet changes
/// - AnalyzeAsync behavior with edge cases
/// </remarks>
public class StyleEngineTests
{
    private readonly Mock<ILogger<StyleEngine>> _loggerMock;
    private readonly StyleEngine _sut;

    public StyleEngineTests()
    {
        _loggerMock = new Mock<ILogger<StyleEngine>>();
        _sut = new StyleEngine(_loggerMock.Object);
    }

    [Fact]
    public void GetActiveStyleSheet_ReturnsEmpty_WhenNotInitialized()
    {
        // Act
        var result = _sut.GetActiveStyleSheet();

        // Assert
        result.Should().Be(StyleSheet.Empty);
    }

    [Fact]
    public void SetActiveStyleSheet_UpdatesActiveSheet()
    {
        // Arrange
        var customSheet = new StyleSheet
        {
            Name = "Custom",
            Version = "1.0.0",
            Rules = []
        };

        // Act
        _sut.SetActiveStyleSheet(customSheet);
        var result = _sut.GetActiveStyleSheet();

        // Assert
        result.Should().BeSameAs(customSheet);
        result.Name.Should().Be("Custom");
    }

    [Fact]
    public void SetActiveStyleSheet_RaisesStyleSheetChangedEvent()
    {
        // Arrange
        var customSheet = new StyleSheet { Name = "Custom", Version = "1.0.0" };
        StyleSheetChangedEventArgs? eventArgs = null;
        _sut.StyleSheetChanged += (_, args) => eventArgs = args;

        // Act
        _sut.SetActiveStyleSheet(customSheet);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.OldStyleSheet.Should().Be(StyleSheet.Empty);
        eventArgs.NewStyleSheet.Should().BeSameAs(customSheet);
        eventArgs.ChangeSource.Should().Be(StyleSheetChangeSource.Api);
    }

    [Fact]
    public void SetActiveStyleSheet_ThrowsOnNull()
    {
        // Act
        var act = () => _sut.SetActiveStyleSheet(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("styleSheet");
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_WhenContentIsNull()
    {
        // Act
        var result = await _sut.AnalyzeAsync(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_WhenContentIsEmpty()
    {
        // Act
        var result = await _sut.AnalyzeAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_WhenStyleSheetHasNoRules()
    {
        // Arrange
        var content = "This is some test content.";
        _sut.SetActiveStyleSheet(new StyleSheet
        {
            Name = "Empty",
            Version = "1.0.0",
            Rules = []
        });

        // Act
        var result = await _sut.AnalyzeAsync(content);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_UsesActiveSheet_WhenNoSheetProvided()
    {
        // Arrange
        var customSheet = new StyleSheet
        {
            Name = "Active",
            Version = "1.0.0",
            Rules = []
        };
        _sut.SetActiveStyleSheet(customSheet);

        // Act
        var result = await _sut.AnalyzeAsync("Some content");

        // Assert
        // With the stub implementation, it just returns empty
        // This test verifies it doesn't throw
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_UsesProvidedSheet_WhenSheetProvided()
    {
        // Arrange
        var providedSheet = new StyleSheet
        {
            Name = "Provided",
            Version = "1.0.0",
            Rules = []
        };

        // Act
        var result = await _sut.AnalyzeAsync("Some content", providedSheet);

        // Assert
        // With the stub implementation, it just returns empty
        // This test verifies it doesn't throw
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsOnNullSheet()
    {
        // Act
        var act = async () => await _sut.AnalyzeAsync("content", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("styleSheet");
    }
}
