// -----------------------------------------------------------------------
// <copyright file="MustachePromptRendererTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Templates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Templates;

/// <summary>
/// Unit tests for <see cref="MustachePromptRenderer"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests validate the Mustache templating functionality including:
/// </para>
/// <list type="bullet">
///   <item><description>Variable substitution</description></item>
///   <item><description>Section rendering (truthy/falsy)</description></item>
///   <item><description>Inverted section rendering</description></item>
///   <item><description>Raw/unescaped output</description></item>
///   <item><description>Case sensitivity options</description></item>
///   <item><description>Validation behavior</description></item>
///   <item><description>RenderMessages functionality</description></item>
///   <item><description>Edge cases and error handling</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3b")]
public class MustachePromptRendererTests
{
    private readonly MustachePromptRenderer _renderer;
    private readonly MustachePromptRenderer _strictRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MustachePromptRendererTests"/> class.
    /// </summary>
    public MustachePromptRendererTests()
    {
        // Default renderer with case-insensitive lookup
        _renderer = new MustachePromptRenderer(
            NullLogger<MustachePromptRenderer>.Instance,
            Options.Create(MustacheRendererOptions.Default));

        // Strict renderer with case-sensitive lookup
        _strictRenderer = new MustachePromptRenderer(
            NullLogger<MustachePromptRenderer>.Instance,
            Options.Create(MustacheRendererOptions.Strict));
    }

    #region Basic Variable Substitution Tests

    /// <summary>
    /// Verifies that simple variable substitution works correctly.
    /// </summary>
    [Fact]
    public void Render_WithSimpleVariable_SubstitutesCorrectly()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, object> { ["name"] = "World" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, World!");
    }

    /// <summary>
    /// Verifies that multiple variables are substituted correctly.
    /// </summary>
    [Fact]
    public void Render_WithMultipleVariables_SubstitutesAll()
    {
        // Arrange
        var template = "{{greeting}}, {{name}}! Welcome to {{place}}.";
        var variables = new Dictionary<string, object>
        {
            ["greeting"] = "Hello",
            ["name"] = "Alice",
            ["place"] = "Lexichord"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, Alice! Welcome to Lexichord.");
    }

    /// <summary>
    /// Verifies that missing variables are rendered as empty strings.
    /// </summary>
    [Fact]
    public void Render_WithMissingVariable_ReturnsEmptyForPlaceholder()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, object>();

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, !");
    }

