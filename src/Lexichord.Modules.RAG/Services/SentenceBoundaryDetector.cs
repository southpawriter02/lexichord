// =============================================================================
// File: SentenceBoundaryDetector.cs
// Project: Lexichord.Modules.RAG
// Description: Detects sentence boundaries for intelligent snippet truncation.
// =============================================================================
// LOGIC: Implements abbreviation-aware sentence boundary detection.
//   - Handles common abbreviations (Dr., Mr., Ph.D., U.S.A., etc.)
//   - Skips decimal numbers (3.14)
//   - Supports multiple terminators (. ! ?)
//   - Falls back to word boundaries when needed
// =============================================================================
// VERSION: v0.5.6c (Smart Truncation)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Detects sentence boundaries in text for intelligent truncation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SentenceBoundaryDetector"/> provides abbreviation-aware sentence
/// boundary detection for the snippet extraction pipeline. It handles common
/// abbreviations and decimal numbers that contain periods.
/// </para>
/// <para>
/// <b>Abbreviation Handling:</b> A curated set of 50+ abbreviations including
/// titles (Dr., Mr.), academic degrees (Ph.D.), time (a.m.), and location
/// abbreviations (U.S., U.K.) are recognized and not treated as sentence ends.
/// </para>
/// <para>
/// <b>Decimal Numbers:</b> Periods preceded by digits and followed by digits
/// are not treated as sentence terminators.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe and stateless.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6c as part of Smart Truncation.
/// </para>
/// </remarks>
public sealed class SentenceBoundaryDetector : ISentenceBoundaryDetector
{
    private readonly ILogger<SentenceBoundaryDetector> _logger;

