using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Implementation of ITerminologySeeder with embedded Microsoft Manual of Style terms.
/// </summary>
/// <remarks>
/// LOGIC: This seeder provides ~50 default terms covering common style issues:
/// - Terminology: Technical jargon and preferred phrasing
/// - Capitalization: Proper casing for technical terms
/// - Punctuation: Comma usage, serial commas, etc.
/// - Voice: Active vs passive, direct language
/// - Clarity: Removing unnecessary words
/// - Grammar: Common grammatical issues
///
/// Design decisions:
/// - Terms are compiled into the assembly (not loaded from files)
/// - Static initialization ensures terms are created once
/// - Seeding operations are idempotent by default
/// </remarks>
public sealed class TerminologySeeder : ITerminologySeeder
{
    private readonly ITerminologyRepository _repository;
    private readonly ILogger<TerminologySeeder> _logger;

    /// <summary>
    /// The default style sheet ID for seed terms.
    /// </summary>
    /// <remarks>
    /// LOGIC: All seed terms belong to the "system" style sheet.
    /// This well-known ID allows distinguishing seed terms from user-created ones.
    /// </remarks>
    private static readonly Guid DefaultStyleSheetId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Static readonly list of default terms.
    /// </summary>
    private static readonly IReadOnlyList<StyleTerm> _defaultTerms = CreateDefaultTerms();

