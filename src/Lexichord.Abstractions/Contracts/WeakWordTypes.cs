// <copyright file="WeakWordTypes.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Categorizes the type of weak word detected.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4c - Different weak word types have different impacts on writing quality
/// and may require different handling in the UI.
/// </remarks>
public enum WeakWordCategory
{
    /// <summary>
    /// Adverb or intensifier that may weaken writing (e.g., "very", "really", "extremely").
    /// </summary>
    /// <remarks>
    /// These words often add emphasis without substance and can usually be
    /// replaced with stronger verbs or more precise adjectives.
    /// </remarks>
    Adverb,

    /// <summary>
    /// Weasel word or hedge that reduces precision (e.g., "perhaps", "maybe", "somewhat").
    /// </summary>
    /// <remarks>
    /// These words hedge statements and reduce commitment, making writing
    /// less authoritative and more ambiguous.
    /// </remarks>
    WeaselWord,

    /// <summary>
    /// Filler word that adds no meaning (e.g., "basically", "essentially", "literally").
    /// </summary>
    /// <remarks>
    /// These words are always flagged regardless of profile settings
    /// because they typically add no value to the writing.
    /// </remarks>
    Filler
}

/// <summary>
/// Represents a detected weak word with position information.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4c - Matches include position data for editor squiggly rendering
/// and category information for severity classification.
/// </remarks>
/// <param name="Word">The detected weak word (normalized to lowercase).</param>
/// <param name="Category">The classification of the weak word type.</param>
/// <param name="StartIndex">Start position of the word in the original text.</param>
/// <param name="EndIndex">End position (exclusive) of the word in the original text.</param>
public record WeakWordMatch(
    string Word,
    WeakWordCategory Category,
    int StartIndex,
    int EndIndex)
{
    /// <summary>
    /// Gets the length of the matched weak word.
    /// </summary>
    public int Length => EndIndex - StartIndex;
}

/// <summary>
/// Statistics about weak words found in a text.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4c - Provides aggregate metrics for the Scorecard Widget
/// and detailed match lists for the Problems Panel.
/// </remarks>
/// <param name="TotalWords">Total number of words in the analyzed text.</param>
/// <param name="TotalWeakWords">Total number of weak words detected.</param>
/// <param name="CountByCategory">Breakdown of weak word counts by category.</param>
/// <param name="Matches">Ordered list of all detected weak word matches.</param>
public record WeakWordStats(
    int TotalWords,
    int TotalWeakWords,
    IReadOnlyDictionary<WeakWordCategory, int> CountByCategory,
    IReadOnlyList<WeakWordMatch> Matches)
{
    /// <summary>
    /// Gets the percentage of weak words in the text.
    /// </summary>
    /// <remarks>
    /// Returns 0.0 if <see cref="TotalWords"/> is zero to avoid division by zero.
    /// </remarks>
    public double WeakWordPercentage => TotalWords > 0
        ? (double)TotalWeakWords / TotalWords * 100.0
        : 0.0;

    /// <summary>
    /// Gets a value indicating whether the text contains any weak words.
    /// </summary>
    public bool HasWeakWords => TotalWeakWords > 0;
}

/// <summary>
/// Service for detecting weak words (adverbs, weasel words, fillers) in text.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4c - The scanner respects the active Voice Profile's constraints:</para>
/// <list type="bullet">
///   <item><c>FlagAdverbs = true</c>: Adverbs trigger Info-level squigglies</item>
///   <item><c>FlagWeaselWords = true</c>: Weasel words trigger Info-level squigglies</item>
///   <item>Filler words are ALWAYS flagged regardless of profile settings</item>
/// </list>
/// <para>Feature-gated to Writer Pro tier via <c>FeatureFlags.Style.VoiceProfiler</c>.</para>
/// </remarks>
public interface IWeakWordScanner
{
    /// <summary>
    /// Scans text for weak words based on the provided Voice Profile constraints.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <param name="profile">
    /// The Voice Profile containing constraint settings.
    /// Used to determine which categories to flag (<c>FlagAdverbs</c>, <c>FlagWeaselWords</c>).
    /// </param>
    /// <returns>
    /// An immutable list of weak word matches, sorted by position.
    /// Returns an empty list if text is empty or no weak words are found.
    /// </returns>
    /// <remarks>
    /// The scanner normalizes words to lowercase for matching against word lists.
    /// Position information is preserved from the original text for highlighting.
    /// </remarks>
    IReadOnlyList<WeakWordMatch> Scan(string text, VoiceProfile profile);

    /// <summary>
    /// Gets comprehensive statistics about weak words in the text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <param name="profile">
    /// The Voice Profile containing constraint settings.
    /// </param>
    /// <returns>
    /// Statistics including total words, weak word counts by category,
    /// percentage calculations, and the full match list.
    /// </returns>
    /// <remarks>
    /// This method is used by the Scorecard Widget to display aggregate metrics
    /// and by the Problems Panel to generate the issue list.
    /// </remarks>
    WeakWordStats GetStatistics(string text, VoiceProfile profile);

    /// <summary>
    /// Gets a suggested replacement or action for a specific weak word.
    /// </summary>
    /// <param name="word">The weak word to get a suggestion for (case-insensitive).</param>
    /// <param name="category">The category of the weak word.</param>
    /// <returns>
    /// A suggestion string, which may be:
    /// <list type="bullet">
    ///   <item>A specific replacement word (e.g., "very" → "Use a stronger adjective")</item>
    ///   <item>Generic advice based on category</item>
    ///   <item><c>"Consider removing"</c> for fillers</item>
    /// </list>
    /// </returns>
    string GetSuggestion(string word, WeakWordCategory category);
}
