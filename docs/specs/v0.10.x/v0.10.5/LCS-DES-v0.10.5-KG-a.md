# LCS-DES-v0.10.5-KG-a: Design Specification — Format Parsers

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-105-a` | Knowledge Import/Export sub-part a |
| **Feature Name** | `Format Parsers` | Parse OWL, RDF, Turtle, JSON-LD formats |
| **Target Version** | `v0.10.5a` | First sub-part of v0.10.5-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Core Knowledge Validation System |
| **Swimlane** | `Knowledge Management` | Knowledge vertical |
| **License Tier** | `WriterPro` | WriterPro and above (CSV/JSON), Teams for all formats |
| **Feature Gate Key** | `FeatureFlags.CKVS.KnowledgeImportExport` | Import/Export feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.5-KG](./LCS-SBD-v0.10.5-KG.md) | Knowledge Import/Export scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.5-KG S2.1](./LCS-SBD-v0.10.5-KG.md#21-sub-parts) | a = Format Parsers |

---

## 2. Executive Summary

### 2.1 The Requirement

v0.10.5-KG requires comprehensive parsing support for standard ontology and knowledge representation formats:

- **OWL/XML:** Web Ontology Language in XML serialization
- **OWL Functional Syntax:** OWL in functional notation
- **RDF/XML:** Resource Description Framework in XML
- **Turtle:** Terse RDF Triple Language
- **N-Triples:** Simple triple format for streaming
- **JSON-LD:** JSON with Linked Data context
- **SKOS:** Simple Knowledge Organization System vocabularies
- **CSV:** Tabular data for simple entity/relationship import

### 2.2 The Proposed Solution

Implement a pluggable format parser system with:

1. **IFormatParser interface:** Pluggable design for format support
2. **Format detection:** Auto-detect input format
3. **Streaming support:** Process large files efficiently
4. **Validation:** Syntax and structure validation
5. **Error reporting:** Detailed error messages with location information

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Store parsed triples/entities |
| Stream handling | System.IO | Input stream processing |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `dotNetRDF` | 3.0+ | RDF/OWL/Turtle/N-Triples parsing |
| `Newtonsoft.Json` | 13.0+ | JSON-LD parsing and processing |
| `CsvHelper` | 30.0+ | CSV parsing |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** CSV and JSON-LD only
- **Teams Tier:** All formats (OWL, RDF, Turtle, N-Triples, SKOS, JSON-LD, CSV)
- **Enterprise Tier:** Teams tier features + extended format support

---

## 4. Data Contract (The API)

### 4.1 IFormatParser Interface

```csharp
namespace Lexichord.Modules.CKVS.ImportExport.Contracts;

/// <summary>
/// Parses a knowledge source in a specific format into an intermediate graph representation.
/// Implementations are pluggable and can be registered for different formats.
/// </summary>
public interface IFormatParser
{
    /// <summary>
    /// Gets the format this parser handles.
    /// </summary>
    ImportFormat Format { get; }

    /// <summary>
    /// Gets supported file extensions for this format.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Determines if this parser can handle the given content.
    /// </summary>
    Task<bool> CanParseAsync(
        Stream content,
        CancellationToken ct = default);

    /// <summary>
    /// Parses content into an intermediate graph representation.
    /// </summary>
    Task<ParsedGraph> ParseAsync(
        Stream content,
        ParserOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates the content without parsing.
    /// </summary>
    Task<ParseValidationResult> ValidateAsync(
        Stream content,
        CancellationToken ct = default);
}

/// <summary>
/// Options for controlling parser behavior.
/// </summary>
public record ParserOptions
{
    /// <summary>
    /// Maximum file size to process (bytes). Null = no limit.
    /// </summary>
    public long? MaxFileSizeBytes { get; init; } = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Use streaming mode for large files.
    /// </summary>
    public bool UseStreaming { get; init; } = true;

    /// <summary>
    /// Default namespace for unprefixed URIs.
    /// </summary>
    public string? DefaultNamespace { get; init; }

