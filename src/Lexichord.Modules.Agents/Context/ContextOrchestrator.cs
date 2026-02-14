// -----------------------------------------------------------------------
// <copyright file="ContextOrchestrator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// Default implementation of <see cref="IContextOrchestrator"/>.
/// Coordinates parallel strategy execution, content deduplication,
/// priority-based sorting, token budget enforcement, and event publishing.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator follows a pipeline model:
/// </para>
/// <list type="number">
///   <item><description>Filter strategies by enabled state, budget exclusions, and license tier</description></item>
///   <item><description>Execute all eligible strategies in parallel with per-strategy timeout</description></item>
///   <item><description>Collect non-null fragments with meaningful content</description></item>
///   <item><description>Deduplicate fragments using Jaccard word-set similarity</description></item>
///   <item><description>Sort by orchestrator-level priority (descending) then relevance (descending)</description></item>
///   <item><description>Trim to fit token budget, with smart truncation for partially-fitting fragments</description></item>
///   <item><description>Extract template variables from request and fragments</description></item>
///   <item><description>Publish <see cref="ContextAssembledEvent"/> for observability</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// This class is thread-safe. Strategy enable/disable state is managed via
/// <see cref="ConcurrentDictionary{TKey, TValue}"/>. Parallel strategy execution
/// uses <see cref="Parallel.ForEachAsync"/> with <see cref="ConcurrentBag{T}"/>
/// for result collection.
/// </para>
/// <para>
/// <strong>Lifetime:</strong>
/// Registered as a singleton in DI. The <see cref="_strategyEnabled"/> state
/// persists for the application lifetime.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Assembler system.
/// </para>
/// </remarks>
/// <seealso cref="IContextOrchestrator"/>
/// <seealso cref="IContextStrategyFactory"/>
/// <seealso cref="AssembledContext"/>
public sealed class ContextOrchestrator : IContextOrchestrator
{
    private readonly IContextStrategyFactory _factory;
    private readonly ITokenCounter _tokenCounter;
    private readonly IMediator _mediator;
    private readonly ContextOptions _options;
    private readonly ILogger<ContextOrchestrator> _logger;

