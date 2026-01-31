// <copyright file="BuiltInProfiles.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Static definitions for built-in Voice Profiles.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4a - Built-in profiles are defined in code and cannot be modified or deleted.
/// They are available to all Writer Pro users without additional licensing.
/// </remarks>
internal static class BuiltInProfiles
{
    /// <summary>
    /// Technical writing profile for documentation and technical content.
    /// </summary>
    public static readonly VoiceProfile Technical = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000001"),
        Name = "Technical",
        Description = "Direct, precise writing for documentation and technical content. " +
                      "Passive voice is forbidden. Adverbs and hedging are flagged.",
        TargetGradeLevel = 11.0,
        GradeLevelTolerance = 2.0,
        MaxSentenceLength = 20,
        AllowPassiveVoice = false,
        MaxPassiveVoicePercentage = 0,
        FlagAdverbs = true,
        FlagWeaselWords = true,
        ForbiddenCategories = [],
        IsBuiltIn = true,
        SortOrder = 1
    };

    /// <summary>
    /// Marketing profile for persuasive sales and marketing content.
    /// </summary>
    public static readonly VoiceProfile Marketing = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000002"),
        Name = "Marketing",
        Description = "Engaging, persuasive copy for marketing and sales content. " +
                      "Moderate passive voice allowed. Adverbs are acceptable.",
        TargetGradeLevel = 9.0,
        GradeLevelTolerance = 2.0,
        MaxSentenceLength = 25,
        AllowPassiveVoice = true,
        MaxPassiveVoicePercentage = 20.0,
        FlagAdverbs = false,
        FlagWeaselWords = true,
        ForbiddenCategories = [],
        IsBuiltIn = true,
        SortOrder = 2
    };

    /// <summary>
    /// Academic profile for research papers and scholarly writing.
    /// </summary>
    public static readonly VoiceProfile Academic = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000003"),
        Name = "Academic",
        Description = "Formal, scholarly writing for research papers and academic content. " +
                      "Passive voice is common in academic writing. No style flags.",
        TargetGradeLevel = 13.0,
        GradeLevelTolerance = 2.0,
        MaxSentenceLength = 30,
        AllowPassiveVoice = true,
        MaxPassiveVoicePercentage = 30.0,
        FlagAdverbs = false,
        FlagWeaselWords = false,
        ForbiddenCategories = [],
        IsBuiltIn = true,
        SortOrder = 3
    };

    /// <summary>
    /// Narrative profile for fiction and creative writing.
    /// </summary>
    public static readonly VoiceProfile Narrative = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000004"),
        Name = "Narrative",
        Description = "Creative, flowing prose for fiction and storytelling. " +
                      "Longer sentences and passive voice are stylistic choices.",
        TargetGradeLevel = 9.0,
        GradeLevelTolerance = 3.0,
        MaxSentenceLength = 35,
        AllowPassiveVoice = true,
        MaxPassiveVoicePercentage = 15.0,
        FlagAdverbs = false,
        FlagWeaselWords = false,
        ForbiddenCategories = [],
        IsBuiltIn = true,
        SortOrder = 4
    };

    /// <summary>
    /// Casual profile for blogs and informal content.
    /// </summary>
    public static readonly VoiceProfile Casual = new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000005"),
        Name = "Casual",
        Description = "Conversational, friendly writing for blogs and informal content. " +
                      "Easy reading level with relaxed style rules.",
        TargetGradeLevel = 7.0,
        GradeLevelTolerance = 2.0,
        MaxSentenceLength = 20,
        AllowPassiveVoice = true,
        MaxPassiveVoicePercentage = 25.0,
        FlagAdverbs = false,
        FlagWeaselWords = false,
        ForbiddenCategories = [],
        IsBuiltIn = true,
        SortOrder = 5
    };

    /// <summary>
    /// All built-in profiles in sort order.
    /// </summary>
    public static readonly IReadOnlyList<VoiceProfile> All =
    [
        Technical, Marketing, Academic, Narrative, Casual
    ];

    /// <summary>
    /// The default profile used when no profile is explicitly selected.
    /// </summary>
    public static readonly VoiceProfile Default = Technical;
}
