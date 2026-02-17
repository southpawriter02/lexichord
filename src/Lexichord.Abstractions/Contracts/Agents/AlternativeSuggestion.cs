// -----------------------------------------------------------------------
// <copyright file="AlternativeSuggestion.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Represents an alternative suggestion for fixing a style deviation,
/// providing users with multiple options to choose from.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The LLM generates multiple fix options when the primary suggestion
/// may not be the only valid approach. Alternatives allow users to select the fix
/// that best matches their intent or preferences.
/// </para>
/// <para>
/// <b>Ordering:</b> Alternatives are typically ordered by descending confidence,
/// with the most likely correct fix appearing first.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
/// <param name="SuggestedText">
/// The alternative replacement text that would fix the deviation.
/// </param>
/// <param name="Explanation">
/// A brief explanation of why this alternative might be preferred,
/// or how it differs from the primary suggestion.
/// </param>
/// <param name="Confidence">
/// Confidence score for this alternative (0.0 to 1.0), indicating how
/// likely this is to be the correct fix.
/// </param>
public record AlternativeSuggestion(
    string SuggestedText,
    string Explanation,
    double Confidence);