    /// <summary>
    /// Common abbreviations that should not be treated as sentence terminators.
    /// </summary>
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Titles
        "Mr", "Mrs", "Ms", "Dr", "Prof", "Sr", "Jr", "Rev", "Hon", "Gov", "Gen", "Col", "Lt", "Sgt", "Capt",
        // Academic
        "Ph.D", "M.D", "B.A", "M.A", "B.S", "M.S", "D.D.S", "J.D", "Ed.D",
        // Time
        "a.m", "p.m", "A.M", "P.M",
        // Measures
        "ft", "in", "cm", "mm", "kg", "lb", "oz", "qt", "pt", "gal",
        // Location
        "St", "Ave", "Blvd", "Rd", "Dr", "Ln", "Ct", "Pl",
        "U.S", "U.K", "U.S.A", "U.K.A", "E.U",
        // Common
        "vs", "etc", "e.g", "i.e", "et al", "cf", "viz", "approx", "est",
        "Inc", "Corp", "Ltd", "Co", "Assn", "Bros",
        "Jan", "Feb", "Mar", "Apr", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
        "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun",
        "No", "Vol", "Fig", "Eq", "Ch", "Sec", "Art"
    };

    /// <summary>
    /// Characters that can terminate a sentence.
    /// </summary>
    private static readonly char[] SentenceTerminators = { '.', '!', '?' };

    /// <summary>
    /// Initializes a new instance of <see cref="SentenceBoundaryDetector"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public SentenceBoundaryDetector(ILogger<SentenceBoundaryDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public int FindSentenceStart(string content, int position)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        position = Math.Clamp(position, 0, content.Length);

        // LOGIC: Search backward for sentence terminator.
        for (var i = position - 1; i >= 0; i--)
        {
            if (IsSentenceTerminator(content, i))
            {
                // LOGIC: Skip trailing punctuation clusters (e.g., "!!" or "?!")
                var sentenceEnd = i + 1;
                while (sentenceEnd < position && char.IsWhiteSpace(content[sentenceEnd]))
                {
                    sentenceEnd++;
                }

                _logger.LogDebug(
                    "Found sentence start at {Position} (terminator at {TerminatorPos})",
                    sentenceEnd, i);

                return sentenceEnd;
            }
        }

        // LOGIC: No terminator found, return start of content.
        return 0;
    }

    /// <inheritdoc />
    public int FindSentenceEnd(string content, int position)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        position = Math.Clamp(position, 0, content.Length);

        // LOGIC: Search forward for sentence terminator.
        for (var i = position; i < content.Length; i++)
        {
            if (IsSentenceTerminator(content, i))
            {
                // LOGIC: Include trailing punctuation clusters.
                var end = i + 1;
                while (end < content.Length && SentenceTerminators.Contains(content[end]))
                {
                    end++;
                }

                _logger.LogDebug(
                    "Found sentence end at {Position} (terminator at {TerminatorPos})",
                    end, i);

                return end;
            }
        }

        // LOGIC: No terminator found, return end of content.
        return content.Length;
    }

    /// <inheritdoc />
    public IReadOnlyList<SentenceBoundary> GetBoundaries(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<SentenceBoundary>();
        }

        var boundaries = new List<SentenceBoundary>();
        var currentStart = SkipWhitespace(text, 0);

        while (currentStart < text.Length)
        {
            var end = FindSentenceEnd(text, currentStart);

            // LOGIC: Skip trailing whitespace for the end, then find next start.
            boundaries.Add(new SentenceBoundary(currentStart, end));

            // LOGIC: Move to next sentence start.
            currentStart = SkipWhitespace(text, end);
        }

        // LOGIC: If no terminators were found, treat entire text as one sentence.
        if (boundaries.Count == 0 && !string.IsNullOrWhiteSpace(text))
        {
            boundaries.Add(new SentenceBoundary(0, text.Length));
        }

        _logger.LogDebug("Detected {Count} sentence boundaries", boundaries.Count);

        return boundaries;
    }

    /// <inheritdoc />
    public int FindWordStart(string text, int position)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        position = Math.Clamp(position, 0, text.Length);

        // LOGIC: If we're at or past end, back up to last char.
        if (position >= text.Length)
        {
            position = text.Length - 1;
        }

        // LOGIC: If we're in whitespace, back up to find a word.
        while (position > 0 && char.IsWhiteSpace(text[position]))
        {
            position--;
        }

        // LOGIC: Search backward for word boundary.
        while (position > 0 && !char.IsWhiteSpace(text[position - 1]))
        {
            position--;
        }

        return position;
    }

    /// <inheritdoc />
    public int FindWordEnd(string text, int position)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        position = Math.Clamp(position, 0, text.Length);

        // LOGIC: Skip leading whitespace.
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        // LOGIC: Search forward for word boundary.
        while (position < text.Length && !char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    #region Private Methods

    /// <summary>
    /// Checks if a character at the given position is a true sentence terminator.
    /// </summary>
    private bool IsSentenceTerminator(string text, int position)
    {
        if (position < 0 || position >= text.Length)
        {
            return false;
        }

        var c = text[position];

        // LOGIC: ! and ? are always sentence terminators.
        if (c == '!' || c == '?')
        {
            return true;
        }

        // LOGIC: Period requires additional checks.
        if (c != '.')
        {
            return false;
        }

        // LOGIC: Check for decimal number (digit.digit).
        if (IsDecimalNumber(text, position))
        {
            return false;
        }

        // LOGIC: Check for abbreviation.
        if (IsAbbreviation(text, position))
        {
            return false;
        }

        // LOGIC: Check for lowercase continuation (mid-sentence period unlikely).
        if (position + 1 < text.Length)
        {
            var nextNonSpace = SkipWhitespace(text, position + 1);
            if (nextNonSpace < text.Length && char.IsLower(text[nextNonSpace]))
            {
                // LOGIC: Lowercase after period suggests abbreviation or typo.
                // Trust the explicit abbreviation list instead.
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the period at the given position is part of a decimal number.
    /// </summary>
    private static bool IsDecimalNumber(string text, int position)
    {
        // LOGIC: Check for digit before and after the period.
        if (position == 0 || position >= text.Length - 1)
        {
            return false;
        }

        return char.IsDigit(text[position - 1]) && char.IsDigit(text[position + 1]);
    }

    /// <summary>
    /// Checks if the period at the given position follows a known abbreviation.
    /// </summary>
    private static bool IsAbbreviation(string text, int position)
    {
        // LOGIC: Extract word before the period, including any internal periods.
        var wordStart = position - 1;
        while (wordStart >= 0 && (char.IsLetter(text[wordStart]) || text[wordStart] == '.'))
        {
            wordStart--;
        }
        wordStart++;

        if (wordStart >= position)
        {
            return false;
        }

        var word = text[wordStart..position];

        // LOGIC: Check against known abbreviations (direct match).
        if (Abbreviations.Contains(word))
        {
            return true;
        }

        // LOGIC: For multi-period abbreviations like U.S.A, the word might be
        // "U.S.A" or "e.g" - check if adding the period makes it match.
        // This handles cases where the full abbreviation with period is in the set.
        var wordWithPeriod = word + ".";
        if (Abbreviations.Contains(wordWithPeriod))
        {
            return true;
        }

        // LOGIC: Also check without leading periods for cases like ".e.g" extracted
        // when another abbreviation precedes it.
        var trimmed = word.TrimStart('.');
        if (Abbreviations.Contains(trimmed))
        {
            return true;
        }

        // LOGIC: For abbreviations that end with periods internally (U.S.A.)
        // check if the period is part of a known pattern.
        // Pattern: letter-period repeated (U.S., U.S.A., etc.)
        if (IsInitialismPattern(text, wordStart, position))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the text from wordStart to position follows an initialism pattern.
    /// </summary>
    /// <remarks>
    /// Initialisms are uppercase letters separated by periods: U.S., U.K., U.S.A.
    /// This method also looks AHEAD to see if the initialism continues.
    /// </remarks>
    private static bool IsInitialismPattern(string text, int wordStart, int periodPosition)
    {
        // LOGIC: For "U.S.A." scenario:
        // - At first period (after U), look ahead to see if pattern continues
        // - Pattern continuation: period followed by uppercase letter or more periods

        // LOGIC: Look ahead - if next char after period is a letter (upper or lower),
        // and the pattern looks like single-letter.single-letter, this is likely
        // an abbreviation like U.S.A. or e.g. or i.e.
        if (periodPosition + 1 < text.Length)
        {
            var nextChar = text[periodPosition + 1];

            // LOGIC: Check for uppercase initialism continuation (U.S.A.)
            if (char.IsUpper(nextChar))
            {
                return true;
            }

            // LOGIC: Check for lowercase Latin abbreviation pattern (e.g., i.e.)
            // Pattern: single lowercase letter + period + single lowercase letter
            if (char.IsLower(nextChar) && periodPosition >= 1)
            {
                var prevChar = text[periodPosition - 1];
                if (char.IsLower(prevChar))
                {
                    // Check if it's a single letter before the period (not a full word)
                    var isLetterBeforePrev = periodPosition >= 2 && char.IsLetter(text[periodPosition - 2]);
                    if (!isLetterBeforePrev)
                    {
                        return true; // Single letter followed by period and letter = e.g., i.e.
                    }
                }
            }
        }

        // LOGIC: For the final period in an initialism, check if we have the
        // classic [A-Z].[A-Z].(...) pattern before this position.
        if (periodPosition - wordStart < 2)
        {
            return false; // Too short
        }

        var hasLetter = false;
        var hasPeriod = false;

        for (var i = wordStart; i < periodPosition; i++)
        {
            if (char.IsUpper(text[i]))
            {
                hasLetter = true;
            }
            else if (text[i] == '.')
            {
                hasPeriod = true;
            }
            else if (!char.IsLetter(text[i]))
            {
                return false; // Invalid character
            }
        }

        // LOGIC: Valid initialism has both letters and internal periods.
        return hasLetter && hasPeriod;
    }

    /// <summary>
    /// Skips whitespace starting from the given position.
    /// </summary>
    private static int SkipWhitespace(string text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    #endregion
}
