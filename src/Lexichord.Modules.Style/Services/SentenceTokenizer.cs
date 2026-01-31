// <copyright file="SentenceTokenizer.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Implementation of <see cref="ISentenceTokenizer"/> that splits text into sentences
/// while respecting common English abbreviations.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3a - Uses a dictionary of 50+ abbreviations to avoid false sentence breaks.
/// Thread-safe: Uses compiled regex and produces immutable results.
/// 
/// Key edge cases handled:
/// - Title abbreviations: Mr., Mrs., Dr., Prof., etc.
/// - Business abbreviations: Inc., Corp., Ltd., etc.
/// - Geographic abbreviations: St., Ave., Blvd., etc.
/// - Latin abbreviations: etc., e.g., i.e., vs., etc.
/// - Period-embedded: U.S.A., Ph.D., a.m., p.m., etc.
/// - Ellipsis: ... is not a sentence break
/// - Initials: J.F.K., not treated as sentence breaks
/// </remarks>
public sealed partial class SentenceTokenizer : ISentenceTokenizer
{
    private readonly ILogger<SentenceTokenizer> _logger;

    /// <summary>
    /// Standard abbreviations that should not trigger sentence breaks.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.3a - These abbreviations end with a period but do not end sentences.
    /// Case-insensitive matching via StringComparer.OrdinalIgnoreCase.
    /// </remarks>
    private static readonly HashSet<string> StandardAbbreviations = new(
        StringComparer.OrdinalIgnoreCase)
    {
        // Titles (personal)
        "Mr", "Mrs", "Ms", "Miss", "Dr", "Prof", "Rev", "Hon",
        "Capt", "Lt", "Col", "Gen", "Sgt", "Cpl", "Pvt", "Adm",
        
        // Suffixes
        "Jr", "Sr", "Esq",
        
        // Business
        "Inc", "Corp", "Ltd", "Co", "LLC", "Bros", "Assoc",
        
        // Geographic / Address
        "St", "Ave", "Blvd", "Rd", "Dr", "Ln", "Ct", "Pl",
        "Mt", "Ft", "Pt",
        
        // Directions
        "N", "S", "E", "W", "NE", "NW", "SE", "SW",
        
        // Latin
        "etc", "vs", "viz", "cf", "al",
        
        // Academic / Professional
        "Dept", "Div", "Gov", "Gov't", "Govt", "Sen", "Rep",
        
        // Measurements
        "oz", "lb", "lbs", "ft", "in", "yd", "mi", "kg", "km", "cm", "mm",
        
        // Miscellaneous
        "No", "Vol", "Fig", "Ch", "Sec", "Art",
        "approx", "est", "min", "max", "avg",
        "Jan", "Feb", "Mar", "Apr", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    };

    /// <summary>
    /// Abbreviations with embedded periods (e.g., "U.S.A.").
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.3a - These contain periods within the abbreviation itself.
    /// Handled separately to avoid breaking on internal periods.
    /// </remarks>
    private static readonly HashSet<string> PeriodAbbreviations = new(
        StringComparer.OrdinalIgnoreCase)
    {
        // Academic degrees
        "Ph.D", "M.D", "B.A", "M.A", "B.S", "M.S", "Ed.D", "J.D", "LL.B", "LL.M",
        
        // Time
        "a.m", "p.m", "A.M", "P.M",
        
        // Latin
        "e.g", "i.e", "et al",
        
        // Countries / Organizations
        "U.S", "U.K", "U.S.A", "U.S.S.R", "U.N"
    };

    /// <summary>
    /// Regex pattern for matching potential sentence-ending punctuation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches one or more of [.!?] followed by whitespace or end-of-string.
    /// Multiple punctuation (e.g., "!?" or "...") is captured together.
    /// </remarks>
    [GeneratedRegex(@"[.!?]+(?=\s|$)", RegexOptions.Compiled)]
    private static partial Regex SentenceEndPattern();

