using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Services;
using Lexichord.Modules.Editor.Services;
using Lexichord.Modules.Editor.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for <see cref="ManuscriptViewModel"/>.
/// </summary>
public class ManuscriptViewModelTests
{
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<IEditorConfigurationService> _configServiceMock;
    private readonly ILogger<ManuscriptViewModel> _logger;
    private readonly ManuscriptViewModel _sut;

    public ManuscriptViewModelTests()
    {
        _editorServiceMock = new Mock<IEditorService>();
        _configServiceMock = new Mock<IEditorConfigurationService>();
        _configServiceMock.Setup(x => x.GetSettings()).Returns(new EditorSettings());
        _logger = NullLogger<ManuscriptViewModel>.Instance;
        
        _sut = new ManuscriptViewModel(
            _editorServiceMock.Object,
            _configServiceMock.Object,
            _logger);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_SetsAllProperties()
    {
        // Arrange
        var id = "test-doc-1";
        var title = "Test Document";
        var filePath = "/path/to/file.md";
        var content = "Hello, world!";
        var encoding = System.Text.Encoding.UTF8;

        // Act
        _sut.Initialize(id, title, filePath, content, encoding);

        // Assert
        _sut.DocumentId.Should().Be(id);
        _sut.Title.Should().Be(title);
        _sut.FilePath.Should().Be(filePath);
        _sut.Content.Should().Be(content);
        _sut.Encoding.Should().Be(encoding);
        _sut.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void Initialize_WithNullFilePath_IsUntitledDocument()
    {
        // Arrange & Act
        _sut.Initialize("id", "Untitled", null, "", System.Text.Encoding.UTF8);

        // Assert
        _sut.FilePath.Should().BeNull();
        _sut.FileExtension.Should().BeEmpty();
    }

    #endregion

    #region Content and Dirty State Tests

    [Fact]
    public void Content_WhenChanged_MarksDirty()
    {
        // Arrange
        _sut.Initialize("id", "Test", "/path.md", "", System.Text.Encoding.UTF8);

        // Act
        _sut.Content = "New content";

        // Assert
        _sut.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void Content_WhenUnchanged_DoesNotMarkDirty()
    {
        // Arrange
        _sut.Initialize("id", "Test", "/path.md", "Original", System.Text.Encoding.UTF8);

        // Act
        _sut.Content = "Original"; // Same content

        // Assert
        _sut.IsDirty.Should().BeFalse();
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void LineCount_ReturnsCorrectCount()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Line 1\nLine 2\nLine 3", System.Text.Encoding.UTF8);

        // Act & Assert
        _sut.LineCount.Should().Be(3);
    }

    [Fact]
    public void LineCount_EmptyDocument_ReturnsOne()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "", System.Text.Encoding.UTF8);

        // Act & Assert
        _sut.LineCount.Should().Be(1);
    }

    [Fact]
    public void WordCount_ReturnsCorrectCount()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Hello world! This is a test.", System.Text.Encoding.UTF8);

        // Act & Assert
        _sut.WordCount.Should().Be(6);
    }

    [Fact]
    public void WordCount_EmptyDocument_ReturnsZero()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "", System.Text.Encoding.UTF8);

        // Act & Assert
        _sut.WordCount.Should().Be(0);
    }

    [Fact]
    public void CharacterCount_ReturnsCorrectCount()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Hello!", System.Text.Encoding.UTF8);

        // Act & Assert
        _sut.CharacterCount.Should().Be(6);
    }

    #endregion

    #region Settings Binding Tests

    [Fact]
    public void ShowLineNumbers_ReturnsValueFromConfigService()
    {
        // Arrange
        var settings = new EditorSettings { ShowLineNumbers = true };
        _configServiceMock.Setup(x => x.GetSettings()).Returns(settings);

        // Act & Assert
        _sut.ShowLineNumbers.Should().BeTrue();
    }

    [Fact]
    public void WordWrap_ReturnsValueFromConfigService()
    {
        // Arrange
        var settings = new EditorSettings { WordWrap = false };
        _configServiceMock.Setup(x => x.GetSettings()).Returns(settings);

        // Act & Assert
        _sut.WordWrap.Should().BeFalse();
    }

    [Fact]
    public void FontFamily_ReturnsValueFromConfigService()
    {
        // Arrange
        var settings = new EditorSettings { FontFamily = "JetBrains Mono" };
        _configServiceMock.Setup(x => x.GetSettings()).Returns(settings);

        // Act & Assert
        _sut.FontFamily.Should().Be("JetBrains Mono");
    }

    #endregion

    #region Selection and Caret Tests

    [Fact]
    public void Select_SetsSelectionCorrectly()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Hello, world!", System.Text.Encoding.UTF8);

        // Act
        _sut.Select(7, 5); // Select "world"

        // Assert
        _sut.Selection.StartOffset.Should().Be(7);
        _sut.Selection.EndOffset.Should().Be(12);
        _sut.Selection.Length.Should().Be(5);
        _sut.Selection.SelectedText.Should().Be("world");
    }

    [Fact]
    public void UpdateCaretPosition_SetsCaretPositionCorrectly()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Line 1\nLine 2", System.Text.Encoding.UTF8);

        // Act
        _sut.UpdateCaretPosition(2, 3, 10);

        // Assert
        _sut.CaretPosition.Line.Should().Be(2);
        _sut.CaretPosition.Column.Should().Be(3);
        _sut.CaretPosition.Offset.Should().Be(10);
    }

    [Fact]
    public void ScrollToLine_UpdatesCaretPosition()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Line 1\nLine 2\nLine 3", System.Text.Encoding.UTF8);

        // Act
        _sut.ScrollToLine(2);

        // Assert
        _sut.CaretPosition.Line.Should().Be(2);
        _sut.CaretPosition.Column.Should().Be(1);
    }

    #endregion

    #region Insert Text Tests

    [Fact]
    public void InsertText_InsertsAtCaretPosition()
    {
        // Arrange
        _sut.Initialize("id", "Test", null, "Hello!", System.Text.Encoding.UTF8);
        _sut.UpdateCaretPosition(1, 6, 5); // After "Hello"

        // Act
        _sut.InsertText(", world");

        // Assert
        _sut.Content.Should().Be("Hello, world!");
        _sut.IsDirty.Should().BeTrue();
    }

    #endregion

    #region FileExtension Tests

    [Fact]
    public void FileExtension_ReturnsLowercaseExtension()
    {
        // Arrange
        _sut.Initialize("id", "Test", "/path/to/File.MD", "", System.Text.Encoding.UTF8);

        // Act & Assert
        _sut.FileExtension.Should().Be(".md");
    }

    #endregion
}
