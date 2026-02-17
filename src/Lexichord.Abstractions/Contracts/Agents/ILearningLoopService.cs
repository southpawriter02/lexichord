// -----------------------------------------------------------------------
// <copyright file="ILearningLoopService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Service for capturing and utilizing user feedback to improve fix suggestions.
/// Requires Teams license for full functionality.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The Learning Loop Service is the central orchestrator for feedback-driven
/// improvement of the Tuning Agent. It captures user accept/reject/modify decisions,
/// analyzes patterns, and generates prompt enhancements that guide future fix suggestions
/// toward user-preferred patterns.
/// </para>
/// <para>
/// <b>Data Flow:</b>
/// <list type="number">
///   <item><description>User makes a decision in the Tuning Panel → MediatR event published</description></item>
///   <item><description><c>LearningLoopService</c> handles event → creates <see cref="FixFeedback"/> → persists to SQLite</description></item>
///   <item><description><c>FixSuggestionGenerator</c> calls <see cref="GetLearningContextAsync"/> before generating fixes</description></item>
///   <item><description>Learning context's <see cref="LearningContext.PromptEnhancement"/> is injected into the prompt</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Requires Teams tier. Methods throw <c>InvalidOperationException</c>
/// when called without a Teams license. The Tuning Agent works without the Learning Loop —
/// fix suggestions are still generated, just without personalization.
/// </para>
/// <para>
/// <b>Privacy:</b> All feedback storage respects <see cref="LearningPrivacyOptions"/>.
/// By default, user IDs are anonymized and original text is reduced to structural patterns.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Recording feedback when a suggestion is accepted
/// var feedback = new FixFeedback
/// {
///     FeedbackId = Guid.NewGuid(),
///     SuggestionId = suggestion.SuggestionId,
///     DeviationId = deviation.DeviationId,
///     RuleId = deviation.RuleId,
///     Category = deviation.Category,
///     Decision = FeedbackDecision.Accepted,
///     OriginalText = deviation.OriginalText,
///     SuggestedText = suggestion.SuggestedText,
///     OriginalConfidence = suggestion.Confidence
/// };
/// await learningLoop.RecordFeedbackAsync(feedback);
///
/// // Getting learning context for prompt enhancement
/// var context = await learningLoop.GetLearningContextAsync("TERM-001");
/// if (context.HasSufficientData &amp;&amp; context.PromptEnhancement is not null)
/// {
///     promptContext["learning_enhancement"] = context.PromptEnhancement;
/// }
/// </code>
/// </example>
/// <seealso cref="FixFeedback"/>
/// <seealso cref="LearningContext"/>
/// <seealso cref="LearningStatistics"/>
/// <seealso cref="LearningPrivacyOptions"/>
public interface ILearningLoopService
{
    /// <summary>
    /// Records user feedback for a fix suggestion.
    /// </summary>
    /// <param name="feedback">The feedback record to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="feedback"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Teams license is not available.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Applies privacy options (anonymization, text pattern extraction)
    /// before persisting to SQLite. After storage, triggers pattern cache update
    /// for the affected rule.
    /// </remarks>
    Task RecordFeedbackAsync(FixFeedback feedback, CancellationToken ct = default);

    /// <summary>
    /// Gets learning context to enhance prompts for a specific rule.
    /// Returns patterns learned from past user decisions.
    /// </summary>
    /// <param name="ruleId">The style rule ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Learning context for prompt enhancement.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="ruleId"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Teams license is not available.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Retrieves feedback records for the rule, extracts patterns via
    /// <c>PatternAnalyzer</c>, generates prompt enhancement text, and returns a complete
    /// <see cref="LearningContext"/>. Returns <see cref="LearningContext.Empty"/> when
    /// no feedback exists for the rule.
    /// </remarks>
    Task<LearningContext> GetLearningContextAsync(string ruleId, CancellationToken ct = default);

    /// <summary>
    /// Gets aggregated learning statistics.
    /// </summary>
    /// <param name="filter">Optional filter for time period or rules.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Teams license is not available.</exception>
    Task<LearningStatistics> GetStatisticsAsync(
        LearningStatisticsFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports learning data for team sharing.
    /// Respects privacy settings.
    /// </summary>
    /// <param name="options">Export options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exportable learning data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Teams license is not available.</exception>
    Task<LearningExport> ExportLearningDataAsync(
        LearningExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Imports learning data from team export.
    /// Merges with existing data.
    /// </summary>
    /// <param name="data">The data to import.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Teams license is not available.</exception>
    Task ImportLearningDataAsync(LearningExport data, CancellationToken ct = default);

    /// <summary>
    /// Clears learning data with confirmation.
    /// </summary>
    /// <param name="options">Clear options including confirmation token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Teams license is not available or confirmation token is invalid.
    /// </exception>
    Task ClearLearningDataAsync(ClearLearningDataOptions options, CancellationToken ct = default);

    /// <summary>
    /// Gets current privacy settings for learning data.
    /// </summary>
    /// <returns>The current privacy options.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Reads privacy settings from <see cref="ISettingsService"/> under
    /// the "Learning:Privacy:" key prefix. Returns defaults when no settings are stored.
    /// </remarks>
    LearningPrivacyOptions GetPrivacyOptions();

    /// <summary>
    /// Updates privacy settings.
    /// </summary>
    /// <param name="options">The new privacy options to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Persists privacy settings to <see cref="ISettingsService"/>.
    /// If retention settings change, triggers retention enforcement asynchronously.
    /// </remarks>
    Task SetPrivacyOptionsAsync(LearningPrivacyOptions options, CancellationToken ct = default);
}
