// -----------------------------------------------------------------------
// <copyright file="WorkflowMetadata.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Metadata about a workflow definition.
/// </summary>
/// <remarks>
/// <para>
/// Contains authorship, versioning, categorization, and licensing information
/// for a workflow definition. Metadata is persisted alongside the workflow
/// and included in YAML exports.
/// </para>
/// <para>
/// The <see cref="RequiredTier"/> field determines which license tier is needed
/// to create and edit workflows. Built-in workflows (<see cref="IsBuiltIn"/> = true)
/// may be available at lower tiers for execution only.
/// </para>
/// </remarks>
/// <param name="Author">Author identifier (email or username) who created the workflow.</param>
/// <param name="CreatedAt">UTC timestamp when the workflow was first created.</param>
/// <param name="ModifiedAt">UTC timestamp of the most recent modification.</param>
/// <param name="Version">Optional semantic version string for the workflow definition.</param>
/// <param name="Tags">Tags for categorization and search. May be empty but never null.</param>
/// <param name="Category">Organizational category for the workflow.</param>
/// <param name="IsBuiltIn">Whether this is a system-provided workflow (not user-created).</param>
/// <param name="RequiredTier">Minimum <see cref="LicenseTier"/> required to create/edit this workflow.</param>
public record WorkflowMetadata(
    string Author,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    string? Version,
    IReadOnlyList<string> Tags,
    WorkflowCategory Category,
    bool IsBuiltIn,
    LicenseTier RequiredTier
);

/// <summary>
/// Categories for organizing workflows.
/// </summary>
/// <remarks>
/// Workflows are grouped by category in the designer's workflow list
/// to help users find and manage their workflow definitions.
/// </remarks>
public enum WorkflowCategory
{
    /// <summary>General-purpose workflows not fitting other categories.</summary>
    General,

    /// <summary>Technical documentation and code review workflows.</summary>
    Technical,

    /// <summary>Marketing and copywriting workflows.</summary>
    Marketing,

    /// <summary>Academic and research writing workflows.</summary>
    Academic,

    /// <summary>Legal document review workflows.</summary>
    Legal,

    /// <summary>User-defined category for custom workflows.</summary>
    Custom
}
