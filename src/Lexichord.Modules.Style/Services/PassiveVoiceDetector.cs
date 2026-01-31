// <copyright file="PassiveVoiceDetector.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Detects passive voice constructions in text using regex-based pattern matching
/// with confidence scoring for adjective disambiguation.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4b - Uses compiled regex patterns to identify four types of
/// passive voice: ToBe, Modal, Progressive, and Get.</para>
/// <para>Confidence scoring algorithm:</para>
/// <list type="bullet">
///   <item>Base confidence: 0.75</item>
///   <item>+0.25 if "by [agent]" phrase is present</item>
///   <item>-0.25 if participle is a common state adjective</item>
///   <item>+0.15 if progressive passive (more definitive)</item>
///   <item>-0.20 if state context verb present (seems, appears, etc.)</item>
/// </list>
/// <para>Feature-gated to Writer Pro tier.</para>
/// </remarks>
public class PassiveVoiceDetector : IPassiveVoiceDetector
{
    private readonly ISentenceTokenizer _sentenceTokenizer;
    private readonly ILogger<PassiveVoiceDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PassiveVoiceDetector"/> class.
    /// </summary>
    /// <param name="sentenceTokenizer">Sentence tokenizer for splitting text.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public PassiveVoiceDetector(
        ISentenceTokenizer sentenceTokenizer,
        ILogger<PassiveVoiceDetector> logger)
    {
        _sentenceTokenizer = sentenceTokenizer ?? throw new ArgumentNullException(nameof(sentenceTokenizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool ContainsPassiveVoice(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return false;
        }

        var match = DetectInSentence(sentence);
        return match?.IsPassiveVoice == true;
    }

    /// <inheritdoc />
    public IReadOnlyList<PassiveVoiceMatch> Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogTrace("Empty text provided, returning no matches");
            return Array.Empty<PassiveVoiceMatch>();
        }

        var sentences = _sentenceTokenizer.Tokenize(text);
        _logger.LogDebug("Analyzing {SentenceCount} sentences for passive voice", sentences.Count);

        var matches = new List<PassiveVoiceMatch>();
        var currentPosition = 0;

        foreach (var sentenceInfo in sentences)
        {
            var sentence = sentenceInfo.Text;
            _logger.LogTrace(
                "Checking sentence: '{SentencePreview}...'",
                sentence.Length > 50 ? sentence[..50] : sentence);

            // Find the sentence position in original text
            var sentenceStart = text.IndexOf(sentence, currentPosition, StringComparison.Ordinal);
            if (sentenceStart < 0)
            {
                sentenceStart = currentPosition;
            }

            var match = DetectInSentenceWithPosition(sentence, sentenceStart);
            if (match is not null && match.IsPassiveVoice)
            {
                matches.Add(match);
                _logger.LogTrace(
                    "Passive voice detected: '{Construction}' confidence {Confidence:F2}",
                    match.PassiveConstruction,
                    match.Confidence);
            }

            currentPosition = sentenceStart + sentence.Length;
        }

        _logger.LogDebug(
            "Detection completed: {PassiveCount} passive sentences in {TotalSentences} total",
            matches.Count, sentences.Count);

        return matches.AsReadOnly();
    }

    /// <inheritdoc />
    public PassiveVoiceMatch? DetectInSentence(string sentence)
    {
        return DetectInSentenceWithPosition(sentence, 0);
    }

    /// <inheritdoc />
    public double GetPassiveVoicePercentage(string text, out int passiveCount, out int totalSentences)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            passiveCount = 0;
            totalSentences = 0;
            return 0.0;
        }

        var sentences = _sentenceTokenizer.Tokenize(text);
        totalSentences = sentences.Count;

        if (totalSentences == 0)
        {
            passiveCount = 0;
            return 0.0;
        }

        passiveCount = 0;
        foreach (var sentenceInfo in sentences)
        {
            if (ContainsPassiveVoice(sentenceInfo.Text))
            {
                passiveCount++;
            }
        }

        var percentage = (double)passiveCount / totalSentences * 100.0;

        _logger.LogDebug(
            "Passive voice percentage: {Percentage:F1}% ({PassiveCount}/{TotalSentences})",
            percentage, passiveCount, totalSentences);

        return percentage;
    }

    /// <summary>
    /// Detects passive voice in a sentence with position tracking.
    /// </summary>
    private PassiveVoiceMatch? DetectInSentenceWithPosition(string sentence, int textOffset)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return null;
        }

        // Try patterns in priority order: Progressive → Perfect → Modal → ToBe → Get
        // Progressive is most specific, so check it first
        if (TryMatchPattern(sentence, PassivePatterns.ProgressivePassive, PassiveType.Progressive, textOffset, out var match))
        {
            return match;
        }

        if (TryMatchPattern(sentence, PassivePatterns.PerfectPassive, PassiveType.ToBe, textOffset, out match))
        {
            return match;
        }

        if (TryMatchPattern(sentence, PassivePatterns.ModalPassive, PassiveType.Modal, textOffset, out match))
        {
            return match;
        }

        if (TryMatchPattern(sentence, PassivePatterns.ToBePassive, PassiveType.ToBe, textOffset, out match))
        {
            return match;
        }

        if (TryMatchPattern(sentence, PassivePatterns.GetPassive, PassiveType.Get, textOffset, out match))
        {
            return match;
        }

        return null;
    }

    /// <summary>
    /// Attempts to match a passive pattern and calculate confidence.
    /// </summary>
    private bool TryMatchPattern(
        string sentence,
        Regex pattern,
        PassiveType type,
        int textOffset,
        out PassiveVoiceMatch? result)
    {
        result = null;

        var regexMatch = pattern.Match(sentence);
        if (!regexMatch.Success)
        {
            return false;
        }

        var passiveConstruction = regexMatch.Value;
        var participle = regexMatch.Groups.Count > 2 ? regexMatch.Groups[2].Value : passiveConstruction;

        _logger.LogTrace(
            "Pattern match: {PatternType} - '{Construction}'",
            type, passiveConstruction);

        var confidence = CalculateConfidence(sentence, participle, type);

        _logger.LogTrace(
            "Disambiguation: adjective={IsAdjective}, hasAgent={HasAgent}",
            PassivePatterns.CommonStateAdjectives.Contains(participle),
            PassivePatterns.ByAgentPhrase.IsMatch(sentence));

        if (confidence < 0.5)
        {
            _logger.LogWarning(
                "Low confidence detection ({Confidence:F2}): '{Sentence}'",
                confidence, sentence.Length > 80 ? sentence[..80] + "..." : sentence);
        }

        result = new PassiveVoiceMatch(
            Sentence: sentence,
            PassiveConstruction: passiveConstruction,
            Type: type,
            Confidence: confidence,
            StartIndex: textOffset + regexMatch.Index,
            EndIndex: textOffset + regexMatch.Index + regexMatch.Length);

        return true;
    }

    /// <summary>
    /// Calculates confidence score for a passive voice match.
    /// </summary>
    private static double CalculateConfidence(string sentence, string participle, PassiveType type)
    {
        // LOGIC: Base confidence for pattern match
        var confidence = 0.75;

        // Boost: "by [agent]" phrase strongly indicates passive
        if (PassivePatterns.ByAgentPhrase.IsMatch(sentence))
        {
            confidence += 0.25;
        }

        // Penalty: Common state adjective (closed, tired, etc.)
        // Use 0.30 penalty to ensure confidence drops below 0.5 threshold
        if (PassivePatterns.CommonStateAdjectives.Contains(participle))
        {
            confidence -= 0.30;
        }

        // Boost: Progressive passive is more definitive
        if (type == PassiveType.Progressive)
        {
            confidence += 0.15;
        }

        // Penalty: State context verb (seems, appears, looks)
        if (PassivePatterns.StateContextVerbPattern.IsMatch(sentence))
        {
            confidence -= 0.20;
        }

        return Math.Clamp(confidence, 0.0, 1.0);
    }
}