    /// <summary>
    /// Namespaces to include (null = all).
    /// </summary>
    public IReadOnlyList<string>? IncludeNamespaces { get; init; }

    /// <summary>
    /// Namespaces to exclude.
    /// </summary>
    public IReadOnlyList<string>? ExcludeNamespaces { get; init; }

    /// <summary>
    /// Stop on first error instead of collecting all errors.
    /// </summary>
    public bool FailFast { get; init; } = false;

    /// <summary>
    /// Log parsing progress.
    /// </summary>
    public bool LogProgress { get; init; } = false;
}

/// <summary>
/// Intermediate graph representation from parser.
/// </summary>
public record ParsedGraph
{
    /// <summary>
    /// Format that was parsed.
    /// </summary>
    public required ImportFormat Format { get; init; }

    /// <summary>
    /// Parsed entities with their properties.
    /// </summary>
    public required IReadOnlyList<ParsedEntity> Entities { get; init; } = [];

    /// <summary>
    /// Parsed relationships between entities.
    /// </summary>
    public required IReadOnlyList<ParsedRelationship> Relationships { get; init; } = [];

    /// <summary>
    /// Namespace mappings found in content.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Namespaces { get; init; }

    /// <summary>
    /// Metadata about the source (title, creator, version, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Any warnings encountered during parsing.
    /// </summary>
    public IReadOnlyList<ParseWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Total number of triples processed.
    /// </summary>
    public int TriplesProcessed { get; init; }

    /// <summary>
    /// Total number of entities extracted.
    /// </summary>
    public int EntitiesExtracted => Entities.Count;

    /// <summary>
    /// Total number of relationships extracted.
    /// </summary>
    public int RelationshipsExtracted => Relationships.Count;
}

/// <summary>
/// A parsed entity from the source.
/// </summary>
public record ParsedEntity
{
    /// <summary>
    /// URI or identifier of the entity.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Namespace URI of the entity.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Local name within the namespace.
    /// </summary>
    public required string LocalName { get; init; }

    /// <summary>
    /// Type(s) of the entity (rdf:type values).
    /// </summary>
    public IReadOnlyList<string> Types { get; init; } = [];

    /// <summary>
    /// Properties and their values.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<PropertyValue>> Properties { get; init; } =
        new Dictionary<string, IReadOnlyList<PropertyValue>>();

    /// <summary>
    /// Labels in various languages.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// Descriptions in various languages.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Descriptions { get; init; }

    /// <summary>
    /// Custom attributes for mapping.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
}

/// <summary>
/// A property value with optional type and language tag.
/// </summary>
public record PropertyValue
{
    /// <summary>
    /// The property value (string, number, boolean, URI reference, etc.).
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// XSD datatype URI (if value is typed literal).
    /// </summary>
    public string? DataType { get; init; }

    /// <summary>
    /// Language tag (if value is language-tagged string).
    /// </summary>
    public string? LanguageTag { get; init; }

    /// <summary>
    /// Whether this value is a URI reference to another entity.
    /// </summary>
    public bool IsReference { get; init; } = false;
}

/// <summary>
/// A parsed relationship between entities.
/// </summary>
public record ParsedRelationship
{
    /// <summary>
    /// URI/ID of the source entity.
    /// </summary>
    public required string SourceEntityId { get; init; }

    /// <summary>
    /// URI/ID of the target entity.
    /// </summary>
    public required string TargetEntityId { get; init; }

    /// <summary>
    /// The relationship type (predicate).
    /// </summary>
    public required string RelationshipType { get; init; }

    /// <summary>
    /// Namespace of the relationship type.
    /// </summary>
    public required string RelationshipNamespace { get; init; }

    /// <summary>
    /// Whether this is an outgoing reference.
    /// </summary>
    public bool IsOutgoing { get; init; } = true;

