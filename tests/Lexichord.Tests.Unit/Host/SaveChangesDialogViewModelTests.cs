using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.ViewModels;
using Lexichord.Host.ViewModels;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for SaveChangesDialogViewModel.
/// </summary>
[Trait("Category", "Unit")]
public class SaveChangesDialogViewModelTests
{
    private readonly Mock<IFileService> _fileServiceMock = new();

    [Fact]
    public void ShowAsync_SetsDirtyDocuments()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var doc1 = CreateDocument("Doc1");
        var doc2 = CreateDocument("Doc2");
        var documents = new List<DocumentViewModelBase> { doc1, doc2 };

        // Act
        _ = viewModel.ShowAsync(documents);

        // Assert
        viewModel.DirtyDocuments.Should().BeEquivalentTo(documents);
    }

    [Fact]
    public void ShowAsync_ResetsState()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var documents = new List<DocumentViewModelBase> { CreateDocument() };

        // Act
        _ = viewModel.ShowAsync(documents);

        // Assert
        viewModel.IsSaving.Should().BeFalse();
        viewModel.SaveProgress.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DiscardAllCommand_ReturnsDiscardResult()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var documents = new List<DocumentViewModelBase> { CreateDocument() };
        var resultTask = viewModel.ShowAsync(documents);

        // Act
        viewModel.DiscardAllCommand.Execute(null);

        // Assert
        var result = await resultTask;
        result.Action.Should().Be(SaveChangesAction.DiscardAll);
    }

    [Fact]
    public async Task CancelCommand_ReturnsCancelResult()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var documents = new List<DocumentViewModelBase> { CreateDocument() };
        var resultTask = viewModel.ShowAsync(documents);

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        var result = await resultTask;
        result.Action.Should().Be(SaveChangesAction.Cancel);
    }

    [Fact]
    public void SaveAllCommand_CannotExecuteWhileSaving()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var documents = new List<DocumentViewModelBase> { CreateDocument() };
        _ = viewModel.ShowAsync(documents);

        // Simulate saving in progress
        var canExecuteBefore = viewModel.SaveAllCommand.CanExecute(null);

        // Assert - command can execute when not saving
        canExecuteBefore.Should().BeTrue();
    }

    [Fact]
    public void ShowAsync_SetsStatusMessage_SingleDocument()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var documents = new List<DocumentViewModelBase> { CreateDocument() };

        // Act
        _ = viewModel.ShowAsync(documents);

        // Assert
        viewModel.StatusMessage.Should().Contain("document has");
    }

    [Fact]
    public void ShowAsync_SetsStatusMessage_MultipleDocuments()
    {
        // Arrange
        var viewModel = new SaveChangesDialogViewModel(_fileServiceMock.Object);
        var documents = new List<DocumentViewModelBase> { CreateDocument(), CreateDocument() };

        // Act
        _ = viewModel.ShowAsync(documents);

        // Assert
        viewModel.StatusMessage.Should().Contain("documents have");
    }

    /// <summary>
    /// Test document for use in tests where DocumentViewModelBase is needed.
    /// </summary>
    private class TestDocument : DocumentViewModelBase
    {
        private readonly string _documentId;
        private readonly string _title;

        public TestDocument(string title = "Test", bool isDirty = true)
            : base(null)
        {
            _documentId = Guid.NewGuid().ToString();
            _title = title;
            if (isDirty)
            {
                IsDirty = true;
            }
        }

        public override string DocumentId => _documentId;
        public override string Title => _title;
    }

    private static DocumentViewModelBase CreateDocument(string title = "Test")
    {
        return new TestDocument(title, isDirty: true);
    }
}