    /// <summary>
    /// Creates a new TerminologySeeder instance.
    /// </summary>
    /// <param name="repository">The terminology repository.</param>
    /// <param name="logger">Logger instance.</param>
    public TerminologySeeder(
        ITerminologyRepository repository,
        ILogger<TerminologySeeder> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SeedResult> SeedIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var existingCount = await _repository.CountAsync(cancellationToken);

        if (existingCount > 0)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Database not empty ({ExistingCount} terms exist), skipping seed",
                existingCount);
            return SeedResult.Skipped(stopwatch.Elapsed);
        }

        _logger.LogInformation("Database empty, seeding with {Count} default terms...", _defaultTerms.Count);

        var inserted = await _repository.InsertManyAsync(_defaultTerms, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Seeded {Count} terms in {Duration}ms",
            inserted,
            stopwatch.ElapsedMilliseconds);

        return new SeedResult(inserted, WasEmpty: true, stopwatch.Elapsed);
    }

    /// <inheritdoc />
    public async Task<SeedResult> ReseedAsync(bool clearExisting = false, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var wasEmpty = true;

        if (clearExisting)
        {
            _logger.LogWarning(
                "RESEED: Clearing all existing terms before seeding. " +
                "This will DELETE all user-customized terms!");

            // Get all terms and delete them
            var allTerms = await _repository.GetAllAsync(cancellationToken);
            var count = 0;
            foreach (var term in allTerms)
            {
                await _repository.DeleteAsync(term.Id, cancellationToken);
                count++;
            }

            _logger.LogWarning("RESEED: Deleted {Count} existing terms", count);
            wasEmpty = count == 0;
        }
        else
        {
            var existingCount = await _repository.CountAsync(cancellationToken);
            wasEmpty = existingCount == 0;
        }

        _logger.LogInformation("Seeding {Count} default terms...", _defaultTerms.Count);

        var inserted = await _repository.InsertManyAsync(_defaultTerms, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Reseeded {Count} terms in {Duration}ms (clearExisting: {ClearExisting})",
            inserted,
            stopwatch.ElapsedMilliseconds,
            clearExisting);

        return new SeedResult(inserted, wasEmpty, stopwatch.Elapsed);
    }

    /// <inheritdoc />
    public IReadOnlyList<StyleTerm> GetDefaultTerms() => _defaultTerms;

    /// <summary>
    /// Creates the default Microsoft Manual of Style terms.
    /// </summary>
    /// <remarks>
    /// LOGIC: Terms are organized by category:
    /// - Terminology: ~15 terms for jargon and technical phrases
    /// - Capitalization: ~8 terms for casing rules
    /// - Punctuation: ~6 terms for punctuation usage
    /// - Voice: ~8 terms for active/direct language
    /// - Clarity: ~8 terms for concise writing
    /// - Grammar: ~5 terms for common grammatical issues
    /// </remarks>
    private static IReadOnlyList<StyleTerm> CreateDefaultTerms()
    {
        var terms = new List<StyleTerm>();

        // ============================================================
        // TERMINOLOGY (~15 terms)
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "leverage",
            Replacement = "use",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Avoid business jargon"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "utilize",
            Replacement = "use",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Prefer simple words"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "functionality",
            Replacement = "feature",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Use concrete terms"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "please",
            Replacement = null,
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Avoid excessive politeness in technical docs"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "basically",
            Replacement = null,
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Filler word that adds no meaning"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "synergy",
            Replacement = "cooperation",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Avoid business jargon"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "paradigm",
            Replacement = "model",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Use simpler alternatives"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "going forward",
            Replacement = "from now on",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Use clearer time references"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "reach out",
            Replacement = "contact",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Prefer direct verbs"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "circle back",
            Replacement = "follow up",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Avoid business jargon"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "drill down",
            Replacement = "examine",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Use standard verbs"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "actionable",
            Replacement = "practical",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Prefer concrete adjectives"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "impactful",
            Replacement = "effective",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Not a standard word"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "learnings",
            Replacement = "lessons",
            Category = "Terminology",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Use standard English"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "deliverable",
            Replacement = "result",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Avoid business jargon"
        });

        // ============================================================
        // CAPITALIZATION (~8 terms)
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "internet",
            Replacement = "Internet",
            Category = "Capitalization",
            Severity = "Error",
            Notes = "Microsoft Manual of Style: Internet is a proper noun"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "web",
            Replacement = "Web",
            Category = "Capitalization",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Capitalize when referring to World Wide Web"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "e-mail",
            Replacement = "email",
            Category = "Capitalization",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: No hyphen in email"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "on-line",
            Replacement = "online",
            Category = "Capitalization",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: No hyphen in online"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "web site",
            Replacement = "website",
            Category = "Capitalization",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: One word, lowercase"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "log-in",
            Replacement = "sign in",
            Category = "Capitalization",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Use 'sign in' as verb"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "log in",
            Replacement = "sign in",
            Category = "Capitalization",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Prefer 'sign in' over 'log in'"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "dropdown",
            Replacement = "drop-down",
            Category = "Capitalization",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Hyphenate when used as adjective"
        });

        // ============================================================
        // PUNCTUATION (~6 terms)
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "etc.",
            Replacement = null,
            Category = "Punctuation",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Avoid etc. - be specific or use 'such as'"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "e.g.",
            Replacement = "for example",
            Category = "Punctuation",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Spell out for clarity"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "i.e.",
            Replacement = "that is",
            Category = "Punctuation",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Spell out for clarity"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "and/or",
            Replacement = null,
            Category = "Punctuation",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Rewrite to avoid ambiguity"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "s/he",
            Replacement = "they",
            Category = "Punctuation",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Use inclusive pronouns"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "(s)",
            Replacement = null,
            Category = "Punctuation",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Choose singular or plural, avoid (s)"
        });

        // ============================================================
        // VOICE (~8 terms)
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "it is recommended that",
            Replacement = "we recommend",
            Category = "Voice",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Use active voice"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "it should be noted that",
            Replacement = "note that",
            Category = "Voice",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Use direct language"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "in order to",
            Replacement = "to",
            Category = "Voice",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Omit unnecessary words"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "due to the fact that",
            Replacement = "because",
            Category = "Voice",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Use simple conjunctions"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "at this point in time",
            Replacement = "now",
            Category = "Voice",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Be concise"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "for the purpose of",
            Replacement = "to",
            Category = "Voice",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Simplify prepositional phrases"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "it is important to note that",
            Replacement = "importantly",
            Category = "Voice",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Be direct"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "there are many ways to",
            Replacement = "you can",
            Category = "Voice",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Address reader directly"
        });

        // ============================================================
        // CLARITY (~8 terms)
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "very",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Often unnecessary, use stronger adjective"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "really",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Often unnecessary intensifier"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "actually",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Usually adds no meaning"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "just",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Often unnecessary filler"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "simply",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Avoid minimizing complexity"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "obviously",
            Replacement = null,
            Category = "Clarity",
            Severity = "Warning",
            Notes = "Microsoft Manual of Style: Avoid assumptions about reader knowledge"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "clearly",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Let facts speak for themselves"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "easy",
            Replacement = null,
            Category = "Clarity",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Avoid subjective judgments"
        });

        // ============================================================
        // GRAMMAR (~5 terms)
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "less",
            Replacement = "fewer",
            Category = "Grammar",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Use 'fewer' for countable items"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "which",
            Replacement = "that",
            Category = "Grammar",
            Severity = "Suggestion",
            Notes = "Microsoft Manual of Style: Use 'that' for restrictive clauses"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "alot",
            Replacement = "a lot",
            Category = "Grammar",
            Severity = "Error",
            Notes = "Microsoft Manual of Style: 'A lot' is two words"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "could of",
            Replacement = "could have",
            Category = "Grammar",
            Severity = "Error",
            Notes = "Microsoft Manual of Style: Common grammar error"
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "should of",
            Replacement = "should have",
            Category = "Grammar",
            Severity = "Error",
            Notes = "Microsoft Manual of Style: Common grammar error"
        });

        // ============================================================
        // FUZZY MATCHING TERMS (~5 terms)
        // These terms have FuzzyEnabled=true for approximate matching
        // ============================================================
        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "whitelist",
            Replacement = "allowlist",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Inclusive language: Use allowlist instead of whitelist",
            FuzzyEnabled = true,
            FuzzyThreshold = 0.85
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "blacklist",
            Replacement = "denylist",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Inclusive language: Use denylist instead of blacklist",
            FuzzyEnabled = true,
            FuzzyThreshold = 0.85
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "master",
            Replacement = "main",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Inclusive language: Use main instead of master (for branches)",
            FuzzyEnabled = true,
            FuzzyThreshold = 0.90
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "slave",
            Replacement = "replica",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Inclusive language: Use replica instead of slave (for databases)",
            FuzzyEnabled = true,
            FuzzyThreshold = 0.90
        });

        terms.Add(new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = DefaultStyleSheetId,
            Term = "sanity check",
            Replacement = "confidence check",
            Category = "Terminology",
            Severity = "Suggestion",
            Notes = "Inclusive language: Use confidence check instead of sanity check",
            FuzzyEnabled = true,
            FuzzyThreshold = 0.80
        });

        return terms.AsReadOnly();
    }
}