    /// <summary>
    /// Metadata about the relationship.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Result of parsing validation.
/// </summary>
public record ParseValidationResult
{
    /// <summary>
    /// Whether the content is valid for this format.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors found.
    /// </summary>
    public IReadOnlyList<ParseError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IReadOnlyList<ParseWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Estimated entity count (if determinable without full parse).
    /// </summary>
    public int? EstimatedEntityCount { get; init; }

    /// <summary>
    /// Estimated file complexity.
    /// </summary>
    public ComplexityLevel ComplexityLevel { get; init; }
}

/// <summary>
/// A parsing error with location information.
/// </summary>
public record ParseError
{
    /// <summary>
    /// Error message describing the problem.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Line number in the source (1-indexed).
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Column number in the source (1-indexed).
    /// </summary>
    public int? ColumnNumber { get; init; }

    /// <summary>
    /// Context snippet from the source.
    /// </summary>
    public string? ContextSnippet { get; init; }

    /// <summary>
    /// Error type/code for categorization.
    /// </summary>
    public ParseErrorType ErrorType { get; init; }

    /// <summary>
    /// Severity level.
    /// </summary>
    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;
}

/// <summary>
/// A parsing warning.
/// </summary>
public record ParseWarning
{
    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional line number.
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Warning type/code.
    /// </summary>
    public string? WarningCode { get; init; }
}

/// <summary>
/// Categories of parsing errors.
/// </summary>
public enum ParseErrorType
{
    /// <summary>Syntax error in the format.</summary>
    SyntaxError,

    /// <summary>Invalid namespace or URI.</summary>
    InvalidNamespace,

    /// <summary>Missing required element.</summary>
    MissingElement,

    /// <summary>Invalid datatype or literal value.</summary>
    InvalidDataType,

    /// <summary>Circular reference or invalid structure.</summary>
    StructuralError,

    /// <summary>Unknown or unsupported construct.</summary>
    UnsupportedConstruct,

    /// <summary>Encoding error (character set issue).</summary>
    EncodingError,

    /// <summary>IO error reading the stream.</summary>
    IOError,

    /// <summary>Other error.</summary>
    Other
}

/// <summary>
/// Error severity levels.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Information only, does not affect parsing.</summary>
    Info,

    /// <summary>Warning that should be reviewed but doesn't block parsing.</summary>
    Warning,

    /// <summary>Error that prevents parsing or affects accuracy.</summary>
    Error,

    /// <summary>Critical error that stops parsing immediately.</summary>
    Critical
}

/// <summary>
/// Complexity level of parsed content.
/// </summary>
public enum ComplexityLevel
{
    /// <summary>Simple content with few entities and relationships.</summary>
    Low,

    /// <summary>Moderate complexity.</summary>
    Medium,

    /// <summary>High complexity (large graph, deep nesting, etc.).</summary>
    High,

    /// <summary>Very high complexity (may require special handling).</summary>
    VeryHigh
}
```

### 4.2 Format Parser Registry

```csharp
namespace Lexichord.Modules.CKVS.ImportExport.Contracts;

/// <summary>
/// Registry for format parsers with auto-detection capabilities.
/// </summary>
public interface IFormatParserRegistry
{
    /// <summary>
    /// Registers a format parser.
    /// </summary>
    void Register(IFormatParser parser);

    /// <summary>
    /// Gets a parser for a specific format.
    /// </summary>
    IFormatParser? GetParser(ImportFormat format);

    /// <summary>
    /// Auto-detects the format of content and returns appropriate parser.
    /// </summary>
    Task<(ImportFormat Format, IFormatParser Parser)?> DetectFormatAsync(
        Stream content,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all registered parsers.
    /// </summary>
    IReadOnlyList<IFormatParser> GetAllParsers();

    /// <summary>
    /// Checks if a format is supported.
    /// </summary>
    bool IsFormatSupported(ImportFormat format);
}
```

### 4.3 Specific Parser Implementations

```csharp
namespace Lexichord.Modules.CKVS.ImportExport.Parsers;

/// <summary>
/// Parses RDF/XML, Turtle, N-Triples, and OWL formats using dotNetRDF.
/// </summary>
public class RdfFormatParser : IFormatParser
{
    public ImportFormat Format { get; }
    public IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Initializes parser for a specific RDF-based format.
    /// </summary>
    public RdfFormatParser(ImportFormat format)
    {
        Format = format;
        SupportedExtensions = GetExtensionsForFormat(format);
    }

