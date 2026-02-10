// -----------------------------------------------------------------------
// <copyright file="BuiltInQuickActions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Models;

namespace Lexichord.Modules.Agents;

/// <summary>
/// Defines the built-in quick actions and their prompt templates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a static registry of pre-defined quick actions that are loaded
/// into <see cref="Services.IQuickActionsService"/> at construction time.
/// Actions are organized into three categories:
/// </para>
/// <list type="bullet">
///   <item><description><b>Prose Actions:</b> Improve, Simplify, Expand, Summarize, Fix — available for prose content</description></item>
///   <item><description><b>Code Actions:</b> Explain, Comment — available for code blocks</description></item>
///   <item><description><b>Table Actions:</b> Add Row — available for table content</description></item>
/// </list>
/// <para>
/// Each action references a prompt template ID that maps to a
/// <see cref="PromptTemplateDefinition"/> in the <see cref="PromptTemplates"/> dictionary.
/// The <see cref="Services.QuickActionsService"/> resolves templates from the
/// <see cref="IPromptTemplateRepository"/> first, falling back to these built-in
/// definitions if no repository template is found.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
public static class BuiltInQuickActions
{
    /// <summary>
    /// Gets all built-in quick action definitions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Actions are ordered by their <see cref="QuickAction.Order"/> value:
    /// <list type="bullet">
    ///   <item><description>0–4: Prose actions (Improve, Simplify, Expand, Summarize, Fix)</description></item>
    ///   <item><description>10–11: Code actions (Explain, Comment)</description></item>
    ///   <item><description>20: Table actions (Add Row)</description></item>
    /// </list>
    /// </remarks>
    public static IReadOnlyList<QuickAction> All => new[]
    {
        // ═══════════════════════════════════════════════════════════════
        // Prose Actions
        // ═══════════════════════════════════════════════════════════════
        new QuickAction(
            ActionId: "improve",
            Name: "Improve",
            Description: "Enhance writing quality and clarity",
            Icon: "IconSparkle",
            PromptTemplateId: "quick-improve",
            KeyboardShortcut: "1",
            RequiresSelection: true,
            SupportedContentTypes: new[] { ContentBlockType.Prose },
            Order: 0),

        new QuickAction(
            ActionId: "simplify",
            Name: "Simplify",
            Description: "Make text clearer and more concise",
            Icon: "IconMinimize",
            PromptTemplateId: "quick-simplify",
            KeyboardShortcut: "2",
            RequiresSelection: true,
            SupportedContentTypes: new[] { ContentBlockType.Prose },
            Order: 1),

        new QuickAction(
            ActionId: "expand",
            Name: "Expand",
            Description: "Add more detail and depth",
            Icon: "IconExpand",
            PromptTemplateId: "quick-expand",
            KeyboardShortcut: "3",
            RequiresSelection: true,
            SupportedContentTypes: new[] { ContentBlockType.Prose },
            Order: 2),

        new QuickAction(
            ActionId: "summarize",
            Name: "Summarize",
            Description: "Create a brief summary",
            Icon: "IconSummary",
            PromptTemplateId: "quick-summarize",
            KeyboardShortcut: "4",
            RequiresSelection: true,
            Order: 3),

        new QuickAction(
            ActionId: "fix",
            Name: "Fix",
            Description: "Correct grammar and spelling",
            Icon: "IconCheck",
            PromptTemplateId: "quick-fix",
            KeyboardShortcut: "5",
            RequiresSelection: true,
            SupportedContentTypes: new[] { ContentBlockType.Prose },
            Order: 4),

        // ═══════════════════════════════════════════════════════════════
        // Code Actions
        // ═══════════════════════════════════════════════════════════════
        new QuickAction(
            ActionId: "explain-code",
            Name: "Explain",
            Description: "Explain what this code does",
            Icon: "IconQuestion",
            PromptTemplateId: "quick-explain-code",
            KeyboardShortcut: "6",
            RequiresSelection: true,
            SupportedContentTypes: new[] { ContentBlockType.CodeBlock },
            Order: 10),

        new QuickAction(
            ActionId: "add-comments",
            Name: "Comment",
            Description: "Add inline comments to code",
            Icon: "IconComment",
            PromptTemplateId: "quick-add-comments",
            KeyboardShortcut: "7",
            RequiresSelection: true,
            SupportedContentTypes: new[] { ContentBlockType.CodeBlock },
            Order: 11),

        // ═══════════════════════════════════════════════════════════════
        // Table Actions
        // ═══════════════════════════════════════════════════════════════
        new QuickAction(
            ActionId: "add-row",
            Name: "Add Row",
            Description: "Add a new row to the table",
            Icon: "IconPlus",
            PromptTemplateId: "quick-table-add-row",
            RequiresSelection: false,
            SupportedContentTypes: new[] { ContentBlockType.Table },
            Order: 20)
    };

