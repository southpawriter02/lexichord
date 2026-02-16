// -----------------------------------------------------------------------
// <copyright file="FixSuggestionGenerator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Generates AI-powered fix suggestions for style deviations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="FixSuggestionGenerator"/> is the core implementation of
/// <see cref="IFixSuggestionGenerator"/> (v0.7.5b). It takes style deviations with context
/// and generates intelligent fix suggestions using LLM:
/// <list type="bullet">
///   <item><description>Constructs prompts using the tuning-agent-fix template</description></item>
///   <item><description>Invokes LLM via <see cref="IChatCompletionService"/></description></item>
///   <item><description>Parses JSON responses with robust fallback handling</description></item>
///   <item><description>Generates diffs using <see cref="DiffGenerator"/></description></item>
///   <item><description>Validates fixes using <see cref="FixValidator"/></description></item>
///   <item><description>Calculates confidence and quality scores</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirement:</b> Requires WriterPro tier or higher.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe. Uses <see cref="SemaphoreSlim"/> for batch
/// parallelism control.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// <b>Updated in:</b> v0.7.5d — added <c>ILearningLoopService</c> for learning-enhanced prompt context.
/// </para>
/// </remarks>
public sealed class FixSuggestionGenerator : IFixSuggestionGenerator
{
    #region Constants

    /// <summary>
    /// The template ID for the fix generation prompt.
    /// </summary>
    private const string TemplateId = "tuning-agent-fix";

    /// <summary>
    /// JSON serializer options for parsing LLM responses.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    #endregion

    #region Fields

    private readonly IChatCompletionService _chatService;
    private readonly IPromptRenderer _promptRenderer;
    private readonly IPromptTemplateRepository _templateRepository;
    private readonly DiffGenerator _diffGenerator;
    private readonly FixValidator _validator;
    private readonly ILearningLoopService? _learningLoop;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<FixSuggestionGenerator> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="FixSuggestionGenerator"/> class.
    /// </summary>
    /// <param name="chatService">The chat completion service for LLM communication.</param>
    /// <param name="promptRenderer">The prompt renderer for template rendering.</param>
    /// <param name="templateRepository">The template repository for prompt templates.</param>
    /// <param name="diffGenerator">The diff generator for creating text diffs.</param>
    /// <param name="validator">The fix validator for validating suggestions.</param>
    /// <param name="learningLoop">The learning loop service for feedback-enhanced prompts (nullable — added in v0.7.5d).</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required (non-nullable) dependency is null.</exception>
    public FixSuggestionGenerator(
        IChatCompletionService chatService,
        IPromptRenderer promptRenderer,
        IPromptTemplateRepository templateRepository,
        DiffGenerator diffGenerator,
        FixValidator validator,
        ILearningLoopService? learningLoop,
        ILicenseContext licenseContext,
        ILogger<FixSuggestionGenerator> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _promptRenderer = promptRenderer ?? throw new ArgumentNullException(nameof(promptRenderer));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _diffGenerator = diffGenerator ?? throw new ArgumentNullException(nameof(diffGenerator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _learningLoop = learningLoop;
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "FixSuggestionGenerator initialized (LearningLoop: {Available})",
            learningLoop is not null ? "available" : "not available");
    }

    #endregion

    #region IFixSuggestionGenerator Implementation

    /// <inheritdoc />
    public async Task<FixSuggestion> GenerateFixAsync(
        StyleDeviation deviation,
        FixGenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deviation);
        options ??= FixGenerationOptions.Default;

        // LOGIC: Check license tier first - WriterPro required
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogWarning(
                "License check failed: Writer Pro required for fix generation. Current tier: {Tier}",
                _licenseContext.GetCurrentTier());
            return FixSuggestion.LicenseRequired(deviation.DeviationId, deviation.OriginalText);
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Generating fix for deviation: {DeviationId} (Rule: {RuleId})",
            deviation.DeviationId,
            deviation.RuleId);

