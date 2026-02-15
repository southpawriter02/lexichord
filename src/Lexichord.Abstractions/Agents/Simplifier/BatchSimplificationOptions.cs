// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Configuration options for batch simplification operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationOptions"/> record controls how
/// the <see cref="IBatchSimplificationService"/> processes documents. Options include
/// scope selection, skip detection rules, concurrency limits, and rate limiting.
/// </para>
/// <para>
/// <b>Default Behavior:</b>
/// <list type="bullet">
///   <item><description>Processes entire document (<see cref="Scope"/> = EntireDocument)</description></item>
///   <item><description>Skips already-simple paragraphs (<see cref="SkipAlreadySimple"/> = true)</description></item>
///   <item><description>Skips headings and code blocks (<see cref="SkipHeadings"/>, <see cref="SkipCodeBlocks"/> = true)</description></item>
///   <item><description>Sequential processing (<see cref="MaxConcurrency"/> = 1)</description></item>
///   <item><description>No delay between paragraphs (<see cref="DelayBetweenParagraphs"/> = Zero)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Skip Detection:</b>
/// Paragraphs are evaluated against multiple skip conditions:
/// <list type="number">
///   <item><description>Already at target level: Grade ≤ Target + <see cref="GradeLevelTolerance"/></description></item>
///   <item><description>Too short: WordCount &lt; <see cref="MinParagraphWords"/></description></item>
///   <item><description>Structural elements: Headings, code blocks, blockquotes (if configured)</description></item>
///   <item><description>Limit reached: Index ≥ <see cref="MaxParagraphs"/> (if set)</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Default options (process entire document, skip simple paragraphs)
/// var options = new BatchSimplificationOptions();
///
/// // Custom options for processing only complex paragraphs
/// var options = new BatchSimplificationOptions
/// {
///     Scope = BatchSimplificationScope.ComplexParagraphsOnly,
///     SkipAlreadySimple = true,
///     GradeLevelTolerance = 2,
///     MinParagraphWords = 15,
///     Strategy = SimplificationStrategy.Aggressive,
///     GenerateGlossary = true
/// };
///
/// // Rate-limited processing with concurrency
/// var options = new BatchSimplificationOptions
/// {
///     MaxConcurrency = 2,
///     DelayBetweenParagraphs = TimeSpan.FromMilliseconds(500),
///     MaxParagraphs = 100
/// };
/// </code>
/// </example>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="BatchSimplificationScope"/>
/// <seealso cref="BatchSimplificationResult"/>
public record BatchSimplificationOptions
{
    /// <summary>
    /// Default minimum paragraph word count for processing.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Paragraphs with fewer than 10 words typically don't benefit
    /// from simplification and may produce unreliable readability metrics.
    /// </remarks>
    public const int DefaultMinParagraphWords = 10;

    /// <summary>
    /// Default grade level tolerance for "already simple" detection.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> A tolerance of 1 grade level allows paragraphs slightly above
    /// the target to be skipped, reducing unnecessary processing.
    /// </remarks>
    public const int DefaultGradeLevelTolerance = 1;

    /// <summary>
    /// Gets the scope of batch processing.
    /// </summary>
    /// <value>
    /// Determines which portions of the document are considered for simplification.
    /// Defaults to <see cref="BatchSimplificationScope.EntireDocument"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The scope affects paragraph enumeration but not skip detection—
    /// paragraphs within scope may still be skipped based on other options.
    /// </remarks>
    public BatchSimplificationScope Scope { get; init; } = BatchSimplificationScope.EntireDocument;

    /// <summary>
    /// Gets a value indicating whether to skip paragraphs already at or below target grade level.
    /// </summary>
    /// <value>
    /// <c>true</c> to skip paragraphs that meet the target readability; otherwise, <c>false</c>.
    /// Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Skipping already-simple paragraphs saves tokens and processing time.
    /// The check uses: Grade ≤ TargetGrade + <see cref="GradeLevelTolerance"/>.
    /// </remarks>
    public bool SkipAlreadySimple { get; init; } = true;

    /// <summary>
    /// Gets the tolerance for "already simple" grade level comparison.
    /// </summary>
    /// <value>
    /// Number of grade levels above target that still qualifies as "simple enough".
    /// Defaults to 1.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> A tolerance of 1 means a paragraph at Grade 9 is considered
    /// "already simple" if the target is Grade 8 (9 ≤ 8 + 1).
    /// </remarks>
    public int GradeLevelTolerance { get; init; } = DefaultGradeLevelTolerance;

