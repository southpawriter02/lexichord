// =============================================================================
// File: SearchFilterPanelViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SearchFilterPanelViewModel filter panel behavior.
// =============================================================================
// LOGIC: Verifies all SearchFilterPanelViewModel functionality:
//   - Constructor null-parameter validation (4 dependencies).
//   - License feature initialization (CanUseDateFilter, CanUseSavedPresets).
//   - Extension toggle initialization and selection.
//   - Date range selection (AnyTime, LastDay, Last7Days, Last30Days, Custom).
//   - Filter chip generation for paths, extensions, and date ranges.
//   - ClearFiltersCommand behavior.
//   - RemoveChipCommand behavior for each chip type.
//   - CurrentFilter property building.
//   - HasActiveFilters computed property.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Enums;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="SearchFilterPanelViewModel"/>.
/// Verifies filter state management, license gating, and filter building.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5b")]
public class SearchFilterPanelViewModelTests
{
    // LOGIC: Mock dependencies for constructor injection.
    private readonly Mock<IWorkspaceService> _workspaceServiceMock;
    private readonly Mock<IFilterValidator> _filterValidatorMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ILogger<SearchFilterPanelViewModel>> _loggerMock;

    public SearchFilterPanelViewModelTests()
    {
        _workspaceServiceMock = new Mock<IWorkspaceService>();
        _filterValidatorMock = new Mock<IFilterValidator>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _loggerMock = new Mock<ILogger<SearchFilterPanelViewModel>>();

        // LOGIC: Default license setup - unlicensed for date filter and presets.
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(false);
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SavedPresets))
            .Returns(false);
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullWorkspaceService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchFilterPanelViewModel(
            null!,
            _filterValidatorMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("workspaceService", act);
    }

    [Fact]
    public void Constructor_NullFilterValidator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchFilterPanelViewModel(
            _workspaceServiceMock.Object,
            null!,
            _licenseContextMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("filterValidator", act);
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchFilterPanelViewModel(
            _workspaceServiceMock.Object,
            _filterValidatorMock.Object,
            null!,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("licenseContext", act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchFilterPanelViewModel(
            _workspaceServiceMock.Object,
            _filterValidatorMock.Object,
            _licenseContextMock.Object,
            null!);
        Assert.Throws<ArgumentNullException>("logger", act);
    }

    // =========================================================================
    // Initial State
    // =========================================================================

    [Fact]
    public void Constructor_InitialState_IsExpandedIsFalse()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.IsExpanded);
    }

    [Fact]
    public void Constructor_InitialState_SelectedDateRangeIsAnyTime()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.Equal(DateRangeOption.AnyTime, sut.SelectedDateRange);
    }

    [Fact]
    public void Constructor_InitialState_HasNoActiveFilters()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.HasActiveFilters);
        Assert.Empty(sut.ActiveChips);
    }

    [Fact]
    public void Constructor_InitialState_FolderTreeIsEmpty()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.NotNull(sut.FolderTree);
        Assert.Empty(sut.FolderTree);
    }

    // =========================================================================
    // License Feature Initialization
    // =========================================================================

    [Fact]
    public void Constructor_Unlicensed_CanUseDateFilterIsFalse()
    {
        // Arrange - license mock already returns false

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.CanUseDateFilter);
    }

    [Fact]
    public void Constructor_Unlicensed_CanUseSavedPresetsIsFalse()
    {
        // Arrange - license mock already returns false

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.CanUseSavedPresets);
    }

    [Fact]
    public void Constructor_Licensed_CanUseDateFilterIsTrue()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.True(sut.CanUseDateFilter);
    }

    [Fact]
    public void Constructor_Licensed_CanUseSavedPresetsIsTrue()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SavedPresets))
            .Returns(true);

        // Act
        var sut = CreateViewModel();

        // Assert
        Assert.True(sut.CanUseSavedPresets);
    }

    // =========================================================================
    // Extension Toggles Initialization
    // =========================================================================

    [Fact]
    public void Constructor_InitializesExtensionToggles()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.NotNull(sut.ExtensionToggles);
        Assert.NotEmpty(sut.ExtensionToggles);
    }

    [Fact]
    public void Constructor_ExtensionToggles_ContainsMarkdown()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        var mdToggle = sut.ExtensionToggles.FirstOrDefault(t => t.Extension == "md");
        Assert.NotNull(mdToggle);
        Assert.Equal("Markdown", mdToggle.DisplayName);
    }

    [Fact]
    public void Constructor_ExtensionToggles_ContainsJson()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        var jsonToggle = sut.ExtensionToggles.FirstOrDefault(t => t.Extension == "json");
        Assert.NotNull(jsonToggle);
        Assert.Equal("JSON", jsonToggle.DisplayName);
    }

    [Fact]
    public void Constructor_ExtensionToggles_AllUnselectedInitially()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.All(sut.ExtensionToggles, t => Assert.False(t.IsSelected));
    }

    // =========================================================================
    // Extension Toggle Selection
    // =========================================================================

    [Fact]
    public void ExtensionToggle_WhenSelected_AddsChip()
    {
        // Arrange
        var sut = CreateViewModel();
        var mdToggle = sut.ExtensionToggles.First(t => t.Extension == "md");

        // Act
        mdToggle.IsSelected = true;

        // Assert
        Assert.True(sut.HasActiveFilters);
        Assert.Contains(sut.ActiveChips, c => c.Type == FilterChipType.Extension && c.Value == "md");
    }

    [Fact]
    public void ExtensionToggle_WhenDeselected_RemovesChip()
    {
        // Arrange
        var sut = CreateViewModel();
        var mdToggle = sut.ExtensionToggles.First(t => t.Extension == "md");
        mdToggle.IsSelected = true;

        // Act
        mdToggle.IsSelected = false;

        // Assert
        Assert.DoesNotContain(sut.ActiveChips, c => c.Type == FilterChipType.Extension && c.Value == "md");
    }

    [Fact]
    public void ExtensionToggle_MultipleSelected_AddsMultipleChips()
    {
        // Arrange
        var sut = CreateViewModel();
        var mdToggle = sut.ExtensionToggles.First(t => t.Extension == "md");
        var jsonToggle = sut.ExtensionToggles.First(t => t.Extension == "json");

        // Act
        mdToggle.IsSelected = true;
        jsonToggle.IsSelected = true;

        // Assert
        Assert.Equal(2, sut.ActiveChips.Count(c => c.Type == FilterChipType.Extension));
    }

    // =========================================================================
    // Date Range Selection (Licensed)
    // =========================================================================

    [Fact]
    public void SelectedDateRange_WhenLicensedAndChanged_AddsChip()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();

        // Act
        sut.SelectedDateRange = DateRangeOption.Last7Days;

        // Assert
        Assert.Contains(sut.ActiveChips, c => c.Type == FilterChipType.DateRange);
    }

    [Fact]
    public void SelectedDateRange_AnyTime_DoesNotAddChip()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Last7Days;

        // Act
        sut.SelectedDateRange = DateRangeOption.AnyTime;

        // Assert
        Assert.DoesNotContain(sut.ActiveChips, c => c.Type == FilterChipType.DateRange);
    }

    [Fact]
    public void SelectedDateRange_WhenUnlicensed_DoesNotAddChip()
    {
        // Arrange - license mock returns false for date filter
        var sut = CreateViewModel();

        // Act
        sut.SelectedDateRange = DateRangeOption.Last7Days;

        // Assert
        Assert.DoesNotContain(sut.ActiveChips, c => c.Type == FilterChipType.DateRange);
    }

    // =========================================================================
    // ClearFiltersCommand
    // =========================================================================

    [Fact]
    public void ClearFiltersCommand_ClearsExtensionSelections()
    {
        // Arrange
        var sut = CreateViewModel();
        sut.ExtensionToggles.First(t => t.Extension == "md").IsSelected = true;
        sut.ExtensionToggles.First(t => t.Extension == "json").IsSelected = true;

        // Act
        sut.ClearFiltersCommand.Execute(null);

        // Assert
        Assert.All(sut.ExtensionToggles, t => Assert.False(t.IsSelected));
    }

    [Fact]
    public void ClearFiltersCommand_ResetsDateRange()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Last30Days;

        // Act
        sut.ClearFiltersCommand.Execute(null);

        // Assert
        Assert.Equal(DateRangeOption.AnyTime, sut.SelectedDateRange);
    }

    [Fact]
    public void ClearFiltersCommand_ClearsCustomDates()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Custom;
        sut.CustomStartDate = DateTime.Today.AddDays(-7);
        sut.CustomEndDate = DateTime.Today;

        // Act
        sut.ClearFiltersCommand.Execute(null);

        // Assert
        Assert.Null(sut.CustomStartDate);
        Assert.Null(sut.CustomEndDate);
    }

    [Fact]
    public void ClearFiltersCommand_ClearsActiveChips()
    {
        // Arrange
        var sut = CreateViewModel();
        sut.ExtensionToggles.First(t => t.Extension == "md").IsSelected = true;
        Assert.NotEmpty(sut.ActiveChips);

        // Act
        sut.ClearFiltersCommand.Execute(null);

        // Assert
        Assert.Empty(sut.ActiveChips);
        Assert.False(sut.HasActiveFilters);
    }

    // =========================================================================
    // RemoveChipCommand
    // =========================================================================

    [Fact]
    public void RemoveChipCommand_ExtensionChip_DeselectsToggle()
    {
        // Arrange
        var sut = CreateViewModel();
        var mdToggle = sut.ExtensionToggles.First(t => t.Extension == "md");
        mdToggle.IsSelected = true;
        var chip = sut.ActiveChips.First(c => c.Type == FilterChipType.Extension && c.Value == "md");

        // Act
        sut.RemoveChipCommand.Execute(chip);

        // Assert
        Assert.False(mdToggle.IsSelected);
        Assert.DoesNotContain(sut.ActiveChips, c => c.Value == "md");
    }

    [Fact]
    public void RemoveChipCommand_DateRangeChip_ResetsToAnyTime()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Last7Days;
        var chip = sut.ActiveChips.First(c => c.Type == FilterChipType.DateRange);

        // Act
        sut.RemoveChipCommand.Execute(chip);

        // Assert
        Assert.Equal(DateRangeOption.AnyTime, sut.SelectedDateRange);
    }

    [Fact]
    public void RemoveChipCommand_NullChip_DoesNotThrow()
    {
        // Arrange
        var sut = CreateViewModel();

        // Act & Assert (should not throw)
        var exception = Record.Exception(() => sut.RemoveChipCommand.Execute(null));
        Assert.Null(exception);
    }

    // =========================================================================
    // CurrentFilter Property
    // =========================================================================

    [Fact]
    public void CurrentFilter_NoFiltersActive_ReturnsEmptyFilter()
    {
        // Arrange
        var sut = CreateViewModel();

        // Act
        var filter = sut.CurrentFilter;

        // Assert
        Assert.Null(filter.PathPatterns);
        Assert.Null(filter.FileExtensions);
        Assert.Null(filter.ModifiedRange);
    }

    [Fact]
    public void CurrentFilter_ExtensionsSelected_IncludesExtensions()
    {
        // Arrange
        var sut = CreateViewModel();
        sut.ExtensionToggles.First(t => t.Extension == "md").IsSelected = true;
        sut.ExtensionToggles.First(t => t.Extension == "json").IsSelected = true;

        // Act
        var filter = sut.CurrentFilter;

        // Assert
        Assert.NotNull(filter.FileExtensions);
        Assert.Contains("md", filter.FileExtensions);
        Assert.Contains("json", filter.FileExtensions);
    }

    [Fact]
    public void CurrentFilter_DateRangeSelected_WhenLicensed_IncludesDateRange()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Last7Days;

        // Act
        var filter = sut.CurrentFilter;

        // Assert
        Assert.NotNull(filter.ModifiedRange);
    }

    [Fact]
    public void CurrentFilter_DateRangeSelected_WhenUnlicensed_ExcludesDateRange()
    {
        // Arrange - license mock returns false for date filter
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Last7Days;

        // Act
        var filter = sut.CurrentFilter;

        // Assert
        Assert.Null(filter.ModifiedRange);
    }

    [Fact]
    public void CurrentFilter_CustomDateRange_IncludesCustomDates()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.Custom;
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        sut.CustomStartDate = startDate;
        sut.CustomEndDate = endDate;

        // Act
        var filter = sut.CurrentFilter;

        // Assert
        Assert.NotNull(filter.ModifiedRange);
        Assert.Equal(startDate, filter.ModifiedRange.Start);
        Assert.Equal(endDate, filter.ModifiedRange.End);
    }

    // =========================================================================
    // HasActiveFilters Computed Property
    // =========================================================================

    [Fact]
    public void HasActiveFilters_NoFilters_ReturnsFalse()
    {
        // Arrange
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.HasActiveFilters);
    }

    [Fact]
    public void HasActiveFilters_WithExtensionFilter_ReturnsTrue()
    {
        // Arrange
        var sut = CreateViewModel();
        sut.ExtensionToggles.First(t => t.Extension == "md").IsSelected = true;

        // Assert
        Assert.True(sut.HasActiveFilters);
    }

    [Fact]
    public void HasActiveFilters_WithDateFilter_WhenLicensed_ReturnsTrue()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DateRangeFilter))
            .Returns(true);
        var sut = CreateViewModel();
        sut.SelectedDateRange = DateRangeOption.LastDay;

        // Assert
        Assert.True(sut.HasActiveFilters);
    }

    // =========================================================================
    // Property Change Notifications
    // =========================================================================

    [Fact]
    public void SelectedDateRange_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateViewModel();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.SelectedDateRange = DateRangeOption.Last7Days;

        // Assert
        Assert.Contains(nameof(SearchFilterPanelViewModel.SelectedDateRange), changedProperties);
    }

    [Fact]
    public void ExtensionToggle_WhenChanged_NotifiesHasActiveFilters()
    {
        // Arrange
        var sut = CreateViewModel();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.ExtensionToggles.First(t => t.Extension == "md").IsSelected = true;

        // Assert
        Assert.Contains(nameof(SearchFilterPanelViewModel.HasActiveFilters), changedProperties);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a SearchFilterPanelViewModel with mock dependencies.
    /// </summary>
    private SearchFilterPanelViewModel CreateViewModel()
    {
        return new SearchFilterPanelViewModel(
            _workspaceServiceMock.Object,
            _filterValidatorMock.Object,
            _licenseContextMock.Object,
            _loggerMock.Object);
    }
}
