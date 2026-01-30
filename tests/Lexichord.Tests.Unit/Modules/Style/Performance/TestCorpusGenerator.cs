namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Options for generating synthetic test documents.
/// </summary>
/// <param name="TargetSizeBytes">Target size of generated document.</param>
/// <param name="IncludeFrontmatter">Whether to include YAML frontmatter.</param>
/// <param name="IncludeCodeBlocks">Whether to include code blocks.</param>
/// <param name="CodeBlockDensity">Percentage of content in code blocks (0-100).</param>
/// <param name="ViolationDensity">Percentage of terms that should be violations (0-100).</param>
/// <param name="Seed">Random seed for reproducibility (null for random).</param>
/// <remarks>
/// LOGIC: DocumentGenerationOptions controls synthetic document creation
/// for stress testing. All options are deterministic when Seed is provided.
///
/// Version: v0.2.7d
/// </remarks>
public record DocumentGenerationOptions(
    int TargetSizeBytes = 5 * 1024 * 1024,
    bool IncludeFrontmatter = true,
    bool IncludeCodeBlocks = true,
    int CodeBlockDensity = 10,
    int ViolationDensity = 5,
    int? Seed = 42
);

/// <summary>
/// Generates synthetic documents for stress testing.
/// </summary>
/// <remarks>
/// LOGIC: TestCorpusGenerator creates deterministic large documents
/// for benchmarking and stress testing the linting system.
///
/// Generated documents include:
/// - YAML frontmatter header
/// - Prose paragraphs with controlled violation density
/// - Code blocks (excluded from linting)
/// - Realistic line lengths and paragraph structure
///
/// Thread Safety:
/// - Instance methods are not thread-safe
/// - Create separate instances for parallel generation
///
/// Version: v0.2.7d
/// </remarks>
public sealed class TestCorpusGenerator
{
    private readonly Random _random;

    // LOGIC: Sample words for paragraph generation
    private static readonly string[] SampleWords =
    [
        "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing",
        "document", "analysis", "terminology", "style", "guide", "compliance",
        "writing", "prose", "technical", "specification", "requirement",
        "performance", "benchmark", "scanning", "linting", "validation"
    ];

    // LOGIC: Violation terms (intentionally non-standard forms)
    private static readonly string[] ViolationTerms =
    [
        "utilise", "colour", "centre", "behaviour", "analyse",
        "optimise", "recognise", "specialise", "prioritise", "maximise"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCorpusGenerator"/> class.
    /// </summary>
    /// <param name="seed">Random seed for reproducibility.</param>
    public TestCorpusGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Generates a synthetic document with the specified options.
    /// </summary>
    /// <param name="options">Generation options.</param>
    /// <returns>Generated document content.</returns>
    public string GenerateDocument(DocumentGenerationOptions? options = null)
    {
        options ??= new DocumentGenerationOptions();

        var builder = new System.Text.StringBuilder();

        // LOGIC: Add frontmatter if requested
        if (options.IncludeFrontmatter)
        {
            builder.AppendLine("---");
            builder.AppendLine("title: Synthetic Test Document");
            builder.AppendLine("author: Test Generator");
            builder.AppendLine($"date: {DateTime.UtcNow:yyyy-MM-dd}");
            builder.AppendLine("tags: [test, benchmark, stress]");
            builder.AppendLine("---");
            builder.AppendLine();
        }

        // LOGIC: Generate content until target size is reached
        var codeBlockCounter = 0;
        while (builder.Length < options.TargetSizeBytes)
        {
            // LOGIC: Occasionally add code blocks
            if (options.IncludeCodeBlocks && _random.Next(100) < options.CodeBlockDensity)
            {
                builder.AppendLine();
                builder.AppendLine("```csharp");
                builder.AppendLine($"// Code block {++codeBlockCounter}");
                builder.AppendLine("public void ExampleMethod()");
                builder.AppendLine("{");
                builder.AppendLine("    Console.WriteLine(\"Hello, World!\");");
                builder.AppendLine("}");
                builder.AppendLine("```");
                builder.AppendLine();
            }
            else
            {
                // LOGIC: Generate a paragraph
                builder.AppendLine(GenerateParagraph(options.ViolationDensity));
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates a single paragraph with controlled violation density.
    /// </summary>
    private string GenerateParagraph(int violationDensity)
    {
        var sentenceCount = _random.Next(3, 8);
        var sentences = new List<string>();

        for (var i = 0; i < sentenceCount; i++)
        {
            sentences.Add(GenerateSentence(violationDensity));
        }

        return string.Join(" ", sentences);
    }

    /// <summary>
    /// Generates a single sentence with possible violations.
    /// </summary>
    private string GenerateSentence(int violationDensity)
    {
        var wordCount = _random.Next(8, 20);
        var words = new List<string>();

        for (var i = 0; i < wordCount; i++)
        {
            // LOGIC: Inject violations based on density
            if (_random.Next(100) < violationDensity)
            {
                words.Add(ViolationTerms[_random.Next(ViolationTerms.Length)]);
            }
            else
            {
                words.Add(SampleWords[_random.Next(SampleWords.Length)]);
            }
        }

        // LOGIC: Capitalize first word
        if (words.Count > 0)
        {
            words[0] = char.ToUpper(words[0][0]) + words[0][1..];
        }

        return string.Join(" ", words) + ".";
    }

    /// <summary>
    /// Generates a document of the specified size in megabytes.
    /// </summary>
    /// <param name="sizeInMegabytes">Target size in megabytes.</param>
    /// <returns>Generated document content.</returns>
    public string GenerateDocument(int sizeInMegabytes)
    {
        return GenerateDocument(new DocumentGenerationOptions(
            TargetSizeBytes: sizeInMegabytes * 1024 * 1024
        ));
    }
}
