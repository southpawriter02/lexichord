// <copyright file="PassiveVoiceTypes.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Categorizes the type of passive voice construction detected.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4b - Different passive types have varying patterns and
/// require different regex matching strategies.
/// </remarks>
public enum PassiveType
{
    /// <summary>
    /// Standard "to be" passive: am/is/are/was/were/been/being + past participle.
    /// </summary>
    /// <example>"The code was written by the developer."</example>
    ToBe,

    /// <summary>
    /// Modal passive: modal verb + be + past participle.
    /// </summary>
    /// <example>"The file will be deleted tomorrow."</example>
    Modal,

    /// <summary>
    /// Progressive passive: form of "to be" + being + past participle.
    /// </summary>
    /// <example>"The project is being reviewed."</example>
    Progressive,

    /// <summary>
    /// Get-passive: get/gets/got/gotten/getting + past participle.
    /// </summary>
    /// <example>"She got fired last week."</example>
    Get
}

/// <summary>
/// Represents a detected passive voice construction with confidence scoring.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4b - The confidence score helps distinguish true passive
/// constructions from predicate adjectives (e.g., "The door is closed").</para>
/// <para>A confidence score ≥ 0.5 indicates likely passive voice.</para>
/// <para>A confidence score &lt; 0.5 suggests a predicate adjective.</para>
/// </remarks>
/// <param name="Sentence">The full sentence containing the passive construction.</param>
/// <param name="PassiveConstruction">The matched passive phrase (e.g., "was written").</param>
/// <param name="Type">The classification of passive voice type.</param>
/// <param name="Confidence">
/// Score from 0.0 to 1.0 indicating likelihood of true passive voice.
/// Higher values indicate more confident detection.
/// </param>
/// <param name="StartIndex">Start position of the construction within the original text.</param>
/// <param name="EndIndex">End position (exclusive) of the construction within the original text.</param>
public record PassiveVoiceMatch(
    string Sentence,
    string PassiveConstruction,
    PassiveType Type,
    double Confidence,
    int StartIndex,
    int EndIndex)
{
    /// <summary>
    /// Gets a value indicating whether this match is considered passive voice
    /// based on the confidence threshold (≥ 0.5).
    /// </summary>
    public bool IsPassiveVoice => Confidence >= 0.5;

    /// <summary>
    /// Gets the length of the passive construction.
    /// </summary>
    public int Length => EndIndex - StartIndex;
}

/// <summary>
/// Service for detecting passive voice constructions in text.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4b - Uses regex-based pattern matching with confidence scoring
/// to identify passive voice while distinguishing from predicate adjectives.</para>
/// <para>The detector respects the active Voice Profile's constraints:
/// - When <c>AllowPassiveVoice = false</c>, any passive triggers Warning
/// - When <c>AllowPassiveVoice = true</c>, exceeding threshold triggers Info</para>
/// <para>Feature-gated to Writer Pro tier via <c>FeatureFlags.Style.VoiceProfiler</c>.</para>
/// </remarks>
public interface IPassiveVoiceDetector
{
    /// <summary>
    /// Quickly checks if a single sentence contains passive voice.
    /// </summary>
    /// <param name="sentence">The sentence to check.</param>
    /// <returns>
    /// True if the sentence contains passive voice with confidence ≥ 0.5;
    /// false otherwise or if the sentence is empty/null.
    /// </returns>
    /// <remarks>
    /// This is a convenience method for quick checks. For detailed analysis
    /// including confidence scores, use <see cref="DetectInSentence"/>.
    /// </remarks>
    bool ContainsPassiveVoice(string sentence);

    /// <summary>
    /// Detects all passive voice constructions in the provided text.
    /// </summary>
    /// <param name="text">The full text to analyze (may contain multiple sentences).</param>
    /// <returns>
    /// An immutable list of all passive voice matches with confidence ≥ 0.5.
    /// Returns an empty list if no passive voice is detected or text is empty.
    /// </returns>
    /// <remarks>
    /// <para>The text is tokenized into sentences using the configured
    /// <c>ISentenceTokenizer</c> before analysis.</para>
    /// <para>Each match includes position information relative to the original text.</para>
    /// </remarks>
    IReadOnlyList<PassiveVoiceMatch> Detect(string text);

    /// <summary>
    /// Analyzes a single sentence for passive voice construction.
    /// </summary>
    /// <param name="sentence">The sentence to analyze.</param>
    /// <returns>
    /// A <see cref="PassiveVoiceMatch"/> if passive voice is detected (regardless of confidence);
    /// null if no passive pattern matches.
    /// </returns>
    /// <remarks>
    /// Unlike <see cref="ContainsPassiveVoice"/>, this method returns matches
    /// even with low confidence, allowing callers to make their own threshold decisions.
    /// </remarks>
    PassiveVoiceMatch? DetectInSentence(string sentence);

    /// <summary>
    /// Calculates the percentage of sentences containing passive voice.
    /// </summary>
    /// <param name="text">The full text to analyze.</param>
    /// <param name="passiveCount">Output: Number of sentences with passive voice.</param>
    /// <param name="totalSentences">Output: Total number of sentences analyzed.</param>
    /// <returns>
    /// The percentage of passive voice sentences (0.0 to 100.0).
    /// Returns 0.0 if text is empty or contains no sentences.
    /// </returns>
    /// <remarks>
    /// Used by the linting orchestrator to check against the Voice Profile's
    /// <c>MaxPassiveVoicePercentage</c> threshold.
    /// </remarks>
    double GetPassiveVoicePercentage(string text, out int passiveCount, out int totalSentences);
}
