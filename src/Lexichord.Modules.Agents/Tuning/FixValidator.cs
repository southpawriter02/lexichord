// -----------------------------------------------------------------------
// <copyright file="FixValidator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Validates fix suggestions by re-linting the fixed text and computing
/// semantic similarity.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This internal helper class validates that a suggested fix:
/// <list type="bullet">
///   <item><description>Resolves the original style violation</description></item>
///   <item><description>Does not introduce new violations</description></item>
///   <item><description>Preserves the semantic meaning of the original text</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Validation Process:</b>
/// <list type="number">
///   <item><description>Apply the suggested fix to the document context</description></item>
///   <item><description>Re-run style analysis on the fixed text</description></item>
///   <item><description>Check if the original violation is resolved</description></item>
///   <item><description>Check for new violations in the fix area</description></item>
///   <item><description>Compute semantic similarity using word overlap</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public sealed class FixValidator
{
    private readonly IStyleEngine _styleEngine;
    private readonly ILogger<FixValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixValidator"/> class.
    /// </summary>
    /// <param name="styleEngine">The style engine for re-analysis.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public FixValidator(
        IStyleEngine styleEngine,
        ILogger<FixValidator> logger)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that a fix suggestion correctly addresses the deviation
    /// without introducing new violations.
    /// </summary>
    /// <param name="deviation">The original deviation.</param>
    /// <param name="suggestion">The suggested fix to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="FixValidationResult"/> with detailed validation information.</returns>
    public async Task<FixValidationResult> ValidateAsync(
        StyleDeviation deviation,
        FixSuggestion suggestion,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deviation);
        ArgumentNullException.ThrowIfNull(suggestion);

        _logger.LogDebug(
            "Validating fix for deviation {DeviationId}, RuleId={RuleId}",
            deviation.DeviationId,
            deviation.RuleId);

        try
        {
            // LOGIC: Apply the suggested fix to the surrounding context
            var fixedText = ApplyFix(
                deviation.SurroundingContext,
                deviation.Location,
                suggestion.SuggestedText,
                deviation.OriginalText);

            // LOGIC: Re-run style analysis on the fixed text
            var newViolations = await _styleEngine.AnalyzeAsync(fixedText, ct);

            // LOGIC: Check if the original violation is still present
            var originalStillPresent = newViolations.Any(v =>
                v.Rule.Id == deviation.RuleId &&
                TextOverlaps(
                    v.StartOffset,
                    v.EndOffset,
                    deviation.Location.Start,
                    deviation.Location.End));

            if (originalStillPresent)
            {
                _logger.LogDebug("Validation failed: original violation still present");
                return FixValidationResult.Invalid("Fix does not resolve the original violation");
            }

            // LOGIC: Check for new violations in the fix area
            var newViolationsInFixArea = newViolations
                .Where(v => TextOverlaps(
                    v.StartOffset,
                    v.EndOffset,
                    deviation.Location.Start,
                    deviation.Location.End + (suggestion.SuggestedText.Length - deviation.OriginalText.Length)))
                .Where(v => v.Rule.Id != deviation.RuleId)
                .ToList();

            // LOGIC: Calculate semantic similarity using word overlap
            var semanticSimilarity = CalculateSemanticSimilarity(
                deviation.OriginalText,
                suggestion.SuggestedText);

            _logger.LogDebug(
                "Validation: resolves={Resolves}, newViolations={NewCount}, similarity={Similarity:F2}",
                true,
                newViolationsInFixArea.Count,
                semanticSimilarity);

            // LOGIC: Determine final validation status
            if (newViolationsInFixArea.Count > 0)
            {
                return new FixValidationResult
                {
                    ResolvesViolation = true,
                    IntroducesNewViolations = true,
                    NewViolations = newViolationsInFixArea,
                    SemanticSimilarity = semanticSimilarity,
                    Status = ValidationStatus.Invalid,
                    Message = $"Fix introduces {newViolationsInFixArea.Count} new violation(s)"
                };
            }

            if (semanticSimilarity < 0.7)
            {
                return new FixValidationResult
                {
                    ResolvesViolation = true,
                    IntroducesNewViolations = false,
                    SemanticSimilarity = semanticSimilarity,
                    Status = ValidationStatus.ValidWithWarnings,
                    Message = "Fix may significantly alter the original meaning"
                };
            }

            return FixValidationResult.Valid(semanticSimilarity);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed with error");
            return FixValidationResult.Failed($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies a fix to the surrounding context by replacing the original text.
    /// </summary>
    /// <param name="context">The surrounding context containing the violation.</param>
    /// <param name="location">The location of the violation within the full document.</param>
    /// <param name="suggestedText">The replacement text.</param>
    /// <param name="originalText">The original violation text.</param>
    /// <returns>The context with the fix applied.</returns>
    private static string ApplyFix(
        string context,
        TextSpan location,
        string suggestedText,
        string originalText)
    {
        // LOGIC: Find the original text within the context and replace it
        var originalIndex = context.IndexOf(originalText, StringComparison.Ordinal);
        if (originalIndex < 0)
        {
            // LOGIC: Fallback - return context as-is if original text not found
            return context;
        }

        return string.Concat(
            context.AsSpan(0, originalIndex),
            suggestedText,
            context.AsSpan(originalIndex + originalText.Length));
    }

    /// <summary>
    /// Checks if two text spans overlap.
    /// </summary>
    /// <param name="start1">Start of first span.</param>
    /// <param name="end1">End of first span.</param>
    /// <param name="start2">Start of second span.</param>
    /// <param name="end2">End of second span.</param>
    /// <returns>True if spans overlap, false otherwise.</returns>
    private static bool TextOverlaps(int start1, int end1, int start2, int end2) =>
        start1 < end2 && start2 < end1;

    /// <summary>
    /// Calculates semantic similarity using word overlap (Jaccard similarity).
    /// </summary>
    /// <param name="original">The original text.</param>
    /// <param name="suggested">The suggested text.</param>
    /// <returns>A similarity score between 0.0 and 1.0.</returns>
    private static double CalculateSemanticSimilarity(string original, string suggested)
    {
        // LOGIC: Use word-level Jaccard similarity as a simple semantic measure
        var originalWords = TokenizeWords(original);
        var suggestedWords = TokenizeWords(suggested);

        if (originalWords.Count == 0 && suggestedWords.Count == 0)
        {
            return 1.0; // Both empty = identical
        }

        if (originalWords.Count == 0 || suggestedWords.Count == 0)
        {
            return 0.0; // One empty, one not = completely different
        }

        var intersection = originalWords.Intersect(suggestedWords, StringComparer.OrdinalIgnoreCase).Count();
        var union = originalWords.Union(suggestedWords, StringComparer.OrdinalIgnoreCase).Count();

        var jaccardSimilarity = (double)intersection / union;

        // LOGIC: Also consider length ratio - significant length changes indicate semantic change
        var lengthRatio = (double)Math.Min(original.Length, suggested.Length) /
                          Math.Max(original.Length, suggested.Length);

        // LOGIC: Combine Jaccard and length ratio with weights
        return (jaccardSimilarity * 0.7) + (lengthRatio * 0.3);
    }

    /// <summary>
    /// Tokenizes text into words, removing punctuation.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>A set of unique words.</returns>
    private static HashSet<string> TokenizeWords(string text)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var currentWord = new System.Text.StringBuilder();
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                currentWord.Append(c);
            }
            else if (currentWord.Length > 0)
            {
                words.Add(currentWord.ToString());
                currentWord.Clear();
            }
        }

        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words;
    }
}
