// =============================================================================
// File: FilterQueryBuilderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for FilterQueryBuilder (v0.5.5c).
// =============================================================================
// LOGIC: Tests the filter query builder for SQL generation from SearchFilter:
//   - Empty filter handling
//   - Path pattern conversion (glob to SQL LIKE)
//   - Extension filtering
//   - Date range filtering
//   - Heading filtering
//   - Combined filter scenarios
//   - Validation error handling
// =============================================================================
// VERSION: v0.5.5c (Filter Query Builder)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="FilterQueryBuilder"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5c")]
public class FilterQueryBuilderTests
{
    private readonly Mock<ILogger<FilterQueryBuilder>> _loggerMock;
    private readonly Mock<IFilterValidator> _validatorMock;
    private readonly FilterQueryBuilder _builder;

    public FilterQueryBuilderTests()
    {
        _loggerMock = new Mock<ILogger<FilterQueryBuilder>>();
        _validatorMock = new Mock<IFilterValidator>();

        // Default: validation passes
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<SearchFilter>()))
            .Returns(Array.Empty<FilterValidationError>());

        _builder = new FilterQueryBuilder(_loggerMock.Object, _validatorMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FilterQueryBuilder(null!, _validatorMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullValidator_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FilterQueryBuilder(_loggerMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validator");
    }

    #endregion

    #region Build - Empty Filter Tests

    [Fact]
    public void Build_WithEmptyFilter_ReturnsEmptyResult()
    {
        // Arrange
        var filter = SearchFilter.Empty;

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.Should().NotBeNull();
        result.HasFilters.Should().BeFalse();
        result.WhereClause.Should().Be("1=1");
        result.Parameters.Should().BeEmpty();
        result.CteClause.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithNullFilter_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _builder.Build(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Build - Path Pattern Tests

    [Fact]
    public void Build_WithSinglePathPattern_GeneratesLikeClause()
    {
        // Arrange
        var filter = SearchFilter.ForPath("docs/**");

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.CteClause.Should().Contain("file_path LIKE @pathPattern0");
        result.Parameters.Should().ContainKey("pathPattern0");
        result.Parameters["pathPattern0"].Should().Be("docs/%");
    }

    [Fact]
    public void Build_WithMultiplePathPatterns_GeneratesOrClause()
    {
        // Arrange
        var filter = new SearchFilter(
            PathPatterns: new[] { "docs/**", "src/**" });

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.CteClause.Should().Contain("file_path LIKE @pathPattern0");
        result.CteClause.Should().Contain("file_path LIKE @pathPattern1");
        result.CteClause.Should().Contain(" OR ");
        result.Parameters.Should().ContainKey("pathPattern0");
        result.Parameters.Should().ContainKey("pathPattern1");
    }

    #endregion

    #region Build - Extension Filter Tests

    [Fact]
    public void Build_WithExtensions_GeneratesAnyClause()
    {
        // Arrange
        var filter = SearchFilter.ForExtensions("md", "txt");

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.CteClause.Should().Contain("LOWER(file_extension) = ANY(@extensions)");
        result.Parameters.Should().ContainKey("extensions");
        var extensions = result.Parameters["extensions"] as string[];
        extensions.Should().Contain("md");
        extensions.Should().Contain("txt");
    }

    [Fact]
    public void Build_WithExtensionsWithDots_NormalizesExtensions()
    {
        // Arrange
        var filter = SearchFilter.ForExtensions(".md", ".TXT");

        // Act
        var result = _builder.Build(filter);

        // Assert
        var extensions = result.Parameters["extensions"] as string[];
        extensions.Should().Contain("md");
        extensions.Should().Contain("txt");
    }

    #endregion

    #region Build - Date Range Tests

    [Fact]
    public void Build_WithDateRangeStart_GeneratesStartCondition()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var filter = new SearchFilter(
            ModifiedRange: new DateRange(startDate, null));

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.CteClause.Should().Contain("modified_at >= @modifiedStart");
        result.Parameters.Should().ContainKey("modifiedStart");
        result.Parameters["modifiedStart"].Should().Be(startDate);
    }

    [Fact]
    public void Build_WithDateRangeEnd_GeneratesEndCondition()
    {
        // Arrange
        var endDate = DateTime.UtcNow;
        var filter = new SearchFilter(
            ModifiedRange: new DateRange(null, endDate));

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.CteClause.Should().Contain("modified_at <= @modifiedEnd");
        result.Parameters.Should().ContainKey("modifiedEnd");
    }

    [Fact]
    public void Build_WithDateRangeBoth_GeneratesBothConditions()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var filter = new SearchFilter(
            ModifiedRange: new DateRange(startDate, endDate));

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.CteClause.Should().Contain("modified_at >= @modifiedStart");
        result.CteClause.Should().Contain("modified_at <= @modifiedEnd");
    }

    #endregion

    #region Build - Heading Filter Tests

    [Fact]
    public void Build_WithHasHeadingsTrue_GeneratesHeadingCondition()
    {
        // Arrange
        var filter = new SearchFilter(HasHeadings: true);

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.WhereClause.Should().Contain("c.heading IS NOT NULL");
    }

    [Fact]
    public void Build_WithHasHeadingsFalse_DoesNotGenerateHeadingCondition()
    {
        // Arrange
        var filter = new SearchFilter(HasHeadings: false);

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeFalse();
        result.WhereClause.Should().NotContain("heading");
    }

    #endregion

    #region Build - Combined Filter Tests

    [Fact]
    public void Build_WithAllCriteria_GeneratesCompleteCte()
    {
        // Arrange
        var filter = new SearchFilter(
            PathPatterns: new[] { "docs/**" },
            FileExtensions: new[] { "md" },
            ModifiedRange: DateRange.LastDays(7),
            HasHeadings: true);

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeTrue();
        result.FilterCount.Should().Be(4);
        result.CteClause.Should().StartWith("WITH filtered_docs AS");
        result.WhereClause.Should().Contain("document_id IN (SELECT id FROM filtered_docs)");
        result.WhereClause.Should().Contain("heading IS NOT NULL");
    }

    #endregion

    #region Build - Validation Error Tests

    [Fact]
    public void Build_WithValidationErrors_ReturnsEmptyResult()
    {
        // Arrange
        var filter = SearchFilter.ForPath("../secret");
        _validatorMock
            .Setup(v => v.Validate(filter))
            .Returns(new[] { new FilterValidationError("PatternTraversal", "Path traversal detected", "PathPatterns[0]") });

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.HasFilters.Should().BeFalse();
        result.WhereClause.Should().Be("1=1");
    }

    #endregion

    #region ConvertGlobToSql Tests

    [Theory]
    [InlineData("", "%")]
    [InlineData("docs/**", "docs/%")]
    [InlineData("**/*.md", "%/%.md")]
    [InlineData("src/*.cs", "src/%.cs")]
    [InlineData("file?.txt", "file_.txt")]
    [InlineData("docs/**/api/**", "docs/%/api/%")]
    public void ConvertGlobToSql_ConvertsPatternCorrectly(string glob, string expected)
    {
        // Act
        var result = _builder.ConvertGlobToSql(glob);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertGlobToSql_EscapesPercent()
    {
        // Arrange
        var glob = "file%name.txt";

        // Act
        var result = _builder.ConvertGlobToSql(glob);

        // Assert
        result.Should().Be(@"file\%name.txt");
    }

    [Fact]
    public void ConvertGlobToSql_EscapesUnderscore()
    {
        // Arrange
        var glob = "file_name.txt";

        // Act
        var result = _builder.ConvertGlobToSql(glob);

        // Assert
        result.Should().Be(@"file\_name.txt");
    }

    #endregion

    #region TryBuild Tests

    [Fact]
    public void TryBuild_WithValidFilter_ReturnsTrue()
    {
        // Arrange
        var filter = SearchFilter.ForPath("docs/**");

        // Act
        var success = _builder.TryBuild(filter, out var errors);

        // Assert
        success.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void TryBuild_WithInvalidFilter_ReturnsFalse()
    {
        // Arrange
        var filter = SearchFilter.ForPath("../secret");
        _validatorMock
            .Setup(v => v.Validate(filter))
            .Returns(new[] { new FilterValidationError("PatternTraversal", "Path traversal detected", "PathPatterns[0]") });

        // Act
        var success = _builder.TryBuild(filter, out var errors);

        // Assert
        success.Should().BeFalse();
        errors.Should().HaveCount(1);
        errors[0].Code.Should().Be("PatternTraversal");
    }

    #endregion

    #region LastResult Tests

    [Fact]
    public void LastResult_AfterBuild_ContainsLastResult()
    {
        // Arrange
        var filter = SearchFilter.ForPath("docs/**");

        // Act
        var result = _builder.Build(filter);

        // Assert
        _builder.LastResult.Should().Be(result);
    }

    [Fact]
    public void LastResult_BeforeAnyBuild_IsNull()
    {
        // Assert
        _builder.LastResult.Should().BeNull();
    }

    #endregion

    #region Summary Tests

    [Fact]
    public void Build_GeneratesDescriptiveSummary()
    {
        // Arrange
        var filter = SearchFilter.ForPath("docs/**") with
        {
            FileExtensions = new[] { "md", "txt" }
        };

        // Act
        var result = _builder.Build(filter);

        // Assert
        result.Summary.Should().Contain("Path:");
        result.Summary.Should().Contain("Extensions:");
    }

    #endregion
}
