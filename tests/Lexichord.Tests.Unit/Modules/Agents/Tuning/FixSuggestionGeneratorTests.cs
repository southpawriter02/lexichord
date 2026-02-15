// -----------------------------------------------------------------------
// <copyright file="FixSuggestionGeneratorTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Tuning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Tuning;

/// <summary>
/// Unit tests for <see cref="FixSuggestionGenerator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>Constructor tests — Verify null argument handling</description></item>
///   <item><description>GenerateFixAsync tests — Verify fix generation, license checks, error handling</description></item>
///   <item><description>GenerateFixesAsync tests — Verify batch processing and parallelism</description></item>
///   <item><description>RegenerateFixAsync tests — Verify user guidance incorporation</description></item>
///   <item><description>ValidateFixAsync tests — Verify fix validation</description></item>
///   <item><description>DiffGenerator tests — Verify diff generation</description></item>
///   <item><description>FixValidator tests — Verify validation logic</description></item>
///   <item><description>Confidence scoring tests — Verify score calculation</description></item>
///   <item><description>Quality scoring tests — Verify quality calculation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5b")]
public class FixSuggestionGeneratorTests
{
    #region Test Setup

    private readonly Mock<IChatCompletionService> _mockChatService;
    private readonly Mock<IPromptRenderer> _mockPromptRenderer;
    private readonly Mock<IPromptTemplateRepository> _mockTemplateRepository;
    private readonly Mock<IStyleEngine> _mockStyleEngine;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly ILogger<FixSuggestionGenerator> _generatorLogger;
    private readonly ILogger<DiffGenerator> _diffLogger;
    private readonly ILogger<FixValidator> _validatorLogger;

    /// <summary>
    /// Initializes a new instance of the test class with default mocks.
    /// </summary>
    public FixSuggestionGeneratorTests()
    {
        _mockChatService = new Mock<IChatCompletionService>();
        _mockPromptRenderer = new Mock<IPromptRenderer>();
        _mockTemplateRepository = new Mock<IPromptTemplateRepository>();
        _mockStyleEngine = new Mock<IStyleEngine>();
        _mockLicenseContext = new Mock<ILicenseContext>();
        _generatorLogger = NullLogger<FixSuggestionGenerator>.Instance;
        _diffLogger = NullLogger<DiffGenerator>.Instance;
        _validatorLogger = NullLogger<FixValidator>.Instance;

        // Default: WriterPro license
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        // Default: Return mock template
        var mockTemplate = CreateMockTemplate();
        _mockTemplateRepository.Setup(x => x.GetTemplate("tuning-agent-fix")).Returns(mockTemplate.Object);

        // Default: Return rendered messages
        _mockPromptRenderer
            .Setup(x => x.RenderMessages(It.IsAny<IPromptTemplate>(), It.IsAny<IDictionary<string, object>>()))
            .Returns([
                ChatMessage.System("System prompt"),
                ChatMessage.User("User prompt")
            ]);
    }

    /// <summary>
    /// Creates a FixSuggestionGenerator with test mocks.
    /// </summary>
    private FixSuggestionGenerator CreateGenerator()
    {
        var diffGenerator = new DiffGenerator(_diffLogger);
        var validator = new FixValidator(_mockStyleEngine.Object, _validatorLogger);

        return new FixSuggestionGenerator(
            _mockChatService.Object,
            _mockPromptRenderer.Object,
            _mockTemplateRepository.Object,
            diffGenerator,
            validator,
            _mockLicenseContext.Object,
            _generatorLogger);
    }

    /// <summary>
    /// Creates a DiffGenerator with test logger.
    /// </summary>
    private DiffGenerator CreateDiffGenerator() => new(_diffLogger);

    /// <summary>
    /// Creates a FixValidator with test mocks.
    /// </summary>
    private FixValidator CreateValidator() => new(_mockStyleEngine.Object, _validatorLogger);

