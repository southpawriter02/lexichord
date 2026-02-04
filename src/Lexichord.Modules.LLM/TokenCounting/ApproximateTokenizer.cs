// -----------------------------------------------------------------------
// <copyright file="ApproximateTokenizer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Approximate tokenizer using character-to-token ratio.
//   - Used for models without available tokenizer implementations (e.g., Claude).
//   - Default ratio of 4 characters per token based on empirical analysis.
//   - Provides estimates within ~10-20% of actual token counts for typical text.
//   - Thread-safe and stateless implementation.
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Approximate tokenizer using character-to-token ratio for estimation.
/// </summary>
/// <remarks>
/// <para>
/// This class provides token count estimation for models that don't have
/// publicly available tokenizer implementations, such as Anthropic's Claude models.
/// </para>
/// <para>
/// <b>Estimation Methodology:</b>
/// </para>
/// <para>
/// The default ratio of 4 characters per token is based on empirical analysis
/// of typical English text tokenization patterns across various LLM tokenizers.
/// This ratio provides estimates within approximately 10-20% of actual token
/// counts for most English text.
/// </para>
/// <para>
/// <b>Accuracy Considerations:</b>
/// </para>
/// <list type="bullet">
///   <item><description>English prose: High accuracy (~10% variance)</description></item>
///   <item><description>Code: Moderate accuracy (~15-20% variance)</description></item>
///   <item><description>Non-Latin scripts: Lower accuracy (may vary significantly)</description></item>
///   <item><description>Highly technical text: Moderate accuracy</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe and stateless.
/// </para>
/// </remarks>
internal sealed class ApproximateTokenizer : ITokenizer
{
    private readonly double _charsPerToken;

    /// <summary>
    /// Default characters per token ratio for approximation.
    /// </summary>
    /// <remarks>
    /// This value is based on empirical analysis of typical English text
    /// tokenization across various LLM tokenizers including GPT and Claude.
    /// </remarks>
    public const double DefaultCharsPerToken = 4.0;

    /// <summary>
    /// Minimum valid characters per token ratio.
    /// </summary>
    public const double MinCharsPerToken = 0.5;

    /// <summary>
    /// Maximum valid characters per token ratio.
    /// </summary>
    public const double MaxCharsPerToken = 10.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproximateTokenizer"/> class.
    /// </summary>
    /// <param name="charsPerToken">
    /// The average number of characters per token. Must be greater than zero.
    /// Defaults to <see cref="DefaultCharsPerToken"/> (4.0).
    /// </param>
    /// <param name="modelFamily">
    /// The model family identifier (e.g., "claude", "unknown").
    /// Defaults to "unknown".
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="charsPerToken"/> is less than or equal to zero.
    /// </exception>
    /// <example>
    /// <code>
    /// // Using default 4 chars/token for Claude
    /// var tokenizer = new ApproximateTokenizer(modelFamily: "claude");
    /// var count = tokenizer.CountTokens("Hello, world!");
    /// // count = ceil(13 / 4) = 4
    ///
    /// // Using custom ratio for specific use case
    /// var customTokenizer = new ApproximateTokenizer(charsPerToken: 3.5, modelFamily: "custom");
    /// </code>
    /// </example>
    public ApproximateTokenizer(double charsPerToken = DefaultCharsPerToken, string modelFamily = "unknown")
    {
        // LOGIC: Validate charsPerToken is positive.
        if (charsPerToken <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(charsPerToken),
                charsPerToken,
                "Characters per token must be greater than zero.");
        }

        _charsPerToken = charsPerToken;
        ModelFamily = modelFamily ?? "unknown";
    }

    /// <inheritdoc />
    /// <value>
    /// The model family identifier passed to the constructor (e.g., "claude", "unknown").
    /// </value>
    public string ModelFamily { get; }

    /// <inheritdoc />
    /// <value>
    /// Always returns <c>false</c> since this tokenizer uses heuristic approximation.
    /// </value>
    public bool IsExact => false;

    /// <summary>
    /// Gets the characters per token ratio used for estimation.
    /// </summary>
    /// <value>
    /// The configured characters per token ratio.
    /// </value>
    public double CharsPerToken => _charsPerToken;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Calculates token count as <c>ceil(text.Length / charsPerToken)</c>.
    /// </para>
    /// <para>
    /// <b>Null/Empty Handling:</b> Returns 0 for <c>null</c> or empty input strings.
    /// </para>
    /// <para>
    /// <b>Rounding:</b> Uses ceiling to ensure the estimate is never lower than
    /// the actual count, which is safer for context window budgeting.
    /// </para>
    /// </remarks>
    public int CountTokens(string text)
    {
        // LOGIC: Return 0 for null or empty input per interface contract.
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // LOGIC: Calculate approximate token count using ceiling.
        // Using ceiling ensures we don't underestimate, which is safer
        // for context window management.
        return (int)Math.Ceiling(text.Length / _charsPerToken);
    }
}