    /// <summary>
    /// Gets the maximum number of paragraphs to process.
    /// </summary>
    /// <value>
    /// Maximum paragraphs to process, or <c>null</c> for no limit.
    /// Defaults to <c>null</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for testing, previewing, or limiting token costs.
    /// Paragraphs beyond this limit receive <see cref="ParagraphSkipReason.MaxParagraphsReached"/>.
    /// </remarks>
    public int? MaxParagraphs { get; init; }

    /// <summary>
    /// Gets the minimum paragraph length (in words) required for processing.
    /// </summary>
    /// <value>
    /// Minimum word count for a paragraph to be eligible for simplification.
    /// Defaults to 10 words.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Short paragraphs often produce unreliable readability metrics
    /// and don't benefit from simplification. Skipped with <see cref="ParagraphSkipReason.TooShort"/>.
    /// </remarks>
    public int MinParagraphWords { get; init; } = DefaultMinParagraphWords;

    /// <summary>
    /// Gets a value indicating whether to skip heading paragraphs.
    /// </summary>
    /// <value>
    /// <c>true</c> to skip Markdown headings; otherwise, <c>false</c>.
    /// Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Headings are typically short, stylized content that should
    /// be preserved verbatim. Skipped with <see cref="ParagraphSkipReason.IsHeading"/>.
    /// </remarks>
    public bool SkipHeadings { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to skip code block paragraphs.
    /// </summary>
    /// <value>
    /// <c>true</c> to skip fenced and indented code blocks; otherwise, <c>false</c>.
    /// Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Code blocks contain technical content where readability metrics
    /// are meaningless. Skipped with <see cref="ParagraphSkipReason.IsCodeBlock"/>.
    /// </remarks>
    public bool SkipCodeBlocks { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to skip blockquote paragraphs.
    /// </summary>
    /// <value>
    /// <c>true</c> to skip Markdown blockquotes; otherwise, <c>false</c>.
    /// Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Blockquotes often represent quoted material that should be
    /// preserved verbatim. Skipped with <see cref="ParagraphSkipReason.IsBlockquote"/>.
    /// </remarks>
    public bool SkipBlockquotes { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to skip list item paragraphs.
    /// </summary>
    /// <value>
    /// <c>true</c> to skip ordered and unordered list items; otherwise, <c>false</c>.
    /// Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> List items may contain prose that benefits from simplification,
    /// but they are often short and context-dependent. Default is <c>false</c> to
    /// allow processing, but can be enabled if desired.
    /// </remarks>
    public bool SkipListItems { get; init; } = false;

    /// <summary>
    /// Gets the simplification strategy to use.
    /// </summary>
    /// <value>
    /// The intensity level for text transformation.
    /// Defaults to <see cref="SimplificationStrategy.Balanced"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Strategy is passed to each <see cref="SimplificationRequest"/>
    /// for individual paragraph processing.
    /// </remarks>
    public SimplificationStrategy Strategy { get; init; } = SimplificationStrategy.Balanced;

    /// <summary>
    /// Gets a value indicating whether to generate a document-wide glossary.
    /// </summary>
    /// <value>
    /// <c>true</c> to aggregate glossary terms from all paragraphs; otherwise, <c>false</c>.
    /// Defaults to <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, glossary entries from each paragraph are merged
    /// into <see cref="BatchSimplificationResult.AggregateGlossary"/>.
    /// </remarks>
    public bool GenerateGlossary { get; init; } = false;

    /// <summary>
    /// Gets the maximum concurrent paragraph processing limit.
    /// </summary>
    /// <value>
    /// Maximum number of paragraphs to process simultaneously.
    /// Defaults to 1 (sequential processing).
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Sequential processing (1) is the safest option and ensures
    /// deterministic ordering. Higher values may improve throughput but increase
    /// memory usage and API rate limit risk.
    /// </para>
    /// <para>
    /// <b>Note:</b> Current implementation only supports sequential processing.
    /// This property is reserved for future concurrent processing support.
    /// </para>
    /// </remarks>
    public int MaxConcurrency { get; init; } = 1;

    /// <summary>
    /// Gets the delay between paragraph processing calls.
    /// </summary>
    /// <value>
    /// Time to wait between processing each paragraph.
    /// Defaults to <see cref="TimeSpan.Zero"/> (no delay).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for rate limiting API calls to avoid throttling.
    /// Applied after each paragraph completes processing.
    /// </remarks>
    public TimeSpan DelayBetweenParagraphs { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the options are invalid.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Performs the following validation checks:
    /// </para>
    /// <list type="number">
    ///   <item><description><see cref="Scope"/> is a valid enum value</description></item>
    ///   <item><description><see cref="GradeLevelTolerance"/> is non-negative</description></item>
    ///   <item><description><see cref="MaxParagraphs"/> is positive (if set)</description></item>
    ///   <item><description><see cref="MinParagraphWords"/> is non-negative</description></item>
    ///   <item><description><see cref="Strategy"/> is a valid enum value</description></item>
    ///   <item><description><see cref="MaxConcurrency"/> is at least 1</description></item>
    ///   <item><description><see cref="DelayBetweenParagraphs"/> is non-negative</description></item>
    /// </list>
    /// </remarks>
    public void Validate()
    {
        // LOGIC: Validate Scope is a valid enum value
        if (!Enum.IsDefined(typeof(BatchSimplificationScope), Scope))
        {
            throw new ArgumentException(
                $"Invalid batch simplification scope: {Scope}.",
                nameof(Scope));
        }

        // LOGIC: Validate GradeLevelTolerance is non-negative
        if (GradeLevelTolerance < 0)
        {
            throw new ArgumentException(
                $"Grade level tolerance must be non-negative. Current value: {GradeLevelTolerance}.",
                nameof(GradeLevelTolerance));
        }

        // LOGIC: Validate MaxParagraphs is positive (if set)
        if (MaxParagraphs.HasValue && MaxParagraphs.Value <= 0)
        {
            throw new ArgumentException(
                $"Maximum paragraphs must be positive. Current value: {MaxParagraphs.Value}.",
                nameof(MaxParagraphs));
        }

        // LOGIC: Validate MinParagraphWords is non-negative
        if (MinParagraphWords < 0)
        {
            throw new ArgumentException(
                $"Minimum paragraph words must be non-negative. Current value: {MinParagraphWords}.",
                nameof(MinParagraphWords));
        }

        // LOGIC: Validate Strategy is a valid enum value
        if (!Enum.IsDefined(typeof(SimplificationStrategy), Strategy))
        {
            throw new ArgumentException(
                $"Invalid simplification strategy: {Strategy}.",
                nameof(Strategy));
        }

        // LOGIC: Validate MaxConcurrency is at least 1
        if (MaxConcurrency < 1)
        {
            throw new ArgumentException(
                $"Maximum concurrency must be at least 1. Current value: {MaxConcurrency}.",
                nameof(MaxConcurrency));
        }

        // LOGIC: Validate DelayBetweenParagraphs is non-negative
        if (DelayBetweenParagraphs < TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"Delay between paragraphs must be non-negative. Current value: {DelayBetweenParagraphs}.",
                nameof(DelayBetweenParagraphs));
        }
    }

    /// <summary>
    /// Creates default options for processing an entire document.
    /// </summary>
    /// <returns>A new <see cref="BatchSimplificationOptions"/> with default settings.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory method that returns default options.
    /// Equivalent to <c>new BatchSimplificationOptions()</c>.
    /// </remarks>
    public static BatchSimplificationOptions Default => new();

    /// <summary>
    /// Creates options optimized for quick preview processing.
    /// </summary>
    /// <param name="maxParagraphs">Maximum paragraphs to preview. Defaults to 5.</param>
    /// <returns>Options configured for preview mode.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Preview options limit processing to a small number of paragraphs
    /// for quick feedback without committing to full document processing.
    /// </remarks>
    public static BatchSimplificationOptions ForPreview(int maxParagraphs = 5) => new()
    {
        MaxParagraphs = maxParagraphs,
        SkipAlreadySimple = true,
        GenerateGlossary = false
    };

    /// <summary>
    /// Creates options for processing only the most complex paragraphs.
    /// </summary>
    /// <returns>Options targeting complex paragraphs only.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Focuses simplification effort on content that exceeds the
    /// target grade level, maximizing impact per token spent.
    /// </remarks>
    public static BatchSimplificationOptions ForComplexOnly() => new()
    {
        Scope = BatchSimplificationScope.ComplexParagraphsOnly,
        SkipAlreadySimple = true,
        GradeLevelTolerance = 0
    };
}
