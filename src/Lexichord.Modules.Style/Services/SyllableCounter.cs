// <copyright file="SyllableCounter.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Counts syllables in English words using heuristic rules and an exception dictionary.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3b - Implementation using vowel-group counting with heuristic adjustments.</para>
/// <para>The algorithm:</para>
/// <list type="number">
///   <item>Checks exception dictionary for known irregular words (40+ entries)</item>
///   <item>Counts vowel groups (aeiou + y)</item>
///   <item>Applies silent 'e' rule (unless "-le" ending)</item>
///   <item>Applies "-ed" suffix rule (silent after consonants except d/t)</item>
///   <item>Applies "-es" suffix rule (silent after most consonants)</item>
///   <item>Ensures minimum of 1 syllable</item>
/// </list>
/// <para>Thread-safe: stateless with static readonly data structures.</para>
/// </remarks>
public sealed class SyllableCounter : ISyllableCounter
{
    /// <summary>
    /// Set of vowel characters including 'y' which functions as a vowel in many words.
    /// </summary>
    private static readonly HashSet<char> Vowels = ['a', 'e', 'i', 'o', 'u', 'y'];

    /// <summary>
    /// Exception dictionary for words that violate heuristic rules.
    /// Keys are lowercase; lookup is case-insensitive.
    /// </summary>
    private static readonly Dictionary<string, int> ExceptionDictionary = new(
        StringComparer.OrdinalIgnoreCase)
    {
        // 1 syllable exceptions (often miscounted as 2+)
        ["queue"] = 1,
        ["fire"] = 1,
        ["hour"] = 1,
        ["heir"] = 1,
        ["aisle"] = 1,
        ["isle"] = 1,
        ["tired"] = 1,
        ["wired"] = 1,
        ["our"] = 1,
        ["iron"] = 1,

        // 2 syllable exceptions
        ["being"] = 2,
        ["lion"] = 2,
        ["quiet"] = 2,
        ["science"] = 2,
        ["diet"] = 2,
        ["fuel"] = 2,
        ["jewel"] = 2,
        ["poem"] = 2,
        ["poet"] = 2,
        ["chaos"] = 2,
        ["create"] = 2,
        ["naive"] = 2,
        ["idea"] = 3,
        ["real"] = 1,
        ["deal"] = 1,

        // 3 syllable exceptions
        ["area"] = 3,
        ["beautiful"] = 3,
        ["animal"] = 3,
        ["library"] = 3,
        ["several"] = 3,
        ["camera"] = 3,
        ["different"] = 3,
        ["chocolate"] = 3,
        ["average"] = 3,
        ["general"] = 3,
        ["interest"] = 3,
        ["favorite"] = 3,
        ["family"] = 3,
        ["evening"] = 3,

        // 4 syllable exceptions
        ["dictionary"] = 4,
        ["category"] = 4,
        ["territory"] = 4,
        ["necessary"] = 4,
        ["experience"] = 4,
        ["understanding"] = 4,

        // 5 syllable exceptions
        ["vocabulary"] = 5,
        ["documentation"] = 5,
        ["abbreviation"] = 5,
        ["organization"] = 5,
        ["administration"] = 5
    };

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: v0.3.3b - Algorithm: exception lookup → vowel groups → heuristic adjustments → minimum 1.
    /// </remarks>
    public int CountSyllables(string word)
    {
        // LOGIC: Return minimum 1 for null/empty/whitespace
        if (string.IsNullOrWhiteSpace(word))
        {
            return 1;
        }

        word = word.ToLowerInvariant().Trim();

        // LOGIC: Check exception dictionary first for known irregular words
        if (ExceptionDictionary.TryGetValue(word, out var cached))
        {
            return cached;
        }

        // LOGIC: Count vowel groups as base syllable count
        var syllables = CountVowelGroups(word);

        // LOGIC: Apply heuristic adjustments for English pronunciation patterns
        syllables = ApplySilentERule(word, syllables);
        syllables = ApplyEdSuffixRule(word, syllables);
        syllables = ApplyEsSuffixRule(word, syllables);

        // LOGIC: Ensure minimum of 1 syllable for any word
        return Math.Max(1, syllables);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: v0.3.3b - Complex words have 3+ syllables, but words reaching 3 only due to
    /// common suffixes (-ing, -ed, -ly) are excluded from the count.
    /// </remarks>
    public bool IsComplexWord(string word)
    {
        // LOGIC: Null/empty words are not complex
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        var syllables = CountSyllables(word);

        // LOGIC: Words with fewer than 3 syllables are never complex
        if (syllables < 3)
        {
            return false;
        }

        word = word.ToLowerInvariant();

        // LOGIC: Check for "-ing" suffix inflation
        // e.g., "running" (2 syllables) should not be flagged as complex
        if (word.EndsWith("ing") && word.Length > 4)
        {
            var root = word[..^3];

            // LOGIC: Add back 'e' if likely removed (e.g., "making" → "make")
            if (!root.EndsWith('e') && CountSyllables(root) < 2)
            {
                root += 'e';
            }

            if (CountSyllables(root) < 3)
            {
                return false;
            }
        }

        // LOGIC: Check for "-ed" suffix inflation
        if (word.EndsWith("ed") && word.Length > 3)
        {
            var root = word[..^2];
            if (CountSyllables(root) < 3)
            {
                return false;
            }
        }

        // LOGIC: Check for "-ly" suffix inflation
        if (word.EndsWith("ly") && word.Length > 3)
        {
            var root = word[..^2];
            if (CountSyllables(root) < 3)
            {
                return false;
            }
        }

        return syllables >= 3;
    }

    /// <summary>
    /// Counts vowel groups in a word. Each vowel group represents one syllable.
    /// </summary>
    /// <param name="word">Lowercase word to analyze.</param>
    /// <returns>Number of vowel groups.</returns>
    private static int CountVowelGroups(string word)
    {
        var count = 0;
        var prevWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = Vowels.Contains(c);

            // LOGIC: New vowel group starts when we encounter a vowel after a consonant
            if (isVowel && !prevWasVowel)
            {
                count++;
            }

            prevWasVowel = isVowel;
        }

        return count;
    }

