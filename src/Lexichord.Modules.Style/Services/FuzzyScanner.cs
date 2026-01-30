// <copyright file="FuzzyScanner.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Implementation of <see cref="IFuzzyScanner"/> for detecting approximate terminology matches.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1c - Scans document text for fuzzy matches against terminology database.
/// - Requires Writer Pro license tier for activation
/// - Uses IFuzzyMatchService for Levenshtein-based similarity scoring
/// - Skips words already flagged by regex scanning to prevent double-counting
/// Thread-safe: Uses immutable inputs and produces immutable results.
/// </remarks>
public sealed class FuzzyScanner : IFuzzyScanner
{
    private readonly ILicenseContext _licenseContext;
    private readonly ITerminologyRepository _terminologyRepository;
    private readonly IDocumentTokenizer _tokenizer;
    private readonly IFuzzyMatchService _fuzzyMatchService;
    private readonly ILogger<FuzzyScanner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzyScanner"/> class.
    /// </summary>
    /// <param name="licenseContext">License context for tier checking.</param>
    /// <param name="terminologyRepository">Repository for terminology data.</param>
    /// <param name="tokenizer">Document tokenizer service.</param>
    /// <param name="fuzzyMatchService">Fuzzy matching service.</param>
    /// <param name="logger">Logger instance.</param>
    public FuzzyScanner(
        ILicenseContext licenseContext,
        ITerminologyRepository terminologyRepository,
        IDocumentTokenizer tokenizer,
        IFuzzyMatchService fuzzyMatchService,
        ILogger<FuzzyScanner> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _terminologyRepository = terminologyRepository ?? throw new ArgumentNullException(nameof(terminologyRepository));
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _fuzzyMatchService = fuzzyMatchService ?? throw new ArgumentNullException(nameof(fuzzyMatchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StyleViolation>> ScanAsync(
        string content,
        IReadOnlySet<string> regexFlaggedWords,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: License gate - fuzzy scanning requires Writer Pro tier
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogDebug("Fuzzy scanning skipped: requires Writer Pro license tier");
            return Array.Empty<StyleViolation>();
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<StyleViolation>();
        }

        // LOGIC: Get fuzzy-enabled terms from repository
        var fuzzyTerms = await _terminologyRepository.GetFuzzyEnabledTermsAsync(cancellationToken);
        if (fuzzyTerms.Count == 0)
        {
            _logger.LogDebug("Fuzzy scanning skipped: no fuzzy-enabled terms configured");
            return Array.Empty<StyleViolation>();
        }

        // LOGIC: Tokenize document with positions for accurate violation reporting
        var tokens = _tokenizer.TokenizeWithPositions(content);
        if (tokens.Count == 0)
        {
            return Array.Empty<StyleViolation>();
        }

        var violations = new List<StyleViolation>();
        var processedPairs = new HashSet<(int Position, Guid TermId)>();

        _logger.LogDebug(
            "Fuzzy scanning {TokenCount} tokens against {TermCount} fuzzy-enabled terms",
            tokens.Count,
            fuzzyTerms.Count);

        foreach (var token in tokens)
        {
            // LOGIC: Skip words already flagged by regex to prevent double-counting
            if (regexFlaggedWords.Contains(token.Token))
            {
                continue;
            }

            foreach (var term in fuzzyTerms)
            {
                // LOGIC: Avoid duplicate violations for same position + term
                var pairKey = (token.StartOffset, term.Id);
                if (processedPairs.Contains(pairKey))
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // LOGIC: Use term-specific threshold, convert from 0.0-1.0 to 0-100
                var thresholdInt = (int)(term.FuzzyThreshold * 100);
                var isMatch = _fuzzyMatchService.IsMatch(
                    token.Token,
                    term.Term.ToLowerInvariant(),
                    thresholdInt);

                if (isMatch)
                {
                    var ratio = _fuzzyMatchService.CalculateRatio(
                        token.Token,
                        term.Term.ToLowerInvariant());

                    // LOGIC: Calculate line/column from offset
                    var (startLine, startColumn) = CalculateLineColumn(content, token.StartOffset);
                    var (endLine, endColumn) = CalculateLineColumn(content, token.EndOffset);

                    // LOGIC: Create a synthetic rule for the fuzzy match
                    var severity = ParseSeverity(term.Severity);
                    var rule = CreateFuzzyRule(term);

                    var violation = new StyleViolation(
                        Rule: rule,
                        Message: CreateFuzzyMessage(token.Token, term),
                        StartOffset: token.StartOffset,
                        EndOffset: token.EndOffset,
                        StartLine: startLine,
                        StartColumn: startColumn,
                        EndLine: endLine,
                        EndColumn: endColumn,
                        MatchedText: content[token.StartOffset..token.EndOffset],
                        Suggestion: term.Replacement,
                        Severity: severity,
                        IsFuzzyMatch: true,
                        FuzzyRatio: ratio);

                    violations.Add(violation);
                    processedPairs.Add(pairKey);

                    _logger.LogTrace(
                        "Fuzzy match: '{Token}' matches '{Term}' with ratio {Ratio}%",
                        token.Token,
                        term.Term,
                        ratio);
                }
            }
        }

        _logger.LogDebug("Fuzzy scanning complete: {ViolationCount} violations found", violations.Count);
        return violations;
    }

    private static (int Line, int Column) CalculateLineColumn(string content, int offset)
    {
        if (offset <= 0)
        {
            return (1, 1);
        }

        var line = 1;
        var lastNewLineIndex = -1;

        for (var i = 0; i < offset && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                lastNewLineIndex = i;
            }
        }

        var column = offset - lastNewLineIndex;
        return (line, column);
    }

    private static ViolationSeverity ParseSeverity(string severity)
    {
        return severity.ToUpperInvariant() switch
        {
            "ERROR" => ViolationSeverity.Error,
            "WARNING" => ViolationSeverity.Warning,
            "SUGGESTION" => ViolationSeverity.Hint,
            "INFO" => ViolationSeverity.Info,
            _ => ViolationSeverity.Hint
        };
    }

    private static StyleRule CreateFuzzyRule(StyleTerm term)
    {
        // LOGIC: Create a synthetic StyleRule for the fuzzy match
        return new StyleRule(
            Id: $"fuzzy-{term.Id:N}",
            Name: $"Fuzzy: {term.Term}",
            Description: term.Notes ?? $"Approximate match for '{term.Term}'",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ParseSeverity(term.Severity),
            Pattern: term.Term,
            PatternType: PatternType.Literal,
            Suggestion: term.Replacement,
            IsEnabled: true);
    }

    private static string CreateFuzzyMessage(string matchedToken, StyleTerm term)
    {
        var message = $"'{matchedToken}' is similar to '{term.Term}'";
        if (!string.IsNullOrEmpty(term.Replacement))
        {
            message += $". Consider: '{term.Replacement}'";
        }

        return message;
    }
}
