// =============================================================================
// File: IngestionBackgroundService.cs
// Project: Lexichord.Modules.RAG
// Description: Background service that processes the ingestion queue.
// =============================================================================
// LOGIC: Hosted service for continuous queue processing.
//   - Extends BackgroundService for lifecycle management.
//   - Dequeues items and calls IIngestionService.IngestFileAsync.
//   - Configurable throttling between items.
//   - Graceful shutdown on cancellation.
//   - Error handling with logging (no retry policy in v0.4.2d).
// =============================================================================

using System.Threading.Channels;
using Lexichord.Abstractions.Contracts.Ingestion;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Background service that continuously processes items from the ingestion queue.
/// </summary>
/// <remarks>
/// <para>
/// This service extends <see cref="BackgroundService"/> to provide automatic
/// lifecycle management when hosted in ASP.NET Core or Generic Host applications.
/// The service starts processing when the host starts and gracefully shuts down
/// when the host stops.
/// </para>
/// <para>
/// <b>Processing Loop:</b> The service continuously dequeues items from
/// <see cref="IIngestionQueue"/> and processes them via <see cref="IIngestionService"/>.
/// A configurable delay between items prevents resource contention.
/// </para>
/// <para>
/// <b>Error Handling:</b> Processing errors for individual items are logged
/// but do not stop the service. The service continues processing remaining
/// items in the queue. Future versions may add configurable retry policies.
/// </para>
/// <para>
/// <b>Graceful Shutdown:</b> When the host signals shutdown, the service
/// completes processing of the current item before stopping. Remaining
/// items in the queue are left for the next service restart.
/// </para>
/// </remarks>
public sealed class IngestionBackgroundService : BackgroundService
{
    private readonly IIngestionQueue _queue;
    private readonly IIngestionService _ingestionService;
    private readonly IngestionQueueOptions _options;
    private readonly ILogger<IngestionBackgroundService> _logger;
    private long _processedCount;
    private long _errorCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionBackgroundService"/> class.
    /// </summary>
    /// <param name="queue">The ingestion queue to process.</param>
    /// <param name="ingestionService">The service to process queue items.</param>
    /// <param name="options">Configuration options for the queue.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public IngestionBackgroundService(
        IIngestionQueue queue,
        IIngestionService ingestionService,
        IOptions<IngestionQueueOptions> options,
        ILogger<IngestionBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(ingestionService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _queue = queue;
        _ingestionService = ingestionService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the total number of items successfully processed.
    /// </summary>
    public long ProcessedCount => Interlocked.Read(ref _processedCount);

    /// <summary>
    /// Gets the total number of items that failed processing.
    /// </summary>
    public long ErrorCount => Interlocked.Read(ref _errorCount);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "IngestionBackgroundService starting with ThrottleDelayMs={ThrottleDelayMs}",
            _options.ThrottleDelayMs);

        try
        {
            await ProcessQueueAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "IngestionBackgroundService stopped gracefully. Processed={Processed}, Errors={Errors}",
                ProcessedCount,
                ErrorCount);
        }
        catch (ChannelClosedException)
        {
            _logger.LogInformation(
                "IngestionBackgroundService completed (queue closed). Processed={Processed}, Errors={Errors}",
                ProcessedCount,
                ErrorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "IngestionBackgroundService terminated unexpectedly. Processed={Processed}, Errors={Errors}",
                ProcessedCount,
                ErrorCount);
            throw;
        }
    }

    /// <summary>
    /// Main processing loop that dequeues and processes items.
    /// </summary>
    /// <param name="stoppingToken">Token to signal shutdown.</param>
    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IngestionQueueItem? item = null;

            try
            {
                // LOGIC: Dequeue the next item (blocks until available).
                item = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);

                // LOGIC: Process the item.
                await ProcessItemAsync(item, stoppingToken).ConfigureAwait(false);

                Interlocked.Increment(ref _processedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // LOGIC: Graceful shutdown - rethrow to exit the loop.
                throw;
            }
            catch (ChannelClosedException)
            {
                // LOGIC: Queue is completed and empty - rethrow to exit.
                throw;
            }
            catch (Exception ex)
            {
                // LOGIC: Log the error but continue processing.
                Interlocked.Increment(ref _errorCount);

                if (item is not null)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process queue item {Id} for {FilePath}",
                        item.Id,
                        item.FilePath);
                }
                else
                {
                    _logger.LogError(ex, "Failed to dequeue item from ingestion queue");
                }
            }

            // LOGIC: Apply throttle delay if configured.
            if (_options.ThrottleDelayMs > 0 && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.ThrottleDelayMs, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // LOGIC: Shutdown requested during delay - exit gracefully.
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Processes a single queue item.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessItemAsync(IngestionQueueItem item, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var queueLatency = startTime - item.EnqueuedAt;

        _logger.LogDebug(
            "Processing queue item {Id} for {FilePath} (queue latency: {LatencyMs:F0}ms, correlation: {CorrelationId})",
            item.Id,
            item.FilePath,
            queueLatency.TotalMilliseconds,
            item.CorrelationId);

        // LOGIC: Call the ingestion service to process the file.
        var result = await _ingestionService.IngestFileAsync(
            item.ProjectId,
            item.FilePath,
            options: null, // Use default options
            cancellationToken).ConfigureAwait(false);

        var processingTime = DateTimeOffset.UtcNow - startTime;

        if (result.Success)
        {
            _logger.LogInformation(
                "Successfully processed {FilePath} in {ProcessingMs:F0}ms (" +
                "chunks: {ChunkCount}, documentId: {DocumentId})",
                item.FilePath,
                processingTime.TotalMilliseconds,
                result.ChunkCount,
                result.DocumentId);
        }
        else
        {
            var errorMessage = result.Errors.Count > 0 ? result.Errors[0] : "(no error message)";
            _logger.LogWarning(
                "Processing {FilePath} failed: {ErrorMessage}",
                item.FilePath,
                errorMessage);
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "IngestionBackgroundService stopping. Waiting up to {TimeoutSeconds}s for graceful shutdown...",
            _options.ShutdownTimeoutSeconds);

        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(_options.ShutdownTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await base.StopAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(
                "IngestionBackgroundService shutdown timed out after {TimeoutSeconds}s",
                _options.ShutdownTimeoutSeconds);
        }

        _logger.LogInformation(
            "IngestionBackgroundService stopped. Total processed: {Processed}, errors: {Errors}",
            ProcessedCount,
            ErrorCount);
    }
}
