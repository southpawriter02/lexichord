// <copyright file="KnowledgeHubViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Indexing;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the Knowledge Hub unified dashboard view.
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
/// <para>Thread-safe: UI thread dispatching handled appropriately.</para>
/// </remarks>
public partial class KnowledgeHubViewModel : ObservableObject, IKnowledgeHubViewModel, INotificationHandler<DocumentIndexedEvent>
{
    #region Log Event IDs

    /// <summary>Log event IDs for structured logging (2000-2015 range).</summary>
    private static class LogEvents
    {
        public const int Initialized = 2000;
        public const int InitializeStarted = 2001;
        public const int InitializeCompleted = 2002;
        public const int InitializeFailed = 2003;
        public const int StatisticsLoaded = 2004;
        public const int StatisticsLoadFailed = 2005;
        public const int ReindexStarted = 2006;
        public const int ReindexCompleted = 2007;
        public const int ReindexFailed = 2008;
        public const int SearchExecuted = 2009;
        public const int LicenseChecked = 2010;
        public const int IndexingProgressUpdated = 2011;
        public const int RecentSearchAdded = 2012;
        public const int DocumentIndexedHandled = 2013;
        public const int Disposed = 2014;
    }

    #endregion

    #region Dependencies

    private readonly IIndexStatusService _indexStatusService;
    private readonly IIndexManagementService _indexManagementService;
    private readonly IQueryHistoryService _queryHistoryService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<KnowledgeHubViewModel> _logger;

    private readonly ObservableCollection<RecentSearch> _recentSearches = new();
    private bool _isDisposed;

    #endregion

    #region Observable Properties

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isLicensed;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isLoading;

    /// <inheritdoc/>
    [ObservableProperty]
    private KnowledgeHubStatistics _statistics = KnowledgeHubStatistics.Empty;

    /// <inheritdoc/>
    [ObservableProperty]
    private bool _isIndexing;

    /// <inheritdoc/>
    [ObservableProperty]
    private double _indexingProgress;

    /// <inheritdoc/>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>
    /// Gets or sets any error message to display.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    #endregion

    #region Computed Properties

