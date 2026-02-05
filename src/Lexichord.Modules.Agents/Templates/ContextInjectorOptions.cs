// -----------------------------------------------------------------------
// <copyright file="ContextInjectorOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Configuration options for the <see cref="ContextInjector"/> service.
/// </summary>
/// <remarks>
/// <para>
/// This record defines the configurable parameters for context assembly, including
/// timeouts for provider execution, content limits, and relevance thresholds.
/// </para>
/// <para>
/// <strong>Configuration Binding:</strong>
/// </para>
/// <code>
/// // In appsettings.json:
/// {
///   "Agents": {
///     "ContextInjector": {
///       "RAGTimeoutMs": 5000,
///       "ProviderTimeoutMs": 2000,
///       "MaxStyleRules": 20,
///       "MaxChunkLength": 1000,
///       "MinRAGRelevanceScore": 0.5
///     }
///   }
/// }
/// </code>
/// <para>
/// <strong>Static Presets:</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Default"/>: Balanced settings for typical use.</description></item>
///   <item><description><see cref="Fast"/>: Reduced timeouts for responsive UI.</description></item>
///   <item><description><see cref="Thorough"/>: Higher limits for comprehensive context.</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
/// </para>
/// </remarks>
/// <param name="RAGTimeoutMs">
/// Timeout in milliseconds for RAG provider execution.
/// RAG searches involve network I/O and may be slower than local providers.
/// Default: 5000ms. Minimum: 500ms.
/// </param>
/// <param name="ProviderTimeoutMs">
/// Timeout in milliseconds for other (non-RAG) provider execution.
/// Document and style providers are typically fast and synchronous.
/// Default: 2000ms. Minimum: 100ms.
/// </param>
/// <param name="MaxStyleRules">
/// Maximum number of style rules to include in the formatted output.
/// Prevents excessively long rule lists from consuming prompt tokens.
/// Default: 20. Range: 1-100.
/// </param>
/// <param name="MaxChunkLength">
/// Maximum character length for each RAG chunk in the formatted output.
/// Chunks exceeding this length are truncated with "..." appended.
/// Default: 1000. Minimum: 50.
/// </param>
/// <param name="MinRAGRelevanceScore">
/// Minimum cosine similarity score for RAG results to be included.
/// Lower values include more results; higher values are more selective.
/// Default: 0.5. Range: 0.0-1.0.
/// </param>
/// <example>
/// <code>
/// // Using default options
/// services.AddContextInjection();
///
/// // Using custom options
/// services.AddContextInjection(options =>
/// {
///     options = options with
///     {
///         RAGTimeoutMs = 3000,
///         MinRAGRelevanceScore = 0.6f
///     };
/// });
///
/// // Using a preset
/// services.Configure&lt;ContextInjectorOptions&gt;(_ => ContextInjectorOptions.Fast);
/// </code>
/// </example>
/// <seealso cref="ContextInjector"/>
public record ContextInjectorOptions(
    int RAGTimeoutMs = 5000,
    int ProviderTimeoutMs = 2000,
    int MaxStyleRules = 20,
    int MaxChunkLength = 1000,
    float MinRAGRelevanceScore = 0.5f)
{
    /// <summary>
    /// The configuration section name for binding from appsettings.json.
    /// </summary>
    /// <value>"Agents:ContextInjector"</value>
    /// <remarks>
    /// LOGIC: Used by the service registration extension to bind configuration from the standard location.
    /// </remarks>
    public const string SectionName = "Agents:ContextInjector";

    /// <summary>
    /// Default options with balanced settings for typical interactive use.
    /// </summary>
    /// <value>
    /// <list type="bullet">
    ///   <item><see cref="RAGTimeoutMs"/>: 5000ms</item>
    ///   <item><see cref="ProviderTimeoutMs"/>: 2000ms</item>
    ///   <item><see cref="MaxStyleRules"/>: 20</item>
    ///   <item><see cref="MaxChunkLength"/>: 1000</item>
    ///   <item><see cref="MinRAGRelevanceScore"/>: 0.5</item>
    /// </list>
    /// </value>
    /// <remarks>
    /// LOGIC: These defaults provide a good balance between responsiveness and thoroughness
    /// for typical prompt assembly scenarios.
    /// </remarks>
    public static ContextInjectorOptions Default { get; } = new();

    /// <summary>
    /// Fast preset with reduced timeouts for responsive UI interactions.
    /// </summary>
    /// <value>
    /// <list type="bullet">
    ///   <item><see cref="RAGTimeoutMs"/>: 2000ms</item>
    ///   <item><see cref="ProviderTimeoutMs"/>: 1000ms</item>
    ///   <item><see cref="MaxStyleRules"/>: 10</item>
    ///   <item><see cref="MaxChunkLength"/>: 500</item>
    ///   <item><see cref="MinRAGRelevanceScore"/>: 0.6</item>
    /// </list>
    /// </value>
    /// <remarks>
    /// LOGIC: Use this preset when prompt assembly is on the critical path for user
    /// interactions and speed is prioritized over comprehensive context.
    /// </remarks>
    public static ContextInjectorOptions Fast { get; } = new(
        RAGTimeoutMs: 2000,
        ProviderTimeoutMs: 1000,
        MaxStyleRules: 10,
        MaxChunkLength: 500,
        MinRAGRelevanceScore: 0.6f);

    /// <summary>
    /// Thorough preset with higher limits for comprehensive context assembly.
    /// </summary>
    /// <value>
    /// <list type="bullet">
    ///   <item><see cref="RAGTimeoutMs"/>: 10000ms</item>
    ///   <item><see cref="ProviderTimeoutMs"/>: 5000ms</item>
    ///   <item><see cref="MaxStyleRules"/>: 50</item>
    ///   <item><see cref="MaxChunkLength"/>: 2000</item>
    ///   <item><see cref="MinRAGRelevanceScore"/>: 0.4</item>
    /// </list>
    /// </value>
    /// <remarks>
    /// LOGIC: Use this preset for background processing or when comprehensive context
    /// is more important than response latency.
    /// </remarks>
    public static ContextInjectorOptions Thorough { get; } = new(
        RAGTimeoutMs: 10000,
        ProviderTimeoutMs: 5000,
        MaxStyleRules: 50,
        MaxChunkLength: 2000,
        MinRAGRelevanceScore: 0.4f);

    /// <summary>
    /// Gets the RAG timeout clamped to a minimum of 500ms.
    /// </summary>
    /// <value>The configured RAG timeout, or 500 if the configured value is lower.</value>
    /// <remarks>
    /// LOGIC: Ensures a minimum timeout to allow for network latency in RAG queries.
    /// </remarks>
    public int EffectiveRAGTimeoutMs => Math.Max(RAGTimeoutMs, 500);

    /// <summary>
    /// Gets the provider timeout clamped to a minimum of 100ms.
    /// </summary>
    /// <value>The configured provider timeout, or 100 if the configured value is lower.</value>
    /// <remarks>
    /// LOGIC: Ensures a minimum timeout for synchronous provider operations.
    /// </remarks>
    public int EffectiveProviderTimeoutMs => Math.Max(ProviderTimeoutMs, 100);

    /// <summary>
    /// Gets the max style rules clamped to the range [1, 100].
    /// </summary>
    /// <value>The configured max style rules, clamped to valid range.</value>
    /// <remarks>
    /// LOGIC: Ensures at least one rule is allowed and prevents unreasonably large lists.
    /// </remarks>
    public int EffectiveMaxStyleRules => Math.Clamp(MaxStyleRules, 1, 100);

    /// <summary>
    /// Gets the max chunk length clamped to a minimum of 50.
    /// </summary>
    /// <value>The configured max chunk length, or 50 if the configured value is lower.</value>
    /// <remarks>
    /// LOGIC: Ensures chunks are long enough to be meaningful.
    /// </remarks>
    public int EffectiveMaxChunkLength => Math.Max(MaxChunkLength, 50);

    /// <summary>
    /// Gets the minimum RAG relevance score clamped to the range [0.0, 1.0].
    /// </summary>
    /// <value>The configured minimum score, clamped to valid range.</value>
    /// <remarks>
    /// LOGIC: Ensures the score threshold is within the valid cosine similarity range.
    /// </remarks>
    public float EffectiveMinRAGRelevanceScore => Math.Clamp(MinRAGRelevanceScore, 0.0f, 1.0f);
}
