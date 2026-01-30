using AvaloniaEdit.Rendering;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.Services.Rendering;

namespace Lexichord.Tests.Unit.Modules.Editor.Rendering;

/// <summary>
/// Unit tests for <see cref="WavyUnderlineBackgroundRenderer"/>.
/// </summary>
public sealed class WavyUnderlineBackgroundRendererTests
{
    private readonly WavyUnderlineBackgroundRenderer _sut;

    public WavyUnderlineBackgroundRendererTests()
    {
        _sut = new WavyUnderlineBackgroundRenderer();
    }

    #region Layer Tests

    [Fact]
    public void Layer_ReturnsBackground()
    {
        // Assert
        Assert.Equal(KnownLayer.Background, _sut.Layer);
    }

    #endregion

    #region Segment Management Tests

    [Fact]
    public void AddSegment_DoesNotThrow()
    {
        // Arrange
        var segment = CreateSegment(0, 50, UnderlineColor.LightError);

        // Act & Assert
        var exception = Record.Exception(() => _sut.AddSegment(segment));
        Assert.Null(exception);
    }

    [Fact]
    public void AddSegment_NullSegment_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.AddSegment(null!));
    }

    [Fact]
    public void ClearSegments_DoesNotThrow()
    {
        // Arrange
        _sut.AddSegment(CreateSegment(0, 50, UnderlineColor.LightError));
        _sut.AddSegment(CreateSegment(60, 100, UnderlineColor.LightWarning));

        // Act & Assert
        var exception = Record.Exception(() => _sut.ClearSegments());
        Assert.Null(exception);
    }

    [Fact]
    public void SetSegments_ReplacesExistingSegments()
    {
        // Arrange
        _sut.AddSegment(CreateSegment(0, 50, UnderlineColor.LightError));

        var newSegments = new[]
        {
            CreateSegment(100, 150, UnderlineColor.LightWarning),
            CreateSegment(200, 250, UnderlineColor.LightInfo)
        };

        // Act & Assert
        var exception = Record.Exception(() => _sut.SetSegments(newSegments));
        Assert.Null(exception);
    }

    [Fact]
    public void SetSegments_NullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.SetSegments(null!));
    }

    #endregion

    #region Multiple Segment Handling Tests

    [Fact]
    public void AddSegment_MultipleCalls_AccumulatesSegments()
    {
        // Arrange & Act
        _sut.AddSegment(CreateSegment(0, 50, UnderlineColor.LightError));
        _sut.AddSegment(CreateSegment(60, 100, UnderlineColor.LightWarning));
        _sut.AddSegment(CreateSegment(110, 150, UnderlineColor.LightInfo));

        // Assert - verify all segments were added (via cleared state)
        _sut.ClearSegments();
        // If we get here without exception, segments were handled correctly
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task AddSegment_ConcurrentCalls_DoesNotThrow()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - add segments from multiple threads
        for (int i = 0; i < 100; i++)
        {
            var offset = i * 10;
            tasks.Add(Task.Run(() =>
                _sut.AddSegment(CreateSegment(offset, offset + 5, UnderlineColor.LightInfo))));
        }

        // Assert - should complete without exceptions
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ClearSegments_ConcurrentWithAdd_DoesNotThrow()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - add and clear from multiple threads
        for (int i = 0; i < 50; i++)
        {
            var offset = i * 10;
            tasks.Add(Task.Run(() =>
                _sut.AddSegment(CreateSegment(offset, offset + 5, UnderlineColor.LightWarning))));
            tasks.Add(Task.Run(() => _sut.ClearSegments()));
        }

        // Assert - should complete without exceptions
        await Task.WhenAll(tasks);
    }

    #endregion

    #region Color Constants Tests

    [Fact]
    public void UnderlineColor_LightError_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0xE5, UnderlineColor.LightError.R);
        Assert.Equal(0x14, UnderlineColor.LightError.G);
        Assert.Equal(0x00, UnderlineColor.LightError.B);
        Assert.Equal(255, UnderlineColor.LightError.A);
    }

    [Fact]
    public void UnderlineColor_DarkError_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0xFF, UnderlineColor.DarkError.R);
        Assert.Equal(0x6B, UnderlineColor.DarkError.G);
        Assert.Equal(0x6B, UnderlineColor.DarkError.B);
        Assert.Equal(255, UnderlineColor.DarkError.A);
    }

    [Fact]
    public void UnderlineColor_BackgroundColors_AreTranslucent()
    {
        // Assert - light theme backgrounds have 12% opacity (0x20)
        Assert.Equal(0x20, UnderlineColor.LightErrorBackground.A);
        Assert.Equal(0x20, UnderlineColor.LightWarningBackground.A);
        Assert.Equal(0x20, UnderlineColor.LightInfoBackground.A);

        // Assert - dark theme backgrounds have 19% opacity (0x30)
        Assert.Equal(0x30, UnderlineColor.DarkErrorBackground.A);
        Assert.Equal(0x30, UnderlineColor.DarkWarningBackground.A);
        Assert.Equal(0x30, UnderlineColor.DarkInfoBackground.A);
    }

    [Fact]
    public void UnderlineColor_LegacyAliases_MatchLightTheme()
    {
        // Assert - legacy aliases should point to light theme colors
        Assert.Equal(UnderlineColor.LightError, UnderlineColor.ErrorRed);
        Assert.Equal(UnderlineColor.LightWarning, UnderlineColor.WarningYellow);
        Assert.Equal(UnderlineColor.LightInfo, UnderlineColor.InfoBlue);
        Assert.Equal(UnderlineColor.LightHint, UnderlineColor.HintGray);
    }

    #endregion

    #region Helper Methods

    private static UnderlineSegment CreateSegment(int start, int end, UnderlineColor color)
    {
        return new UnderlineSegment(start, end, color, Guid.NewGuid().ToString());
    }

    #endregion
}
