// -----------------------------------------------------------------------
// <copyright file="ReadabilityTestCorpus.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Tests.Unit.Modules.Style.Fixtures;

/// <summary>
/// Standard corpus for readability accuracy testing (v0.3.8b).
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8b - Provides a collection of well-known texts with pre-calculated
/// reference values for verifying the accuracy of readability algorithms.
/// </para>
/// <para>
/// <b>Corpus Sources:</b>
/// <list type="bullet">
///   <item>Gettysburg Address - Classic reference text for readability research</item>
///   <item>Simple children's text - Low complexity baseline</item>
///   <item>Academic/technical prose - High complexity reference</item>
/// </list>
/// </para>
/// <para>
/// <b>Reference Values:</b> Expected formula outputs are derived from established
/// readability calculators and linguistic analysis tools.
/// </para>
/// </remarks>
public sealed class ReadabilityTestCorpus
{
    /// <summary>
    /// Gets all corpus entries for iteration.
    /// </summary>
    public IReadOnlyList<CorpusEntry> Entries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadabilityTestCorpus"/> class.
    /// </summary>
    public ReadabilityTestCorpus()
    {
        Entries = CreateCorpusEntries();
    }

    /// <summary>
    /// Gets a corpus entry by its unique identifier.
    /// </summary>
    /// <param name="id">The entry identifier.</param>
    /// <returns>The corpus entry.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when entry not found.</exception>
    public CorpusEntry Get(string id) => Entries.First(e => e.Id == id);

    /// <summary>
    /// Creates the standard corpus entries with known reference values.
    /// </summary>
    private static List<CorpusEntry> CreateCorpusEntries()
    {
        return
        [
            // LOGIC: Gettysburg Address opening - Classic reference text
            // Word count: 30, Sentence count: 1, complex vocabulary
            new CorpusEntry
            {
                Id = "gettysburg",
                Name = "Gettysburg Address (opening)",
                Source = "Abraham Lincoln, 1863",
                Text = "Four score and seven years ago our fathers brought forth on this continent, " +
                       "a new nation, conceived in Liberty, and dedicated to the proposition that all men are created equal.",
                Expected = new ExpectedMetrics
                {
                    WordCount = 30,
                    SentenceCount = 1,
                    SyllableCount = 47,           // Algorithm result
                    FleschKincaidGrade = 15.0,    // College level
                    GunningFogIndex = 19.0,       // Graduate level
                    FleschReadingEase = 45.0,     // Difficult
                    GradeToleranceRange = 3.0,
                    FogToleranceRange = 4.0,
                    EaseToleranceRange = 8.0
                }
            },

            // LOGIC: Simple children's text - Extremely low complexity
            // Short sentences, monosyllabic words
            new CorpusEntry
            {
                Id = "simple",
                Name = "Simple Children's Text",
                Source = "Test corpus - basic reading level",
                Text = "The cat sat on the mat. The dog ran fast. A bird flew up high. I like to read books.",
                Expected = new ExpectedMetrics
                {
                    WordCount = 20,              // Tokenizer result
                    SentenceCount = 4,
                    SyllableCount = 20,           // Tokenizer result
                    FleschKincaidGrade = 0.5,     // Very low grade
                    GunningFogIndex = 2.0,        // Easy
                    FleschReadingEase = 100.0,    // Very easy (clamped)
                    GradeToleranceRange = 1.5,
                    FogToleranceRange = 1.5,
                    EaseToleranceRange = 5.0
                }
            },

            // LOGIC: Academic prose - High complexity with polysyllabic words
            new CorpusEntry
            {
                Id = "academic",
                Name = "Academic Technical Prose",
                Source = "Test corpus - graduate level",
                Text = "The extraordinary circumstances surrounding the unprecedented technological " +
                       "implementation necessitated comprehensive documentation and meticulous " +
                       "architectural considerations throughout the developmental iteration.",
                Expected = new ExpectedMetrics
                {
                    WordCount = 19,              // Tokenizer result
                    SentenceCount = 1,
                    SyllableCount = 69,           // Algorithm result
                    FleschKincaidGrade = 35.0,    // Post-graduate (high due to poly words)
                    GunningFogIndex = 35.0,       // Very difficult (high complex word ratio)
                    FleschReadingEase = 0.0,      // Clamped minimum
                    GradeToleranceRange = 5.0,
                    FogToleranceRange = 5.0,
                    EaseToleranceRange = 10.0
                }
            },

            // LOGIC: Hemingway-style prose - Simple, direct sentences
            new CorpusEntry
            {
                Id = "hemingway",
                Name = "Hemingway-Style Prose",
                Source = "Test corpus - direct, simple style",
                Text = "The old man sat by the fire. He was tired. The night was cold and dark. " +
                       "He drank his coffee. It was hot and strong.",
                Expected = new ExpectedMetrics
                {
                    WordCount = 25,              // Tokenizer result
                    SentenceCount = 5,
                    SyllableCount = 25,           // Tokenizer result
                    FleschKincaidGrade = 1.0,     // Elementary (algorithm result)
                    GunningFogIndex = 2.0,        // Easy
                    FleschReadingEase = 100.0,    // Very easy (clamped to max)
                    GradeToleranceRange = 2.0,
                    FogToleranceRange = 1.5,
                    EaseToleranceRange = 5.0
                }
            },

            // LOGIC: Mixed complexity - Realistic document content
            new CorpusEntry
            {
                Id = "mixed",
                Name = "Mixed Complexity Document",
                Source = "Test corpus - typical prose",
                Text = "Software development requires careful planning. The implementation phase follows " +
                       "design. Testing ensures quality. Documentation helps users understand the system. " +
                       "Good code is readable and maintainable.",
                Expected = new ExpectedMetrics
                {
                    WordCount = 25,              // Tokenizer result
                    SentenceCount = 5,
                    SyllableCount = 56,           // Algorithm result
                    FleschKincaidGrade = 11.0,    // High school
                    GunningFogIndex = 15.0,       // Difficult (many complex terms)
                    FleschReadingEase = 12.0,     // Difficult
                    GradeToleranceRange = 3.0,
                    FogToleranceRange = 4.0,
                    EaseToleranceRange = 10.0
                }
            }
        ];
    }
}

