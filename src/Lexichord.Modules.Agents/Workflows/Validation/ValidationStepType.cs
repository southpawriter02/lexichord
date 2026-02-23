// -----------------------------------------------------------------------
// <copyright file="ValidationStepType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Enumerates the categories of validation steps available in the
//   validation workflow pipeline. Each type maps to a specific class of
//   validation logic (schema compliance, cross-reference integrity, etc.).
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Types of validation steps available in the validation workflow pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Each step type corresponds to a category of validation logic that can be
/// executed within a <see cref="IValidationWorkflowStep"/>. The workflow engine
/// uses this to route validation requests to the appropriate service.
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>WriterPro: <see cref="Schema"/> and <see cref="Grammar"/> only</description></item>
///   <item><description>Teams: All step types including <see cref="Custom"/></description></item>
///   <item><description>Enterprise: Full access with unlimited rules</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public enum ValidationStepType
{
    /// <summary>
    /// Schema validation (JSON Schema, XML Schema, document structure, etc.).
    /// </summary>
    /// <remarks>
    /// Validates that a document conforms to a defined structural schema.
    /// Available at WriterPro tier and above.
    /// </remarks>
    Schema = 0,

    /// <summary>
    /// Cross-reference validation (links, citations, internal references, etc.).
    /// </summary>
    /// <remarks>
    /// Validates that all cross-references within a document resolve correctly,
    /// including internal links, citations, and external references.
    /// Available at Teams tier and above.
    /// </remarks>
    CrossReference = 1,

    /// <summary>
    /// Content consistency validation (duplicate terms, contradictions, etc.).
    /// </summary>
    /// <remarks>
    /// Validates that the document content is internally consistent,
    /// checking for duplicate definitions, contradictory statements, and
    /// terminology inconsistencies.
    /// Available at Teams tier and above.
    /// </remarks>
    Consistency = 2,

    /// <summary>
    /// Custom validation rule execution.
    /// </summary>
    /// <remarks>
    /// Executes user-defined or plugin-provided validation rules.
    /// Available at Teams tier and above.
    /// </remarks>
    Custom = 3,

    /// <summary>
    /// Spell and grammar checking.
    /// </summary>
    /// <remarks>
    /// Validates spelling, grammar, and language conventions.
    /// Available at WriterPro tier and above.
    /// </remarks>
    Grammar = 4,

    /// <summary>
    /// Knowledge graph alignment validation.
    /// </summary>
    /// <remarks>
    /// Validates that document content aligns with the knowledge graph,
    /// checking entity references, relationship accuracy, and ontology compliance.
    /// Available at Teams tier and above.
    /// </remarks>
    KnowledgeGraphAlignment = 5,

    /// <summary>
    /// Metadata validation.
    /// </summary>
    /// <remarks>
    /// Validates document metadata fields including frontmatter, tags,
    /// categories, and required metadata completeness.
    /// Available at Teams tier and above.
    /// </remarks>
    Metadata = 6
}
