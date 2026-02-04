// =============================================================================
// File: ReferenceViewModelNavigationTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ReferenceViewModel keyboard navigation (v0.5.7a).
// =============================================================================
// LOGIC: Verifies the keyboard navigation and filter chip functionality:
//   - MoveSelectionUp/Down command behavior.
//   - Selection index boundary clamping.
//   - OpenSelectedResult navigation.
//   - Filter chip removal and clearing.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Enums;
using Lexichord.Modules.RAG.Search;
using Lexichord.Modules.RAG.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="ReferenceViewModel"/> keyboard navigation functionality.
/// Introduced in v0.5.7a.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7a")]
public class ReferenceViewModelNavigationTests
{
    // =========================================================================
    // Test Infrastructure
    // =========================================================================

    private readonly Mock<ISemanticSearchService> _semanticSearchMock;
    private readonly Mock<IBM25SearchService> _bm25SearchMock;
    private readonly Mock<IHybridSearchService> _hybridSearchMock;
    private readonly Mock<ISearchHistoryService> _historyServiceMock;
    private readonly Mock<IReferenceNavigationService> _navigationServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ReferenceViewModel>> _loggerMock;
    private readonly Mock<ILogger<SearchLicenseGuard>> _guardLoggerMock;
    private readonly SearchLicenseGuard _licenseGuard;

    public ReferenceViewModelNavigationTests()
    {
        _semanticSearchMock = new Mock<ISemanticSearchService>();
        _bm25SearchMock = new Mock<IBM25SearchService>();
        _hybridSearchMock = new Mock<IHybridSearchService>();
        _historyServiceMock = new Mock<ISearchHistoryService>();
        _navigationServiceMock = new Mock<IReferenceNavigationService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _settingsRepositoryMock = new Mock<ISystemSettingsRepository>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ReferenceViewModel>>();
        _guardLoggerMock = new Mock<ILogger<SearchLicenseGuard>>();

        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        _licenseGuard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        _historyServiceMock.Setup(x => x.GetRecentQueries(It.IsAny<int>())).Returns(Array.Empty<string>());
    }

    private ReferenceViewModel CreateSut()
    {
        return new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    // =========================================================================
    // SelectedResultIndex Tests
    // =========================================================================

    /// <summary>
    /// Verifies the initial selected index is -1 (no selection).
    /// </summary>
    [Fact]
    public void SelectedResultIndex_Default_IsNegativeOne()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex.Should().Be(-1);
    }

