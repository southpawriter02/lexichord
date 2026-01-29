using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Placeholder heartbeat service implementation.
/// </summary>
/// <remarks>
/// LOGIC: This is a stub implementation for v0.0.8a.
/// Real periodic health checks come in v0.0.8b.
/// </remarks>
public sealed class HeartbeatService : IHeartbeatService
{
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(ILogger<HeartbeatService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public TimeSpan Interval => TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<bool>? HealthChanged;

    /// <inheritdoc/>
    public void Start()
    {
        _logger.LogDebug("HeartbeatService.Start called (stub)");
        IsRunning = true;
        HealthChanged?.Invoke(this, true);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _logger.LogDebug("HeartbeatService.Stop called (stub)");
        IsRunning = false;
    }
}
