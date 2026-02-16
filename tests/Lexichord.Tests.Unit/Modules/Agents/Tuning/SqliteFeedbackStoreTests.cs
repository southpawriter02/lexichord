// -----------------------------------------------------------------------
// <copyright file="SqliteFeedbackStoreTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Modules.Agents.Tuning.Configuration;
using Lexichord.Modules.Agents.Tuning.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="SqliteFeedbackStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor tests — Verify null argument handling</description></item>
///   <item><description>InitializeAsync tests — Verify schema creation and idempotency</description></item>
///   <item><description>StoreFeedbackAsync tests — Verify insert and retrieval of feedback records</description></item>
///   <item><description>GetFeedbackByRuleAsync tests — Verify rule-based querying, ordering, and limit</description></item>
///   <item><description>GetStatisticsAsync tests — Verify aggregated statistics with filtering</description></item>
///   <item><description>UpdatePatternCacheAsync tests — Verify pattern cache computation</description></item>
///   <item><description>ClearDataAsync tests — Verify targeted and full data deletion</description></item>
///   <item><description>GetFeedbackCountAsync tests — Verify count with optional date filtering</description></item>
///   <item><description>ExportPatternsAsync tests — Verify pattern export</description></item>
///   <item><description>DeleteOlderThanAsync tests — Verify age-based deletion</description></item>
///   <item><description>DeleteOldestAsync tests — Verify count-based deletion</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5d")]
public class SqliteFeedbackStoreTests : IDisposable
{
    #region Test Setup

    private readonly string _dbPath;
    private SqliteFeedbackStore? _store;

