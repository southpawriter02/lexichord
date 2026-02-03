// =============================================================================
// File: QueryAnalyzerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for QueryAnalyzer (v0.5.4a).
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for <see cref="QueryAnalyzer"/> verifying keyword extraction,
/// entity recognition, intent detection, and specificity scoring.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.4a")]
public class QueryAnalyzerTests
{
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<QueryAnalyzer> _logger;
    private readonly QueryAnalyzer _analyzer;

    public QueryAnalyzerTests()
    {
        _licenseContext = Substitute.For<ILicenseContext>();
        _licenseContext.IsFeatureEnabled(Arg.Any<string>()).Returns(true);
        _licenseContext.GetCurrentTier().Returns(LicenseTier.WriterPro);

        _logger = Substitute.For<ILogger<QueryAnalyzer>>();
        _analyzer = new QueryAnalyzer(_licenseContext, _logger);
    }

    #region Null/Empty Handling

    [Fact]
    public void Analyze_NullQuery_ReturnsEmptyAnalysis()
    {
        // Act
        var result = _analyzer.Analyze(null);

        // Assert
        result.OriginalQuery.Should().BeEmpty();
        result.Keywords.Should().BeEmpty();
        result.Entities.Should().BeEmpty();
        result.Intent.Should().Be(QueryIntent.Factual);
        result.Specificity.Should().Be(0.0f);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Analyze_EmptyOrWhitespace_ReturnsEmptyAnalysis(string query)
    {
        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Keywords.Should().BeEmpty();
        result.Entities.Should().BeEmpty();
    }

    #endregion

    #region Keyword Extraction

    [Fact]
    public void Analyze_SimpleQuery_ExtractsKeywords()
    {
        // Arrange
        var query = "token refresh endpoint";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Keywords.Should().Contain("token");
        result.Keywords.Should().Contain("refresh");
        result.Keywords.Should().Contain("endpoint");
    }

    [Fact]
    public void Analyze_RemovesStopWords()
    {
        // Arrange
        var query = "the configuration is important for the application";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Keywords.Should().Contain("configuration");
        result.Keywords.Should().Contain("important");
        result.Keywords.Should().Contain("application");
        result.Keywords.Should().NotContain("the");
        result.Keywords.Should().NotContain("is");
        result.Keywords.Should().NotContain("for");
    }

    [Fact]
    public void Analyze_NormalizesCase()
    {
        // Arrange
        var query = "OAuth Authentication TOKEN";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Keywords.Should().AllSatisfy(k => k.Should().Be(k.ToLowerInvariant()));
    }

    [Fact]
    public void Analyze_DeduplicatesKeywords()
    {
        // Arrange
        var query = "token token token refresh";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Keywords.Should().HaveCount(2);
        result.Keywords.Should().Contain("token");
        result.Keywords.Should().Contain("refresh");
    }

    #endregion

    #region Entity Recognition

    [Theory]
    [InlineData("IQueryAnalyzer")]
    [InlineData("QueryAnalyzer")]
    [InlineData("handleAsync")]
    [InlineData("getUserById")]
    public void Analyze_RecognizesCodeIdentifiers(string identifier)
    {
        // Arrange
        var query = $"find {identifier} in the code";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Entities.Should().Contain(e =>
            e.Text == identifier && e.Type == EntityType.CodeIdentifier);
    }

    [Theory]
    [InlineData("src/Services/MyService.cs")]
    [InlineData("./config.json")]
    [InlineData("docs/api/*.md")]
    public void Analyze_RecognizesFilePaths(string path)
    {
        // Arrange
        var query = $"look in {path}";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Entities.Should().Contain(e =>
            e.Text == path && e.Type == EntityType.FilePath);
    }

    [Theory]
    [InlineData("v1.2.3")]
    [InlineData("v0.5.4a")]
    [InlineData("2.0")]
    public void Analyze_RecognizesVersionNumbers(string version)
    {
        // Arrange
        var query = $"changes in version {version}";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Entities.Should().Contain(e =>
            e.Text == version && e.Type == EntityType.VersionNumber);
    }

    [Theory]
    [InlineData("404")]
    [InlineData("HTTP 500")]
    [InlineData("ERR_001")]
    public void Analyze_RecognizesErrorCodes(string errorCode)
    {
        // Arrange
        var query = $"fix error {errorCode}";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Entities.Should().Contain(e => e.Type == EntityType.ErrorCode);
    }

    [Fact]
    public void Analyze_EntityStartIndex_IsCorrect()
    {
        // Arrange
        var query = "find IQueryAnalyzer in code";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        var entity = result.Entities.First(e => e.Text == "IQueryAnalyzer");
        entity.StartIndex.Should().Be(5); // "find " = 5 characters
    }

    #endregion

    #region Intent Detection

    [Theory]
    [InlineData("how to configure authentication", QueryIntent.Procedural)]
    [InlineData("how do I set up OAuth", QueryIntent.Procedural)]
    [InlineData("steps to deploy the application", QueryIntent.Procedural)]
    [InlineData("tutorial for API integration", QueryIntent.Procedural)]
    [InlineData("setup database connection", QueryIntent.Procedural)]
    public void Analyze_DetectsProceduralIntent(string query, QueryIntent expected)
    {
        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Intent.Should().Be(expected);
    }

    [Theory]
    [InlineData("what is OAuth", QueryIntent.Factual)]
    [InlineData("definition of JWT", QueryIntent.Factual)]
    [InlineData("what does async mean", QueryIntent.Factual)]
    [InlineData("list of supported formats", QueryIntent.Factual)]
    public void Analyze_DetectsFactualIntent(string query, QueryIntent expected)
    {
        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Intent.Should().Be(expected);
    }

    [Theory]
    [InlineData("why use dependency injection", QueryIntent.Conceptual)]
    [InlineData("explain CQRS pattern", QueryIntent.Conceptual)]
    [InlineData("understand the architecture", QueryIntent.Conceptual)]
    [InlineData("difference between REST and GraphQL", QueryIntent.Conceptual)]
    public void Analyze_DetectsConceptualIntent(string query, QueryIntent expected)
    {
        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Intent.Should().Be(expected);
    }

    [Theory]
    [InlineData("where is the config file", QueryIntent.Navigational)]
    [InlineData("find UserService.cs", QueryIntent.Navigational)]
    [InlineData("locate the settings folder", QueryIntent.Navigational)]
    [InlineData("path to appsettings.json", QueryIntent.Navigational)]
    public void Analyze_DetectsNavigationalIntent(string query, QueryIntent expected)
    {
        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Intent.Should().Be(expected);
    }

    [Fact]
    public void Analyze_DefaultsToFactualIntent()
    {
        // Arrange - query with no strong intent signals
        var query = "token refresh endpoint";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Intent.Should().Be(QueryIntent.Factual);
    }

    #endregion

    #region Specificity Scoring

    [Fact]
    public void Analyze_SingleKeyword_LowSpecificity()
    {
        // Arrange
        var query = "configuration";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Specificity.Should().BeLessThan(0.4f);
    }

    [Fact]
    public void Analyze_MultipleKeywords_HigherSpecificity()
    {
        // Arrange
        var query = "oauth token refresh endpoint implementation";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Specificity.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void Analyze_WithEntities_IncreasesSpecificity()
    {
        // Arrange
        var queryWithEntity = "IQueryAnalyzer interface";
        var queryWithoutEntity = "query analyzer interface";

        // Act
        var resultWithEntity = _analyzer.Analyze(queryWithEntity);
        var resultWithoutEntity = _analyzer.Analyze(queryWithoutEntity);

        // Assert
        resultWithEntity.Specificity.Should().BeGreaterThan(resultWithoutEntity.Specificity);
    }

    [Fact]
    public void Analyze_Specificity_ClampedTo_0_1()
    {
        // Arrange - very specific query
        var query = "IQueryAnalyzer v0.5.4a implementation details in src/Services/QueryAnalyzer.cs";

        // Act
        var result = _analyzer.Analyze(query);

        // Assert
        result.Specificity.Should().BeGreaterThanOrEqualTo(0.0f);
        result.Specificity.Should().BeLessThanOrEqualTo(1.0f);
    }

    #endregion

    #region QueryAnalysis Record Tests

    [Fact]
    public void QueryAnalysis_HasEntities_ReturnsCorrectly()
    {
        // Arrange
        var withEntities = new QueryAnalysis(
            "test",
            new[] { "test" },
            new[] { new QueryEntity("ITest", EntityType.CodeIdentifier, 0) },
            QueryIntent.Factual,
            0.5f);

        var withoutEntities = new QueryAnalysis(
            "test",
            new[] { "test" },
            Array.Empty<QueryEntity>(),
            QueryIntent.Factual,
            0.3f);

        // Assert
        withEntities.HasEntities.Should().BeTrue();
        withoutEntities.HasEntities.Should().BeFalse();
    }

    [Fact]
    public void QueryAnalysis_KeywordCount_ReturnsCorrectly()
    {
        // Arrange
        var analysis = new QueryAnalysis(
            "test query",
            new[] { "test", "query" },
            Array.Empty<QueryEntity>(),
            QueryIntent.Factual,
            0.4f);

        // Assert
        analysis.KeywordCount.Should().Be(2);
    }

    [Fact]
    public void QueryAnalysis_Empty_CreatesValidInstance()
    {
        // Act
        var empty = QueryAnalysis.Empty("original");

        // Assert
        empty.OriginalQuery.Should().Be("original");
        empty.Keywords.Should().BeEmpty();
        empty.Entities.Should().BeEmpty();
        empty.Intent.Should().Be(QueryIntent.Factual);
        empty.Specificity.Should().Be(0.0f);
    }

    #endregion
}
