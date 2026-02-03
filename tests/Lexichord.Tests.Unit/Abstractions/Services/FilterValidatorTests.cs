// =============================================================================
// File: FilterValidatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for FilterValidator (v0.5.5a).
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Services;

namespace Lexichord.Tests.Unit.Abstractions.Services;

/// <summary>
/// Unit tests for <see cref="FilterValidator"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5a")]
public class FilterValidatorTests
{
    private readonly FilterValidator _sut = new();

    #region Empty/Valid Filter Tests

    [Fact]
    public void Validate_EmptyFilter_ReturnsNoErrors()
    {
        // Arrange
        var filter = SearchFilter.Empty;

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidFilter_ReturnsNoErrors()
    {
        // Arrange
        var filter = new SearchFilter(
            PathPatterns: new[] { "docs/**", "src/**/*.md" },
            FileExtensions: new[] { "md", "txt" },
            ModifiedRange: DateRange.LastDays(7),
            HasHeadings: true);

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NullFilter_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.Validate(null!);

        act.Should().Throw<NullReferenceException>();
    }

    #endregion

    #region Path Pattern Validation

    [Fact]
    public void Validate_EmptyPathPattern_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(PathPatterns: new[] { "" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("PatternEmpty");
        errors[0].Property.Should().Be("PathPatterns[0]");
    }

    [Fact]
    public void Validate_WhitespacePathPattern_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(PathPatterns: new[] { "   " });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("PatternEmpty");
    }

    [Fact]
    public void Validate_NullBytePattern_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(PathPatterns: new[] { "docs\0secrets" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("PatternNullByte");
        errors[0].Message.Should().Contain("null byte");
    }

    [Fact]
    public void Validate_PathTraversal_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(PathPatterns: new[] { "docs/../secrets" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("PatternTraversal");
        errors[0].Message.Should().Contain("..");
    }

    [Fact]
    public void Validate_PathTraversal_AtStart_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(PathPatterns: new[] { "../config" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("PatternTraversal");
    }

    [Fact]
    public void ValidatePattern_ValidGlob_ReturnsNull()
    {
        // Act
        var error = _sut.ValidatePattern("docs/**/*.md");

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void ValidatePattern_EmptyString_ReturnsError()
    {
        // Act
        var error = _sut.ValidatePattern("");

        // Assert
        error.Should().NotBeNull();
        error!.Code.Should().Be("PatternEmpty");
        error.Property.Should().Be("PathPatterns");
    }

    [Fact]
    public void ValidatePattern_NullString_ReturnsError()
    {
        // Act
        var error = _sut.ValidatePattern(null!);

        // Assert
        error.Should().NotBeNull();
        error!.Code.Should().Be("PatternEmpty");
    }

    [Fact]
    public void ValidatePattern_PathTraversal_ReturnsError()
    {
        // Act
        var error = _sut.ValidatePattern("../secrets");

        // Assert
        error.Should().NotBeNull();
        error!.Code.Should().Be("PatternTraversal");
    }

    #endregion

    #region Extension Validation

    [Fact]
    public void Validate_EmptyExtension_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(FileExtensions: new[] { "md", "", "txt" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("ExtensionEmpty");
        errors[0].Property.Should().Be("FileExtensions");
    }

    [Fact]
    public void Validate_WhitespaceExtension_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(FileExtensions: new[] { "  " });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("ExtensionEmpty");
    }

    [Fact]
    public void Validate_ExtensionWithForwardSlash_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(FileExtensions: new[] { "md/txt" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("ExtensionInvalid");
        errors[0].Message.Should().Contain("md/txt");
    }

    [Fact]
    public void Validate_ExtensionWithBackslash_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(FileExtensions: new[] { @"md\txt" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("ExtensionInvalid");
        errors[0].Message.Should().Contain("path separators");
    }

    [Fact]
    public void Validate_ValidExtensions_ReturnsNoErrors()
    {
        // Arrange
        var filter = new SearchFilter(FileExtensions: new[] { "md", "txt", "json", "yaml" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ExtensionWithDot_ReturnsNoErrors()
    {
        // Arrange - dots in extensions are allowed (user may include them)
        var filter = new SearchFilter(FileExtensions: new[] { ".md", ".txt" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Date Range Validation

    [Fact]
    public void Validate_InvalidDateRange_ReturnsError()
    {
        // Arrange
        var filter = new SearchFilter(
            ModifiedRange: new DateRange(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().ContainSingle();
        errors[0].Code.Should().Be("DateRangeInvalid");
        errors[0].Property.Should().Be("ModifiedRange");
        errors[0].Message.Should().Contain("Start date cannot be after end date");
    }

    [Fact]
    public void Validate_ValidDateRange_ReturnsNoErrors()
    {
        // Arrange
        var filter = new SearchFilter(
            ModifiedRange: new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow));

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_OpenEndedDateRange_ReturnsNoErrors()
    {
        // Arrange
        var filter = new SearchFilter(ModifiedRange: DateRange.LastDays(7));

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_DateRangeWithNullBounds_ReturnsNoErrors()
    {
        // Arrange
        var filter = new SearchFilter(ModifiedRange: new DateRange(null, null));

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public void Validate_MultipleInvalidPatterns_ReturnsAllErrors()
    {
        // Arrange
        var filter = new SearchFilter(PathPatterns: new[] { "", "../secrets", "valid/**" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.Code == "PatternEmpty" && e.Property == "PathPatterns[0]");
        errors.Should().Contain(e => e.Code == "PatternTraversal" && e.Property == "PathPatterns[1]");
    }

    [Fact]
    public void Validate_MixedErrors_ReturnsAllErrors()
    {
        // Arrange
        var filter = new SearchFilter(
            PathPatterns: new[] { "../escape" },
            FileExtensions: new[] { "", "md/txt" },
            ModifiedRange: new DateRange(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().HaveCount(4);
        errors.Should().Contain(e => e.Code == "PatternTraversal");
        errors.Should().Contain(e => e.Code == "ExtensionEmpty");
        errors.Should().Contain(e => e.Code == "ExtensionInvalid");
        errors.Should().Contain(e => e.Code == "DateRangeInvalid");
    }

    [Fact]
    public void Validate_MultipleEmptyExtensions_ReturnsMultipleErrors()
    {
        // Arrange
        var filter = new SearchFilter(FileExtensions: new[] { "", "  ", "valid", "" });

        // Act
        var errors = _sut.Validate(filter);

        // Assert
        errors.Should().HaveCount(3);
        errors.All(e => e.Code == "ExtensionEmpty").Should().BeTrue();
    }

    #endregion

    #region FilterValidationError Record Tests

    [Fact]
    public void FilterValidationError_RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var error1 = new FilterValidationError("Code", "Message", "Property");
        var error2 = new FilterValidationError("Code", "Message", "Property");

        // Assert
        error1.Should().Be(error2);
        error1.GetHashCode().Should().Be(error2.GetHashCode());
    }

    [Fact]
    public void FilterValidationError_WithExpression_ProducesNewInstance()
    {
        // Arrange
        var original = new FilterValidationError("Code", "Message", "Property");

        // Act
        var modified = original with { Property = "NewProperty" };

        // Assert
        modified.Property.Should().Be("NewProperty");
        modified.Code.Should().Be("Code");
        original.Property.Should().Be("Property");
    }

    #endregion
}
