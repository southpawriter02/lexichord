namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Represents a request to temporarily highlight a text span.
/// </summary>
/// <remarks>
/// LOGIC: Created by ManuscriptViewModel.HighlightSpanAsync and
/// consumed by the View to render a fade-out highlight animation.
///
/// Version: v0.2.6b
/// </remarks>
/// <param name="StartOffset">Starting character offset (0-indexed).</param>
/// <param name="Length">Number of characters to highlight.</param>
/// <param name="Duration">Duration of the highlight animation.</param>
public record HighlightRequest(
    int StartOffset,
    int Length,
    TimeSpan Duration);
