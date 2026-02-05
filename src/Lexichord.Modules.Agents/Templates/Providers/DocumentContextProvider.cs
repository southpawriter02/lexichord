// -----------------------------------------------------------------------
// <copyright file="DocumentContextProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Templates.Providers;

/// <summary>
/// Context provider that supplies document-related variables from the current editor state.
/// </summary>
/// <remarks>
/// <para>
/// This provider extracts metadata about the current document being edited and any
/// selected text. The variables produced are foundational context used by most prompts.
/// </para>
/// <para>
/// <strong>Variables Produced:</strong>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Variable</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term><c>document_path</c></term>
///     <description>Full file path of the current document.</description>
///   </item>
///   <item>
///     <term><c>document_name</c></term>
///     <description>Filename without directory path.</description>
///   </item>
///   <item>
///     <term><c>document_extension</c></term>
///     <description>File extension including the dot (e.g., ".md").</description>
///   </item>
///   <item>
///     <term><c>cursor_position</c></term>
///     <description>Current cursor offset in the document.</description>
///   </item>
///   <item>
///     <term><c>selected_text</c></term>
///     <description>Currently selected text, if any.</description>
///   </item>
///   <item>
///     <term><c>selection_length</c></term>
///     <description>Character count of selected text.</description>
///   </item>
///   <item>
///     <term><c>selection_word_count</c></term>
///     <description>Approximate word count of selected text.</description>
///   </item>
/// </list>
/// <para>
/// <strong>License Requirements:</strong> None. Document context is available to all tiers.
/// </para>
/// <para>
/// <strong>Priority:</strong> 50 (lowest - foundational data that other providers may reference).
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new DocumentContextProvider(logger);
/// var request = ContextRequest.Full("/path/to/document.md", "selected content here");
///
/// if (provider.IsEnabled(request))
/// {
///     var result = await provider.GetContextAsync(request, CancellationToken.None);
///     if (result.Success)
///     {
///         // result.Data contains: document_path, document_name, document_extension,
///         // selected_text, selection_length, selection_word_count
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IContextProvider"/>
/// <seealso cref="ContextResult"/>
public sealed class DocumentContextProvider : IContextProvider
{
    private readonly ILogger<DocumentContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentContextProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger for provider diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public DocumentContextProvider(ILogger<DocumentContextProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("DocumentContextProvider initialized");
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: This provider is identified as "Document" in logs and result tracking.
    /// </remarks>
    public string ProviderName => "Document";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Priority 50 is the lowest, meaning document context is processed first
    /// and may be overwritten by higher-priority providers if they produce the same keys.
    /// </remarks>
    public int Priority => 50;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Document context is available to all license tiers and does not require
    /// a specific feature code.
    /// </remarks>
    public string? RequiredLicenseFeature => null;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: This provider is enabled when there is document context to provide,
    /// specifically when at least one of these conditions is met:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A document path is specified.</description></item>
    ///   <item><description>Selected text is provided.</description></item>
    ///   <item><description>A cursor position is specified.</description></item>
    /// </list>
    /// </remarks>
    public bool IsEnabled(ContextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var isEnabled = request.HasDocumentContext ||
                        request.HasSelectedText ||
                        request.HasCursorPosition;

        _logger.LogDebug(
            "DocumentContextProvider.IsEnabled: {IsEnabled} (HasDocumentContext={HasDoc}, HasSelectedText={HasSel}, HasCursorPosition={HasCursor})",
            isEnabled,
            request.HasDocumentContext,
            request.HasSelectedText,
            request.HasCursorPosition);

        return isEnabled;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Extracts document metadata from the request and assembles a dictionary
    /// of context variables. All operations are synchronous and fast, so this method
    /// completes immediately.
    /// </para>
    /// <para>
    /// The method handles partial data gracefully - if only some fields are available,
    /// only those are included in the result.
    /// </para>
    /// </remarks>
    public Task<ContextResult> GetContextAsync(ContextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "DocumentContextProvider.GetContextAsync starting (Path={DocumentPath})",
            request.CurrentDocumentPath ?? "(none)");

        var data = new Dictionary<string, object>();

        // LOGIC: Extract document path information if available
        if (!string.IsNullOrEmpty(request.CurrentDocumentPath))
        {
            data["document_path"] = request.CurrentDocumentPath;
            data["document_name"] = Path.GetFileName(request.CurrentDocumentPath);
            data["document_extension"] = Path.GetExtension(request.CurrentDocumentPath);

            _logger.LogDebug(
                "Added document_path='{Path}', document_name='{Name}', document_extension='{Extension}'",
                data["document_path"],
                data["document_name"],
                data["document_extension"]);
        }

        // LOGIC: Include cursor position if specified
        if (request.CursorPosition.HasValue)
        {
            data["cursor_position"] = request.CursorPosition.Value;

            _logger.LogDebug("Added cursor_position={CursorPosition}", request.CursorPosition.Value);
        }

        // LOGIC: Include selected text and computed metrics if available
        if (!string.IsNullOrEmpty(request.SelectedText))
        {
            data["selected_text"] = request.SelectedText;
            data["selection_length"] = request.SelectedText.Length;
            data["selection_word_count"] = CountWords(request.SelectedText);

            _logger.LogDebug(
                "Added selected_text ({Length} chars, {WordCount} words)",
                data["selection_length"],
                data["selection_word_count"]);
        }

        sw.Stop();

        // LOGIC: Return empty result if no data was extracted
        if (data.Count == 0)
        {
            _logger.LogDebug("DocumentContextProvider produced no context (duration={Duration}ms)", sw.ElapsedMilliseconds);
            return Task.FromResult(ContextResult.Empty(ProviderName, sw.Elapsed));
        }

        _logger.LogInformation(
            "DocumentContextProvider produced {VariableCount} variables in {Duration}ms",
            data.Count,
            sw.ElapsedMilliseconds);

        return Task.FromResult(ContextResult.Ok(ProviderName, data, sw.Elapsed));
    }

    /// <summary>
    /// Counts the approximate number of words in the given text.
    /// </summary>
    /// <param name="text">The text to count words in.</param>
    /// <returns>The word count.</returns>
    /// <remarks>
    /// LOGIC: Uses a simple whitespace split approach. This is an approximation
    /// that works well for most prose content. Consecutive whitespace is handled
    /// by filtering empty entries.
    /// </remarks>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
