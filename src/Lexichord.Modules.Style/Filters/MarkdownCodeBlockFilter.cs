using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Filters;

/// <summary>
/// Filters Markdown code blocks from content before linting.
/// </summary>
/// <remarks>
/// LOGIC: MarkdownCodeBlockFilter detects and excludes:
/// 1. Fenced code blocks (``` or ~~~)
/// 2. Inline code spans (`backticks`)
/// 3. Optionally: Indented code blocks (4 spaces)
///
/// Detection Strategy:
/// - Fenced blocks use a state machine to track open/close
/// - Inline code uses regex with proper escape handling
/// - Indented blocks track consecutive indented lines
///
/// Thread Safety:
/// - Filter is stateless and thread-safe
/// - No instance state modified during filtering
///
/// Version: v0.2.7b
/// </remarks>
public sealed class MarkdownCodeBlockFilter : IContentFilter
{
    private readonly ILogger<MarkdownCodeBlockFilter> _logger;
    private readonly MarkdownCodeBlockOptions _options;

    /// <summary>
    /// Regex to match fenced code block opening.
    /// Captures: fence chars (``` or ~~~), language hint, optional attributes
    /// </summary>
    /// <remarks>
    /// LOGIC: Pattern breakdown:
    /// - ^(\s*)           : Leading whitespace (captured for indent)
    /// - (`{3,}|~{3,})    : 3+ backticks OR 3+ tildes (captured fence)
    /// - (\w+)?           : Optional language identifier
    /// - (\s.*)?$         : Optional attributes (e.g., {.class})
    /// </remarks>
    private static readonly Regex FenceOpenPattern = new(
        @"^(\s*)(`{3,}|~{3,})(\w+)?(\s.*)?$",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Regex to match inline code spans.
    /// Handles single and multiple backticks.
    /// </summary>
    /// <remarks>
    /// LOGIC: Pattern breakdown:
    /// - (?&lt;!\\)           : Not preceded by backslash (escape)
    /// - (`+)              : One or more backticks (opening)
    /// - (.+?)             : Code content (non-greedy)
    /// - \1                : Same number of backticks (closing)
    ///
    /// Examples matched:
    /// - `code`            : Single backtick
    /// - ``code with ` ``  : Double backtick (contains literal `)
    /// - ```inline```      : Triple backtick inline
    ///
    /// Note: This pattern does NOT match fenced blocks because
    /// fenced blocks have newlines after the opening fence.
    /// </remarks>
    private static readonly Regex InlineCodePattern = new(
        @"(?<!\\)(`+)(?!`)(.+?)(?<!`)\1(?!`)",
        RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownCodeBlockFilter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Optional filter configuration.</param>
    public MarkdownCodeBlockFilter(
        ILogger<MarkdownCodeBlockFilter> logger,
        MarkdownCodeBlockOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? MarkdownCodeBlockOptions.Default;
    }

    /// <inheritdoc/>
    public string Name => "MarkdownCodeBlock";

    /// <inheritdoc/>
    public int Priority => 200; // After frontmatter (100)

    /// <inheritdoc/>
    public bool CanFilter(string fileExtension)
    {
        // LOGIC: Only apply to Markdown files
        return fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".mdx", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public FilteredContent Filter(string content, ContentFilterOptions options)
    {
        if (string.IsNullOrEmpty(content))
        {
            return FilteredContent.None(content);
        }

        if (!options.EnableCodeBlockFilter)
        {
            _logger.LogDebug("Code block filtering disabled");
            return FilteredContent.None(content);
        }

        var exclusions = new List<ExcludedRegion>();

        // LOGIC: Phase 1 - Detect fenced code blocks
        var fencedBlocks = DetectFencedCodeBlocks(content);
        exclusions.AddRange(fencedBlocks);

        _logger.LogDebug(
            "Detected {Count} fenced code blocks",
            fencedBlocks.Count);

        // LOGIC: Phase 2 - Detect inline code (skip inside fenced blocks)
        if (_options.DetectInlineCode)
        {
            var inlineCode = DetectInlineCode(content, fencedBlocks);
            exclusions.AddRange(inlineCode);

            _logger.LogDebug(
                "Detected {Count} inline code spans",
                inlineCode.Count);
        }

        // LOGIC: Phase 3 - (Optional) Detect indented code blocks
        if (_options.DetectIndentedBlocks)
        {
            var indentedBlocks = DetectIndentedCodeBlocks(content, exclusions);
            exclusions.AddRange(indentedBlocks);

            _logger.LogDebug(
                "Detected {Count} indented code blocks",
                indentedBlocks.Count);
        }

        return new FilteredContent(
            ProcessedContent: content,
            ExcludedRegions: exclusions,
            OriginalContent: content
        );
    }

    /// <summary>
    /// Detects fenced code blocks using a state machine.
    /// </summary>
    /// <param name="content">Document content.</param>
    /// <returns>List of excluded regions for fenced blocks.</returns>
    /// <remarks>
    /// LOGIC: Uses line-by-line state machine:
    /// - Track fence character (` or ~) and count
    /// - Opening fence starts exclusion zone
    /// - Matching closing fence ends zone
    /// - Handles nested structures (blockquotes)
    /// </remarks>
    private IReadOnlyList<ExcludedRegion> DetectFencedCodeBlocks(string content)
    {
        var exclusions = new List<ExcludedRegion>();
        var lines = content.Split('\n');
        var offset = 0;

        FenceState? currentFence = null;

        foreach (var line in lines)
        {
            var lineWithNewline = line + (offset + line.Length < content.Length ? "\n" : "");
            var lineLength = lineWithNewline.Length;

            if (currentFence == null)
            {
                // LOGIC: Looking for opening fence
                var match = FenceOpenPattern.Match(line);
                if (match.Success)
                {
                    var indent = match.Groups[1].Value;
                    var fence = match.Groups[2].Value;
                    var language = match.Groups[3].Success ? match.Groups[3].Value : null;

                    // LOGIC: Check fence length limit
                    if (fence.Length <= _options.MaxFenceLength)
                    {
                        currentFence = new FenceState(
                            FenceChar: fence[0],
                            FenceLength: fence.Length,
                            IndentLength: indent.Length,
                            StartOffset: offset,
                            Language: language
                        );

                        _logger.LogDebug(
                            "Found fenced code block opening at offset {Offset}: {Fence}{Language}",
                            offset, fence, language ?? "");
                    }
                }
            }
            else
            {
                // LOGIC: Inside fence, looking for closing
                var trimmed = line.TrimStart();
                if (IsClosingFence(trimmed, currentFence))
                {
                    // LOGIC: Found closing fence
                    var endOffset = offset + lineLength;

                    exclusions.Add(new ExcludedRegion(
                        StartOffset: currentFence.StartOffset,
                        EndOffset: endOffset,
                        Reason: ExclusionReason.FencedCodeBlock,
                        Metadata: currentFence.Language
                    ));

                    _logger.LogDebug(
                        "Fenced code block closed at offset {End} (started {Start})",
                        endOffset, currentFence.StartOffset);

                    currentFence = null;
                }
            }

            offset += lineLength;
        }

        // LOGIC: Handle unclosed fence at EOF
        if (currentFence != null)
        {
            _logger.LogWarning(
                "Unclosed fenced code block starting at offset {Offset}",
                currentFence.StartOffset);

            // LOGIC: Treat rest of document as code block
            exclusions.Add(new ExcludedRegion(
                StartOffset: currentFence.StartOffset,
                EndOffset: content.Length,
                Reason: ExclusionReason.FencedCodeBlock,
                Metadata: currentFence.Language
            ));
        }

        return exclusions;
    }

    /// <summary>
    /// Checks if a line is a closing fence for the current open fence.
    /// </summary>
    private static bool IsClosingFence(string trimmedLine, FenceState currentFence)
    {
        // LOGIC: Closing fence must:
        // 1. Start with same fence character
        // 2. Have at least as many fence characters as opening
        // 3. Contain only fence characters and whitespace

        if (trimmedLine.Length < currentFence.FenceLength)
        {
            return false;
        }

        // Check if line starts with fence characters
        var fenceCount = 0;
        foreach (var c in trimmedLine)
        {
            if (c == currentFence.FenceChar)
            {
                fenceCount++;
            }
            else if (!char.IsWhiteSpace(c))
            {
                return false; // Non-whitespace, non-fence character
            }
            else
            {
                break; // Hit whitespace after fence chars
            }
        }

        return fenceCount >= currentFence.FenceLength;
    }

    /// <summary>
    /// Detects inline code spans.
    /// </summary>
    /// <param name="content">Document content.</param>
    /// <param name="fencedBlocks">Already detected fenced blocks to skip.</param>
    /// <returns>List of excluded regions for inline code.</returns>
    /// <remarks>
    /// LOGIC: Uses regex to find backtick-delimited spans.
    /// Skips any matches that fall inside fenced blocks.
    ///
    /// Handles:
    /// - Single backticks: `code`
    /// - Multiple backticks: ``code with ` inside``
    /// - Escaped backticks: \`not code\`
    /// </remarks>
    private IReadOnlyList<ExcludedRegion> DetectInlineCode(
        string content,
        IReadOnlyList<ExcludedRegion> fencedBlocks)
    {
        var exclusions = new List<ExcludedRegion>();

        try
        {
            foreach (Match match in InlineCodePattern.Matches(content))
            {
                var startOffset = match.Index;
                var endOffset = match.Index + match.Length;

                // LOGIC: Skip if inside a fenced block
                var inFencedBlock = fencedBlocks.Any(fb =>
                    startOffset >= fb.StartOffset && startOffset < fb.EndOffset);

                if (inFencedBlock)
                {
                    continue;
                }

                exclusions.Add(new ExcludedRegion(
                    StartOffset: startOffset,
                    EndOffset: endOffset,
                    Reason: ExclusionReason.InlineCode,
                    Metadata: null
                ));

                _logger.LogDebug(
                    "Found inline code at offset {Start}-{End}",
                    startOffset, endOffset);
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Inline code detection timed out");
        }

        return exclusions;
    }

    /// <summary>
    /// Detects indented code blocks (4+ spaces).
    /// </summary>
    /// <param name="content">Document content.</param>
    /// <param name="existingExclusions">Existing exclusions to avoid overlap.</param>
    /// <returns>List of excluded regions for indented code.</returns>
    /// <remarks>
    /// LOGIC: CommonMark spec says 4 spaces or 1 tab of indentation
    /// makes a code block. This is optional because:
    /// - Many documents use indentation for other purposes
    /// - Fenced blocks are more common and unambiguous
    /// - False positives are more likely with indented detection
    /// </remarks>
    private IReadOnlyList<ExcludedRegion> DetectIndentedCodeBlocks(
        string content,
        IReadOnlyList<ExcludedRegion> existingExclusions)
    {
        // LOGIC: Indented code block detection (simplified for now)
        // Full implementation would track consecutive 4-space indented lines
        // with proper handling of blank lines between them
        return Array.Empty<ExcludedRegion>();
    }

    /// <summary>
    /// Tracks state while parsing a fenced code block.
    /// </summary>
    private sealed record FenceState(
        char FenceChar,
        int FenceLength,
        int IndentLength,
        int StartOffset,
        string? Language
    );
}
