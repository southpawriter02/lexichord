// -----------------------------------------------------------------------
// <copyright file="StyleContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Provides active style rules as context for AI agents.
/// Ensures agent suggestions align with the workspace's configured style guide.
/// </summary>
/// <remarks>
/// <para>
/// This strategy retrieves the active <see cref="StyleSheet"/> from the
/// <see cref="IStyleEngine"/> and formats its enabled rules into a readable
/// context block. Rules are optionally filtered by agent type to provide
/// only relevant guidance (e.g., grammar rules for the Editor agent,
/// readability rules for the Simplifier).
/// </para>
/// <para>
/// <strong>Priority:</strong> <see cref="StrategyPriority.Optional"/> + 30 = 50 —
/// Style rules are supplementary guidance that improves consistency but is not
/// essential for basic agent functionality.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.Teams"/> or higher.
/// Style enforcement is a team collaboration feature.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 1000 — Style rules are compact text descriptions.
/// Most style sheets fit well within this budget.
/// </para>
/// <para>
/// <strong>No Document Required:</strong> Style rules apply workspace-wide and
/// do not depend on document path, selection, or cursor position.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2b as part of the Built-in Context Strategies.
/// Follows the same <see cref="IStyleEngine"/> access pattern as <c>StyleRulesContextProvider</c>.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.Teams)]
public sealed class StyleContextStrategy : ContextStrategyBase
{
    private readonly IStyleEngine _styleEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleContextStrategy"/> class.
    /// </summary>
    /// <param name="styleEngine">Style engine for accessing active style rules.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="styleEngine"/> is null.
    /// </exception>
    public StyleContextStrategy(
        IStyleEngine styleEngine,
        ITokenCounter tokenCounter,
        ILogger<StyleContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
    }

    /// <inheritdoc />
    public override string StrategyId => "style";

    /// <inheritdoc />
    public override string DisplayName => "Style Rules";

    /// <inheritdoc />
    public override int Priority => StrategyPriority.Optional + 30; // 50

    /// <inheritdoc />
    public override int MaxTokens => 1000;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Style context gathering:
    /// </para>
    /// <list type="number">
    ///   <item><description>Retrieves the active <see cref="StyleSheet"/> from <see cref="IStyleEngine"/>.</description></item>
    ///   <item><description>Returns <c>null</c> if no active style sheet or it equals <see cref="StyleSheet.Empty"/>.</description></item>
    ///   <item><description>Gets enabled rules via <see cref="StyleSheet.GetEnabledRules"/>.</description></item>
    ///   <item><description>Optionally filters rules by agent type for relevance.</description></item>
    ///   <item><description>Formats rules grouped by <see cref="RuleCategory"/>.</description></item>
    /// </list>
    /// <para>
    /// <strong>Note:</strong> No document, selection, or cursor position is required.
    /// Style rules apply globally to the workspace.
    /// </para>
    /// </remarks>
    public override Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        _logger.LogDebug("{Strategy} gathering active style rules", StrategyId);

        // LOGIC: Retrieve the active style sheet (follows StyleRulesContextProvider pattern)
        var styleSheet = _styleEngine.GetActiveStyleSheet();

        if (styleSheet is null || styleSheet == StyleSheet.Empty)
        {
            _logger.LogDebug("{Strategy} no active style sheet configured", StrategyId);
            return Task.FromResult<ContextFragment?>(null);
        }

        _logger.LogDebug(
            "{Strategy} retrieved style sheet '{Name}' with {RuleCount} rules",
            StrategyId, styleSheet.Name, styleSheet.Rules.Count);

        // LOGIC: Get only enabled rules
        var enabledRules = styleSheet.GetEnabledRules();

        if (enabledRules.Count == 0)
        {
            _logger.LogDebug("{Strategy} no enabled rules in active style sheet", StrategyId);
            return Task.FromResult<ContextFragment?>(null);
        }

        // LOGIC: Filter rules by agent type for targeted guidance
        var relevantRules = FilterRulesForAgent(enabledRules, request.AgentId);

