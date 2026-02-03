// =============================================================================
// File: SearchFilterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Filter Model records (v0.5.5a).
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for <see cref="SearchFilter"/>, <see cref="DateRange"/>,
/// and <see cref="FilterPreset"/> records.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5a")]
public class SearchFilterTests
{
    #region SearchFilter Tests

    [Fact]
    public void SearchFilter_Empty_HasNoCriteria()
    {
        // Act
        var filter = SearchFilter.Empty;

        // Assert
        filter.HasCriteria.Should().BeFalse();
        filter.CriteriaCount.Should().Be(0);
        filter.PathPatterns.Should().BeNull();
        filter.FileExtensions.Should().BeNull();
        filter.ModifiedRange.Should().BeNull();
        filter.Tags.Should().BeNull();
        filter.HasHeadings.Should().BeNull();
    }

    [Fact]
    public void SearchFilter_ForPath_CreatesSinglePathFilter()
    {
        // Act
        var filter = SearchFilter.ForPath("docs/**");

        // Assert
        filter.HasCriteria.Should().BeTrue();
        filter.CriteriaCount.Should().Be(1);
        filter.PathPatterns.Should().ContainSingle().Which.Should().Be("docs/**");
    }

    [Fact]
    public void SearchFilter_ForExtensions_CreatesExtensionFilter()
    {
        // Act
        var filter = SearchFilter.ForExtensions("md", "txt", "rst");

        // Assert
        filter.HasCriteria.Should().BeTrue();
        filter.CriteriaCount.Should().Be(1);
        filter.FileExtensions.Should().HaveCount(3);
        filter.FileExtensions.Should().Contain("md");
        filter.FileExtensions.Should().Contain("txt");
        filter.FileExtensions.Should().Contain("rst");
    }

