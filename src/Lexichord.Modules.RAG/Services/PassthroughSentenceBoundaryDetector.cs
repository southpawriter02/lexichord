// =============================================================================
// File: PassthroughSentenceBoundaryDetector.cs
// Project: Lexichord.Modules.RAG
// Description: Placeholder sentence boundary detector that passes through positions.
// =============================================================================
// LOGIC: Minimal implementation that returns positions unchanged.
//   - Serves as a placeholder until v0.5.6c delivers the real detector.
//   - FindSentenceStart returns the input position (no backward expansion).
//   - FindSentenceEnd returns the input position (no forward expansion).
//   - Thread-safe and stateless.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction) - Placeholder
//          v0.5.6c (Sentence Boundary Detection) - Will be replaced
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// A placeholder sentence boundary detector that passes through positions unchanged.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PassthroughSentenceBoundaryDetector"/> is a minimal implementation
/// of <see cref="ISentenceBoundaryDetector"/> that returns input positions without
/// modification. This allows <see cref="SnippetService"/> to function before the
/// actual sentence detection logic is implemented in v0.5.6c.
/// </para>
/// <para>
/// <b>Behavior:</b> When <see cref="SnippetOptions.RespectSentenceBoundaries"/>
/// is enabled, snippets will still be extracted but without sentence-aware
/// boundary expansion.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is stateless and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a as a placeholder for v0.5.6c.
/// </para>
/// </remarks>
public sealed class PassthroughSentenceBoundaryDetector : ISentenceBoundaryDetector
{
    private readonly ILogger<PassthroughSentenceBoundaryDetector> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PassthroughSentenceBoundaryDetector"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public PassthroughSentenceBoundaryDetector(ILogger<PassthroughSentenceBoundaryDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "PassthroughSentenceBoundaryDetector initialized. " +
            "Sentence boundary detection disabled until v0.5.6c.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the input position unchanged. The actual sentence boundary
    /// detection will be implemented in v0.5.6c.
    /// </remarks>
    public int FindSentenceStart(string content, int position)
    {
        // LOGIC: Return position unchanged. Real logic in v0.5.6c.
        return Math.Max(0, position);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the input position unchanged. The actual sentence boundary
    /// detection will be implemented in v0.5.6c.
    /// </remarks>
    public int FindSentenceEnd(string content, int position)
    {
        // LOGIC: Return position unchanged. Real logic in v0.5.6c.
        return string.IsNullOrEmpty(content)
            ? 0
            : Math.Min(content.Length, position);
    }
}
