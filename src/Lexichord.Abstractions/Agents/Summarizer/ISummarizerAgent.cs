// -----------------------------------------------------------------------
// <copyright file="ISummarizerAgent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Specialized agent interface for document summarization (v0.7.6a).
//   Extends IAgent with summarization-specific operations:
//     - SummarizeAsync: Summarize a document from a file path
//     - SummarizeContentAsync: Summarize provided text content directly
//     - ParseCommand: Parse natural language into SummarizationOptions
//     - GetDefaultOptions: Get mode-specific default options
//
//   DI registration pattern:
//     services.AddSingleton<ISummarizerAgent, SummarizerAgent>();
//     services.AddSingleton<IAgent>(sp => (IAgent)sp.GetRequiredService<ISummarizerAgent>());
//
//   This dual registration makes the agent discoverable both:
//     - As ISummarizerAgent for command handlers needing typed access
//     - As IAgent for the IAgentRegistry discovery mechanism
//
//   License Requirements:
//     The implementing SummarizerAgent requires WriterPro tier or higher.
//
//   Introduced in: v0.7.6a
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Summarizer;

/// <summary>
/// Specialized agent interface for document summarization.
/// Extends base <see cref="IAgent"/> with summarization-specific operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ISummarizerAgent"/> defines the core summarization operations
/// implemented by the SummarizerAgent (v0.7.6a). It extends <see cref="IAgent"/> so the
/// implementation can be registered both as a typed summarization service and as a
/// discoverable agent via <see cref="IAgentRegistry"/>.
/// </para>
/// <para>
/// <b>Operations:</b>
/// <list type="bullet">
///   <item><description><see cref="SummarizeAsync"/>: Summarize a document at a file path</description></item>
///   <item><description><see cref="SummarizeContentAsync"/>: Summarize provided text content directly</description></item>
///   <item><description><see cref="ParseCommand"/>: Parse a natural language command into options</description></item>
///   <item><description><see cref="GetDefaultOptions"/>: Get mode-specific default options</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// The implementing SummarizerAgent requires <see cref="Abstractions.Contracts.LicenseTier.WriterPro"/>
/// tier or higher. Unlicensed users receive a "Upgrade to WriterPro" message.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6a as part of the Summarizer Agent Summarization Modes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Summarize from file path
/// var agent = serviceProvider.GetRequiredService&lt;ISummarizerAgent&gt;();
/// var options = agent.ParseCommand("Summarize in 5 bullets for developers");
/// var result = await agent.SummarizeAsync("/path/to/document.md", options, ct);
///
/// if (result.Success)
/// {
///     Console.WriteLine(result.Summary);
///     Console.WriteLine($"Compression: {result.CompressionRatio:F1}x");
/// }
///
/// // Summarize content directly
/// var contentResult = await agent.SummarizeContentAsync(selectedText, options, ct);
/// </code>
/// </example>
/// <seealso cref="SummarizationMode"/>
/// <seealso cref="SummarizationOptions"/>
/// <seealso cref="SummarizationResult"/>
/// <seealso cref="IAgent"/>
public interface ISummarizerAgent : IAgent
{
    /// <summary>
    /// Generates a summary of the document at the specified path.
    /// </summary>
    /// <param name="documentPath">Absolute path to the document to summarize.</param>
    /// <param name="options">Summarization configuration options.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the
    /// <see cref="SummarizationResult"/> with the generated summary and metadata.
    /// </returns>
    /// <exception cref="FileNotFoundException">Document not found at <paramref name="documentPath"/>.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Reads the document content via <c>IFileService.LoadAsync</c> and
    /// delegates to <see cref="SummarizeContentAsync"/>. The document path is used for
    /// context assembly and event publishing.
    /// </para>
    /// <para>
    /// <b>Error Handling:</b> Returns a failed <see cref="SummarizationResult"/> for
    /// runtime errors (timeouts, LLM failures, cancellation) instead of throwing.
    /// </para>
    /// </remarks>
    Task<SummarizationResult> SummarizeAsync(
        string documentPath,
        SummarizationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a summary of the provided content directly.
    /// Use for selections, excerpts, or content not stored in files.
    /// </summary>
    /// <param name="content">Text content to summarize.</param>
    /// <param name="options">Summarization configuration options.</param>
    /// <param name="ct">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the
    /// <see cref="SummarizationResult"/> with the generated summary and metadata.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This is the primary summarization entry point. The flow is:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validate options via <see cref="SummarizationOptions.Validate"/></description></item>
    ///   <item><description>Publish <c>SummarizationStartedEvent</c></description></item>
    ///   <item><description>Estimate tokens and chunk if needed (4000 tokens/chunk)</description></item>
    ///   <item><description>Assemble context via <c>IContextOrchestrator</c></description></item>
    ///   <item><description>Render mode-specific prompt via <c>IPromptRenderer</c></description></item>
    ///   <item><description>Invoke LLM via <c>IChatCompletionService.CompleteAsync</c></description></item>
    ///   <item><description>Parse response and calculate metrics</description></item>
    ///   <item><description>Publish <c>SummarizationCompletedEvent</c> or <c>SummarizationFailedEvent</c></description></item>
    /// </list>
    /// <para>
    /// <b>Error Handling:</b> Returns a failed <see cref="SummarizationResult"/> for
    /// runtime errors instead of throwing.
    /// </para>
    /// </remarks>
    Task<SummarizationResult> SummarizeContentAsync(
        string content,
        SummarizationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Parses a natural language command and returns appropriate summarization options.
    /// </summary>
    /// <param name="naturalLanguageCommand">User's natural language request.</param>
    /// <returns>
    /// Parsed <see cref="SummarizationOptions"/> matching the user's intent.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses regex-based keyword detection to infer the mode:
    /// <list type="bullet">
    ///   <item><description>"abstract" → <see cref="SummarizationMode.Abstract"/></description></item>
    ///   <item><description>"tldr", "tl;dr" → <see cref="SummarizationMode.TLDR"/></description></item>
    ///   <item><description>"executive", "stakeholder", "management" → <see cref="SummarizationMode.Executive"/></description></item>
    ///   <item><description>"takeaway", "insight", "learning" → <see cref="SummarizationMode.KeyTakeaways"/></description></item>
    ///   <item><description>"bullet", "point" → <see cref="SummarizationMode.BulletPoints"/></description></item>
    ///   <item><description>Default → <see cref="SummarizationMode.BulletPoints"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Additionally extracts numeric values for <see cref="SummarizationOptions.MaxItems"/>
    /// and audience phrases for <see cref="SummarizationOptions.TargetAudience"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// agent.ParseCommand("Summarize in 3 bullets")
    ///   // → Mode = BulletPoints, MaxItems = 3
    ///
    /// agent.ParseCommand("Create an abstract")
    ///   // → Mode = Abstract
    ///
    /// agent.ParseCommand("TLDR for developers")
    ///   // → Mode = TLDR, TargetAudience = "developers"
    /// </code>
    /// </example>
    SummarizationOptions ParseCommand(string naturalLanguageCommand);

    /// <summary>
    /// Gets the default options for a given mode.
    /// </summary>
    /// <param name="mode">The summarization mode.</param>
    /// <returns>
    /// Default <see cref="SummarizationOptions"/> optimized for the specified mode.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Returns mode-specific defaults:
    /// <list type="bullet">
    ///   <item><description><see cref="SummarizationMode.Abstract"/>: TargetWordCount = 200</description></item>
    ///   <item><description><see cref="SummarizationMode.TLDR"/>: TargetWordCount = 75</description></item>
    ///   <item><description><see cref="SummarizationMode.BulletPoints"/>: MaxItems = 5</description></item>
    ///   <item><description><see cref="SummarizationMode.KeyTakeaways"/>: MaxItems = 5</description></item>
    ///   <item><description><see cref="SummarizationMode.Executive"/>: TargetWordCount = 150</description></item>
    ///   <item><description><see cref="SummarizationMode.Custom"/>: Requires CustomPrompt to be set</description></item>
    /// </list>
    /// </remarks>
    SummarizationOptions GetDefaultOptions(SummarizationMode mode);
}
