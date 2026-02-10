// -----------------------------------------------------------------------
// <copyright file="ContextAwarePromptSelector.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Models;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Selects appropriate prompts based on document context.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="IDocumentContextAnalyzer"/> to determine the structural
/// context at the cursor position, then maps the detected content type to
/// an appropriate prompt template from <see cref="IPromptTemplateRepository"/>.
/// Also provides context-specific prompt suggestions for UI display.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
/// <seealso cref="IDocumentContextAnalyzer"/>
/// <seealso cref="IPromptTemplateRepository"/>
/// <seealso cref="PromptSuggestion"/>
internal class ContextAwarePromptSelector
{
    // ─────────────────────────────────────────────────────────────────────
    // Dependencies
    // ─────────────────────────────────────────────────────────────────────

    private readonly IDocumentContextAnalyzer _analyzer;
    private readonly IPromptTemplateRepository _templates;
    private readonly ILogger<ContextAwarePromptSelector> _logger;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextAwarePromptSelector"/> class.
    /// </summary>
    /// <param name="analyzer">
    /// The document context analyzer for structural analysis.
    /// </param>
    /// <param name="templates">
    /// The prompt template repository for template lookups.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public ContextAwarePromptSelector(
        IDocumentContextAnalyzer analyzer,
        IPromptTemplateRepository templates,
        ILogger<ContextAwarePromptSelector> logger)
    {
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ContextAwarePromptSelector initialized");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the most appropriate prompt template for the given editor context.
    /// </summary>
    /// <param name="editorContext">
    /// The current editor state including document path and cursor position.
    /// </param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// The matched <see cref="IPromptTemplate"/>, or <c>null</c> if no
    /// matching template is found in the repository.
    /// </returns>
    /// <remarks>
    /// LOGIC: Analyzes the document at the cursor position to determine
    /// content type, then maps to a template ID:
    /// <list type="bullet">
    ///   <item><see cref="ContentBlockType.CodeBlock"/> → "context-code-review"</item>
    ///   <item><see cref="ContentBlockType.Table"/> → "context-table-help"</item>
    ///   <item><see cref="ContentBlockType.List"/> → "context-list-expand"</item>
    ///   <item>All others → "context-general-improve"</item>
    /// </list>
    /// </remarks>
    public async Task<IPromptTemplate?> SelectPromptAsync(
        EditorContext editorContext,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Selecting prompt for document {Path} at position {Position}",
            editorContext.DocumentPath, editorContext.CursorPosition);

        // LOGIC: Analyze document structure at cursor position.
        var docContext = await _analyzer.AnalyzeAtPositionAsync(
            editorContext.DocumentPath,
            editorContext.CursorPosition,
            ct);

        // LOGIC: Map content type to template ID.
        var templateId = docContext.ContentType switch
        {
            ContentBlockType.CodeBlock => "context-code-review",
            ContentBlockType.Table => "context-table-help",
            ContentBlockType.List => "context-list-expand",
            _ => "context-general-improve"
        };

        _logger.LogDebug(
            "Selected prompt template {TemplateId} for content type {Type}",
            templateId, docContext.ContentType);

        return _templates.GetTemplate(templateId);
    }

    /// <summary>
    /// Gets a list of suggested prompts for the current context.
    /// </summary>
    /// <param name="editorContext">
    /// The current editor state including document path and cursor position.
    /// </param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// A read-only list of <see cref="PromptSuggestion"/> instances appropriate
    /// for the detected content type. Includes section-specific suggestions when
    /// the cursor is within a headed section.
    /// </returns>
    /// <remarks>
    /// LOGIC: Generates context-specific suggestions based on content type:
    /// <list type="bullet">
    ///   <item>Code blocks: "Review this code", "Add comments", "Optimize"</item>
    ///   <item>Tables: "Add column", "Sort by..."</item>
    ///   <item>Lists: "Add items", "Reorder"</item>
    ///   <item>Prose (default): "Improve writing", "Simplify", "Expand"</item>
    /// </list>
    /// Additionally appends a "Summarize [section]" suggestion when within
    /// a headed section.
    /// </remarks>
    public async Task<IReadOnlyList<PromptSuggestion>> GetSuggestionsAsync(
        EditorContext editorContext,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting suggestions for document {Path} at position {Position}",
            editorContext.DocumentPath, editorContext.CursorPosition);

        // LOGIC: Analyze document structure at cursor position.
        var docContext = await _analyzer.AnalyzeAtPositionAsync(
            editorContext.DocumentPath,
            editorContext.CursorPosition,
            ct);

        var suggestions = new List<PromptSuggestion>();

        // LOGIC: Add content-type-specific suggestions.
        switch (docContext.ContentType)
        {
            case ContentBlockType.CodeBlock:
                suggestions.Add(new PromptSuggestion("Review this code", "code-review"));
                suggestions.Add(new PromptSuggestion("Add comments", "code-comment"));
                suggestions.Add(new PromptSuggestion("Optimize", "code-optimize"));
                break;

            case ContentBlockType.Table:
                suggestions.Add(new PromptSuggestion("Add column", "table-add-column"));
                suggestions.Add(new PromptSuggestion("Sort by...", "table-sort"));
                break;

            case ContentBlockType.List:
                suggestions.Add(new PromptSuggestion("Add items", "list-expand"));
                suggestions.Add(new PromptSuggestion("Reorder", "list-reorder"));
                break;

            default:
                suggestions.Add(new PromptSuggestion("Improve writing", "prose-improve"));
                suggestions.Add(new PromptSuggestion("Simplify", "prose-simplify"));
                suggestions.Add(new PromptSuggestion("Expand", "prose-expand"));
                break;
        }

        // LOGIC: Add section-specific suggestions if cursor is within a headed section.
        if (docContext.HasSection)
        {
            suggestions.Add(new PromptSuggestion(
                $"Summarize {docContext.CurrentSection}",
                "section-summarize"));

            _logger.LogDebug(
                "Added section-specific suggestion for: {Section}",
                docContext.CurrentSection);
        }

        _logger.LogDebug(
            "Generated {Count} prompt suggestions for content type {Type}",
            suggestions.Count, docContext.ContentType);

        return suggestions;
    }
}
