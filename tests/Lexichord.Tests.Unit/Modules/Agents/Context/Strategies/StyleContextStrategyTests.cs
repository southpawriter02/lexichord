// -----------------------------------------------------------------------
// <copyright file="StyleContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Context.Strategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context.Strategies;

/// <summary>
/// Unit tests for <see cref="StyleContextStrategy"/>.
/// </summary>
/// <remarks>
/// Tests verify constructor validation, property values, GatherAsync behavior including
/// style sheet retrieval and rule filtering, and the static helper methods
/// FilterRulesForAgent and FormatStyleRules. Introduced in v0.7.2b.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2b")]
public class StyleContextStrategyTests
{
    #region Helper Factories

    private static StyleContextStrategy CreateStrategy(
        IStyleEngine? styleEngine = null,
        ITokenCounter? tokenCounter = null,
        ILogger<StyleContextStrategy>? logger = null)
    {
        styleEngine ??= Substitute.For<IStyleEngine>();
        tokenCounter ??= CreateDefaultTokenCounter();
        logger ??= Substitute.For<ILogger<StyleContextStrategy>>();
        return new StyleContextStrategy(styleEngine, tokenCounter, logger);
    }

    private static ITokenCounter CreateDefaultTokenCounter()
    {
        var tc = Substitute.For<ITokenCounter>();
        tc.CountTokens(Arg.Any<string>()).Returns(ci => ci.Arg<string>().Length / 4);
        return tc;
    }

    private static StyleRule CreateRule(
        RuleCategory category,
        string name = "Test Rule",
        string description = "Test description",
        string? suggestion = null,
        bool isEnabled = true)
    {
        return new StyleRule(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            Description: description,
            Category: category,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: @"\btest\b",
            PatternType: PatternType.Regex,
            Suggestion: suggestion,
            IsEnabled: isEnabled);
    }

    private static StyleSheet CreateStyleSheet(
        string name = "Test Style Guide",
        params StyleRule[] rules)
    {
        return new StyleSheet(
            Name: name,
            Rules: rules.ToList());
    }