    public Task<bool> CanParseAsync(Stream content, CancellationToken ct = default);
    public Task<ParsedGraph> ParseAsync(Stream content, ParserOptions options, CancellationToken ct = default);
    public Task<ParseValidationResult> ValidateAsync(Stream content, CancellationToken ct = default);

    private IReadOnlyList<string> GetExtensionsForFormat(ImportFormat format) =>
        format switch
        {
            ImportFormat.RdfXml => new[] { ".rdf", ".xml" },
            ImportFormat.Turtle => new[] { ".ttl", ".turtle" },
            ImportFormat.NTriples => new[] { ".nt", ".ntriples" },
            _ => []
        };
}

/// <summary>
/// Parses OWL formats using dotNetRDF.
/// </summary>
public class OwlFormatParser : IFormatParser
{
    public ImportFormat Format { get; }
    public IReadOnlyList<string> SupportedExtensions => new[] { ".owl", ".xml" };

    /// <summary>
    /// Initializes parser for a specific OWL format.
    /// </summary>
    public OwlFormatParser(ImportFormat format)
    {
        if (format != ImportFormat.OwlXml && format != ImportFormat.OwlFunctional)
            throw new ArgumentException("Unsupported OWL format", nameof(format));
        Format = format;
    }

    public Task<bool> CanParseAsync(Stream content, CancellationToken ct = default);
    public Task<ParsedGraph> ParseAsync(Stream content, ParserOptions options, CancellationToken ct = default);
    public Task<ParseValidationResult> ValidateAsync(Stream content, CancellationToken ct = default);
}

/// <summary>
/// Parses JSON-LD format.
/// </summary>
public class JsonLdFormatParser : IFormatParser
{
    public ImportFormat Format => ImportFormat.JsonLd;
    public IReadOnlyList<string> SupportedExtensions => new[] { ".jsonld", ".json" };

    public Task<bool> CanParseAsync(Stream content, CancellationToken ct = default);
    public Task<ParsedGraph> ParseAsync(Stream content, ParserOptions options, CancellationToken ct = default);
    public Task<ParseValidationResult> ValidateAsync(Stream content, CancellationToken ct = default);
}

/// <summary>
/// Parses SKOS (Simple Knowledge Organization System) vocabularies.
/// </summary>
public class SkosFormatParser : IFormatParser
{
    public ImportFormat Format => ImportFormat.Skos;
    public IReadOnlyList<string> SupportedExtensions => new[] { ".skos", ".rdf", ".xml" };

    public Task<bool> CanParseAsync(Stream content, CancellationToken ct = default);
    public Task<ParsedGraph> ParseAsync(Stream content, ParserOptions options, CancellationToken ct = default);
    public Task<ParseValidationResult> ValidateAsync(Stream content, CancellationToken ct = default);
}

/// <summary>
/// Parses CSV tabular data.
/// </summary>
public class CsvFormatParser : IFormatParser
{
    public ImportFormat Format => ImportFormat.Csv;
    public IReadOnlyList<string> SupportedExtensions => new[] { ".csv", ".tsv", ".txt" };

    public Task<bool> CanParseAsync(Stream content, CancellationToken ct = default);
    public Task<ParsedGraph> ParseAsync(Stream content, ParserOptions options, CancellationToken ct = default);
    public Task<ParseValidationResult> ValidateAsync(Stream content, CancellationToken ct = default);
}

/// <summary>
/// Parses native CKVS JSON format.
/// </summary>
public class CkvsJsonFormatParser : IFormatParser
{
    public ImportFormat Format => ImportFormat.CkvsJson;
    public IReadOnlyList<string> SupportedExtensions => new[] { ".ckvs.json", ".json" };

