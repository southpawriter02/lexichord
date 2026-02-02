// =============================================================================
// File: EntityCrudResults.cs
// Project: Lexichord.Abstractions
// Description: Result records for entity CRUD operations.
// =============================================================================
// LOGIC: Defines immutable result records that communicate operation outcomes,
//   including success/failure state, affected entities, and any errors/warnings.
//
// Results:
//   - EntityOperationResult: Standard result for create/update/delete
//   - MergeOperationResult: Extended result for merge with removed entity IDs
//   - EntityValidationResult: Validation-specific result with error details
//   - EntityValidationError: Individual validation error with context
//
// v0.4.7g: Entity CRUD Operations
// Dependencies: KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Result of an entity CRUD operation (create, update, or delete).
/// </summary>
/// <remarks>
/// <para>
/// Provides a standardized result pattern for entity operations with
/// access to the affected entity, success state, and any errors or warnings.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record EntityOperationResult
{
    /// <summary>
    /// Gets the entity affected by the operation, or null if the operation failed.
    /// </summary>
    public KnowledgeEntity? Entity { get; init; }

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets any error messages if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets any warning messages (operation may still succeed with warnings).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful result with the affected entity.
    /// </summary>
    public static EntityOperationResult Succeeded(KnowledgeEntity entity) =>
        new() { Entity = entity, Success = true };

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static EntityOperationResult Failed(string error) =>
        new() { Success = false, Errors = new[] { error } };

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    public static EntityOperationResult Failed(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };

    /// <summary>
    /// Creates a validation failure result from validation errors.
    /// </summary>
    public static EntityOperationResult ValidationFailed(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };
}

/// <summary>
/// Result of an entity merge operation.
/// </summary>
/// <remarks>
/// <para>
/// Extends the standard operation result with information about
/// the entities that were merged and subsequently removed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record MergeOperationResult
{
    /// <summary>
    /// Gets the merged target entity with combined properties.
    /// </summary>
    public KnowledgeEntity? MergedEntity { get; init; }

    /// <summary>
    /// Gets the IDs of source entities that were removed during merge.
    /// </summary>
    public IReadOnlyList<Guid> RemovedEntityIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets whether the merge operation completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets any error messages if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful merge result.
    /// </summary>
    public static MergeOperationResult Succeeded(KnowledgeEntity merged, IReadOnlyList<Guid> removed) =>
        new() { MergedEntity = merged, RemovedEntityIds = removed, Success = true };

    /// <summary>
    /// Creates a failed merge result with the specified error.
    /// </summary>
    public static MergeOperationResult Failed(string error) =>
        new() { Success = false, Errors = new[] { error } };
}

/// <summary>
/// Result of entity validation against schema and axioms.
/// </summary>
/// <remarks>
/// <para>
/// Provides detailed validation feedback including individual errors
/// and warnings with property-level context.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record EntityValidationResult
{
    /// <summary>
    /// Gets whether the entity passes all validation rules.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation errors that caused failure.
    /// </summary>
    public IReadOnlyList<EntityValidationError> Errors { get; init; } = Array.Empty<EntityValidationError>();

    /// <summary>
    /// Gets validation warnings that don't prevent the operation.
    /// </summary>
    public IReadOnlyList<EntityValidationError> Warnings { get; init; } = Array.Empty<EntityValidationError>();

    /// <summary>
    /// Creates a valid result with no errors.
    /// </summary>
    public static EntityValidationResult Valid() =>
        new() { IsValid = true };

    /// <summary>
    /// Creates an invalid result with the specified errors.
    /// </summary>
    public static EntityValidationResult Invalid(IEnumerable<EntityValidationError> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };

    /// <summary>
    /// Creates a valid result with warnings.
    /// </summary>
    public static EntityValidationResult ValidWithWarnings(IEnumerable<EntityValidationError> warnings) =>
        new() { IsValid = true, Warnings = warnings.ToList() };
}

/// <summary>
/// Individual validation error with context information.
/// </summary>
/// <remarks>
/// <para>
/// Provides detailed error information including the affected property,
/// error code for programmatic handling, and user-friendly message.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record EntityValidationError
{
    /// <summary>
    /// Gets the name of the property that failed validation, if applicable.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Gets the error code for programmatic error handling.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the user-friendly error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the severity level of the validation issue.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

/// <summary>
/// Severity level for validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message, does not affect validation.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning that should be reviewed but doesn't block the operation.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error that prevents the operation from completing.
    /// </summary>
    Error = 2
}
