using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for filtering style terminology based on criteria.
/// </summary>
/// <remarks>
/// LOGIC: Implements pure filtering logic using LINQ.
/// All filter conditions are applied as AND logic.
///
/// Version: v0.2.5b
/// </remarks>
public sealed class TermFilterService : ITermFilterService
{
    private readonly ILogger<TermFilterService> _logger;

    /// <summary>
    /// Initializes a new instance of the TermFilterService.
    /// </summary>
    public TermFilterService(ILogger<TermFilterService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IEnumerable<StyleTerm> Filter(IEnumerable<StyleTerm> terms, FilterCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(criteria);

        _logger.LogDebug("Filtering terms with criteria: SearchText='{SearchText}', ShowInactive={ShowInactive}, Category='{Category}', Severity='{Severity}'",
            criteria.SearchText,
            criteria.ShowInactive,
            criteria.CategoryFilter ?? "(all)",
            criteria.SeverityFilter ?? "(all)");

        var result = terms;

        // LOGIC: Apply text search filter (case-insensitive substring match)
        // Searches Term, Replacement, and Notes fields
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var searchText = criteria.SearchText.Trim();
            result = result.Where(t =>
                (t.Term?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Replacement?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Notes?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));

            _logger.LogDebug("Search filter '{SearchText}' applied", searchText);
        }

        // LOGIC: Apply inactive toggle filter
        // When ShowInactive is false, only show active terms
        if (!criteria.ShowInactive)
        {
            result = result.Where(t => t.IsActive);
            _logger.LogDebug("Inactive filter applied (hiding inactive)");
        }

        // LOGIC: Apply category filter (exact match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(criteria.CategoryFilter))
        {
            result = result.Where(t =>
                string.Equals(t.Category, criteria.CategoryFilter, StringComparison.OrdinalIgnoreCase));

            _logger.LogDebug("Category filter '{Category}' applied", criteria.CategoryFilter);
        }

        // LOGIC: Apply severity filter (exact match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(criteria.SeverityFilter))
        {
            result = result.Where(t =>
                string.Equals(t.Severity, criteria.SeverityFilter, StringComparison.OrdinalIgnoreCase));

            _logger.LogDebug("Severity filter '{Severity}' applied", criteria.SeverityFilter);
        }

        return result;
    }
}
