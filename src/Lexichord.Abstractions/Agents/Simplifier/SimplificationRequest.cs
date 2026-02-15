// -----------------------------------------------------------------------
// <copyright file="SimplificationRequest.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents a request to simplify text to a target readability level.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SimplificationRequest"/> encapsulates all parameters
/// needed by the <see cref="ISimplificationPipeline"/> to transform text. Required
/// fields are <see cref="OriginalText"/> and <see cref="Target"/>; all other fields
/// have sensible defaults.
/// </para>
/// <para>
/// <b>Validation:</b>
/// Call <see cref="Validate"/> before invoking the pipeline to catch configuration
/// errors early. Alternatively, use <see cref="ISimplificationPipeline.ValidateRequest"/>
/// for a non-throwing validation check.
/// </para>
/// <para>
/// <b>Strategy Selection:</b>
/// The <see cref="Strategy"/> property controls how aggressively text is transformed:
/// <list type="bullet">
///   <item><description><see cref="SimplificationStrategy.Conservative"/>: Minimal changes, high fidelity</description></item>
///   <item><description><see cref="SimplificationStrategy.Balanced"/>: Moderate changes (default)</description></item>
///   <item><description><see cref="SimplificationStrategy.Aggressive"/>: Maximum simplification</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a basic request
/// var request = new SimplificationRequest
/// {
///     OriginalText = complexText,
///     Target = await targetService.GetTargetAsync(presetId: "general-public")
/// };
///
/// // Creating a fully-configured request
/// var request = new SimplificationRequest
/// {
///     OriginalText = legalDocument,
///     Target = ReadabilityTarget.FromExplicit(
///         targetGradeLevel: 8.0,
///         maxSentenceLength: 20,
///         avoidJargon: true),
///     DocumentPath = documentUri.AbsolutePath,
///     Strategy = SimplificationStrategy.Conservative,
///     GenerateGlossary = true,
///     PreserveFormatting = true,
///     AdditionalInstructions = "Preserve all legal terminology but add explanations.",
///     Timeout = TimeSpan.FromSeconds(90)
/// };
///
/// // Validate before processing
/// request.Validate(); // Throws if invalid
/// </code>
/// </example>
/// <seealso cref="SimplificationResult"/>
/// <seealso cref="ISimplificationPipeline"/>
/// <seealso cref="ReadabilityTarget"/>
public record SimplificationRequest
{
    /// <summary>
    /// Maximum allowed text length for simplification requests.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> 50,000 characters is approximately 8,000-10,000 words,
    /// which fits comfortably within most LLM context windows while leaving
    /// room for the system prompt, context, and response.
    /// </remarks>
    public const int MaxTextLength = 50_000;

    /// <summary>
    /// Maximum allowed timeout for simplification requests.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> 5 minutes is a reasonable upper limit for long documents.
    /// Most simplifications complete within 30-60 seconds.
    /// </remarks>
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default timeout for simplification requests.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> 60 seconds is sufficient for most text lengths.
    /// </remarks>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets the original text to be simplified.
    /// </summary>
    /// <value>
    /// The source text that will be transformed. Must not be null, empty, or whitespace.
    /// Maximum length is <see cref="MaxTextLength"/> characters.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> This is the primary input to the simplification process.
    /// The text should be plain text or markdown; HTML is not supported.
    /// </remarks>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Gets the readability target for the simplification.
    /// </summary>
    /// <value>
    /// A <see cref="ReadabilityTarget"/> containing the target grade level,
    /// tolerance, sentence length, and jargon handling preferences.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The target is typically obtained from
    /// <see cref="IReadabilityTargetService.GetTargetAsync"/> using a preset
    /// or explicit parameters.
    /// </remarks>
    public required ReadabilityTarget Target { get; init; }

    /// <summary>
    /// Gets the document path for context gathering.
    /// </summary>
    /// <value>
    /// Optional path to the document containing the text. When provided,
    /// enables surrounding context, style rules, and terminology extraction.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The path is used by the <see cref="IContextOrchestrator"/>
    /// to gather relevant context that improves simplification quality.
    /// </remarks>
    public string? DocumentPath { get; init; }

