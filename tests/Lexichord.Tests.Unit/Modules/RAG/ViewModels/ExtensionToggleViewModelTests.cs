// =============================================================================
// File: ExtensionToggleViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ExtensionToggleViewModel extension toggle behavior.
// =============================================================================
// LOGIC: Verifies all ExtensionToggleViewModel functionality:
//   - Constructor null-parameter validation (3 parameters).
//   - Property initialization: Extension, DisplayName, Icon, IsSelected.
//   - IsSelected property change notifications.
//   - Initial state: IsSelected is false.
// =============================================================================

using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="ExtensionToggleViewModel"/>.
/// Verifies property initialization, selection behavior, and change notifications.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5b")]
public class ExtensionToggleViewModelTests
{
    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullExtension_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ExtensionToggleViewModel(null!, "Markdown", "M↓");
        Assert.Throws<ArgumentNullException>("extension", act);
    }

    [Fact]
    public void Constructor_NullDisplayName_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ExtensionToggleViewModel("md", null!, "M↓");
        Assert.Throws<ArgumentNullException>("displayName", act);
    }

    [Fact]
    public void Constructor_NullIcon_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ExtensionToggleViewModel("md", "Markdown", null!);
        Assert.Throws<ArgumentNullException>("icon", act);
    }

    // =========================================================================
    // Property Initialization
    // =========================================================================

    [Fact]
    public void Constructor_ValidParameters_SetsExtension()
    {
        // Arrange & Act
        var sut = new ExtensionToggleViewModel("md", "Markdown", "M↓");

        // Assert
        Assert.Equal("md", sut.Extension);
    }

    [Fact]
    public void Constructor_ValidParameters_SetsDisplayName()
    {
        // Arrange & Act
        var sut = new ExtensionToggleViewModel("json", "JSON", "{}");

        // Assert
        Assert.Equal("JSON", sut.DisplayName);
    }

    [Fact]
    public void Constructor_ValidParameters_SetsIcon()
    {
        // Arrange & Act
        var sut = new ExtensionToggleViewModel("yaml", "YAML", "Y");

        // Assert
        Assert.Equal("Y", sut.Icon);
    }

    [Fact]
    public void Constructor_InitialState_IsSelectedIsFalse()
    {
        // Arrange & Act
        var sut = new ExtensionToggleViewModel("txt", "Text", "T");

        // Assert
        Assert.False(sut.IsSelected);
    }

    // =========================================================================
    // IsSelected Property
    // =========================================================================

    [Fact]
    public void IsSelected_SetTrue_UpdatesValue()
    {
        // Arrange
        var sut = new ExtensionToggleViewModel("rst", "RST", "R");

        // Act
        sut.IsSelected = true;

        // Assert
        Assert.True(sut.IsSelected);
    }

    [Fact]
    public void IsSelected_SetFalse_UpdatesValue()
    {
        // Arrange
        var sut = new ExtensionToggleViewModel("md", "Markdown", "M↓");
        sut.IsSelected = true;

        // Act
        sut.IsSelected = false;

        // Assert
        Assert.False(sut.IsSelected);
    }

    // =========================================================================
    // Property Change Notifications
    // =========================================================================

    [Fact]
    public void IsSelected_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new ExtensionToggleViewModel("md", "Markdown", "M↓");
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.IsSelected = true;

        // Assert
        Assert.Contains(nameof(ExtensionToggleViewModel.IsSelected), changedProperties);
    }

    [Fact]
    public void IsSelected_WhenSetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var sut = new ExtensionToggleViewModel("md", "Markdown", "M↓");
        var propertyChangedCount = 0;
        sut.PropertyChanged += (_, _) => propertyChangedCount++;

        // Act
        sut.IsSelected = false; // Already false

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }
}