    /// <summary>
    /// Verifies that null variable values are rendered as empty strings.
    /// </summary>
    [Fact]
    public void Render_WithNullVariable_ReturnsEmpty()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var variables = new Dictionary<string, object> { ["name"] = null! };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, !");
    }

    /// <summary>
    /// Verifies that numeric variables are converted to strings.
    /// </summary>
    [Fact]
    public void Render_WithNumericVariable_ConvertsToString()
    {
        // Arrange
        var template = "You have {{count}} messages.";
        var variables = new Dictionary<string, object> { ["count"] = 42 };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("You have 42 messages.");
    }

    #endregion

    #region Section Rendering Tests

    /// <summary>
    /// Verifies that truthy sections are rendered.
    /// </summary>
    [Fact]
    public void Render_WithTruthySection_RendersContent()
    {
        // Arrange
        var template = "{{#show_rules}}Rules: {{rules}}{{/show_rules}}";
        var variables = new Dictionary<string, object>
        {
            ["show_rules"] = true,
            ["rules"] = "Be concise"
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Rules: Be concise");
    }

    /// <summary>
    /// Verifies that falsy sections are not rendered.
    /// </summary>
    [Fact]
    public void Render_WithFalsySection_HidesContent()
    {
        // Arrange
        var template = "Start{{#show}}Hidden{{/show}}End";
        var variables = new Dictionary<string, object> { ["show"] = false };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("StartEnd");
    }

    /// <summary>
    /// Verifies that empty list sections are not rendered.
    /// </summary>
    [Fact]
    public void Render_WithEmptyListSection_HidesContent()
    {
        // Arrange
        var template = "Items:{{#items}} {{.}}{{/items}}Done";
        var variables = new Dictionary<string, object> { ["items"] = Array.Empty<string>() };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Items:Done");
    }

    /// <summary>
    /// Verifies that populated list sections iterate correctly.
    /// </summary>
    [Fact]
    public void Render_WithPopulatedListSection_IteratesContent()
    {
        // Arrange
        var template = "Items:{{#items}} {{.}}{{/items}}";
        var variables = new Dictionary<string, object> { ["items"] = new[] { "a", "b", "c" } };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Items: a b c");
    }

    /// <summary>
    /// Verifies that nested sections render correctly.
    /// </summary>
    [Fact]
    public void Render_WithNestedSections_RendersCorrectly()
    {
        // Arrange
        var template = "{{#outer}}Outer{{#inner}}Inner{{/inner}}{{/outer}}";
        var variables = new Dictionary<string, object>
        {
            ["outer"] = true,
            ["inner"] = true
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("OuterInner");
    }

    #endregion

    #region Inverted Section Tests

    /// <summary>
    /// Verifies that inverted sections render when the value is falsy.
    /// </summary>
    [Fact]
    public void Render_WithInvertedSectionFalsy_RendersContent()
    {
        // Arrange
        var template = "{{^show}}Not shown{{/show}}";
        var variables = new Dictionary<string, object> { ["show"] = false };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Not shown");
    }

    /// <summary>
    /// Verifies that inverted sections do not render when the value is truthy.
    /// </summary>
    [Fact]
    public void Render_WithInvertedSectionTruthy_HidesContent()
    {
        // Arrange
        var template = "{{^show}}Not shown{{/show}}";
        var variables = new Dictionary<string, object> { ["show"] = true };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that inverted sections render for empty lists.
    /// </summary>
    [Fact]
    public void Render_WithInvertedSectionEmptyList_RendersContent()
    {
        // Arrange
        var template = "{{^items}}No items{{/items}}";
        var variables = new Dictionary<string, object> { ["items"] = Array.Empty<string>() };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("No items");
    }

    #endregion

    #region Raw/Unescaped Output Tests

    /// <summary>
    /// Verifies that triple mustache preserves HTML content.
    /// </summary>
    [Fact]
    public void Render_WithTripleMustache_PreservesHtml()
    {
        // Arrange
        var template = "Content: {{{content}}}";
        var variables = new Dictionary<string, object> { ["content"] = "<b>bold</b>" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Content: <b>bold</b>");
    }

    /// <summary>
    /// Verifies that ampersand syntax preserves HTML content.
    /// </summary>
    [Fact]
    public void Render_WithAmpersand_PreservesHtml()
    {
        // Arrange
        var template = "Content: {{&content}}";
        var variables = new Dictionary<string, object> { ["content"] = "<i>italic</i>" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Content: <i>italic</i>");
    }

    #endregion

    #region Case Sensitivity Tests

    /// <summary>
    /// Verifies that case-insensitive lookup matches variables ignoring case.
    /// </summary>
    [Fact]
    public void Render_WithCaseInsensitive_MatchesIgnoringCase()
    {
        // Arrange
        var template = "Hello, {{NAME}}!";
        var variables = new Dictionary<string, object> { ["name"] = "World" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, World!");
    }

    /// <summary>
    /// Verifies that case-sensitive lookup requires exact match.
    /// </summary>
    [Fact]
    public void Render_WithCaseSensitive_RequiresExactMatch()
    {
        // Arrange
        var template = "Hello, {{NAME}}!";
        var variables = new Dictionary<string, object> { ["name"] = "World" };

        // Act
        var result = _strictRenderer.Render(template, variables);

        // Assert
        result.Should().Be("Hello, !");
    }

    /// <summary>
    /// Verifies that case-insensitive validation finds variables ignoring case.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithCaseInsensitive_FindsVariable()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "System", "{{Name}}",
            requiredVariables: new[] { "Name" });

        var variables = new Dictionary<string, object> { ["name"] = "value" };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region RenderMessages Tests

    /// <summary>
    /// Verifies that RenderMessages returns both system and user messages.
    /// </summary>
    [Fact]
    public void RenderMessages_WithBothPrompts_ReturnsSystemAndUser()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test",
            "You are {{role}}.",
            "{{input}}",
            requiredVariables: new[] { "input" },
            optionalVariables: new[] { "role" });

        var variables = new Dictionary<string, object>
        {
            ["role"] = "a helpful assistant",
            ["input"] = "Hello!"
        };

        // Act
        var messages = _renderer.RenderMessages(template, variables);

        // Assert
        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[0].Content.Should().Contain("helpful assistant");
        messages[1].Role.Should().Be(ChatRole.User);
        messages[1].Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Verifies that RenderMessages returns only user message when system is empty.
    /// </summary>
    [Fact]
    public void RenderMessages_WithOnlyUserPrompt_ReturnsUserOnly()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test",
            string.Empty,
            "{{input}}",
            requiredVariables: new[] { "input" });

        var variables = new Dictionary<string, object> { ["input"] = "Hello!" };

        // Act
        var messages = _renderer.RenderMessages(template, variables);

        // Assert
        messages.Should().HaveCount(1);
        messages[0].Role.Should().Be(ChatRole.User);
        messages[0].Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Verifies that RenderMessages excludes empty rendered system prompt.
    /// </summary>
    [Fact]
    public void RenderMessages_WithEmptySystemPrompt_ReturnsUserOnly()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test",
            "{{#context}}{{context}}{{/context}}",
            "{{input}}",
            requiredVariables: new[] { "input" },
            optionalVariables: new[] { "context" });

        var variables = new Dictionary<string, object>
        {
            ["input"] = "Hello!",
            ["context"] = false // Falsy, so section won't render
        };

        // Act
        var messages = _renderer.RenderMessages(template, variables);

        // Assert
        messages.Should().HaveCount(1);
        messages[0].Role.Should().Be(ChatRole.User);
    }

    /// <summary>
    /// Verifies that RenderMessages throws when required variables are missing.
    /// </summary>
    [Fact]
    public void RenderMessages_WithMissingRequired_ThrowsValidationException()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test",
            "System: {{context}}",
            "{{user_input}}",
            requiredVariables: new[] { "context", "user_input" });

        var variables = new Dictionary<string, object>
        {
            ["context"] = "Some context"
            // Missing: user_input
        };

        // Act
        var action = () => _renderer.RenderMessages(template, variables);

        // Assert
        action.Should().Throw<TemplateValidationException>()
            .Where(ex => ex.MissingVariables.Contains("user_input"))
            .Where(ex => ex.TemplateId == "test");
    }

    #endregion

    #region ValidateVariables Tests

    /// <summary>
    /// Verifies that validation passes when all required variables are present.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithAllRequired_ReturnsSuccess()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}}", "{{b}}",
            requiredVariables: new[] { "a", "b" });

        var variables = new Dictionary<string, object>
        {
            ["a"] = "value1",
            ["b"] = "value2"
        };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeTrue();
        result.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that validation fails when required variables are missing.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithMissingRequired_ReturnsFailure()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}}", "{{b}}",
            requiredVariables: new[] { "a", "b" });

        var variables = new Dictionary<string, object>
        {
            ["a"] = "value1"
            // Missing: b
        };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("b");
    }

    /// <summary>
    /// Verifies that validation fails when a required variable has null value.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithNullRequiredValue_ReturnsFailure()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}}", "{{b}}",
            requiredVariables: new[] { "a" });

        var variables = new Dictionary<string, object>
        {
            ["a"] = null!
        };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("a");
    }

    /// <summary>
    /// Verifies that validation fails when a required variable has empty string value.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithEmptyRequiredValue_ReturnsFailure()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}}", "{{b}}",
            requiredVariables: new[] { "a" });

        var variables = new Dictionary<string, object>
        {
            ["a"] = ""
        };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("a");
    }

    /// <summary>
    /// Verifies that validation generates warnings for unused provided variables.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithUnusedProvided_ReturnsWarning()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}}", "{{b}}",
            requiredVariables: new[] { "a" });

        var variables = new Dictionary<string, object>
        {
            ["a"] = "value",
            ["unexpected"] = "extra"
        };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("unexpected"));
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    /// <summary>
    /// Verifies that Render throws ArgumentNullException for null template.
    /// </summary>
    [Fact]
    public void Render_WithNullTemplate_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _renderer.Render(null!, new Dictionary<string, object>());

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("template");
    }

    /// <summary>
    /// Verifies that Render throws ArgumentNullException for null variables.
    /// </summary>
    [Fact]
    public void Render_WithNullVariables_ThrowsArgumentNullException()
    {
        // Act
        var action = () => _renderer.Render("template", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("variables");
    }

    /// <summary>
    /// Verifies that Render returns empty string for empty template.
    /// </summary>
    [Fact]
    public void Render_WithEmptyTemplate_ReturnsEmpty()
    {
        // Act
        var result = _renderer.Render(string.Empty, new Dictionary<string, object>());

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Render preserves template without placeholders.
    /// </summary>
    [Fact]
    public void Render_WithNoPlaceholders_ReturnsTemplateAsIs()
    {
        // Arrange
        var template = "Plain text with no variables.";

        // Act
        var result = _renderer.Render(template, new Dictionary<string, object>());

        // Assert
        result.Should().Be(template);
    }

    /// <summary>
    /// Verifies that nested objects can be accessed with dot notation.
    /// </summary>
    [Fact]
    public void Render_WithNestedObject_AccessesProperties()
    {
        // Arrange
        var template = "Name: {{user.name}}, Age: {{user.age}}";
        var variables = new Dictionary<string, object>
        {
            ["user"] = new { name = "Alice", age = 30 }
        };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Name: Alice, Age: 30");
    }

    /// <summary>
    /// Verifies that newlines are preserved in templates and output.
    /// </summary>
    [Fact]
    public void Render_WithNewlines_PreservesFormatting()
    {
        // Arrange
        var template = "Line1\n{{middle}}\nLine3";
        var variables = new Dictionary<string, object> { ["middle"] = "Line2" };

        // Act
        var result = _renderer.Render(template, variables);

        // Assert
        result.Should().Be("Line1\nLine2\nLine3");
    }

    /// <summary>
    /// Verifies that whitespace-only required variables are treated as missing.
    /// </summary>
    [Fact]
    public void ValidateVariables_WithWhitespaceRequiredValue_ReturnsFailure()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}}", "{{b}}",
            requiredVariables: new[] { "a" });

        var variables = new Dictionary<string, object>
        {
            ["a"] = "   "
        };

        // Act
        var result = _renderer.ValidateVariables(template, variables);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("a");
    }

    /// <summary>
    /// Verifies that RenderMessages trims whitespace from rendered prompts.
    /// </summary>
    [Fact]
    public void RenderMessages_TrimsWhitespace()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test",
            "  System with spaces  ",
            "  User with spaces  ");

        var variables = new Dictionary<string, object>();

        // Act
        var messages = _renderer.RenderMessages(template, variables);

        // Assert
        messages[0].Content.Should().Be("System with spaces");
        messages[1].Content.Should().Be("User with spaces");
    }

    #endregion

    #region Options Preset Tests

    /// <summary>
    /// Verifies that Default preset has expected values.
    /// </summary>
    [Fact]
    public void MustacheRendererOptions_Default_HasExpectedValues()
    {
        // Act
        var options = MustacheRendererOptions.Default;

        // Assert
        options.IgnoreCaseOnKeyLookup.Should().BeTrue();
        options.ThrowOnMissingVariables.Should().BeTrue();
        options.FastRenderThresholdMs.Should().Be(10);
    }

    /// <summary>
    /// Verifies that Strict preset has expected values.
    /// </summary>
    [Fact]
    public void MustacheRendererOptions_Strict_HasExpectedValues()
    {
        // Act
        var options = MustacheRendererOptions.Strict;

        // Assert
        options.IgnoreCaseOnKeyLookup.Should().BeFalse();
        options.ThrowOnMissingVariables.Should().BeTrue();
        options.FastRenderThresholdMs.Should().Be(10);
    }

    /// <summary>
    /// Verifies that Lenient preset has expected values.
    /// </summary>
    [Fact]
    public void MustacheRendererOptions_Lenient_HasExpectedValues()
    {
        // Act
        var options = MustacheRendererOptions.Lenient;

        // Assert
        options.IgnoreCaseOnKeyLookup.Should().BeTrue();
        options.ThrowOnMissingVariables.Should().BeFalse();
        options.FastRenderThresholdMs.Should().Be(10);
    }

    /// <summary>
    /// Verifies that lenient mode does not throw on missing required variables.
    /// </summary>
    [Fact]
    public void RenderMessages_WithLenientMode_DoesNotThrowOnMissing()
    {
        // Arrange
        var lenientRenderer = new MustachePromptRenderer(
            NullLogger<MustachePromptRenderer>.Instance,
            Options.Create(MustacheRendererOptions.Lenient));

        var template = PromptTemplate.Create(
            "test", "Test",
            "System",
            "{{missing}}",
            requiredVariables: new[] { "missing" });

        var variables = new Dictionary<string, object>();

        // Act
        var action = () => lenientRenderer.RenderMessages(template, variables);

        // Assert
        action.Should().NotThrow();
    }

    #endregion
}
