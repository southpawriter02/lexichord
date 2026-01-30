// <copyright file="IFuzzyScanner.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for scanning documents for fuzzy terminology matches.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1c - The fuzzy scanner detects approximate matches for forbidden terms.
/// Uses Levenshtein distance-based similarity scoring to catch typos and variations.
/// Requires Writer Pro license tier for activation.
/// </remarks>
public interface IFuzzyScanner
{
    /// <summary>
    /// Scans the document content for fuzzy matches against the terminology database.
    /// </summary>
    /// <param name="content">The document content to scan.</param>
    /// <param name="regexFlaggedWords">
    /// Words already flagged by regex scanning, used to prevent double-counting.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of style violations for fuzzy matches found.</returns>
    /// <remarks>
    /// LOGIC: Integration point for LintingOrchestrator.ExecuteScanCoreAsync.
    /// - Returns empty if license is not Writer Pro tier
    /// - Excludes words already in regexFlaggedWords
    /// - Only returns matches above the configured threshold
    /// </remarks>
    Task<IReadOnlyList<StyleViolation>> ScanAsync(
        string content,
        IReadOnlySet<string> regexFlaggedWords,
        CancellationToken cancellationToken = default);
}