    /// <summary>
    /// Applies the silent 'e' rule for words ending in 'e'.
    /// </summary>
    /// <param name="word">Lowercase word to analyze.</param>
    /// <param name="syllables">Current syllable count.</param>
    /// <returns>Adjusted syllable count.</returns>
    private static int ApplySilentERule(string word, int syllables)
    {
        // LOGIC: Silent 'e' at end reduces count, unless "-le" which is pronounced
        if (word.EndsWith('e') && !word.EndsWith("le") && syllables > 1)
        {
            syllables--;
        }

        return syllables;
    }

    /// <summary>
    /// Applies the '-ed' suffix rule.
    /// </summary>
    /// <param name="word">Lowercase word to analyze.</param>
    /// <param name="syllables">Current syllable count.</param>
    /// <returns>Adjusted syllable count.</returns>
    private static int ApplyEdSuffixRule(string word, int syllables)
    {
        if (word.EndsWith("ed") && word.Length > 2)
        {
            var beforeEd = word[^3];

            // LOGIC: "-ed" adds syllable only after 'd' or 't' (e.g., "wanted", "loaded")
            // Otherwise it's silent (e.g., "jumped", "played")
            if (beforeEd != 'd' && beforeEd != 't' && syllables > 1)
            {
                syllables--;
            }
        }

        return syllables;
    }

    /// <summary>
    /// Applies the '-es' suffix rule.
    /// </summary>
    /// <param name="word">Lowercase word to analyze.</param>
    /// <param name="syllables">Current syllable count.</param>
    /// <returns>Adjusted syllable count.</returns>
    private static int ApplyEsSuffixRule(string word, int syllables)
    {
        if (word.EndsWith("es") && word.Length > 2)
        {
            var beforeEs = word[^3];

            // LOGIC: "-es" adds syllable after vowels, s, x, z, ch, sh
            // Otherwise it's silent (e.g., "makes", "takes")
            if (!Vowels.Contains(beforeEs) &&
                beforeEs != 's' && beforeEs != 'x' && beforeEs != 'z' &&
                !word.EndsWith("ches") && !word.EndsWith("shes") &&
                syllables > 1)
            {
                syllables--;
            }
        }

        return syllables;
    }
}
