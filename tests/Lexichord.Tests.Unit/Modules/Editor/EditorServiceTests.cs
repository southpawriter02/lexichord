using System.Text;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.Services;
using Lexichord.Modules.Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Editor;

/// <summary>
/// Unit tests for <see cref="EditorService"/>.
/// </summary>
public class EditorServiceTests : IDisposable
{
    private readonly Mock<IEditorConfigurationService> _configServiceMock;
    private readonly IServiceProvider _serviceProvider;
    private readonly EditorService _sut;
    private readonly string _testFilePath;

    public EditorServiceTests()
    {
        _configServiceMock = new Mock<IEditorConfigurationService>();
        _configServiceMock.Setup(x => x.GetSettings()).Returns(new EditorSettings());

        var services = new ServiceCollection();
        services.AddSingleton(_configServiceMock.Object);
        services.AddSingleton<ILogger<ManuscriptViewModel>>(NullLogger<ManuscriptViewModel>.Instance);
        services.AddSingleton<ILogger<EditorService>>(NullLogger<EditorService>.Instance);
        services.AddSingleton<IEditorService, EditorService>();
        _serviceProvider = services.BuildServiceProvider();

        _sut = new EditorService(
            _serviceProvider,
            _configServiceMock.Object,
            NullLogger<EditorService>.Instance);

        // Create test file
        _testFilePath = Path.Combine(Path.GetTempPath(), $"editor-test-{Guid.NewGuid()}.txt");
        File.WriteAllText(_testFilePath, "Test content\nLine 2\nLine 3", Encoding.UTF8);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    #region CreateDocumentAsync Tests

    [Fact]
    public async Task CreateDocumentAsync_WithNoTitle_ReturnsUntitledDocument()
    {
        // Act
        var result = await _sut.CreateDocumentAsync();

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().StartWith("Untitled-");
        result.FilePath.Should().BeNull();
        result.Content.Should().BeEmpty();
        result.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDocumentAsync_WithTitle_UsesProvidedTitle()
    {
        // Act
        var result = await _sut.CreateDocumentAsync("My Document");

        // Assert
        result.Title.Should().Be("My Document");
    }

    [Fact]
    public async Task CreateDocumentAsync_MultipleCalls_IncrementsUntitledNumber()
    {
        // Act
        var doc1 = await _sut.CreateDocumentAsync();
        var doc2 = await _sut.CreateDocumentAsync();

        // Assert
        doc1.Title.Should().NotBe(doc2.Title);
    }

    [Fact]
    public async Task CreateDocumentAsync_AddsToOpenDocuments()
    {
        // Act
        var doc = await _sut.CreateDocumentAsync();

        // Assert
        _sut.GetOpenDocuments().Should().Contain(doc);
    }

    #endregion

    #region OpenDocumentAsync Tests

    [Fact]
    public async Task OpenDocumentAsync_ValidFile_ReturnsDocumentWithContent()
    {
        // Act
        var result = await _sut.OpenDocumentAsync(_testFilePath);

        // Assert
        result.Should().NotBeNull();
        result.FilePath.Should().Be(_testFilePath);
        result.Content.Should().Contain("Test content");
        result.LineCount.Should().Be(3);
        result.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task OpenDocumentAsync_ValidFile_SetsCorrectTitle()
    {
        // Act
        var result = await _sut.OpenDocumentAsync(_testFilePath);

        // Assert
        result.Title.Should().Be(Path.GetFileName(_testFilePath));
    }

    [Fact]
    public async Task OpenDocumentAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Act
        var act = () => _sut.OpenDocumentAsync("/nonexistent/file.txt");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task OpenDocumentAsync_AlreadyOpen_ReturnsSameInstance()
    {
        // Arrange
        var first = await _sut.OpenDocumentAsync(_testFilePath);

        // Act
        var second = await _sut.OpenDocumentAsync(_testFilePath);

        // Assert
        second.Should().BeSameAs(first);
    }

    #endregion

    #region SaveDocumentAsync Tests

    [Fact]
    public async Task SaveDocumentAsync_WithFilePath_WritesToDisk()
    {
        // Arrange
        var doc = await _sut.OpenDocumentAsync(_testFilePath);
        doc.Content = "Modified content";

        // Act
        var result = await _sut.SaveDocumentAsync(doc);

        // Assert
        result.Should().BeTrue();
        var savedContent = await File.ReadAllTextAsync(_testFilePath);
        savedContent.Should().Be("Modified content");
    }

    [Fact]
    public async Task SaveDocumentAsync_UntitledDocument_ReturnsFalse()
    {
        // Arrange
        var doc = await _sut.CreateDocumentAsync();
        doc.Content = "Some content";

        // Act
        var result = await _sut.SaveDocumentAsync(doc);

        // Assert
        result.Should().BeFalse(); // No file path set
    }

    #endregion

    #region GetOpenDocuments Tests

    [Fact]
    public async Task GetOpenDocuments_ReturnsAllOpenDocuments()
    {
        // Arrange
        var doc1 = await _sut.CreateDocumentAsync();
        var doc2 = await _sut.CreateDocumentAsync();

        // Act
        var openDocs = _sut.GetOpenDocuments();

        // Assert
        openDocs.Should().HaveCount(2);
        openDocs.Should().Contain(doc1);
        openDocs.Should().Contain(doc2);
    }

    #endregion

    #region GetDocumentByPath Tests

    [Fact]
    public async Task GetDocumentByPath_ExistingDocument_ReturnsDocument()
    {
        // Arrange
        var doc = await _sut.OpenDocumentAsync(_testFilePath);

        // Act
        var result = _sut.GetDocumentByPath(_testFilePath);

        // Assert
        result.Should().BeSameAs(doc);
    }

    [Fact]
    public void GetDocumentByPath_NonexistentPath_ReturnsNull()
    {
        // Act
        var result = _sut.GetDocumentByPath("/nonexistent/path.txt");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CloseDocumentAsync Tests

    [Fact]
    public async Task CloseDocumentAsync_CleanDocument_RemovesFromOpenDocuments()
    {
        // Arrange
        var doc = await _sut.CreateDocumentAsync();

        // Act
        var result = await _sut.CloseDocumentAsync(doc);

        // Assert
        result.Should().BeTrue();
        _sut.GetOpenDocuments().Should().NotContain(doc);
    }

    #endregion
}
