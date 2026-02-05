// <copyright file="IKnowledgeHubViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents statistics about the knowledge base index.
/// </summary>
/// <remarks>
/// LOGIC: v0.6.4a - Provides aggregated statistics for the Knowledge Hub dashboard.
/// </remarks>
public record KnowledgeHubStatistics(
    int TotalDocuments,
    int TotalChunks,
    int IndexedDocuments,
    int PendingDocuments,
    DateTime? LastIndexedAt,
    long StorageSizeBytes)
{
    /// <summary>
    /// Gets an empty statistics instance.
    /// </summary>
    public static KnowledgeHubStatistics Empty => new(0, 0, 0, 0, null, 0);

    /// <summary>
    /// Gets whether there is any indexed content.
    /// </summary>
    public bool HasIndexedContent => IndexedDocuments > 0;

    /// <summary>
    /// Gets the formatted storage size (e.g., "1.2 MB").
    /// </summary>
    public string FormattedStorageSize => StorageSizeBytes switch
    {
        < 1024 => $"{StorageSizeBytes} B",
        < 1024 * 1024 => $"{StorageSizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{StorageSizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{StorageSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    /// <summary>
    /// Gets the indexing progress percentage (0-100).
    /// </summary>
    public double IndexingProgress => TotalDocuments > 0
        ? (double)IndexedDocuments / TotalDocuments * 100
        : 0;
}

/// <summary>
/// Represents a recent search query in the Knowledge Hub.
/// </summary>
/// <remarks>
/// LOGIC: v0.6.4a - Tracks recent searches for analytics and quick re-execution.
/// </remarks>
public record RecentSearch(
    string Query,
    int ResultCount,
    DateTime ExecutedAt,
    TimeSpan Duration)
{
    /// <summary>
    /// Gets a human-readable relative time (e.g., "5 min ago").
    /// </summary>
    public string RelativeTime
    {
        get
        {
            var elapsed = DateTime.UtcNow - ExecutedAt;
            return elapsed.TotalMinutes < 1 ? "just now" :
                   elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes} min ago" :
                   elapsed.TotalHours < 24 ? $"{(int)elapsed.TotalHours} hr ago" :
                   $"{(int)elapsed.TotalDays} days ago";
        }
    }
}

/// <summary>
/// ViewModel interface for the Knowledge Hub unified dashboard view.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.6.4a - The Knowledge Hub provides a unified container for all RAG functionality.</para>
/// <para>Features:</para>
/// <list type="bullet">
///   <item>Index statistics and health overview</item>
///   <item>Embedded search interface (ReferenceView)</item>
///   <item>Recent search history</item>
///   <item>Source management shortcuts</item>
///   <item>License gating for WriterPro tier</item>
/// </list>
/// </remarks>
public interface IKnowledgeHubViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Gets whether the user has the required license.
    /// </summary>
    bool IsLicensed { get; }

    /// <summary>
    /// Gets whether the dashboard is currently loading data.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the current knowledge base statistics.
    /// </summary>
    KnowledgeHubStatistics Statistics { get; }

    /// <summary>
    /// Gets the recent search queries.
    /// </summary>
    ReadOnlyObservableCollection<RecentSearch> RecentSearches { get; }

    /// <summary>
    /// Gets whether indexing is currently in progress.
    /// </summary>
    bool IsIndexing { get; }

    /// <summary>
    /// Gets the indexing progress percentage (0-100).
    /// </summary>
    double IndexingProgress { get; }

    /// <summary>
    /// Gets whether the knowledge base has any indexed content.
    /// </summary>
    bool HasIndexedContent { get; }

    /// <summary>
    /// Gets or sets the search query text.
    /// </summary>
    string SearchQuery { get; set; }

    /// <summary>
    /// Initializes the view model and loads initial data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Refreshes the statistics from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task RefreshStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Triggers a re-index of all sources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ReindexAllAsync(CancellationToken ct = default);
}