        try
        {
            // LOGIC: Build prompt context from deviation and options
            var promptContext = BuildPromptContext(deviation, options);

            // LOGIC: Get prompt template
            var template = _templateRepository.GetTemplate(TemplateId);
            if (template is null)
            {
                _logger.LogError("Template not found: {TemplateId}", TemplateId);
                return FixSuggestion.Failed(
                    deviation.DeviationId,
                    deviation.OriginalText,
                    $"Template not found: {TemplateId}");
            }

            // LOGIC: Render messages using prompt template
            var messages = _promptRenderer.RenderMessages(template, promptContext);

            // LOGIC: Create chat request with low temperature for consistency
            var chatOptions = new ChatOptions
            {
                Temperature = 0.2f,
                MaxTokens = 1024
            };
            var request = new ChatRequest(
                [.. messages],
                chatOptions);

            // LOGIC: Call LLM
            _logger.LogDebug("Invoking LLM for fix generation");
            var response = await _chatService.CompleteAsync(request, ct);

            // LOGIC: Parse LLM response
            var llmResponse = ParseLlmResponse(response.Content);

            // LOGIC: Generate diff
            var diff = _diffGenerator.GenerateDiff(
                deviation.OriginalText,
                llmResponse.SuggestedText);

            // LOGIC: Validate if enabled
            FixValidationResult? validationResult = null;
            if (options.ValidateFixes)
            {
                var preliminarySuggestion = CreatePreliminarySuggestion(
                    deviation,
                    llmResponse,
                    diff);

                validationResult = await _validator.ValidateAsync(
                    deviation,
                    preliminarySuggestion,
                    ct);

                _logger.LogDebug(
                    "Validation result: {Status}",
                    validationResult.Status);
            }

            // LOGIC: Calculate confidence and quality scores
            var confidence = CalculateConfidence(
                llmResponse.Confidence,
                validationResult);

            var quality = CalculateQuality(
                deviation.OriginalText,
                llmResponse.SuggestedText,
                validationResult);

            stopwatch.Stop();

            _logger.LogInformation(
                "Fix generated: confidence={Confidence:F2}, quality={Quality:F2}, duration={Duration}ms",
                confidence,
                quality,
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Build final suggestion
            return new FixSuggestion
            {
                SuggestionId = Guid.NewGuid(),
                DeviationId = deviation.DeviationId,
                OriginalText = deviation.OriginalText,
                SuggestedText = llmResponse.SuggestedText,
                Explanation = llmResponse.Explanation,
                Confidence = confidence,
                QualityScore = quality,
                Diff = diff,
                TokenUsage = new UsageMetrics(
                    response.PromptTokens,
                    response.CompletionTokens,
                    0m), // Cost calculated separately
                GenerationTime = stopwatch.Elapsed,
                Alternatives = llmResponse.Alternatives?
                    .Select(a => new AlternativeSuggestion(a.Text, a.Explanation, a.Confidence))
                    .ToList(),
                IsValidated = options.ValidateFixes,
                ValidationResult = validationResult
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Fix generation cancelled for deviation: {DeviationId}", deviation.DeviationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Fix generation failed for deviation: {DeviationId}",
                deviation.DeviationId);

            return FixSuggestion.Failed(
                deviation.DeviationId,
                deviation.OriginalText,
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FixSuggestion>> GenerateFixesAsync(
        IReadOnlyList<StyleDeviation> deviations,
        FixGenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deviations);
        options ??= FixGenerationOptions.Default;

        if (deviations.Count == 0)
        {
            _logger.LogDebug("GenerateFixesAsync called with empty deviations list");
            return Array.Empty<FixSuggestion>();
        }

        _logger.LogInformation(
            "Batch generating fixes for {Count} deviations (parallelism: {Parallelism})",
            deviations.Count,
            options.MaxParallelism);

        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Use SemaphoreSlim to limit parallelism
        using var semaphore = new SemaphoreSlim(options.MaxParallelism);

        var tasks = deviations.Select(async deviation =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                return await GenerateFixAsync(deviation, options, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();
        _logger.LogInformation(
            "Batch generation completed: {Success}/{Total} successful in {ElapsedMs}ms",
            results.Count(r => r.Success),
            results.Length,
            stopwatch.ElapsedMilliseconds);

        return results;
    }

    /// <inheritdoc />
    public async Task<FixSuggestion> RegenerateFixAsync(
        StyleDeviation deviation,
        string userGuidance,
        FixGenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deviation);
        ArgumentException.ThrowIfNullOrWhiteSpace(userGuidance);

        _logger.LogDebug(
            "Regenerating fix with user guidance: {DeviationId}",
            deviation.DeviationId);

        options ??= FixGenerationOptions.Default;

        // LOGIC: Check license tier
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            return FixSuggestion.LicenseRequired(deviation.DeviationId, deviation.OriginalText);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // LOGIC: Build prompt context with user guidance
            var promptContext = BuildPromptContext(deviation, options);
            promptContext["user_guidance"] = userGuidance;

            // LOGIC: Get and render template
            var template = _templateRepository.GetTemplate(TemplateId);
            if (template is null)
            {
                return FixSuggestion.Failed(
                    deviation.DeviationId,
                    deviation.OriginalText,
                    $"Template not found: {TemplateId}");
            }

            var messages = _promptRenderer.RenderMessages(template, promptContext);

            // LOGIC: Create request
            var request = new ChatRequest(
                [.. messages],
                new ChatOptions { Temperature = 0.3f, MaxTokens = 1024 });

            // LOGIC: Call LLM
            var response = await _chatService.CompleteAsync(request, ct);
            var llmResponse = ParseLlmResponse(response.Content);

            // LOGIC: Generate diff and validate
            var diff = _diffGenerator.GenerateDiff(
                deviation.OriginalText,
                llmResponse.SuggestedText);

            FixValidationResult? validationResult = null;
            if (options.ValidateFixes)
            {
                var preliminarySuggestion = CreatePreliminarySuggestion(
                    deviation,
                    llmResponse,
                    diff);

                validationResult = await _validator.ValidateAsync(
                    deviation,
                    preliminarySuggestion,
                    ct);
            }

            var confidence = CalculateConfidence(llmResponse.Confidence, validationResult);
            var quality = CalculateQuality(
                deviation.OriginalText,
                llmResponse.SuggestedText,
                validationResult);

            stopwatch.Stop();

            return new FixSuggestion
            {
                SuggestionId = Guid.NewGuid(),
                DeviationId = deviation.DeviationId,
                OriginalText = deviation.OriginalText,
                SuggestedText = llmResponse.SuggestedText,
                Explanation = llmResponse.Explanation,
                Confidence = confidence,
                QualityScore = quality,
                Diff = diff,
                TokenUsage = new UsageMetrics(
                    response.PromptTokens,
                    response.CompletionTokens,
                    0m),
                GenerationTime = stopwatch.Elapsed,
                Alternatives = llmResponse.Alternatives?
                    .Select(a => new AlternativeSuggestion(a.Text, a.Explanation, a.Confidence))
                    .ToList(),
                IsValidated = options.ValidateFixes,
                ValidationResult = validationResult
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Regeneration failed for deviation: {DeviationId}",
                deviation.DeviationId);

            return FixSuggestion.Failed(
                deviation.DeviationId,
                deviation.OriginalText,
                ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<FixValidationResult> ValidateFixAsync(
        StyleDeviation deviation,
        FixSuggestion suggestion,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deviation);
        ArgumentNullException.ThrowIfNull(suggestion);

        return _validator.ValidateAsync(deviation, suggestion, ct);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Builds the prompt context dictionary from deviation, options, and optional learning context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Constructs the template variable dictionary that the prompt renderer uses
    /// to fill in the fix generation template. When a <see cref="ILearningLoopService"/> is
    /// available, also fetches the <see cref="LearningContext"/> for the deviation's rule and
    /// injects the <see cref="LearningContext.PromptEnhancement"/> into the context dictionary.
    /// </para>
    /// <para>
    /// <b>Updated in:</b> v0.7.5d — changed from <c>static</c> to instance method and added
    /// learning loop integration for feedback-enhanced prompts.
    /// </para>
    /// </remarks>
    /// <param name="deviation">The style deviation.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>A dictionary of prompt context variables.</returns>
    private Dictionary<string, object> BuildPromptContext(
        StyleDeviation deviation,
        FixGenerationOptions options)
    {
        var context = new Dictionary<string, object>
        {
            ["rule_id"] = deviation.RuleId,
            ["rule_name"] = deviation.ViolatedRule.Name,
            ["rule_description"] = deviation.ViolatedRule.Description ?? string.Empty,
            ["rule_category"] = deviation.Category,
            ["violation_message"] = deviation.Message,
            ["violation_severity"] = deviation.Priority.ToString(),
            ["original_text"] = deviation.OriginalText,
            ["max_alternatives"] = options.MaxAlternatives,
            ["preserve_voice"] = options.PreserveVoice
        };

        // LOGIC: Add optional context variables
        if (!string.IsNullOrEmpty(deviation.SurroundingContext))
        {
            context["surrounding_context"] = deviation.SurroundingContext;
        }

        if (!string.IsNullOrEmpty(deviation.LinterSuggestedFix))
        {
            context["linter_suggested_fix"] = deviation.LinterSuggestedFix;
        }

        if (options.Tone != TonePreference.Neutral)
        {
            context["tone"] = options.Tone.ToString().ToLowerInvariant();
        }

        // LOGIC: v0.7.5d — Inject learning context from feedback history.
        // When the learning loop service is available, fetch the learning context for
        // this rule and add the prompt enhancement to the template context. This allows
        // the LLM to incorporate user preference patterns from past accept/reject decisions.
        // Wrapped in try/catch for resilience — learning loop failures should never
        // prevent fix generation.
        if (_learningLoop is not null)
        {
            try
            {
                // LOGIC: Use synchronous blocking here because BuildPromptContext is called
                // within an already-async pipeline. The learning context is cached and fast.
                var learningContext = _learningLoop
                    .GetLearningContextAsync(deviation.RuleId, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                if (!string.IsNullOrEmpty(learningContext.PromptEnhancement))
                {
                    context["learning_enhancement"] = learningContext.PromptEnhancement;
                    _logger.LogDebug(
                        "Added learning enhancement for rule {RuleId} ({AcceptedPatterns} accepted, {RejectedPatterns} rejected patterns)",
                        deviation.RuleId,
                        learningContext.AcceptedPatterns.Count,
                        learningContext.RejectedPatterns.Count);
                }
                else
                {
                    _logger.LogTrace(
                        "No learning enhancement available for rule {RuleId} (insufficient feedback data)",
                        deviation.RuleId);
                }
            }
            catch (Exception ex)
            {
                // LOGIC: Learning loop failures are non-fatal — log and continue without enhancement
                _logger.LogWarning(
                    ex,
                    "Failed to retrieve learning context for rule {RuleId}; proceeding without learning enhancement",
                    deviation.RuleId);
            }
        }

        return context;
    }

    /// <summary>
    /// Creates a preliminary fix suggestion for validation.
    /// </summary>
    private static FixSuggestion CreatePreliminarySuggestion(
        StyleDeviation deviation,
        LlmFixResponse llmResponse,
        TextDiff diff)
    {
        return new FixSuggestion
        {
            SuggestionId = Guid.NewGuid(),
            DeviationId = deviation.DeviationId,
            OriginalText = deviation.OriginalText,
            SuggestedText = llmResponse.SuggestedText,
            Explanation = llmResponse.Explanation,
            Diff = diff
        };
    }

    /// <summary>
    /// Parses the LLM response content into structured fix data.
    /// </summary>
    /// <param name="responseContent">The raw LLM response content.</param>
    /// <returns>Parsed fix response data.</returns>
    private LlmFixResponse ParseLlmResponse(string responseContent)
    {
        var json = ExtractJson(responseContent);

        try
        {
            var response = JsonSerializer.Deserialize<LlmFixResponse>(json, JsonOptions);

            if (response is null)
            {
                _logger.LogWarning("LLM response parsed to null, using fallback");
                return ParseFallback(responseContent);
            }

            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parse failed, attempting fallback");
            return ParseFallback(responseContent);
        }
    }

    /// <summary>
    /// Extracts JSON from response content which may be wrapped in markdown code blocks.
    /// </summary>
    /// <param name="content">The raw content.</param>
    /// <returns>The extracted JSON string.</returns>
    private static string ExtractJson(string content)
    {
        // LOGIC: Try to find JSON in code blocks first
        var codeBlockMatch = Regex.Match(content, @"```(?:json)?\s*([\s\S]*?)\s*```");
        if (codeBlockMatch.Success)
        {
            return codeBlockMatch.Groups[1].Value.Trim();
        }

        // LOGIC: Try to find raw JSON object
        var objectMatch = Regex.Match(content, @"\{[\s\S]*\}");
        if (objectMatch.Success)
        {
            return objectMatch.Value;
        }

        // LOGIC: Return original content as fallback
        return content;
    }

    /// <summary>
    /// Fallback parser when JSON parsing fails.
    /// </summary>
    /// <param name="content">The raw content.</param>
    /// <returns>A basic fix response extracted from plain text.</returns>
    private LlmFixResponse ParseFallback(string content)
    {
        _logger.LogDebug("Using fallback parser for response");

        // LOGIC: Use the entire response as the suggested text
        return new LlmFixResponse
        {
            SuggestedText = content.Trim(),
            Explanation = "Unable to parse structured response",
            Confidence = 0.5,
            Alternatives = null
        };
    }

    /// <summary>
    /// Calculates the confidence score for a suggestion.
    /// </summary>
    /// <param name="llmConfidence">The LLM's self-reported confidence.</param>
    /// <param name="validation">The validation result, if available.</param>
    /// <returns>A confidence score between 0.0 and 1.0.</returns>
    private static double CalculateConfidence(
        double llmConfidence,
        FixValidationResult? validation)
    {
        var confidence = llmConfidence;

        // LOGIC: Adjust based on validation results
        if (validation is not null)
        {
            confidence *= validation.Status switch
            {
                ValidationStatus.Valid => 1.05,
                ValidationStatus.ValidWithWarnings => 0.85,
                ValidationStatus.Invalid => 0.5,
                _ => 1.0
            };
        }

        return Math.Clamp(confidence, 0.0, 1.0);
    }

    /// <summary>
    /// Calculates the quality score based on semantic preservation.
    /// </summary>
    /// <param name="original">The original text.</param>
    /// <param name="suggested">The suggested text.</param>
    /// <param name="validation">The validation result, if available.</param>
    /// <returns>A quality score between 0.0 and 1.0.</returns>
    private static double CalculateQuality(
        string original,
        string suggested,
        FixValidationResult? validation)
    {
        // LOGIC: Start with semantic similarity from validation
        var quality = validation?.SemanticSimilarity ?? 0.8;

        // LOGIC: Penalize significant length changes
        var lengthRatio = (double)suggested.Length / Math.Max(original.Length, 1);
        if (lengthRatio < 0.5 || lengthRatio > 2.0)
        {
            quality *= 0.8;
        }
        else if (lengthRatio < 0.7 || lengthRatio > 1.5)
        {
            quality *= 0.9;
        }

        return Math.Clamp(quality, 0.0, 1.0);
    }

    #endregion

    #region Internal DTOs

    /// <summary>
    /// Internal DTO for deserializing LLM fix responses.
    /// </summary>
    private sealed class LlmFixResponse
    {
        [JsonPropertyName("suggested_text")]
        public string SuggestedText { get; init; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string Explanation { get; init; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; } = 0.8;

        [JsonPropertyName("alternatives")]
        public IReadOnlyList<LlmAlternative>? Alternatives { get; init; }
    }

    /// <summary>
    /// Internal DTO for deserializing alternative suggestions.
    /// </summary>
    private sealed class LlmAlternative
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;

        [JsonPropertyName("explanation")]
        public string Explanation { get; init; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; } = 0.7;
    }

    #endregion
}
