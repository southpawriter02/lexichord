// -----------------------------------------------------------------------
// <copyright file="LearningLoopServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// Unit tests for the Learning Loop Service (v0.7.5d).
//   Tests cover constructor validation, feedback recording, learning context
//   retrieval, statistics, export/import, clear operations, privacy options,
//   MediatR event handlers, and anonymization behavior.
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Agents.Events;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Tuning;
using Lexichord.Modules.Agents.Tuning.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="LearningLoopService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5d")]
public class LearningLoopServiceTests
{
    #region Test Setup

    // ── Mock Dependencies ─────────────────────────────────────────────────
    private readonly Mock<IFeedbackStore> _mockStore;
    private readonly Mock<ISettingsService> _mockSettings;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly PatternAnalyzer _analyzer;
    private readonly ILogger<LearningLoopService> _logger;

    public LearningLoopServiceTests()
    {
        _mockStore = new Mock<IFeedbackStore>();
        _mockSettings = new Mock<ISettingsService>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _analyzer = new PatternAnalyzer(NullLogger<PatternAnalyzer>.Instance);
        _logger = NullLogger<LearningLoopService>.Instance;

        // LOGIC: Default settings setup — return default values for all Get<T> calls.
        _mockSettings
            .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string _, bool d) => d);
        _mockSettings
            .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((string _, int d) => d);

        // LOGIC: Default store setup — empty results for feedback queries.
        _mockStore
            .Setup(x => x.GetFeedbackByRuleAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FeedbackRecord>());
        _mockStore
            .Setup(x => x.GetStatisticsAsync(
                It.IsAny<LearningStatisticsFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LearningStatistics.Empty);
        _mockStore
            .Setup(x => x.GetFeedbackCountAsync(
                It.IsAny<DateRange?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockStore
            .Setup(x => x.ExportPatternsAsync(
                It.IsAny<LearningExportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExportedPattern>());
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="LearningLoopService"/> with all mocked dependencies.
    /// </summary>
    /// <param name="teamsLicensed">Whether the mock license context should report Teams tier.</param>
    /// <returns>A configured <see cref="LearningLoopService"/> instance.</returns>
    private LearningLoopService CreateService(bool teamsLicensed = true)
    {
        if (teamsLicensed)
        {
            _mockLicenseContext
                .Setup(x => x.GetCurrentTier())
                .Returns(LicenseTier.Teams);
        }
        else
        {
            _mockLicenseContext
                .Setup(x => x.GetCurrentTier())
                .Returns(LicenseTier.WriterPro);
        }

        return new LearningLoopService(
            _mockStore.Object,
            _analyzer,
            _mockSettings.Object,
            _mockLicenseContext.Object,
            _logger);
    }

    /// <summary>
    /// Creates a test <see cref="FixFeedback"/> with default values.
    /// </summary>
    private static FixFeedback CreateTestFeedback(
        FeedbackDecision decision = FeedbackDecision.Accepted,
        string ruleId = "test-rule-001",
        string category = "Terminology",
        string originalText = "bad text",
        string suggestedText = "good text",
        string? finalText = null,
        string? userModification = null,
        string? anonymizedUserId = null,
        double confidence = 0.85)
    {
        return new FixFeedback
        {
            FeedbackId = Guid.NewGuid(),
            SuggestionId = Guid.NewGuid(),
            DeviationId = Guid.NewGuid(),
            RuleId = ruleId,
            Category = category,
            Decision = decision,
            OriginalText = originalText,
            SuggestedText = suggestedText,
            FinalText = finalText ?? (decision == FeedbackDecision.Rejected ? null : suggestedText),
            UserModification = userModification,
            OriginalConfidence = confidence,
            Timestamp = DateTime.UtcNow,
            AnonymizedUserId = anonymizedUserId
        };
    }

    /// <summary>
    /// Creates a list of <see cref="FeedbackRecord"/> for testing pattern extraction.
    /// </summary>
    private static IReadOnlyList<FeedbackRecord> CreateTestFeedbackRecords(
        int acceptedCount,
        int rejectedCount,
        string ruleId = "test-rule-001",
        string originalText = "bad text",
        string suggestedText = "good text")
    {
        var records = new List<FeedbackRecord>();
        for (var i = 0; i < acceptedCount; i++)
        {
            records.Add(new FeedbackRecord
            {
                Id = Guid.NewGuid().ToString(),
                SuggestionId = Guid.NewGuid().ToString(),
                DeviationId = Guid.NewGuid().ToString(),
                RuleId = ruleId,
                Category = "Terminology",
                Decision = (int)FeedbackDecision.Accepted,
                OriginalText = originalText,
                SuggestedText = suggestedText,
                OriginalConfidence = 0.85,
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }

        for (var i = 0; i < rejectedCount; i++)
        {
            records.Add(new FeedbackRecord
            {
                Id = Guid.NewGuid().ToString(),
                SuggestionId = Guid.NewGuid().ToString(),
                DeviationId = Guid.NewGuid().ToString(),
                RuleId = ruleId,
                Category = "Terminology",
                Decision = (int)FeedbackDecision.Rejected,
                OriginalText = originalText,
                SuggestedText = suggestedText,
                OriginalConfidence = 0.85,
                Timestamp = DateTime.UtcNow.ToString("O")
            });
        }

        return records;
    }

    /// <summary>
    /// Creates a test <see cref="StyleRule"/> with default values.
    /// </summary>
    private static StyleRule CreateTestRule(
        string id = "test-rule-001",
        string name = "Test Rule") =>
        new(
            Id: id,
            Name: name,
            Description: "A test rule for unit tests",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: "suggested fix");

    /// <summary>
    /// Creates a test <see cref="StyleViolation"/> with default values.
    /// </summary>
    private static StyleViolation CreateTestViolation(
        StyleRule? rule = null,
        int startOffset = 0,
        string matchedText = "test") =>
        new(
            Rule: rule ?? CreateTestRule(),
            Message: "Test violation message",
            StartOffset: startOffset,
            EndOffset: startOffset + matchedText.Length,
            StartLine: 1,
            StartColumn: startOffset,
            EndLine: 1,
            EndColumn: startOffset + matchedText.Length,
            MatchedText: matchedText,
            Suggestion: "fixed",
            Severity: ViolationSeverity.Warning);

    /// <summary>
    /// Creates a test <see cref="StyleDeviation"/> with default values.
    /// </summary>
    private static StyleDeviation CreateTestDeviation(
        string ruleId = "test-rule-001",
        string originalText = "test",
        int startOffset = 0)
    {
        var testRule = CreateTestRule(id: ruleId);
        var violation = CreateTestViolation(
            rule: testRule,
            startOffset: startOffset,
            matchedText: originalText);

        return new StyleDeviation
        {
            DeviationId = Guid.NewGuid(),
            Violation = violation,
            Location = new TextSpan(startOffset, originalText.Length),
            OriginalText = originalText,
            SurroundingContext = $"surrounding context for '{originalText}'",
            ViolatedRule = testRule,
            IsAutoFixable = true,
            Priority = DeviationPriority.Normal
        };
    }

    /// <summary>
    /// Creates a test <see cref="FixSuggestion"/> with default values.
    /// </summary>
    private static FixSuggestion CreateTestSuggestion(
        Guid? deviationId = null,
        string originalText = "test",
        string suggestedText = "fixed",
        double confidence = 0.85) =>
        new()
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviationId ?? Guid.NewGuid(),
            OriginalText = originalText,
            SuggestedText = suggestedText,
            Explanation = "Test explanation for the fix",
            Diff = TextDiff.Empty,
            Confidence = confidence,
            QualityScore = 0.85
        };

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LearningLoopService(
            null!,
            _analyzer,
            _mockSettings.Object,
            _mockLicenseContext.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("store");
    }

    [Fact]
    public void Constructor_NullAnalyzer_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LearningLoopService(
            _mockStore.Object,
            null!,
            _mockSettings.Object,
            _mockLicenseContext.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("analyzer");
    }

    [Fact]
    public void Constructor_NullSettingsService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LearningLoopService(
            _mockStore.Object,
            _analyzer,
            null!,
            _mockLicenseContext.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settingsService");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LearningLoopService(
            _mockStore.Object,
            _analyzer,
            _mockSettings.Object,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LearningLoopService(
            _mockStore.Object,
            _analyzer,
            _mockSettings.Object,
            _mockLicenseContext.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region RecordFeedbackAsync Tests

    [Fact]
    public async Task RecordFeedbackAsync_NullFeedback_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.RecordFeedbackAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("feedback");
    }

    [Fact]
    public async Task RecordFeedbackAsync_NoTeamsLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);
        var feedback = CreateTestFeedback();

        // Act
        var act = () => sut.RecordFeedbackAsync(feedback);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Teams license*");
    }

    [Fact]
    public async Task RecordFeedbackAsync_ValidFeedback_StoresRecord()
    {
        // Arrange
        var sut = CreateService();
        var feedback = CreateTestFeedback();

        // Act
        await sut.RecordFeedbackAsync(feedback);

        // Assert
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordFeedbackAsync_UpdatesPatternCacheAfterRecording()
    {
        // Arrange
        var sut = CreateService();
        var feedback = CreateTestFeedback(ruleId: "TERM-001");

        // Act
        await sut.RecordFeedbackAsync(feedback);

        // Assert
        _mockStore.Verify(
            x => x.UpdatePatternCacheAsync("TERM-001", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordFeedbackAsync_PatternCacheUpdateFailure_IsNonFatal()
    {
        // Arrange
        var sut = CreateService();
        var feedback = CreateTestFeedback();

        _mockStore
            .Setup(x => x.UpdatePatternCacheAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache update failed"));

        // Act
        var act = () => sut.RecordFeedbackAsync(feedback);

        // Assert — should not throw despite pattern cache failure.
        await act.Should().NotThrowAsync();

        // LOGIC: StoreFeedbackAsync should still have been called before the cache update failure.
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordFeedbackAsync_EnforcesRetentionPolicyAfterRecording()
    {
        // Arrange
        var sut = CreateService();
        var feedback = CreateTestFeedback();

        // LOGIC: Set up store to report a record count above the default max (10000).
        _mockStore
            .Setup(x => x.GetFeedbackCountAsync(
                It.IsAny<DateRange?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10500);

        // Act
        await sut.RecordFeedbackAsync(feedback);

        // Assert — should call DeleteOlderThanAsync (for max age) and DeleteOldestAsync (for count).
        _mockStore.Verify(
            x => x.DeleteOlderThanAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockStore.Verify(
            x => x.DeleteOldestAsync(
                It.Is<int>(count => count == 500), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetLearningContextAsync Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetLearningContextAsync_NullOrEmptyRuleId_ThrowsArgumentException(
        string? ruleId)
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.GetLearningContextAsync(ruleId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetLearningContextAsync_NoTeamsLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);

        // Act
        var act = () => sut.GetLearningContextAsync("TERM-001");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Teams license*");
    }

    [Fact]
    public async Task GetLearningContextAsync_EmptyRecords_ReturnsEmptyContext()
    {
        // Arrange
        var sut = CreateService();
        _mockStore
            .Setup(x => x.GetFeedbackByRuleAsync(
                "TERM-001", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FeedbackRecord>());

        // Act
        var result = await sut.GetLearningContextAsync("TERM-001");

        // Assert
        result.RuleId.Should().Be("TERM-001");
        result.SampleCount.Should().Be(0);
        result.AcceptanceRate.Should().Be(0);
        result.HasSufficientData.Should().BeFalse();
    }

    [Fact]
    public async Task GetLearningContextAsync_WithRecords_ReturnsContextWithPatterns()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: Create 15 records — 12 accepted, 3 rejected — to exceed the 10-sample threshold.
        var records = CreateTestFeedbackRecords(12, 3, ruleId: "TERM-001");
        _mockStore
            .Setup(x => x.GetFeedbackByRuleAsync(
                "TERM-001", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var result = await sut.GetLearningContextAsync("TERM-001");

        // Assert
        result.RuleId.Should().Be("TERM-001");
        result.SampleCount.Should().Be(15);
        result.HasSufficientData.Should().BeTrue();
        result.AcceptedPatterns.Should().NotBeNull();
        result.RejectedPatterns.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLearningContextAsync_CalculatesAcceptanceRateCorrectly()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: 8 accepted + 2 rejected = 10 total non-skipped. Rate = 8/10 = 0.8.
        var records = CreateTestFeedbackRecords(8, 2, ruleId: "TERM-001");
        _mockStore
            .Setup(x => x.GetFeedbackByRuleAsync(
                "TERM-001", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var result = await sut.GetLearningContextAsync("TERM-001");

        // Assert
        result.AcceptanceRate.Should().BeApproximately(0.8, 0.01);
    }

    [Fact]
    public async Task GetLearningContextAsync_SufficientData_GeneratesPromptEnhancement()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: Need 10+ samples and patterns that meet the threshold (count >= 3, rate >= 0.7).
        var records = CreateTestFeedbackRecords(10, 2, ruleId: "TERM-001");
        _mockStore
            .Setup(x => x.GetFeedbackByRuleAsync(
                "TERM-001", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var result = await sut.GetLearningContextAsync("TERM-001");

        // Assert
        result.HasSufficientData.Should().BeTrue();
        // LOGIC: With 10 accepted and 2 rejected out of 12 total,
        // the patterns meet the threshold and prompt enhancement should be generated.
        result.PromptEnhancement.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_NoTeamsLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);

        // Act
        var act = () => sut.GetStatisticsAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Teams license*");
    }

    [Fact]
    public async Task GetStatisticsAsync_DelegatesToStore()
    {
        // Arrange
        var sut = CreateService();
        var expectedStats = LearningStatistics.Empty;
        _mockStore
            .Setup(x => x.GetStatisticsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await sut.GetStatisticsAsync();

        // Assert
        result.Should().BeSameAs(expectedStats);
        _mockStore.Verify(
            x => x.GetStatisticsAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_PassesFilterToStore()
    {
        // Arrange
        var sut = CreateService();
        var filter = new LearningStatisticsFilter
        {
            RuleIds = new[] { "TERM-001" },
            ExcludeSkipped = true
        };

        // Act
        await sut.GetStatisticsAsync(filter);

        // Assert
        _mockStore.Verify(
            x => x.GetStatisticsAsync(filter, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ExportLearningDataAsync Tests

    [Fact]
    public async Task ExportLearningDataAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.ExportLearningDataAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task ExportLearningDataAsync_NoTeamsLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);
        var options = new LearningExportOptions();

        // Act
        var act = () => sut.ExportLearningDataAsync(options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Teams license*");
    }

    [Fact]
    public async Task ExportLearningDataAsync_IncludePatterns_ExportsPatterns()
    {
        // Arrange
        var sut = CreateService();
        var options = new LearningExportOptions { IncludePatterns = true };
        var expectedPatterns = new[]
        {
            new ExportedPattern("TERM-001", "Accepted", "bad", "good", 5, 0.9)
        };

        _mockStore
            .Setup(x => x.ExportPatternsAsync(options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPatterns);

        // Act
        var result = await sut.ExportLearningDataAsync(options);

        // Assert
        result.Version.Should().Be("1.0");
        result.Patterns.Should().HaveCount(1);
        result.Patterns[0].RuleId.Should().Be("TERM-001");
        _mockStore.Verify(
            x => x.ExportPatternsAsync(options, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportLearningDataAsync_SkipsPatterns_WhenIncludePatternsIsFalse()
    {
        // Arrange
        var sut = CreateService();
        var options = new LearningExportOptions
        {
            IncludePatterns = false,
            IncludeStatistics = false
        };

        // Act
        var result = await sut.ExportLearningDataAsync(options);

        // Assert
        result.Patterns.Should().BeEmpty();
        _mockStore.Verify(
            x => x.ExportPatternsAsync(
                It.IsAny<LearningExportOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ImportLearningDataAsync Tests

    [Fact]
    public async Task ImportLearningDataAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.ImportLearningDataAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("data");
    }

    [Fact]
    public async Task ImportLearningDataAsync_NoTeamsLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);
        var data = new LearningExport
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow
        };

        // Act
        var act = () => sut.ImportLearningDataAsync(data);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Teams license*");
    }

    [Fact]
    public async Task ImportLearningDataAsync_ValidData_ReturnsCompletedTask()
    {
        // Arrange
        var sut = CreateService();
        var data = new LearningExport
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            Patterns = new[]
            {
                new ExportedPattern("TERM-001", "Accepted", "bad", "good", 5, 0.9)
            }
        };

        // Act
        var act = () => sut.ImportLearningDataAsync(data);

        // Assert — import is not yet implemented, should complete without error.
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ClearLearningDataAsync Tests

    [Fact]
    public async Task ClearLearningDataAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.ClearLearningDataAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task ClearLearningDataAsync_NoTeamsLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);
        var options = new ClearLearningDataOptions
        {
            ClearAll = true,
            ConfirmationToken = "CONFIRM"
        };

        // Act
        var act = () => sut.ClearLearningDataAsync(options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Teams license*");
    }

    [Fact]
    public async Task ClearLearningDataAsync_InvalidConfirmationToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateService();
        var options = new ClearLearningDataOptions
        {
            ClearAll = true,
            ConfirmationToken = "WRONG"
        };

        // Act
        var act = () => sut.ClearLearningDataAsync(options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*confirmation token*");
    }

    [Fact]
    public async Task ClearLearningDataAsync_ValidConfirmationToken_DelegatesToStore()
    {
        // Arrange
        var sut = CreateService();
        var options = new ClearLearningDataOptions
        {
            ClearAll = true,
            ConfirmationToken = "CONFIRM"
        };

        // Act
        await sut.ClearLearningDataAsync(options);

        // Assert
        _mockStore.Verify(
            x => x.ClearDataAsync(options, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetPrivacyOptions Tests

    [Fact]
    public void GetPrivacyOptions_ReturnsDefaultsWhenNoSettingsStored()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var options = sut.GetPrivacyOptions();

        // Assert — should return privacy-first defaults.
        options.AnonymizeUsers.Should().BeTrue();
        options.StoreOriginalText.Should().BeFalse();
        options.MaxDataAge.Should().Be(TimeSpan.FromDays(365));
        options.ParticipateInTeamLearning.Should().BeTrue();
        options.MaxRecords.Should().Be(10000);
    }

    [Fact]
    public void GetPrivacyOptions_ReturnsStoredValuesFromSettingsService()
    {
        // Arrange
        var sut = CreateService();

        // LOGIC: Override settings to return non-default values.
        _mockSettings
            .Setup(x => x.Get("Learning:Privacy:AnonymizeUsers", true))
            .Returns(false);
        _mockSettings
            .Setup(x => x.Get("Learning:Privacy:StoreOriginalText", false))
            .Returns(true);
        _mockSettings
            .Setup(x => x.Get("Learning:Privacy:MaxDataAgeDays", 365))
            .Returns(90);
        _mockSettings
            .Setup(x => x.Get("Learning:Privacy:ParticipateInTeamLearning", true))
            .Returns(false);
        _mockSettings
            .Setup(x => x.Get("Learning:Privacy:MaxRecords", 10000))
            .Returns(5000);

        // Act
        var options = sut.GetPrivacyOptions();

        // Assert
        options.AnonymizeUsers.Should().BeFalse();
        options.StoreOriginalText.Should().BeTrue();
        options.MaxDataAge.Should().Be(TimeSpan.FromDays(90));
        options.ParticipateInTeamLearning.Should().BeFalse();
        options.MaxRecords.Should().Be(5000);
    }

    #endregion

    #region SetPrivacyOptionsAsync Tests

    [Fact]
    public async Task SetPrivacyOptionsAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var act = () => sut.SetPrivacyOptionsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task SetPrivacyOptionsAsync_WritesAllPropertiesToSettingsService()
    {
        // Arrange
        var sut = CreateService();
        var options = new LearningPrivacyOptions
        {
            AnonymizeUsers = false,
            StoreOriginalText = true,
            MaxDataAge = TimeSpan.FromDays(90),
            ParticipateInTeamLearning = false,
            MaxRecords = 5000
        };

        // Act
        await sut.SetPrivacyOptionsAsync(options);

        // Assert
        _mockSettings.Verify(x => x.Set("Learning:Privacy:AnonymizeUsers", false), Times.Once);
        _mockSettings.Verify(x => x.Set("Learning:Privacy:StoreOriginalText", true), Times.Once);
        _mockSettings.Verify(x => x.Set("Learning:Privacy:MaxDataAgeDays", 90), Times.Once);
        _mockSettings.Verify(
            x => x.Set("Learning:Privacy:ParticipateInTeamLearning", false), Times.Once);
        _mockSettings.Verify(x => x.Set("Learning:Privacy:MaxRecords", 5000), Times.Once);
    }

    [Fact]
    public async Task SetPrivacyOptionsAsync_ReturnsCompletedTask()
    {
        // Arrange
        var sut = CreateService();
        var options = new LearningPrivacyOptions();

        // Act
        var act = () => sut.SetPrivacyOptionsAsync(options);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Handle SuggestionAcceptedEvent Tests

    [Fact]
    public async Task HandleAccepted_NoTeamsLicense_SilentlySkips()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);
        INotificationHandler<SuggestionAcceptedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionAcceptedEvent.Create(deviation, suggestion);

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert — store should NOT be called when unlicensed.
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAccepted_TeamsLicensed_RecordsFeedback()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: true);
        INotificationHandler<SuggestionAcceptedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionAcceptedEvent.Create(deviation, suggestion);

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.Is<FeedbackRecord>(r => r.Decision == (int)FeedbackDecision.Accepted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAccepted_ModifiedSuggestion_RecordsModifiedDecision()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: true);
        INotificationHandler<SuggestionAcceptedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionAcceptedEvent.CreateModified(
            deviation, suggestion, "user modified text");

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.Is<FeedbackRecord>(r => r.Decision == (int)FeedbackDecision.Modified),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAccepted_StoreException_DoesNotThrow()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: true);
        INotificationHandler<SuggestionAcceptedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionAcceptedEvent.Create(deviation, suggestion);

        _mockStore
            .Setup(x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = () => handler.Handle(evt, CancellationToken.None);

        // Assert — handler should catch exceptions gracefully.
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Handle SuggestionRejectedEvent Tests

    [Fact]
    public async Task HandleRejected_NoTeamsLicense_SilentlySkips()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: false);
        INotificationHandler<SuggestionRejectedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionRejectedEvent.Create(deviation, suggestion);

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert — store should NOT be called when unlicensed.
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleRejected_TeamsLicensed_RecordsFeedback()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: true);
        INotificationHandler<SuggestionRejectedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionRejectedEvent.Create(deviation, suggestion);

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        _mockStore.Verify(
            x => x.StoreFeedbackAsync(
                It.Is<FeedbackRecord>(r => r.Decision == (int)FeedbackDecision.Rejected),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleRejected_StoreException_DoesNotThrow()
    {
        // Arrange
        var sut = CreateService(teamsLicensed: true);
        INotificationHandler<SuggestionRejectedEvent> handler = sut;

        var deviation = CreateTestDeviation();
        var suggestion = CreateTestSuggestion(deviation.DeviationId);
        var evt = SuggestionRejectedEvent.Create(deviation, suggestion);

        _mockStore
            .Setup(x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = () => handler.Handle(evt, CancellationToken.None);

        // Assert — handler should catch exceptions gracefully.
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Privacy and Anonymization Tests

    [Fact]
    public async Task RecordFeedbackAsync_AnonymizeUsersEnabled_HashesUserId()
    {
        // Arrange
        var sut = CreateService();
        var feedback = CreateTestFeedback(anonymizedUserId: "user@example.com");

        FeedbackRecord? storedRecord = null;
        _mockStore
            .Setup(x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()))
            .Callback<FeedbackRecord, CancellationToken>((r, _) => storedRecord = r)
            .Returns(Task.CompletedTask);

        // LOGIC: Default privacy has AnonymizeUsers = true.

        // Act
        await sut.RecordFeedbackAsync(feedback);

        // Assert — user ID should be hashed, not the original value.
        storedRecord.Should().NotBeNull();
        storedRecord!.AnonymizedUserId.Should().NotBe("user@example.com");
        storedRecord.AnonymizedUserId.Should().NotBeNullOrEmpty();
        storedRecord.AnonymizedUserId!.Length.Should().BeLessThan("user@example.com".Length + 10);
    }

    [Fact]
    public async Task RecordFeedbackAsync_StoreOriginalTextFalse_ReplacesTextWithPatterns()
    {
        // Arrange
        var sut = CreateService();
        var feedback = CreateTestFeedback(
            originalText: "The implementation should be refactored",
            suggestedText: "The implementation must be restructured");

        FeedbackRecord? storedRecord = null;
        _mockStore
            .Setup(x => x.StoreFeedbackAsync(
                It.IsAny<FeedbackRecord>(), It.IsAny<CancellationToken>()))
            .Callback<FeedbackRecord, CancellationToken>((r, _) => storedRecord = r)
            .Returns(Task.CompletedTask);

        // LOGIC: Default privacy has StoreOriginalText = false.

        // Act
        await sut.RecordFeedbackAsync(feedback);

        // Assert — original text should be replaced with structural patterns.
        storedRecord.Should().NotBeNull();
        storedRecord!.OriginalText.Should().NotBe("The implementation should be refactored");
        // LOGIC: Words of 5+ characters are replaced with [WORD].
        storedRecord.OriginalText.Should().Contain("[WORD]");
    }

    #endregion
}
