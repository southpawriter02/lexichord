// =============================================================================
// File: CitationFormatterRegistryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CitationFormatterRegistry.
// =============================================================================
// LOGIC: Verifies registry functionality:
//   - Constructor null-parameter validation (3 dependencies).
//   - Constructor builds dictionary from IEnumerable<ICitationFormatter>.
//   - All returns all registered formatters.
//   - GetFormatter returns correct formatter for each style.
//   - GetFormatter throws for unregistered style.
//   - GetPreferredStyleAsync returns Inline as default when not set.
//   - GetPreferredStyleAsync returns persisted style from settings.
//   - GetPreferredStyleAsync falls back to Inline for invalid setting.
//   - SetPreferredStyleAsync persists to settings repository.
//   - GetPreferredFormatterAsync returns formatter for preferred style.
//   - Duplicate formatter registration logs warning, last wins.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Formatters;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CitationFormatterRegistry"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2b")]
public class CitationFormatterRegistryTests
{
    // LOGIC: Mock dependencies.
    private readonly Mock<ISystemSettingsRepository> _settingsMock;
    private readonly Mock<ILogger<CitationFormatterRegistry>> _loggerMock;

    // LOGIC: Real formatter instances (stateless, safe to share).
    private readonly InlineCitationFormatter _inlineFormatter = new();
    private readonly FootnoteCitationFormatter _footnoteFormatter = new();
    private readonly MarkdownCitationFormatter _markdownFormatter = new();