        if (relevantRules.Count == 0)
        {
            _logger.LogDebug(
                "{Strategy} no rules relevant to agent {Agent}",
                StrategyId, request.AgentId);
            return Task.FromResult<ContextFragment?>(null);
        }

        // LOGIC: Format rules into readable context
        var content = FormatStyleRules(styleSheet.Name, relevantRules);

        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult<ContextFragment?>(null);

        // LOGIC: Apply truncation if needed
        content = TruncateToMaxTokens(content);

        _logger.LogInformation(
            "{Strategy} gathered {RuleCount} style rules from '{Profile}'",
            StrategyId, relevantRules.Count, styleSheet.Name);

        return Task.FromResult<ContextFragment?>(CreateFragment(content, relevance: 0.8f));
    }

    /// <summary>
    /// Filters style rules based on the requesting agent type.
    /// Different agents benefit from different rule categories.
    /// </summary>
    /// <param name="rules">All enabled style rules.</param>
    /// <param name="agentId">The ID of the requesting agent.</param>
    /// <returns>Filtered rules relevant to the agent type.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Agent-specific rule filtering:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>editor</c>: Syntax and Terminology rules (grammar, punctuation).</description></item>
    ///   <item><description><c>simplifier</c>: Formatting and Syntax rules (readability, sentence length).</description></item>
    ///   <item><description><c>tuning</c>: Terminology rules (voice, tone, word choice).</description></item>
    ///   <item><description>Other agents: All rules (no filtering).</description></item>
    /// </list>
    /// </remarks>
    internal static IReadOnlyList<StyleRule> FilterRulesForAgent(
        IReadOnlyList<StyleRule> rules,
        string agentId)
    {
        return agentId switch
        {
            "editor" => rules
                .Where(r => r.Category is RuleCategory.Syntax or RuleCategory.Terminology)
                .ToList(),

            "simplifier" => rules
                .Where(r => r.Category is RuleCategory.Formatting or RuleCategory.Syntax)
                .ToList(),

            "tuning" => rules
                .Where(r => r.Category is RuleCategory.Terminology)
                .ToList(),

            // LOGIC: For unknown agents, return all rules (no filtering)
            _ => rules.ToList()
        };
    }

    /// <summary>
    /// Formats style rules into a readable context block grouped by category.
    /// </summary>
    /// <param name="profileName">Name of the active style profile.</param>
    /// <param name="rules">The rules to format.</param>
    /// <returns>Formatted style rules string.</returns>
    /// <remarks>
    /// LOGIC: Rules are grouped by <see cref="RuleCategory"/> with each category
    /// formatted as a section header. Each rule includes its name, description,
    /// and optional suggestion for how to fix violations.
    /// </remarks>
    internal static string FormatStyleRules(string profileName, IReadOnlyList<StyleRule> rules)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Style Guide: {profileName}");
        sb.AppendLine();
        sb.AppendLine("Follow these style rules in your suggestions:");
        sb.AppendLine();

        // LOGIC: Group rules by category for organized output
        var byCategory = rules.GroupBy(r => r.Category);

        foreach (var category in byCategory)
        {
            sb.AppendLine($"### {FormatCategoryName(category.Key)}");
            sb.AppendLine();

            foreach (var rule in category)
            {
                sb.AppendLine($"- **{rule.Name}**: {rule.Description}");

                if (!string.IsNullOrEmpty(rule.Suggestion))
                {
                    sb.AppendLine($"  - Suggestion: {rule.Suggestion}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Converts a <see cref="RuleCategory"/> enum value to a display-friendly name.
    /// </summary>
    /// <param name="category">The rule category to format.</param>
    /// <returns>A human-readable category name.</returns>
    private static string FormatCategoryName(RuleCategory category)
    {
        return category switch
        {
            RuleCategory.Terminology => "Terminology & Word Choice",
            RuleCategory.Formatting => "Formatting & Structure",
            RuleCategory.Syntax => "Grammar & Syntax",
            _ => category.ToString()
        };
    }
}
