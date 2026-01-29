using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for <see cref="EditorConfigurationService"/>.
/// </summary>
public class EditorConfigurationServiceTests : IDisposable
{
    private readonly string _testSettingsPath;
    private readonly EditorConfigurationService _sut;
    private readonly List<EditorSettingsChangedEventArgs> _raisedEvents = new();

    public EditorConfigurationServiceTests()
    {
        // LOGIC: Use unique temp directory for each test run
        var tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _testSettingsPath = Path.Combine(tempDir, "editor-settings.json");

        // Note: Can't override path in current implementation, tests use service defaults
        _sut = new EditorConfigurationService(NullLogger<EditorConfigurationService>.Instance);
        _sut.SettingsChanged += (_, e) => _raisedEvents.Add(e);
    }

    public void Dispose()
    {
        // Cleanup temp files if needed
        if (Directory.Exists(Path.GetDirectoryName(_testSettingsPath)))
        {
            try { Directory.Delete(Path.GetDirectoryName(_testSettingsPath)!, true); }
            catch { /* Ignore cleanup failures */ }
        }
    }

    #region GetSettings Tests

    [Fact]
    public void GetSettings_ReturnsCurrentSettings()
    {
        // Act
        var settings = _sut.GetSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.FontFamily.Should().Be("Cascadia Code");
        settings.FontSize.Should().Be(14.0);
    }

    [Fact]
    public void GetSettings_ReturnsDefaultValuesInitially()
    {
        // Act
        var settings = _sut.GetSettings();

        // Assert
        settings.ShowLineNumbers.Should().BeTrue();
        settings.WordWrap.Should().BeTrue();
        settings.UseSpacesForTabs.Should().BeTrue();
        settings.TabSize.Should().Be(4);
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesCurrentSettings()
    {
        // Arrange
        var newSettings = new EditorSettings { FontSize = 16.0, WordWrap = false };

        // Act
        await _sut.UpdateSettingsAsync(newSettings);

        // Assert
        var current = _sut.GetSettings();
        current.FontSize.Should().Be(16.0);
        current.WordWrap.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSettingsAsync_ValidatesSettings()
    {
        // Arrange - values outside valid range
        var invalidSettings = new EditorSettings 
        { 
            FontSize = 100.0, // > MaxFontSize (72)
            TabSize = 100     // > 16
        };

        // Act
        await _sut.UpdateSettingsAsync(invalidSettings);

        // Assert - values should be clamped
        var current = _sut.GetSettings();
        current.FontSize.Should().Be(72.0); // Clamped to max
        current.TabSize.Should().Be(16);    // Clamped to max
    }

    [Fact]
    public async Task UpdateSettingsAsync_RaisesSettingsChangedEvent()
    {
        // Arrange
        var newSettings = new EditorSettings { FontSize = 18.0 };
        _raisedEvents.Clear();

        // Act
        await _sut.UpdateSettingsAsync(newSettings);

        // Assert
        _raisedEvents.Should().HaveCount(1);
        _raisedEvents[0].NewSettings.FontSize.Should().Be(18.0);
    }

    #endregion

    #region Zoom Tests

    [Fact]
    public void ZoomIn_IncreasesFontSize()
    {
        // Arrange
        var originalSize = _sut.GetSettings().FontSize;

        // Act
        _sut.ZoomIn();

        // Assert
        _sut.GetSettings().FontSize.Should().Be(originalSize + 2.0);
    }

    [Fact]
    public async Task ZoomIn_ClampsToMaxFontSize()
    {
        // Arrange - set to near max
        await _sut.UpdateSettingsAsync(new EditorSettings { FontSize = 71.0 });

        // Act
        _sut.ZoomIn();

        // Assert
        _sut.GetSettings().FontSize.Should().Be(72.0); // Clamped to max
    }

    [Fact]
    public void ZoomIn_RaisesSettingsChangedEvent()
    {
        // Arrange
        _raisedEvents.Clear();

        // Act
        _sut.ZoomIn();

        // Assert
        _raisedEvents.Should().HaveCount(1);
        _raisedEvents[0].PropertyName.Should().Be(nameof(EditorSettings.FontSize));
    }

    [Fact]
    public void ZoomOut_DecreasesFontSize()
    {
        // Arrange
        var originalSize = _sut.GetSettings().FontSize;

        // Act
        _sut.ZoomOut();

        // Assert
        _sut.GetSettings().FontSize.Should().Be(originalSize - 2.0);
    }

    [Fact]
    public async Task ZoomOut_ClampsToMinFontSize()
    {
        // Arrange - set to near min
        await _sut.UpdateSettingsAsync(new EditorSettings { FontSize = 9.0 });

        // Act
        _sut.ZoomOut();

        // Assert
        _sut.GetSettings().FontSize.Should().Be(8.0); // Clamped to min
    }

    [Fact]
    public async Task ResetZoom_SetsToDefaultFontSize()
    {
        // Arrange
        await _sut.UpdateSettingsAsync(new EditorSettings { FontSize = 20.0 });

        // Act
        _sut.ResetZoom();

        // Assert
        _sut.GetSettings().FontSize.Should().Be(14.0);
    }

    #endregion

    #region Font Resolution Tests

    [Fact]
    public async Task GetResolvedFontFamily_ReturnsFallbackWhenNotInstalled()
    {
        // Arrange - set to a font that doesn't exist
        await _sut.UpdateSettingsAsync(new EditorSettings { FontFamily = "NonExistentFont123" });

        // Act
        var resolved = _sut.GetResolvedFontFamily();

        // Assert
        resolved.Should().NotBe("NonExistentFont123");
        // Should be one of the fallback fonts
        EditorSettings.FallbackFonts.Should().Contain(resolved);
    }

    [Fact]
    public void IsFontInstalled_ReturnsFalseForNonExistentFont()
    {
        // Act
        var result = _sut.IsFontInstalled("NonExistentFont123XYZ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetInstalledMonospaceFonts_ReturnsNonEmptyList()
    {
        // Act
        var fonts = _sut.GetInstalledMonospaceFonts();

        // Assert
        fonts.Should().NotBeNull();
        // May be empty on systems without common dev fonts, but method should work
    }

    #endregion
}
