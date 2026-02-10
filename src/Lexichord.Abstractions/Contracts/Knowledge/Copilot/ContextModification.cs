// =============================================================================
// File: ContextModification.cs
// Project: Lexichord.Abstractions
// Description: Suggested modification to knowledge context from validation.
// =============================================================================
// LOGIC: When the pre-generation validator detects issues, it may suggest
//   specific modifications to the context that would resolve them. These
//   modifications are advisory — they are not applied automatically but
//   returned to the caller for optional action.
//
// v0.6.6f: Pre-Generation Validator (CKVS Phase 3b)
// Dependencies: KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// A suggested modification to the knowledge context.
/// </summary>
/// <remarks>
/// <para>
/// Context modifications are advisory suggestions produced by the
/// <see cref="IPreGenerationValidator"/> when it detects issues that
/// could be resolved by changing the context. They are returned in
/// <see cref="PreValidationResult.SuggestedModifications"/> for the
/// caller to optionally apply.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6f as part of the Pre-Generation Validator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var modification = new ContextModification
/// {
///     Type = ContextModificationType.RemoveEntity,
///     Description = "Remove duplicate entity 'UserService'",
///     EntityIdToRemove = duplicateEntity.Id
/// };
/// </code>
/// </example>
public record ContextModification
{
    /// <summary>
    /// The type of modification suggested.
    /// </summary>
    public ContextModificationType Type { get; init; }

    /// <summary>
    /// Human-readable description of the suggested modification.
    /// </summary>
    /// <value>Defaults to an empty string.</value>
    public string Description { get; init; } = "";

    /// <summary>
    /// The entity to add to the context, if applicable.
    /// </summary>
    /// <value>
    /// Non-null when <see cref="Type"/> is
    /// <see cref="ContextModificationType.AddEntity"/>.
    /// </value>
    public KnowledgeEntity? EntityToAdd { get; init; }

    /// <summary>
    /// The ID of the entity to remove from the context, if applicable.
    /// </summary>
    /// <value>
    /// Non-null when <see cref="Type"/> is
    /// <see cref="ContextModificationType.RemoveEntity"/>.
    /// </value>
    public Guid? EntityIdToRemove { get; init; }
}
