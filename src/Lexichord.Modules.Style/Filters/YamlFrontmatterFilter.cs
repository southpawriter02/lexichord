using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Filters;

/// <summary>
/// Filters YAML, TOML, and JSON frontmatter from content before linting.
/// </summary>
/// <remarks>
/// LOGIC: YamlFrontmatterFilter detects and excludes document frontmatter:
/// 1. YAML frontmatter (--- delimited) - Most common format
/// 2. TOML frontmatter (+++ delimited) - Used by Hugo
/// 3. JSON frontmatter ({ on first line) - Less common
///
/// Detection Strategy:
/// - Frontmatter MUST start at document beginning (after optional BOM)
/// - Each format has specific opening and closing delimiters
/// - Unclosed frontmatter extends exclusion to EOF
///
/// Thread Safety:
/// - Filter is stateless and thread-safe
/// - No instance state modified during filtering
///
/// Priority: 100 (runs BEFORE code block filter at 200)
/// This ensures frontmatter is detected before any code block
/// detection runs, preventing --- from being misinterpreted.
///
/// Version: v0.2.7c
/// </remarks>
public sealed class YamlFrontmatterFilter : IContentFilter
{
    private readonly ILogger<YamlFrontmatterFilter> _logger;
    private readonly YamlFrontmatterOptions _options;

    /// <summary>
    /// UTF-8 BOM character that may prefix documents.
    /// </summary>
    /// <remarks>
    /// LOGIC: The Byte Order Mark (U+FEFF) is sometimes prepended
    /// by text editors. We need to skip it for frontmatter detection.
    /// </remarks>
    private const char Bom = '\uFEFF';

    /// <summary>
    /// YAML frontmatter delimiter.
    /// </summary>
    private const string YamlDelimiter = "---";

    /// <summary>
    /// TOML frontmatter delimiter.
    /// </summary>
    private const string TomlDelimiter = "+++";

    /// <summary>
    /// Regex to match YAML/TOML frontmatter opening line.
    /// </summary>
    /// <remarks>
    /// LOGIC: Pattern breakdown:
    /// - ^              : Start of line
    /// - (---|\\+\\+\\+): YAML (---) or TOML (+++) delimiter
    /// - \\s*           : Optional trailing whitespace
    /// - $              : End of line
    /// </remarks>
    private static readonly Regex DelimiterPattern = new(
        @"^(---|\+\+\+)\s*$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlFrontmatterFilter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="options">Optional filter configuration. Defaults to all formats enabled.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public YamlFrontmatterFilter(
        ILogger<YamlFrontmatterFilter> logger,
        YamlFrontmatterOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? YamlFrontmatterOptions.Default;

        _logger.LogDebug(
            "YamlFrontmatterFilter initialized with YAML={Yaml}, TOML={Toml}, JSON={Json}",
            _options.DetectYamlFrontmatter,
            _options.DetectTomlFrontmatter,
            _options.DetectJsonFrontmatter);
    }

    /// <inheritdoc/>
    public string Name => "YamlFrontmatter";

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Priority 100 ensures frontmatter detection runs BEFORE
    /// code block detection (priority 200). This prevents --- delimiters
    /// from being misinterpreted as horizontal rules or code fences.
    /// </remarks>
    public int Priority => 100;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Frontmatter filtering applies to Markdown files only.
    /// Supported extensions: .md, .markdown, .mdx
    /// </remarks>
    public bool CanFilter(string fileExtension)
    {
        // LOGIC: Only apply to Markdown files
        var canFilter = fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".mdx", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug(
            "CanFilter({Extension}) = {Result}",
            fileExtension,
            canFilter);

        return canFilter;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Main filtering entry point. Detects frontmatter at document start
    /// and returns an exclusion region if found.
    ///
    /// Detection order:
    /// 1. Check if filtering is enabled
    /// 2. Strip BOM if present and configured
    /// 3. Detect YAML (---) frontmatter
    /// 4. Detect TOML (+++) frontmatter
    /// 5. Detect JSON ({) frontmatter
    ///
    /// Only ONE frontmatter block can exist (at document start).
    /// </remarks>
    public FilteredContent Filter(string content, ContentFilterOptions options)
    {
        // LOGIC: Handle null or empty content
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug("Content is empty, returning no exclusions");
            return FilteredContent.None(content ?? string.Empty);
        }

        // LOGIC: Check if frontmatter filtering is enabled
        if (!options.EnableFrontmatterFilter)
        {
            _logger.LogDebug("Frontmatter filtering disabled by options");
            return FilteredContent.None(content);
        }

        _logger.LogDebug(
            "Filtering content of {Length} characters for frontmatter",
            content.Length);

        // LOGIC: Detect and strip BOM if present
        var bomOffset = 0;
        var contentToAnalyze = content;

        if (_options.StripBom && content.Length > 0 && content[0] == Bom)
        {
            bomOffset = 1;
            contentToAnalyze = content[1..];
            _logger.LogDebug("Stripped UTF-8 BOM from content start");
        }

        // LOGIC: Try each frontmatter format in order
        ExcludedRegion? exclusion = null;

        // Try YAML frontmatter (---)
        if (_options.DetectYamlFrontmatter && exclusion == null)
        {
            exclusion = DetectYamlFrontmatter(contentToAnalyze, bomOffset);
        }

        // Try TOML frontmatter (+++)
        if (_options.DetectTomlFrontmatter && exclusion == null)
        {
            exclusion = DetectTomlFrontmatter(contentToAnalyze, bomOffset);
        }

        // Try JSON frontmatter ({)
        if (_options.DetectJsonFrontmatter && exclusion == null)
        {
            exclusion = DetectJsonFrontmatter(contentToAnalyze, bomOffset);
        }

        // LOGIC: Return result
        if (exclusion != null)
        {
            _logger.LogDebug(
                "Found {Reason} frontmatter: offset {Start}-{End}",
                exclusion.Reason,
                exclusion.StartOffset,
                exclusion.EndOffset);

            return new FilteredContent(
                ProcessedContent: content,
                ExcludedRegions: new[] { exclusion },
                OriginalContent: content);
        }

        _logger.LogDebug("No frontmatter detected");
        return FilteredContent.None(content);
    }

