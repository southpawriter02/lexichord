// <copyright file="WeakWordLists.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Static word collections for weak word detection.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4c - These curated lists are sourced from professional
/// editing guides and writing style manuals.</para>
/// <para>Word lists are stored as frozen HashSets for optimal O(1) lookup
/// performance and thread-safety.</para>
/// </remarks>
public static class WeakWordLists
{
    /// <summary>
    /// Common adverbs and intensifiers that often weaken writing.
    /// </summary>
    /// <remarks>
    /// These words typically add emphasis without substance and can often
    /// be replaced with stronger verbs or more precise adjectives.
    /// </remarks>
    public static readonly HashSet<string> Adverbs = new(StringComparer.OrdinalIgnoreCase)
    {
        // Intensifiers
        "very",
        "really",
        "extremely",
        "absolutely",
        "completely",
        "totally",
        "utterly",
        "highly",
        "incredibly",
        "tremendously",
        "particularly",
        "especially",
        "exceptionally",
        "remarkably",
        "extraordinarily",

        // Manner adverbs that can be replaced
        "quickly",
        "slowly",
        "carefully",
        "easily",
        "simply",
        "actually",
        "definitely",
        "certainly",
        "obviously",
        "clearly",
        "surely",
        "truly",
        "frankly",
        "honestly",
        "personally",

        // Frequency/degree weakeners
        "quite",
        "rather",
        "fairly",
        "slightly",
        "somewhat",
        "barely",
        "hardly",
        "nearly",
        "almost",
        "just"
    };

    /// <summary>
    /// Weasel words and hedges that reduce precision and commitment.
    /// </summary>
    /// <remarks>
    /// These words hedge statements, making writing less authoritative
    /// and more ambiguous. They're particularly problematic in technical
    /// and marketing content.
    /// </remarks>
    public static readonly HashSet<string> WeaselWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Uncertainty hedges
        "perhaps",
        "maybe",
        "possibly",
        "probably",
        "presumably",
        "apparently",
        "seemingly",
        "supposedly",

        // Vague qualifiers
        "some",
        "many",
        "most",
        "several",
        "few",
        "various",
        "numerous",
        "countless",

        // Commitment avoiders
        "might",
        "could",
        "would",
        "should",
        "may",
        "can",
        "tend",
        "seems",
        "appears",

        // Vague references
        "often",
        "sometimes",
        "rarely",
        "usually",
        "generally",
        "typically",
        "normally",
        "frequently"
    };

    /// <summary>
    /// Filler words that add no meaning to sentences.
    /// </summary>
    /// <remarks>
    /// These words are ALWAYS flagged regardless of profile settings
    /// because they typically add no value to the writing and can
    /// safely be removed.
    /// </remarks>
    public static readonly HashSet<string> Fillers = new(StringComparer.OrdinalIgnoreCase)
    {
        "basically",
        "essentially",
        "literally",
        "actually",
        "practically",
        "virtually",
        "fundamentally",
        "technically",
        "effectively",
        "ultimately",
        "anyway",
        "anyways",
        "whatever",
        "like",
        "stuff"
    };

    /// <summary>
    /// Suggested replacements or actions for specific weak words.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Keys are lowercase for case-insensitive lookup.</para>
    /// <para>Suggestions may be:</para>
    /// <list type="bullet">
    ///   <item>Specific replacement advice</item>
    ///   <item>Action recommendations (e.g., "Remove" or "Be specific")</item>
    ///   <item>Alternative phrasing suggestions</item>
    /// </list>
    /// </remarks>
    public static readonly Dictionary<string, string> Suggestions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Intensifiers - suggest removal or stronger words
        ["very"] = "Use a stronger adjective (e.g., 'very big' → 'enormous')",
        ["really"] = "Consider removing or use 'truly' if emphasis is needed",
        ["extremely"] = "Use a stronger adjective that inherently conveys intensity",
        ["absolutely"] = "Remove if not adding meaning, or use 'completely'",
        ["completely"] = "Consider if the context already implies completeness",
        ["totally"] = "Replace with 'entirely' or remove",
        ["highly"] = "Be specific about the degree or use a stronger adjective",
        ["incredibly"] = "Use a more precise descriptor",

        // Manner adverbs - suggest rewording
        ["quickly"] = "Use a more vivid verb (e.g., 'ran quickly' → 'sprinted')",
        ["slowly"] = "Use a more vivid verb (e.g., 'walked slowly' → 'ambled')",
        ["carefully"] = "Show the careful action instead of telling",
        ["easily"] = "Describe what makes it easy specifically",
        ["simply"] = "Remove or explain the simplicity",
        ["actually"] = "Remove—it often adds nothing",
        ["definitely"] = "Remove or provide evidence for certainty",
        ["certainly"] = "Remove or provide supporting evidence",
        ["obviously"] = "Remove—if obvious, no need to say so",
        ["clearly"] = "Remove—show clarity through the content itself",

        // Hedges - suggest commitment
        ["perhaps"] = "Commit to the statement or explain the uncertainty",
        ["maybe"] = "Be definitive or explain the conditions",
        ["possibly"] = "State the probability or conditions",
        ["probably"] = "State the likelihood with evidence",
        ["apparently"] = "Verify and state definitively, or cite the source",
        ["seemingly"] = "Investigate and state the fact",
        ["supposedly"] = "Verify the claim and cite the source",

        // Vague qualifiers - suggest specifics
        ["some"] = "Specify the quantity or provide a range",
        ["many"] = "Provide a number or percentage",
        ["most"] = "Provide a percentage or specific data",
        ["several"] = "Specify the exact number if known",
        ["few"] = "Specify the number",
        ["various"] = "List the specific items or categories",
        ["numerous"] = "Provide the count or estimate",

        // Fillers - always remove
        ["basically"] = "Remove—adds no meaning",
        ["essentially"] = "Remove or be more precise",
        ["literally"] = "Remove unless describing something literal",
        ["practically"] = "Remove or use 'nearly' if needed",
        ["virtually"] = "Remove or use 'almost' if needed",
        ["fundamentally"] = "Remove or explain the fundamental aspect",
        ["technically"] = "Remove or explain the technical nuance",
        ["effectively"] = "Remove or describe the effect specifically",
        ["ultimately"] = "Remove or use 'finally' if sequence matters",
        ["anyway"] = "Remove—distracts from the point",
        ["whatever"] = "Be specific about what you're referring to",
        ["like"] = "Remove when used as filler",
        ["stuff"] = "Be specific about what 'stuff' refers to"
    };
}
