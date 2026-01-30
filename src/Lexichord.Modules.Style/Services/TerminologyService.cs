using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service implementation for terminology CRUD operations.
/// </summary>
/// <remarks>
/// LOGIC: Implements ITerminologyService with validation, event publishing, and logging.
///
/// Design decisions:
/// - Uses Result&lt;T&gt; for explicit success/failure handling
/// - Publishes LexiconChangedEvent on all mutations
/// - Event handler failures are logged but do not fail the main operation
/// - All operations are logged for debugging and auditing
///
/// Version: v0.2.2d
/// </remarks>
public sealed class TerminologyService : ITerminologyService
{
    private readonly ITerminologyRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<TerminologyService> _logger;

    /// <summary>
    /// Creates a new TerminologyService instance.
    /// </summary>
    /// <param name="repository">Terminology repository.</param>
    /// <param name="mediator">MediatR mediator for event publishing.</param>
    /// <param name="logger">Logger instance.</param>
    public TerminologyService(
        ITerminologyRepository repository,
        IMediator mediator,
        ILogger<TerminologyService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region CRUD Operations

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(CreateTermCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating term: {Term}", command.Term);

        // Validate pattern
        var validationResult = TermPatternValidator.Validate(command.Term);
        if (validationResult.IsFailure)
        {
            _logger.LogWarning("Pattern validation failed: {Error}", validationResult.Error);
            return Result<Guid>.Failure(validationResult.Error!);
        }

        // Create entity
        var term = new StyleTerm
        {
            Id = Guid.NewGuid(),
            Term = command.Term,
            Replacement = command.Replacement,
            Category = command.Category,
            Severity = command.Severity,
            Notes = command.Notes,
            StyleSheetId = command.StyleSheetId ?? Guid.Empty,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _repository.InsertAsync(term, cancellationToken);
            _logger.LogInformation("Created term {TermId}: {Pattern}", term.Id, command.Term);

            // Publish event (failure is logged but doesn't fail operation)
            await PublishEventSafelyAsync(
                new LexiconChangedEvent(LexiconChangeType.Created, term.Id, command.Term, command.Category),
                cancellationToken);

            return Result<Guid>.Success(term.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create term: {Term}", command.Term);
            return Result<Guid>.Failure($"Database error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateAsync(UpdateTermCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating term {TermId}: {Term}", command.Id, command.Term);

        // Check if term exists
        var existing = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (existing is null)
        {
            _logger.LogWarning("Term not found for update: {TermId}", command.Id);
            return Result<bool>.Failure("Term not found.");
        }

        // Validate pattern
        var validationResult = TermPatternValidator.Validate(command.Term);
        if (validationResult.IsFailure)
        {
            _logger.LogWarning("Pattern validation failed: {Error}", validationResult.Error);
            return Result<bool>.Failure(validationResult.Error!);
        }

        // Update entity
        var updated = existing with
        {
            Term = command.Term,
            Replacement = command.Replacement,
            Category = command.Category,
            Severity = command.Severity,
            Notes = command.Notes,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var success = await _repository.UpdateAsync(updated, cancellationToken);
            if (!success)
            {
                return Result<bool>.Failure("Update failed.");
            }

            _logger.LogInformation("Updated term {TermId}: {Pattern}", command.Id, command.Term);

            // Publish event
            await PublishEventSafelyAsync(
                new LexiconChangedEvent(LexiconChangeType.Updated, command.Id, command.Term, command.Category),
                cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update term: {TermId}", command.Id);
            return Result<bool>.Failure($"Database error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting term {TermId}", id);

        // Check if term exists
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            _logger.LogWarning("Term not found for delete: {TermId}", id);
            return Result<bool>.Failure("Term not found.");
        }

        if (!existing.IsActive)
        {
            _logger.LogWarning("Term already inactive: {TermId}", id);
            return Result<bool>.Failure("Term is already inactive.");
        }

        // Soft delete (set IsActive = false)
        var updated = existing with
        {
            IsActive = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var success = await _repository.UpdateAsync(updated, cancellationToken);
            if (!success)
            {
                return Result<bool>.Failure("Delete failed.");
            }

            _logger.LogInformation("Soft-deleted term {TermId}: {Pattern}", id, existing.Term);

            // Publish event
            await PublishEventSafelyAsync(
                new LexiconChangedEvent(LexiconChangeType.Deleted, id, existing.Term, existing.Category),
                cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete term: {TermId}", id);
            return Result<bool>.Failure($"Database error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ReactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reactivating term {TermId}", id);

        // Check if term exists
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            _logger.LogWarning("Term not found for reactivate: {TermId}", id);
            return Result<bool>.Failure("Term not found.");
        }

        if (existing.IsActive)
        {
            _logger.LogWarning("Term already active: {TermId}", id);
            return Result<bool>.Failure("Term is already active.");
        }

        // Reactivate (set IsActive = true)
        var updated = existing with
        {
            IsActive = true,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var success = await _repository.UpdateAsync(updated, cancellationToken);
            if (!success)
            {
                return Result<bool>.Failure("Reactivate failed.");
            }

            _logger.LogInformation("Reactivated term {TermId}: {Pattern}", id, existing.Term);

            // Publish event
            await PublishEventSafelyAsync(
                new LexiconChangedEvent(LexiconChangeType.Reactivated, id, existing.Term, existing.Category),
                cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reactivate term: {TermId}", id);
            return Result<bool>.Failure($"Database error: {ex.Message}");
        }
    }

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public async Task<StyleTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var activeTerms = await _repository.GetAllActiveTermsAsync(cancellationToken);
        return activeTerms;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByCategoryAsync(category, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default)
    {
        return await _repository.GetBySeverityAsync(severity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _repository.SearchAsync(searchTerm, cancellationToken);
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public async Task<TermStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching terminology statistics");

        var allTerms = (await _repository.GetAllAsync(cancellationToken)).ToList();

        var activeCount = allTerms.Count(t => t.IsActive);
        var inactiveCount = allTerms.Count - activeCount;

        var categoryCounts = allTerms
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var severityCounts = allTerms
            .GroupBy(t => t.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        return new TermStatistics(
            TotalCount: allTerms.Count,
            ActiveCount: activeCount,
            InactiveCount: inactiveCount,
            CategoryCounts: categoryCounts,
            SeverityCounts: severityCounts);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Publishes an event safely, logging any failures without throwing.
    /// </summary>
    /// <remarks>
    /// LOGIC: Event handler failures should not fail the main operation.
    /// This follows the specification requirement for safe event handling.
    /// </remarks>
    private async Task PublishEventSafelyAsync(LexiconChangedEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Publish(evt, cancellationToken);
            _logger.LogDebug("Published {EventType} event for term {TermId}", evt.ChangeType, evt.TermId);
        }
        catch (Exception ex)
        {
            // LOGIC: Log but don't throw - event handler failures are non-critical
            _logger.LogWarning(ex, "Failed to publish {EventType} event for term {TermId}", evt.ChangeType, evt.TermId);
        }
    }

    #endregion
}
