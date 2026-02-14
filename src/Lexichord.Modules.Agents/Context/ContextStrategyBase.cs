// -----------------------------------------------------------------------
// <copyright file="ContextStrategyBase.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// Base class for context strategies providing common functionality and helper methods.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides:
/// </para>
/// <list type="bullet">
///   <item><description><b>Token Counting:</b> Via injected <see cref="ITokenCounter"/> with helper methods</description></item>
///   <item><description><b>Logging:</b> Via injected <see cref="ILogger"/> for diagnostic output</description></item>
///   <item><description><b>Fragment Creation:</b> Automatic token estimation via <see cref="CreateFragment"/></description></item>
///   <item><description><b>Content Truncation:</b> Smart paragraph-aware truncation via <see cref="TruncateToMaxTokens"/></description></item>
///   <item><description><b>Request Validation:</b> Prerequisite checking via <see cref="ValidateRequest"/></description></item>
/// </list>
/// <para>
/// <strong>Usage Pattern:</strong>
/// Derive from this class and implement the abstract members (<see cref="StrategyId"/>,
/// <see cref="DisplayName"/>, <see cref="Priority"/>, <see cref="MaxTokens"/>, and
/// <see cref="GatherAsync"/>). Use the provided helper methods in your <see cref="GatherAsync"/>
/// implementation to simplify token counting, content truncation, and validation.
/// </para>
/// <para>
/// <strong>Dependency Injection:</strong>
/// The base class requires <see cref="ITokenCounter"/> and <see cref="ILogger"/> to be
/// injected via the protected constructor. Derived classes must call the base constructor
/// and may inject additional dependencies as needed.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2a as part of the Context Strategy Interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocumentContextStrategy : ContextStrategyBase
/// {
///     private readonly IFileService _fileService;
///
///     public DocumentContextStrategy(
///         ITokenCounter tokenCounter,
///         IFileService fileService,
///         ILogger&lt;DocumentContextStrategy&gt; logger)
///         : base(tokenCounter, logger)
///     {
///         _fileService = fileService;
///     }
///
///     public override string StrategyId => "document";
///     public override string DisplayName => "Document Content";
///     public override int Priority => StrategyPriority.Critical;
///     public override int MaxTokens => 4000;
///
///     public override async Task&lt;ContextFragment?&gt; GatherAsync(
///         ContextGatheringRequest request,
///         CancellationToken ct)
///     {
///         // Use base class validation helper
///         if (!ValidateRequest(request, requireDocument: true))
///             return null;
///
///         // Read document content
///         var content = await _fileService.ReadAllTextAsync(request.DocumentPath!, ct);
///
///         // Use base class truncation helper
///         var truncated = TruncateToMaxTokens(content);
///
///         // Use base class fragment creation helper (with automatic token counting)
///         return CreateFragment(truncated, relevance: 1.0f);
///     }
/// }
/// </code>
/// </example>
public abstract class ContextStrategyBase : IContextStrategy
{
    /// <summary>
    /// Token counter for estimating fragment sizes.
    /// </summary>
    protected readonly ITokenCounter _tokenCounter;

