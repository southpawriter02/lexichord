// -----------------------------------------------------------------------
// <copyright file="DefaultContextFormatter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Templates.Formatters;

/// <summary>
/// Default implementation of <see cref="IContextFormatter"/> using bullet-list and
/// source-attributed formatting styles.
/// </summary>
/// <remarks>
/// <para>
/// This formatter produces plain text output optimized for LLM consumption:
/// </para>
/// <list type="bullet">
///   <item><description>Style rules: Bullet list with name and description.</description></item>
///   <item><description>RAG chunks: Source-attributed blocks with content.</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// This class is stateless and thread-safe. A single instance can be shared
/// across multiple concurrent formatting operations.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var formatter = new DefaultContextFormatter(logger);
///
/// // Format style rules
/// var rules = new List&lt;StyleRule&gt;
/// {
///     new StyleRule("no-jargon", "Avoid Jargon", "Technical jargon reduces accessibility", ...),
///     new StyleRule("active-voice", "Use Active Voice", "Prefer active voice for clarity", ...)
/// };
/// var formattedRules = formatter.FormatStyleRules(rules);
/// // Result:
/// // - Avoid Jargon: Technical jargon reduces accessibility
/// // - Use Active Voice: Prefer active voice for clarity
/// </code>
/// </example>
/// <seealso cref="IContextFormatter"/>
public sealed class DefaultContextFormatter : IContextFormatter
{
    private readonly ILogger<DefaultContextFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultContextFormatter"/> class.
    /// </summary>
    /// <param name="logger">The logger for formatting diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public DefaultContextFormatter(ILogger<DefaultContextFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("DefaultContextFormatter initialized");
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Iterates through the rules and formats each as a bullet point:
    /// <c>- {Name}: {Description}</c>
    /// </para>
    /// <para>
    /// Rules with empty names or descriptions are skipped with a debug log message.
    /// </para>
    /// </remarks>
    public string FormatStyleRules(IReadOnlyList<StyleRule> rules)
    {
        // LOGIC: Return empty string for null or empty input
        if (rules is not { Count: > 0 })
        {
            _logger.LogDebug("FormatStyleRules called with empty or null rules collection");
            return string.Empty;
        }

        _logger.LogDebug("Formatting {RuleCount} style rules", rules.Count);

        var sb = new StringBuilder();
        var formattedCount = 0;

        foreach (var rule in rules)
        {
            // LOGIC: Skip rules with missing name or description
            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                _logger.LogDebug("Skipping rule '{RuleId}' with empty name", rule.Id);
                continue;
            }

            // LOGIC: Format as bullet point with name and description
            // Description may be empty, in which case just use the name
            if (string.IsNullOrWhiteSpace(rule.Description))
            {
                sb.AppendLine($"- {rule.Name}");
            }
            else
            {
                sb.AppendLine($"- {rule.Name}: {rule.Description}");
            }

            formattedCount++;
        }

        _logger.LogDebug(
            "Formatted {FormattedCount} of {TotalCount} style rules",
            formattedCount,
            rules.Count);

        // LOGIC: TrimEnd removes the trailing newline for cleaner output
        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Formats each search hit with a source header and content block:
    /// </para>
    /// <code>
    /// [Source: {DocumentPath}]
    /// {ChunkContent}
    /// </code>
    /// <para>
    /// Chunks are separated by blank lines. Content exceeding <paramref name="maxChunkLength"/>
    /// is truncated with "..." appended.
    /// </para>
    /// </remarks>
    public string FormatRAGChunks(IReadOnlyList<SearchHit> hits, int maxChunkLength = 1000)
    {
        // LOGIC: Return empty string for null or empty input
        if (hits is not { Count: > 0 })
        {
            _logger.LogDebug("FormatRAGChunks called with empty or null hits collection");
            return string.Empty;
        }

        // LOGIC: Clamp maxChunkLength to reasonable bounds
        if (maxChunkLength < 50)
        {
            _logger.LogDebug(
                "maxChunkLength {MaxChunkLength} is too small, using minimum of 50",
                maxChunkLength);
            maxChunkLength = 50;
        }

        _logger.LogDebug(
            "Formatting {HitCount} RAG chunks with maxChunkLength={MaxChunkLength}",
            hits.Count,
            maxChunkLength);

        var sb = new StringBuilder();
        var formattedCount = 0;

        foreach (var hit in hits)
        {
            // LOGIC: Skip hits with null or empty chunk content
            if (hit.Chunk is null || string.IsNullOrWhiteSpace(hit.Chunk.Content))
            {
                _logger.LogDebug(
                    "Skipping hit with null or empty chunk content from document '{DocumentPath}'",
                    hit.Document?.FilePath ?? "unknown");
                continue;
            }

            // LOGIC: Add blank line between chunks (not before first)
            if (formattedCount > 0)
            {
                sb.AppendLine();
            }

            // LOGIC: Format source header with document path
            var sourcePath = hit.Document?.FilePath ?? "Unknown source";
            sb.AppendLine($"[Source: {sourcePath}]");

            // LOGIC: Truncate content if it exceeds maxChunkLength
            var content = hit.Chunk.Content;
            if (content.Length > maxChunkLength)
            {
                content = content[..maxChunkLength].TrimEnd() + "...";
                _logger.LogDebug(
                    "Truncated chunk from {OriginalLength} to {TruncatedLength} characters",
                    hit.Chunk.Content.Length,
                    content.Length);
            }

            sb.AppendLine(content);
            formattedCount++;
        }

        _logger.LogDebug(
            "Formatted {FormattedCount} of {TotalCount} RAG chunks",
            formattedCount,
            hits.Count);

        // LOGIC: TrimEnd removes the trailing newline for cleaner output
        return sb.ToString().TrimEnd();
    }
}
