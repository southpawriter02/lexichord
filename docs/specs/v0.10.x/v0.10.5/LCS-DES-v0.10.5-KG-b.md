# LCS-DES-v0.10.5-KG-b: Design Specification — Schema Mapper

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-105-b` | Knowledge Import/Export sub-part b |
| **Feature Name** | `Schema Mapper` | Map external schemas to CKVS model |
| **Target Version** | `v0.10.5b` | Second sub-part of v0.10.5-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Core Knowledge Validation System |
| **Swimlane** | `Knowledge Management` | Knowledge vertical |
| **License Tier** | `WriterPro` | WriterPro and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.KnowledgeImportExport` | Import/Export feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.5-KG](./LCS-SBD-v0.10.5-KG.md) | Knowledge Import/Export scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.5-KG S2.1](./LCS-SBD-v0.10.5-KG.md#21-sub-parts) | b = Schema Mapper |

---

## 2. Executive Summary

### 2.1 The Requirement

The Schema Mapper bridges external ontologies and knowledge representations to the CKVS model by:

1. Auto-detecting mappings from parsed external schemas
2. Validating mapping consistency with CKVS constraints
3. Persisting mappings for reuse across imports
4. Supporting user adjustment through the mapping UI
5. Providing confidence scoring for auto-detected mappings

### 2.2 The Proposed Solution

Implement a comprehensive schema mapping service with:

1. **Auto-detection engine:** Analyze external schema and suggest CKVS mappings
2. **Mapping validator:** Ensure mappings are consistent and valid
3. **Mapping persistence:** Store and retrieve saved mappings
4. **Heuristic matching:** Use similarity metrics and naming conventions
5. **Confidence scoring:** Indicate quality of detected mappings

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Access CKVS schema and entity types |
| `IAxiomStore` | v0.4.6-KG | Validate mappings against CKVS axioms |
| `IValidationEngine` | v0.6.5-KG | Validate mapping consistency |
| Format Parsers | v0.10.5a | Get parsed graph structure |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `FuzzySharp` or similar | Latest | String similarity matching |

### 3.2 Licensing Behavior

- **WriterPro Tier:** Basic auto-detection only
- **Teams Tier:** Full auto-detection + custom mapping creation + persistence
- **Enterprise Tier:** Teams tier + advanced heuristics + ML-based suggestions

---

## 4. Data Contract (The API)

### 4.1 ISchemaMappingService Interface

```csharp
namespace Lexichord.Modules.CKVS.ImportExport.Contracts;

/// <summary>
/// Service for detecting, validating, and managing schema mappings
/// from external formats to CKVS model.
/// </summary>
public interface ISchemaMappingService
{
    /// <summary>
    /// Auto-detects mappings from an external schema.
    /// Analyzes entity types, properties, and relationships to suggest CKVS mappings.
    /// </summary>
    Task<SchemaMapping> DetectMappingsAsync(
        ParsedGraph externalSchema,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a schema mapping for consistency and correctness.
    /// </summary>
    Task<MappingValidationResult> ValidateMappingAsync(
        SchemaMapping mapping,
        CancellationToken ct = default);

    /// <summary>
    /// Saves a mapping for reuse across multiple imports.
    /// </summary>
    Task<Guid> SaveMappingAsync(
        SchemaMapping mapping,
        string name,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a previously saved mapping by ID.
    /// </summary>
    Task<SavedMapping?> GetMappingAsync(
        Guid mappingId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all saved mappings, optionally filtered.
    /// </summary>
    Task<IReadOnlyList<SavedMapping>> GetSavedMappingsAsync(
        MappingFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a saved mapping.
    /// </summary>
    Task DeleteMappingAsync(
        Guid mappingId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets CKVS entity types for use in mapping suggestions.
    /// </summary>
    Task<IReadOnlyList<EntityTypeInfo>> GetCkvsEntityTypesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets CKVS relationship types for use in mapping suggestions.
    /// </summary>
    Task<IReadOnlyList<RelationshipTypeInfo>> GetCkvsRelationshipTypesAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Complete schema mapping from external format to CKVS model.
/// </summary>
public record SchemaMapping
{
    /// <summary>
    /// Unique identifier for this mapping (if saved).
    /// </summary>
    public Guid? MappingId { get; init; }

    /// <summary>
    /// Human-readable name of the mapping.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Description of what this mapping is used for.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type mappings (external class → CKVS entity type).
    /// </summary>
    public required IReadOnlyList<TypeMapping> TypeMappings { get; init; } = [];

    /// <summary>
    /// Property mappings (external property → CKVS property).
    /// </summary>
    public required IReadOnlyList<PropertyMapping> PropertyMappings { get; init; } = [];

    /// <summary>
    /// Relationship mappings (external predicate → CKVS relationship type).
    /// </summary>
    public required IReadOnlyList<RelationshipMapping> RelationshipMappings { get; init; } = [];

    /// <summary>
    /// Namespace prefix mappings for output.
    /// </summary>
    public IReadOnlyDictionary<string, string>? NamespacePrefixes { get; init; }

    /// <summary>
    /// Metadata about when mapping was created/modified.
    /// </summary>
    public MappingMetadata? Metadata { get; init; }

    /// <summary>
    /// Properties that were not mapped (left unmapped).
    /// </summary>
    public IReadOnlyList<UnmappedProperty> UnmappedProperties { get; init; } = [];
}

/// <summary>
/// Maps an external entity type to a CKVS entity type.
/// </summary>
public record TypeMapping
{
    /// <summary>
    /// External type name (e.g., "Class", "ontology:Service").
    /// </summary>
    public required string SourceType { get; init; }

    /// <summary>
    /// Namespace of the external type.
    /// </summary>
    public required string SourceNamespace { get; init; }

    /// <summary>
    /// Target CKVS entity type name.
    /// </summary>
    public required string TargetType { get; init; }

    /// <summary>
    /// Confidence in this mapping (0-1).
    /// 1.0 = certain, 0.0 = guess.
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Whether this mapping was auto-detected vs. manually specified.
    /// </summary>
    public required bool IsAutoDetected { get; init; }

    /// <summary>
    /// Reason for the mapping (for user understanding).
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Alternative type suggestions (ordered by confidence).
    /// </summary>
    public IReadOnlyList<AlternativeMapping>? Alternatives { get; init; }
}

/// <summary>
/// Maps an external property to a CKVS property.
/// </summary>
public record PropertyMapping
{
    /// <summary>
    /// External property name.
    /// </summary>
    public required string SourceProperty { get; init; }

    /// <summary>
    /// Entity type this property applies to (or null for all).
    /// </summary>
    public string? SourceType { get; init; }

    /// <summary>
    /// Target CKVS property name.
    /// </summary>
    public required string TargetProperty { get; init; }

    /// <summary>
    /// Optional transformation to apply to the value.
    /// </summary>
    public PropertyTransform? Transform { get; init; }

    /// <summary>
    /// Confidence in this mapping (0-1).
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Whether this mapping was auto-detected.
    /// </summary>
    public required bool IsAutoDetected { get; init; }

    /// <summary>
    /// Expected data type of the property value.
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Alternative property suggestions.
    /// </summary>
    public IReadOnlyList<AlternativeMapping>? Alternatives { get; init; }
}

/// <summary>
/// Maps an external relationship to a CKVS relationship type.
/// </summary>
public record RelationshipMapping
{
    /// <summary>
    /// External relationship type name.
    /// </summary>
    public required string SourceRelationship { get; init; }

    /// <summary>
    /// Namespace of the external relationship.
    /// </summary>
    public required string SourceNamespace { get; init; }

    /// <summary>
    /// Target CKVS relationship type name.
    /// </summary>
    public required string TargetRelationship { get; init; }

    /// <summary>
    /// Source entity type constraint (optional).
    /// </summary>
    public string? SourceTypeConstraint { get; init; }

    /// <summary>
    /// Target entity type constraint (optional).
    /// </summary>
    public string? TargetTypeConstraint { get; init; }

    /// <summary>
    /// Confidence in this mapping (0-1).
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Whether this mapping was auto-detected.
    /// </summary>
    public required bool IsAutoDetected { get; init; }

    /// <summary>
    /// Whether the relationship direction should be reversed.
    /// </summary>
    public bool ReverseDirection { get; init; } = false;

    /// <summary>
    /// Alternative relationship suggestions.
    /// </summary>
    public IReadOnlyList<AlternativeMapping>? Alternatives { get; init; }
}

/// <summary>
/// Transformation to apply to a property value during import.
/// </summary>
public record PropertyTransform
{
    /// <summary>
    /// Type of transformation.
    /// </summary>
    public required TransformType Type { get; init; }

    /// <summary>
    /// Regex pattern (for Extract and Convert).
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Replacement string (for Rename and Extract).
    /// </summary>
    public string? Replacement { get; init; }

    /// <summary>
    /// Target type for type conversion.
    /// </summary>
    public string? TargetType { get; init; }

    /// <summary>
    /// Optional parsing format (e.g., date format string).
    /// </summary>
    public string? Format { get; init; }
}

/// <summary>
/// Types of transformations available.
/// </summary>
public enum TransformType
{
    /// <summary>No transformation.</summary>
    None,

    /// <summary>Rename/map the property.</summary>
    Rename,

    /// <summary>Extract part of value using regex.</summary>
    Extract,

    /// <summary>Combine multiple properties.</summary>
    Combine,

    /// <summary>Convert to different type.</summary>
    Convert,

    /// <summary>Apply custom function.</summary>
    Custom
}

/// <summary>
/// Alternative mapping suggestion.
/// </summary>
public record AlternativeMapping
{
    /// <summary>
    /// The alternative target (type name, property name, etc.).
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Confidence score for this alternative (0-1).
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Reason why this alternative is suggested.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Result of mapping validation.
/// </summary>
public record MappingValidationResult
{
    /// <summary>
    /// Whether the mapping is valid and usable.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors (prevent import if any).
    /// </summary>
    public IReadOnlyList<MappingError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings (user should review).
    /// </summary>
    public IReadOnlyList<MappingWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Coverage percentage (how many entities/relationships are mapped).
    /// </summary>
    public float CoveragePercentage { get; init; }

    /// <summary>
    /// Estimated confidence in the mapping (0-1).
    /// </summary>
    public float OverallConfidence { get; init; }
}

/// <summary>
/// A validation error in a mapping.
/// </summary>
public record MappingError
{
    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Type of mapping (TypeMapping, PropertyMapping, etc.).
    /// </summary>
    public required string MappingType { get; init; }

    /// <summary>
    /// Source element involved in the error.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Target element involved in the error.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Error code for categorization.
    /// </summary>
    public string? ErrorCode { get; init; }
}

/// <summary>
/// A validation warning for a mapping.
/// </summary>
public record MappingWarning
{
    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Affected mapping element.
    /// </summary>
    public string? AffectedElement { get; init; }

    /// <summary>
    /// Severity level.
    /// </summary>
    public WarningLevel Level { get; init; }
}

/// <summary>
/// Warning severity levels.
/// </summary>
public enum WarningLevel
{
    /// <summary>Informational, no action needed.</summary>
    Info,

    /// <summary>Should be reviewed before import.</summary>
    Warning,

    /// <summary>May affect import quality.</summary>
    Caution
}

/// <summary>
/// Metadata about a mapping.
/// </summary>
public record MappingMetadata
{
    /// <summary>
    /// User who created the mapping.
    /// </summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>
    /// When the mapping was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the mapping was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <summary>
    /// User who last modified the mapping.
    /// </summary>
    public Guid? ModifiedBy { get; init; }

    /// <summary>
    /// External schema format this mapping is for.
    /// </summary>
    public ImportFormat? SourceFormat { get; init; }

    /// <summary>
    /// Tags for organizing mappings.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Number of times this mapping has been used.
    /// </summary>
    public int UsageCount { get; init; }
}

/// <summary>
/// A property from the external schema that was not mapped.
/// </summary>
public record UnmappedProperty
{
    /// <summary>
    /// The property name.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Entity type this property applies to.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Reason why it wasn't mapped.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Suggested CKVS property name for user consideration.
    /// </summary>
    public string? SuggestedTarget { get; init; }

    /// <summary>
    /// Confidence in the suggestion (0-1).
    /// </summary>
    public float? SuggestionConfidence { get; init; }
}

/// <summary>
/// A saved mapping with metadata.
/// </summary>
public record SavedMapping
{
    /// <summary>
    /// Mapping ID.
    /// </summary>
    public required Guid MappingId { get; init; }

    /// <summary>
    /// Mapping name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The actual mapping.
    /// </summary>
    public required SchemaMapping Mapping { get; init; }

    /// <summary>
    /// Metadata about the mapping.
    /// </summary>
    public required MappingMetadata Metadata { get; init; }

    /// <summary>
    /// Last validation result.
    /// </summary>
    public MappingValidationResult? LastValidation { get; init; }
}

/// <summary>
/// Filter for querying saved mappings.
/// </summary>
public record MappingFilter
{
    /// <summary>
    /// Search by name (partial match).
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// Filter by source format.
    /// </summary>
    public ImportFormat? SourceFormat { get; init; }

    /// <summary>
    /// Filter by creation date range.
    /// </summary>
    public DateRange? CreatedDateRange { get; init; }

    /// <summary>
    /// Filter by tags.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Sort order.
    /// </summary>
    public MappingSortOrder SortOrder { get; init; } = MappingSortOrder.RecentlyModified;
}

/// <summary>
/// Date range for filtering.
/// </summary>
public record DateRange
{
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
}

/// <summary>
/// Sort order for mapping queries.
/// </summary>
public enum MappingSortOrder
{
    /// <summary>Sort by name A-Z.</summary>
    NameAscending,

    /// <summary>Sort by name Z-A.</summary>
    NameDescending,

    /// <summary>Most recently modified first.</summary>
    RecentlyModified,

    /// <summary>Most recently created first.</summary>
    RecentlyCreated,

    /// <summary>Most frequently used first.</summary>
    MostUsed
}

/// <summary>
/// Information about a CKVS entity type for mapping UI.
/// </summary>
public record EntityTypeInfo
{
    /// <summary>
    /// Type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Type description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Available properties for this type.
    /// </summary>
    public IReadOnlyList<PropertyInfo> Properties { get; init; } = [];

    /// <summary>
    /// Whether this type is abstract.
    /// </summary>
    public bool IsAbstract { get; init; }

    /// <summary>
    /// Parent type (if any).
    /// </summary>
    public string? ParentType { get; init; }
}

/// <summary>
/// Information about a property for display in mapping UI.
/// </summary>
public record PropertyInfo
{
    /// <summary>
    /// Property name.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Expected data type.
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Whether property is required.
    /// </summary>
    public bool IsRequired { get; init; }
}

/// <summary>
/// Information about a CKVS relationship type for mapping UI.
/// </summary>
public record RelationshipTypeInfo
{
    /// <summary>
    /// Relationship type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Source entity type constraint.
    /// </summary>
    public string? SourceTypeConstraint { get; init; }

    /// <summary>
    /// Target entity type constraint.
    /// </summary>
    public string? TargetTypeConstraint { get; init; }

    /// <summary>
    /// Whether relationship is directed.
    /// </summary>
    public bool IsDirected { get; init; } = true;
}
```

---

## 5. Implementation Strategy

### 5.1 Detection Algorithm

```
Input: ParsedGraph from external schema
├─→ [1] Type Detection
│        • Analyze all entity types in parsed graph
│        • Find similar CKVS entity types by name/similarity
│        • Calculate confidence based on match quality
│        • Consider ontology hints (rdfs:label, rdfs:comment)
│
├─→ [2] Property Detection
│        • For each type, collect all properties
│        • Match properties to CKVS properties
│        • Identify transformation needs
│        • Note unmapped properties
│
├─→ [3] Relationship Detection
│        • Analyze relationship types in parsed graph
│        • Find matching CKVS relationship types
│        • Check domain/range constraints
│        • Detect direction reversals
│
└─→ Output: SchemaMapping
   (All type, property, and relationship mappings)
```

### 5.2 Confidence Calculation

Confidence is based on:
- **Name similarity:** Levenshtein distance or token overlap
- **Description match:** Semantic similarity (if available)
- **Cardinality match:** Property multiplicity alignment
- **Type constraints:** Domain/range compatibility
- **User feedback:** Boost for previously validated mappings

### 5.3 Heuristic Matching

Common patterns:
- `rdfs:label` → property label
- `rdfs:comment` → property description
- `rdf:type` → entity type
- `owl:ObjectProperty` → relationship type
- `owl:DatatypeProperty` → scalar property

---

## 6. Testing

### 6.1 Unit Tests

```csharp
[TestClass]
public class SchemaMappingServiceTests
{
    [TestMethod]
    public async Task DetectMappingsAsync_SimpleOntology_MappingsDetected()
    {
        var graph = new ParsedGraph
        {
            Format = ImportFormat.OwlXml,
            Entities = new[]
            {
                new ParsedEntity
                {
                    Id = "http://example.com/Service",
                    Namespace = "http://example.com/",
                    LocalName = "Service",
                    Types = new[] { "http://www.w3.org/2002/07/owl#Class" }
                }
            }.ToList()
        };

        var service = new SchemaMappingService(/* dependencies */);
        var mapping = await service.DetectMappingsAsync(graph);

        Assert.IsTrue(mapping.TypeMappings.Count > 0);
        Assert.AreEqual("Service", mapping.TypeMappings[0].SourceType);
    }

    [TestMethod]
    public async Task ValidateMappingAsync_ValidMapping_ReturnsValid()
    {
        var mapping = new SchemaMapping
        {
            TypeMappings = new[] { /* valid mapping */ }.ToList()
        };

        var service = new SchemaMappingService(/* dependencies */);
        var result = await service.ValidateMappingAsync(mapping);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task SaveMappingAsync_NewMapping_ReturnsMappingId()
    {
        var mapping = new SchemaMapping { TypeMappings = [] };
        var service = new SchemaMappingService(/* dependencies */);

        var id = await service.SaveMappingAsync(mapping, "Test Mapping");

        Assert.AreNotEqual(Guid.Empty, id);
    }

    [TestMethod]
    public async Task GetSavedMappingsAsync_WithFilter_FiltersCorrectly()
    {
        var service = new SchemaMappingService(/* dependencies */);
        var filter = new MappingFilter { SourceFormat = ImportFormat.Turtle };

        var mappings = await service.GetSavedMappingsAsync(filter);

        Assert.IsTrue(mappings.All(m => m.Mapping.Metadata?.SourceFormat == ImportFormat.Turtle));
    }
}
```

### 6.2 Integration Tests

```csharp
[TestClass]
public class SchemaMappingIntegrationTests
{
    [TestMethod]
    public async Task DetectMappingsAsync_ComplexOntology_AllMappingsDetected()
    {
        var ontologyFile = "test-data/api-ontology.owl";
        var parser = new OwlFormatParser(ImportFormat.OwlXml);
        var parsedGraph = await parser.ParseAsync(File.OpenRead(ontologyFile), new());

        var service = new SchemaMappingService(/* dependencies */);
        var mapping = await service.DetectMappingsAsync(parsedGraph);

        Assert.IsTrue(mapping.TypeMappings.Count > 0);
        Assert.IsTrue(mapping.PropertyMappings.Count > 0);
        Assert.IsTrue(mapping.RelationshipMappings.Count > 0);
    }

    [TestMethod]
    public async Task SaveAndRetrieveMapping_RoundTrip_PreservesMapping()
    {
        var mapping = new SchemaMapping
        {
            TypeMappings = new[] { /* mappings */ }.ToList()
        };

        var service = new SchemaMappingService(/* dependencies */);
        var id = await service.SaveMappingAsync(mapping, "Test");
        var retrieved = await service.GetMappingAsync(id);

        Assert.AreEqual(mapping.TypeMappings.Count, retrieved?.Mapping.TypeMappings.Count);
    }
}
```

---

## 7. Error Handling

### 7.1 Unmappable Type

**Scenario:** External type has no good match in CKVS.

**Handling:**
- Mark mapping with low confidence
- Include warning in validation result
- Suggest creating new entity type or using generic type
- Allow user to manually select target type

### 7.2 Circular Dependencies

**Scenario:** Schema has circular type references.

**Handling:**
- Detect cycles during relationship mapping
- Report as warning, not error
- Allow import to proceed
- Document cycle in mapping metadata

### 7.3 Property Type Mismatch

**Scenario:** Property type in external schema doesn't match target property.

**Handling:**
- Suggest PropertyTransform to convert value
- Warn user about potential data loss
- Provide examples of conversion
- Allow user to skip or rename property

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Auto-detection (1K entities) | <5s | Parallel type matching |
| Mapping validation | <2s | Schema cache + batch validation |
| Save mapping | <500ms | Async persistence |
| Retrieve saved mappings | <200ms | In-memory cache |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Mapping injection | Low | Type-safe API, validation |
| Data loss from bad mapping | Medium | Validation + user review |
| Performance degradation | Low | Caching + timeouts |

---

## 10. License Gating

| Tier | Features |
| :--- | :--- |
| WriterPro | Basic auto-detection (JSON-LD/CSV only) |
| Teams | Full auto-detection + persistence |
| Enterprise | Teams + ML-based suggestions |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Parsed external schema | DetectMappingsAsync | Type/property/relationship mappings detected |
| 2 | Mapping with errors | ValidateMappingAsync | Validation result includes errors |
| 3 | Valid mapping | SaveMappingAsync | Mapping persisted with returned ID |
| 4 | Saved mapping | GetMappingAsync | Original mapping retrieved |
| 5 | Multiple saved mappings | GetSavedMappingsAsync with filter | Filtered results returned |
| 6 | Unmapped property | Detection | UnmappedProperty record created with suggestion |
| 7 | Type with no CKVS match | Detection | Low-confidence mapping with warning |
| 8 | Mapping with alternatives | Detection | AlternativeMapping suggestions included |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - ISchemaMappingService, type/property/relationship mappings, validation, persistence |

---

**Parent Document:** [LCS-DES-v0.10.5-KG-INDEX](./LCS-DES-v0.10.5-KG-INDEX.md)
**Previous Part:** [LCS-DES-v0.10.5-KG-a](./LCS-DES-v0.10.5-KG-a.md)
**Next Part:** [LCS-DES-v0.10.5-KG-c](./LCS-DES-v0.10.5-KG-c.md)
