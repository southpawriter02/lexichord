// <copyright file="LabeledSentenceCorpus.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Tests.Unit.Modules.Style.TestData;

/// <summary>
/// Provides categorized sentence corpuses for v0.3.8c accuracy testing.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.8c - Each corpus contains labeled sentences for testing
/// detection accuracy and false positive rates.</para>
/// <para>Categories:</para>
/// <list type="bullet">
///   <item><see cref="ClearPassive"/>: Unambiguous passive voice sentences</item>
///   <item><see cref="ClearActive"/>: Active voice sentences (should not be flagged)</item>
///   <item><see cref="LinkingVerb"/>: Linking verb sentences (false positive cases)</item>
///   <item><see cref="AdjectiveComplement"/>: Predicate adjective sentences</item>
///   <item><see cref="ProgressiveActive"/>: Active progressive sentences</item>
///   <item><see cref="GetPassive"/>: Get-passive constructions</item>
///   <item><see cref="ModalPassive"/>: Modal + be + participle patterns</item>
///   <item><see cref="EdgeCase"/>: Edge cases for robustness testing</item>
/// </list>
/// </remarks>
public static class LabeledSentenceCorpus
{
    #region Clear Passive Voice Sentences

    /// <summary>
    /// Unambiguous passive voice sentences for accuracy testing.
    /// </summary>
    /// <remarks>
    /// Target: 95% detection accuracy on this corpus.
    /// All sentences follow the pattern: [subject] + [form of "be"] + [past participle].
    /// </remarks>
    public static readonly string[] ClearPassive =
    [
        // Simple "was/were + participle" passive
        "The code was written by the user.",
        "The report was submitted yesterday.",
        "The tests were executed successfully.",
        "The document was reviewed by the team.",
        "The bug was fixed in the latest release.",
        "The feature was implemented last week.",
        "The application was deployed to production.",
        "The database was migrated to the new server.",
        "The configuration was updated by the admin.",
        "The files were deleted during cleanup.",

        // "has/have been + participle" perfect passive
        "The book has been read by millions.",
        "The project has been completed on time.",
        "The changes have been merged into main.",
        "The data has been backed up successfully.",
        "The system has been tested thoroughly.",

        // "is/are + participle" present passive
        "The cake is eaten at every party.",
        "The rules are enforced by the moderators.",
        "The emails are sent automatically.",
        "The logs are reviewed daily.",
        "The metrics are tracked in real-time.",
    ];

    #endregion

    #region Clear Active Voice Sentences

    /// <summary>
    /// Active voice sentences that should NOT be flagged as passive.
    /// </summary>
    /// <remarks>
    /// Target: 0% false positive rate on this corpus.
    /// </remarks>
    public static readonly string[] ClearActive =
    [
        "The user wrote the code.",
        "She submitted the report yesterday.",
        "The team executed the tests successfully.",
        "The developer reviewed the document.",
        "We fixed the bug in the latest release.",
        "The team implemented the feature last week.",
        "DevOps deployed the application to production.",
        "The admin migrated the database to the new server.",
        "They updated the configuration.",
        "The cleanup process deleted the files.",
        "Millions have read the book.",
        "The team completed the project on time.",
        "We merged the changes into main.",
        "The system backed up the data successfully.",
        "QA tested the system thoroughly.",
        "Everyone eats cake at every party.",
        "The moderators enforce the rules.",
        "The system sends emails automatically.",
        "The team reviews logs daily.",
        "We track metrics in real-time.",
    ];

    #endregion

    #region Linking Verb Sentences (False Positive Cases)

    /// <summary>
    /// Sentences with linking verbs that should NOT be flagged as passive.
    /// </summary>
    /// <remarks>
    /// These often look like passive constructions but are actually predicate adjectives.
    /// Verbs: is, are, was, were + adjective (not participle).
    /// </remarks>
    public static readonly string[] LinkingVerb =
    [
        "The door is closed.",
        "She seemed tired.",
        "The window appears broken.",
        "He looks worried.",
        "They remain concerned.",
        "The code is complex.",
        "The solution seems elegant.",
        "The results appear promising.",
        "The team feels confident.",
        "The project looks finished.",
    ];

    #endregion

    #region Adjective Complement Sentences

    /// <summary>
    /// Sentences with participial adjectives that should NOT be flagged as passive.
    /// </summary>
    /// <remarks>
    /// These use past participles as adjectives describing a state, not an action.
    /// </remarks>
    public static readonly string[] AdjectiveComplement =
    [
        "I am tired from the long journey.",
        "She is bored with the lecture.",
        "The children are excited about the trip.",
        "He was surprised by the news.",
        "They were pleased with the result.",
        "The glass is broken.",
        "The meeting is finished.",
        "The report is completed.",
        "The system is updated.",
        "The file is deleted.",
    ];

    #endregion

    #region Progressive Active Sentences

    /// <summary>
    /// Active progressive sentences that should NOT be flagged as passive.
    /// </summary>
    /// <remarks>
    /// Pattern: is/are/was/were + verb-ing (present participle, not past participle).
    /// </remarks>
    public static readonly string[] ProgressiveActive =
    [
        "The team is reviewing the project.",
        "She is writing the documentation.",
        "They are implementing the feature.",
        "He was debugging the code.",
        "We were testing the application.",
        "The server is processing requests.",
        "The system is running smoothly.",
        "The developers are coding the solution.",
        "The build is compiling now.",
        "The tests are passing.",
    ];

    #endregion

    #region Get-Passive Sentences

    /// <summary>
    /// Get-passive constructions that SHOULD be detected as passive.
    /// </summary>
    /// <remarks>
    /// Pattern: get/gets/got/gotten/getting + past participle.
    /// </remarks>
    public static readonly string[] GetPassive =
    [
        "She got fired last week.",
        "He got promoted to senior developer.",
        "The code got reviewed by the lead.",
        "The bug got fixed overnight.",
        "They got invited to the meeting.",
        "The feature got deployed yesterday.",
        "The server got upgraded last month.",
        "The team got assigned a new project.",
        "The report gets generated automatically.",
        "The data gets processed every hour.",
    ];

    #endregion

    #region Modal Passive Sentences

    /// <summary>
    /// Modal passive constructions that SHOULD be detected as passive.
    /// </summary>
    /// <remarks>
    /// Pattern: modal (will/should/must/can/could/would/might) + be + past participle.
    /// </remarks>
    public static readonly string[] ModalPassive =
    [
        "The file will be deleted tomorrow.",
        "The code should be reviewed before merge.",
        "The tests must be executed before release.",
        "The feature can be enabled in settings.",
        "The data could be corrupted by the bug.",
        "The application would be deployed next week.",
        "The configuration might be overwritten.",
        "The report shall be submitted by Friday.",
        "The changes may be reverted if needed.",
        "The system ought to be restarted.",
    ];

    #endregion

    #region Edge Cases

    /// <summary>
    /// Edge cases for robustness testing.
    /// </summary>
    /// <remarks>
    /// Tests handling of empty strings, whitespace, case variations, etc.
    /// </remarks>
    public static readonly string[] EdgeCase =
    [
        "",                                          // Empty string
        "   ",                                       // Whitespace only
        "THE CODE WAS WRITTEN BY THE USER.",         // All caps (should detect)
        "The Code Was Written By The User.",         // Title case (should detect)
        "the code was written by the user.",         // All lowercase (should detect)
        "A.",                                        // Single character sentence
        "Was written.",                              // Fragment (should detect)
        "Written by the user.",                      // No auxiliary verb
        "The user.",                                 // No verb
    ];

    #endregion
}
