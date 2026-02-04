// -----------------------------------------------------------------------
// <copyright file="MustacheRendererOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Configuration options for the Mustache prompt renderer.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="MustachePromptRenderer"/>
/// including case sensitivity, validation behavior, and performance thresholds.
/// </para>
/// <para>
/// The default configuration uses case-insensitive variable lookup and throws
/// <see cref="Lexichord.Abstractions.Contracts.LLM.TemplateValidationException"/>
/// when required variables are missing from <see cref="MustachePromptRenderer.RenderMessages"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using default options
/// var options = MustacheRendererOptions.Default;
///
/// // Using strict mode (case-sensitive, throws on missing)
/// var strictOptions = MustacheRendererOptions.Strict;
///
/// // Custom configuration
/// var customOptions = new MustacheRendererOptions(
///     IgnoreCaseOnKeyLookup: false,
///     ThrowOnMissingVariables: true,
///     FastRenderThresholdMs: 5
/// );
/// </code>
/// </example>
/// <param name="IgnoreCaseOnKeyLookup">
/// Whether variable lookup should be case-insensitive. When <c>true</c>,
/// <c>{{Name}}</c> will match a variable named <c>name</c> or <c>NAME</c>.
/// Default: <c>true</c>.
/// </param>
/// <param name="ThrowOnMissingVariables">
/// Whether <see cref="MustachePromptRenderer.RenderMessages"/> should throw
/// <see cref="Lexichord.Abstractions.Contracts.LLM.TemplateValidationException"/>
/// when required variables are missing. When <c>false</c>, validation must be
/// performed explicitly via <see cref="MustachePromptRenderer.ValidateVariables"/>.
/// Default: <c>true</c>.
/// </param>
/// <param name="FastRenderThresholdMs">
/// Threshold in milliseconds for "fast" rendering. Used for performance monitoring
/// and can inform <see cref="Lexichord.Abstractions.Contracts.LLM.RenderedPrompt.WasFastRender"/>.
/// Default: <c>10</c>.
/// </param>
public record MustacheRendererOptions
{
    /// <summary>
    /// Gets or initializes a value indicating whether variable lookup should be case-insensitive.
    /// </summary>
    public bool IgnoreCaseOnKeyLookup { get; init; } = true;

    /// <summary>
    /// Gets or initializes a value indicating whether <see cref="MustachePromptRenderer.RenderMessages"/>
    /// should throw when required variables are missing.
    /// </summary>
    public bool ThrowOnMissingVariables { get; init; } = true;

    /// <summary>
    /// Gets or initializes the threshold in milliseconds for "fast" rendering.
    /// </summary>
    public int FastRenderThresholdMs { get; init; } = 10;

    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    /// <remarks>
    /// Use this constant when binding configuration:
    /// <code>
    /// services.Configure&lt;MustacheRendererOptions&gt;(
    ///     configuration.GetSection(MustacheRendererOptions.SectionName));
    /// </code>
    /// </remarks>
    public const string SectionName = "Agents:MustacheRenderer";

    /// <summary>
    /// Gets the default renderer options.
    /// </summary>
    /// <remarks>
    /// Default settings:
    /// <list type="bullet">
    ///   <item><description><see cref="IgnoreCaseOnKeyLookup"/>: <c>true</c></description></item>
    ///   <item><description><see cref="ThrowOnMissingVariables"/>: <c>true</c></description></item>
    ///   <item><description><see cref="FastRenderThresholdMs"/>: <c>10</c></description></item>
    /// </list>
    /// </remarks>
    public static MustacheRendererOptions Default { get; } = new();

    /// <summary>
    /// Gets options configured for strict mode.
    /// </summary>
    /// <remarks>
    /// Strict settings:
    /// <list type="bullet">
    ///   <item><description><see cref="IgnoreCaseOnKeyLookup"/>: <c>false</c> (case-sensitive)</description></item>
    ///   <item><description><see cref="ThrowOnMissingVariables"/>: <c>true</c></description></item>
    ///   <item><description><see cref="FastRenderThresholdMs"/>: <c>10</c></description></item>
    /// </list>
    /// Use strict mode when variable names must match exactly and validation
    /// errors should be caught early.
    /// </remarks>
    public static MustacheRendererOptions Strict { get; } = new()
    {
        IgnoreCaseOnKeyLookup = false,
        ThrowOnMissingVariables = true,
        FastRenderThresholdMs = 10
    };

    /// <summary>
    /// Gets options configured for lenient mode.
    /// </summary>
    /// <remarks>
    /// Lenient settings:
    /// <list type="bullet">
    ///   <item><description><see cref="IgnoreCaseOnKeyLookup"/>: <c>true</c> (case-insensitive)</description></item>
    ///   <item><description><see cref="ThrowOnMissingVariables"/>: <c>false</c></description></item>
    ///   <item><description><see cref="FastRenderThresholdMs"/>: <c>10</c></description></item>
    /// </list>
    /// Use lenient mode when missing variables should be silently skipped
    /// and validation is performed separately.
    /// </remarks>
    public static MustacheRendererOptions Lenient { get; } = new()
    {
        IgnoreCaseOnKeyLookup = true,
        ThrowOnMissingVariables = false,
        FastRenderThresholdMs = 10
    };
}
