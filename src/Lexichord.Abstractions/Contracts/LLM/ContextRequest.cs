// -----------------------------------------------------------------------
// <copyright file="ContextRequest.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Request for context assembly before prompt rendering.
/// Specifies what context sources to include in the assembled context.
/// </summary>
/// <remarks>
/// <para>
/// Context requests configure how <see cref="IContextInjector"/> assembles context
/// from multiple sources including:
/// </para>
/// <list type="bullet">
///   <item><description>Document context (current file path, cursor position)</description></item>
///   <item><description>Selected text from the editor</description></item>
///   <item><description>Style rules from the active style guide</description></item>
///   <item><description>RAG (Retrieval-Augmented Generation) context from semantic search</description></item>
/// </list>
/// <para>
/// Use the static factory methods to create common request configurations,
/// or construct directly for full control over all options.
/// </para>
/// </remarks>
/// <param name="CurrentDocumentPath">Path to the active document, if any.</param>
/// <param name="CursorPosition">Cursor offset in the document, if relevant.</param>
/// <param name="SelectedText">Currently selected text for context.</param>
/// <param name="IncludeStyleRules">Whether to inject active style rules into context.</param>
/// <param name="IncludeRAGContext">Whether to query semantic search for relevant context.</param>
/// <param name="MaxRAGChunks">Maximum number of RAG chunks to include (default: 3).</param>
/// <example>
/// <code>
/// // Simple user input context
/// var request = ContextRequest.ForUserInput("What is dependency injection?");
///
/// // Full context with all sources enabled
/// var fullRequest = ContextRequest.Full("/path/to/document.md", "selected text here");
///
/// // Style-only context for style guide enforcement
/// var styleRequest = ContextRequest.StyleOnly("/path/to/document.md");
///
/// // RAG-only context for knowledge retrieval
/// var ragRequest = ContextRequest.RAGOnly("database connection patterns", maxChunks: 5);
/// </code>
/// </example>
public record ContextRequest(
    string? CurrentDocumentPath,
    int? CursorPosition,
    string? SelectedText,
    bool IncludeStyleRules,
    bool IncludeRAGContext,
    int MaxRAGChunks = 3)
{
    /// <summary>
    /// Gets the maximum number of RAG chunks to include.
    /// </summary>
    /// <value>A positive integer. Defaults to 3 if not specified or if a non-positive value is provided.</value>
    public int MaxRAGChunks { get; init; } = MaxRAGChunks > 0 ? MaxRAGChunks : 3;

    /// <summary>
    /// Creates a minimal context request with only user input.
    /// </summary>
    /// <param name="input">The user's input text, stored in <see cref="SelectedText"/>.</param>
    /// <returns>A new <see cref="ContextRequest"/> with style rules and RAG context disabled.</returns>
    /// <remarks>
    /// Use this factory method for simple prompts that don't require
    /// document context, style rules, or semantic search results.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ContextRequest.ForUserInput("Explain async/await in C#");
    /// var context = await injector.AssembleContextAsync(request);
    /// </code>
    /// </example>
    public static ContextRequest ForUserInput(string input)
        => new(null, null, input, false, false);

    /// <summary>
    /// Creates a full context request with all sources enabled.
    /// </summary>
    /// <param name="documentPath">Path to the current document for context.</param>
    /// <param name="selectedText">Currently selected text from the editor.</param>
    /// <returns>A new <see cref="ContextRequest"/> with style rules and RAG context enabled.</returns>
    /// <remarks>
    /// Use this factory method when you want the AI to have full awareness
    /// of the document context, style guidelines, and relevant knowledge.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ContextRequest.Full(
    ///     documentPath: "/project/docs/api.md",
    ///     selectedText: "The function returns a Task object.");
    /// var context = await injector.AssembleContextAsync(request);
    /// </code>
    /// </example>
    public static ContextRequest Full(
        string? documentPath,
        string? selectedText)
        => new(documentPath, null, selectedText, true, true);

    /// <summary>
    /// Creates a style-only context request.
    /// </summary>
    /// <param name="documentPath">Path to the current document for context.</param>
    /// <returns>A new <see cref="ContextRequest"/> with only style rules enabled.</returns>
    /// <remarks>
    /// Use this factory method when you want the AI to follow style guidelines
    /// but don't need RAG context (e.g., for simple editing tasks).
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ContextRequest.StyleOnly("/project/docs/readme.md");
    /// var context = await injector.AssembleContextAsync(request);
    /// // context["style_rules"] contains the active style rules
    /// </code>
    /// </example>
    public static ContextRequest StyleOnly(string? documentPath)
        => new(documentPath, null, null, true, false);

    /// <summary>
    /// Creates a RAG-only context request.
    /// </summary>
    /// <param name="query">The query text for semantic search.</param>
    /// <param name="maxChunks">Maximum number of chunks to retrieve (default: 3).</param>
    /// <returns>A new <see cref="ContextRequest"/> with only RAG context enabled.</returns>
    /// <remarks>
    /// Use this factory method for knowledge retrieval scenarios where
    /// you want relevant context from the knowledge base but don't need
    /// style rules or document context.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = ContextRequest.RAGOnly("error handling best practices", maxChunks: 5);
    /// var context = await injector.AssembleContextAsync(request);
    /// // context["context"] contains semantically relevant chunks
    /// </code>
    /// </example>
    public static ContextRequest RAGOnly(string query, int maxChunks = 3)
        => new(null, null, query, false, true, maxChunks);

    /// <summary>
    /// Gets a value indicating whether any context sources are enabled.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if either <see cref="IncludeStyleRules"/> or
    /// <see cref="IncludeRAGContext"/> is <see langword="true"/>; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// When <see langword="false"/>, the context injector may return a minimal
    /// context dictionary without external data sources.
    /// </remarks>
    public bool HasContextSources => IncludeStyleRules || IncludeRAGContext;

    /// <summary>
    /// Gets a value indicating whether this request has document context.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="CurrentDocumentPath"/> is not null;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasDocumentContext => CurrentDocumentPath is not null;

    /// <summary>
    /// Gets a value indicating whether this request has selected text.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="SelectedText"/> is not null or whitespace;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasSelectedText => !string.IsNullOrWhiteSpace(SelectedText);

    /// <summary>
    /// Gets a value indicating whether this request has cursor position information.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="CursorPosition"/> has a value;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasCursorPosition => CursorPosition.HasValue;
}