    public CitationFormatterRegistryTests()
    {
        _settingsMock = new Mock<ISystemSettingsRepository>();
        _loggerMock = new Mock<ILogger<CitationFormatterRegistry>>();

        // LOGIC: Default mock behavior — return "Inline" for default style.
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CitationStyle.Inline.ToString());
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    /// <summary>
    /// Verifies constructor throws when formatters is null.
    /// </summary>
    [Fact]
    public void Constructor_NullFormatters_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CitationFormatterRegistry(null!, _settingsMock.Object, _loggerMock.Object));
    }

    /// <summary>
    /// Verifies constructor throws when settings is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CitationFormatterRegistry(
                new[] { _inlineFormatter },
                null!,
                _loggerMock.Object));
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CitationFormatterRegistry(
                new[] { _inlineFormatter },
                _settingsMock.Object,
                null!));
    }

    // =========================================================================
    // All Property
    // =========================================================================

    /// <summary>
    /// Verifies All returns all registered formatters.
    /// </summary>
    [Fact]
    public void All_ReturnsAllRegisteredFormatters()
    {
        // Arrange
        var sut = CreateRegistry();

        // Act
        var all = sut.All;

        // Assert
        Assert.Equal(3, all.Count);
    }

    /// <summary>
    /// Verifies All with empty formatters returns empty collection.
    /// </summary>
    [Fact]
    public void All_EmptyFormatters_ReturnsEmptyCollection()
    {
        // Arrange
        var sut = new CitationFormatterRegistry(
            Array.Empty<ICitationFormatter>(),
            _settingsMock.Object,
            _loggerMock.Object);

        // Act
        var all = sut.All;

        // Assert
        Assert.Empty(all);
    }

    // =========================================================================
    // GetFormatter Tests
    // =========================================================================

    /// <summary>
    /// Verifies GetFormatter returns correct formatter for Inline style.
    /// </summary>
    [Fact]
    public void GetFormatter_Inline_ReturnsInlineFormatter()
    {
        // Arrange
        var sut = CreateRegistry();

        // Act
        var formatter = sut.GetFormatter(CitationStyle.Inline);

        // Assert
        Assert.IsType<InlineCitationFormatter>(formatter);
    }

    /// <summary>
    /// Verifies GetFormatter returns correct formatter for Footnote style.
    /// </summary>
    [Fact]
    public void GetFormatter_Footnote_ReturnsFootnoteFormatter()
    {
        // Arrange
        var sut = CreateRegistry();

        // Act
        var formatter = sut.GetFormatter(CitationStyle.Footnote);

        // Assert
        Assert.IsType<FootnoteCitationFormatter>(formatter);
    }

    /// <summary>
    /// Verifies GetFormatter returns correct formatter for Markdown style.
    /// </summary>
    [Fact]
    public void GetFormatter_Markdown_ReturnsMarkdownFormatter()
    {
        // Arrange
        var sut = CreateRegistry();

        // Act
        var formatter = sut.GetFormatter(CitationStyle.Markdown);

        // Assert
        Assert.IsType<MarkdownCitationFormatter>(formatter);
    }

    /// <summary>
    /// Verifies GetFormatter throws ArgumentException for unregistered style.
    /// </summary>
    [Fact]
    public void GetFormatter_UnregisteredStyle_ThrowsArgumentException()
    {
        // Arrange — registry with only inline formatter
        var sut = new CitationFormatterRegistry(
            new ICitationFormatter[] { _inlineFormatter },
            _settingsMock.Object,
            _loggerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => sut.GetFormatter(CitationStyle.Footnote));
    }

    // =========================================================================
    // GetPreferredStyleAsync Tests
    // =========================================================================

    /// <summary>
    /// Verifies GetPreferredStyleAsync returns Inline as default when not set.
    /// </summary>
    [Fact]
    public async Task GetPreferredStyleAsync_NotSet_ReturnsInline()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Inline");

        var sut = CreateRegistry();

        // Act
        var style = await sut.GetPreferredStyleAsync();

        // Assert
        Assert.Equal(CitationStyle.Inline, style);
    }

    /// <summary>
    /// Verifies GetPreferredStyleAsync returns persisted style.
    /// </summary>
    [Fact]
    public async Task GetPreferredStyleAsync_PersistedMarkdown_ReturnsMarkdown()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Markdown");

        var sut = CreateRegistry();

        // Act
        var style = await sut.GetPreferredStyleAsync();

        // Assert
        Assert.Equal(CitationStyle.Markdown, style);
    }

    /// <summary>
    /// Verifies GetPreferredStyleAsync returns Footnote when persisted.
    /// </summary>
    [Fact]
    public async Task GetPreferredStyleAsync_PersistedFootnote_ReturnsFootnote()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Footnote");

        var sut = CreateRegistry();

        // Act
        var style = await sut.GetPreferredStyleAsync();

        // Assert
        Assert.Equal(CitationStyle.Footnote, style);
    }

    /// <summary>
    /// Verifies GetPreferredStyleAsync falls back to Inline for invalid setting.
    /// </summary>
    [Fact]
    public async Task GetPreferredStyleAsync_InvalidSetting_ReturnsInline()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("InvalidStyleName");

        var sut = CreateRegistry();

        // Act
        var style = await sut.GetPreferredStyleAsync();

        // Assert
        Assert.Equal(CitationStyle.Inline, style);
    }

    /// <summary>
    /// Verifies GetPreferredStyleAsync is case-insensitive.
    /// </summary>
    [Fact]
    public async Task GetPreferredStyleAsync_CaseInsensitive_ReturnsCorrectStyle()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("markdown");

        var sut = CreateRegistry();

        // Act
        var style = await sut.GetPreferredStyleAsync();

        // Assert
        Assert.Equal(CitationStyle.Markdown, style);
    }

    // =========================================================================
    // SetPreferredStyleAsync Tests
    // =========================================================================

    /// <summary>
    /// Verifies SetPreferredStyleAsync persists the style to settings.
    /// </summary>
    [Fact]
    public async Task SetPreferredStyleAsync_PersistsToSettings()
    {
        // Arrange
        var sut = CreateRegistry();

        // Act
        await sut.SetPreferredStyleAsync(CitationStyle.Markdown);

        // Assert
        _settingsMock.Verify(s => s.SetValueAsync(
            CitationSettingsKeys.DefaultStyle,
            "Markdown",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies SetPreferredStyleAsync persists Footnote style.
    /// </summary>
    [Fact]
    public async Task SetPreferredStyleAsync_Footnote_PersistsFootnoteString()
    {
        // Arrange
        var sut = CreateRegistry();

        // Act
        await sut.SetPreferredStyleAsync(CitationStyle.Footnote);

        // Assert
        _settingsMock.Verify(s => s.SetValueAsync(
            CitationSettingsKeys.DefaultStyle,
            "Footnote",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // GetPreferredFormatterAsync Tests
    // =========================================================================

    /// <summary>
    /// Verifies GetPreferredFormatterAsync returns formatter for preferred style.
    /// </summary>
    [Fact]
    public async Task GetPreferredFormatterAsync_ReturnsFormatterForPreferredStyle()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Footnote");

        var sut = CreateRegistry();

        // Act
        var formatter = await sut.GetPreferredFormatterAsync();

        // Assert
        Assert.IsType<FootnoteCitationFormatter>(formatter);
    }

    /// <summary>
    /// Verifies GetPreferredFormatterAsync defaults to Inline formatter.
    /// </summary>
    [Fact]
    public async Task GetPreferredFormatterAsync_DefaultsToInline()
    {
        // Arrange
        _settingsMock
            .Setup(s => s.GetValueAsync(
                CitationSettingsKeys.DefaultStyle,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Inline");

        var sut = CreateRegistry();

        // Act
        var formatter = await sut.GetPreferredFormatterAsync();

        // Assert
        Assert.IsType<InlineCitationFormatter>(formatter);
    }

    // =========================================================================
    // Duplicate Registration
    // =========================================================================

    /// <summary>
    /// Verifies duplicate formatter registration — last one wins.
    /// </summary>
    [Fact]
    public void Constructor_DuplicateStyle_LastFormatterWins()
    {
        // Arrange — two inline formatters, the second should win
        var formatter1 = new InlineCitationFormatter();
        var formatter2 = new InlineCitationFormatter();

        var sut = new CitationFormatterRegistry(
            new ICitationFormatter[] { formatter1, formatter2 },
            _settingsMock.Object,
            _loggerMock.Object);

        // Act
        var result = sut.GetFormatter(CitationStyle.Inline);

        // Assert — the second formatter instance is the one in the registry
        Assert.Same(formatter2, result);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a registry with all three built-in formatters.
    /// </summary>
    private CitationFormatterRegistry CreateRegistry()
    {
        var formatters = new ICitationFormatter[]
        {
            _inlineFormatter,
            _footnoteFormatter,
            _markdownFormatter
        };

        return new CitationFormatterRegistry(
            formatters,
            _settingsMock.Object,
            _loggerMock.Object);
    }
}
