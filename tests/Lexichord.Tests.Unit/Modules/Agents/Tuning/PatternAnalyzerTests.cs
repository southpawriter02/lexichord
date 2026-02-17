// -----------------------------------------------------------------------
// <copyright file="PatternAnalyzerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Modules.Agents.Tuning;
using Lexichord.Modules.Agents.Tuning.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="PatternAnalyzer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor tests — Verify null argument handling and successful creation</description></item>
///   <item><description>ExtractAcceptedPatterns tests — Verify accepted pattern extraction logic</description></item>
///   <item><description>ExtractRejectedPatterns tests — Verify rejected pattern extraction logic</description></item>
///   <item><description>ExtractUserModifications tests — Verify user modification extraction logic</description></item>
///   <item><description>GeneratePromptEnhancement tests — Verify prompt enhancement generation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5d")]
public class PatternAnalyzerTests
{
    #region Test Setup

    /// <summary>
    /// Creates a PatternAnalyzer with a null logger instance.
    /// </summary>
    private static PatternAnalyzer CreateAnalyzer() =>
        new(NullLogger<PatternAnalyzer>.Instance);

    /// <summary>
    /// Creates a test FeedbackRecord with configurable properties.
    /// </summary>
    private static FeedbackRecord CreateRecord(
        string originalText = "original text",
        string suggestedText = "suggested text",
        FeedbackDecision decision = FeedbackDecision.Accepted,
        string ruleId = "RULE-001",
        string? userModification = null,
        string? userComment = null)
    {
        return new FeedbackRecord
        {
            Id = Guid.NewGuid().ToString(),
            SuggestionId = Guid.NewGuid().ToString(),
            DeviationId = Guid.NewGuid().ToString(),
            RuleId = ruleId,
            Category = "Grammar",
            Decision = (int)decision,
            OriginalText = originalText,
            SuggestedText = suggestedText,
            UserModification = userModification,
            OriginalConfidence = 0.85,
            Timestamp = DateTime.UtcNow.ToString("O"),
            UserComment = userComment
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PatternAnalyzer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Act
        var analyzer = CreateAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
    }

    #endregion

    #region ExtractAcceptedPatterns Tests

    [Fact]
    public void ExtractAcceptedPatterns_WithNullRecords_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = CreateAnalyzer();

        // Act
        var act = () => analyzer.ExtractAcceptedPatterns(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("records");
    }

    [Fact]
    public void ExtractAcceptedPatterns_WithEmptyRecords_ReturnsEmptyList()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = Array.Empty<FeedbackRecord>();

        // Act
        var result = analyzer.ExtractAcceptedPatterns(records);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractAcceptedPatterns_WithHighSuccessRateAndSufficientCount_ReturnsPatterns()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Pattern A: 3 accepted, 0 rejected → success rate 1.0 (>= 0.7), count 3 (>= 3)
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Modified)
        };

        // Act
        var result = analyzer.ExtractAcceptedPatterns(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].OriginalPattern.Should().Be("error text");
        result[0].SuggestedPattern.Should().Be("correct text");
        result[0].AcceptCount.Should().Be(3);
        result[0].SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public void ExtractAcceptedPatterns_WithCountLessThanThree_DoesNotIncludePattern()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Pattern A: 2 accepted → count 2 (< 3), should be excluded
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted)
        };

        // Act
        var result = analyzer.ExtractAcceptedPatterns(records);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractAcceptedPatterns_WithSuccessRateBelowThreshold_DoesNotIncludePattern()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Pattern A: 1 accepted, 2 rejected → success rate 0.33 (< 0.7), should be excluded
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Rejected),
            CreateRecord("error text", "correct text", FeedbackDecision.Rejected)
        };

        // Act
        var result = analyzer.ExtractAcceptedPatterns(records);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractAcceptedPatterns_LimitsToTopFiveOrderedByAcceptCountDescending()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>();

        // Create 7 different patterns, each with >= 3 records and success rate >= 0.7
        for (int i = 1; i <= 7; i++)
        {
            int acceptCount = i + 2; // 3, 4, 5, 6, 7, 8, 9
            for (int j = 0; j < acceptCount; j++)
            {
                records.Add(CreateRecord($"original {i}", $"suggested {i}", FeedbackDecision.Accepted));
            }
        }

        // Act
        var result = analyzer.ExtractAcceptedPatterns(records);

        // Assert
        result.Should().HaveCount(5);
        // Should be ordered by AcceptCount descending: patterns 7, 6, 5, 4, 3
        result[0].AcceptCount.Should().Be(9);
        result[1].AcceptCount.Should().Be(8);
        result[2].AcceptCount.Should().Be(7);
        result[3].AcceptCount.Should().Be(6);
        result[4].AcceptCount.Should().Be(5);
    }

    [Fact]
    public void ExtractAcceptedPatterns_ExcludesSkippedDecisionsFromCalculations()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Pattern A: 3 accepted, 5 skipped → only 3 non-skipped, success rate = 3/3 = 1.0
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Accepted),
            CreateRecord("error text", "correct text", FeedbackDecision.Skipped),
            CreateRecord("error text", "correct text", FeedbackDecision.Skipped),
            CreateRecord("error text", "correct text", FeedbackDecision.Skipped),
            CreateRecord("error text", "correct text", FeedbackDecision.Skipped),
            CreateRecord("error text", "correct text", FeedbackDecision.Skipped)
        };

        // Act
        var result = analyzer.ExtractAcceptedPatterns(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].AcceptCount.Should().Be(3);
        result[0].SuccessRate.Should().Be(1.0);
    }

    #endregion

    #region ExtractRejectedPatterns Tests

    [Fact]
    public void ExtractRejectedPatterns_WithNullRecords_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = CreateAnalyzer();

        // Act
        var act = () => analyzer.ExtractRejectedPatterns(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("records");
    }

    [Fact]
    public void ExtractRejectedPatterns_WithEmptyRecords_ReturnsEmptyList()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = Array.Empty<FeedbackRecord>();

        // Act
        var result = analyzer.ExtractRejectedPatterns(records);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractRejectedPatterns_WithLowSuccessRateAndSufficientCount_ReturnsPatterns()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Pattern A: 0 accepted, 3 rejected → success rate 0.0 (<= 0.3), count 3 (>= 3)
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected),
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected),
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected)
        };

        // Act
        var result = analyzer.ExtractRejectedPatterns(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].OriginalPattern.Should().Be("bad original");
        result[0].SuggestedPattern.Should().Be("bad suggestion");
        result[0].RejectCount.Should().Be(3);
    }

    [Fact]
    public void ExtractRejectedPatterns_ExtractsMostCommonUserCommentAsRejectionReason()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Pattern A: 4 rejected with different comments
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected, userComment: "Too formal"),
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected, userComment: "Too formal"),
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected, userComment: "Not accurate"),
            CreateRecord("bad original", "bad suggestion", FeedbackDecision.Rejected, userComment: "Too formal")
        };

        // Act
        var result = analyzer.ExtractRejectedPatterns(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].CommonRejectionReason.Should().Be("Too formal");
    }

    #endregion

    #region ExtractUserModifications Tests

    [Fact]
    public void ExtractUserModifications_WithNullRecords_ThrowsArgumentNullException()
    {
        // Arrange
        var analyzer = CreateAnalyzer();

        // Act
        var act = () => analyzer.ExtractUserModifications(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("records");
    }

    [Fact]
    public void ExtractUserModifications_WithEmptyRecords_ReturnsEmptyList()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = Array.Empty<FeedbackRecord>();

        // Act
        var result = analyzer.ExtractUserModifications(records);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ExtractUserModifications_IdentifiesMadeMoreConcise_WhenModifiedTextIsSignificantlyShorter()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // UserModification < 80% of SuggestedText length → "Made more concise"
            CreateRecord(
                suggestedText: "This is a very long and verbose suggestion that needs trimming",
                decision: FeedbackDecision.Modified,
                userModification: "Short and concise")
        };

        // Act
        var result = analyzer.ExtractUserModifications(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].Improvement.Should().Be("Made more concise");
    }

    [Fact]
    public void ExtractUserModifications_IdentifiesAddedMoreDetail_WhenModifiedTextIsSignificantlyLonger()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // UserModification > 120% of SuggestedText length → "Added more detail"
            CreateRecord(
                suggestedText: "Short text",
                decision: FeedbackDecision.Modified,
                userModification: "A much longer and more detailed version of the text with extra context added")
        };

        // Act
        var result = analyzer.ExtractUserModifications(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].Improvement.Should().Be("Added more detail");
    }

    [Fact]
    public void ExtractUserModifications_IdentifiesWordReplacements_WhenWordsAreSwapped()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            // Word replacements: similar length but different words
            CreateRecord(
                suggestedText: "The quick brown fox jumps over",
                decision: FeedbackDecision.Modified,
                userModification: "The fast brown dog jumps over")
        };

        // Act
        var result = analyzer.ExtractUserModifications(records);

        // Assert
        result.Should().HaveCount(1);
        result[0].Improvement.Should().Contain("Replaced");
        result[0].Improvement.Should().Contain("with");
    }

    [Fact]
    public void ExtractUserModifications_LimitsToTopThree()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var records = new List<FeedbackRecord>
        {
            CreateRecord(
                suggestedText: "Verbose suggestion one that is very long and wordy and unnecessary",
                decision: FeedbackDecision.Modified,
                userModification: "Short one"),
            CreateRecord(
                suggestedText: "Verbose suggestion two that is very long and wordy and unnecessary",
                decision: FeedbackDecision.Modified,
                userModification: "Short two"),
            CreateRecord(
                suggestedText: "Verbose suggestion three that is very long and wordy and unnecessary",
                decision: FeedbackDecision.Modified,
                userModification: "Short three"),
            CreateRecord(
                suggestedText: "Verbose suggestion four that is very long and wordy and unnecessary",
                decision: FeedbackDecision.Modified,
                userModification: "Short four"),
            CreateRecord(
                suggestedText: "Verbose suggestion five that is very long and wordy and unnecessary",
                decision: FeedbackDecision.Modified,
                userModification: "Short five")
        };

        // Act
        var result = analyzer.ExtractUserModifications(records);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region GeneratePromptEnhancement Tests

    [Fact]
    public void GeneratePromptEnhancement_WithSampleCountLessThanTen_ReturnsNull()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var acceptedPatterns = new List<AcceptedPattern>
        {
            new("original", "suggested", 5, 0.9)
        };

        // Act
        var result = analyzer.GeneratePromptEnhancement(
            acceptedPatterns,
            Array.Empty<RejectedPattern>(),
            Array.Empty<UserModificationExample>(),
            acceptanceRate: 0.8,
            sampleCount: 9);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GeneratePromptEnhancement_WithAcceptedPatterns_IncludesPreferredPatternsSection()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var acceptedPatterns = new List<AcceptedPattern>
        {
            new("utilize", "use", 10, 0.85)
        };

        // Act
        var result = analyzer.GeneratePromptEnhancement(
            acceptedPatterns,
            Array.Empty<RejectedPattern>(),
            Array.Empty<UserModificationExample>(),
            acceptanceRate: 0.7,
            sampleCount: 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("PREFERRED");
        result.Should().Contain("utilize");
        result.Should().Contain("use");
    }

    [Fact]
    public void GeneratePromptEnhancement_WithRejectedPatterns_IncludesAvoidedPatternsSection()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var rejectedPatterns = new List<RejectedPattern>
        {
            new("good word", "bad replacement", 8, "Sounds awkward")
        };

        // Act
        var result = analyzer.GeneratePromptEnhancement(
            Array.Empty<AcceptedPattern>(),
            rejectedPatterns,
            Array.Empty<UserModificationExample>(),
            acceptanceRate: 0.7,
            sampleCount: 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("AVOID");
        result.Should().Contain("bad replacement");
        result.Should().Contain("Sounds awkward");
    }

    [Fact]
    public void GeneratePromptEnhancement_WithAcceptanceRateBelowHalf_AddsConservativeNote()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var acceptedPatterns = new List<AcceptedPattern>
        {
            new("original", "suggested", 5, 0.8)
        };

        // Act
        var result = analyzer.GeneratePromptEnhancement(
            acceptedPatterns,
            Array.Empty<RejectedPattern>(),
            Array.Empty<UserModificationExample>(),
            acceptanceRate: 0.4,
            sampleCount: 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("conservative");
    }

    [Fact]
    public void GeneratePromptEnhancement_WithAcceptanceRateAboveNinety_AddsConfidentNote()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var acceptedPatterns = new List<AcceptedPattern>
        {
            new("original", "suggested", 5, 0.95)
        };

        // Act
        var result = analyzer.GeneratePromptEnhancement(
            acceptedPatterns,
            Array.Empty<RejectedPattern>(),
            Array.Empty<UserModificationExample>(),
            acceptanceRate: 0.95,
            sampleCount: 20);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("confident");
    }

    [Fact]
    public void GeneratePromptEnhancement_WithAllEmptyListsAndSufficientSamples_ReturnsNull()
    {
        // Arrange
        var analyzer = CreateAnalyzer();

        // Act — acceptance rate 0.7 does not trigger conservative or confident note
        var result = analyzer.GeneratePromptEnhancement(
            Array.Empty<AcceptedPattern>(),
            Array.Empty<RejectedPattern>(),
            Array.Empty<UserModificationExample>(),
            acceptanceRate: 0.7,
            sampleCount: 20);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