    /// <summary>
    /// Logger for diagnostic messages.
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextStrategyBase"/> class.
    /// </summary>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="tokenCounter"/> or <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Protected constructor ensures derived classes explicitly handle
    /// dependency injection. Both parameters are required because all strategies
    /// need token counting (for <see cref="MaxTokens"/> enforcement) and logging
    /// (for debugging and error reporting).
    /// </remarks>
    protected ContextStrategyBase(
        ITokenCounter tokenCounter,
        ILogger logger)
    {
        _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public abstract string StrategyId { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public abstract int Priority { get; }

    /// <inheritdoc />
    public abstract int MaxTokens { get; }

    /// <inheritdoc />
    public abstract Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct);

    /// <summary>
    /// Creates a fragment with automatic token estimation and logging.
    /// </summary>
    /// <param name="content">The content to include in the fragment.</param>
    /// <param name="relevance">Relevance score from 0.0 to 1.0 (default: 1.0).</param>
    /// <param name="customLabel">Optional custom label (default: uses <see cref="DisplayName"/>).</param>
    /// <returns>A new <see cref="ContextFragment"/> with estimated token count.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This helper method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Counts tokens using the injected <see cref="_tokenCounter"/></description></item>
    ///   <item><description>Logs the fragment creation at Debug level for diagnostics</description></item>
    ///   <item><description>Creates a fragment with the strategy's <see cref="StrategyId"/> and <see cref="DisplayName"/></description></item>
    /// </list>
    /// <para>
    /// <strong>Performance:</strong>
    /// Token counting is performed once at fragment creation. The result is
    /// cached in the fragment's <see cref="ContextFragment.TokenEstimate"/>
    /// property to avoid redundant calculations during budget management.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Creating a fragment with default relevance (1.0)
    /// var fragment = CreateFragment(documentContent);
    ///
    /// // Creating a fragment with custom relevance
    /// var ragFragment = CreateFragment(searchResults, relevance: 0.75f);
    ///
    /// // Creating a fragment with custom label
    /// var headingFragment = CreateFragment(
    ///     headingText,
    ///     relevance: 0.6f,
    ///     customLabel: "Section: Introduction");
    ///
    /// // Debug log output:
    /// // "document creating fragment with 1234 tokens"
    /// </code>
    /// </example>
    protected ContextFragment CreateFragment(
        string content,
        float relevance = 1.0f,
        string? customLabel = null)
    {
        // LOGIC: Count tokens once at creation time
        var tokens = _tokenCounter.CountTokens(content);

        // LOGIC: Log fragment creation for debugging and performance monitoring
        _logger.LogDebug(
            "{Strategy} creating fragment with {Tokens} tokens",
            StrategyId, tokens);

        // LOGIC: Create fragment with strategy metadata
        return new ContextFragment(
            SourceId: StrategyId,
            Label: customLabel ?? DisplayName,
            Content: content,
            TokenEstimate: tokens,
            Relevance: relevance);
    }

    /// <summary>
    /// Truncates content to fit <see cref="MaxTokens"/>, preferring paragraph boundaries.
    /// </summary>
    /// <param name="content">The content to truncate.</param>
    /// <returns>
    /// The original content if it fits within <see cref="MaxTokens"/>;
    /// otherwise, truncated content that respects paragraph boundaries.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Smart truncation that:
    /// </para>
    /// <list type="number">
    ///   <item><description>Returns original content if already within limit (early return optimization)</description></item>
    ///   <item><description>Splits on paragraph boundaries (double newline) for readability</description></item>
    ///   <item><description>Includes complete paragraphs up to the token limit</description></item>
    ///   <item><description>Logs a warning when truncation occurs for diagnostics</description></item>
    /// </list>
    /// <para>
    /// <strong>Truncation Strategy:</strong>
    /// Paragraph-aware truncation preserves document structure and produces more
    /// coherent truncated content than character-based truncation. This is especially
    /// important for Markdown documents where paragraph breaks are semantically meaningful.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong>
    /// The method performs token counting twice in the worst case:
    /// once for the full content (to detect over-limit),
    /// and again for each paragraph during truncation.
    /// For content within limits, only one count is performed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Content within limit - returned unchanged
    /// var shortContent = "Brief paragraph.";
    /// var result1 = TruncateToMaxTokens(shortContent); // No truncation
    ///
    /// // Content over limit - truncated at paragraph boundary
    /// var longContent = "Para 1...\n\nPara 2...\n\nPara 3...";
    /// var result2 = TruncateToMaxTokens(longContent); // Returns "Para 1...\n\nPara 2..."
    ///
    /// // Warning logged:
    /// // "document content (5000 tokens) exceeds max (4000), truncating"
    /// </code>
    /// </example>
    protected string TruncateToMaxTokens(string content)
    {
        // LOGIC: Early return if content already fits (avoid unnecessary work)
        var tokens = _tokenCounter.CountTokens(content);
        if (tokens <= MaxTokens) return content;

        // LOGIC: Log warning when truncation is necessary
        _logger.LogWarning(
            "{Strategy} content ({Tokens} tokens) exceeds max ({Max}), truncating",
            StrategyId, tokens, MaxTokens);

        // LOGIC: Smart truncation keeps complete paragraphs for readability
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        var currentTokens = 0;

        foreach (var para in paragraphs)
        {
            // LOGIC: Calculate tokens for this paragraph
            var paraTokens = _tokenCounter.CountTokens(para);

            // LOGIC: Stop if adding this paragraph would exceed limit
            if (currentTokens + paraTokens > MaxTokens)
                break;

            // LOGIC: Add paragraph with separator (if not first paragraph)
            if (result.Length > 0) result.Append("\n\n");
            result.Append(para);
            currentTokens += paraTokens;
        }

        return result.ToString();
    }

    /// <summary>
    /// Verifies that the request has the required properties for this strategy.
    /// </summary>
    /// <param name="request">The context gathering request to validate.</param>
    /// <param name="requireDocument">Whether a document path is required (default: false).</param>
    /// <param name="requireSelection">Whether selected text is required (default: false).</param>
    /// <param name="requireCursor">Whether cursor position is required (default: false).</param>
    /// <returns>
    /// <c>true</c> if all required properties are present; otherwise, <c>false</c>.
    /// Debug messages are logged for missing properties.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: This helper allows strategies to quickly check prerequisites
    /// and return <c>null</c> from <see cref="GatherAsync"/> if requirements
    /// aren't met. Debug logging helps with troubleshooting without cluttering
    /// production logs with expected conditions.
    /// </para>
    /// <para>
    /// <strong>Design Rationale:</strong>
    /// Separating validation into a helper method keeps <see cref="GatherAsync"/>
    /// implementations clean and focuses them on the core gathering logic.
    /// The method returns <c>false</c> (rather than throwing) because missing
    /// prerequisites are normal, expected conditions - not errors.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override async Task&lt;ContextFragment?&gt; GatherAsync(
    ///     ContextGatheringRequest request,
    ///     CancellationToken ct)
    /// {
    ///     // Document context strategy requires a document
    ///     if (!ValidateRequest(request, requireDocument: true))
    ///         return null;
    ///
    ///     // Proceed with gathering...
    /// }
    ///
    /// public override async Task&lt;ContextFragment?&gt; GatherAsync(
    ///     ContextGatheringRequest request,
    ///     CancellationToken ct)
    /// {
    ///     // Selection context strategy requires both document and selection
    ///     if (!ValidateRequest(request, requireDocument: true, requireSelection: true))
    ///         return null;
    ///
    ///     // Proceed with gathering...
    /// }
    ///
    /// // Debug log output when prerequisite missing:
    /// // "document: No document available"
    /// </code>
    /// </example>
    protected bool ValidateRequest(
        ContextGatheringRequest request,
        bool requireDocument = false,
        bool requireSelection = false,
        bool requireCursor = false)
    {
        // LOGIC: Check document requirement
        if (requireDocument && !request.HasDocument)
        {
            _logger.LogDebug("{Strategy}: No document available", StrategyId);
            return false;
        }

        // LOGIC: Check selection requirement
        if (requireSelection && !request.HasSelection)
        {
            _logger.LogDebug("{Strategy}: No selection available", StrategyId);
            return false;
        }

        // LOGIC: Check cursor requirement
        if (requireCursor && !request.HasCursor)
        {
            _logger.LogDebug("{Strategy}: No cursor position available", StrategyId);
            return false;
        }

        return true;
    }
}