    /// <summary>
    /// Detects YAML frontmatter (--- delimited).
    /// </summary>
    /// <param name="content">Content to analyze (BOM already stripped).</param>
    /// <param name="bomOffset">Offset to add for BOM if it was stripped.</param>
    /// <returns>ExcludedRegion if YAML frontmatter found, null otherwise.</returns>
    /// <remarks>
    /// LOGIC: YAML frontmatter format:
    /// ---
    /// key: value
    /// ---
    ///
    /// Rules:
    /// - Opening --- must be on first line
    /// - Closing --- must be on its own line
    /// - Trailing whitespace on delimiter lines is allowed
    /// </remarks>
    private ExcludedRegion? DetectYamlFrontmatter(string content, int bomOffset)
    {
        return DetectDelimitedFrontmatter(
            content,
            bomOffset,
            YamlDelimiter,
            ExclusionReason.Frontmatter);
    }

    /// <summary>
    /// Detects TOML frontmatter (+++ delimited).
    /// </summary>
    /// <param name="content">Content to analyze (BOM already stripped).</param>
    /// <param name="bomOffset">Offset to add for BOM if it was stripped.</param>
    /// <returns>ExcludedRegion if TOML frontmatter found, null otherwise.</returns>
    /// <remarks>
    /// LOGIC: TOML frontmatter format (Hugo style):
    /// +++
    /// key = "value"
    /// +++
    ///
    /// Same rules as YAML but with +++ delimiter.
    /// </remarks>
    private ExcludedRegion? DetectTomlFrontmatter(string content, int bomOffset)
    {
        return DetectDelimitedFrontmatter(
            content,
            bomOffset,
            TomlDelimiter,
            ExclusionReason.TomlFrontmatter);
    }

    /// <summary>
    /// Detects delimited frontmatter (YAML or TOML).
    /// </summary>
    /// <param name="content">Content to analyze.</param>
    /// <param name="bomOffset">Offset for BOM if stripped.</param>
    /// <param name="delimiter">The delimiter to look for (--- or +++).</param>
    /// <param name="reason">The exclusion reason to use.</param>
    /// <returns>ExcludedRegion if frontmatter found, null otherwise.</returns>
    /// <remarks>
    /// LOGIC: Shared detection logic for YAML and TOML:
    /// 1. Check if first line matches delimiter pattern
    /// 2. Scan subsequent lines for closing delimiter
    /// 3. Include everything from start to end of closing line
    /// 4. If no closing found, extend to EOF (with warning)
    /// </remarks>
    private ExcludedRegion? DetectDelimitedFrontmatter(
        string content,
        int bomOffset,
        string delimiter,
        ExclusionReason reason)
    {
        // LOGIC: Split into lines for analysis
        var lines = content.Split('\n');
        if (lines.Length == 0)
        {
            return null;
        }

        // LOGIC: Check first line for opening delimiter
        var firstLine = lines[0].TrimEnd('\r'); // Handle CRLF
        if (!IsDelimiterLine(firstLine, delimiter))
        {
            return null;
        }

        _logger.LogDebug(
            "Found {Delimiter} opening delimiter on first line",
            delimiter);

        // LOGIC: Search for closing delimiter
        var currentOffset = lines[0].Length + 1; // +1 for \n
        if (content.Length > lines[0].Length && content[lines[0].Length] == '\r')
        {
            // Account for CRLF in offset calculation
            // Note: Split already removed \n, but we need accurate offsets
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var lineLength = lines[i].Length + (i < lines.Length - 1 ? 1 : 0); // +1 for \n except last

            if (IsDelimiterLine(line, delimiter))
            {
                // LOGIC: Found closing delimiter
                // Calculate end offset: include the closing delimiter line
                var endOffset = CalculateOffset(content, lines, i + 1);

                _logger.LogDebug(
                    "Found {Delimiter} closing delimiter on line {Line}",
                    delimiter,
                    i + 1);

                return new ExcludedRegion(
                    StartOffset: 0, // Always start at 0 to include BOM if present
                    EndOffset: endOffset + bomOffset,
                    Reason: reason,
                    Metadata: delimiter);
            }
        }

        // LOGIC: No closing delimiter found - extend to EOF
        _logger.LogWarning(
            "Unclosed {Delimiter} frontmatter, excluding to EOF",
            delimiter);

        return new ExcludedRegion(
            StartOffset: 0, // Always start at 0 to include BOM if present
            EndOffset: content.Length + bomOffset,
            Reason: reason,
            Metadata: $"{delimiter} (unclosed)");
    }