    public Task<bool> CanParseAsync(Stream content, CancellationToken ct = default);
    public Task<ParsedGraph> ParseAsync(Stream content, ParserOptions options, CancellationToken ct = default);
    public Task<ParseValidationResult> ValidateAsync(Stream content, CancellationToken ct = default);
}
```

---

## 5. Implementation Strategy

### 5.1 Parser Lifecycle

```
Input Stream
    ↓
    ├─→ [1] Format Detection
    │        • File magic bytes
    │        • Header inspection
    │        • File extension
    │
    ├─→ [2] Pre-Validation
    │        • Encoding check
    │        • Basic syntax validation
    │        • Size limits
    │
    ├─→ [3] Parsing
    │        • Triple/statement extraction
    │        • Entity and relationship discovery
    │        • Property collection
    │
    ├─→ [4] Post-Processing
    │        • Namespace consolidation
    │        • Type inference
    │        • Reference resolution
    │
    └─→ ParsedGraph Output
        (Entities, Relationships, Namespaces, Metadata)
```

### 5.2 Streaming Considerations

For large files (>10MB):
- Use streaming parser from dotNetRDF where available
- Process entities in batches (1000 at a time)
- Track memory usage and spill to disk if needed
- Provide progress callbacks

### 5.3 Namespace Handling

- Preserve all namespace prefixes from source
- Auto-generate prefixes for URIs without explicit namespace
- Consolidate duplicate namespace declarations
- Track namespace changes across streaming boundaries

### 5.4 Error Recovery

- Collect all errors instead of failing fast (unless FailFast=true)
- Continue parsing to report all issues in one pass
- Preserve partial results with warnings
- Provide detailed location information for each error

---

## 6. Testing

### 6.1 Unit Tests

```csharp
[TestClass]
public class FormatParserTests
{
    [TestMethod]
    public async Task RdfFormatParser_CanParse_ValidTurtle_ReturnsTrue()
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes(
            @"@prefix ex: <http://example.com/> .
              ex:entity1 a ex:Class1 ;
              ex:property1 ""value1"" ."));

        var parser = new RdfFormatParser(ImportFormat.Turtle);
        var result = await parser.CanParseAsync(content);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task JsonLdFormatParser_ParseAsync_ValidJsonLd_ExtractsEntities()
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes(
            @"{ ""@context"": { ""ex"": ""http://example.com/"" },
               ""@type"": ""ex:Class1"",
               ""ex:property1"": ""value1"" }"));

        var parser = new JsonLdFormatParser();
        var graph = await parser.ParseAsync(content, new ParserOptions());

        Assert.IsTrue(graph.Entities.Count > 0);
    }

    [TestMethod]
    public async Task CsvFormatParser_ParseAsync_SimpleCsv_CreatesEntities()
    {
        var csv = "id,type,name\\n1,Entity,TestEntity\\n2,Relationship,rel1";
        var content = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var parser = new CsvFormatParser();
        var graph = await parser.ParseAsync(content, new ParserOptions());

        Assert.IsTrue(graph.Entities.Count > 0);
    }

