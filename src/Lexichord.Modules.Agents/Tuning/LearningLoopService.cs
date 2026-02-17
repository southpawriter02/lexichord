// -----------------------------------------------------------------------
// <copyright file="LearningLoopService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Agents.Events;
using Lexichord.Modules.Agents.Tuning.Storage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Central orchestrator for the Learning Loop feedback system.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="LearningLoopService"/> implements three concerns:
/// <list type="bullet">
///   <item><description>
///     <see cref="ILearningLoopService"/> — Public API for feedback recording,
///     learning context retrieval, statistics, export/import, and privacy management.
///   </description></item>
///   <item><description>
///     <see cref="INotificationHandler{SuggestionAcceptedEvent}"/> — Automatically records
///     feedback when users accept or modify suggestions via the Tuning Panel.
///   </description></item>
///   <item><description>
///     <see cref="INotificationHandler{SuggestionRejectedEvent}"/> — Automatically records
///     feedback when users reject suggestions via the Tuning Panel.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>DI Registration:</b> Registered as a singleton. MediatR handler registrations
/// are forwarded to the same singleton instance to ensure consistent state.
/// </para>
/// <para>
/// <b>License Gating:</b> Requires Teams tier for all public API methods.
/// MediatR handlers (accept/reject) silently skip recording when Teams license
/// is not available — this prevents errors during normal usage while the feature
/// remains gated.
/// </para>
/// <para>
/// <b>Privacy:</b> All feedback is processed through privacy filters before storage.
/// User IDs are hashed via SHA256 when anonymization is enabled. Original text is
/// replaced with structural patterns when <see cref="LearningPrivacyOptions.StoreOriginalText"/>
/// is <c>false</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="ILearningLoopService"/>
/// <seealso cref="IFeedbackStore"/>
/// <seealso cref="PatternAnalyzer"/>
internal sealed class LearningLoopService :
    ILearningLoopService,
    INotificationHandler<SuggestionAcceptedEvent>,
    INotificationHandler<SuggestionRejectedEvent>
{
    #region Constants

    /// <summary>
    /// Settings key prefix for privacy options.
    /// </summary>
    private const string PrivacyKeyPrefix = "Learning:Privacy:";

    /// <summary>
    /// Current export format version.
    /// </summary>
    private const string ExportVersion = "1.0";

    /// <summary>
    /// Confirmation token required for clear operations.
    /// </summary>
    private const string RequiredConfirmationToken = "CONFIRM";

    #endregion

    #region Fields

    private readonly IFeedbackStore _store;
    private readonly PatternAnalyzer _analyzer;
    private readonly ISettingsService _settingsService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<LearningLoopService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="LearningLoopService"/> class.
    /// </summary>
    /// <param name="store">The feedback persistence store.</param>
    /// <param name="analyzer">The pattern analyzer.</param>
    /// <param name="settingsService">The settings service for privacy preferences.</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public LearningLoopService(
        IFeedbackStore store,
        PatternAnalyzer analyzer,
        ISettingsService settingsService,
        ILicenseContext licenseContext,
        ILogger<LearningLoopService> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(licenseContext);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _analyzer = analyzer;
        _settingsService = settingsService;
        _licenseContext = licenseContext;
        _logger = logger;

        _logger.LogDebug("LearningLoopService initialized");
    }

    #endregion

    #region ILearningLoopService Implementation

    /// <inheritdoc />
    public async Task RecordFeedbackAsync(FixFeedback feedback, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedback);
        EnsureTeamsLicense();

        _logger.LogDebug(
            "Recording feedback: {Decision} for rule {RuleId}",
            feedback.Decision, feedback.RuleId);

        // LOGIC: Apply privacy options before storage
        var privacyOptions = GetPrivacyOptions();
        var processedFeedback = ApplyPrivacyOptions(feedback, privacyOptions);

        // LOGIC: Convert to storage record
        var record = MapToRecord(processedFeedback);
        await _store.StoreFeedbackAsync(record, ct);

        // LOGIC: Trigger pattern cache update for the affected rule
        try
        {
            await _store.UpdatePatternCacheAsync(feedback.RuleId, ct);
        }
        catch (Exception ex)
        {
            // LOGIC: Pattern cache update failure is non-critical — log and continue
            _logger.LogWarning(ex,
                "Failed to update pattern cache for rule {RuleId}",
                feedback.RuleId);
        }

        // LOGIC: Enforce retention policy after recording
        await EnforceRetentionPolicyAsync(privacyOptions, ct);

        _logger.LogDebug(
            "Feedback recorded successfully: {FeedbackId}",
            feedback.FeedbackId);
    }

    /// <inheritdoc />
    public async Task<LearningContext> GetLearningContextAsync(
        string ruleId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleId);
        EnsureTeamsLicense();

        _logger.LogDebug("Getting learning context for rule {RuleId}", ruleId);

        // LOGIC: Fetch feedback records for this rule
        var records = await _store.GetFeedbackByRuleAsync(ruleId, 1000, ct);

        if (records.Count == 0)
        {
            _logger.LogDebug(
                "No feedback data for rule {RuleId}, returning empty context",
                ruleId);
            return LearningContext.Empty(ruleId);
        }

        // LOGIC: Extract patterns using the analyzer
        var acceptedPatterns = _analyzer.ExtractAcceptedPatterns(records);
        var rejectedPatterns = _analyzer.ExtractRejectedPatterns(records);
        var modifications = _analyzer.ExtractUserModifications(records);

        // LOGIC: Calculate acceptance rate
        var nonSkipped = records.Count(r => r.Decision != (int)FeedbackDecision.Skipped);
        var accepted = records.Count(r =>
            r.Decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified);
        var acceptanceRate = nonSkipped > 0 ? (double)accepted / nonSkipped : 0;

        // LOGIC: Generate prompt enhancement text
        var promptEnhancement = _analyzer.GeneratePromptEnhancement(
            acceptedPatterns,
            rejectedPatterns,
            modifications,
            acceptanceRate,
            records.Count);

        var context = new LearningContext
        {
            RuleId = ruleId,
            AcceptanceRate = acceptanceRate,
            SampleCount = records.Count,
            AcceptedPatterns = acceptedPatterns,
            RejectedPatterns = rejectedPatterns,
            UsefulModifications = modifications,
            PromptEnhancement = promptEnhancement,
            AdjustedConfidenceBaseline = records.Count >= 10 ? acceptanceRate : null
        };

        _logger.LogInformation(
            "Learning context generated: {SampleCount} samples for rule {RuleId}",
            records.Count, ruleId);

        return context;
    }

    /// <inheritdoc />
    public async Task<LearningStatistics> GetStatisticsAsync(
        LearningStatisticsFilter? filter = null,
        CancellationToken ct = default)
    {
        EnsureTeamsLicense();

        _logger.LogDebug("Getting learning statistics");

        var stats = await _store.GetStatisticsAsync(filter, ct);

        _logger.LogDebug(
            "Statistics retrieved: {TotalFeedback} total, {AcceptanceRate:P1} acceptance rate",
            stats.TotalFeedback, stats.OverallAcceptanceRate);

        return stats;
    }

    /// <inheritdoc />
    public async Task<LearningExport> ExportLearningDataAsync(
        LearningExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureTeamsLicense();

        _logger.LogDebug("Exporting learning data");

        var patterns = options.IncludePatterns
            ? await _store.ExportPatternsAsync(options, ct)
            : Array.Empty<ExportedPattern>();

        LearningStatistics? statistics = null;
        if (options.IncludeStatistics)
        {
            var filter = new LearningStatisticsFilter { Period = options.Period };
            statistics = await _store.GetStatisticsAsync(filter, ct);
        }

        var export = new LearningExport
        {
            Version = ExportVersion,
            ExportedAt = DateTime.UtcNow,
            Patterns = patterns,
            Statistics = statistics
        };

        _logger.LogInformation(
            "Learning data exported: {PatternCount} patterns",
            patterns.Count);

        return export;
    }

    /// <inheritdoc />
    public Task ImportLearningDataAsync(LearningExport data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        EnsureTeamsLicense();

        _logger.LogInformation(
            "Learning data import requested: {PatternCount} patterns, version {Version}",
            data.Patterns.Count, data.Version);

        // LOGIC: Import is a future enhancement — for now, log the request.
        // Full import requires merging pattern frequencies and deduplication.
        _logger.LogWarning(
            "Learning data import is not yet implemented. {PatternCount} patterns were not imported.",
            data.Patterns.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ClearLearningDataAsync(
        ClearLearningDataOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureTeamsLicense();

        // LOGIC: Require confirmation token to prevent accidental data loss
        if (options.ConfirmationToken != RequiredConfirmationToken)
        {
            throw new InvalidOperationException(
                $"Invalid confirmation token. Expected '{RequiredConfirmationToken}'.");
        }

        _logger.LogInformation(
            "Clearing learning data: ClearAll={ClearAll}",
            options.ClearAll);

        await _store.ClearDataAsync(options, ct);

        _logger.LogInformation("Learning data cleared successfully");
    }

    /// <inheritdoc />
    public LearningPrivacyOptions GetPrivacyOptions()
    {
        // LOGIC: Read privacy settings from ISettingsService, using privacy-first defaults
        return new LearningPrivacyOptions
        {
            AnonymizeUsers = _settingsService.Get($"{PrivacyKeyPrefix}AnonymizeUsers", true),
            StoreOriginalText = _settingsService.Get($"{PrivacyKeyPrefix}StoreOriginalText", false),
            MaxDataAge = TimeSpan.FromDays(
                _settingsService.Get($"{PrivacyKeyPrefix}MaxDataAgeDays", 365)),
            ParticipateInTeamLearning = _settingsService.Get(
                $"{PrivacyKeyPrefix}ParticipateInTeamLearning", true),
            MaxRecords = _settingsService.Get($"{PrivacyKeyPrefix}MaxRecords", 10000)
        };
    }

    /// <inheritdoc />
    public Task SetPrivacyOptionsAsync(
        LearningPrivacyOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug("Updating privacy options");

        // LOGIC: Persist each privacy setting individually
        _settingsService.Set($"{PrivacyKeyPrefix}AnonymizeUsers", options.AnonymizeUsers);
        _settingsService.Set($"{PrivacyKeyPrefix}StoreOriginalText", options.StoreOriginalText);
        _settingsService.Set($"{PrivacyKeyPrefix}MaxDataAgeDays",
            options.MaxDataAge?.Days ?? 0);
        _settingsService.Set($"{PrivacyKeyPrefix}ParticipateInTeamLearning",
            options.ParticipateInTeamLearning);
        _settingsService.Set($"{PrivacyKeyPrefix}MaxRecords", options.MaxRecords);

        _logger.LogInformation(
            "Privacy options updated: AnonymizeUsers={Anonymize}, StoreOriginalText={StoreText}",
            options.AnonymizeUsers, options.StoreOriginalText);

        return Task.CompletedTask;
    }

    #endregion

    #region MediatR Handlers

    /// <summary>
    /// Handles <see cref="SuggestionAcceptedEvent"/> by recording accept/modify feedback.
    /// </summary>
    /// <param name="notification">The accepted event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Silently skips recording when Teams license is not available.
    /// Creates a <see cref="FixFeedback"/> with <see cref="FeedbackDecision.Accepted"/> or
    /// <see cref="FeedbackDecision.Modified"/> based on <see cref="SuggestionAcceptedEvent.IsModified"/>.
    /// </remarks>
    public async Task Handle(SuggestionAcceptedEvent notification, CancellationToken ct)
    {
        // LOGIC: Silently skip if no Teams license — feature is gated but events fire regardless
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogDebug(
                "Learning Loop requires Teams license, skipping accept feedback recording");
            return;
        }

        try
        {
            var feedback = new FixFeedback
            {
                FeedbackId = Guid.NewGuid(),
                SuggestionId = notification.Suggestion.SuggestionId,
                DeviationId = notification.Deviation.DeviationId,
                RuleId = notification.Deviation.RuleId,
                Category = notification.Deviation.Category,
                Decision = notification.IsModified
                    ? FeedbackDecision.Modified
                    : FeedbackDecision.Accepted,
                OriginalText = notification.Deviation.OriginalText,
                SuggestedText = notification.Suggestion.SuggestedText,
                FinalText = notification.AppliedText,
                UserModification = notification.IsModified ? notification.ModifiedText : null,
                OriginalConfidence = notification.Suggestion.Confidence,
                Timestamp = notification.Timestamp
            };

            // LOGIC: Apply privacy and store — reuse internal logic but bypass license check
            var privacyOptions = GetPrivacyOptions();
            var processedFeedback = ApplyPrivacyOptions(feedback, privacyOptions);
            var record = MapToRecord(processedFeedback);
            await _store.StoreFeedbackAsync(record, ct);

            // LOGIC: Update pattern cache asynchronously
            try
            {
                await _store.UpdatePatternCacheAsync(feedback.RuleId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to update pattern cache for rule {RuleId}",
                    feedback.RuleId);
            }

            _logger.LogDebug(
                "Accept feedback recorded via MediatR: {Decision} for rule {RuleId}",
                feedback.Decision, feedback.RuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record accept feedback via MediatR");
        }
    }

    /// <summary>
    /// Handles <see cref="SuggestionRejectedEvent"/> by recording reject feedback.
    /// </summary>
    /// <param name="notification">The rejected event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Silently skips recording when Teams license is not available.
    /// </remarks>
    public async Task Handle(SuggestionRejectedEvent notification, CancellationToken ct)
    {
        // LOGIC: Silently skip if no Teams license
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogDebug(
                "Learning Loop requires Teams license, skipping reject feedback recording");
            return;
        }

        try
        {
            var feedback = new FixFeedback
            {
                FeedbackId = Guid.NewGuid(),
                SuggestionId = notification.Suggestion.SuggestionId,
                DeviationId = notification.Deviation.DeviationId,
                RuleId = notification.Deviation.RuleId,
                Category = notification.Deviation.Category,
                Decision = FeedbackDecision.Rejected,
                OriginalText = notification.Deviation.OriginalText,
                SuggestedText = notification.Suggestion.SuggestedText,
                OriginalConfidence = notification.Suggestion.Confidence,
                Timestamp = notification.Timestamp
            };

            var privacyOptions = GetPrivacyOptions();
            var processedFeedback = ApplyPrivacyOptions(feedback, privacyOptions);
            var record = MapToRecord(processedFeedback);
            await _store.StoreFeedbackAsync(record, ct);

            // LOGIC: Update pattern cache asynchronously
            try
            {
                await _store.UpdatePatternCacheAsync(feedback.RuleId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to update pattern cache for rule {RuleId}",
                    feedback.RuleId);
            }

            _logger.LogDebug(
                "Reject feedback recorded via MediatR for rule {RuleId}",
                feedback.RuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record reject feedback via MediatR");
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Ensures the current license tier is Teams or higher.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Teams license is not available.</exception>
    private void EnsureTeamsLicense()
    {
        if (_licenseContext.GetCurrentTier() < LicenseTier.Teams)
        {
            _logger.LogWarning(
                "Learning Loop requires Teams license. Current tier: {Tier}",
                _licenseContext.GetCurrentTier());
            throw new InvalidOperationException(
                "Learning Loop requires a Teams license. " +
                $"Current tier: {_licenseContext.GetCurrentTier()}.");
        }
    }

    /// <summary>
    /// Applies privacy options to feedback before storage.
    /// </summary>
    private static FixFeedback ApplyPrivacyOptions(
        FixFeedback feedback,
        LearningPrivacyOptions options)
    {
        var result = feedback;

        // LOGIC: Anonymize user ID via SHA256
        if (options.AnonymizeUsers && !string.IsNullOrEmpty(feedback.AnonymizedUserId))
        {
            result = result with
            {
                AnonymizedUserId = HashUserId(feedback.AnonymizedUserId)
            };
        }

        // LOGIC: Strip original text to structural patterns if not allowed
        if (!options.StoreOriginalText)
        {
            result = result with
            {
                OriginalText = ExtractPatternOnly(feedback.OriginalText),
                SuggestedText = ExtractPatternOnly(feedback.SuggestedText),
                FinalText = feedback.FinalText is not null
                    ? ExtractPatternOnly(feedback.FinalText)
                    : null,
                UserModification = feedback.UserModification is not null
                    ? ExtractPatternOnly(feedback.UserModification)
                    : null
            };
        }

        return result;
    }

    /// <summary>
    /// Hashes a user ID using SHA256 for anonymization.
    /// </summary>
    private static string HashUserId(string userId)
    {
        // LOGIC: Use SHA256.HashData() for thread-safe, allocation-efficient hashing
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        return Convert.ToBase64String(hash)[..12];
    }

    /// <summary>
    /// Extracts structural pattern from text, replacing specific words with placeholders.
    /// </summary>
    private static string ExtractPatternOnly(string text)
    {
        // LOGIC: Replace words of 5+ characters with [WORD] placeholder.
        // This preserves grammatical structure while removing specific content.
        var pattern = Regex.Replace(text, @"\b\w{5,}\b", "[WORD]");
        return pattern.Length <= 100 ? pattern : pattern[..100];
    }

    /// <summary>
    /// Maps a FixFeedback to a FeedbackRecord for storage.
    /// </summary>
    private static FeedbackRecord MapToRecord(FixFeedback feedback)
    {
        return new FeedbackRecord
        {
            Id = feedback.FeedbackId.ToString(),
            SuggestionId = feedback.SuggestionId.ToString(),
            DeviationId = feedback.DeviationId.ToString(),
            RuleId = feedback.RuleId,
            Category = feedback.Category,
            Decision = (int)feedback.Decision,
            OriginalText = feedback.OriginalText,
            SuggestedText = feedback.SuggestedText,
            FinalText = feedback.FinalText,
            UserModification = feedback.UserModification,
            OriginalConfidence = feedback.OriginalConfidence,
            Timestamp = feedback.Timestamp.ToString("O"),
            UserComment = feedback.UserComment,
            AnonymizedUserId = feedback.AnonymizedUserId,
            IsBulkOperation = feedback.IsBulkOperation ? 1 : 0
        };
    }

    /// <summary>
    /// Enforces data retention policies.
    /// </summary>
    private async Task EnforceRetentionPolicyAsync(
        LearningPrivacyOptions options,
        CancellationToken ct)
    {
        try
        {
            // LOGIC: Delete data older than the configured max age
            if (options.MaxDataAge.HasValue && options.MaxDataAge.Value.TotalDays > 0)
            {
                var cutoff = DateTime.UtcNow - options.MaxDataAge.Value;
                await _store.DeleteOlderThanAsync(cutoff, ct);
            }

            // LOGIC: Enforce maximum record count
            if (options.MaxRecords > 0)
            {
                var count = await _store.GetFeedbackCountAsync(null, ct);
                if (count > options.MaxRecords)
                {
                    var toDelete = count - options.MaxRecords;
                    await _store.DeleteOldestAsync(toDelete, ct);
                }
            }
        }
        catch (Exception ex)
        {
            // LOGIC: Retention enforcement failure is non-critical
            _logger.LogWarning(ex, "Failed to enforce retention policy");
        }
    }

    #endregion
}
