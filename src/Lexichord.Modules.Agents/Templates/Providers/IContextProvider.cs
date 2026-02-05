// -----------------------------------------------------------------------
// <copyright file="IContextProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;

namespace Lexichord.Modules.Agents.Templates.Providers;

/// <summary>
/// Defines a provider that supplies context variables for prompt template rendering.
/// </summary>
/// <remarks>
/// <para>
/// Context providers are modular components that contribute variables to the context
/// dictionary used by <see cref="IContextInjector"/> during prompt assembly. Each provider
/// is responsible for a specific domain of context (documents, style rules, RAG, etc.).
/// </para>
/// <para>
/// <strong>Provider Execution Model:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Providers execute in parallel via <c>Task.WhenAll()</c>.</description></item>
///   <item><description>Each provider has an individual timeout to prevent blocking.</description></item>
///   <item><description>Provider failures are isolated and do not fail the entire request.</description></item>
///   <item><description>Results are merged in priority order (lower priority first).</description></item>
/// </list>
/// <para>
/// <strong>Priority System:</strong>
/// Providers with lower priority values execute first and their results may be
/// overwritten by higher-priority providers. This allows providers to layer
/// context appropriately.
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Priority</term>
///     <description>Provider Type</description>
///   </listheader>
///   <item>
///     <term>50</term>
///     <description>Document context (foundational)</description>
///   </item>
///   <item>
///     <term>100</term>
///     <description>Style rules (enhancing)</description>
///   </item>
///   <item>
///     <term>200</term>
///     <description>RAG context (may override)</description>
///   </item>
/// </list>
/// <para>
/// <strong>License Gating:</strong>
/// Providers may require specific license features via <see cref="RequiredLicenseFeature"/>.
/// When a feature is not enabled, the provider is skipped silently during context assembly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyContextProvider : IContextProvider
/// {
///     public string ProviderName => "MyProvider";
///     public int Priority => 150;
///     public string? RequiredLicenseFeature => "Feature.MyFeature";
///
///     public bool IsEnabled(ContextRequest request)
///     {
///         return request.IncludeMyData;
///     }
///
///     public async Task&lt;ContextResult&gt; GetContextAsync(
///         ContextRequest request,
///         CancellationToken ct)
///     {
///         var data = new Dictionary&lt;string, object&gt;
///         {
///             ["my_variable"] = "value"
///         };
///         return ContextResult.Ok(ProviderName, data, TimeSpan.FromMilliseconds(5));
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ContextResult"/>
/// <seealso cref="IContextInjector"/>
/// <seealso cref="ContextRequest"/>
public interface IContextProvider
{
    /// <summary>
    /// Gets the unique name of this provider for logging and diagnostics.
    /// </summary>
    /// <value>
    /// A string identifying this provider, such as "Document", "StyleRules", or "RAG".
    /// </value>
    /// <remarks>
    /// LOGIC: The provider name is used in log messages and the <see cref="ContextResult"/>
    /// to identify which provider produced specific context or errors.
    /// </remarks>
    string ProviderName { get; }

    /// <summary>
    /// Gets the execution priority of this provider.
    /// </summary>
    /// <value>
    /// An integer where lower values indicate earlier processing in the merge order.
    /// Typical values: 50 (foundational), 100 (enhancing), 200 (overriding).
    /// </value>
    /// <remarks>
    /// LOGIC: When multiple providers produce the same variable name, the provider
    /// with the higher priority value wins. All providers execute in parallel,
    /// but results are merged in priority order.
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets the license feature code required to use this provider, if any.
    /// </summary>
    /// <value>
    /// A feature code string (e.g., "Feature.RAGContext") or <c>null</c> if no license is required.
    /// </value>
    /// <remarks>
    /// LOGIC: When this value is non-null, the <see cref="IContextInjector"/> checks
    /// <see cref="Abstractions.Contracts.ILicenseContext.IsFeatureEnabled"/> before
    /// executing this provider. If the feature is not enabled, the provider is
    /// skipped silently with a debug log message.
    /// </remarks>
    string? RequiredLicenseFeature { get; }

    /// <summary>
    /// Determines whether this provider should execute for the given request.
    /// </summary>
    /// <param name="request">The context request containing configuration flags.</param>
    /// <returns>
    /// <c>true</c> if this provider should execute; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method performs a fast synchronous check before async execution.
    /// Providers should check the relevant flags in the <see cref="ContextRequest"/>
    /// to determine if they should contribute context.
    /// </para>
    /// <para>
    /// Examples:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>DocumentContextProvider</c> checks if document path is present.</description></item>
    ///   <item><description><c>StyleRulesContextProvider</c> checks <c>IncludeStyleRules</c>.</description></item>
    ///   <item><description><c>RAGContextProvider</c> checks <c>IncludeRAGContext</c>.</description></item>
    /// </list>
    /// </remarks>
    bool IsEnabled(ContextRequest request);

    /// <summary>
    /// Asynchronously retrieves context variables from this provider.
    /// </summary>
    /// <param name="request">The context request containing document state and configuration.</param>
    /// <param name="ct">A cancellation token to observe for cancellation requests.</param>
    /// <returns>
    /// A task that resolves to a <see cref="ContextResult"/> containing either
    /// the context data or error information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This method executes within an individual timeout managed by the
    /// <see cref="IContextInjector"/>. Implementations should:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Observe the <paramref name="ct"/> cancellation token.</description></item>
    ///   <item><description>Return <see cref="ContextResult.Empty"/> when no data is available.</description></item>
    ///   <item><description>Return <see cref="ContextResult.Error"/> on recoverable failures.</description></item>
    ///   <item><description>Allow <see cref="OperationCanceledException"/> to propagate on cancellation.</description></item>
    /// </list>
    /// <para>
    /// <strong>Variable Naming Convention:</strong>
    /// Use snake_case for all variable names (e.g., <c>document_path</c>, <c>style_rules</c>).
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<ContextResult> GetContextAsync(ContextRequest request, CancellationToken ct);
}