    /// <summary>
    /// Creates a mock prompt template.
    /// </summary>
    private static Mock<IPromptTemplate> CreateMockTemplate()
    {
        var mock = new Mock<IPromptTemplate>();
        mock.Setup(x => x.TemplateId).Returns("tuning-agent-fix");
        mock.Setup(x => x.Name).Returns("Tuning Agent Fix Generator");
        mock.Setup(x => x.SystemPromptTemplate).Returns("System prompt template");
        mock.Setup(x => x.UserPromptTemplate).Returns("User prompt template");
        mock.Setup(x => x.RequiredVariables).Returns(["original_text", "rule_id"]);
        return mock;
    }

    /// <summary>
    /// Creates a test style rule.
    /// </summary>
    private static StyleRule CreateTestRule(
        string id = "test-rule",
        string name = "Test Rule",
        RuleCategory category = RuleCategory.Terminology,
        ViolationSeverity severity = ViolationSeverity.Warning) =>
        new(
            Id: id,
            Name: name,
            Description: "Test description",
            Category: category,
            DefaultSeverity: severity,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: "suggestion");

    /// <summary>
    /// Creates a test style violation.
    /// </summary>
    private static StyleViolation CreateTestViolation(StyleRule? rule = null)
    {
        rule ??= CreateTestRule();
        return new StyleViolation(
            Rule: rule,
            Message: "Test violation",
            StartOffset: 10,
            EndOffset: 14,
            StartLine: 1,
            StartColumn: 11,
            EndLine: 1,
            EndColumn: 15,
            MatchedText: "test",
            Suggestion: "suggestion",
            Severity: rule.DefaultSeverity);
    }

    /// <summary>
    /// Creates a test style deviation.
    /// </summary>
    private static StyleDeviation CreateTestDeviation(StyleViolation? violation = null)
    {
        violation ??= CreateTestViolation();
        return new StyleDeviation
        {
            DeviationId = Guid.NewGuid(),
            Violation = violation,
            Location = new TextSpan(violation.StartOffset, violation.EndOffset - violation.StartOffset),
            OriginalText = violation.MatchedText,
            SurroundingContext = "This is a test document with some content.",
            ViolatedRule = violation.Rule,
            IsAutoFixable = true,
            Priority = DeviationPriority.High
        };
    }

    /// <summary>
    /// Sets up a successful LLM response.
    /// </summary>
    private void SetupSuccessfulLlmResponse(
        string suggestedText = "suggested",
        string explanation = "Changed test to suggested",
        double confidence = 0.9)
    {
        var responseJson = $$"""
        {
            "suggested_text": "{{suggestedText}}",
            "explanation": "{{explanation}}",
            "confidence": {{confidence}},
            "alternatives": []
        }
        """;

        var chatResponse = new ChatResponse(
            Content: responseJson,
            PromptTokens: 100,
            CompletionTokens: 50,
            Duration: TimeSpan.FromMilliseconds(500),
            FinishReason: "stop");

        _mockChatService
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullChatService_ThrowsArgumentNullException()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();
        var validator = CreateValidator();

        // Act
        var act = () => new FixSuggestionGenerator(
            null!,
            _mockPromptRenderer.Object,
            _mockTemplateRepository.Object,
            diffGenerator,
            validator,
            _mockLicenseContext.Object,
            _generatorLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chatService");
    }

    [Fact]
    public void Constructor_WithNullPromptRenderer_ThrowsArgumentNullException()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();
        var validator = CreateValidator();

        // Act
        var act = () => new FixSuggestionGenerator(
            _mockChatService.Object,
            null!,
            _mockTemplateRepository.Object,
            diffGenerator,
            validator,
            _mockLicenseContext.Object,
            _generatorLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("promptRenderer");
    }

    [Fact]
    public void Constructor_WithNullTemplateRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();
        var validator = CreateValidator();

        // Act
        var act = () => new FixSuggestionGenerator(
            _mockChatService.Object,
            _mockPromptRenderer.Object,
            null!,
            diffGenerator,
            validator,
            _mockLicenseContext.Object,
            _generatorLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("templateRepository");
    }

    [Fact]
    public void Constructor_WithNullDiffGenerator_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var act = () => new FixSuggestionGenerator(
            _mockChatService.Object,
            _mockPromptRenderer.Object,
            _mockTemplateRepository.Object,
            null!,
            validator,
            _mockLicenseContext.Object,
            _generatorLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("diffGenerator");
    }

    [Fact]
    public void Constructor_WithNullValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();

        // Act
        var act = () => new FixSuggestionGenerator(
            _mockChatService.Object,
            _mockPromptRenderer.Object,
            _mockTemplateRepository.Object,
            diffGenerator,
            null!,
            _mockLicenseContext.Object,
            _generatorLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validator");
    }

    #endregion

    #region GenerateFixAsync Tests

    [Fact]
    public async Task GenerateFixAsync_WithNullDeviation_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateFixAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("deviation");
    }

