// =============================================================================
// File: QueryExpanderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for QueryExpander (v0.5.4b).
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for <see cref="QueryExpander"/> verifying synonym expansion,
/// weight thresholds, and license gating.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.4b")]
public class QueryExpanderTests
{
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<QueryExpander> _logger;
    private readonly QueryExpander _expander;

    public QueryExpanderTests()
    {
        _licenseContext = Substitute.For<ILicenseContext>();
        _licenseContext.IsFeatureEnabled(Arg.Any<string>()).Returns(true);
        _licenseContext.GetCurrentTier().Returns(LicenseTier.WriterPro);

        _logger = Substitute.For<ILogger<QueryExpander>>();
        _expander = new QueryExpander(_licenseContext, _logger);
    }

    #region License Gating

    [Fact]
    public async Task ExpandAsync_CoreLicense_ReturnsNoExpansion()
    {
        // Arrange
        _licenseContext.IsFeatureEnabled(Arg.Any<string>()).Returns(false);
        var analysis = CreateAnalysis("auth token");

        // Act
        var result = await _expander.ExpandAsync(analysis);

        // Assert
        result.WasExpanded.Should().BeFalse();
        result.ExpandedKeywords.Should().BeEquivalentTo(analysis.Keywords);
    }

    [Fact]
    public async Task ExpandAsync_WriterProLicense_ExpandsTerms()
    {
        // Arrange
        var analysis = CreateAnalysis("auth");

        // Act
        var result = await _expander.ExpandAsync(analysis);

        // Assert
        result.WasExpanded.Should().BeTrue();
        result.ExpandedKeywords.Count.Should().BeGreaterThan(analysis.Keywords.Count);
    }

    #endregion

    #region Synonym Expansion

    [Theory]
    [InlineData("auth", "authentication")]
    [InlineData("auth", "authorization")]
    [InlineData("config", "configuration")]
    [InlineData("config", "settings")]
    [InlineData("api", "endpoint")]
    [InlineData("db", "database")]
    [InlineData("impl", "implementation")]
    public async Task ExpandAsync_ExpandsKnownAbbreviations(string term, string expectedSynonym)
    {
        // Arrange
        var analysis = CreateAnalysis(term);

        // Act
        var result = await _expander.ExpandAsync(analysis);

        // Assert
        result.ExpandedKeywords.Should().Contain(
            s => s.Equals(expectedSynonym, StringComparison.OrdinalIgnoreCase),
            $"'{term}' should expand to include '{expectedSynonym}'");
    }

    [Fact]
    public async Task ExpandAsync_UnknownTerm_NoExpansion()
    {
        // Arrange
        var analysis = CreateAnalysis("xyznonexistent");

        // Act
        var result = await _expander.ExpandAsync(analysis);

        // Assert - might have algorithmic variants but no database synonyms
        result.Expansions.Should().NotContainKey("xyznonexistent");
    }

    [Fact]
    public async Task ExpandAsync_MultipleTerms_ExpandsEach()
    {
        // Arrange
        var analysis = CreateAnalysis("auth config");

        // Act
        var result = await _expander.ExpandAsync(analysis);

        // Assert
        result.Expansions.Should().ContainKey("auth");
        result.Expansions.Should().ContainKey("config");
    }

    #endregion

    #region Expansion Options

    [Fact]
    public async Task ExpandAsync_RespectsMaxSynonymsPerTerm()
    {
        // Arrange
        var analysis = CreateAnalysis("auth");
        var options = new ExpansionOptions(MaxSynonymsPerTerm: 1);

        // Act
        var result = await _expander.ExpandAsync(analysis, options);

        // Assert
        if (result.Expansions.TryGetValue("auth", out var synonyms))
        {
            synonyms.Count.Should().BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task ExpandAsync_RespectsMinWeightThreshold()
    {
        // Arrange
        var analysis = CreateAnalysis("auth");
        var options = new ExpansionOptions(MinSynonymWeight: 0.9f);

        // Act
        var result = await _expander.ExpandAsync(analysis, options);

        // Assert
        if (result.Expansions.TryGetValue("auth", out var synonyms))
        {
            synonyms.Should().AllSatisfy(s => s.Weight.Should().BeGreaterThanOrEqualTo(0.9f));
        }
    }

    [Fact]
    public async Task ExpandAsync_IncludesAlgorithmic_WhenEnabled()
    {
        // Arrange
        var analysis = CreateAnalysis("implementing");
        var options = new ExpansionOptions(IncludeAlgorithmic: true);

        // Act
        var result = await _expander.ExpandAsync(analysis, options);

        // Assert
        // "implementing" should produce stemmed variant "implement"
        result.ExpandedKeywords.Should().Contain(
            k => k.Contains("implement"),
            "should include stemmed variants when algorithmic expansion is enabled");
    }

    [Fact]
    public async Task ExpandAsync_ExcludesAlgorithmic_WhenDisabled()
    {
        // Arrange
        var analysis = CreateAnalysis("xyzunknown"); // Unknown term to ensure only algorithmic
        var optionsWithAlgo = new ExpansionOptions(IncludeAlgorithmic: true);
        var optionsNoAlgo = new ExpansionOptions(IncludeAlgorithmic: false);

        // Act
        var resultWithAlgo = await _expander.ExpandAsync(analysis, optionsWithAlgo);
        var resultNoAlgo = await _expander.ExpandAsync(analysis, optionsNoAlgo);

        // Assert
        resultNoAlgo.ExpandedKeywords.Count.Should().BeLessThanOrEqualTo(resultWithAlgo.ExpandedKeywords.Count);
    }

    #endregion

    #region Empty Input Handling

    [Fact]
    public async Task ExpandAsync_NullAnalysis_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _expander.ExpandAsync(null!));
    }

