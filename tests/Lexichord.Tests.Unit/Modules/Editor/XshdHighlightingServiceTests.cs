using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for <see cref="XshdHighlightingService"/>.
/// </summary>
public class XshdHighlightingServiceTests : IDisposable
{
    private readonly Mock<IThemeManager> _themeManagerMock;
    private readonly XshdHighlightingService _sut;

    public XshdHighlightingServiceTests()
    {
        _themeManagerMock = new Mock<IThemeManager>();
        _themeManagerMock.Setup(x => x.GetEffectiveTheme()).Returns(ThemeMode.Light);

        _sut = new XshdHighlightingService(
            _themeManagerMock.Object,
            NullLogger<XshdHighlightingService>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region LoadDefinitionsAsync Tests

    [Fact]
    public async Task LoadDefinitionsAsync_LoadsAllBuiltInDefinitions()
    {
        // Act
        await _sut.LoadDefinitionsAsync();
        var available = _sut.GetAvailableHighlightings();

        // Assert
        available.Should().Contain("Markdown");
        available.Should().Contain("JSON");
        available.Should().Contain("YAML");
        available.Should().Contain("XML");
    }

    [Fact]
    public async Task LoadDefinitionsAsync_CanRetrieveLoadedDefinitions()
    {
        // Arrange
        await _sut.LoadDefinitionsAsync();

        // Act
        var markdown = _sut.GetHighlightingByName("Markdown");
        var json = _sut.GetHighlightingByName("JSON");

        // Assert
        markdown.Should().NotBeNull();
        markdown!.Name.Should().Be("Markdown");
        json.Should().NotBeNull();
        json!.Name.Should().Be("JSON");
    }

    #endregion

    #region GetHighlighting Tests

    [Theory]
    [InlineData(".md", "Markdown")]
    [InlineData(".markdown", "Markdown")]
    [InlineData(".json", "JSON")]
    [InlineData(".yml", "YAML")]
    [InlineData(".yaml", "YAML")]
    [InlineData(".xml", "XML")]
    [InlineData(".csproj", "XML")]
    [InlineData(".axaml", "XML")]
    public void GetHighlighting_KnownExtension_ReturnsCorrectDefinition(
        string extension,
        string expectedName)
    {
        // Act
        var result = _sut.GetHighlighting(extension);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".cs")]
    [InlineData(".unknown")]
    [InlineData("")]
    public void GetHighlighting_UnknownExtension_ReturnsNull(string extension)
    {
        // Act
        var result = _sut.GetHighlighting(extension);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetHighlighting_NullExtension_ReturnsNull()
    {
        // Act
        var result = _sut.GetHighlighting(null!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(".MD")]
    [InlineData(".Json")]
    [InlineData(".YML")]
    [InlineData(".XML")]
    public void GetHighlighting_ExtensionIsCaseInsensitive(string extension)
    {
        // Act
        var result = _sut.GetHighlighting(extension);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("md")]
    [InlineData("json")]
    [InlineData("yml")]
    public void GetHighlighting_ExtensionWithoutDot_StillWorks(string extension)
    {
        // Act
        var result = _sut.GetHighlighting(extension);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetHighlightingByName Tests

    [Fact]
    public void GetHighlightingByName_ValidName_ReturnsDefinition()
    {
        // Act
        var result = _sut.GetHighlightingByName("Markdown");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Markdown");
    }

    [Fact]
    public void GetHighlightingByName_InvalidName_ReturnsNull()
    {
        // Act
        var result = _sut.GetHighlightingByName("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetHighlightingByName_CachesDefinition()
    {
        // Arrange
        var first = _sut.GetHighlightingByName("Markdown");

        // Act
        var second = _sut.GetHighlightingByName("Markdown");

        // Assert
        second.Should().BeSameAs(first);
    }

    #endregion

    #region SetTheme Tests

    [Fact]
    public void SetTheme_DifferentTheme_ClearsCacheAndRaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        HighlightingChangeReason? reason = null;

        _ = _sut.GetHighlightingByName("Markdown"); // Cache a definition
        _sut.HighlightingChanged += (_, e) =>
        {
            eventRaised = true;
            reason = e.Reason;
        };

        // Act
        _sut.SetTheme(EditorTheme.Dark);

        // Assert
        eventRaised.Should().BeTrue();
        reason.Should().Be(HighlightingChangeReason.ThemeChanged);
        _sut.CurrentTheme.Should().Be(EditorTheme.Dark);
    }

    [Fact]
    public void SetTheme_SameTheme_DoesNotRaiseEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.HighlightingChanged += (_, _) => eventRaised = true;

        // Act
        _sut.SetTheme(EditorTheme.Light); // Already light

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SetTheme_AfterCache_ReloadsDefinitionsWithNewTheme()
    {
        // Arrange
        var lightDef = _sut.GetHighlightingByName("Markdown");

        // Act
        _sut.SetTheme(EditorTheme.Dark);
        var darkDef = _sut.GetHighlightingByName("Markdown");

        // Assert
        // After theme change, a new definition should be loaded (different instance)
        darkDef.Should().NotBeSameAs(lightDef);
    }

    #endregion

    #region GetExtensionsForHighlighting Tests

    [Fact]
    public void GetExtensionsForHighlighting_ValidName_ReturnsExtensions()
    {
        // Act
        var result = _sut.GetExtensionsForHighlighting("Markdown");

        // Assert
        result.Should().Contain(".md");
        result.Should().Contain(".markdown");
    }

    [Fact]
    public void GetExtensionsForHighlighting_InvalidName_ReturnsEmpty()
    {
        // Act
        var result = _sut.GetExtensionsForHighlighting("NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region RegisterHighlighting Tests

    [Fact]
    public void RegisterHighlighting_AddsCustomDefinition()
    {
        // Arrange
        var mockDefinition = new Mock<AvaloniaEdit.Highlighting.IHighlightingDefinition>();
        mockDefinition.Setup(x => x.Name).Returns("CustomLang");

        // Act
        _sut.RegisterHighlighting("CustomLang", new[] { ".custom" }, mockDefinition.Object);

        // Assert
        var result = _sut.GetHighlighting(".custom");
        result.Should().BeSameAs(mockDefinition.Object);
    }

    [Fact]
    public void RegisterHighlighting_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        string? highlightingName = null;
        var mockDefinition = new Mock<AvaloniaEdit.Highlighting.IHighlightingDefinition>();

        _sut.HighlightingChanged += (_, e) =>
        {
            eventRaised = true;
            highlightingName = e.HighlightingName;
        };

        // Act
        _sut.RegisterHighlighting("CustomLang", new[] { ".custom" }, mockDefinition.Object);

        // Assert
        eventRaised.Should().BeTrue();
        highlightingName.Should().Be("CustomLang");
    }

    [Fact]
    public void RegisterHighlighting_OverridesExisting()
    {
        // Arrange
        var mockDefinition = new Mock<AvaloniaEdit.Highlighting.IHighlightingDefinition>();
        mockDefinition.Setup(x => x.Name).Returns("CustomMarkdown");

        // Act - override built-in Markdown
        _sut.RegisterHighlighting("Markdown", new[] { ".md" }, mockDefinition.Object);

        // Assert
        var result = _sut.GetHighlighting(".md");
        result.Should().BeSameAs(mockDefinition.Object);
    }

    #endregion

    #region ThemeManager Integration Tests

    [Fact]
    public void Constructor_InitializesFromThemeManager()
    {
        // Arrange
        var darkThemeManager = new Mock<IThemeManager>();
        darkThemeManager.Setup(x => x.GetEffectiveTheme()).Returns(ThemeMode.Dark);

        // Act
        using var service = new XshdHighlightingService(
            darkThemeManager.Object,
            NullLogger<XshdHighlightingService>.Instance);

        // Assert
        service.CurrentTheme.Should().Be(EditorTheme.Dark);
    }

    [Fact]
    public void ThemeManagerThemeChanged_UpdatesEditorTheme()
    {
        // Arrange
        var eventRaised = false;
        _sut.HighlightingChanged += (_, _) => eventRaised = true;

        // Act - simulate theme manager raising event
        _themeManagerMock.Raise(x => x.ThemeChanged += null, _themeManagerMock.Object, ThemeMode.Dark);

        // Assert
        _sut.CurrentTheme.Should().Be(EditorTheme.Dark);
        eventRaised.Should().BeTrue();
    }

    #endregion
}