    /// <summary>
    /// Verifies the index can be set programmatically.
    /// </summary>
    [Fact]
    public void SelectedResultIndex_CanBeSet()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex = 5;
        sut.SelectedResultIndex.Should().Be(5);
    }

    // =========================================================================
    // MoveSelectionUp Tests
    // =========================================================================

    /// <summary>
    /// Verifies MoveSelectionUp decrements the index.
    /// </summary>
    [Fact]
    public void MoveSelectionUp_AtMiddle_Decrements()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex = 3;

        sut.MoveSelectionUpCommand.Execute(null);

        sut.SelectedResultIndex.Should().Be(2);
    }

    /// <summary>
    /// Verifies MoveSelectionUp clamps at zero.
    /// </summary>
    [Fact]
    public void MoveSelectionUp_AtFirst_StaysAtFirst()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex = 0;

        sut.MoveSelectionUpCommand.Execute(null);

        sut.SelectedResultIndex.Should().Be(0);
    }

    /// <summary>
    /// Verifies MoveSelectionUp does not decrement at negative index.
    /// </summary>
    [Fact]
    public void MoveSelectionUp_AtNegative_StaysNegative()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex = -1;

        sut.MoveSelectionUpCommand.Execute(null);

        sut.SelectedResultIndex.Should().Be(-1);
    }

    // =========================================================================
    // MoveSelectionDown Tests
    // =========================================================================

    /// <summary>
    /// Verifies MoveSelectionDown does nothing when no results (Results.Count-1 = -1).
    /// </summary>
    [Fact]
    public void MoveSelectionDown_NoResults_DoesNotIncrement()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex = 1;

        sut.MoveSelectionDownCommand.Execute(null);

        // LOGIC: With no results, Results.Count-1 = -1, so index 1 is already >= boundary.
        // The command's boundary check prevents incrementing past the last result.
        sut.SelectedResultIndex.Should().Be(1);
    }

    /// <summary>
    /// Verifies MoveSelectionDown does nothing at -1 with no results.
    /// </summary>
    [Fact]
    public void MoveSelectionDown_AtNegativeOne_NoResults_StaysNegative()
    {
        var sut = CreateSut();
        sut.SelectedResultIndex = -1;

        sut.MoveSelectionDownCommand.Execute(null);

        // LOGIC: With no results, cannot go past index -1 (below 0, which is Results.Count-1).
        sut.SelectedResultIndex.Should().Be(-1);
    }

    // =========================================================================
    // CanNavigate Tests
    // =========================================================================

    /// <summary>
    /// Verifies CanNavigate is false when no results.
    /// </summary>
    [Fact]
    public void CanNavigate_NoResults_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.CanNavigate.Should().BeFalse();
    }

    // =========================================================================
    // ActiveFilterChips Tests
    // =========================================================================

    /// <summary>
    /// Verifies the initial filter chips collection is empty.
    /// </summary>
    [Fact]
    public void ActiveFilterChips_Default_IsEmpty()
    {
        var sut = CreateSut();
        sut.ActiveFilterChips.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies HasActiveFilters is false when no chips.
    /// </summary>
    [Fact]
    public void HasActiveFilters_NoChips_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.HasActiveFilters.Should().BeFalse();
    }

    /// <summary>
    /// Verifies HasActiveFilters is true when chips are present.
    /// </summary>
    [Fact]
    public void HasActiveFilters_WithChips_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.ActiveFilterChips.Add(FilterChip.ForPath("docs/**"));
        sut.HasActiveFilters.Should().BeTrue();
    }

    /// <summary>
    /// Verifies ActiveFilterCount returns correct count.
    /// </summary>
    [Fact]
    public void ActiveFilterCount_ReturnsCorrectCount()
    {
        var sut = CreateSut();
        sut.ActiveFilterChips.Add(FilterChip.ForPath("docs/**"));
        sut.ActiveFilterChips.Add(FilterChip.ForExtension("md"));
        sut.ActiveFilterCount.Should().Be(2);
    }

    // =========================================================================
    // RemoveFilterChip Tests
    // =========================================================================

    /// <summary>
    /// Verifies RemoveFilterChip removes the specified chip.
    /// </summary>
    [Fact]
    public void RemoveFilterChip_ExistingChip_RemovesFromCollection()
    {
        var sut = CreateSut();
        var chip = FilterChip.ForPath("docs/**");
        sut.ActiveFilterChips.Add(chip);

        sut.RemoveFilterChipCommand.Execute(chip);

        sut.ActiveFilterChips.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies RemoveFilterChip handles null gracefully.
    /// </summary>
    [Fact]
    public void RemoveFilterChip_NullChip_DoesNotThrow()
    {
        var sut = CreateSut();
        FluentActions.Invoking(() => sut.RemoveFilterChipCommand.Execute(null!))
            .Should().NotThrow();
    }

    // =========================================================================
    // ClearAllFilters Tests
    // =========================================================================

    /// <summary>
    /// Verifies ClearAllFilters removes all chips.
    /// </summary>
    [Fact]
    public void ClearAllFilters_RemovesAllChips()
    {
        var sut = CreateSut();
        sut.ActiveFilterChips.Add(FilterChip.ForPath("docs/**"));
        sut.ActiveFilterChips.Add(FilterChip.ForExtension("md"));
        sut.ActiveFilterChips.Add(FilterChip.ForDateRange(DateRangeOption.Last7Days));

        sut.ClearAllFiltersCommand.Execute(null);

        sut.ActiveFilterChips.Should().BeEmpty();
        sut.HasActiveFilters.Should().BeFalse();
    }

    /// <summary>
    /// Verifies ClearAllFilters works on empty collection.
    /// </summary>
    [Fact]
    public void ClearAllFilters_EmptyCollection_DoesNotThrow()
    {
        var sut = CreateSut();
        FluentActions.Invoking(() => sut.ClearAllFiltersCommand.Execute(null))
            .Should().NotThrow();
    }
}
