// =============================================================================
// File: ContextCacheOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Configuration options for the context expansion cache.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Configures session-scoped cache behavior including per-session limits.
// =============================================================================

namespace Lexichord.Modules.RAG.Configuration;

/// <summary>
/// Configuration options for <see cref="Services.ContextExpansionCacheService"/>.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the session-scoped context expansion cache:
/// </para>
/// <list type="bullet">
///   <item><see cref="MaxEntriesPerSession"/>: Maximum cached chunks per session.</item>
///   <item><see cref="Enabled"/>: Master switch to enable/disable caching.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public sealed class ContextCacheOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "RAG:ContextCache";

    /// <summary>
    /// Gets or sets the maximum number of cached entries per session.
    /// </summary>
    /// <value>Default is 50.</value>
    /// <remarks>
    /// LOGIC: Each session maintains its own entry limit. When exceeded, the oldest
    /// entry in that session is removed.
    /// </remarks>
    public int MaxEntriesPerSession { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether the cache is enabled.
    /// </summary>
    /// <value>Default is <c>true</c>.</value>
    /// <remarks>
    /// When disabled, <c>TryGet</c> always returns false and <c>Set</c> is a no-op.
    /// </remarks>
    public bool Enabled { get; set; } = true;
}
