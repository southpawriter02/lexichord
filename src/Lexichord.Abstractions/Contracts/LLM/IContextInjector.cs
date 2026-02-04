// -----------------------------------------------------------------------
// <copyright file="IContextInjector.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Assembles context from multiple sources for template variable injection.
/// </summary>
/// <remarks>
/// <para>
/// The context injector gathers information from various sources and assembles
/// them into a dictionary suitable for template variable substitution:
/// </para>
/// <list type="bullet">
///   <item><description><b>Style rules</b>: Active style guide rules for writing assistance</description></item>
///   <item><description><b>RAG context</b>: Semantically relevant chunks from the knowledge base</description></item>
///   <item><description><b>Document context</b>: Current document path and cursor position</description></item>
///   <item><description><b>Selected text</b>: User's text selection from the editor</description></item>
/// </list>
/// <para>
/// The assembled context can be merged with user-provided variables before
/// template rendering. This separation allows context assembly to be async
/// (for I/O operations like RAG queries) while rendering remains synchronous.
/// </para>
/// <para>
/// Implementation is provided in v0.6.3d; this interface is defined in Abstractions
/// to allow module-level dependency injection without circular references.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a full context request
/// var request = ContextRequest.Full(
///     documentPath: "/path/to/document.md",
///     selectedText: "The quick brown fox jumps over the lazy dog."
/// );
///
/// // Assemble context from all sources
/// var context = await contextInjector.AssembleContextAsync(request);
///
/// // Context now contains:
/// // - "document_path": "/path/to/document.md"
/// // - "selected_text": "The quick brown fox..."
/// // - "style_rules": "• Use active voice\n• Avoid jargon" (if enabled)
/// // - "context": "[doc1.md]\nRelevant content..." (if RAG enabled)
///
/// // Merge with user-provided variables
/// context["user_input"] = "Please review this text for clarity.";
///
/// // Render template with assembled context
/// var messages = renderer.RenderMessages(template, context);
/// </code>
/// </example>
/// <seealso cref="ContextRequest"/>
/// <seealso cref="IPromptRenderer"/>
public interface IContextInjector
{
    /// <summary>
    /// Assembles context from configured sources.
    /// </summary>
    /// <param name="request">
    /// Specifies which context sources to include and provides input data.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for async operations such as RAG queries.
    /// </param>
    /// <returns>
    /// A task that resolves to a dictionary of variable names to context values.
    /// The dictionary can be passed directly to <see cref="IPromptRenderer.RenderMessages"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via <paramref name="ct"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The returned dictionary contains standard variable names based on the request:
    /// </para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Variable</term>
    ///     <description>Source</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>style_rules</c></term>
    ///     <description>Formatted style rules string (when <see cref="ContextRequest.IncludeStyleRules"/> is true)</description>
    ///   </item>
    ///   <item>
    ///     <term><c>context</c></term>
    ///     <description>RAG context chunks as formatted string (when <see cref="ContextRequest.IncludeRAGContext"/> is true)</description>
    ///   </item>
    ///   <item>
    ///     <term><c>document_path</c></term>
    ///     <description>Current document path (when <see cref="ContextRequest.CurrentDocumentPath"/> is not null)</description>
    ///   </item>
    ///   <item>
    ///     <term><c>selected_text</c></term>
    ///     <description>User's text selection (when <see cref="ContextRequest.SelectedText"/> is not null)</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Missing or unavailable sources simply omit the corresponding key from the dictionary
    /// rather than including null or empty values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // RAG-only context for knowledge retrieval
    /// var request = ContextRequest.RAGOnly("error handling best practices", maxChunks: 5);
    /// var context = await contextInjector.AssembleContextAsync(request, cancellationToken);
    ///
    /// // context["context"] contains semantically relevant chunks:
    /// // "[error-handling.md]
    /// //  Always catch specific exceptions rather than base Exception..."
    ///
    /// // Style-only context for writing assistance
    /// var styleRequest = ContextRequest.StyleOnly("/project/docs/readme.md");
    /// var styleContext = await contextInjector.AssembleContextAsync(styleRequest, cancellationToken);
    ///
    /// // styleContext["style_rules"] contains:
    /// // "• Use active voice
    /// //  • Keep sentences under 25 words
    /// //  • Avoid technical jargon"
    /// </code>
    /// </example>
    Task<IDictionary<string, object>> AssembleContextAsync(
        ContextRequest request,
        CancellationToken ct = default);
}