    /// <summary>
    /// Gets the simplification strategy.
    /// </summary>
    /// <value>
    /// The intensity level for text transformation. Defaults to
    /// <see cref="SimplificationStrategy.Balanced"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Strategy affects LLM temperature and prompt instructions:
    /// Conservative (0.3), Balanced (0.4), Aggressive (0.5).
    /// </remarks>
    public SimplificationStrategy Strategy { get; init; } = SimplificationStrategy.Balanced;

    /// <summary>
    /// Gets a value indicating whether to generate a glossary of simplified terms.
    /// </summary>
    /// <value>
    /// <c>true</c> to include a glossary mapping technical terms to their
    /// simplified equivalents; otherwise, <c>false</c>. Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, the LLM includes a glossary in its response,
    /// which is parsed and included in <see cref="SimplificationResult.Glossary"/>.
    /// </remarks>
    public bool GenerateGlossary { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to preserve text formatting.
    /// </summary>
    /// <value>
    /// <c>true</c> to preserve paragraph breaks, bullet lists, and other
    /// formatting; otherwise, <c>false</c>. Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When true, the LLM is instructed to maintain the
    /// original structure and formatting of the text.
    /// </remarks>
    public bool PreserveFormatting { get; init; } = true;

    /// <summary>
    /// Gets additional instructions for the simplification process.
    /// </summary>
    /// <value>
    /// Optional user-provided instructions that are appended to the prompt.
    /// May include specific guidance about terminology, tone, or content.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Additional instructions allow users to customize the
    /// simplification beyond what the strategy and target provide.
    /// </remarks>
    public string? AdditionalInstructions { get; init; }

    /// <summary>
    /// Gets the timeout for the simplification operation.
    /// </summary>
    /// <value>
    /// Maximum time to wait for the LLM response. Defaults to 60 seconds.
    /// Maximum allowed is 5 minutes (<see cref="MaxTimeout"/>).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Timeout protects against hung requests and allows
    /// graceful degradation when the LLM is slow.
    /// </remarks>
    public TimeSpan Timeout { get; init; } = DefaultTimeout;

    /// <summary>
    /// Validates the request and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when validation fails. The message describes the validation error.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Performs the following validation checks:
    /// </para>
    /// <list type="number">
    ///   <item><description><see cref="OriginalText"/> is not null, empty, or whitespace</description></item>
    ///   <item><description><see cref="OriginalText"/> length does not exceed <see cref="MaxTextLength"/></description></item>
    ///   <item><description><see cref="Target"/> is not null</description></item>
    ///   <item><description><see cref="Strategy"/> is a valid enum value</description></item>
    ///   <item><description><see cref="Timeout"/> is positive</description></item>
    ///   <item><description><see cref="Timeout"/> does not exceed <see cref="MaxTimeout"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     request.Validate();
    ///     var result = await pipeline.SimplifyAsync(request, ct);
    /// }
    /// catch (ArgumentException ex)
    /// {
    ///     Console.WriteLine($"Invalid request: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public void Validate()
    {
        // LOGIC: Validate OriginalText is not null, empty, or whitespace
        if (string.IsNullOrWhiteSpace(OriginalText))
        {
            throw new ArgumentException(
                "Original text is required and cannot be empty or whitespace.",
                nameof(OriginalText));
        }

        // LOGIC: Validate OriginalText length
        if (OriginalText.Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"Original text exceeds maximum length of {MaxTextLength:N0} characters. " +
                $"Current length: {OriginalText.Length:N0} characters.",
                nameof(OriginalText));
        }

        // LOGIC: Validate Target is not null
        if (Target is null)
        {
            throw new ArgumentException(
                "Readability target is required.",
                nameof(Target));
        }

        // LOGIC: Validate Strategy is a valid enum value
        if (!Enum.IsDefined(typeof(SimplificationStrategy), Strategy))
        {
            throw new ArgumentException(
                $"Invalid simplification strategy: {Strategy}.",
                nameof(Strategy));
        }

        // LOGIC: Validate Timeout is positive
        if (Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Timeout must be positive.",
                nameof(Timeout));
        }

        // LOGIC: Validate Timeout does not exceed maximum
        if (Timeout > MaxTimeout)
        {
            throw new ArgumentException(
                $"Timeout exceeds maximum of {MaxTimeout.TotalMinutes:N0} minutes.",
                nameof(Timeout));
        }
    }
}