    /// <inheritdoc/>
    public bool HasIndexedContent => Statistics.HasIndexedContent;

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<RecentSearch> RecentSearches { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeHubViewModel"/> class.
    /// </summary>
    /// <param name="indexStatusService">Service for index status queries.</param>
    /// <param name="indexManagementService">Service for index management operations.</param>
    /// <param name="queryHistoryService">Service for query history tracking.</param>
    /// <param name="licenseService">Service for license validation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public KnowledgeHubViewModel(
        IIndexStatusService indexStatusService,
        IIndexManagementService indexManagementService,
        IQueryHistoryService queryHistoryService,
        ILicenseService licenseService,
        ILogger<KnowledgeHubViewModel> logger)
    {
        _indexStatusService = indexStatusService ?? throw new ArgumentNullException(nameof(indexStatusService));
        _indexManagementService = indexManagementService ?? throw new ArgumentNullException(nameof(indexManagementService));
        _queryHistoryService = queryHistoryService ?? throw new ArgumentNullException(nameof(queryHistoryService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RecentSearches = new ReadOnlyObservableCollection<RecentSearch>(_recentSearches);

        // Check license
        IsLicensed = _licenseService.IsFeatureEnabled(FeatureCodes.KnowledgeHub);

        _logger.LogInformation(
            LogEvents.Initialized,
            "KnowledgeHubViewModel initialized. Licensed: {IsLicensed}",
            IsLicensed);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("InitializeAsync called after disposal");
            return;
        }

        _logger.LogDebug(LogEvents.InitializeStarted, "KnowledgeHubViewModel.InitializeAsync started");

        // Check license on property access
        IsLicensed = _licenseService.IsFeatureEnabled(FeatureCodes.KnowledgeHub);
        _logger.LogDebug(LogEvents.LicenseChecked, "License check: {IsLicensed}", IsLicensed);

        if (!IsLicensed)
        {
            _logger.LogWarning("Knowledge Hub not licensed, skipping initialization");
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Load statistics
            await RefreshStatisticsAsync(ct).ConfigureAwait(false);

            // Load recent searches
            await LoadRecentSearchesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(LogEvents.InitializeCompleted, "KnowledgeHubViewModel initialized successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Initialization cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during KnowledgeHubViewModel initialization");
            ErrorMessage = "Failed to load knowledge hub data.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <inheritdoc/>
    public async Task RefreshStatisticsAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            var stats = await _indexStatusService.GetStatisticsAsync(ct).ConfigureAwait(false);

            Statistics = new KnowledgeHubStatistics(
                TotalDocuments: stats.DocumentCount,
                TotalChunks: stats.ChunkCount,
                IndexedDocuments: stats.DocumentCount,
                PendingDocuments: stats.PendingCount,
                LastIndexedAt: stats.LastIndexedAt.HasValue ? stats.LastIndexedAt.Value.DateTime : (DateTime?)null,
                StorageSizeBytes: stats.StorageSizeBytes);

            OnPropertyChanged(nameof(HasIndexedContent));

            _logger.LogDebug(
                LogEvents.StatisticsLoaded,
                "Statistics loaded: {DocumentCount} docs, {ChunkCount} chunks",
                Statistics.IndexedDocuments,
                Statistics.TotalChunks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Statistics refresh cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load statistics");
            throw;
        }
    }

    /// <inheritdoc/>
    [RelayCommand]
    public async Task ReindexAllAsync(CancellationToken ct = default)
    {
        if (_isDisposed || IsIndexing)
        {
            return;
        }

        _logger.LogInformation(LogEvents.ReindexStarted, "Re-indexing all documents started");

        try
        {
            IsIndexing = true;
            IndexingProgress = 0;
            ErrorMessage = null;

            var progress = new Progress<int>(p =>
            {
                IndexingProgress = p;
                _logger.LogDebug(LogEvents.IndexingProgressUpdated, "Indexing progress: {Progress}%", p);
            });

            var result = await _indexManagementService.ReindexAllAsync(progress, ct).ConfigureAwait(false);

            _logger.LogInformation(
                LogEvents.ReindexCompleted,
                "Re-index completed: {ProcessedCount} processed, {FailedCount} failed",
                result.ProcessedCount,
                result.FailedCount);

            // Refresh statistics after re-index
            await RefreshStatisticsAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Re-index cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Re-index failed");
            ErrorMessage = "Re-indexing failed. Please try again.";
        }
        finally
        {
            IsIndexing = false;
            IndexingProgress = 0;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles document indexed events to refresh statistics.
    /// </summary>
    public async Task Handle(DocumentIndexedEvent notification, CancellationToken cancellationToken)
    {
        if (_isDisposed)
        {
            return;
        }

        _logger.LogDebug(
            LogEvents.DocumentIndexedHandled,
            "DocumentIndexedEvent received for {DocumentId}",
            notification.DocumentId);

        // Refresh statistics when a document is indexed
        try
        {
            await RefreshStatisticsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh statistics after document indexed");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads recent search queries from history.
    /// </summary>
    private async Task LoadRecentSearchesAsync(CancellationToken ct)
    {
        try
        {
            // LOGIC: Load recent queries from QueryHistoryService
            const int count = 10;
            var queries = await _queryHistoryService.GetRecentAsync(count, ct).ConfigureAwait(false);
            
            _recentSearches.Clear();
            foreach (var item in queries)
            {
                _recentSearches.Add(new RecentSearch(
                    Query: item.Query,
                    ResultCount: item.ResultCount,
                    ExecutedAt: item.ExecutedAt,
                    Duration: TimeSpan.FromMilliseconds(item.DurationMs)));
            }

            _logger.LogDebug(LogEvents.RecentSearchAdded, "Loaded {Count} recent searches", _recentSearches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent searches");
            // Non-critical, don't throw
        }
    }

    #endregion

    #region Property Changed Handlers

    partial void OnStatisticsChanged(KnowledgeHubStatistics value)
    {
        OnPropertyChanged(nameof(HasIndexedContent));
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _recentSearches.Clear();

        _logger.LogDebug(LogEvents.Disposed, "KnowledgeHubViewModel disposed");
    }

    #endregion
}
