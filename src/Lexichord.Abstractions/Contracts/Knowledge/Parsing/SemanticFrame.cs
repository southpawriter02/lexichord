// =============================================================================
// File: SemanticFrame.cs
// Project: Lexichord.Abstractions
// Description: A semantic frame representing a predicate and its arguments.
// =============================================================================
// LOGIC: Represents the predicate-argument structure of a clause identified
//   by Semantic Role Labeling. Contains the predicate (typically a verb) and
//   its semantic arguments (agent, patient, etc.).
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: Token, SemanticArgument, SemanticRole (v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A semantic frame representing a predicate and its arguments.
/// </summary>
/// <remarks>
/// <para>
/// Semantic frames capture the predicate-argument structure of a sentence,
/// identifying who did what to whom. This is essential for claim extraction
/// where we need to identify subjects, predicates, and objects.
/// </para>
/// <para>
/// <b>Example:</b> "The endpoint accepts parameters."
/// <list type="bullet">
///   <item><b>Predicate:</b> "accepts"</item>
///   <item><b>ARG0 (Agent):</b> "The endpoint"</item>
///   <item><b>ARG1 (Patient):</b> "parameters"</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var frame = new SemanticFrame
/// {
///     Predicate = verbToken, // "accepts"
///     Arguments = new[]
///     {
///         new SemanticArgument { Role = SemanticRole.ARG0, Tokens = subjectTokens },
///         new SemanticArgument { Role = SemanticRole.ARG1, Tokens = objectTokens }
///     }
/// };
///
/// var agent = frame.Agent;   // ARG0
/// var patient = frame.Patient; // ARG1
/// </code>
/// </example>
public record SemanticFrame
{
    /// <summary>
    /// The predicate (typically a verb) of this frame.
    /// </summary>
    /// <value>The token representing the main action or state.</value>
    public required Token Predicate { get; init; }

    /// <summary>
    /// The semantic arguments of this frame.
    /// </summary>
    /// <value>A list of arguments with their semantic roles.</value>
    public required IReadOnlyList<SemanticArgument> Arguments { get; init; }

    /// <summary>
    /// Gets an argument by its semantic role.
    /// </summary>
    /// <param name="role">The role to search for.</param>
    /// <returns>The matching argument, or null if not found.</returns>
    public SemanticArgument? GetArgument(SemanticRole role)
    {
        return Arguments.FirstOrDefault(a => a.Role == role);
    }

    /// <summary>
    /// Gets the agent argument (ARG0).
    /// </summary>
    /// <value>The agent performing the action, or null if not present.</value>
    /// <remarks>
    /// LOGIC: ARG0 is typically the doer/agent in PropBank conventions.
    /// </remarks>
    public SemanticArgument? Agent => GetArgument(SemanticRole.ARG0);

    /// <summary>
    /// Gets the patient/theme argument (ARG1).
    /// </summary>
    /// <value>The entity affected by the action, or null if not present.</value>
    /// <remarks>
    /// LOGIC: ARG1 is typically the theme/patient in PropBank conventions.
    /// </remarks>
    public SemanticArgument? Patient => GetArgument(SemanticRole.ARG1);

    /// <summary>
    /// Gets the location modifier (ARGM_LOC).
    /// </summary>
    /// <value>The location argument, or null if not present.</value>
    public SemanticArgument? Location => GetArgument(SemanticRole.ARGM_LOC);

    /// <summary>
    /// Gets the temporal modifier (ARGM_TMP).
    /// </summary>
    /// <value>The temporal argument, or null if not present.</value>
    public SemanticArgument? Temporal => GetArgument(SemanticRole.ARGM_TMP);

    /// <summary>
    /// Whether this frame contains a negation.
    /// </summary>
    /// <value>True if ARGM_NEG is present.</value>
    public bool IsNegated => GetArgument(SemanticRole.ARGM_NEG) != null;
}
