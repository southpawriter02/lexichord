// =============================================================================
// File: StubHeadingHierarchyService.cs
// Project: Lexichord.Modules.RAG
// Description: Placeholder implementation until v0.5.3c heading hierarchy.
// =============================================================================
// LOGIC: Returns empty results for all heading queries. Logs warning on first
//   use to indicate the stub nature. Will be replaced in v0.5.3c with full
//   implementation that parses document structure.
// =============================================================================
// VERSION: v0.5.3a (Context Expansion Service) - STUB
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Placeholder heading hierarchy service until v0.5.3c implementation.
/// </summary>
/// <remarks>
/// <para>
/// This stub returns empty breadcrumbs and null heading trees for all queries.
/// It allows the Context Expansion Service to function without blocking on
/// the Heading Hierarchy feature implementation.
/// </para>
/// <para>
/// <b>Note:</b> This class will be replaced in v0.5.3c with
/// <c>HeadingHierarchyService</c> that parses actual document structure.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.3a as a temporary placeholder.
/// </para>
/// </remarks>
public sealed class StubHeadingHierarchyService : IHeadingHierarchyService
{
    private readonly ILogger<StubHeadingHierarchyService> _logger;
    private bool _hasLoggedStubWarning;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubHeadingHierarchyService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public StubHeadingHierarchyService(ILogger<StubHeadingHierarchyService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Stub behavior:</b> Always returns an empty list.
    /// </remarks>
    public Task<IReadOnlyList<string>> GetBreadcrumbAsync(
        Guid documentId,
        int chunkIndex,
        CancellationToken cancellationToken = default)
    {
        LogStubWarningOnce();

        _logger.LogDebug(
            "Stub breadcrumb request for document {DocumentId}, chunk index {ChunkIndex} - returning empty",
            documentId, chunkIndex);

        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Stub behavior:</b> Always returns null.
    /// </remarks>
    public Task<HeadingNode?> BuildHeadingTreeAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        LogStubWarningOnce();

        _logger.LogDebug(
            "Stub heading tree request for document {DocumentId} - returning null",
            documentId);

        return Task.FromResult<HeadingNode?>(null);
    }

    /// <summary>
    /// Logs a one-time warning about stub implementation.
    /// </summary>
    private void LogStubWarningOnce()
    {
        if (_hasLoggedStubWarning) return;

        _logger.LogWarning(
            "Using stub heading hierarchy service. " +
            "Heading breadcrumbs will be empty until v0.5.3c is implemented");
        _hasLoggedStubWarning = true;
    }
}
