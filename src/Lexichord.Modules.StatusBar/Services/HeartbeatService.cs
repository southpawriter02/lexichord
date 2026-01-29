using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Service that records periodic heartbeats to the database.
/// </summary>
/// <remarks>
/// LOGIC: The heartbeat service provides a simple liveness indicator.
/// By recording timestamps at regular intervals, we can detect:
/// - Application freezes (heartbeat stops)
/// - Thread starvation (timer delays)
/// - Database issues (recording fails)
///
/// The default interval is 60 seconds, which balances:
/// - Frequency: Often enough to detect issues quickly
/// - Overhead: Infrequent enough to not impact performance
/// </remarks>
public sealed class HeartbeatService : IHeartbeatService
{
    private readonly IHealthRepository _healthRepo;
    private readonly ILogger<HeartbeatService> _logger;
    private readonly System.Timers.Timer _timer;
    private bool _disposed;

    private const int DefaultIntervalSeconds = 60;
    private const int MaxConsecutiveFailures = 5;

    /// <inheritdoc/>
    public TimeSpan Interval { get; }

    /// <inheritdoc/>
    public bool IsRunning => _timer.Enabled;

    /// <inheritdoc/>
    public DateTime? LastHeartbeat { get; private set; }

    /// <inheritdoc/>
    public int ConsecutiveFailures { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<bool>? HealthChanged;

    public HeartbeatService(
        IHealthRepository healthRepo,
        ILogger<HeartbeatService> logger)
    {
        _healthRepo = healthRepo;
        _logger = logger;

        Interval = TimeSpan.FromSeconds(DefaultIntervalSeconds);

        _timer = new System.Timers.Timer(Interval.TotalMilliseconds);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;

        _logger.LogDebug("HeartbeatService created with {Interval} interval", Interval);
    }

    /// <inheritdoc/>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
        {
            _logger.LogWarning("HeartbeatService is already running");
            return;
        }

        _timer.Start();
        _logger.LogInformation("Heartbeat service started with {Interval} interval", Interval);

        // LOGIC: Record initial heartbeat immediately
        _ = RecordHeartbeatAsync();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!IsRunning)
        {
            _logger.LogWarning("HeartbeatService is not running");
            return;
        }

        _timer.Stop();
        _logger.LogInformation("Heartbeat service stopped");
    }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await RecordHeartbeatAsync();
    }

    private async Task RecordHeartbeatAsync()
    {
        try
        {
            await _healthRepo.RecordHeartbeatAsync();
            LastHeartbeat = DateTime.UtcNow;

            // LOGIC: Reset failures on success and notify if we recovered
            if (ConsecutiveFailures > 0)
            {
                _logger.LogInformation("Heartbeat recovered after {Failures} failures", ConsecutiveFailures);
                ConsecutiveFailures = 0;
                HealthChanged?.Invoke(this, true);
            }
            else
            {
                ConsecutiveFailures = 0;
            }

            _logger.LogDebug("Heartbeat recorded successfully");
        }
        catch (Exception ex)
        {
            ConsecutiveFailures++;
            _logger.LogError(ex,
                "Failed to record heartbeat. Consecutive failures: {Failures}",
                ConsecutiveFailures);

            // LOGIC: If we've failed too many times, log a critical warning
            if (ConsecutiveFailures >= MaxConsecutiveFailures)
            {
                _logger.LogCritical(
                    "Heartbeat has failed {Failures} consecutive times. " +
                    "Database may be unavailable.",
                    ConsecutiveFailures);
            }

            // Notify listeners of unhealthy state
            HealthChanged?.Invoke(this, false);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _timer.Dispose();
        _disposed = true;

        _logger.LogDebug("HeartbeatService disposed");
        GC.SuppressFinalize(this);
    }
}
