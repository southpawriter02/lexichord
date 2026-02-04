// =============================================================================
// File: DependencyNode.cs
// Project: Lexichord.Abstractions
// Description: Node in a dependency tree representing a token and its children.
// =============================================================================
// LOGIC: Represents a node in a dependency tree, containing a token and its
//   children. Provides methods for traversing the tree and extracting subtree
//   text for phrase reconstruction.
//
// v0.5.6f: Sentence Parser (Claim Extraction Pipeline)
// Dependencies: Token (v0.5.6f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Parsing;

/// <summary>
/// A node in a dependency tree.
/// </summary>
/// <remarks>
/// <para>
/// Dependency trees represent the grammatical structure of a sentence as a
/// tree where each node corresponds to a word (token), and edges represent
/// grammatical relations.
/// </para>
/// <para>
/// <b>Example Tree:</b> "The endpoint accepts parameters."
/// <code>
///        accepts (ROOT)
///        /      \
///    nsubj      dobj
///      |          |
///  endpoint  parameters
///      |
///     det
///      |
///     The
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6f as part of the Sentence Parser.
/// </para>
/// </remarks>
public record DependencyNode
{
    /// <summary>
    /// The token at this node.
    /// </summary>
    /// <value>The word represented by this node.</value>
    public required Token Token { get; init; }

    /// <summary>
    /// The dependency relation to the parent node.
    /// </summary>
    /// <value>
    /// The relation type (e.g., "nsubj", "dobj"). Null for the root node.
    /// </value>
    public string? Relation { get; init; }

    /// <summary>
    /// Child nodes in the dependency tree.
    /// </summary>
    /// <value>Nodes that depend on this node. May be empty or null.</value>
    public IReadOnlyList<DependencyNode>? Children { get; init; }

    /// <summary>
    /// Gets all descendant nodes of this node.
    /// </summary>
    /// <returns>An enumerable of all descendants in depth-first order.</returns>
    /// <remarks>
    /// LOGIC: Recursively traverses the tree to yield all nodes below this one.
    /// </remarks>
    public IEnumerable<DependencyNode> GetDescendants()
    {
        if (Children == null) yield break;

        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets the text of the subtree rooted at this node.
    /// </summary>
    /// <returns>The concatenated text of all tokens in the subtree, ordered by position.</returns>
    /// <remarks>
    /// LOGIC: Collects this node's token and all descendant tokens, orders by
    /// token index, and joins with spaces.
    /// </remarks>
    /// <example>
    /// <code>
    /// // For node "endpoint" with child "The":
    /// var text = node.GetSubtreeText(); // Returns "The endpoint"
    /// </code>
    /// </example>
    public string GetSubtreeText()
    {
        var tokens = new List<Token> { Token };
        tokens.AddRange(GetDescendants().Select(n => n.Token));
        return string.Join(" ", tokens.OrderBy(t => t.Index).Select(t => t.Text));
    }

    /// <summary>
    /// Finds a child node with the specified relation.
    /// </summary>
    /// <param name="relation">The relation type to search for.</param>
    /// <returns>The first matching child node, or null if not found.</returns>
    public DependencyNode? FindChild(string relation)
    {
        return Children?.FirstOrDefault(c => c.Relation == relation);
    }

    /// <summary>
    /// Finds all child nodes with the specified relation.
    /// </summary>
    /// <param name="relation">The relation type to search for.</param>
    /// <returns>All matching child nodes.</returns>
    public IEnumerable<DependencyNode> FindChildren(string relation)
    {
        return Children?.Where(c => c.Relation == relation) ?? Enumerable.Empty<DependencyNode>();
    }
}