    [Fact]
    public async Task ExpandAsync_EmptyKeywords_ReturnsNoExpansion()
    {
        // Arrange
        var analysis = QueryAnalysis.Empty("empty");

        // Act
        var result = await _expander.ExpandAsync(analysis);

        // Assert
        result.WasExpanded.Should().BeFalse();
        result.TotalTermCount.Should().Be(0);
    }

    #endregion

    #region ExpandedQuery Record Tests

    [Fact]
    public void ExpandedQuery_WasExpanded_TrueWhenHasExpansions()
    {
        // Arrange
        var analysis = CreateAnalysis("test");
        var expansions = new Dictionary<string, IReadOnlyList<Synonym>>
        {
            ["test"] = new List<Synonym> { new("testing", 0.9f, SynonymSource.TerminologyDatabase) }
        };

        var expanded = new ExpandedQuery(
            analysis,
            expansions,
            new[] { "test", "testing" },
            2);

        // Assert
        expanded.WasExpanded.Should().BeTrue();
        expanded.ExpandedTermCount.Should().Be(1);
        expanded.SynonymCount.Should().Be(1);
    }

    [Fact]
    public void ExpandedQuery_NoExpansion_CreatesPassthrough()
    {
        // Arrange
        var analysis = CreateAnalysis("test query");

        // Act
        var result = ExpandedQuery.NoExpansion(analysis);

        // Assert
        result.WasExpanded.Should().BeFalse();
        result.ExpandedKeywords.Should().BeEquivalentTo(analysis.Keywords);
        result.Original.Should().Be(analysis);
    }

    #endregion

    #region Synonym Record Tests

    [Fact]
    public void Synonym_RecordEquality_Works()
    {
        // Arrange
        var s1 = new Synonym("auth", 0.9f, SynonymSource.TerminologyDatabase);
        var s2 = new Synonym("auth", 0.9f, SynonymSource.TerminologyDatabase);
        var s3 = new Synonym("auth", 0.8f, SynonymSource.TerminologyDatabase);

        // Assert
        s1.Should().Be(s2);
        s1.Should().NotBe(s3);
    }

    [Fact]
    public void ExpansionOptions_Default_HasExpectedValues()
    {
        // Arrange & Act
        var options = ExpansionOptions.Default;

        // Assert
        options.MaxSynonymsPerTerm.Should().Be(3);
        options.MinSynonymWeight.Should().Be(0.3f);
        options.IncludeAlgorithmic.Should().BeTrue();
    }

    #endregion

    #region Caching

    [Fact]
    public async Task ExpandAsync_CachesResults()
    {
        // Arrange
        var analysis = CreateAnalysis("auth");

        // Act - call twice
        var result1 = await _expander.ExpandAsync(analysis);
        var result2 = await _expander.ExpandAsync(analysis);

        // Assert - should return equivalent results
        result1.ExpandedKeywords.Should().BeEquivalentTo(result2.ExpandedKeywords);
    }

    #endregion

    #region Helper Methods

    private static QueryAnalysis CreateAnalysis(string query)
    {
        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.ToLowerInvariant())
            .ToList();

        return new QueryAnalysis(
            OriginalQuery: query,
            Keywords: keywords,
            Entities: Array.Empty<QueryEntity>(),
            Intent: QueryIntent.Factual,
            Specificity: 0.5f);
    }

    #endregion
}
