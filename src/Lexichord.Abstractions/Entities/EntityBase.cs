using Dapper.Contrib.Extensions;

namespace Lexichord.Abstractions.Entities;

/// <summary>
/// Base class for entities with standard audit fields.
/// </summary>
/// <remarks>
/// LOGIC: All entities inherit these audit fields for tracking creation and modification times.
/// The [Computed] attribute tells Dapper.Contrib to not include these in INSERT/UPDATE as they
/// are managed by database defaults and triggers.
/// </remarks>
public abstract record EntityBase
{
    /// <summary>
    /// When the entity was created.
    /// </summary>
    /// <remarks>Set by database default, never modified by application code.</remarks>
    [Computed]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the entity was last updated.
    /// </summary>
    /// <remarks>Updated by database trigger on modification.</remarks>
    [Computed]
    public DateTimeOffset UpdatedAt { get; init; }
}
