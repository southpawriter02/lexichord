// <copyright file="ISyllableCounter.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Counts syllables in English words using heuristic rules and an exception dictionary.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3b - The syllable counter provides syllable analysis for readability metrics.</para>
/// <para>Algorithm overview:</para>
/// <list type="number">
///   <item>Check exception dictionary for known irregular words</item>
///   <item>Count vowel groups (consecutive vowels = 1 syllable)</item>
///   <item>Apply heuristic adjustments for silent 'e', suffixes, diphthongs</item>
///   <item>Ensure minimum of 1 syllable</item>
/// </list>
/// <para>Thread-safe: stateless service suitable for concurrent access.</para>
/// </remarks>
/// <example>
/// <code>
/// var counter = new SyllableCounter();
/// counter.CountSyllables("queue");         // Returns 1
/// counter.CountSyllables("documentation"); // Returns 5
/// counter.IsComplexWord("understanding");  // Returns true (4 syllables)
/// </code>
/// </example>
public interface ISyllableCounter
{
    /// <summary>
    /// Counts the number of syllables in a word.
    /// </summary>
    /// <param name="word">The word to analyze. MAY be null or empty.</param>
    /// <returns>
    /// The syllable count. SHALL return minimum 1 for any non-empty input.
    /// Returns 1 for null or empty input.
    /// </returns>
    int CountSyllables(string word);

    /// <summary>
    /// Determines if a word is "complex" for Gunning Fog Index calculation.
    /// </summary>
    /// <remarks>
    /// A complex word has 3+ syllables, excluding words that only reach 3 syllables
    /// due to common suffixes (-ing, -ed, -es, -ly).
    /// </remarks>
    /// <param name="word">The word to analyze.</param>
    /// <returns>True if the word is complex (3+ meaningful syllables); false otherwise.</returns>
    bool IsComplexWord(string word);
}