    /// <summary>
    /// Tracks which strategies are enabled/disabled at runtime.
    /// Key: strategy ID, Value: true = enabled.
    /// Strategies not in this dictionary default to enabled.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _strategyEnabled = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextOrchestrator"/> class.
    /// </summary>
    /// <param name="factory">
    /// Factory for creating context strategies filtered by the current license tier.
    /// </param>
    /// <param name="tokenCounter">
    /// Token counter for estimating and enforcing token budgets during trimming.
    /// </param>
    /// <param name="mediator">
    /// MediatR mediator for publishing <see cref="ContextAssembledEvent"/> and
    /// <see cref="StrategyToggleEvent"/> notifications.
    /// </param>
    /// <param name="options">
    /// Configuration options controlling timeout, deduplication, and parallelism.
    /// </param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    public ContextOrchestrator(
        IContextStrategyFactory factory,
        ITokenCounter tokenCounter,
        IMediator mediator,
        IOptions<ContextOptions> options,
        ILogger<ContextOrchestrator> logger)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(tokenCounter);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _factory = factory;
        _tokenCounter = tokenCounter;
        _mediator = mediator;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AssembledContext> AssembleAsync(
        ContextGatheringRequest request,
        ContextBudget budget,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting context assembly for agent {AgentId} with budget {Budget} tokens",
            request.AgentId, budget.MaxTokens);

        try
        {
            // LOGIC: Step 1 — Get all strategies that should execute for this request.
            // Filters by: user-enabled state AND budget exclusion rules AND license tier.
            var strategies = GetEnabledStrategies(budget);

            if (strategies.Count == 0)
            {
                _logger.LogWarning("No strategies available for context assembly");
                return AssembledContext.Empty;
            }

            _logger.LogDebug(
                "Executing {Count} strategies in parallel",
                strategies.Count);

            // LOGIC: Step 2 — Execute all strategies concurrently with per-strategy timeout.
            // Each strategy gets a linked CancellationTokenSource that cancels after StrategyTimeout.
            var fragments = await ExecuteStrategiesAsync(strategies, request, ct);

            if (fragments.Count == 0)
            {
                _logger.LogInformation("No context fragments gathered");
                return AssembledContext.Empty;
            }

            // LOGIC: Step 3 — Deduplicate overlapping content if enabled.
            // Uses Jaccard word-set similarity via ContentDeduplicator.
            if (_options.EnableDeduplication)
            {
                fragments = DeduplicateFragments(fragments);
            }

            // LOGIC: Step 4 — Sort by orchestrator-level priority (descending),
            // then by relevance score (descending) for equal-priority fragments.
            fragments = SortFragments(fragments);

            // LOGIC: Step 5 — Trim to fit within token budget.
            // Iterates from highest to lowest priority, truncating or dropping fragments.
            fragments = TrimToFitBudget(fragments, budget.MaxTokens);

            // LOGIC: Step 6 — Extract template variables from the request and fragments.
            // These variables can be used in prompt rendering (e.g., {{DocumentName}}).
            var variables = ExtractVariables(request, fragments);

            // LOGIC: Step 7 — Build the final result.
            var totalTokens = fragments.Sum(f => f.TokenEstimate);
            sw.Stop();

            var result = new AssembledContext(
                fragments,
                totalTokens,
                variables,
                sw.Elapsed);

            _logger.LogInformation(
                "Context assembly completed: {FragmentCount} fragments, {Tokens} tokens, {Duration}ms",
                fragments.Count, totalTokens, sw.ElapsedMilliseconds);

            // LOGIC: Step 8 — Publish event for UI and telemetry.
            // Fire-and-forget: handler failures do not affect the assembly result.
            await PublishEventAsync(request.AgentId, result, ct);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Context assembly failed for agent {AgentId}", request.AgentId);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextStrategy> GetStrategies()
    {
        // LOGIC: Delegate to factory which filters by current license tier.
        return _factory.CreateAllStrategies();
    }

    /// <inheritdoc />
    public void SetStrategyEnabled(string strategyId, bool enabled)
    {
        _strategyEnabled[strategyId] = enabled;

        _logger.LogDebug(
            "Strategy {StrategyId} {Status}",
            strategyId,
            enabled ? "enabled" : "disabled");

        // LOGIC: Publish toggle event for UI synchronization (fire-and-forget).
        // Matching the AgentRegistry pattern for event publishing.
        _ = _mediator.Publish(new StrategyToggleEvent(strategyId, enabled), default);
    }

    /// <inheritdoc />
    public bool IsStrategyEnabled(string strategyId)
    {
        // LOGIC: Return true (enabled) by default for strategies that have
        // never been explicitly toggled. This ensures all strategies participate
        // in assembly unless the user explicitly disables them.
        return _strategyEnabled.GetValueOrDefault(strategyId, true);
    }

    /// <summary>
    /// Filters strategies to only those that should execute for this request.
    /// </summary>
    /// <param name="budget">Budget constraints with exclusion and requirement rules.</param>
    /// <returns>List of strategies that passed all filters.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: A strategy is eligible if ALL of the following are true:
    /// </para>
    /// <list type="number">
    ///   <item><description>It is available for the current license tier (via factory)</description></item>
    ///   <item><description>It is enabled (not explicitly disabled via <see cref="SetStrategyEnabled"/>)</description></item>
    ///   <item><description>It is not in the budget's excluded strategies list</description></item>
    /// </list>
    /// </remarks>
    private IReadOnlyList<IContextStrategy> GetEnabledStrategies(ContextBudget budget)
    {
        return _factory.CreateAllStrategies()
            .Where(s => IsStrategyEnabled(s.StrategyId))
            .Where(s => budget.ShouldExecute(s.StrategyId))
            .ToList();
    }

    /// <summary>
    /// Executes all strategies in parallel with per-strategy timeout.
    /// </summary>
    /// <param name="strategies">Strategies to execute.</param>
    /// <param name="request">Context gathering request parameters.</param>
    /// <param name="ct">Top-level cancellation token.</param>
    /// <returns>List of non-null, non-empty fragments from all strategies.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Each strategy executes with its own linked cancellation token that
    /// cancels after <see cref="ContextOptions.StrategyTimeout"/> milliseconds.
    /// Strategy failures are isolated:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="OperationCanceledException"/>: Logged as warning (timeout)</description></item>
    ///   <item><description>Other exceptions: Logged as error (strategy failure)</description></item>
    /// </list>
    /// <para>
    /// Results are collected in a <see cref="ConcurrentBag{T}"/> for thread-safe
    /// accumulation during parallel execution.
    /// </para>
    /// </remarks>
    private async Task<List<ContextFragment>> ExecuteStrategiesAsync(
        IReadOnlyList<IContextStrategy> strategies,
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        var results = new ConcurrentBag<ContextFragment>();
        var timeout = TimeSpan.FromMilliseconds(_options.StrategyTimeout);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(strategies, parallelOptions, async (strategy, token) =>
        {
            try
            {
                // LOGIC: Create a linked token that cancels after the configured timeout.
                // This prevents slow strategies from blocking the entire assembly.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(timeout);

                _logger.LogDebug(
                    "{StrategyId} gathering context for {DocumentPath}",
                    strategy.StrategyId, request.DocumentPath);

                var fragment = await strategy.GatherAsync(request, cts.Token);

                if (fragment is not null && fragment.HasContent)
                {
                    results.Add(fragment);

                    _logger.LogInformation(
                        "{StrategyId} returned {Tokens} tokens (relevance: {Relevance})",
                        strategy.StrategyId, fragment.TokenEstimate, fragment.Relevance);
                }
            }
            catch (OperationCanceledException)
            {
                // LOGIC: Strategy timed out — log warning but continue with other strategies.
                _logger.LogWarning(
                    "Strategy {StrategyId} timed out after {Timeout}ms",
                    strategy.StrategyId, _options.StrategyTimeout);
            }
            catch (Exception ex)
            {
                // LOGIC: Strategy failed — log error but continue with other strategies.
                // Individual strategy failures should not crash the entire assembly.
                _logger.LogError(
                    ex,
                    "Strategy {StrategyId} failed",
                    strategy.StrategyId);
            }
        });

        return results.ToList();
    }

    /// <summary>
    /// Removes duplicate or near-duplicate fragments based on content similarity.
    /// </summary>
    /// <param name="fragments">Fragments to deduplicate.</param>
    /// <returns>Deduplicated list, preserving higher-relevance fragments.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Iterates fragments in order of descending relevance score.
    /// For each fragment, checks if any previously seen fragment has a Jaccard
    /// similarity above the configured threshold. If so, the lower-relevance
    /// fragment is considered a duplicate and removed.
    /// </para>
    /// <para>
    /// This preserves the most relevant version of overlapping content (e.g.,
    /// keeps the selection fragment over a document fragment that contains the
    /// same text, since the selection has higher specificity).
    /// </para>
    /// </remarks>
    private List<ContextFragment> DeduplicateFragments(List<ContextFragment> fragments)
    {
        // LOGIC: No deduplication needed for 0 or 1 fragments.
        if (fragments.Count <= 1)
            return fragments;

        var deduplicated = new List<ContextFragment>();
        var seenContent = new List<string>();

        // LOGIC: Process in relevance order so higher-relevance fragments are kept.
        foreach (var fragment in fragments.OrderByDescending(f => f.Relevance))
        {
            var isDuplicate = seenContent.Any(seen =>
                ContentDeduplicator.CalculateJaccardSimilarity(seen, fragment.Content)
                    >= _options.DeduplicationThreshold);

            if (!isDuplicate)
            {
                deduplicated.Add(fragment);
                seenContent.Add(fragment.Content);
            }
            else
            {
                _logger.LogDebug(
                    "Deduplicating fragment from {SourceId}",
                    fragment.SourceId);
            }
        }

        if (deduplicated.Count < fragments.Count)
        {
            _logger.LogDebug(
                "Removed {Count} duplicate fragments",
                fragments.Count - deduplicated.Count);
        }

        return deduplicated;
    }

    /// <summary>
    /// Sorts fragments by orchestrator-level priority (descending), then relevance (descending).
    /// </summary>
    /// <param name="fragments">Fragments to sort.</param>
    /// <returns>Sorted list with highest-priority fragments first.</returns>
    /// <remarks>
    /// LOGIC: Uses <see cref="GetPriorityForSource"/> to map source IDs to
    /// orchestrator-level priorities, which may differ from the strategy's
    /// self-reported <see cref="IContextStrategy.Priority"/>. This provides
    /// the orchestrator with independent control over trimming order.
    /// </remarks>
    private static List<ContextFragment> SortFragments(List<ContextFragment> fragments)
    {
        return fragments
            .OrderByDescending(f => GetPriorityForSource(f.SourceId))
            .ThenByDescending(f => f.Relevance)
            .ToList();
    }

    /// <summary>
    /// Maps a source strategy ID to an orchestrator-level priority value.
    /// </summary>
    /// <param name="sourceId">The <see cref="ContextFragment.SourceId"/> to map.</param>
    /// <returns>Priority value for sorting (higher = more important).</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: The orchestrator uses its own priority mapping rather than the
    /// strategy's self-reported priority. This allows the orchestrator to
    /// control the trimming order independently of execution order.
    /// </para>
    /// <para>
    /// Priority mapping:
    /// </para>
    /// <list type="table">
    ///   <listheader><term>Source</term><description>Priority</description></listheader>
    ///   <item><term>document</term><description>100 (Critical)</description></item>
    ///   <item><term>selection</term><description>80 (High)</description></item>
    ///   <item><term>cursor</term><description>70 (High - 10)</description></item>
    ///   <item><term>heading</term><description>70 (Medium + 10)</description></item>
    ///   <item><term>rag</term><description>60 (Medium)</description></item>
    ///   <item><term>style</term><description>50 (Optional + 30)</description></item>
    ///   <item><term>unknown</term><description>40 (Low)</description></item>
    /// </list>
    /// </remarks>
    private static int GetPriorityForSource(string sourceId)
    {
        return sourceId switch
        {
            "document" => StrategyPriority.Critical,       // 100
            "selection" => StrategyPriority.High,           // 80
            "cursor" => StrategyPriority.High - 10,         // 70
            "heading" => StrategyPriority.Medium + 10,      // 70
            "rag" => StrategyPriority.Medium,               // 60
            "style" => StrategyPriority.Optional + 30,      // 50
            _ => StrategyPriority.Low                       // 40
        };
    }

    /// <summary>
    /// Trims fragments to fit within the token budget.
    /// </summary>
    /// <param name="fragments">Sorted fragments (highest priority first).</param>
    /// <param name="maxTokens">Maximum total tokens allowed.</param>
    /// <returns>Fragments that fit within the budget, possibly with truncation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Iterates fragments from highest to lowest priority:
    /// </para>
    /// <list type="number">
    ///   <item><description>If the fragment fits entirely, include it</description></item>
    ///   <item><description>If it doesn't fit but there are &gt;100 tokens remaining, try truncating it</description></item>
    ///   <item><description>If truncation produces meaningful content, include the truncated version</description></item>
    ///   <item><description>Stop processing once the budget is exhausted</description></item>
    /// </list>
    /// <para>
    /// The 100-token minimum ensures we don't waste budget space on fragments
    /// too small to provide meaningful context.
    /// </para>
    /// </remarks>
    private List<ContextFragment> TrimToFitBudget(
        List<ContextFragment> fragments,
        int maxTokens)
    {
        var result = new List<ContextFragment>();
        var currentTokens = 0;

        foreach (var fragment in fragments)
        {
            // LOGIC: Fragment fits entirely within remaining budget.
            if (currentTokens + fragment.TokenEstimate <= maxTokens)
            {
                result.Add(fragment);
                currentTokens += fragment.TokenEstimate;
            }
            else
            {
                // LOGIC: Fragment doesn't fit. Try truncation if meaningful space remains.
                var remaining = maxTokens - currentTokens;
                if (remaining > 100)
                {
                    var truncated = fragment.TruncateTo(remaining, _tokenCounter);
                    if (truncated.HasContent)
                    {
                        result.Add(truncated);
                        currentTokens += truncated.TokenEstimate;

                        _logger.LogDebug(
                            "Truncated {SourceId} from {Original} to {Truncated} tokens",
                            fragment.SourceId, fragment.TokenEstimate, truncated.TokenEstimate);
                    }
                }

                // LOGIC: Budget exhausted — stop processing remaining fragments.
                _logger.LogInformation(
                    "Budget exhausted at {Tokens}/{Max} tokens, dropping remaining fragments",
                    currentTokens, maxTokens);
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts template variables from the request and assembled fragments.
    /// </summary>
    /// <param name="request">Original context gathering request.</param>
    /// <param name="fragments">Assembled context fragments.</param>
    /// <returns>Dictionary of template variables for prompt rendering.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Extracts the following variables:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>DocumentName</c>: File name of the active document</description></item>
    ///   <item><description><c>DocumentPath</c>: Full path of the active document</description></item>
    ///   <item><description><c>CursorPosition</c>: Current cursor offset</description></item>
    ///   <item><description><c>SelectionLength</c>: Length of selected text</description></item>
    ///   <item><description><c>FragmentCount</c>: Number of assembled fragments</description></item>
    ///   <item><description><c>TotalTokens</c>: Total token count of all fragments</description></item>
    /// </list>
    /// <para>
    /// These variables are available for Mustache template rendering in agent prompts
    /// (e.g., <c>{{DocumentName}}</c>, <c>{{FragmentCount}}</c>).
    /// </para>
    /// </remarks>
    private static Dictionary<string, object> ExtractVariables(
        ContextGatheringRequest request,
        IReadOnlyList<ContextFragment> fragments)
    {
        var variables = new Dictionary<string, object>();

        // LOGIC: Document metadata
        if (request.HasDocument)
        {
            variables["DocumentName"] = Path.GetFileName(request.DocumentPath!);
            variables["DocumentPath"] = request.DocumentPath!;
        }

        // LOGIC: Cursor position
        if (request.HasCursor)
        {
            variables["CursorPosition"] = request.CursorPosition!.Value;
        }

        // LOGIC: Selection metadata
        if (request.HasSelection)
        {
            variables["SelectionLength"] = request.SelectedText!.Length;
        }

        // LOGIC: Assembly summary
        variables["FragmentCount"] = fragments.Count;
        variables["TotalTokens"] = fragments.Sum(f => f.TokenEstimate);

        return variables;
    }

    /// <summary>
    /// Publishes the <see cref="ContextAssembledEvent"/> via MediatR.
    /// </summary>
    /// <param name="agentId">ID of the agent that requested context.</param>
    /// <param name="context">The assembled context result.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Publishes the event for the Context Preview panel (v0.7.2d)
    /// and telemetry handlers. Uses MediatR's <see cref="IMediator.Publish"/>
    /// for fire-and-forget notification delivery.
    /// </remarks>
    private async Task PublishEventAsync(
        string agentId,
        AssembledContext context,
        CancellationToken ct)
    {
        var @event = new ContextAssembledEvent(
            agentId,
            context.Fragments,
            context.TotalTokens,
            context.AssemblyDuration);

        await _mediator.Publish(@event, ct);
    }
}
