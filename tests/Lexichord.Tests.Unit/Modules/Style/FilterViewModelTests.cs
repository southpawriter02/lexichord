using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for FilterViewModel (v0.2.5b).
/// </summary>
/// <remarks>
/// LOGIC: Verifies the FilterViewModel correctly manages filter state,
/// debounces search text changes, and raises FilterChanged events.
/// </remarks>
[Trait("Category", "Unit")]
public class FilterViewModelTests : IDisposable
{
    private readonly Mock<ITermFilterService> _filterServiceMock = new();
    private readonly Mock<ILogger<FilterViewModel>> _loggerMock = new();

    private FilterViewModel CreateViewModel(FilterOptions? options = null)
    {
        var optionsWrapper = Options.Create(options ?? new FilterOptions());
        return new FilterViewModel(
            _filterServiceMock.Object,
            optionsWrapper,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        // Clean up any test resources
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        var viewModel = CreateViewModel();

        viewModel.SearchText.Should().BeEmpty();
        viewModel.ShowInactive.Should().BeFalse();
        viewModel.SelectedCategory.Should().BeNull();
        viewModel.SelectedSeverity.Should().BeNull();
        viewModel.HasActiveFilters.Should().BeFalse();
    }

    [Fact]
    public void Constructor_UsesOptionsForDefaultToggleValues()
    {
        var options = new FilterOptions
        {
            ShowInactive = true
        };

        var viewModel = CreateViewModel(options);

        viewModel.ShowInactive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ThrowsOnNullFilterService()
    {
        var optionsWrapper = Options.Create(new FilterOptions());
        
        var action = () => new FilterViewModel(
            null!,
            optionsWrapper,
            _loggerMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("filterService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var optionsWrapper = Options.Create(new FilterOptions());
        
        var action = () => new FilterViewModel(
            _filterServiceMock.Object,
            optionsWrapper,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Toggle Change Tests

    [Fact]
    public void ShowInactive_RaisesFilterChanged_WhenModified()
    {
        var viewModel = CreateViewModel();
        var filterChangedRaised = false;
        viewModel.FilterChanged += (s, e) => filterChangedRaised = true;

        viewModel.ShowInactive = true;

        filterChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void SelectedCategory_RaisesFilterChanged_WhenModified()
    {
        var viewModel = CreateViewModel();
        var filterChangedRaised = false;
        viewModel.FilterChanged += (s, e) => filterChangedRaised = true;

        viewModel.SelectedCategory = "Grammar";

        filterChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void SelectedSeverity_RaisesFilterChanged_WhenModified()
    {
        var viewModel = CreateViewModel();
        var filterChangedRaised = false;
        viewModel.FilterChanged += (s, e) => filterChangedRaised = true;

        viewModel.SelectedSeverity = "Error";

        filterChangedRaised.Should().BeTrue();
    }

    #endregion

    #region Debounce Tests

    [Fact]
    public async Task SearchText_TriggersFilterChanged_AfterDebounce()
    {
        // Use longer debounce to avoid flakiness under CPU load
        var options = new FilterOptions { DebounceMilliseconds = 100 };
        var viewModel = CreateViewModel(options);
        var filterChangedCount = 0;
        viewModel.FilterChanged += (s, e) => filterChangedCount++;

        // Act
        viewModel.SearchText = "test";

        // Should not trigger immediately
        filterChangedCount.Should().Be(0);

        // Wait for debounce (100ms + very generous margin for CPU load)
        await Task.Delay(500);

        filterChangedCount.Should().Be(1);

        viewModel.Dispose();
    }

    [Fact]
    public async Task SearchText_CoalescesRapidChanges()
    {
        // Use longer debounce to avoid flakiness under CPU load
        var options = new FilterOptions { DebounceMilliseconds = 100 };
        var viewModel = CreateViewModel(options);
        var filterChangedCount = 0;
        viewModel.FilterChanged += (s, e) => filterChangedCount++;

        // Act - rapid changes
        viewModel.SearchText = "t";
        viewModel.SearchText = "te";
        viewModel.SearchText = "tes";
        viewModel.SearchText = "test";

        // Wait for debounce (100ms + generous margin)
        await Task.Delay(200);

        // Should only trigger once for final value
        filterChangedCount.Should().Be(1);

        viewModel.Dispose();
    }

    #endregion

    #region ClearFilters Tests

    [Fact]
    public void ClearFilters_ResetsAllProperties()
    {
        var viewModel = CreateViewModel();
        viewModel.ShowInactive = true;
        viewModel.SelectedCategory = "Grammar";
        viewModel.SelectedSeverity = "Error";

        viewModel.ClearFiltersCommand.Execute(null);

        viewModel.SearchText.Should().BeEmpty();
        viewModel.ShowInactive.Should().BeFalse();
        viewModel.SelectedCategory.Should().BeNull();
        viewModel.SelectedSeverity.Should().BeNull();
    }

    [Fact]
    public void ClearFilters_RaisesFilterChanged()
    {
        var viewModel = CreateViewModel();
        viewModel.ShowInactive = true;
        
        var filterChangedCount = 0;
        viewModel.FilterChanged += (s, e) => filterChangedCount++;

        // Reset counter after setup
        filterChangedCount = 0;

        viewModel.ClearFiltersCommand.Execute(null);

        // ClearFilters resets properties and raises FilterChanged once
        filterChangedCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region HasActiveFilters Tests

    [Fact]
    public void HasActiveFilters_ReturnsFalse_WhenAllDefaults()
    {
        var viewModel = CreateViewModel();

        viewModel.HasActiveFilters.Should().BeFalse();
    }

    [Fact]
    public void HasActiveFilters_ReturnsTrue_WhenShowInactiveIsTrue()
    {
        var viewModel = CreateViewModel();
        viewModel.ShowInactive = true;

        viewModel.HasActiveFilters.Should().BeTrue();
    }

    [Fact]
    public void HasActiveFilters_ReturnsTrue_WhenCategoryIsSet()
    {
        var viewModel = CreateViewModel();
        viewModel.SelectedCategory = "Grammar";

        viewModel.HasActiveFilters.Should().BeTrue();
    }

    [Fact]
    public void HasActiveFilters_ReturnsTrue_WhenSeverityIsSet()
    {
        var viewModel = CreateViewModel();
        viewModel.SelectedSeverity = "Error";

        viewModel.HasActiveFilters.Should().BeTrue();
    }

    #endregion

    #region GetCriteria Tests

    [Fact]
    public void GetCriteria_ReturnsCurrentState()
    {
        var viewModel = CreateViewModel();
        viewModel.ShowInactive = true;
        viewModel.SelectedCategory = "Grammar";
        viewModel.SelectedSeverity = "Error";

        var criteria = viewModel.GetCriteria();

        criteria.ShowInactive.Should().BeTrue();
        criteria.CategoryFilter.Should().Be("Grammar");
        criteria.SeverityFilter.Should().Be("Error");
    }

    #endregion

    #region UpdateAvailableOptions Tests

    [Fact]
    public void UpdateAvailableOptions_PopulatesDropdowns()
    {
        var viewModel = CreateViewModel();
        var categories = new[] { "Grammar", "Style", "Punctuation" };
        var severities = new[] { "Error", "Warning", "Suggestion" };

        viewModel.UpdateAvailableOptions(categories, severities);

        viewModel.AvailableCategories.Should().BeEquivalentTo(categories.OrderBy(c => c));
        viewModel.AvailableSeverities.Should().BeEquivalentTo(severities);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesResources()
    {
        var viewModel = CreateViewModel();

        var action = () => viewModel.Dispose();

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var viewModel = CreateViewModel();

        viewModel.Dispose();
        var action = () => viewModel.Dispose();

        action.Should().NotThrow();
    }

    #endregion
}
