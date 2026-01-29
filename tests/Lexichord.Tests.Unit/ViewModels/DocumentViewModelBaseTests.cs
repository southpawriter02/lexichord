using FluentAssertions;
using Lexichord.Abstractions.Layout;
using Lexichord.Abstractions.Services;
using Lexichord.Abstractions.ViewModels;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.ViewModels;

/// <summary>
/// Unit tests for <see cref="DocumentViewModelBase"/>.
/// </summary>
public class DocumentViewModelBaseTests
{
    /// <summary>
    /// Test implementation of DocumentViewModelBase.
    /// </summary>
    private sealed class TestDocumentViewModel : DocumentViewModelBase
    {
        public TestDocumentViewModel(ISaveDialogService? saveDialogService = null)
            : base(saveDialogService)
        {
        }

        public override string DocumentId => "test-doc-id";
        public override string Title => "Test Document";
        
        public bool SaveWasCalled { get; private set; }
        public bool SaveShouldSucceed { get; set; } = true;
        
        public override Task<bool> SaveAsync()
        {
            SaveWasCalled = true;
            if (SaveShouldSucceed)
            {
                IsDirty = false;
            }
            return Task.FromResult(SaveShouldSucceed);
        }
        
        public void SetDirty() => MarkDirty();
        public void SetClean() => MarkClean();
    }

    [Fact]
    public void DisplayTitle_WhenClean_ReturnsTitle()
    {
        // Arrange
        var sut = new TestDocumentViewModel();

        // Act
        var result = sut.DisplayTitle;

        // Assert
        result.Should().Be("Test Document");
    }

    [Fact]
    public void DisplayTitle_WhenDirty_ReturnsTitleWithAsterisk()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        sut.SetDirty();

        // Act
        var result = sut.DisplayTitle;

        // Assert
        result.Should().Be("Test Document*");
    }

    [Fact]
    public void MarkDirty_SetsIsDirtyTrue()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        sut.IsDirty.Should().BeFalse();

        // Act
        sut.SetDirty();

        // Assert
        sut.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void MarkClean_SetsIsDirtyFalse()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        sut.SetDirty();
        sut.IsDirty.Should().BeTrue();

        // Act
        sut.SetClean();

        // Assert
        sut.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void CanClose_ByDefault_ReturnsTrue()
    {
        // Arrange
        var sut = new TestDocumentViewModel();

        // Act & Assert
        sut.CanClose.Should().BeTrue();
    }

    [Fact]
    public async Task CanCloseAsync_WhenClean_ReturnsTrue()
    {
        // Arrange
        var sut = new TestDocumentViewModel();

        // Act
        var result = await sut.CanCloseAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanCloseAsync_WhenDirtyAndNoDialogService_ReturnsFalse()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        sut.SetDirty();

        // Act
        var result = await sut.CanCloseAsync();

        // Assert
        result.Should().BeFalse();
        sut.SaveWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task CanCloseAsync_WhenDirtyAndUserClicksSave_CallsSaveAndReturnsResult()
    {
        // Arrange
        var mockDialogService = new Mock<ISaveDialogService>();
        mockDialogService
            .Setup(x => x.ShowSaveDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(SaveDialogResult.Save);

        var sut = new TestDocumentViewModel(mockDialogService.Object);
        sut.SetDirty();

        // Act
        var result = await sut.CanCloseAsync();

        // Assert
        result.Should().BeTrue();
        sut.SaveWasCalled.Should().BeTrue();
        sut.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task CanCloseAsync_WhenDirtyAndUserClicksSaveButSaveFails_ReturnsFalse()
    {
        // Arrange
        var mockDialogService = new Mock<ISaveDialogService>();
        mockDialogService
            .Setup(x => x.ShowSaveDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(SaveDialogResult.Save);

        var sut = new TestDocumentViewModel(mockDialogService.Object);
        sut.SetDirty();
        sut.SaveShouldSucceed = false;

        // Act
        var result = await sut.CanCloseAsync();

        // Assert
        result.Should().BeFalse();
        sut.SaveWasCalled.Should().BeTrue();
        sut.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task CanCloseAsync_WhenDirtyAndUserClicksDontSave_ReturnsTrueWithoutSaving()
    {
        // Arrange
        var mockDialogService = new Mock<ISaveDialogService>();
        mockDialogService
            .Setup(x => x.ShowSaveDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(SaveDialogResult.DontSave);

        var sut = new TestDocumentViewModel(mockDialogService.Object);
        sut.SetDirty();

        // Act
        var result = await sut.CanCloseAsync();

        // Assert
        result.Should().BeTrue();
        sut.SaveWasCalled.Should().BeFalse();
        sut.IsDirty.Should().BeTrue(); // Still dirty since we didn't save
    }

    [Fact]
    public async Task CanCloseAsync_WhenDirtyAndUserClicksCancel_ReturnsFalse()
    {
        // Arrange
        var mockDialogService = new Mock<ISaveDialogService>();
        mockDialogService
            .Setup(x => x.ShowSaveDialogAsync(It.IsAny<string>()))
            .ReturnsAsync(SaveDialogResult.Cancel);

        var sut = new TestDocumentViewModel(mockDialogService.Object);
        sut.SetDirty();

        // Act
        var result = await sut.CanCloseAsync();

        // Assert
        result.Should().BeFalse();
        sut.SaveWasCalled.Should().BeFalse();
    }

    [Fact]
    public void StateChanged_WhenIsDirtyChanges_RaisesEvent()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        DocumentStateChangedEventArgs? capturedArgs = null;
        sut.StateChanged += (_, args) => capturedArgs = args;

        // Act
        sut.SetDirty();

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.PropertyName.Should().Be(nameof(IDocumentTab.IsDirty));
        capturedArgs.OldValue.Should().Be(false);
        capturedArgs.NewValue.Should().Be(true);
    }

    [Fact]
    public void StateChanged_WhenIsPinnedChanges_RaisesEvent()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        DocumentStateChangedEventArgs? capturedArgs = null;
        sut.StateChanged += (_, args) => capturedArgs = args;

        // Act
        sut.IsPinned = true;

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.PropertyName.Should().Be(nameof(IDocumentTab.IsPinned));
        capturedArgs.OldValue.Should().Be(false);
        capturedArgs.NewValue.Should().Be(true);
    }

    [Fact]
    public void PropertyChanged_WhenIsDirtyChanges_DisplayTitleAlsoChanges()
    {
        // Arrange
        var sut = new TestDocumentViewModel();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != null)
                changedProperties.Add(args.PropertyName);
        };

        // Act
        sut.SetDirty();

        // Assert
        changedProperties.Should().Contain(nameof(DocumentViewModelBase.IsDirty));
        changedProperties.Should().Contain(nameof(DocumentViewModelBase.DisplayTitle));
    }
}
