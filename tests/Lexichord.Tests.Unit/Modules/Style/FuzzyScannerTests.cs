using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for FuzzyScanner.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1c - Verifies fuzzy scanning behavior including
/// license gating, double-count prevention, and threshold matching.
/// </remarks>
public class FuzzyScannerTests
{
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ITerminologyRepository> _terminologyRepositoryMock;
    private readonly Mock<IDocumentTokenizer> _tokenizerMock;
    private readonly Mock<IFuzzyMatchService> _fuzzyMatchServiceMock;
    private readonly Mock<ILogger<FuzzyScanner>> _loggerMock;
    private readonly FuzzyScanner _sut;

    public FuzzyScannerTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _terminologyRepositoryMock = new Mock<ITerminologyRepository>();
        _tokenizerMock = new Mock<IDocumentTokenizer>();
        _fuzzyMatchServiceMock = new Mock<IFuzzyMatchService>();
        _loggerMock = new Mock<ILogger<FuzzyScanner>>();

        // LOGIC: Default to WriterPro tier (enabled)
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        // LOGIC: Default tokenizer returns empty
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(Array.Empty<DocumentToken>());

        // LOGIC: Default repository returns empty list
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm>());

        _sut = new FuzzyScanner(
            _licenseContextMock.Object,
            _terminologyRepositoryMock.Object,
            _tokenizerMock.Object,
            _fuzzyMatchServiceMock.Object,
            _loggerMock.Object);
    }

    #region License Gating Tests

    [Fact]
    public async Task ScanAsync_CoreLicense_ReturnsEmpty()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);

        // Act
        var result = await _sut.ScanAsync("test content", new HashSet<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_WriterProLicense_ProceedsWithScan()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        SetupScanScenario("hello", "helo", 85);

        // Act
        var result = await _sut.ScanAsync("hello world", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ScanAsync_TeamsLicense_ProceedsWithScan()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Teams);
        SetupScanScenario("hello", "helo", 85);

        // Act
        var result = await _sut.ScanAsync("hello world", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Empty Input Tests

    [Fact]
    public async Task ScanAsync_EmptyContent_ReturnsEmpty()
    {
        // Act
        var result = await _sut.ScanAsync("", new HashSet<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_WhitespaceContent_ReturnsEmpty()
    {
        // Act
        var result = await _sut.ScanAsync("   \n\t  ", new HashSet<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_NoFuzzyTerms_ReturnsEmpty()
    {
        // Arrange
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm>());

        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("hello", 0, 5) });

        // Act
        var result = await _sut.ScanAsync("hello", new HashSet<string>());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Double-Counting Prevention Tests

    [Fact]
    public async Task ScanAsync_WordAlreadyFlaggedByRegex_SkipsWord()
    {
        // Arrange
        var regexFlaggedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hello" };
        SetupScanScenario("hello", "helo", 85);

        // Act
        var result = await _sut.ScanAsync("hello world", regexFlaggedWords);

        // Assert
        result.Should().BeEmpty();
        _fuzzyMatchServiceMock.Verify(
            x => x.IsMatch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanAsync_SomeWordsFlagged_OnlyScansUnflaggedWords()
    {
        // Arrange
        var regexFlaggedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hello" };

        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken>
            {
                new("hello", 0, 5),
                new("wrld", 6, 10)
            });

        var term = CreateFuzzyTerm("world", 0.80);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("wrld", "world", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("wrld", "world")).Returns(85);

        // Act
        var result = await _sut.ScanAsync("hello wrld", regexFlaggedWords);

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchedText.Should().Be("wrld");
    }

    #endregion

    #region Fuzzy Matching Tests

    [Fact]
    public async Task ScanAsync_MatchAboveThreshold_CreatesViolation()
    {
        // Arrange
        SetupScanScenario("helo", "hello", 85, threshold: 0.80);

        // Act
        var result = await _sut.ScanAsync("helo world", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        var violation = result[0];
        violation.IsFuzzyMatch.Should().BeTrue();
        violation.FuzzyRatio.Should().Be(85);
    }

    [Fact]
    public async Task ScanAsync_MatchBelowThreshold_NoViolation()
    {
        // Arrange
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("test", 0, 4) });

        var term = CreateFuzzyTerm("testing", 0.90);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("test", "testing", 90)).Returns(false);

        // Act
        var result = await _sut.ScanAsync("test", new HashSet<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_ViolationHasCorrectRule()
    {
        // Arrange
        SetupScanScenario("helo", "hello", 85);

        // Act
        var result = await _sut.ScanAsync("helo", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        var violation = result[0];
        violation.Rule.Name.Should().Contain("Fuzzy");
        violation.Rule.Category.Should().Be(RuleCategory.Terminology);
    }

    [Fact]
    public async Task ScanAsync_ViolationHasCorrectMessage()
    {
        // Arrange
        SetupScanScenario("helo", "hello", 85, replacement: "hello");

        // Act
        var result = await _sut.ScanAsync("helo", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        var violation = result[0];
        violation.Message.Should().Contain("helo");
        violation.Message.Should().Contain("hello");
        violation.Suggestion.Should().Be("hello");
    }

    #endregion

    #region Position Calculation Tests

    [Fact]
    public async Task ScanAsync_ViolationHasCorrectLineColumn()
    {
        // Arrange
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("helo", 11, 15) });

        var term = CreateFuzzyTerm("hello", 0.80);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("helo", "hello", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("helo", "hello")).Returns(85);

        // Act
        var result = await _sut.ScanAsync("First line\nhelo world", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        var violation = result[0];
        violation.StartLine.Should().Be(2); // Second line
        violation.StartColumn.Should().Be(1);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ScanAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        SetupScanScenario("hello", "helo", 85);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ScanAsync("hello world", new HashSet<string>(), cts.Token));
    }

    #endregion

    #region v0.3.8a Scanner Accuracy Tests

    /// <summary>
    /// Verifies scanner correctly identifies matches when multiple similar terms exist.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8a - When multiple terms are configured, the scanner should
    /// match each token against all terms and report all that exceed threshold.
    /// </remarks>
    [Fact]
    public async Task ScanAsync_MultipleTerms_MatchesCorrectTerm()
    {
        // Arrange - Token "whitelst" should match "whitelist" but not "blocklist"
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("whitelst", 0, 8) });

        var whitelistTerm = CreateFuzzyTerm("whitelist", 0.80);
        var blocklistTerm = CreateFuzzyTerm("blocklist", 0.80);

        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { whitelistTerm, blocklistTerm });

        // LOGIC: "whitelst" matches "whitelist" (high ratio) but not "blocklist"
        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("whitelst", "whitelist", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("whitelst", "blocklist", 80)).Returns(false);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("whitelst", "whitelist")).Returns(88);

        // Act
        var result = await _sut.ScanAsync("whitelst", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        result[0].Rule.Name.Should().Contain("whitelist");
    }

    /// <summary>
    /// Verifies scanner matches multiple terms for the same token when applicable.
    /// </summary>
    [Fact]
    public async Task ScanAsync_TokenMatchesMultipleTerms_ReportsAllMatches()
    {
        // Arrange - Token "listt" could potentially match both "list" and "lists"
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("listt", 0, 5) });

        var listTerm = CreateFuzzyTerm("list", 0.80);
        var listsTerm = CreateFuzzyTerm("lists", 0.80);

        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { listTerm, listsTerm });

        // Both terms match the token
        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("listt", "list", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("listt", "lists", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("listt", "list")).Returns(80);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("listt", "lists")).Returns(80);

        // Act
        var result = await _sut.ScanAsync("listt", new HashSet<string>());

        // Assert
        result.Should().HaveCount(2, "token matches two different terms");
    }

    /// <summary>
    /// Verifies position calculation accuracy with multi-line content.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8a - Position accuracy is critical for editor navigation.
    /// </remarks>
    [Fact]
    public async Task ScanAsync_MultiLineContent_PositionCalculatedCorrectly()
    {
        // Arrange - Token on line 3, column 5
        // Line 1: "First line\n" (11 chars, offset 0-10)
        // Line 2: "Second line\n" (12 chars, offset 11-22)
        // Line 3: "    helo world" (token at offset 27-31)
        const string content = "First line\nSecond line\n    helo world";
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("helo", 27, 31) });

        var term = CreateFuzzyTerm("hello", 0.80);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("helo", "hello", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("helo", "hello")).Returns(85);

        // Act
        var result = await _sut.ScanAsync(content, new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        var violation = result[0];
        violation.StartLine.Should().Be(3, "token is on third line");
        violation.StartColumn.Should().Be(5, "token starts at column 5 (after 4 spaces)");
    }

    /// <summary>
    /// Verifies that FuzzyRatio in violation matches the calculated ratio.
    /// </summary>
    [Fact]
    public async Task ScanAsync_ViolationFuzzyRatio_MatchesCalculatedRatio()
    {
        // Arrange
        const int expectedRatio = 92;
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("terminolgy", 0, 10) });

        var term = CreateFuzzyTerm("terminology", 0.80);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("terminolgy", "terminology", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("terminolgy", "terminology")).Returns(expectedRatio);

        // Act
        var result = await _sut.ScanAsync("terminolgy", new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        result[0].FuzzyRatio.Should().Be(expectedRatio, "ratio should match calculated value exactly");
    }

    /// <summary>
    /// Verifies matched text extraction is accurate.
    /// </summary>
    [Fact]
    public async Task ScanAsync_MatchedText_ExtractedCorrectly()
    {
        // Arrange
        const string content = "The whitelst should work";
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new("whitelst", 4, 12) });

        var term = CreateFuzzyTerm("whitelist", 0.80);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        _fuzzyMatchServiceMock.Setup(x => x.IsMatch("whitelst", "whitelist", 80)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio("whitelst", "whitelist")).Returns(88);

        // Act
        var result = await _sut.ScanAsync(content, new HashSet<string>());

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchedText.Should().Be("whitelst", "matched text extracted from content");
        result[0].StartOffset.Should().Be(4);
        result[0].EndOffset.Should().Be(12);
    }

    #endregion

    #region Helper Methods

    private void SetupScanScenario(
        string tokenText,
        string termText,
        int ratio,
        double threshold = 0.80,
        string? replacement = null)
    {
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(It.IsAny<string>()))
            .Returns(new List<DocumentToken> { new(tokenText, 0, tokenText.Length) });

        var term = CreateFuzzyTerm(termText, threshold, replacement);
        _terminologyRepositoryMock.Setup(x => x.GetFuzzyEnabledTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleTerm> { term });

        var thresholdInt = (int)(threshold * 100);
        _fuzzyMatchServiceMock.Setup(x => x.IsMatch(tokenText, termText.ToLowerInvariant(), thresholdInt)).Returns(true);
        _fuzzyMatchServiceMock.Setup(x => x.CalculateRatio(tokenText, termText.ToLowerInvariant())).Returns(ratio);
    }

    private static StyleTerm CreateFuzzyTerm(string term, double threshold = 0.80, string? replacement = null)
    {
        return new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = Guid.NewGuid(),
            Term = term,
            FuzzyEnabled = true,
            FuzzyThreshold = threshold,
            Replacement = replacement,
            Category = "Terminology",
            Severity = "Warning",
            IsActive = true
        };
    }

    #endregion
}
