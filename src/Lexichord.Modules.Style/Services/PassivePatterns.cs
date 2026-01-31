// <copyright file="PassivePatterns.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Static class containing compiled regex patterns and disambiguation data
/// for passive voice detection.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4b - All patterns are compiled for performance and
/// use case-insensitive matching.</para>
/// <para>Patterns are evaluated in priority order: Progressive → Perfect → Modal → ToBe → Get
/// to ensure the most specific match is found first.</para>
/// </remarks>
internal static class PassivePatterns
{
    /// <summary>
    /// Pattern for progressive passive: "is/are/was/were being + past participle".
    /// </summary>
    /// <example>"The project is being reviewed."</example>
    /// <remarks>
    /// Checked first as it's a more specific form of passive voice.
    /// </remarks>
    public static readonly Regex ProgressivePassive = new(
        @"\b(is|are|was|were)\s+being\s+(\w+(?:ed|en|t|d))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern for modal passive: "modal + be + past participle".
    /// </summary>
    /// <example>"The file will be deleted tomorrow."</example>
    public static readonly Regex ModalPassive = new(
        @"\b(will|would|can|could|shall|should|may|might|must)\s+be\s+(\w+(?:ed|en|t|d))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern for perfect passive: "has/had been + past participle".
    /// </summary>
    /// <example>"The book has been read by millions."</example>
    /// <remarks>
    /// Handles present and past perfect passive forms.
    /// </remarks>
    public static readonly Regex PerfectPassive = new(
        @"\b(has|had|have)\s+been\s+(\w+(?:ed|en|t|d))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern for standard "to be" passive: "to be verb + past participle".
    /// </summary>
    /// <example>"The code was written by the developer."</example>
    public static readonly Regex ToBePassive = new(
        @"\b(am|is|are|was|were)\s+(\w+(?:ed|en|t|d))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern for "get" passive: "get/gets/got/gotten/getting + past participle".
    /// </summary>
    /// <example>"She got fired last week."</example>
    public static readonly Regex GetPassive = new(
        @"\b(get|gets|got|gotten|getting)\s+(\w+(?:ed|en|t|d))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Pattern to detect "by [agent]" phrase which strongly indicates passive voice.
    /// </summary>
    /// <example>"was written by the developer"</example>
    public static readonly Regex ByAgentPhrase = new(
        @"\bby\s+(?:the\s+)?(?:a\s+)?[\w]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50));

    /// <summary>
    /// Common state adjectives that appear like passive voice but are not.
    /// These words often follow "to be" verbs but describe states rather than actions.
    /// </summary>
    /// <remarks>
    /// When the participle is in this set, confidence is reduced by 0.25.
    /// </remarks>
    public static readonly HashSet<string> CommonStateAdjectives = new(StringComparer.OrdinalIgnoreCase)
    {
        // Physical states
        "closed", "opened", "broken", "bent", "cracked", "damaged",
        "locked", "unlocked", "sealed", "blocked", "cleared",
        
        // Emotional/mental states
        "tired", "exhausted", "worried", "concerned", "excited",
        "bored", "interested", "pleased", "disappointed", "satisfied",
        "confused", "convinced", "surprised", "shocked", "amazed",
        "annoyed", "frustrated", "relaxed", "stressed", "depressed",
        
        // Physical appearance
        "dressed", "undressed", "covered", "uncovered", "hidden",
        "exposed", "painted", "decorated", "furnished", "equipped",
        
        // Positional states
        "seated", "positioned", "located", "situated", "parked",
        "stored", "placed", "arranged", "organized", "scattered",
        
        // Relationship states
        "married", "engaged", "divorced", "separated", "related",
        "connected", "attached", "detached", "linked", "joined"
    };

    /// <summary>
    /// Verbs that suggest the following word is a state adjective, not passive voice.
    /// </summary>
    /// <example>"The door seems closed." (adjective, not passive)</example>
    /// <remarks>
    /// When one of these verbs appears, confidence is reduced by 0.2.
    /// </remarks>
    public static readonly HashSet<string> StateContextVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "seems", "seem", "seemed",
        "appears", "appear", "appeared",
        "looks", "look", "looked",
        "remains", "remain", "remained",
        "stays", "stay", "stayed",
        "feels", "feel", "felt",
        "sounds", "sound", "sounded",
        "becomes", "become", "became"
    };

    /// <summary>
    /// Pattern to detect state context verbs in a sentence.
    /// </summary>
    public static readonly Regex StateContextVerbPattern = new(
        @"\b(seems?|seemed|appears?|appeared|looks?|looked|remains?|remained|stays?|stayed|feels?|felt|sounds?|sounded|becomes?|became)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50));
}