    /// <summary>
    /// Regex pattern for counting words.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches word characters including hyphens for compound words.
    /// Same pattern as DocumentTokenizer for consistency.
    /// </remarks>
    [GeneratedRegex(@"\b[\w-]+\b", RegexOptions.Compiled)]
    private static partial Regex WordPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="SentenceTokenizer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SentenceTokenizer(ILogger<SentenceTokenizer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<SentenceInfo> Tokenize(string text)
    {
        _logger.LogDebug("Starting sentence tokenization for text of length {Length}", text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Input is empty or whitespace, returning empty result");
            return Array.Empty<SentenceInfo>();
        }

        var sentences = new List<SentenceInfo>();
        var currentStart = 0;

        // LOGIC: Skip leading whitespace
        while (currentStart < text.Length && char.IsWhiteSpace(text[currentStart]))
        {
            currentStart++;
        }

        var matches = SentenceEndPattern().Matches(text);
        _logger.LogDebug("Found {MatchCount} potential sentence boundaries", matches.Count);

        foreach (Match match in matches)
        {
            var endPosition = match.Index + match.Length;

            // LOGIC: Check if this is a real sentence break (not an abbreviation)
            if (IsRealSentenceBreak(text, match.Index))
            {
                var sentenceText = text[currentStart..endPosition].Trim();

                if (!string.IsNullOrWhiteSpace(sentenceText))
                {
                    var wordCount = CountWords(sentenceText);

                    _logger.LogTrace(
                        "Confirmed sentence break at position {Position}: \"{Preview}\" ({WordCount} words)",
                        match.Index,
                        sentenceText.Length > 50 ? sentenceText[..50] + "..." : sentenceText,
                        wordCount);

                    sentences.Add(new SentenceInfo(
                        Text: sentenceText,
                        StartIndex: currentStart,
                        EndIndex: endPosition,
                        WordCount: wordCount));
                }

                currentStart = endPosition;

                // LOGIC: Skip whitespace after sentence for next sentence start
                while (currentStart < text.Length && char.IsWhiteSpace(text[currentStart]))
                {
                    currentStart++;
                }
            }
            else
            {
                _logger.LogTrace(
                    "Skipped abbreviation at position {Position} (not a sentence break)",
                    match.Index);
            }
        }

        // LOGIC: Handle remaining text (sentence without terminal punctuation)
        if (currentStart < text.Length)
        {
            var remaining = text[currentStart..].Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                var wordCount = CountWords(remaining);
                _logger.LogTrace(
                    "Adding final sentence without terminal punctuation: \"{Preview}\" ({WordCount} words)",
                    remaining.Length > 50 ? remaining[..50] + "..." : remaining,
                    wordCount);

                sentences.Add(new SentenceInfo(
                    Text: remaining,
                    StartIndex: currentStart,
                    EndIndex: text.Length,
                    WordCount: wordCount));
            }
        }

        _logger.LogDebug(
            "Tokenization complete: {SentenceCount} sentences found",
            sentences.Count);

