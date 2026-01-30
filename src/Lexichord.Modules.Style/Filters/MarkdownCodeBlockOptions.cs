namespace Lexichord.Modules.Style.Filters;

/// <summary>
/// Configuration options for Markdown code block detection.
/// </summary>
/// <remarks>
/// LOGIC: MarkdownCodeBlockOptions controls code block detection behavior:
/// - DetectIndentedBlocks: Off by default (less common, more false positives)
/// - DetectInlineCode: On by default (common in technical docs)
/// - MaxFenceLength: Safety limit to prevent regex issues
///
/// Version: v0.2.7b
/// </remarks>
/// <param name="DetectIndentedBlocks">Whether to detect 4-space indented blocks.</param>
/// <param name="DetectInlineCode">Whether to detect `inline` code spans.</param>
/// <param name="MaxFenceLength">Maximum fence length to recognize (safety limit).</param>
public sealed record MarkdownCodeBlockOptions(
    bool DetectIndentedBlocks = false,
    bool DetectInlineCode = true,
    int MaxFenceLength = 10)
{
    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static MarkdownCodeBlockOptions Default { get; } = new();
}
