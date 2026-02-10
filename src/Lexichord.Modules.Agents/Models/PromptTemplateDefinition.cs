// -----------------------------------------------------------------------
// <copyright file="PromptTemplateDefinition.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Models;

/// <summary>
/// Lightweight definition for a prompt template used by quick actions.
/// </summary>
/// <remarks>
/// <para>
/// This record provides a simple, self-contained prompt template definition
/// that can be used as a fallback when the full template repository does not
/// contain a matching template. Quick actions use this for their built-in
/// prompt definitions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.
/// </para>
/// </remarks>
/// <param name="TemplateId">Unique identifier for the template (e.g., "quick-improve").</param>
/// <param name="Name">Human-readable name for the template.</param>
/// <param name="SystemPrompt">The system prompt content (may contain Mustache variables).</param>
/// <param name="UserPromptTemplate">The user prompt template with <c>{{text}}</c> placeholder.</param>
public record PromptTemplateDefinition(
    string TemplateId,
    string Name,
    string SystemPrompt,
    string UserPromptTemplate);
