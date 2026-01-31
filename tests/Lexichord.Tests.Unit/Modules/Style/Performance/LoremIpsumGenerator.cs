// <copyright file="LoremIpsumGenerator.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Generates reproducible Lorem Ipsum text for performance benchmarks.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8d - Uses a fixed seed to ensure identical text generation
/// across benchmark runs, enabling accurate performance comparisons.
///
/// Thread Safety:
/// - Static methods are thread-safe
/// - Uses deterministic seeding for reproducibility
///
/// Version: v0.3.8d
/// </remarks>
public static class LoremIpsumGenerator
{
    /// <summary>
    /// Fixed seed for reproducible text generation.
    /// </summary>
    private const int DefaultSeed = 42;

    /// <summary>
    /// Standard Lorem Ipsum vocabulary for generating prose.
    /// </summary>
    private static readonly string[] LoremWords =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
        "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
        "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud",
        "exercitation", "ullamco", "laboris", "nisi", "aliquip", "ex", "ea", "commodo",
        "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
        "velit", "esse", "cillum", "fugiat", "nulla", "pariatur", "excepteur", "sint",
        "occaecat", "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia",
        "deserunt", "mollit", "anim", "id", "est", "laborum", "the", "a", "an", "is",
        "was", "were", "been", "being", "have", "has", "had", "having", "does", "did",
        "doing", "would", "should", "could", "ought", "might", "must", "shall", "will",
        "can", "need", "dare", "may", "used", "that", "which", "who", "whom", "what",
        "how", "why", "when", "where", "this", "these", "those", "here", "there"
    ];

    /// <summary>
    /// Passive voice constructions for voice analysis testing.
    /// </summary>
    private static readonly string[] PassiveConstructions =
    [
        "was created by",
        "were written by",
        "has been updated by",
        "had been modified by",
        "is being processed by",
        "are handled by",
        "were discovered by",
        "was implemented by",
        "has been reviewed by",
        "will be completed by",
        "can be seen by",
        "should be done by",
        "must be approved by",
        "was analyzed by",
        "were tested by"
    ];

    /// <summary>
    /// Generates Lorem Ipsum text with the specified word count.
    /// </summary>
    /// <param name="wordCount">The target number of words.</param>
    /// <param name="seed">Random seed for reproducibility (default: 42).</param>
    /// <returns>Generated Lorem Ipsum text.</returns>
    /// <remarks>
    /// LOGIC: Generates deterministic text for consistent benchmark runs.
    /// Words are arranged into sentences (8-15 words) and paragraphs (3-6 sentences).
    /// </remarks>
    public static string Generate(int wordCount, int seed = DefaultSeed)
    {
        if (wordCount <= 0)
        {
            return string.Empty;
        }

        var random = new Random(seed);
        var builder = new System.Text.StringBuilder();
        var wordsGenerated = 0;
        var isNewSentence = true;

        while (wordsGenerated < wordCount)
        {
            // LOGIC: Generate a sentence of 8-15 words
            var sentenceLength = random.Next(8, 16);
            var sentenceWords = new List<string>();

            for (var i = 0; i < sentenceLength && wordsGenerated < wordCount; i++)
            {
                var word = LoremWords[random.Next(LoremWords.Length)];

                // LOGIC: Capitalize first word of sentence
                if (isNewSentence && sentenceWords.Count == 0)
                {
                    word = char.ToUpperInvariant(word[0]) + word[1..];
                }

                sentenceWords.Add(word);
                wordsGenerated++;
            }

            if (sentenceWords.Count > 0)
            {
                builder.Append(string.Join(" ", sentenceWords));
                builder.Append(". ");
                isNewSentence = true;

                // LOGIC: Add paragraph break every 3-6 sentences
                if (random.Next(6) == 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates Lorem Ipsum text with passive voice constructions interspersed.
    /// </summary>
    /// <param name="wordCount">The target number of words.</param>
    /// <param name="passiveRatio">Ratio of sentences that should contain passive voice (0.0-1.0).</param>
    /// <param name="seed">Random seed for reproducibility (default: 42).</param>
    /// <returns>Generated text with passive voice constructions.</returns>
    /// <remarks>
    /// LOGIC: Injects passive voice constructions at the specified ratio
    /// to test passive voice detection accuracy at various densities.
    /// </remarks>
    public static string GenerateWithPassive(int wordCount, double passiveRatio = 0.2, int seed = DefaultSeed)
    {
        if (wordCount <= 0)
        {
            return string.Empty;
        }

        var random = new Random(seed);
        var builder = new System.Text.StringBuilder();
        var wordsGenerated = 0;

        while (wordsGenerated < wordCount)
        {
            // LOGIC: Decide if this sentence should contain passive voice
            var includePassive = random.NextDouble() < passiveRatio;

            if (includePassive)
            {
                // LOGIC: Generate a passive voice sentence
                var passive = PassiveConstructions[random.Next(PassiveConstructions.Length)];
                var subject = LoremWords[random.Next(LoremWords.Length)];
                var objectWord = LoremWords[random.Next(LoremWords.Length)];

                var sentence = $"The {subject} {passive} the {objectWord}.";
                builder.Append(sentence);
                builder.Append(' ');

                // LOGIC: Count words in the passive sentence
                wordsGenerated += sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            }
            else
            {
                // LOGIC: Generate a normal sentence
                var sentenceLength = random.Next(8, 16);
                var sentenceWords = new List<string>();

                for (var i = 0; i < sentenceLength && wordsGenerated < wordCount; i++)
                {
                    var word = LoremWords[random.Next(LoremWords.Length)];

                    if (i == 0)
                    {
                        word = char.ToUpperInvariant(word[0]) + word[1..];
                    }

                    sentenceWords.Add(word);
                    wordsGenerated++;
                }

                if (sentenceWords.Count > 0)
                {
                    builder.Append(string.Join(" ", sentenceWords));
                    builder.Append(". ");
                }
            }

            // LOGIC: Add paragraph break occasionally
            if (random.Next(8) == 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates text of approximately the specified size in kilobytes.
    /// </summary>
    /// <param name="sizeKb">Target size in kilobytes.</param>
    /// <param name="seed">Random seed for reproducibility.</param>
    /// <returns>Generated text.</returns>
    /// <remarks>
    /// LOGIC: Estimates ~5 characters per word average for size calculation.
    /// </remarks>
    public static string GenerateBySize(int sizeKb, int seed = DefaultSeed)
    {
        // LOGIC: Estimate ~5 chars/word + 1 space = 6 chars per word average
        var estimatedWordCount = (sizeKb * 1024) / 6;
        return Generate(estimatedWordCount, seed);
    }
}
