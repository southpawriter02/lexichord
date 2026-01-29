using Avalonia.Controls;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.ViewModels;

/// <summary>
/// Unit tests for SettingsViewModel.
/// </summary>
public class SettingsViewModelTests
{
    private readonly Mock<ISettingsPageRegistry> _registryMock = new();
    private readonly Mock<ILicenseContext> _licenseContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<SettingsViewModel>> _loggerMock = new();

    private SettingsViewModel CreateViewModel() =>
        new(_registryMock.Object, _licenseContextMock.Object, _mediatorMock.Object, _loggerMock.Object);

    private static Mock<ISettingsPage> CreateMockPage(
        string categoryId = "test.page",
        string displayName = "Test Page",
        string? parentCategoryId = null,
        int sortOrder = 100)
    {
        var mock = new Mock<ISettingsPage>();
        mock.Setup(p => p.CategoryId).Returns(categoryId);
        mock.Setup(p => p.DisplayName).Returns(displayName);
        mock.Setup(p => p.ParentCategoryId).Returns(parentCategoryId);
        mock.Setup(p => p.SortOrder).Returns(sortOrder);
        mock.Setup(p => p.RequiredTier).Returns(LicenseTier.Core);
        mock.Setup(p => p.SearchKeywords).Returns(Array.Empty<string>());
        mock.Setup(p => p.CreateView()).Returns(new TextBlock { Text = categoryId });
        return mock;
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_BuildsCategoryTree()
    {
        // Arrange
        var page = CreateMockPage("root", "Root").Object;
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages("root"))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();

        // Assert
        Assert.Single(viewModel.Categories);
        Assert.Equal("root", viewModel.Categories[0].CategoryId);
    }

    [Fact]
    public void Initialize_WithInitialCategory_NavigatesToIt()
    {
        // Arrange
        var page1 = CreateMockPage("page1", "Page 1").Object;
        var page2 = CreateMockPage("page2", "Page 2").Object;
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page1, page2 });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page1, page2 });
        _registryMock.Setup(r => r.GetChildPages(It.IsAny<string>()))
            .Returns(new List<ISettingsPage>());
        _registryMock.Setup(r => r.GetPage("page2")).Returns(page2);
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        var options = new SettingsWindowOptions(InitialCategoryId: "page2");

        // Act
        viewModel.Initialize(options);

        // Assert
        Assert.NotNull(viewModel.SelectedPage);
        Assert.Equal("page2", viewModel.SelectedPage.CategoryId);
    }

    [Fact]
    public void Initialize_WithSearchQuery_SetsSearchQuery()
    {
        // Arrange
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage>());
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        var options = new SettingsWindowOptions(SearchQuery: "editor");

        // Act
        viewModel.Initialize(options);

        // Assert
        Assert.Equal("editor", viewModel.SearchQuery);
    }

    [Fact]
    public void Initialize_SelectsFirstCategoryByDefault()
    {
        // Arrange
        var page1 = CreateMockPage("page1", "Page 1").Object;
        var page2 = CreateMockPage("page2", "Page 2").Object;
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page1, page2 });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page1, page2 });
        _registryMock.Setup(r => r.GetChildPages(It.IsAny<string>()))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();

        // Assert
        Assert.NotNull(viewModel.SelectedCategory);
        Assert.Equal("page1", viewModel.SelectedCategory.CategoryId);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void NavigateTo_ValidCategory_SelectsPage()
    {
        // Arrange
        var page1 = CreateMockPage("page1", "Page 1").Object;
        var page2 = CreateMockPage("page2", "Page 2").Object;
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page1, page2 });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page1, page2 });
        _registryMock.Setup(r => r.GetChildPages(It.IsAny<string>()))
            .Returns(new List<ISettingsPage>());
        _registryMock.Setup(r => r.GetPage("page2")).Returns(page2);
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        viewModel.Initialize();

        // Act
        viewModel.NavigateTo("page2");

        // Assert
        Assert.NotNull(viewModel.SelectedPage);
        Assert.Equal("page2", viewModel.SelectedPage.CategoryId);
    }

    [Fact]
    public void NavigateTo_InvalidCategory_DoesNothing()
    {
        // Arrange
        var page = CreateMockPage("page1", "Page 1").Object;
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(It.IsAny<string>()))
            .Returns(new List<ISettingsPage>());
        _registryMock.Setup(r => r.GetPage("nonexistent")).Returns((ISettingsPage?)null);
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        viewModel.Initialize();
        var originalSelection = viewModel.SelectedPage;

        // Act
        viewModel.NavigateTo("nonexistent");

        // Assert - selection should not change
        Assert.Equal(originalSelection, viewModel.SelectedPage);
    }

    #endregion

    #region Page Loading Tests

    [Fact]
    public void SelectingPage_LoadsContent()
    {
        // Arrange
        var page = CreateMockPage("page", "Page").Object;
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(It.IsAny<string>()))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        viewModel.Initialize();

        // Assert
        Assert.NotNull(viewModel.CurrentPageContent);
        Assert.IsType<TextBlock>(viewModel.CurrentPageContent);
    }

    [Fact]
    public void SelectingPage_CreateViewThrows_ShowsErrorView()
    {
        // Arrange
        var pageMock = CreateMockPage("error", "Error Page");
        pageMock.Setup(p => p.CreateView()).Throws(new InvalidOperationException("Test error"));
        var page = pageMock.Object;

        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { page });
        _registryMock.Setup(r => r.GetChildPages(It.IsAny<string>()))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();

        // Assert - should show error view instead of crashing
        Assert.NotNull(viewModel.CurrentPageContent);
    }

    #endregion

    #region Hierarchy Tests

    [Fact]
    public void Initialize_BuildsHierarchicalTree()
    {
        // Arrange
        var parent = CreateMockPage("parent", "Parent", parentCategoryId: null).Object;
        var child = CreateMockPage("child", "Child", parentCategoryId: "parent").Object;

        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage> { parent, child });
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage> { parent });
        _registryMock.Setup(r => r.GetChildPages("parent"))
            .Returns(new List<ISettingsPage> { child });
        _registryMock.Setup(r => r.GetChildPages("child"))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();

        // Act
        viewModel.Initialize();

        // Assert
        Assert.Single(viewModel.Categories);
        Assert.Equal("parent", viewModel.Categories[0].CategoryId);
        Assert.Single(viewModel.Categories[0].Children);
        Assert.Equal("child", viewModel.Categories[0].Children[0].CategoryId);
    }

    #endregion

    #region Close Tests

    [Fact]
    public async Task CloseAsync_PublishesSettingsClosedEvent()
    {
        // Arrange
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage>());
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        viewModel.Initialize();

        // Act
        await viewModel.CloseAsync();

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<Abstractions.Events.SettingsClosedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CloseAsync_InvokesCloseWindowAction()
    {
        // Arrange
        _registryMock.Setup(r => r.GetPages(It.IsAny<LicenseTier>()))
            .Returns(new List<ISettingsPage>());
        _registryMock.Setup(r => r.GetChildPages(null))
            .Returns(new List<ISettingsPage>());
        _licenseContextMock.Setup(l => l.CurrentTier).Returns(LicenseTier.Core);

        var viewModel = CreateViewModel();
        viewModel.Initialize();
        var closeActionCalled = false;
        viewModel.CloseWindowAction = () => closeActionCalled = true;

        // Act
        await viewModel.CloseAsync();

        // Assert
        Assert.True(closeActionCalled);
    }

    #endregion
}
