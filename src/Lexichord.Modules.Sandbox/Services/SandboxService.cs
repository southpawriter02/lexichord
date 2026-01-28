using Lexichord.Modules.Sandbox.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Sandbox.Services;

/// <summary>
/// Implementation of ISandboxService for architecture validation.
/// </summary>
/// <remarks>
/// LOGIC: This service demonstrates:
/// - Constructor injection works for module services
/// - ILogger{T} is resolvable (proves DI integration)
/// - Service state persists between calls (singleton behavior)
///
/// Design Pattern:
/// - Uses primary constructor for dependency injection
/// - Implements ISandboxService interface
/// - Registered as Singleton in SandboxModule.RegisterServices
/// </remarks>
public sealed class SandboxService(ILogger<SandboxService> logger) : ISandboxService
{
    private DateTime _initializationTime = DateTime.MinValue;

    /// <summary>
    /// Sets the initialization time (called by SandboxModule.InitializeAsync).
    /// </summary>
    /// <param name="time">The initialization timestamp.</param>
    /// <remarks>
    /// LOGIC: This method is called by the module during InitializeAsync
    /// to prove that initialization occurs after service resolution is available.
    /// Internal visibility keeps it accessible to the module but not external callers.
    /// </remarks>
    internal void SetInitializationTime(DateTime time)
    {
        _initializationTime = time;
        logger.LogDebug("Sandbox service initialization time set to {Time}", time);
    }

    /// <inheritdoc/>
    public string GetModuleName()
    {
        logger.LogDebug("GetModuleName called");
        return "Lexichord.Modules.Sandbox";
    }

    /// <inheritdoc/>
    public string Echo(string input)
    {
        logger.LogDebug("Echo called with input: {Input}", input);
        return $"[Sandbox] {input}";
    }

    /// <inheritdoc/>
    public DateTime GetInitializationTime()
    {
        logger.LogDebug("GetInitializationTime called, returning {Time}", _initializationTime);
        return _initializationTime;
    }
}