        return sentences.AsReadOnly();
    }

    /// <summary>
    /// Determines if a period at the given position marks a real sentence break.
    /// </summary>
    /// <param name="text">The full text being tokenized.</param>
    /// <param name="periodIndex">Index of the period/punctuation in the text.</param>
    /// <returns>True if this is a sentence break; false if it's an abbreviation.</returns>
    /// <remarks>
    /// LOGIC: v0.3.3a - Implements the decision tree from the design specification:
    /// 1. Check for ellipsis (...)
    /// 2. Check for period-embedded abbreviations (U.S.A.)
    /// 3. Check for standard abbreviations (Mr., Dr.)
    /// 4. Check for initials (J.F.K.)
    /// 5. Check if followed by lowercase (continuation vs new sentence)
    /// </remarks>
    private bool IsRealSentenceBreak(string text, int periodIndex)
    {
        // LOGIC: Non-period punctuation (! ?) is always a sentence break
        if (text[periodIndex] != '.')
        {
            return true;
        }

        // LOGIC: Check for ellipsis (three or more periods)
        if (IsEllipsis(text, periodIndex))
        {
            _logger.LogTrace("Detected ellipsis at position {Position}", periodIndex);
            return false;
        }

        // LOGIC: Extract the word before the period
        var wordBeforePeriod = GetWordBeforePeriod(text, periodIndex);

        if (string.IsNullOrEmpty(wordBeforePeriod))
        {
            return true;
        }

        // LOGIC: Check period-embedded abbreviations first (e.g., "U.S.A")
        if (IsPeriodEmbeddedAbbreviation(wordBeforePeriod))
        {
            // LOGIC: Even period-embedded abbreviations can end sentences
            // if followed by uppercase letter (new sentence)
            return IsFollowedByUppercase(text, periodIndex);
        }

        // LOGIC: Check standard abbreviations
        if (StandardAbbreviations.Contains(wordBeforePeriod))
        {
            _logger.LogTrace(
                "Detected standard abbreviation \"{Abbreviation}\" at position {Position}",
                wordBeforePeriod,
                periodIndex);
            return false;
        }

        // LOGIC: Check for single-letter initials (e.g., J. in J.F.K.)
        if (wordBeforePeriod.Length == 1 && char.IsUpper(wordBeforePeriod[0]))
        {
            if (IsPartOfInitials(text, periodIndex))
            {
                _logger.LogTrace("Detected initials at position {Position}", periodIndex);
                return false;
            }
        }

        // LOGIC: Default case - check if followed by lowercase (continuation)
        var nextCharIndex = periodIndex + 1;
        while (nextCharIndex < text.Length && char.IsWhiteSpace(text[nextCharIndex]))
        {
            nextCharIndex++;
        }

        if (nextCharIndex < text.Length && char.IsLower(text[nextCharIndex]))
        {
            _logger.LogTrace(
                "Period at {Position} followed by lowercase, treating as continuation",
                periodIndex);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the period at the given position is part of an ellipsis.
    /// </summary>
    private static bool IsEllipsis(string text, int periodIndex)
    {
        // LOGIC: Check for at least two preceding periods
        var periodCount = 1;
        var checkIndex = periodIndex - 1;

        while (checkIndex >= 0 && text[checkIndex] == '.')
        {
            periodCount++;
            checkIndex--;
        }

        // LOGIC: Also check following periods
        checkIndex = periodIndex + 1;
        while (checkIndex < text.Length && text[checkIndex] == '.')
        {
            periodCount++;
            checkIndex++;
        }

        return periodCount >= 3;
    }

    /// <summary>
    /// Extracts the word immediately before the period.
    /// </summary>
    private static string GetWordBeforePeriod(string text, int periodIndex)
    {
        var wordEnd = periodIndex;
        var wordStart = periodIndex - 1;

        // LOGIC: Walk backwards to find the start of the word
        while (wordStart >= 0 && (char.IsLetterOrDigit(text[wordStart]) || text[wordStart] == '.'))
        {
            wordStart--;
        }

        wordStart++;

        if (wordStart >= wordEnd)
        {
            return string.Empty;
        }

        // LOGIC: Get the word, removing trailing period if captured
        var word = text[wordStart..wordEnd];
        return word.TrimEnd('.');
    }

    /// <summary>
    /// Checks if the word is a period-embedded abbreviation.
    /// </summary>
    private bool IsPeriodEmbeddedAbbreviation(string word)
    {
        // LOGIC: Check with and without trailing period
        if (PeriodAbbreviations.Contains(word))
        {
            _logger.LogTrace("Detected period-embedded abbreviation \"{Abbreviation}\"", word);
            return true;
        }

        // LOGIC: Also check the full form with period
        var withPeriod = word + ".";
        if (PeriodAbbreviations.Contains(withPeriod))
        {
            _logger.LogTrace("Detected period-embedded abbreviation \"{Abbreviation}\"", word);
            return true;
        }

        // LOGIC: Check if it looks like an abbreviation pattern (alternating letter-period)
        if (word.Contains('.') && IsAlternatingPattern(word))
        {
            _logger.LogTrace("Detected alternating letter-period pattern \"{Pattern}\"", word);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the string follows an alternating letter-period pattern (e.g., "U.S.A").
    /// </summary>
    private static bool IsAlternatingPattern(string word)
    {
        // LOGIC: Pattern like U.S.A or J.F.K
        var parts = word.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        // Each part should be a single letter or short
        return parts.All(p => p.Length == 1 && char.IsLetter(p[0]));
    }

    /// <summary>
    /// Checks if the period is followed by an uppercase letter (new sentence).
    /// </summary>
    private static bool IsFollowedByUppercase(string text, int periodIndex)
    {
        var nextCharIndex = periodIndex + 1;

        // LOGIC: Skip whitespace to find the next non-whitespace character
        while (nextCharIndex < text.Length && char.IsWhiteSpace(text[nextCharIndex]))
        {
            nextCharIndex++;
        }

        if (nextCharIndex >= text.Length)
        {
            return true; // End of text counts as sentence end
        }

        return char.IsUpper(text[nextCharIndex]);
    }

    /// <summary>
    /// Checks if a single letter before a period is part of a series of initials.
    /// </summary>
    private static bool IsPartOfInitials(string text, int periodIndex)
    {
        // LOGIC: Check if followed by another initial pattern (Letter.)
        var nextCharIndex = periodIndex + 1;

        // Skip optional whitespace
        while (nextCharIndex < text.Length && char.IsWhiteSpace(text[nextCharIndex]))
        {
            nextCharIndex++;
        }

        // LOGIC: Check for pattern: uppercase letter followed by period
        if (nextCharIndex < text.Length - 1 &&
            char.IsUpper(text[nextCharIndex]) &&
            text[nextCharIndex + 1] == '.')
        {
            return true;
        }

        // LOGIC: Also check preceding context for initials pattern
        var prevCharIndex = periodIndex - 2; // Skip past the letter before period
        if (prevCharIndex >= 0 && text[prevCharIndex] == '.')
        {
            var checkIndex = prevCharIndex - 1;
            if (checkIndex >= 0 && char.IsUpper(text[checkIndex]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts the number of words in the given text.
    /// </summary>
    /// <param name="text">The text to count words in.</param>
    /// <returns>The word count.</returns>
    private static int CountWords(string text)
    {
        return WordPattern().Matches(text).Count;
    }
}
