// =============================================================================
// File: ChunkingStrategyFactory.cs
// Project: Lexichord.Modules.RAG
// Description: Factory for selecting and creating chunking strategies based on
//              mode or content characteristics.
// =============================================================================
// LOGIC: Enables flexible strategy selection and auto-detection.
//   - GetStrategy(mode) returns strategy by explicit ChunkingMode.
//   - GetStrategy(content, extension) uses heuristics for auto-detection.
//   - IServiceProvider enables dependency injection for strategy instances.
//   - Supports extensibility via additional strategy registrations.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Indexing;

/// <summary>
/// Factory for selecting and creating chunking strategies.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ChunkingStrategyFactory"/> provides two key capabilities:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Explicit Selection:</b> Return a strategy for a specific <see cref="ChunkingMode"/>.
///   </description></item>
///   <item><description>
///     <b>Auto-Detection:</b> Analyze content and file extension to recommend the best strategy.
///   </description></item>
/// </list>
/// <para>
/// <b>Strategy Registration:</b> Strategies must be registered in the DI container
/// before this factory can resolve them. Typical registration:
/// </para>
/// <code>
/// services.AddScoped&lt;IChunkingStrategy, FixedSizeChunkingStrategy&gt;();
/// services.AddScoped&lt;IChunkingStrategy, ParagraphChunkingStrategy&gt;();
/// services.AddScoped&lt;IChunkingStrategy, MarkdownHeaderChunkingStrategy&gt;();
/// </code>
/// <para>
/// <b>Auto-Detection Logic:</b> The factory uses heuristics to select the best
/// strategy based on content characteristics and file extension:
/// </para>
/// <list type="bullet">
///   <item><description>Markdown files (.md, .markdown): MarkdownHeaderChunkingStrategy</description></item>
///   <item><description>Content with markdown headers: MarkdownHeaderChunkingStrategy</description></item>
///   <item><description>Prose-like content: ParagraphChunkingStrategy</description></item>
///   <item><description>Code or unstructured: FixedSizeChunkingStrategy</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This factory is stateless and thread-safe.
/// Multiple concurrent calls are supported.
/// </para>
/// </remarks>
public sealed class ChunkingStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChunkingStrategyFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkingStrategyFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The DI service provider for resolving strategy instances.
    /// </param>
    /// <param name="logger">
    /// The logger instance for diagnostic output.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ChunkingStrategyFactory(IServiceProvider serviceProvider, ILogger<ChunkingStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a chunking strategy for the specified mode.
    /// </summary>
    /// <param name="mode">
    /// The chunking mode to select a strategy for.
    /// </param>
    /// <returns>
    /// An <see cref="IChunkingStrategy"/> implementation matching the specified mode.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="mode"/> is <see cref="ChunkingMode.Semantic"/>
    /// (not yet implemented) or when the mode cannot be resolved from DI.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs a linear search through registered strategies to find
    /// one matching the requested mode. The search is O(n) where n is the number
    /// of registered strategies, but this is typically a small constant.
    /// </para>
    /// <para>
    /// <b>Semantic Mode:</b> <see cref="ChunkingMode.Semantic"/> is reserved for
    /// future NLP-based chunking and will throw <see cref="NotSupportedException"/>.
    /// </para>
    /// </remarks>
    public IChunkingStrategy GetStrategy(ChunkingMode mode)
    {
        if (mode == ChunkingMode.Semantic)
        {
            _logger.LogError("Semantic chunking mode is not yet implemented");
            throw new NotSupportedException(
                "Semantic chunking mode is not yet implemented. " +
                "Use FixedSize, Paragraph, or MarkdownHeader instead.");
        }

        // LOGIC: Attempt to resolve all registered strategies and find matching mode.
        var strategies = _serviceProvider.GetService(typeof(IEnumerable<IChunkingStrategy>)) as IEnumerable<IChunkingStrategy>;

        if (strategies != null)
        {
            var strategy = strategies.FirstOrDefault(s => s.Mode == mode);
            if (strategy != null)
            {
                _logger.LogDebug("Resolved strategy for mode {Mode}: {StrategyType}",
                    mode, strategy.GetType().Name);
                return strategy;
            }
        }

        // LOGIC: If enumerable resolution fails, try direct registration by type name.
        // This is a fallback for configurations that don't use IEnumerable<T> patterns.
        var strategyType = mode switch
        {
            ChunkingMode.FixedSize => typeof(FixedSizeChunkingStrategy),
            ChunkingMode.Paragraph => typeof(ParagraphChunkingStrategy),
            ChunkingMode.MarkdownHeader => typeof(MarkdownHeaderChunkingStrategy),
            _ => null
        };

        if (strategyType != null)
        {
            if (_serviceProvider.GetService(strategyType) is IChunkingStrategy directStrategy)
            {
                _logger.LogDebug("Resolved strategy for mode {Mode} via direct type lookup", mode);
                return directStrategy;
            }
        }

        _logger.LogError("Failed to resolve strategy for mode {Mode}", mode);
        throw new InvalidOperationException(
            $"No chunking strategy is registered for mode {mode}. " +
            "Ensure the strategy is registered in the DI container.");
    }

    /// <summary>
    /// Auto-detects the best chunking strategy based on content characteristics and file extension.
    /// </summary>
    /// <param name="content">
    /// The document content to analyze. Must not be null.
    /// </param>
    /// <param name="fileExtension">
    /// The file extension (with or without leading dot) for hint-based detection.
    /// May be null for content-based detection only.
    /// </param>
    /// <returns>
    /// An <see cref="IChunkingStrategy"/> selected based on content analysis and extension.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses a heuristic-based approach to select the best strategy:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     <b>File Extension Hints:</b> If the extension is .md or .markdown,
    ///     MarkdownHeaderChunkingStrategy is returned immediately.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Content Analysis:</b> The content is scanned for structural markers:
    ///     <list type="bullet">
    ///       <item>Markdown headers (#, ##, ###, etc): MarkdownHeaderChunkingStrategy</item>
    ///       <item>Paragraph structure (double newlines): ParagraphChunkingStrategy</item>
    ///       <item>Default fallback: FixedSizeChunkingStrategy</item>
    ///     </list>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Performance Note:</b> Content analysis scans the first 10KB of content
    /// to avoid expensive analysis of very large documents. For more precise detection,
    /// call <see cref="GetStrategy(ChunkingMode)"/> with an explicit mode.
    /// </para>
    /// </remarks>
    public IChunkingStrategy GetStrategy(string content, string? fileExtension)
    {
        ArgumentNullException.ThrowIfNull(content);

        _logger.LogDebug("Auto-detecting chunking strategy for content ({ContentLength} chars, extension={Extension})",
            content.Length, fileExtension ?? "none");

        // LOGIC: Extension-based hint has highest priority.
        if (!string.IsNullOrWhiteSpace(fileExtension))
        {
            var ext = fileExtension.TrimStart('.').ToLowerInvariant();
            if (ext == "md" || ext == "markdown")
            {
                _logger.LogDebug("Detected Markdown file extension, using MarkdownHeaderChunkingStrategy");
                return GetStrategy(ChunkingMode.MarkdownHeader);
            }
        }

        // LOGIC: Content-based analysis. Sample the first 10KB to avoid expensive
        // analysis on very large files.
        var sampleSize = Math.Min(10000, content.Length);
        var sample = content[..sampleSize];

        // LOGIC: Check for Markdown headers (most specific structure).
        if (sample.Contains("#"))
        {
            _logger.LogDebug("Detected Markdown headers in content, using MarkdownHeaderChunkingStrategy");
            return GetStrategy(ChunkingMode.MarkdownHeader);
        }

        // LOGIC: Check for paragraph structure (double newlines).
        if (sample.Contains("\n\n"))
        {
            _logger.LogDebug("Detected paragraph structure in content, using ParagraphChunkingStrategy");
            return GetStrategy(ChunkingMode.Paragraph);
        }

        // LOGIC: Default fallback for unstructured or code content.
        _logger.LogDebug("No specific structure detected, using FixedSizeChunkingStrategy as fallback");
        return GetStrategy(ChunkingMode.FixedSize);
    }
}