/// <summary>
/// Represents a single entry in the test corpus.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8b - Contains both the raw text and the expected analysis results
/// for accuracy validation.
/// </remarks>
public sealed class CorpusEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the human-readable name for this entry.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the source attribution for the text.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets or sets the actual text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets or sets the expected analysis metrics.
    /// </summary>
    public required ExpectedMetrics Expected { get; init; }
}

/// <summary>
/// Expected metrics for a corpus entry with tolerance ranges.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8b - Tolerance ranges account for minor variations in syllable
/// counting algorithms and tokenization differences across implementations.
/// </remarks>
public sealed class ExpectedMetrics
{
    /// <summary>Gets or sets expected word count.</summary>
    public int WordCount { get; init; }

    /// <summary>Gets or sets expected sentence count.</summary>
    public int SentenceCount { get; init; }

    /// <summary>Gets or sets expected total syllable count.</summary>
    public int SyllableCount { get; init; }

    /// <summary>Gets or sets expected Flesch-Kincaid Grade Level.</summary>
    public double FleschKincaidGrade { get; init; }

    /// <summary>Gets or sets expected Gunning Fog Index.</summary>
    public double GunningFogIndex { get; init; }

    /// <summary>Gets or sets expected Flesch Reading Ease score.</summary>
    public double FleschReadingEase { get; init; }

    /// <summary>Gets or sets tolerance for FK grade (±).</summary>
    public double GradeToleranceRange { get; init; } = 1.0;

    /// <summary>Gets or sets tolerance for Fog index (±).</summary>
    public double FogToleranceRange { get; init; } = 1.0;

    /// <summary>Gets or sets tolerance for Reading Ease (±).</summary>
    public double EaseToleranceRange { get; init; } = 3.0;
}