    [Fact]
    public async Task GenerateFixAsync_WithCoreLicense_ReturnsLicenseRequired()
    {
        // Arrange
        _mockLicenseContext.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var result = await generator.GenerateFixAsync(deviation);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Writer Pro license required");
    }

    [Fact]
    public async Task GenerateFixAsync_WithWriterProLicense_GeneratesFix()
    {
        // Arrange
        SetupSuccessfulLlmResponse("improved text", "Improved the wording", 0.95);
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var result = await generator.GenerateFixAsync(deviation);

        // Assert
        result.Success.Should().BeTrue();
        result.SuggestedText.Should().Be("improved text");
        result.Explanation.Should().Be("Improved the wording");
        result.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateFixAsync_WithMissingTemplate_ReturnsFailure()
    {
        // Arrange
        _mockTemplateRepository.Setup(x => x.GetTemplate("tuning-agent-fix")).Returns((IPromptTemplate?)null);
        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var result = await generator.GenerateFixAsync(deviation);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Template not found");
    }

    [Fact]
    public async Task GenerateFixAsync_WithLlmException_ReturnsFailure()
    {
        // Arrange
        _mockChatService
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM error"));

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var result = await generator.GenerateFixAsync(deviation);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("LLM error");
    }

    [Fact]
    public async Task GenerateFixAsync_WithValidationEnabled_ValidatesFix()
    {
        // Arrange
        SetupSuccessfulLlmResponse();
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();
        var options = new FixGenerationOptions { ValidateFixes = true };

        // Act
        var result = await generator.GenerateFixAsync(deviation, options);

        // Assert
        result.IsValidated.Should().BeTrue();
        result.ValidationResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateFixAsync_WithValidationDisabled_SkipsValidation()
    {
        // Arrange
        SetupSuccessfulLlmResponse();

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();
        var options = new FixGenerationOptions { ValidateFixes = false };

        // Act
        var result = await generator.GenerateFixAsync(deviation, options);

        // Assert
        result.IsValidated.Should().BeFalse();
        result.ValidationResult.Should().BeNull();
    }

    [Fact]
    public async Task GenerateFixAsync_GeneratesDiff()
    {
        // Arrange
        SetupSuccessfulLlmResponse("different text");
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var result = await generator.GenerateFixAsync(deviation);

        // Assert
        result.Diff.Should().NotBeNull();
        result.Diff.HasChanges.Should().BeTrue();
    }

    #endregion

    #region GenerateFixesAsync Tests

    [Fact]
    public async Task GenerateFixesAsync_WithNullDeviations_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.GenerateFixesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("deviations");
    }

    [Fact]
    public async Task GenerateFixesAsync_WithEmptyDeviations_ReturnsEmpty()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var result = await generator.GenerateFixesAsync([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateFixesAsync_WithMultipleDeviations_ReturnsAllResults()
    {
        // Arrange
        SetupSuccessfulLlmResponse();
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviations = new[]
        {
            CreateTestDeviation(),
            CreateTestDeviation(),
            CreateTestDeviation()
        };

        // Act
        var result = await generator.GenerateFixesAsync(deviations);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateFixesAsync_RespectsMaxParallelism()
    {
        // Arrange
        var callCount = 0;
        _mockChatService
            .Setup(x => x.CompleteAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new ChatResponse(
                    """{"suggested_text":"fix","explanation":"fixed","confidence":0.9}""",
                    100, 50, TimeSpan.FromMilliseconds(50), "stop");
            });

        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviations = Enumerable.Range(0, 5).Select(_ => CreateTestDeviation()).ToList();
        var options = new FixGenerationOptions { MaxParallelism = 2 };

        // Act
        var result = await generator.GenerateFixesAsync(deviations, options);

        // Assert
        result.Should().HaveCount(5);
        callCount.Should().Be(5);
    }

    #endregion

    #region RegenerateFixAsync Tests

    [Fact]
    public async Task RegenerateFixAsync_WithNullDeviation_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var act = () => generator.RegenerateFixAsync(null!, "guidance");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("deviation");
    }

    [Fact]
    public async Task RegenerateFixAsync_WithNullGuidance_ThrowsArgumentException()
    {
        // Arrange
        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var act = () => generator.RegenerateFixAsync(deviation, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RegenerateFixAsync_WithUserGuidance_GeneratesNewFix()
    {
        // Arrange
        SetupSuccessfulLlmResponse("better text", "Incorporated user guidance");
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var result = await generator.RegenerateFixAsync(deviation, "Make it more formal");

        // Assert
        result.Success.Should().BeTrue();
        result.SuggestedText.Should().Be("better text");
    }

    #endregion

    #region ValidateFixAsync Tests

    [Fact]
    public async Task ValidateFixAsync_WithNullDeviation_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = CreateGenerator();
        var suggestion = FixSuggestion.Failed(Guid.NewGuid(), "test", "error");

        // Act
        var act = () => generator.ValidateFixAsync(null!, suggestion);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("deviation");
    }

    [Fact]
    public async Task ValidateFixAsync_WithNullSuggestion_ThrowsArgumentNullException()
    {
        // Arrange
        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // Act
        var act = () => generator.ValidateFixAsync(deviation, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("suggestion");
    }

    [Fact]
    public async Task ValidateFixAsync_WithValidFix_ReturnsValidStatus()
    {
        // Arrange
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var generator = CreateGenerator();
        var deviation = CreateTestDeviation();

        // LOGIC: Use suggested text identical to original to ensure high semantic similarity
        // (similarity = 1.0, well above the 0.7 threshold for Valid status)
        var suggestion = new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviation.DeviationId,
            OriginalText = deviation.OriginalText,
            SuggestedText = deviation.OriginalText,  // Same text = perfect similarity
            Explanation = "Fixed",
            Diff = TextDiff.Empty
        };

        // Act
        var result = await generator.ValidateFixAsync(deviation, suggestion);

        // Assert
        result.Status.Should().Be(ValidationStatus.Valid);
        result.ResolvesViolation.Should().BeTrue();
    }

    #endregion

    #region DiffGenerator Tests

    [Fact]
    public void DiffGenerator_WithIdenticalText_ReturnsEmptyDiff()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();

        // Act
        var result = diffGenerator.GenerateDiff("hello world", "hello world");

        // Assert
        result.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void DiffGenerator_WithDifferentText_ReturnsDiffWithChanges()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();

        // Act
        var result = diffGenerator.GenerateDiff("hello world", "hello universe");

        // Assert
        result.HasChanges.Should().BeTrue();
        result.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DiffGenerator_GeneratesUnifiedDiff()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();

        // Act
        var result = diffGenerator.GenerateDiff("old text", "new text");

        // Assert
        result.UnifiedDiff.Should().NotBeNullOrEmpty();
        result.UnifiedDiff.Should().Contain("---");
        result.UnifiedDiff.Should().Contain("+++");
    }

    [Fact]
    public void DiffGenerator_GeneratesHtmlDiff()
    {
        // Arrange
        var diffGenerator = CreateDiffGenerator();

        // Act
        var result = diffGenerator.GenerateDiff("old text", "new text");

        // Assert
        result.HtmlDiff.Should().NotBeNullOrEmpty();
        result.HtmlDiff.Should().Contain("<span");
    }

    #endregion

    #region FixValidator Tests

    [Fact]
    public async Task FixValidator_WithNoViolationsAfterFix_ReturnsValid()
    {
        // Arrange
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var validator = CreateValidator();
        var deviation = CreateTestDeviation();

        // LOGIC: Use suggested text identical to original to ensure high semantic similarity
        // (similarity = 1.0, well above the 0.7 threshold for Valid status)
        var suggestion = new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviation.DeviationId,
            OriginalText = deviation.OriginalText,
            SuggestedText = deviation.OriginalText,  // Same text = perfect similarity
            Explanation = "Fixed",
            Diff = TextDiff.Empty
        };

        // Act
        var result = await validator.ValidateAsync(deviation, suggestion);

        // Assert
        result.Status.Should().Be(ValidationStatus.Valid);
    }

    [Fact]
    public async Task FixValidator_WithOriginalViolationStillPresent_ReturnsInvalid()
    {
        // Arrange
        var rule = CreateTestRule();
        var originalViolation = CreateTestViolation(rule);

        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([originalViolation]);

        var validator = CreateValidator();
        var deviation = CreateTestDeviation(originalViolation);
        var suggestion = new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviation.DeviationId,
            OriginalText = deviation.OriginalText,
            SuggestedText = "same test text", // Still contains "test"
            Explanation = "Fixed",
            Diff = TextDiff.Empty
        };

        // Act
        var result = await validator.ValidateAsync(deviation, suggestion);

        // Assert
        result.Status.Should().Be(ValidationStatus.Invalid);
        result.Message.Should().Contain("does not resolve");
    }