    [TestMethod]
    public async Task FormatParserRegistry_DetectFormat_RdfContent_ReturnsCorrectParser()
    {
        var rdfContent = new MemoryStream(Encoding.UTF8.GetBytes(
            @"<?xml version=""1.0""?>
              <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
              </rdf:RDF>"));

        var registry = new FormatParserRegistry();
        var (format, parser) = await registry.DetectFormatAsync(rdfContent) ?? (ImportFormat.RdfXml, null);

        Assert.IsNotNull(parser);
        Assert.AreEqual(ImportFormat.RdfXml, format);
    }

    [TestMethod]
    public async Task RdfFormatParser_ValidateAsync_InvalidSyntax_ReturnsErrors()
    {
        var invalid = new MemoryStream(Encoding.UTF8.GetBytes("@prefix ex: <<<"));
        var parser = new RdfFormatParser(ImportFormat.Turtle);

        var result = await parser.ValidateAsync(invalid);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }
}
```

### 6.2 Integration Tests

```csharp
[TestClass]
public class FormatParserIntegrationTests
{
    [TestMethod]
    public async Task RdfFormatParser_ParseAsync_ComplexOntology_AllEntitiesExtracted()
    {
        var ontologyFile = "test-data/complex-ontology.owl";
        var content = File.OpenRead(ontologyFile);

        var parser = new OwlFormatParser(ImportFormat.OwlXml);
        var graph = await parser.ParseAsync(content, new ParserOptions());

        Assert.IsTrue(graph.EntitiesExtracted > 100);
        Assert.IsTrue(graph.RelationshipsExtracted > 200);
    }

    [TestMethod]
    public async Task FormatParserRegistry_MultipleFormats_AllDetected()
    {
        var registry = new FormatParserRegistry();
        var formats = new[] { ImportFormat.Turtle, ImportFormat.JsonLd, ImportFormat.Csv };

        foreach (var format in formats)
        {
            var parser = registry.GetParser(format);
            Assert.IsNotNull(parser);
        }
    }
}
```

---

## 7. Error Handling

### 7.1 Unsupported Format

**Scenario:** User uploads a file in unsupported format.

**Handling:**
- Format detection fails to identify format
- FormatParserRegistry returns null
- Display error message to user with supported formats list

### 7.2 Corrupted File

**Scenario:** RDF file has invalid syntax.

**Handling:**
- Parser detects syntax error
- Returns ParseValidationResult with IsValid=false
- Detailed error with line/column information
- User can view errors and decide whether to continue

### 7.3 Large File

**Scenario:** User uploads 500MB+ ontology.

**Handling:**
- Parser.CanParseAsync checks file size
- Switch to streaming mode automatically
- Report progress via logger
- Process in batches to manage memory

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Format detection | <100ms | Magic bytes + header inspection |
| Small file parsing (<1MB) | <1s | Full in-memory parsing |
| Medium file parsing (1-100MB) | <30s | Streaming with batching |
| Large file parsing (100MB+) | <5min | Streaming + spill to disk |
| Validation only | <500ms | Partial parse + syntax check |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| XXE (XML External Entity) | Critical | Disable external entity processing in XML parsers |
| Billion laughs attack | High | Limit XML entity expansion depth |
| Malformed input | Medium | Comprehensive error handling |
| Memory exhaustion | High | File size limits + streaming mode |

---

## 10. License Gating

| Tier | Formats Supported |
| :--- | :--- |
| Core | None |
| WriterPro | CSV, JSON-LD |
| Teams | All formats (OWL, RDF, Turtle, N-Triples, SKOS, JSON-LD, CSV) |
| Enterprise | Teams tier + extended formats |

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Valid Turtle file | Parsed | All entities and relationships extracted |
| 2 | Valid JSON-LD file | Parsed | All entities and relationships extracted |
| 3 | Invalid RDF/XML | Validated | Errors reported with line/column info |
| 4 | Large ontology (10K+ entities) | Parsed | Completes in <5 minutes with streaming |
| 5 | CSV file | Parsed | Entities created from rows |
| 6 | Unsupported format | Detected | Format detection fails gracefully |
| 7 | Namespace declarations | Preserved | Prefix mappings maintained |
| 8 | Partial parse on error | FailFast=false | Returns partial graph with warnings |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IFormatParser interface, parser implementations, format detection, streaming support |

---

**Parent Document:** [LCS-DES-v0.10.5-KG-INDEX](./LCS-DES-v0.10.5-KG-INDEX.md)
**Next Part:** [LCS-DES-v0.10.5-KG-b](./LCS-DES-v0.10.5-KG-b.md)
