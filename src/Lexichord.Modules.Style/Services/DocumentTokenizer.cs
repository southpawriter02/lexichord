// <copyright file="DocumentTokenizer.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Implementation of <see cref="IDocumentTokenizer"/> for extracting words from document text.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1c - Tokenizes text using word boundaries, preserving hyphenated words.
/// Thread-safe: Uses compiled regex and produces immutable results.
/// </remarks>
public sealed partial class DocumentTokenizer : IDocumentTokenizer
{
    /// <summary>
    /// Regex pattern for matching words including hyphenated compounds.
    /// </summary>
    /// <remarks>
    /// LOGIC: \b[\w-]+\b matches word boundaries with word characters and hyphens.
    /// Examples: "self-aware", "state-of-the-art", "high-performance"
    /// </remarks>
    [GeneratedRegex(@"\b[\w-]+\b", RegexOptions.Compiled)]
    private static partial Regex WordPattern();

    /// <inheritdoc />
    public IReadOnlySet<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>();
        }

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = WordPattern().Matches(text);

        foreach (Match match in matches)
        {
            // LOGIC: Normalize to lowercase for case-insensitive matching
            var token = match.Value.ToLowerInvariant();
            tokens.Add(token);
        }

        return tokens;
    }

    /// <inheritdoc />
    public IReadOnlyList<DocumentToken> TokenizeWithPositions(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<DocumentToken>();
        }

        var matches = WordPattern().Matches(text);
        var tokens = new List<DocumentToken>(matches.Count);

        foreach (Match match in matches)
        {
            // LOGIC: Preserve position information for violation reporting
            tokens.Add(new DocumentToken(
                Token: match.Value.ToLowerInvariant(),
                StartOffset: match.Index,
                EndOffset: match.Index + match.Length));
        }

        return tokens;
    }
}