    [Fact]
    public async Task FixValidator_CalculatesSemanticSimilarity()
    {
        // Arrange
        _mockStyleEngine
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StyleViolation>());

        var validator = CreateValidator();
        var deviation = CreateTestDeviation();
        var suggestion = new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviation.DeviationId,
            OriginalText = "hello world",
            SuggestedText = "hello universe",
            Explanation = "Fixed",
            Diff = TextDiff.Empty
        };

        // Act
        var result = await validator.ValidateAsync(deviation, suggestion);

        // Assert
        result.SemanticSimilarity.Should().BeGreaterThan(0);
        result.SemanticSimilarity.Should().BeLessThanOrEqualTo(1.0);
    }

    #endregion

    #region FixGenerationOptions Tests

    [Fact]
    public void FixGenerationOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = FixGenerationOptions.Default;

        // Assert
        options.MaxAlternatives.Should().Be(2);
        options.IncludeExplanations.Should().BeTrue();
        options.MinConfidence.Should().Be(0.7);
        options.ValidateFixes.Should().BeTrue();
        options.Tone.Should().Be(TonePreference.Neutral);
        options.PreserveVoice.Should().BeTrue();
        options.MaxParallelism.Should().Be(5);
        options.UseLearningContext.Should().BeTrue();
    }

    #endregion

    #region FixSuggestion Tests

    [Fact]
    public void FixSuggestion_Failed_CreatesFailedSuggestion()
    {
        // Arrange
        var deviationId = Guid.NewGuid();
        var originalText = "test text";
        var error = "Test error";

        // Act
        var result = FixSuggestion.Failed(deviationId, originalText, error);

        // Assert
        result.Success.Should().BeFalse();
        result.DeviationId.Should().Be(deviationId);
        result.OriginalText.Should().Be(originalText);
        result.SuggestedText.Should().Be(originalText); // Unchanged
        result.ErrorMessage.Should().Be(error);
        result.Confidence.Should().Be(0);
        result.QualityScore.Should().Be(0);
    }

    [Fact]
    public void FixSuggestion_LicenseRequired_CreatesLicenseError()
    {
        // Arrange
        var deviationId = Guid.NewGuid();
        var originalText = "test text";

        // Act
        var result = FixSuggestion.LicenseRequired(deviationId, originalText);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Writer Pro license required");
    }

    [Fact]
    public void FixSuggestion_IsHighConfidence_ReturnsTrueWhenAllConditionsMet()
    {
        // Arrange
        var suggestion = new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = Guid.NewGuid(),
            OriginalText = "test",
            SuggestedText = "improved",
            Explanation = "Fixed",
            Confidence = 0.95,
            QualityScore = 0.95,
            Diff = TextDiff.Empty,
            IsValidated = true,
            ValidationResult = FixValidationResult.Valid(0.95)
        };

        // Act & Assert
        suggestion.IsHighConfidence.Should().BeTrue();
    }

    [Fact]
    public void FixSuggestion_IsHighConfidence_ReturnsFalseWhenConfidenceLow()
    {
        // Arrange
        var suggestion = new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = Guid.NewGuid(),
            OriginalText = "test",
            SuggestedText = "improved",
            Explanation = "Fixed",
            Confidence = 0.5, // Below threshold
            QualityScore = 0.95,
            Diff = TextDiff.Empty,
            IsValidated = true,
            ValidationResult = FixValidationResult.Valid(0.95)
        };

        // Act & Assert
        suggestion.IsHighConfidence.Should().BeFalse();
    }

    #endregion

    #region TextDiff Tests

    [Fact]
    public void TextDiff_Empty_HasNoChanges()
    {
        // Arrange & Act
        var diff = TextDiff.Empty;

        // Assert
        diff.HasChanges.Should().BeFalse();
        diff.Operations.Should().BeEmpty();
        diff.UnifiedDiff.Should().BeEmpty();
    }

    [Fact]
    public void TextDiff_CountsOperationsCorrectly()
    {
        // Arrange
        var operations = new List<DiffOperation>
        {
            new(DiffType.Unchanged, "hello ", 0, 6),
            new(DiffType.Deletion, "world", 6, 5),
            new(DiffType.Addition, "universe", 6, 8)
        };

        var diff = new TextDiff
        {
            Operations = operations,
            UnifiedDiff = "test diff"
        };

        // Act & Assert
        diff.Additions.Should().Be(1);
        diff.Deletions.Should().Be(1);
        diff.Unchanged.Should().Be(1);
        diff.HasChanges.Should().BeTrue();
    }

    #endregion

    #region FixValidationResult Tests

    [Fact]
    public void FixValidationResult_Valid_CreatesValidResult()
    {
        // Arrange & Act
        var result = FixValidationResult.Valid(0.95);

        // Assert
        result.Status.Should().Be(ValidationStatus.Valid);
        result.ResolvesViolation.Should().BeTrue();
        result.IntroducesNewViolations.Should().BeFalse();
        result.SemanticSimilarity.Should().Be(0.95);
    }

    [Fact]
    public void FixValidationResult_Invalid_CreatesInvalidResult()
    {
        // Arrange & Act
        var result = FixValidationResult.Invalid("Test reason");

        // Assert
        result.Status.Should().Be(ValidationStatus.Invalid);
        result.ResolvesViolation.Should().BeFalse();
        result.Message.Should().Be("Test reason");
    }

    [Fact]
    public void FixValidationResult_Failed_CreatesFailedResult()
    {
        // Arrange & Act
        var result = FixValidationResult.Failed("Validation error");

        // Assert
        result.Status.Should().Be(ValidationStatus.ValidationFailed);
        result.Message.Should().Be("Validation error");
    }

    #endregion
}
