// =============================================================================
// File: BatchJobFilter.cs
// Project: Lexichord.Abstractions
// Description: Filter criteria for querying batch deduplication job history.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Defines filtering options for ListJobsAsync queries.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Filter criteria for listing batch deduplication jobs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This record provides filtering options for <see cref="IBatchDeduplicationJob.ListJobsAsync"/>.
/// All filter properties are optional; when null, no filtering is applied for that property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get all running jobs
/// var filter = new BatchJobFilter { States = new[] { BatchJobState.Running } };
/// 
/// // Get recent failed jobs for a project
/// var filter = new BatchJobFilter
/// {
///     ProjectId = myProjectId,
///     States = new[] { BatchJobState.Failed },
///     CreatedAfter = DateTime.UtcNow.AddDays(-7)
/// };
/// 
/// // Get all jobs with pagination
/// var filter = new BatchJobFilter { Limit = 20 };
/// </code>
/// </example>
/// <param name="ProjectId">Optional project ID to filter by scope.</param>
/// <param name="States">Optional state filter. When set, only jobs in these states are returned.</param>
/// <param name="CreatedAfter">Optional minimum creation date.</param>
/// <param name="CreatedBefore">Optional maximum creation date.</param>
/// <param name="Limit">Maximum number of jobs to return. Default: 100.</param>
public record BatchJobFilter(
    Guid? ProjectId = null,
    BatchJobState[]? States = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null,
    int Limit = 100)
{
    /// <summary>
    /// Gets a filter that returns all jobs (up to limit).
    /// </summary>
    public static BatchJobFilter All { get; } = new();

    /// <summary>
    /// Gets a filter for currently running jobs only.
    /// </summary>
    public static BatchJobFilter Running { get; } = new(States: new[] { BatchJobState.Running });

    /// <summary>
    /// Gets a filter for jobs that can be resumed.
    /// </summary>
    public static BatchJobFilter Resumable { get; } = new(States: new[] { BatchJobState.Paused });

    /// <summary>
    /// Gets a filter for failed jobs.
    /// </summary>
    public static BatchJobFilter Failed { get; } = new(States: new[] { BatchJobState.Failed });

    /// <summary>
    /// Creates a filter for jobs in a specific project.
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    /// <returns>A filter scoped to the specified project.</returns>
    public static BatchJobFilter ForProject(Guid projectId) => new(ProjectId: projectId);
}