    private static ContextGatheringRequest CreateRequest(string agentId = "editor")
    {
        return new ContextGatheringRequest(
            DocumentPath: null,
            CursorPosition: null,
            SelectedText: null,
            AgentId: agentId,
            Hints: null);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that passing a null style engine to the constructor throws
    /// an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_NullStyleEngine_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<StyleContextStrategy>>();

        // Act
        var act = () => new StyleContextStrategy(null!, tokenCounter, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("styleEngine");
    }

    /// <summary>
    /// Verifies that constructing with valid parameters succeeds without error.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var styleEngine = Substitute.For<IStyleEngine>();
        var tokenCounter = CreateDefaultTokenCounter();
        var logger = Substitute.For<ILogger<StyleContextStrategy>>();

        // Act
        var sut = new StyleContextStrategy(styleEngine, tokenCounter, logger);

        // Assert
        sut.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.StrategyId"/> returns "style".
    /// </summary>
    [Fact]
    public void StrategyId_ReturnsStyle()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.StrategyId;

        // Assert
        result.Should().Be("style");
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.DisplayName"/> returns "Style Rules".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsStyleRules()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.DisplayName;

        // Assert
        result.Should().Be("Style Rules");
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.Priority"/> returns 50
    /// (<see cref="StrategyPriority.Optional"/> + 30).
    /// </summary>
    [Fact]
    public void Priority_Returns50()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.Priority;

        // Assert
        result.Should().Be(StrategyPriority.Optional + 30);
        result.Should().Be(50);
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.MaxTokens"/> returns 1000.
    /// </summary>
    [Fact]
    public void MaxTokens_Returns1000()
    {
        // Arrange
        var sut = CreateStrategy();

        // Act
        var result = sut.MaxTokens;

        // Assert
        result.Should().Be(1000);
    }

    #endregion

    #region GatherAsync Tests

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.GatherAsync"/> returns null
    /// when <see cref="IStyleEngine.GetActiveStyleSheet"/> returns null.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NullStyleSheet_ReturnsNull()
    {
        // Arrange
        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns((StyleSheet)null!);

        var sut = CreateStrategy(styleEngine: styleEngine);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.GatherAsync"/> returns null
    /// when <see cref="IStyleEngine.GetActiveStyleSheet"/> returns <see cref="StyleSheet.Empty"/>.
    /// </summary>
    [Fact]
    public async Task GatherAsync_EmptyStyleSheet_ReturnsNull()
    {
        // Arrange
        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns(StyleSheet.Empty);

        var sut = CreateStrategy(styleEngine: styleEngine);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.GatherAsync"/> returns null
    /// when the active style sheet has no enabled rules.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoEnabledRules_ReturnsNull()
    {
        // Arrange
        var disabledRule = CreateRule(RuleCategory.Syntax, isEnabled: false);
        var styleSheet = CreateStyleSheet("Disabled Rules Guide", disabledRule);

        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns(styleSheet);

        var sut = CreateStrategy(styleEngine: styleEngine);
        var request = CreateRequest();

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.GatherAsync"/> returns null
    /// when no rules are relevant to the requesting agent. For example, a "tuning"
    /// agent only gets Terminology rules, so providing only Formatting rules yields null.
    /// </summary>
    [Fact]
    public async Task GatherAsync_NoRelevantRulesForAgent_ReturnsNull()
    {
        // Arrange
        var formattingRule = CreateRule(RuleCategory.Formatting, name: "Line Length");
        var styleSheet = CreateStyleSheet("Formatting Only Guide", formattingRule);

        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns(styleSheet);

        var sut = CreateStrategy(styleEngine: styleEngine);
        var request = CreateRequest(agentId: "tuning");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.GatherAsync"/> returns a properly
    /// formatted <see cref="ContextFragment"/> when enabled rules are available and
    /// relevant to the requesting agent.
    /// </summary>
    [Fact]
    public async Task GatherAsync_WithRules_ReturnsFormattedFragment()
    {
        // Arrange
        var syntaxRule = CreateRule(
            RuleCategory.Syntax,
            name: "Use Active Voice",
            description: "Prefer active voice over passive voice",
            suggestion: "Rewrite using active voice");
        var terminologyRule = CreateRule(
            RuleCategory.Terminology,
            name: "Avoid Jargon",
            description: "Do not use technical jargon");

        var styleSheet = CreateStyleSheet("Corporate Guide", syntaxRule, terminologyRule);

        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns(styleSheet);

        var sut = CreateStrategy(styleEngine: styleEngine);
        var request = CreateRequest(agentId: "editor");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("Corporate Guide");
        result.Content.Should().Contain("Use Active Voice");
        result.Content.Should().Contain("Avoid Jargon");
    }

    /// <summary>
    /// Verifies that the <see cref="ContextFragment"/> returned by
    /// <see cref="StyleContextStrategy.GatherAsync"/> has the correct metadata:
    /// SourceId of "style" and Relevance of 0.8.
    /// </summary>
    [Fact]
    public async Task GatherAsync_FragmentHasCorrectMetadata()
    {
        // Arrange
        var rule = CreateRule(
            RuleCategory.Syntax,
            name: "Active Voice",
            description: "Use active voice");

        var styleSheet = CreateStyleSheet("Test Guide", rule);

        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns(styleSheet);

        var sut = CreateStrategy(styleEngine: styleEngine);
        var request = CreateRequest(agentId: "editor");

        // Act
        var result = await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SourceId.Should().Be("style");
        result.Relevance.Should().Be(0.8f);
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.GatherAsync"/> logs an
    /// Information-level message upon successfully gathering style rules.
    /// </summary>
    [Fact]
    public async Task GatherAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var rule = CreateRule(
            RuleCategory.Syntax,
            name: "Active Voice",
            description: "Use active voice");

        var styleSheet = CreateStyleSheet("Logged Guide", rule);

        var styleEngine = Substitute.For<IStyleEngine>();
        styleEngine.GetActiveStyleSheet().Returns(styleSheet);

        var logger = Substitute.For<ILogger<StyleContextStrategy>>();
        var sut = CreateStrategy(styleEngine: styleEngine, logger: logger);
        var request = CreateRequest(agentId: "editor");

        // Act
        await sut.GatherAsync(request, CancellationToken.None);

        // Assert
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("style")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region FilterRulesForAgent Tests

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FilterRulesForAgent"/> returns
    /// only Syntax and Terminology rules when the agent is "editor".
    /// </summary>
    [Fact]
    public void FilterRulesForAgent_EditorAgent_ReturnsSyntaxAndTerminology()
    {
        // Arrange
        var syntaxRule = CreateRule(RuleCategory.Syntax, name: "Grammar Rule");
        var terminologyRule = CreateRule(RuleCategory.Terminology, name: "Term Rule");
        var formattingRule = CreateRule(RuleCategory.Formatting, name: "Format Rule");
        var rules = new List<StyleRule> { syntaxRule, terminologyRule, formattingRule };

        // Act
        var result = StyleContextStrategy.FilterRulesForAgent(rules, "editor");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Category == RuleCategory.Syntax);
        result.Should().Contain(r => r.Category == RuleCategory.Terminology);
        result.Should().NotContain(r => r.Category == RuleCategory.Formatting);
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FilterRulesForAgent"/> returns
    /// only Formatting and Syntax rules when the agent is "simplifier".
    /// </summary>
    [Fact]
    public void FilterRulesForAgent_SimplifierAgent_ReturnsFormattingAndSyntax()
    {
        // Arrange
        var syntaxRule = CreateRule(RuleCategory.Syntax, name: "Grammar Rule");
        var terminologyRule = CreateRule(RuleCategory.Terminology, name: "Term Rule");
        var formattingRule = CreateRule(RuleCategory.Formatting, name: "Format Rule");
        var rules = new List<StyleRule> { syntaxRule, terminologyRule, formattingRule };

        // Act
        var result = StyleContextStrategy.FilterRulesForAgent(rules, "simplifier");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Category == RuleCategory.Formatting);
        result.Should().Contain(r => r.Category == RuleCategory.Syntax);
        result.Should().NotContain(r => r.Category == RuleCategory.Terminology);
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FilterRulesForAgent"/> returns
    /// only Terminology rules when the agent is "tuning".
    /// </summary>
    [Fact]
    public void FilterRulesForAgent_TuningAgent_ReturnsTerminologyOnly()
    {
        // Arrange
        var syntaxRule = CreateRule(RuleCategory.Syntax, name: "Grammar Rule");
        var terminologyRule = CreateRule(RuleCategory.Terminology, name: "Term Rule");
        var formattingRule = CreateRule(RuleCategory.Formatting, name: "Format Rule");
        var rules = new List<StyleRule> { syntaxRule, terminologyRule, formattingRule };

        // Act
        var result = StyleContextStrategy.FilterRulesForAgent(rules, "tuning");

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(r => r.Category == RuleCategory.Terminology);
        result.Should().NotContain(r => r.Category == RuleCategory.Syntax);
        result.Should().NotContain(r => r.Category == RuleCategory.Formatting);
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FilterRulesForAgent"/> returns
    /// all rules when the agent ID is not a recognized specialist type.
    /// </summary>
    [Fact]
    public void FilterRulesForAgent_UnknownAgent_ReturnsAllRules()
    {
        // Arrange
        var syntaxRule = CreateRule(RuleCategory.Syntax, name: "Grammar Rule");
        var terminologyRule = CreateRule(RuleCategory.Terminology, name: "Term Rule");
        var formattingRule = CreateRule(RuleCategory.Formatting, name: "Format Rule");
        var rules = new List<StyleRule> { syntaxRule, terminologyRule, formattingRule };

        // Act
        var result = StyleContextStrategy.FilterRulesForAgent(rules, "unknown-agent");

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.Category == RuleCategory.Syntax);
        result.Should().Contain(r => r.Category == RuleCategory.Terminology);
        result.Should().Contain(r => r.Category == RuleCategory.Formatting);
    }

    #endregion

    #region FormatStyleRules Tests

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FormatStyleRules"/> groups rules
    /// by category and includes the appropriate category headers.
    /// </summary>
    [Fact]
    public void FormatStyleRules_GroupsByCategory()
    {
        // Arrange
        var syntaxRule = CreateRule(
            RuleCategory.Syntax,
            name: "Active Voice",
            description: "Prefer active voice");
        var terminologyRule = CreateRule(
            RuleCategory.Terminology,
            name: "No Jargon",
            description: "Avoid jargon");
        var formattingRule = CreateRule(
            RuleCategory.Formatting,
            name: "Line Length",
            description: "Keep lines short");
        var rules = new List<StyleRule> { syntaxRule, terminologyRule, formattingRule };

        // Act
        var result = StyleContextStrategy.FormatStyleRules("My Style Guide", rules);

        // Assert
        result.Should().Contain("## Style Guide: My Style Guide");
        result.Should().Contain("### Grammar & Syntax");
        result.Should().Contain("### Terminology & Word Choice");
        result.Should().Contain("### Formatting & Structure");
        result.Should().Contain("Active Voice");
        result.Should().Contain("No Jargon");
        result.Should().Contain("Line Length");
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FormatStyleRules"/> includes
    /// the "Suggestion:" line when a rule has a non-null suggestion.
    /// </summary>
    [Fact]
    public void FormatStyleRules_IncludesSuggestion()
    {
        // Arrange
        var rule = CreateRule(
            RuleCategory.Syntax,
            name: "Active Voice",
            description: "Prefer active voice",
            suggestion: "Rewrite using active voice");
        var rules = new List<StyleRule> { rule };

        // Act
        var result = StyleContextStrategy.FormatStyleRules("Guide", rules);

        // Assert
        result.Should().Contain("Suggestion: Rewrite using active voice");
    }

    /// <summary>
    /// Verifies that <see cref="StyleContextStrategy.FormatStyleRules"/> omits the
    /// "Suggestion:" line when a rule has a null suggestion.
    /// </summary>
    [Fact]
    public void FormatStyleRules_OmitsSuggestionWhenNull()
    {
        // Arrange
        var rule = CreateRule(
            RuleCategory.Syntax,
            name: "Active Voice",
            description: "Prefer active voice",
            suggestion: null);
        var rules = new List<StyleRule> { rule };

        // Act
        var result = StyleContextStrategy.FormatStyleRules("Guide", rules);

        // Assert
        result.Should().NotContain("Suggestion:");
    }

    #endregion
}
