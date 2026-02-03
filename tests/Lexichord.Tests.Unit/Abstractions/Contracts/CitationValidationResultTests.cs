// =============================================================================
// File: CitationValidationResultTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the CitationValidationResult record.
// =============================================================================
// LOGIC: Verifies all computed properties and behavior of CitationValidationResult:
//   - IsStale returns true only for Stale status.
//   - IsMissing returns true only for Missing status.
//   - HasError returns true only for Error status.
//   - StatusMessage returns correct string for each status.
//   - StatusMessage includes formatted date for Stale status.
//   - StatusMessage uses ErrorMessage when status is Error.
//   - StatusMessage uses default message when Error and ErrorMessage is null.
//   - Record equality and immutability behavior.
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for <see cref="CitationValidationResult"/>.
/// Verifies computed properties, status messages, and record behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2c")]
public class CitationValidationResultTests
{
    // LOGIC: Shared test citation for creating validation results.
    private static readonly Citation TestCitation = new(
        ChunkId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DocumentPath: "/docs/test.md",
        DocumentTitle: "Test Document",
        StartOffset: 0,
        EndOffset: 100,
        Heading: "Introduction",
        LineNumber: 10,
        IndexedAt: new DateTime(2026, 1, 25, 10, 0, 0, DateTimeKind.Utc));

    // =========================================================================
    // IsStale Property
    // =========================================================================

    [Fact]
    public void IsStale_StaleStatus_ReturnsTrue()
    {
        // Arrange
        var result = CreateResult(CitationValidationStatus.Stale);

        // Assert
        Assert.True(result.IsStale);
    }

    [Theory]
    [InlineData(CitationValidationStatus.Valid)]
    [InlineData(CitationValidationStatus.Missing)]
    [InlineData(CitationValidationStatus.Error)]
    public void IsStale_NonStaleStatus_ReturnsFalse(CitationValidationStatus status)
    {
        // Arrange
        var result = CreateResult(status);

        // Assert
        Assert.False(result.IsStale);
    }

    // =========================================================================
    // IsMissing Property
    // =========================================================================

    [Fact]
    public void IsMissing_MissingStatus_ReturnsTrue()
    {
        // Arrange
        var result = CreateResult(CitationValidationStatus.Missing);

        // Assert
        Assert.True(result.IsMissing);
    }

    [Theory]
    [InlineData(CitationValidationStatus.Valid)]
    [InlineData(CitationValidationStatus.Stale)]
    [InlineData(CitationValidationStatus.Error)]
    public void IsMissing_NonMissingStatus_ReturnsFalse(CitationValidationStatus status)
    {
        // Arrange
        var result = CreateResult(status);

        // Assert
        Assert.False(result.IsMissing);
    }

    // =========================================================================
    // HasError Property
    // =========================================================================

    [Fact]
    public void HasError_ErrorStatus_ReturnsTrue()
    {
        // Arrange
        var result = CreateResult(CitationValidationStatus.Error);

        // Assert
        Assert.True(result.HasError);
    }

    [Theory]
    [InlineData(CitationValidationStatus.Valid)]
    [InlineData(CitationValidationStatus.Stale)]
    [InlineData(CitationValidationStatus.Missing)]
    public void HasError_NonErrorStatus_ReturnsFalse(CitationValidationStatus status)
    {
        // Arrange
        var result = CreateResult(status);

        // Assert
        Assert.False(result.HasError);
    }

    // =========================================================================
    // StatusMessage Property
    // =========================================================================

    [Fact]
    public void StatusMessage_ValidStatus_ReturnsCitationIsCurrent()
    {
        // Arrange
        var result = CreateResult(CitationValidationStatus.Valid);

        // Assert
        Assert.Equal("Citation is current", result.StatusMessage);
    }

    [Fact]
    public void StatusMessage_StaleStatus_IncludesModifiedDate()
    {
        // Arrange
        var modifiedAt = new DateTime(2026, 1, 27, 14, 30, 0, DateTimeKind.Utc);
        var result = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: modifiedAt,
            ErrorMessage: null);

        // Assert
        Assert.StartsWith("Source modified", result.StatusMessage);
        Assert.Contains(modifiedAt.ToString("g"), result.StatusMessage);
    }

    [Fact]
    public void StatusMessage_StaleStatus_NullModifiedAt_ReturnsRecently()
    {
        // Arrange
        var result = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: null,
            ErrorMessage: null);

        // Assert
        Assert.Equal("Source modified recently", result.StatusMessage);
    }

    [Fact]
    public void StatusMessage_MissingStatus_ReturnsSourceFileNotFound()
    {
        // Arrange
        var result = CreateResult(CitationValidationStatus.Missing);

        // Assert
        Assert.Equal("Source file not found", result.StatusMessage);
    }

    [Fact]
    public void StatusMessage_ErrorStatus_WithErrorMessage_ReturnsErrorMessage()
    {
        // Arrange
        var result = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Error,
            CurrentModifiedAt: null,
            ErrorMessage: "Access denied");

        // Assert
        Assert.Equal("Access denied", result.StatusMessage);
    }

    [Fact]
    public void StatusMessage_ErrorStatus_NullErrorMessage_ReturnsValidationFailed()
    {
        // Arrange
        var result = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Error,
            CurrentModifiedAt: null,
            ErrorMessage: null);

        // Assert
        Assert.Equal("Validation failed", result.StatusMessage);
    }

    // =========================================================================
    // Record Equality
    // =========================================================================

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var modifiedAt = DateTime.UtcNow;
        var result1 = new CitationValidationResult(
            TestCitation, true, CitationValidationStatus.Valid, modifiedAt, null);
        var result2 = new CitationValidationResult(
            TestCitation, true, CitationValidationStatus.Valid, modifiedAt, null);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void RecordEquality_DifferentStatus_AreNotEqual()
    {
        // Arrange
        var result1 = CreateResult(CitationValidationStatus.Valid);
        var result2 = CreateResult(CitationValidationStatus.Stale);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    // =========================================================================
    // IsValid Property
    // =========================================================================

    [Fact]
    public void IsValid_ValidStatus_ReturnsTrue()
    {
        // Arrange
        var result = new CitationValidationResult(
            TestCitation, true, CitationValidationStatus.Valid, DateTime.UtcNow, null);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void IsValid_StaleStatus_ReturnsFalse()
    {
        // Arrange
        var result = new CitationValidationResult(
            TestCitation, false, CitationValidationStatus.Stale, DateTime.UtcNow, null);

        // Assert
        Assert.False(result.IsValid);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a CitationValidationResult with the specified status and default values.
    /// </summary>
    private static CitationValidationResult CreateResult(CitationValidationStatus status)
    {
        return new CitationValidationResult(
            Citation: TestCitation,
            IsValid: status == CitationValidationStatus.Valid,
            Status: status,
            CurrentModifiedAt: status == CitationValidationStatus.Valid ? DateTime.UtcNow : null,
            ErrorMessage: status == CitationValidationStatus.Error ? "Test error" : null);
    }
}