    [Fact]
    public void SearchFilter_RecentlyModified_CreatesDateRangeFilter()
    {
        // Act
        var filter = SearchFilter.RecentlyModified(7);

        // Assert
        filter.HasCriteria.Should().BeTrue();
        filter.CriteriaCount.Should().Be(1);
        filter.ModifiedRange.Should().NotBeNull();
        filter.ModifiedRange!.Start.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(-7),
            TimeSpan.FromSeconds(1));
        filter.ModifiedRange.End.Should().BeNull();
    }

    [Fact]
    public void SearchFilter_CriteriaCount_CountsAllCriteria()
    {
        // Arrange
        var filter = new SearchFilter(
            PathPatterns: new[] { "docs/**" },
            FileExtensions: new[] { "md" },
            ModifiedRange: DateRange.LastDays(7),
            HasHeadings: true);

        // Assert
        filter.CriteriaCount.Should().Be(4);
    }

    [Fact]
    public void SearchFilter_HasCriteria_ReturnsFalse_WhenAllNull()
    {
        // Arrange
        var filter = new SearchFilter();

        // Assert
        filter.HasCriteria.Should().BeFalse();
    }

    [Fact]
    public void SearchFilter_HasCriteria_ReturnsTrue_WhenOnlyHasHeadingsSet()
    {
        // Arrange
        var filter = new SearchFilter(HasHeadings: true);

        // Assert
        filter.HasCriteria.Should().BeTrue();
        filter.CriteriaCount.Should().Be(1);
    }

    [Fact]
    public void SearchFilter_HasCriteria_ReturnsTrue_WhenHasHeadingsSetToFalse()
    {
        // Arrange
        var filter = new SearchFilter(HasHeadings: false);

        // Assert
        filter.HasCriteria.Should().BeTrue();
        filter.CriteriaCount.Should().Be(1);
    }

    [Fact]
    public void SearchFilter_WithExpression_ProducesNewInstance()
    {
        // Arrange
        var original = SearchFilter.ForPath("docs/**");

        // Act
        var modified = original with { FileExtensions = new[] { "md" } };

        // Assert
        modified.Should().NotBeSameAs(original);
        modified.PathPatterns.Should().BeEquivalentTo(original.PathPatterns);
        modified.FileExtensions.Should().ContainSingle().Which.Should().Be("md");
        original.FileExtensions.Should().BeNull();
    }

    [Fact]
    public void SearchFilter_RecordEquality_SameReference_AreEqual()
    {
        // Arrange
        var pathPatterns = new[] { "docs/**" };
        var filter1 = new SearchFilter(PathPatterns: pathPatterns);
        var filter2 = new SearchFilter(PathPatterns: pathPatterns);

        // Assert - Records with same collection reference are equal
        filter1.Should().Be(filter2);
        filter1.GetHashCode().Should().Be(filter2.GetHashCode());
    }

    [Fact]
    public void SearchFilter_RecordEquality_DifferentArrays_AreNotEqual()
    {
        // Arrange - Different array instances with same content
        var filter1 = SearchFilter.ForPath("docs/**");
        var filter2 = SearchFilter.ForPath("docs/**");

        // Assert - Records with different array references are NOT equal (C# record behavior)
        // This is expected behavior - arrays use reference equality
        filter1.Should().NotBe(filter2);
    }

    [Fact]
    public void SearchFilter_HasCriteria_ReturnsFalse_WhenEmptyCollections()
    {
        // Arrange - empty collections vs null
        var filter = new SearchFilter(
            PathPatterns: Array.Empty<string>(),
            FileExtensions: Array.Empty<string>(),
            Tags: Array.Empty<string>());

        // Assert
        filter.HasCriteria.Should().BeFalse();
        filter.CriteriaCount.Should().Be(0);
    }

    #endregion

    #region DateRange Tests

    [Fact]
    public void DateRange_LastDays_CreatesOpenEndedRange()
    {
        // Act
        var range = DateRange.LastDays(7);

        // Assert
        range.Start.Should().NotBeNull();
        range.Start.Should().BeCloseTo(DateTime.UtcNow.AddDays(-7), TimeSpan.FromSeconds(1));
        range.End.Should().BeNull();
        range.IsOpenEnded.Should().BeTrue();
        range.HasBounds.Should().BeTrue();
    }

    [Fact]
    public void DateRange_LastHours_CreatesOpenEndedRange()
    {
        // Act
        var range = DateRange.LastHours(24);

        // Assert
        range.Start.Should().NotBeNull();
        range.Start.Should().BeCloseTo(DateTime.UtcNow.AddHours(-24), TimeSpan.FromSeconds(1));
        range.End.Should().BeNull();
        range.IsOpenEnded.Should().BeTrue();
    }

    [Fact]
    public void DateRange_Today_CoversCurrentDay()
    {
        // Act
        var range = DateRange.Today();

        // Assert
        range.Start.Should().Be(DateTime.UtcNow.Date);
        range.End.Should().BeCloseTo(
            DateTime.UtcNow.Date.AddDays(1).AddTicks(-1),
            TimeSpan.FromMilliseconds(1));
        range.IsOpenEnded.Should().BeFalse();
        range.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DateRange_ForMonth_CoversEntireMonth()
    {
        // Act
        var range = DateRange.ForMonth(2026, 1);

        // Assert
        range.Start.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        range.End.Should().BeCloseTo(
            new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            TimeSpan.FromSeconds(1));
        range.IsOpenEnded.Should().BeFalse();
    }

    [Fact]
    public void DateRange_ForMonth_December_CoversEntireMonth()
    {
        // Act - Test month boundary crossing
        var range = DateRange.ForMonth(2025, 12);

        // Assert
        range.Start.Should().Be(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
        range.End.Should().BeCloseTo(
            new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DateRange_IsValid_ReturnsTrue_WhenStartBeforeEnd()
    {
        // Arrange
        var range = new DateRange(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);

        // Assert
        range.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DateRange_IsValid_ReturnsFalse_WhenStartAfterEnd()
    {
        // Arrange
        var range = new DateRange(
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1));

        // Assert
        range.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DateRange_IsValid_ReturnsTrue_WhenSameStartAndEnd()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var range = new DateRange(now, now);

        // Assert
        range.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DateRange_IsOpenEnded_ReturnsTrue_WhenOnlyStart()
    {
        // Arrange
        var range = new DateRange(DateTime.UtcNow, null);

        // Assert
        range.IsOpenEnded.Should().BeTrue();
        range.HasBounds.Should().BeTrue();
    }

    [Fact]
    public void DateRange_IsOpenEnded_ReturnsTrue_WhenOnlyEnd()
    {
        // Arrange
        var range = new DateRange(null, DateTime.UtcNow);

        // Assert
        range.IsOpenEnded.Should().BeTrue();
        range.HasBounds.Should().BeTrue();
    }

    [Fact]
    public void DateRange_HasBounds_ReturnsFalse_WhenBothNull()
    {
        // Arrange
        var range = new DateRange(null, null);

        // Assert
        range.HasBounds.Should().BeFalse();
        range.IsOpenEnded.Should().BeTrue();
        range.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DateRange_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var time = DateTime.UtcNow;
        var range1 = new DateRange(time, time.AddDays(1));
        var range2 = new DateRange(time, time.AddDays(1));

        // Assert
        range1.Should().Be(range2);
        range1.GetHashCode().Should().Be(range2.GetHashCode());
    }

    #endregion

    #region FilterPreset Tests

    [Fact]
    public void FilterPreset_Create_GeneratesNewId()
    {
        // Act
        var preset = FilterPreset.Create("Test", SearchFilter.Empty);

        // Assert
        preset.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void FilterPreset_Create_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var preset = FilterPreset.Create("Test", SearchFilter.Empty);

        // Assert
        var after = DateTime.UtcNow;
        preset.CreatedAt.Should().BeOnOrAfter(before);
        preset.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void FilterPreset_Create_SetsNameAndFilter()
    {
        // Arrange
        var filter = SearchFilter.ForPath("docs/**");

        // Act
        var preset = FilterPreset.Create("API Docs", filter);

        // Assert
        preset.Name.Should().Be("API Docs");
        preset.Filter.Should().Be(filter);
    }

    [Fact]
    public void FilterPreset_Rename_PreservesOtherProperties()
    {
        // Arrange
        var original = FilterPreset.Create("Original", SearchFilter.ForPath("docs/**"));

        // Act
        var renamed = original.Rename("Renamed");

        // Assert
        renamed.Name.Should().Be("Renamed");
        renamed.Id.Should().Be(original.Id);
        renamed.Filter.Should().Be(original.Filter);
        renamed.CreatedAt.Should().Be(original.CreatedAt);
        renamed.IsShared.Should().Be(original.IsShared);
        original.Name.Should().Be("Original");
    }

    [Fact]
    public void FilterPreset_UpdateFilter_PreservesIdAndCreatedAt()
    {
        // Arrange
        var original = FilterPreset.Create("Test", SearchFilter.ForPath("docs/**"));
        var newFilter = SearchFilter.ForExtensions("md", "txt");

        // Act
        var updated = original.UpdateFilter(newFilter);

        // Assert
        updated.Filter.Should().Be(newFilter);
        updated.Id.Should().Be(original.Id);
        updated.Name.Should().Be(original.Name);
        updated.CreatedAt.Should().Be(original.CreatedAt);
        original.Filter.PathPatterns.Should().NotBeNull();
    }

    [Fact]
    public void FilterPreset_IsShared_DefaultsToFalse()
    {
        // Act
        var preset = FilterPreset.Create("Test", SearchFilter.Empty);

        // Assert
        preset.IsShared.Should().BeFalse();
    }

    [Fact]
    public void FilterPreset_Constructor_AllowsSettingIsShared()
    {
        // Act
        var preset = new FilterPreset(
            Guid.NewGuid(),
            "Shared Preset",
            SearchFilter.Empty,
            DateTime.UtcNow,
            IsShared: true);

        // Assert
        preset.IsShared.Should().BeTrue();
    }

    [Fact]
    public void FilterPreset_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var time = DateTime.UtcNow;
        var filter = SearchFilter.Empty;

        var preset1 = new FilterPreset(id, "Test", filter, time, false);
        var preset2 = new FilterPreset(id, "Test", filter, time, false);

        // Assert
        preset1.Should().Be(preset2);
        preset1.GetHashCode().Should().Be(preset2.GetHashCode());
    }

    [Fact]
    public void FilterPreset_Create_MultipleCallsGenerateDifferentIds()
    {
        // Act
        var preset1 = FilterPreset.Create("Test1", SearchFilter.Empty);
        var preset2 = FilterPreset.Create("Test2", SearchFilter.Empty);

        // Assert
        preset1.Id.Should().NotBe(preset2.Id);
    }

    #endregion
}