    /// <summary>
    /// Detects JSON frontmatter ({ on first line).
    /// </summary>
    /// <param name="content">Content to analyze (BOM already stripped).</param>
    /// <param name="bomOffset">Offset to add for BOM if it was stripped.</param>
    /// <returns>ExcludedRegion if JSON frontmatter found, null otherwise.</returns>
    /// <remarks>
    /// LOGIC: JSON frontmatter format:
    /// {
    ///   "key": "value"
    /// }
    ///
    /// Rules:
    /// - Opening { must be on first line (possibly with whitespace)
    /// - Closing } must be on its own line
    /// - Uses brace counting for nested objects
    /// </remarks>
    private ExcludedRegion? DetectJsonFrontmatter(string content, int bomOffset)
    {
        if (content.Length == 0)
        {
            return null;
        }

        // LOGIC: Check if first line starts with { (allowing leading whitespace)
        var firstLine = content.Split('\n')[0].TrimEnd('\r');
        var trimmedFirst = firstLine.TrimStart();

        if (!trimmedFirst.StartsWith('{'))
        {
            return null;
        }

        _logger.LogDebug("Found JSON frontmatter opening brace");

        // LOGIC: Use brace counting to find matching }
        var braceCount = 0;
        var endOffset = -1;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (c == '{')
            {
                braceCount++;
            }
            else if (c == '}')
            {
                braceCount--;
                if (braceCount == 0)
                {
                    // LOGIC: Found matching closing brace
                    // Include up to end of line or EOF
                    endOffset = i + 1;

                    // Skip to end of line
                    while (endOffset < content.Length &&
                           content[endOffset] != '\n')
                    {
                        endOffset++;
                    }

                    // Include the newline if present
                    if (endOffset < content.Length && content[endOffset] == '\n')
                    {
                        endOffset++;
                    }

                    break;
                }
            }
        }

        if (endOffset > 0)
        {
            _logger.LogDebug(
                "Found JSON frontmatter closing brace at offset {Offset}",
                endOffset);

            return new ExcludedRegion(
                StartOffset: 0, // Always start at 0 to include BOM if present
                EndOffset: endOffset + bomOffset,
                Reason: ExclusionReason.JsonFrontmatter,
                Metadata: "json");
        }

        // LOGIC: No closing brace found - extend to EOF
        _logger.LogWarning("Unclosed JSON frontmatter, excluding to EOF");

        return new ExcludedRegion(
            StartOffset: 0, // Always start at 0 to include BOM if present
            EndOffset: content.Length + bomOffset,
            Reason: ExclusionReason.JsonFrontmatter,
            Metadata: "json (unclosed)");
    }

    /// <summary>
    /// Checks if a line is a frontmatter delimiter line.
    /// </summary>
    /// <param name="line">The line to check (already trimmed of CR).</param>
    /// <param name="delimiter">The delimiter to match (--- or +++).</param>
    /// <returns>True if the line is a valid delimiter line.</returns>
    /// <remarks>
    /// LOGIC: A delimiter line:
    /// - Contains only the delimiter characters
    /// - May have trailing whitespace
    /// - No other content allowed
    /// </remarks>
    private static bool IsDelimiterLine(string line, string delimiter)
    {
        var trimmed = line.TrimEnd();
        return trimmed == delimiter;
    }

    /// <summary>
    /// Calculates the character offset for a given line number.
    /// </summary>
    /// <param name="content">Original content.</param>
    /// <param name="lines">Lines array from Split.</param>
    /// <param name="lineIndex">Target line index (0-based, exclusive).</param>
    /// <returns>Character offset in the content.</returns>
    /// <remarks>
    /// LOGIC: Accurately calculates offset by walking through content,
    /// accounting for both LF and CRLF line endings.
    /// </remarks>
    private static int CalculateOffset(string content, string[] lines, int lineIndex)
    {
        var offset = 0;
        var lineCount = Math.Min(lineIndex, lines.Length);

        for (var i = 0; i < lineCount; i++)
        {
            offset += lines[i].Length;

            // Add newline character if not at end of content
            if (offset < content.Length)
            {
                offset++; // For \n

                // Check for CRLF (already handled by Split removing \n)
            }
        }

        return Math.Min(offset, content.Length);
    }
}
