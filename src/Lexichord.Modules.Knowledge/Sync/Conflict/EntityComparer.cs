// =============================================================================
// File: EntityComparer.cs
// Project: Lexichord.Modules.Knowledge
// Description: Compares entities to detect property-level differences.
// =============================================================================
// LOGIC: EntityComparer performs property-by-property comparison between
//   document and graph entities. It identifies differences in standard
//   properties (Name, Type) and custom properties dictionary,
//   computing confidence scores for each difference.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: IEntityComparer, EntityComparison, PropertyDifference (v0.7.6h),
//               KnowledgeEntity (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Service for comparing entities to detect property-level differences.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IEntityComparer"/> to compare document entities
/// against graph entities and identify property differences.
/// </para>
/// <para>
/// <b>Comparison Logic:</b>
/// <list type="bullet">
///   <item>Compares standard properties: Name, Type, Value, Description.</item>
///   <item>Compares custom properties from the Properties dictionary.</item>
///   <item>Computes confidence based on value similarity.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public sealed class EntityComparer : IEntityComparer
{
    private readonly ILogger<EntityComparer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EntityComparer"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public EntityComparer(ILogger<EntityComparer> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<EntityComparison> CompareAsync(
        KnowledgeEntity documentEntity,
        KnowledgeEntity graphEntity,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Comparing entities: Document={DocName} ({DocType}) vs Graph={GraphName} ({GraphType})",
            documentEntity.Name, documentEntity.Type,
            graphEntity.Name, graphEntity.Type);

        var differences = new List<PropertyDifference>();

        // LOGIC: Compare standard properties.

        // Compare Name
        if (!StringEquals(documentEntity.Name, graphEntity.Name))
        {
            differences.Add(new PropertyDifference
            {
                PropertyName = "Name",
                DocumentValue = documentEntity.Name,
                GraphValue = graphEntity.Name,
                Confidence = ComputeStringConfidence(documentEntity.Name, graphEntity.Name)
            });
        }

        // Compare Type
        if (!StringEquals(documentEntity.Type, graphEntity.Type))
        {
            differences.Add(new PropertyDifference
            {
                PropertyName = "Type",
                DocumentValue = documentEntity.Type,
                GraphValue = graphEntity.Type,
                Confidence = ComputeStringConfidence(documentEntity.Type, graphEntity.Type)
            });
        }

        // LOGIC: Compare custom properties from Properties dictionary.
        var allPropertyNames = documentEntity.Properties.Keys
            .Union(graphEntity.Properties.Keys)
            .Distinct()
            .OrderBy(k => k);

        foreach (var propName in allPropertyNames)
        {
            ct.ThrowIfCancellationRequested();

            var docHas = documentEntity.Properties.TryGetValue(propName, out var docValue);
            var graphHas = graphEntity.Properties.TryGetValue(propName, out var graphValue);

            if (docHas && graphHas)
            {
                // LOGIC: Both have the property - compare values.
                if (!ValuesEqual(docValue, graphValue))
                {
                    differences.Add(new PropertyDifference
                    {
                        PropertyName = propName,
                        DocumentValue = docValue,
                        GraphValue = graphValue,
                        Confidence = ComputeValueConfidence(docValue, graphValue)
                    });
                }
            }
            else if (docHas && !graphHas)
            {
                // LOGIC: Property exists in document but not in graph (added).
                differences.Add(new PropertyDifference
                {
                    PropertyName = propName,
                    DocumentValue = docValue,
                    GraphValue = null,
                    Confidence = 1.0f // High confidence in new property detection
                });
            }
            else if (!docHas && graphHas)
            {
                // LOGIC: Property exists in graph but not in document (removed).
                differences.Add(new PropertyDifference
                {
                    PropertyName = propName,
                    DocumentValue = null,
                    GraphValue = graphValue,
                    Confidence = 1.0f // High confidence in removal detection
                });
            }
        }

        _logger.LogDebug(
            "Entity comparison complete: {Count} differences found",
            differences.Count);

        var result = new EntityComparison
        {
            DocumentEntity = documentEntity,
            GraphEntity = graphEntity,
            PropertyDifferences = differences
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Performs case-insensitive string comparison.
    /// </summary>
    private static bool StringEquals(string? a, string? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compares two values for equality.
    /// </summary>
    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        // LOGIC: Handle string comparison case-insensitively.
        if (a is string strA && b is string strB)
        {
            return string.Equals(strA, strB, StringComparison.OrdinalIgnoreCase);
        }

        return a.Equals(b);
    }

    /// <summary>
    /// Computes confidence for string differences using simple similarity.
    /// </summary>
    private static float ComputeStringConfidence(string? a, string? b)
    {
        if (a is null || b is null) return 1.0f;

        // LOGIC: Use simple character overlap as a basic similarity measure.
        // This could be enhanced with Levenshtein distance or other algorithms.
        var longer = a.Length > b.Length ? a : b;
        var shorter = a.Length > b.Length ? b : a;

        if (longer.Length == 0) return 1.0f;

        // Count matching characters
        var matchCount = 0;
        var shorterLower = shorter.ToLowerInvariant();
        var longerLower = longer.ToLowerInvariant();

        for (var i = 0; i < shorterLower.Length; i++)
        {
            if (i < longerLower.Length && shorterLower[i] == longerLower[i])
            {
                matchCount++;
            }
        }

        // LOGIC: Higher similarity = lower confidence in the "difference" being real.
        // Completely different strings = high confidence there's a real conflict.
        var similarity = (float)matchCount / longer.Length;
        return 1.0f - similarity;
    }

    /// <summary>
    /// Computes confidence for general value differences.
    /// </summary>
    private static float ComputeValueConfidence(object? a, object? b)
    {
        if (a is null || b is null) return 1.0f;

        // LOGIC: For strings, use string confidence.
        if (a is string strA && b is string strB)
        {
            return ComputeStringConfidence(strA, strB);
        }

        // LOGIC: For numbers, compute relative difference.
        if (a is IConvertible convA && b is IConvertible convB)
        {
            try
            {
                var numA = Convert.ToDouble(convA);
                var numB = Convert.ToDouble(convB);

                if (numA == numB) return 0.0f; // No difference

                var maxAbs = Math.Max(Math.Abs(numA), Math.Abs(numB));
                if (maxAbs == 0) return 1.0f;

                var relativeDiff = Math.Abs(numA - numB) / maxAbs;
                return (float)Math.Min(relativeDiff, 1.0);
            }
            catch
            {
                // Fall through to default
            }
        }

        // LOGIC: Default: complete difference = high confidence.
        return 1.0f;
    }
}
