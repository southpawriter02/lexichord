// <copyright file="SentenceInfo.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Information about a single sentence within a text.
/// </summary>
/// <param name="Text">The sentence text including terminal punctuation.</param>
/// <param name="StartIndex">Character index where the sentence begins in the original text.</param>
/// <param name="EndIndex">Character index where the sentence ends in the original text.</param>
/// <param name="WordCount">Number of words in the sentence.</param>
/// <remarks>
/// LOGIC: v0.3.3a - Data contract for sentence tokenization results.
/// Used by IReadabilityService at v0.3.3c to calculate readability metrics.
/// Positions are zero-indexed and EndIndex is exclusive (follows .NET conventions).
/// </remarks>
public readonly record struct SentenceInfo(
    string Text,
    int StartIndex,
    int EndIndex,
    int WordCount);