    /// <summary>
    /// Gets the prompt templates for built-in actions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Each template maps to a built-in action's <see cref="QuickAction.PromptTemplateId"/>.
    /// Templates are used as fallbacks when the <see cref="IPromptTemplateRepository"/>
    /// does not contain a matching template ID. This allows users to override
    /// built-in templates by placing custom YAML templates with the same ID
    /// in their workspace.
    /// </para>
    /// <para>
    /// All templates use the <c>{{text}}</c> Mustache variable placeholder
    /// for the input text.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, PromptTemplateDefinition> PromptTemplates =>
        new Dictionary<string, PromptTemplateDefinition>
        {
            ["quick-improve"] = new(
                TemplateId: "quick-improve",
                Name: "Quick Improve",
                SystemPrompt: """
                    You are a writing improvement assistant. Enhance the provided text
                    by improving clarity, flow, and word choice while maintaining the
                    original meaning and voice. Return ONLY the improved text.
                    """,
                UserPromptTemplate: "Improve this text:\n\n{{text}}"),

            ["quick-simplify"] = new(
                TemplateId: "quick-simplify",
                Name: "Quick Simplify",
                SystemPrompt: """
                    You are a simplification assistant. Make the text clearer and more
                    concise while preserving essential meaning. Use simpler words and
                    shorter sentences. Return ONLY the simplified text.
                    """,
                UserPromptTemplate: "Simplify this text:\n\n{{text}}"),

            ["quick-expand"] = new(
                TemplateId: "quick-expand",
                Name: "Quick Expand",
                SystemPrompt: """
                    You are an expansion assistant. Add more detail, examples, and depth
                    to the provided text while maintaining its original purpose and style.
                    Return ONLY the expanded text.
                    """,
                UserPromptTemplate: "Expand this text with more detail:\n\n{{text}}"),

            ["quick-summarize"] = new(
                TemplateId: "quick-summarize",
                Name: "Quick Summarize",
                SystemPrompt: """
                    You are a summarization assistant. Create a brief, accurate summary
                    of the provided text that captures the main points. Return ONLY the
                    summary.
                    """,
                UserPromptTemplate: "Summarize this text:\n\n{{text}}"),

            ["quick-fix"] = new(
                TemplateId: "quick-fix",
                Name: "Quick Fix",
                SystemPrompt: """
                    You are a proofreading assistant. Correct any grammar, spelling,
                    and punctuation errors in the text. Make minimal changes beyond
                    corrections. Return ONLY the corrected text.
                    """,
                UserPromptTemplate: "Fix errors in this text:\n\n{{text}}"),

            ["quick-explain-code"] = new(
                TemplateId: "quick-explain-code",
                Name: "Quick Explain Code",
                SystemPrompt: """
                    You are a code explanation assistant. Provide a clear, concise
                    explanation of what the code does. Return ONLY the explanation.
                    """,
                UserPromptTemplate: "Explain this code:\n\n```\n{{text}}\n```"),

            ["quick-add-comments"] = new(
                TemplateId: "quick-add-comments",
                Name: "Quick Add Comments",
                SystemPrompt: """
                    You are a code documentation assistant. Add helpful inline comments
                    to explain the code. Preserve the original code structure.
                    Return ONLY the commented code.
                    """,
                UserPromptTemplate: "Add comments to this code:\n\n```\n{{text}}\n```"),

            ["quick-table-add-row"] = new(
                TemplateId: "quick-table-add-row",
                Name: "Quick Table Add Row",
                SystemPrompt: """
                    You are a table editing assistant. Add a new row to the markdown
                    table with appropriate placeholder or inferred values based on
                    existing data. Return ONLY the complete table.
                    """,
                UserPromptTemplate: "Add a new row to this table:\n\n{{text}}")
        };

    /// <summary>
    /// Adapts a <see cref="PromptTemplateDefinition"/> to the <see cref="IPromptTemplate"/> interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: This adapter allows built-in quick action templates to be used
    /// interchangeably with repository-loaded templates. It wraps the lightweight
    /// <see cref="PromptTemplateDefinition"/> record and exposes it through the
    /// full <see cref="IPromptTemplate"/> interface.
    /// </para>
    /// <para>
    /// All built-in quick action templates require a single <c>text</c> variable
    /// and have no optional variables.
    /// </para>
    /// </remarks>
    internal class QuickActionPromptTemplateAdapter : IPromptTemplate
    {
        private readonly PromptTemplateDefinition _definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickActionPromptTemplateAdapter"/> class.
        /// </summary>
        /// <param name="definition">The prompt template definition to adapt.</param>
        public QuickActionPromptTemplateAdapter(PromptTemplateDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <inheritdoc/>
        public string TemplateId => _definition.TemplateId;

        /// <inheritdoc/>
        public string Name => _definition.Name;

        /// <inheritdoc/>
        public string Description => _definition.Name;

        /// <inheritdoc/>
        public string SystemPromptTemplate => _definition.SystemPrompt;

        /// <inheritdoc/>
        public string UserPromptTemplate => _definition.UserPromptTemplate;

        /// <inheritdoc/>
        public IReadOnlyList<string> RequiredVariables => ["text"];

        /// <inheritdoc/>
        public IReadOnlyList<string> OptionalVariables => [];
    }
}