    public SqliteFeedbackStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lexichord_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        _store?.Dispose();
        if (File.Exists(_dbPath))
        {
            SqliteConnection.ClearAllPools();
            File.Delete(_dbPath);
        }
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="FeedbackRecord"/> with sensible defaults.
    /// </summary>
    private static FeedbackRecord CreateRecord(
        string ruleId = "RULE-001",
        FeedbackDecision decision = FeedbackDecision.Accepted,
        string? originalText = "original text",
        string? suggestedText = "suggested text",
        DateTime? timestamp = null)
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
            OriginalConfidence = 0.85,
            Timestamp = (timestamp ?? DateTime.UtcNow).ToString("O")
        };
    }

    /// <summary>
    /// Creates a <see cref="SqliteFeedbackStore"/>, initializes its schema, and returns it.
    /// </summary>
    private async Task<SqliteFeedbackStore> CreateAndInitializeStoreAsync()
    {
        var options = Options.Create(new LearningStorageOptions
        {
            DatabasePath = _dbPath
        });
        _store = new SqliteFeedbackStore(options, NullLogger<SqliteFeedbackStore>.Instance);
        await _store.InitializeAsync();
        return _store;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SqliteFeedbackStore(
            null!,
            NullLogger<SqliteFeedbackStore>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var options = Options.Create(new LearningStorageOptions
        {
            DatabasePath = _dbPath
        });
        var act = () => new SqliteFeedbackStore(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region InitializeAsync Tests

    [Fact]
    public async Task InitializeAsync_CreatesTablesSuccessfully()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act — store a record to verify the tables exist.
        var record = CreateRecord();
        var act = () => store.StoreFeedbackAsync(record);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act — calling InitializeAsync again should be idempotent.
        var act = () => store.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region StoreFeedbackAsync Tests

    [Fact]
    public async Task StoreFeedbackAsync_NullRecord_ThrowsArgumentNullException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.StoreFeedbackAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreFeedbackAsync_StoresAndRetrievesRecord()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var record = CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "bad grammar",
            suggestedText: "good grammar");

        // Act
        await store.StoreFeedbackAsync(record);
        var results = await store.GetFeedbackByRuleAsync("RULE-001");

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(record.Id);
        results[0].RuleId.Should().Be("RULE-001");
        results[0].Decision.Should().Be((int)FeedbackDecision.Accepted);
        results[0].OriginalText.Should().Be("bad grammar");
        results[0].SuggestedText.Should().Be("good grammar");
        results[0].OriginalConfidence.Should().Be(0.85);
    }

    [Fact]
    public async Task StoreFeedbackAsync_HandlesNullOptionalFields()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var record = CreateRecord(originalText: null, suggestedText: null);

        // Act
        await store.StoreFeedbackAsync(record);
        var results = await store.GetFeedbackByRuleAsync("RULE-001");

        // Assert
        results.Should().HaveCount(1);
        results[0].OriginalText.Should().BeNull();
        results[0].SuggestedText.Should().BeNull();
        results[0].FinalText.Should().BeNull();
        results[0].UserModification.Should().BeNull();
        results[0].UserComment.Should().BeNull();
        results[0].AnonymizedUserId.Should().BeNull();
    }

    #endregion

    #region GetFeedbackByRuleAsync Tests

    [Fact]
    public async Task GetFeedbackByRuleAsync_NullRuleId_ThrowsArgumentException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.GetFeedbackByRuleAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetFeedbackByRuleAsync_EmptyRuleId_ThrowsArgumentException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.GetFeedbackByRuleAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetFeedbackByRuleAsync_ReturnsMatchingRecords()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001"));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001"));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-002"));

        // Act
        var results = await store.GetFeedbackByRuleAsync("RULE-001");

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.RuleId == "RULE-001");
    }

    [Fact]
    public async Task GetFeedbackByRuleAsync_RespectsLimit()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        for (var i = 0; i < 10; i++)
        {
            await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001"));
        }

        // Act
        var results = await store.GetFeedbackByRuleAsync("RULE-001", limit: 3);

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFeedbackByRuleAsync_ReturnsOrderedByTimestampDescending()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var oldest = DateTime.UtcNow.AddDays(-3);
        var middle = DateTime.UtcNow.AddDays(-2);
        var newest = DateTime.UtcNow.AddDays(-1);

        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001", timestamp: oldest));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001", timestamp: newest));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001", timestamp: middle));

        // Act
        var results = await store.GetFeedbackByRuleAsync("RULE-001");

        // Assert — should be newest first.
        results.Should().HaveCount(3);
        DateTime.Parse(results[0].Timestamp).Should().BeAfter(DateTime.Parse(results[1].Timestamp));
        DateTime.Parse(results[1].Timestamp).Should().BeAfter(DateTime.Parse(results[2].Timestamp));
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_EmptyDatabase_ReturnsEmptyStatistics()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var stats = await store.GetStatisticsAsync();

        // Assert
        stats.TotalFeedback.Should().Be(0);
        stats.OverallAcceptanceRate.Should().Be(0);
        stats.RulesWithFeedback.Should().Be(0);
        stats.ModificationRate.Should().Be(0);
        stats.SkipRate.Should().Be(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_CalculatesCorrectAcceptanceRate()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // LOGIC: 2 accepted + 1 modified = 3 "accepted" out of 4 non-skipped.
        // Acceptance rate = (2 + 1) / 4 = 0.75.
        await store.StoreFeedbackAsync(CreateRecord(decision: FeedbackDecision.Accepted));
        await store.StoreFeedbackAsync(CreateRecord(decision: FeedbackDecision.Accepted));
        await store.StoreFeedbackAsync(CreateRecord(decision: FeedbackDecision.Modified));
        await store.StoreFeedbackAsync(CreateRecord(decision: FeedbackDecision.Rejected));
        await store.StoreFeedbackAsync(CreateRecord(decision: FeedbackDecision.Skipped));

        // Act
        var stats = await store.GetStatisticsAsync();

        // Assert
        stats.TotalFeedback.Should().Be(5);
        stats.OverallAcceptanceRate.Should().BeApproximately(0.75, 0.001);
        stats.ModificationRate.Should().BeApproximately(0.25, 0.001);
        stats.SkipRate.Should().BeApproximately(0.2, 0.001);
    }

    [Fact]
    public async Task GetStatisticsAsync_FiltersByRuleIds()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001", decision: FeedbackDecision.Accepted));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-002", decision: FeedbackDecision.Rejected));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-003", decision: FeedbackDecision.Accepted));

        var filter = new LearningStatisticsFilter
        {
            RuleIds = new[] { "RULE-001", "RULE-003" }
        };

        // Act
        var stats = await store.GetStatisticsAsync(filter);

        // Assert
        stats.TotalFeedback.Should().Be(2);
        stats.OverallAcceptanceRate.Should().Be(1.0);
        stats.RulesWithFeedback.Should().Be(2);
    }

    [Fact]
    public async Task GetStatisticsAsync_FiltersByDateRange()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var now = DateTime.UtcNow;

        await store.StoreFeedbackAsync(CreateRecord(
            decision: FeedbackDecision.Accepted, timestamp: now.AddDays(-10)));
        await store.StoreFeedbackAsync(CreateRecord(
            decision: FeedbackDecision.Accepted, timestamp: now.AddDays(-5)));
        await store.StoreFeedbackAsync(CreateRecord(
            decision: FeedbackDecision.Rejected, timestamp: now.AddDays(-1)));

        var filter = new LearningStatisticsFilter
        {
            Period = new DateRange(now.AddDays(-7), now)
        };

        // Act
        var stats = await store.GetStatisticsAsync(filter);

        // Assert — only the two records within the last 7 days should be included.
        stats.TotalFeedback.Should().Be(2);
    }

    #endregion

    #region UpdatePatternCacheAsync Tests

    [Fact]
    public async Task UpdatePatternCacheAsync_NullRuleId_ThrowsArgumentException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.UpdatePatternCacheAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdatePatternCacheAsync_EmptyRuleId_ThrowsArgumentException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.UpdatePatternCacheAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdatePatternCacheAsync_CreatesPatternCacheFromFeedback()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // LOGIC: Insert 3 identical patterns — meets the minimum threshold of 2.
        // 2 accepted + 1 rejected = 66.7% success rate => "Accepted" pattern type.
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "bad phrase",
            suggestedText: "good phrase"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "bad phrase",
            suggestedText: "good phrase"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Rejected,
            originalText: "bad phrase",
            suggestedText: "good phrase"));

        // Act
        await store.UpdatePatternCacheAsync("RULE-001");

        // Assert
        var patterns = await store.GetAcceptedPatternsAsync("RULE-001", minFrequency: 1);
        patterns.Should().HaveCount(1);
        patterns[0].OriginalPattern.Should().Be("bad phrase");
        patterns[0].SuggestedPattern.Should().Be("good phrase");
        patterns[0].SuccessRate.Should().BeApproximately(0.667, 0.01);
    }

    [Fact]
    public async Task UpdatePatternCacheAsync_ClearsOldCacheBeforeUpdating()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Insert patterns and build cache once.
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "old text",
            suggestedText: "old fix"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "old text",
            suggestedText: "old fix"));
        await store.UpdatePatternCacheAsync("RULE-001");

        var initialPatterns = await store.GetAcceptedPatternsAsync("RULE-001", minFrequency: 1);
        initialPatterns.Should().HaveCount(1);

        // Now add new patterns and update again.
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "new text",
            suggestedText: "new fix"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001",
            decision: FeedbackDecision.Accepted,
            originalText: "new text",
            suggestedText: "new fix"));

        // Act
        await store.UpdatePatternCacheAsync("RULE-001");

        // Assert — both "old text" and "new text" patterns should be present.
        var patterns = await store.GetAcceptedPatternsAsync("RULE-001", minFrequency: 1);
        patterns.Should().HaveCount(2);
    }

    #endregion

    #region ClearDataAsync Tests

    [Fact]
    public async Task ClearDataAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.ClearDataAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ClearDataAsync_ClearAll_RemovesAllData()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001"));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-002"));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-003"));

        var clearOptions = new ClearLearningDataOptions
        {
            ClearAll = true,
            ConfirmationToken = "CONFIRM"
        };

        // Act
        await store.ClearDataAsync(clearOptions);

        // Assert
        var count = await store.GetFeedbackCountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ClearDataAsync_FiltersByRuleIds()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-001"));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-002"));
        await store.StoreFeedbackAsync(CreateRecord(ruleId: "RULE-003"));

        var clearOptions = new ClearLearningDataOptions
        {
            RuleIds = new[] { "RULE-001", "RULE-003" },
            ConfirmationToken = "CONFIRM"
        };

        // Act
        await store.ClearDataAsync(clearOptions);

        // Assert — only RULE-002 should remain.
        var count = await store.GetFeedbackCountAsync();
        count.Should().Be(1);

        var remaining = await store.GetFeedbackByRuleAsync("RULE-002");
        remaining.Should().HaveCount(1);
    }

    #endregion

    #region GetFeedbackCountAsync Tests

    [Fact]
    public async Task GetFeedbackCountAsync_NoFilter_ReturnsTotalCount()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        await store.StoreFeedbackAsync(CreateRecord());
        await store.StoreFeedbackAsync(CreateRecord());
        await store.StoreFeedbackAsync(CreateRecord());

        // Act
        var count = await store.GetFeedbackCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetFeedbackCountAsync_FiltersByDateRange()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var now = DateTime.UtcNow;

        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-30)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-5)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-1)));

        var dateRange = new DateRange(now.AddDays(-7), now);

        // Act
        var count = await store.GetFeedbackCountAsync(dateRange);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region ExportPatternsAsync Tests

    [Fact]
    public async Task ExportPatternsAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Act
        var act = () => store.ExportPatternsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExportPatternsAsync_ExportsAllPatterns()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();

        // Create feedback data for two patterns, each with 2+ occurrences.
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001", decision: FeedbackDecision.Accepted,
            originalText: "pattern A", suggestedText: "fix A"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-001", decision: FeedbackDecision.Accepted,
            originalText: "pattern A", suggestedText: "fix A"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-002", decision: FeedbackDecision.Rejected,
            originalText: "pattern B", suggestedText: "fix B"));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-002", decision: FeedbackDecision.Rejected,
            originalText: "pattern B", suggestedText: "fix B"));

        await store.UpdatePatternCacheAsync("RULE-001");
        await store.UpdatePatternCacheAsync("RULE-002");

        var exportOptions = new LearningExportOptions
        {
            IncludeOriginalText = true
        };

        // Act
        var patterns = await store.ExportPatternsAsync(exportOptions);

        // Assert
        patterns.Should().HaveCount(2);
        patterns.Should().Contain(p => p.RuleId == "RULE-001");
        patterns.Should().Contain(p => p.RuleId == "RULE-002");
    }

    #endregion

    #region DeleteOlderThanAsync Tests

    [Fact]
    public async Task DeleteOlderThanAsync_DeletesRecordsOlderThanCutoff()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var now = DateTime.UtcNow;

        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-60)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-45)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-10)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-1)));

        // Act — delete records older than 30 days.
        var deleted = await store.DeleteOlderThanAsync(now.AddDays(-30));

        // Assert
        deleted.Should().Be(2);
        var remaining = await store.GetFeedbackCountAsync();
        remaining.Should().Be(2);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_ReturnsCountOfDeletedRecords()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var now = DateTime.UtcNow;

        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-100)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-90)));
        await store.StoreFeedbackAsync(CreateRecord(timestamp: now.AddDays(-80)));

        // Act
        var deleted = await store.DeleteOlderThanAsync(now.AddDays(-50));

        // Assert
        deleted.Should().Be(3);
    }

    #endregion

    #region DeleteOldestAsync Tests

    [Fact]
    public async Task DeleteOldestAsync_DeletesOldestNRecords()
    {
        // Arrange
        var store = await CreateAndInitializeStoreAsync();
        var now = DateTime.UtcNow;

        // Insert 5 records with known timestamps.
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-OLD-1", timestamp: now.AddDays(-5)));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-OLD-2", timestamp: now.AddDays(-4)));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-MID", timestamp: now.AddDays(-3)));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-NEW-1", timestamp: now.AddDays(-2)));
        await store.StoreFeedbackAsync(CreateRecord(
            ruleId: "RULE-NEW-2", timestamp: now.AddDays(-1)));

        // Act — delete the 2 oldest records.
        var deleted = await store.DeleteOldestAsync(2);

        // Assert
        deleted.Should().Be(2);
        var remaining = await store.GetFeedbackCountAsync();
        remaining.Should().Be(3);

        // Verify the oldest records were the ones deleted.
        var rule1Results = await store.GetFeedbackByRuleAsync("RULE-OLD-1");
        rule1Results.Should().BeEmpty();
        var rule2Results = await store.GetFeedbackByRuleAsync("RULE-OLD-2");
        rule2Results.Should().BeEmpty();

        // Verify the newer records remain.
        var midResults = await store.GetFeedbackByRuleAsync("RULE-MID");
        midResults.Should().HaveCount(1);
    }

    #endregion
}
