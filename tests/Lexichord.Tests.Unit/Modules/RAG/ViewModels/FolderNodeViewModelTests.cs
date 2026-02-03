// =============================================================================
// File: FolderNodeViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for FolderNodeViewModel folder tree node behavior.
// =============================================================================
// LOGIC: Verifies all FolderNodeViewModel functionality:
//   - Constructor null-parameter validation (2 parameters).
//   - Property initialization: Name, Path, IsSelected, IsExpanded, Children.
//   - Selection propagation to children (cascading selection).
//   - IsPartiallySelected computed property (tri-state checkbox).
//   - GetGlobPattern() method for filter building.
//   - Property change notifications.
// =============================================================================

using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="FolderNodeViewModel"/>.
/// Verifies property initialization, selection propagation, and computed properties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5b")]
public class FolderNodeViewModelTests
{
    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FolderNodeViewModel(null!, "/workspace/docs");
        Assert.Throws<ArgumentNullException>("name", act);
    }

    [Fact]
    public void Constructor_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FolderNodeViewModel("docs", null!);
        Assert.Throws<ArgumentNullException>("path", act);
    }

    // =========================================================================
    // Property Initialization
    // =========================================================================

    [Fact]
    public void Constructor_ValidParameters_SetsName()
    {
        // Arrange & Act
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");

        // Assert
        Assert.Equal("docs", sut.Name);
    }

    [Fact]
    public void Constructor_ValidParameters_SetsPath()
    {
        // Arrange & Act
        var sut = new FolderNodeViewModel("specs", "/workspace/docs/specs");

        // Assert
        Assert.Equal("/workspace/docs/specs", sut.Path);
    }

    [Fact]
    public void Constructor_InitialState_IsSelectedIsFalse()
    {
        // Arrange & Act
        var sut = new FolderNodeViewModel("guides", "/workspace/guides");

        // Assert
        Assert.False(sut.IsSelected);
    }

    [Fact]
    public void Constructor_InitialState_IsExpandedIsTrue()
    {
        // Arrange & Act
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");

        // Assert
        Assert.True(sut.IsExpanded);
    }

    [Fact]
    public void Constructor_InitialState_ChildrenIsEmptyCollection()
    {
        // Arrange & Act
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");

        // Assert
        Assert.NotNull(sut.Children);
        Assert.Empty(sut.Children);
    }

    // =========================================================================
    // Selection Propagation
    // =========================================================================

    [Fact]
    public void IsSelected_SetTrue_PropagatesSelectionToChildren()
    {
        // Arrange
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        // Act
        parent.IsSelected = true;

        // Assert
        Assert.True(child1.IsSelected);
        Assert.True(child2.IsSelected);
    }

    [Fact]
    public void IsSelected_SetFalse_PropagatesDeselectionToChildren()
    {
        // Arrange
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        parent.IsSelected = true; // Select all first

        // Act
        parent.IsSelected = false;

        // Assert
        Assert.False(child1.IsSelected);
        Assert.False(child2.IsSelected);
    }

    [Fact]
    public void IsSelected_SetTrue_PropagatesRecursively()
    {
        // Arrange
        var grandparent = new FolderNodeViewModel("workspace", "/workspace");
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        grandparent.Children.Add(parent);
        parent.Children.Add(child);

        // Act
        grandparent.IsSelected = true;

        // Assert
        Assert.True(parent.IsSelected);
        Assert.True(child.IsSelected);
    }

    [Fact]
    public void IsSelected_NoChildren_DoesNotThrow()
    {
        // Arrange
        var sut = new FolderNodeViewModel("empty", "/workspace/empty");

        // Act & Assert (should not throw)
        var exception = Record.Exception(() => sut.IsSelected = true);
        Assert.Null(exception);
        Assert.True(sut.IsSelected);
    }

    // =========================================================================
    // IsPartiallySelected Computed Property
    // =========================================================================

    [Fact]
    public void IsPartiallySelected_NoChildren_ReturnsFalse()
    {
        // Arrange
        var sut = new FolderNodeViewModel("empty", "/workspace/empty");

        // Assert
        Assert.False(sut.IsPartiallySelected);
    }

    [Fact]
    public void IsPartiallySelected_AllChildrenSelected_ReturnsFalse()
    {
        // Arrange
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.IsSelected = true;
        child2.IsSelected = true;

        // Assert
        Assert.False(parent.IsPartiallySelected);
    }

    [Fact]
    public void IsPartiallySelected_NoChildrenSelected_ReturnsFalse()
    {
        // Arrange
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        parent.Children.Add(child1);
        parent.Children.Add(child2);

        // Assert
        Assert.False(parent.IsPartiallySelected);
    }

    [Fact]
    public void IsPartiallySelected_SomeChildrenSelected_ReturnsTrue()
    {
        // Arrange
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.IsSelected = true; // Only one child selected
        // child2.IsSelected remains false

        // Assert
        Assert.True(parent.IsPartiallySelected);
    }

    [Fact]
    public void IsPartiallySelected_SomeChildrenPartiallySelected_ReturnsTrue()
    {
        // Arrange - Grandparent with multiple children, one partially selected
        var grandparent = new FolderNodeViewModel("workspace", "/workspace");
        var parent1 = new FolderNodeViewModel("docs", "/workspace/docs");
        var parent2 = new FolderNodeViewModel("src", "/workspace/src");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        grandparent.Children.Add(parent1);
        grandparent.Children.Add(parent2);
        parent1.Children.Add(child1);
        parent1.Children.Add(child2);
        child1.IsSelected = true; // Makes parent1 partially selected

        // Assert
        Assert.True(parent1.IsPartiallySelected);
        // grandparent has 2 children, 1 is partially selected, so grandparent is also partial
        Assert.True(grandparent.IsPartiallySelected);
    }

    [Fact]
    public void IsPartiallySelected_SingleChildPartiallySelected_ReturnsFalse()
    {
        // Arrange - Grandparent with single child that is partially selected
        // When ALL children are partially selected (or selected), parent should NOT be partial
        var grandparent = new FolderNodeViewModel("workspace", "/workspace");
        var parent = new FolderNodeViewModel("docs", "/workspace/docs");
        var child1 = new FolderNodeViewModel("specs", "/workspace/docs/specs");
        var child2 = new FolderNodeViewModel("guides", "/workspace/docs/guides");
        grandparent.Children.Add(parent);
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.IsSelected = true; // Makes parent partially selected

        // Assert
        Assert.True(parent.IsPartiallySelected);
        // grandparent has only 1 child (100% are in partial state) - not "some but not all"
        Assert.False(grandparent.IsPartiallySelected);
    }

    // =========================================================================
    // GetGlobPattern Method
    // =========================================================================

    [Fact]
    public void GetGlobPattern_ReturnsPathWithDoubleAsterisk()
    {
        // Arrange
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");

        // Act
        var pattern = sut.GetGlobPattern();

        // Assert
        Assert.Equal("/workspace/docs/**", pattern);
    }

    [Fact]
    public void GetGlobPattern_NestedPath_ReturnsCorrectPattern()
    {
        // Arrange
        var sut = new FolderNodeViewModel("specs", "/workspace/docs/specs/api");

        // Act
        var pattern = sut.GetGlobPattern();

        // Assert
        Assert.Equal("/workspace/docs/specs/api/**", pattern);
    }

    [Fact]
    public void GetGlobPattern_RootPath_ReturnsCorrectPattern()
    {
        // Arrange
        var sut = new FolderNodeViewModel("workspace", "/workspace");

        // Act
        var pattern = sut.GetGlobPattern();

        // Assert
        Assert.Equal("/workspace/**", pattern);
    }

    // =========================================================================
    // Property Change Notifications
    // =========================================================================

    [Fact]
    public void IsSelected_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.IsSelected = true;

        // Assert
        Assert.Contains(nameof(FolderNodeViewModel.IsSelected), changedProperties);
    }

    [Fact]
    public void IsSelected_WhenChanged_NotifiesIsPartiallySelected()
    {
        // Arrange
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.IsSelected = true;

        // Assert
        Assert.Contains(nameof(FolderNodeViewModel.IsPartiallySelected), changedProperties);
    }

    [Fact]
    public void IsExpanded_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new FolderNodeViewModel("docs", "/workspace/docs");
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.IsExpanded = false;

        // Assert
        Assert.Contains(nameof(FolderNodeViewModel.IsExpanded), changedProperties);
    }
}
