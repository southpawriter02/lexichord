// -----------------------------------------------------------------------
// <copyright file="DiffGenerator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Lexichord.Abstractions.Contracts.Agents;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Generates text diffs between original and suggested text using DiffPlex.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This internal helper class provides diff generation functionality
/// for the <see cref="FixSuggestionGenerator"/>:
/// <list type="bullet">
///   <item><description>Uses DiffPlex for accurate inline diff computation</description></item>
///   <item><description>Generates structured <see cref="DiffOperation"/> sequences</description></item>
///   <item><description>Produces unified diff format for console/text display</description></item>
///   <item><description>Creates HTML diff with CSS classes for UI rendering</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. Each method call creates
/// new diff builder instances.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
public sealed class DiffGenerator
{
    private readonly ILogger<DiffGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffGenerator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public DiffGenerator(ILogger<DiffGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a diff between original and suggested text.
    /// </summary>
    /// <param name="original">The original text containing the violation.</param>
    /// <param name="suggested">The suggested replacement text.</param>
    /// <returns>A <see cref="TextDiff"/> with operations, unified diff, and HTML diff.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses DiffPlex's <see cref="InlineDiffBuilder"/> for word-level
    /// diffing, then transforms the result into our <see cref="TextDiff"/> format.
    /// </para>
    /// </remarks>
    public TextDiff GenerateDiff(string original, string suggested)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(suggested);

        // LOGIC: Handle identical text case early
        if (original == suggested)
        {
            _logger.LogDebug("Diff generation: texts are identical, returning empty diff");
            return TextDiff.Empty;
        }

        _logger.LogDebug("Generating diff: original={OrigLen}chars, suggested={SugLen}chars",
            original.Length, suggested.Length);

        try
        {
            // LOGIC: Use DiffPlex for accurate inline diff computation
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(original, suggested);

            // LOGIC: Transform DiffPlex result to our DiffOperation sequence
            var operations = BuildOperations(diff);

            // LOGIC: Generate unified diff format for text display
            var unifiedDiff = GenerateUnifiedDiff(original, suggested, operations);

            // LOGIC: Generate HTML diff for UI rendering
            var htmlDiff = GenerateHtmlDiff(operations);

            _logger.LogDebug("Diff generated: {Additions} additions, {Deletions} deletions, {Unchanged} unchanged",
                operations.Count(o => o.Type == DiffType.Addition),
                operations.Count(o => o.Type == DiffType.Deletion),
                operations.Count(o => o.Type == DiffType.Unchanged));

            return new TextDiff
            {
                Operations = operations,
                UnifiedDiff = unifiedDiff,
                HtmlDiff = htmlDiff
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate diff");

            // LOGIC: Return empty diff on error rather than propagating exception
            return TextDiff.Empty;
        }
    }

    /// <summary>
    /// Builds a list of <see cref="DiffOperation"/> from DiffPlex's diff model.
    /// </summary>
    /// <param name="diff">The DiffPlex diff model.</param>
    /// <returns>An ordered list of diff operations.</returns>
    private static IReadOnlyList<DiffOperation> BuildOperations(DiffPaneModel diff)
    {
        var operations = new List<DiffOperation>();
        var position = 0;

        foreach (var line in diff.Lines)
        {
            // LOGIC: Map DiffPlex ChangeType to our DiffType enum
            var type = line.Type switch
            {
                ChangeType.Inserted => DiffType.Addition,
                ChangeType.Deleted => DiffType.Deletion,
                ChangeType.Modified => DiffType.Addition, // Modified treated as add for inline diff
                ChangeType.Imaginary => DiffType.Unchanged, // Padding lines
                _ => DiffType.Unchanged
            };

            var text = line.Text ?? string.Empty;

            operations.Add(new DiffOperation(
                type,
                text,
                position,
                text.Length));

            // LOGIC: Only advance position for non-deleted content
            if (type != DiffType.Deletion)
            {
                position += text.Length;
            }
        }

        return operations;
    }

    /// <summary>
    /// Generates a unified diff format string.
    /// </summary>
    /// <param name="original">The original text.</param>
    /// <param name="suggested">The suggested text.</param>
    /// <param name="operations">The diff operations.</param>
    /// <returns>A unified diff format string.</returns>
    private static string GenerateUnifiedDiff(
        string original,
        string suggested,
        IReadOnlyList<DiffOperation> operations)
    {
        var sb = new StringBuilder();

        // LOGIC: Standard unified diff header
        sb.AppendLine("--- Original");
        sb.AppendLine("+++ Suggested");
        sb.AppendLine("@@ @@");

        foreach (var op in operations)
        {
            var prefix = op.Type switch
            {
                DiffType.Addition => "+ ",
                DiffType.Deletion => "- ",
                _ => "  "
            };

            // LOGIC: Handle multi-line text properly
            var lines = op.Text.Split('\n');
            foreach (var line in lines)
            {
                sb.AppendLine($"{prefix}{line}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an HTML diff with CSS classes for styling.
    /// </summary>
    /// <param name="operations">The diff operations.</param>
    /// <returns>An HTML string with span elements and CSS classes.</returns>
    private static string GenerateHtmlDiff(IReadOnlyList<DiffOperation> operations)
    {
        var sb = new StringBuilder();

        foreach (var op in operations)
        {
            // LOGIC: Map operation type to CSS class
            var cssClass = op.Type switch
            {
                DiffType.Addition => "diff-add",
                DiffType.Deletion => "diff-del",
                _ => "diff-unchanged"
            };

            // LOGIC: HTML-encode text content for safety
            var encodedText = WebUtility.HtmlEncode(op.Text);

            sb.Append($"<span class=\"{cssClass}\">{encodedText}</span>");
        }

        return sb.ToString();
    }
}
