using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Placeholder health repository implementation.
/// </summary>
/// <remarks>
/// LOGIC: This is a stub implementation for v0.0.8a.
/// Real database integration comes in v0.0.8b.
/// </remarks>
public sealed class HealthRepository : IHealthRepository
{
    private readonly ILogger<HealthRepository> _logger;

    public HealthRepository(ILogger<HealthRepository> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task RecordStartupAsync()
    {
        _logger.LogDebug("HealthRepository.RecordStartupAsync called (stub)");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string> GetDatabaseVersionAsync()
    {
        _logger.LogDebug("HealthRepository.GetDatabaseVersionAsync called (stub)");
        return Task.FromResult("0.0.8-stub");
    }

    /// <inheritdoc/>
    public Task<bool> IsHealthyAsync()
    {
        _logger.LogDebug("HealthRepository.IsHealthyAsync called (stub)");
        return Task.FromResult(true);
    }
}
