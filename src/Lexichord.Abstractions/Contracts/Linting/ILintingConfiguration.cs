namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Configuration interface for linting behavior.
/// </summary>
/// <remarks>
/// LOGIC: Abstracts linting configuration to enable testing with
/// custom values and future support for per-document or workspace
/// configuration overrides.
///
/// Version: v0.2.3b
/// </remarks>
public interface ILintingConfiguration
{
    /// <summary>
    /// Debounce delay in milliseconds between content changes and scan trigger.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents excessive scans during rapid typing.
    /// Range: 100-2000ms, Default: 300ms
    /// </remarks>
    int DebounceMilliseconds { get; }

    /// <summary>
    /// Maximum number of concurrent document scans.
    /// </summary>
    /// <remarks>
    /// LOGIC: Limits CPU pressure when many documents are open.
    /// Range: 1-8, Default: 2
    /// </remarks>
    int MaxConcurrentScans { get; }

    /// <summary>
    /// Whether linting is globally enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Master switch for disabling all background linting.
    /// </remarks>
    bool Enabled { get; }
}
