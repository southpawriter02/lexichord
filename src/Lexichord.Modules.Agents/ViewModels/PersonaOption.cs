// -----------------------------------------------------------------------
// <copyright file="PersonaOption.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// Persona option for step configuration in the workflow designer.
/// </summary>
/// <remarks>
/// Displayed in the persona dropdown of the step configuration panel.
/// Each option represents an available persona variant for the selected agent.
/// </remarks>
/// <param name="PersonaId">Unique persona identifier.</param>
/// <param name="DisplayName">Human-readable name shown in the dropdown.</param>
/// <param name="Description">Optional description of the persona's behavior.</param>
public record PersonaOption(
    string PersonaId,
    string DisplayName,
    string? Description
);
