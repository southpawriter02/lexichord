using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for SettingsPageRegistry.
/// </summary>
public class SettingsPageRegistryTests
{
    private readonly Mock<ILogger<SettingsPageRegistry>> _loggerMock = new();

    private SettingsPageRegistry CreateRegistry() => new(_loggerMock.Object);

    private static Mock<ISettingsPage> CreateMockPage(
        string categoryId = "test.page",
        string displayName = "Test Page",
        string? parentCategoryId = null,
        int sortOrder = 100,
        LicenseTier requiredTier = LicenseTier.Core)
    {
        var mock = new Mock<ISettingsPage>();
        mock.Setup(p => p.CategoryId).Returns(categoryId);
        mock.Setup(p => p.DisplayName).Returns(displayName);
        mock.Setup(p => p.ParentCategoryId).Returns(parentCategoryId);
        mock.Setup(p => p.SortOrder).Returns(sortOrder);
        mock.Setup(p => p.RequiredTier).Returns(requiredTier);
        mock.Setup(p => p.SearchKeywords).Returns(Array.Empty<string>());
        return mock;
    }

    #region Registration Tests

    [Fact]
    public void RegisterPage_ValidPage_AddsToRegistry()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage().Object;

        // Act
        registry.RegisterPage(page);

        // Assert
        var pages = registry.GetPages();
        Assert.Single(pages);
        Assert.Equal(page, pages[0]);
    }

    [Fact]
    public void RegisterPage_NullPage_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.RegisterPage(null!));
    }

    [Fact]
    public void RegisterPage_EmptyCategoryId_ThrowsArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(categoryId: "").Object;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => registry.RegisterPage(page));
        Assert.Contains("CategoryId", ex.Message);
    }

    [Fact]
    public void RegisterPage_EmptyDisplayName_ThrowsArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(displayName: "").Object;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => registry.RegisterPage(page));
        Assert.Contains("DisplayName", ex.Message);
    }

    [Fact]
    public void RegisterPage_DuplicateCategoryId_ThrowsArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();
        var page1 = CreateMockPage(categoryId: "duplicate").Object;
        var page2 = CreateMockPage(categoryId: "duplicate").Object;
        registry.RegisterPage(page1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => registry.RegisterPage(page2));
        Assert.Contains("duplicate", ex.Message);
    }

    [Fact]
    public void RegisterPage_CaseInsensitiveDuplicateCheck()
    {
        // Arrange
        var registry = CreateRegistry();
        var page1 = CreateMockPage(categoryId: "Test.Page").Object;
        var page2 = CreateMockPage(categoryId: "TEST.PAGE").Object;
        registry.RegisterPage(page1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.RegisterPage(page2));
    }

    [Fact]
    public void RegisterPage_RaisesPageRegisteredEvent()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage().Object;
        SettingsPageEventArgs? eventArgs = null;
        registry.PageRegistered += (s, e) => eventArgs = e;

        // Act
        registry.RegisterPage(page);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(page, eventArgs.Page);
    }

    [Fact]
    public void RegisterPage_SortsBySortOrderThenDisplayName()
    {
        // Arrange
        var registry = CreateRegistry();
        var page1 = CreateMockPage("page.c", "C Page", sortOrder: 100).Object;
        var page2 = CreateMockPage("page.a", "A Page", sortOrder: 50).Object;
        var page3 = CreateMockPage("page.b", "B Page", sortOrder: 100).Object;

        // Act
        registry.RegisterPage(page1);
        registry.RegisterPage(page2);
        registry.RegisterPage(page3);

        // Assert
        var pages = registry.GetPages();
        Assert.Equal(3, pages.Count);
        Assert.Equal("page.a", pages[0].CategoryId); // SortOrder 50
        Assert.Equal("page.b", pages[1].CategoryId); // SortOrder 100, "B" before "C"
        Assert.Equal("page.c", pages[2].CategoryId); // SortOrder 100, "C" after "B"
    }

    #endregion

    #region Unregistration Tests

    [Fact]
    public void UnregisterPage_ExistingPage_ReturnsTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(categoryId: "test").Object;
        registry.RegisterPage(page);

        // Act
        var result = registry.UnregisterPage("test");

        // Assert
        Assert.True(result);
        Assert.Empty(registry.GetPages());
    }

    [Fact]
    public void UnregisterPage_NonExistentPage_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.UnregisterPage("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnregisterPage_NullCategoryId_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.UnregisterPage(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnregisterPage_RaisesPageUnregisteredEvent()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(categoryId: "test").Object;
        registry.RegisterPage(page);
        SettingsPageEventArgs? eventArgs = null;
        registry.PageUnregistered += (s, e) => eventArgs = e;

        // Act
        registry.UnregisterPage("test");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(page, eventArgs.Page);
    }

    [Fact]
    public void UnregisterPage_CaseInsensitive()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(categoryId: "Test.Page").Object;
        registry.RegisterPage(page);

        // Act
        var result = registry.UnregisterPage("TEST.PAGE");

        // Assert
        Assert.True(result);
        Assert.Empty(registry.GetPages());
    }

    #endregion

    #region Retrieval Tests

    [Fact]
    public void GetPages_ReturnsAllPages()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("page1", "Page 1").Object);
        registry.RegisterPage(CreateMockPage("page2", "Page 2").Object);

        // Act
        var pages = registry.GetPages();

        // Assert
        Assert.Equal(2, pages.Count);
    }

    [Fact]
    public void GetPages_WithTier_FiltersByTier()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("core", "Core", requiredTier: LicenseTier.Core).Object);
        registry.RegisterPage(CreateMockPage("pro", "Pro", requiredTier: LicenseTier.WriterPro).Object);

        // Act
        var corePages = registry.GetPages(LicenseTier.Core);
        var proPages = registry.GetPages(LicenseTier.WriterPro);

        // Assert
        Assert.Single(corePages);
        Assert.Equal("core", corePages[0].CategoryId);
        Assert.Equal(2, proPages.Count);
    }

    [Fact]
    public void GetPage_ExistingId_ReturnsPage()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(categoryId: "find.me").Object;
        registry.RegisterPage(page);

        // Act
        var result = registry.GetPage("find.me");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(page, result);
    }

    [Fact]
    public void GetPage_NonExistentId_ReturnsNull()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.GetPage("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPage_CaseInsensitive()
    {
        // Arrange
        var registry = CreateRegistry();
        var page = CreateMockPage(categoryId: "Test.Page").Object;
        registry.RegisterPage(page);

        // Act
        var result = registry.GetPage("TEST.PAGE");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(page, result);
    }

    [Fact]
    public void GetChildPages_RootPages_ReturnsNullParentPages()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("root1", "Root 1", parentCategoryId: null).Object);
        registry.RegisterPage(CreateMockPage("root2", "Root 2", parentCategoryId: null).Object);
        registry.RegisterPage(CreateMockPage("child", "Child", parentCategoryId: "root1").Object);

        // Act
        var rootPages = registry.GetChildPages(null);

        // Assert
        Assert.Equal(2, rootPages.Count);
        Assert.All(rootPages, p => Assert.Null(p.ParentCategoryId));
    }

    [Fact]
    public void GetChildPages_SpecificParent_ReturnsChildren()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("parent", "Parent").Object);
        registry.RegisterPage(CreateMockPage("child1", "Child 1", parentCategoryId: "parent").Object);
        registry.RegisterPage(CreateMockPage("child2", "Child 2", parentCategoryId: "parent").Object);
        registry.RegisterPage(CreateMockPage("other", "Other").Object);

        // Act
        var children = registry.GetChildPages("parent");

        // Assert
        Assert.Equal(2, children.Count);
        Assert.All(children, c => Assert.Equal("parent", c.ParentCategoryId));
    }

    #endregion

    #region Search Tests

    [Fact]
    public void SearchPages_MatchesDisplayName()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("page1", "Editor Settings").Object);
        registry.RegisterPage(CreateMockPage("page2", "Theme Options").Object);

        // Act
        var results = registry.SearchPages("editor", LicenseTier.Core);

        // Assert
        Assert.Single(results);
        Assert.Equal("page1", results[0].CategoryId);
    }

    [Fact]
    public void SearchPages_MatchesCategoryId()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("editor.fonts", "Fonts").Object);
        registry.RegisterPage(CreateMockPage("theme.colors", "Colors").Object);

        // Act
        var results = registry.SearchPages("fonts", LicenseTier.Core);

        // Assert
        Assert.Single(results);
        Assert.Equal("editor.fonts", results[0].CategoryId);
    }

    [Fact]
    public void SearchPages_MatchesKeywords()
    {
        // Arrange
        var registry = CreateRegistry();
        var pageMock = CreateMockPage("page1", "Editor");
        pageMock.Setup(p => p.SearchKeywords).Returns(new[] { "typeface", "typography" });
        registry.RegisterPage(pageMock.Object);

        // Act
        var results = registry.SearchPages("typography", LicenseTier.Core);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public void SearchPages_RespectsTierFilter()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("core", "Core Editor", requiredTier: LicenseTier.Core).Object);
        registry.RegisterPage(CreateMockPage("pro", "Pro Editor", requiredTier: LicenseTier.WriterPro).Object);

        // Act
        var coreResults = registry.SearchPages("editor", LicenseTier.Core);
        var proResults = registry.SearchPages("editor", LicenseTier.WriterPro);

        // Assert
        Assert.Single(coreResults);
        Assert.Equal("core", coreResults[0].CategoryId);
        Assert.Equal(2, proResults.Count);
    }

    [Fact]
    public void SearchPages_EmptyQuery_ReturnsAllFiltered()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("page1", "Page 1").Object);
        registry.RegisterPage(CreateMockPage("page2", "Page 2").Object);

        // Act
        var results = registry.SearchPages("", LicenseTier.Core);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void SearchPages_CaseInsensitive()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.RegisterPage(CreateMockPage("page", "UPPERCASE").Object);

        // Act
        var results = registry.SearchPages("uppercase", LicenseTier.Core);

        // Assert
        Assert.Single(results);
    }

    #endregion
}
