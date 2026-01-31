# LCS-DES-v0.10.2-KG-a: Design Specification — Inference Rule Language

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-102-a` | Inference Engine sub-part a |
| **Feature Name** | `Inference Rule Language` | DSL for defining inference rules |
| **Target Version** | `v0.10.2a` | First sub-part of v0.10.2-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Composite Knowledge & Versioned Store |
| **Swimlane** | `Knowledge Graph` | Knowledge Graph vertical |
| **License Tier** | `WriterPro` / `Teams` / `Enterprise` | Built-in + Custom rules support |
| **Feature Gate Key** | `FeatureFlags.CKVS.InferenceEngine` | Inference engine feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.2-KG](./LCS-SBD-v0.10.2-KG.md) | Inference Engine scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.2-KG S2.1](./LCS-SBD-v0.10.2-KG.md#21-sub-parts) | a = Inference Rule Language |

---

## 2. Executive Summary

### 2.1 The Requirement

The Inference Engine requires a human-readable Domain-Specific Language (DSL) for expressing inference rules. Rules must support:

1. **Pattern matching** on relationships and properties
2. **Conditions** with logical operators (AND, OR, NOT)
3. **Path matching** for transitive relationships
4. **Property filtering** with type checking and regex patterns
5. **Conclusions** that derive new relationships or properties

### 2.2 The Proposed Solution

Define a rule DSL with keywords and syntax for:

1. **RULE declaration** with name and optional description
2. **WHEN clause** with conditions (pattern matching, filters, negation)
3. **THEN clause** with derivation statements (DERIVE, SET)
4. **Pattern syntax** for relationships, properties, and entity types
5. **Variables** with `?name` prefix for bindings

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IAxiomStore` | v0.4.6-KG | Retrieve stored rule DSL text |
| `IGraphRepository` | v0.4.5e | Validate entity/relationship types at compile time |
| Entity Models | v0.4.5e | Understand entity type schemas |
| Relationship Models | v0.4.5e | Understand relationship type definitions |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Rule language uses only standard C# |

### 3.2 Licensing Behavior

- **WriterPro Tier:** Access to built-in rules (read-only DSL definitions)
- **Teams Tier:** Can create/edit up to 50 custom rules
- **Enterprise Tier:** Unlimited custom rules

---

## 4. Data Contract (The API)

### 4.1 Rule DSL Grammar (EBNF)

```ebnf
rule = "RULE" string newline
        ["DESCRIPTION" string newline]
        "WHEN" conditions newline
        "THEN" conclusions

conditions = condition (newline condition)*

condition = pattern_match
          | property_match
          | type_match
          | negation
          | comparison

pattern_match = variable "-[" relationship_type "]->" variable
              | variable "-[" relationship_type "]->" "\"" string "\""

property_match = variable "HAS" property_name ("=" | "!=" | "MATCHES") value

type_match = variable "TYPE" "\"" entity_type "\""

negation = "NOT" condition

comparison = variable ("==" | "!=") variable
           | property_name ("==" | "!=") value

conclusions = conclusion (newline conclusion)*

conclusion = "DERIVE" pattern_match
           | "DERIVE" property_match
           | "SET" property_name "=" value

variable = "?" identifier

relationship_type = identifier | string

entity_type = string

property_name = identifier

value = string | number | boolean | null

identifier = [a-zA-Z_][a-zA-Z0-9_]*
```

### 4.2 Rule Record Structure

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// An inference rule definition in DSL format.
/// </summary>
public record InferenceRule
{
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    public required Guid RuleId { get; init; }

    /// <summary>
    /// Human-readable name of the rule.
    /// Example: "Grandparent Inference"
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of the rule's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The WHEN clause: conditions as plain DSL text.
    /// Example: "?a -[PARENT_OF]-> ?b\n?b -[PARENT_OF]-> ?c"
    /// </summary>
    public required string Condition { get; init; }

    /// <summary>
    /// The THEN clause: conclusions as plain DSL text.
    /// Example: "DERIVE ?a -[GRANDPARENT_OF]-> ?c"
    /// </summary>
    public required string Conclusion { get; init; }

    /// <summary>
    /// Priority for rule execution order (0-1000, lower = higher priority).
    /// Default is 100 (neutral). Used in conflict resolution.
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Whether this rule is enabled for execution.
    /// Disabled rules are not evaluated during inference.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Scope of rule applicability.
    /// </summary>
    public InferenceRuleScope Scope { get; init; } = InferenceRuleScope.Workspace;

    /// <summary>
    /// Timestamp when this rule was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when this rule was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <summary>
    /// User ID of the rule creator.
    /// </summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>
    /// Whether this is a built-in rule (immutable).
    /// </summary>
    public bool IsBuiltIn { get; init; } = false;
}

/// <summary>
/// Defines the scope where a rule applies.
/// </summary>
public enum InferenceRuleScope
{
    /// <summary>Rule applies to entire workspace.</summary>
    Workspace = 1,

    /// <summary>Rule applies to a specific project.</summary>
    Project = 2,

    /// <summary>Rule applies globally across all workspaces.</summary>
    Global = 3
}
```

### 4.3 Rule AST Representation

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Abstract syntax tree node for a parsed rule.
/// </summary>
public abstract record RuleAstNode;

/// <summary>
/// Root node representing a complete rule.
/// </summary>
public record RuleNode(
    string Name,
    string? Description,
    IReadOnlyList<ConditionNode> Conditions,
    IReadOnlyList<ConclusionNode> Conclusions) : RuleAstNode;

/// <summary>
/// Base class for condition expressions.
/// </summary>
public abstract record ConditionNode : RuleAstNode;

/// <summary>
/// Pattern match condition: ?a -[REL]-> ?b
/// </summary>
public record PatternMatchNode(
    string SourceVariable,
    string RelationshipType,
    string TargetVariable) : ConditionNode;

/// <summary>
/// Pattern match with literal: ?a -[REL]-> "literal"
/// </summary>
public record PatternMatchLiteralNode(
    string SourceVariable,
    string RelationshipType,
    string LiteralValue) : ConditionNode;

/// <summary>
/// Property condition: ?x HAS propName = value
/// </summary>
public record PropertyNode(
    string Variable,
    string PropertyName,
    PropertyOperator Operator,
    object? Value) : ConditionNode;

/// <summary>
/// Type condition: ?x TYPE "EntityType"
/// </summary>
public record TypeNode(
    string Variable,
    string EntityType) : ConditionNode;

/// <summary>
/// Negation: NOT condition
/// </summary>
public record NegationNode(ConditionNode Inner) : ConditionNode;

/// <summary>
/// Comparison: ?x == ?y or property == value
/// </summary>
public record ComparisonNode(
    string LeftOperand,
    ComparisonOperator Operator,
    string RightOperand) : ConditionNode;

/// <summary>
/// Base class for conclusion expressions.
/// </summary>
public abstract record ConclusionNode : RuleAstNode;

/// <summary>
/// Derive relationship: DERIVE ?a -[REL]-> ?b
/// </summary>
public record DeriveRelationshipNode(
    string SourceVariable,
    string RelationshipType,
    string TargetVariable) : ConclusionNode;

/// <summary>
/// Derive property: DERIVE ?x HAS propName = value
/// </summary>
public record DerivePropertyNode(
    string Variable,
    string PropertyName,
    object? Value) : ConclusionNode;

/// <summary>
/// Operators for property matching.
/// </summary>
public enum PropertyOperator
{
    Equals = 1,
    NotEquals = 2,
    Matches = 3
}

/// <summary>
/// Operators for comparison conditions.
/// </summary>
public enum ComparisonOperator
{
    Equals = 1,
    NotEquals = 2
}
```

### 4.4 Rule DSL Examples

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Built-in rule examples shipped with the inference engine.
/// </summary>
public static class BuiltInRuleDsl
{
    /// <summary>
    /// Transitivity rule: if A->B and B->C, derive A->C
    /// </summary>
    public static string GrandparentInference => @"
RULE ""Grandparent Inference""
DESCRIPTION ""Infer grandparent relationships from parent chains""
WHEN
    ?a -[PARENT_OF]-> ?b
    ?b -[PARENT_OF]-> ?c
THEN
    DERIVE ?a -[GRANDPARENT_OF]-> ?c
";

    /// <summary>
    /// Service dependency rule: if A calls endpoint E, and E is in B, derive A depends on B
    /// </summary>
    public static string ServiceDependency => @"
RULE ""Service Dependency Detection""
DESCRIPTION ""Infer service dependencies from API calls""
WHEN
    ?service1 -[CALLS]-> ?endpoint
    ?endpoint -[DEFINED_IN]-> ?service2
    ?service1 != ?service2
THEN
    DERIVE ?service1 -[DEPENDS_ON]-> ?service2
";

    /// <summary>
    /// Document dependency rule: if entity E is defined in D1 and referenced in D2, derive D2 depends on D1
    /// </summary>
    public static string DocumentDependency => @"
RULE ""Document Dependency""
DESCRIPTION ""Infer document dependencies from entity references""
WHEN
    ?entity -[DEFINED_IN]-> ?doc1
    ?entity -[REFERENCED_IN]-> ?doc2
    ?doc1 != ?doc2
THEN
    DERIVE ?doc2 -[DEPENDS_ON]-> ?doc1
";

    /// <summary>
    /// Auth propagation: if endpoint requires auth and caller calls it, caller requires auth
    /// </summary>
    public static string AuthPropagation => @"
RULE ""Auth Requirement Propagation""
DESCRIPTION ""Propagate auth requirements through call chains""
WHEN
    ?callee HAS requiresAuth = true
    ?caller -[CALLS]-> ?callee
THEN
    DERIVE ?caller HAS requiresAuth = true
";

    /// <summary>
    /// Deprecation propagation: if endpoint is deprecated and caller calls it, mark caller
    /// </summary>
    public static string DeprecationPropagation => @"
RULE ""Deprecation Propagation""
DESCRIPTION ""Mark callers of deprecated endpoints as affected""
WHEN
    ?endpoint HAS deprecated = true
    ?caller -[CALLS]-> ?endpoint
THEN
    DERIVE ?caller HAS usesDeprecated = true
";
}
```

### 4.5 Rule DSL Syntax Rules

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Syntax rules and constraints for the rule DSL.
/// </summary>
public static class RuleDslSyntaxRules
{
    /// <summary>
    /// Variables must start with ? and contain only alphanumeric and underscore.
    /// Pattern: ?[a-zA-Z_][a-zA-Z0-9_]*
    /// </summary>
    public const string VariablePattern = @"^\?[a-zA-Z_][a-zA-Z0-9_]*$";

    /// <summary>
    /// Relationship types must be uppercase identifiers or quoted strings.
    /// Pattern: [A-Z_][A-Z0-9_]* or "any string"
    /// </summary>
    public const string RelationshipTypePattern = @"^([A-Z_][A-Z0-9_]*|"".*"")$";

    /// <summary>
    /// Property names must be lowercase identifiers.
    /// Pattern: [a-z_][a-z0-9_]*
    /// </summary>
    public const string PropertyNamePattern = @"^[a-z_][a-z0-9_]*$";

    /// <summary>
    /// String literals are enclosed in double quotes.
    /// Escape sequences: \" for quote, \\ for backslash, \n for newline
    /// </summary>
    public const string StringLiteralPattern = @"^""(?:\\[\\""n]|[^""])*""$";

    /// <summary>
    /// Numeric literals are integers or floats.
    /// Pattern: -?\d+(\.\d+)?
    /// </summary>
    public const string NumericPattern = @"^-?\d+(\.\d+)?$";

    /// <summary>
    /// Boolean literals: true or false
    /// </summary>
    public const string BooleanPattern = @"^(true|false)$";

    /// <summary>
    /// Keywords reserved in the rule DSL.
    /// </summary>
    public static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "RULE",
        "DESCRIPTION",
        "WHEN",
        "THEN",
        "DERIVE",
        "SET",
        "HAS",
        "TYPE",
        "NOT",
        "AND",
        "OR",
        "MATCHES"
    };

    /// <summary>
    /// Operators for property matching and comparison.
    /// </summary>
    public static readonly string[] PropertyOperators = { "=", "!=", "MATCHES" };
    public static readonly string[] ComparisonOperators = { "==", "!=" };
}
```

---

## 5. Rule DSL Detailed Specification

### 5.1 Pattern Matching Syntax

```
// Relationship pattern: source -[type]-> target
?a -[PARENT_OF]-> ?b
?a -[PARENT_OF]-> "literal_value"

// Entity type pattern
?entity TYPE "Service"
?entity TYPE "Endpoint"

// Property pattern
?endpoint HAS path = "/api/users"
?endpoint HAS path MATCHES "/api/*/search"
?service HAS deprecated = true
?service HAS deprecated != true
```

### 5.2 Logical Operators

```
// Conjunction (implicit when lines are listed)
WHEN
    ?a -[PARENT_OF]-> ?b
    ?b -[PARENT_OF]-> ?c

// Explicit negation
WHEN
    NOT ?a -[PARENT_OF]-> ?b

// Negation on property
WHEN
    NOT ?endpoint HAS requiresAuth = true

// Multiple conditions with negation
WHEN
    ?endpoint TYPE "Endpoint"
    NOT ?endpoint HAS deprecated = true
    NOT ?endpoint -[PROTECTED_BY]-> ?provider
```

### 5.3 Variable Bindings

Variables must:
- Start with `?` character
- Be followed by identifier (letter/underscore, then letters/digits/underscore)
- Be consistent across conditions
- Appear in at least one positive condition (not just negations)

```
WHEN
    ?a -[PARENT_OF]-> ?b
    ?b -[PARENT_OF]-> ?c
THEN
    DERIVE ?a -[GRANDPARENT_OF]-> ?c
```

### 5.4 Comparisons and Filters

```
// Variable to variable comparison
WHEN
    ?entity1 -[DEPENDS_ON]-> ?entity2
    ?entity1 != ?entity2

// Property value matching
WHEN
    ?endpoint HAS path = "/secure"
    ?endpoint HAS method = "POST"
    ?endpoint HAS method != "DELETE"

// Regex matching on properties
WHEN
    ?endpoint HAS path MATCHES "/api/*/search"
    ?service HAS name MATCHES "^Auth.*Service$"
```

---

## 6. Implementation Strategy

### 6.1 Parsing Pipeline

1. **Lexer:** Tokenize rule DSL text into tokens (keywords, identifiers, operators, literals)
2. **Parser:** Build AST from token stream, enforcing grammar rules
3. **Validator:** Check variable bindings, entity/relationship types, property existence
4. **Compiler:** Convert AST to executable rule format (covered in v0.10.2b)

### 6.2 Error Handling

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Result of parsing/compiling a rule DSL.
/// </summary>
public record RuleParseResult
{
    /// <summary>
    /// Whether parsing succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Parsed AST if successful.
    /// </summary>
    public RuleNode? Ast { get; init; }

    /// <summary>
    /// Errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<RuleParseError> Errors { get; init; } = [];

    /// <summary>
    /// Warnings (e.g., unused variables).
    /// </summary>
    public IReadOnlyList<RuleParseWarning> Warnings { get; init; } = [];
}

/// <summary>
/// A parsing error with location information.
/// </summary>
public record RuleParseError
{
    /// <summary>
    /// Line number where error occurred (1-indexed).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Column number where error occurred (1-indexed).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The token or text that caused the error.
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// A parsing warning.
/// </summary>
public record RuleParseWarning
{
    /// <summary>
    /// Line number where warning occurred.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }
}
```

### 6.3 Validation Rules

- All variables in conclusions must appear in at least one positive condition
- Relationship types must exist in the schema or be generic
- Entity types must exist in the schema
- Property names must exist on referenced entity types
- No self-referential comparisons (`?a != ?a`)
- Rules must have at least one condition and one conclusion

---

## 7. Testing

### 7.1 Unit Tests

```csharp
[TestClass]
public class RuleDslParserTests
{
    private readonly IRuleDslParser _parser = new RuleDslParser();

    [TestMethod]
    public void ParseRule_SimpleTransitivity_Succeeds()
    {
        var dsl = @"
RULE ""Test Rule""
WHEN
    ?a -[PARENT_OF]-> ?b
    ?b -[PARENT_OF]-> ?c
THEN
    DERIVE ?a -[GRANDPARENT_OF]-> ?c";

        var result = _parser.Parse(dsl);
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Ast);
        Assert.AreEqual("Test Rule", result.Ast.Name);
    }

    [TestMethod]
    public void ParseRule_PropertyMatching_Succeeds()
    {
        var dsl = @"
RULE ""Auth Propagation""
WHEN
    ?callee HAS requiresAuth = true
    ?caller -[CALLS]-> ?callee
THEN
    DERIVE ?caller HAS requiresAuth = true";

        var result = _parser.Parse(dsl);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void ParseRule_Negation_Succeeds()
    {
        var dsl = @"
RULE ""Public Endpoint""
WHEN
    ?endpoint TYPE ""Endpoint""
    NOT ?endpoint HAS requiresAuth = true
THEN
    DERIVE ?endpoint HAS isPublic = true";

        var result = _parser.Parse(dsl);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void ParseRule_MissingVariable_ReturnsError()
    {
        var dsl = @"
RULE ""Bad Rule""
WHEN
    ?a -[PARENT_OF]-> ?b
THEN
    DERIVE ?a -[GRANDPARENT_OF]-> ?c";

        var result = _parser.Parse(dsl);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("?c")));
    }

    [TestMethod]
    public void ParseRule_InvalidSyntax_ReturnsError()
    {
        var dsl = @"
RULE ""Bad Syntax""
WHEN
    ?a [PARENT_OF] ?b
THEN
    DERIVE ?a -[GRANDPARENT_OF]-> ?c";

        var result = _parser.Parse(dsl);
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public void ValidateRule_AllVariablesBound_Succeeds()
    {
        var ast = new RuleNode(
            "Test",
            null,
            new[] {
                new PatternMatchNode("?a", "PARENT_OF", "?b"),
                new PatternMatchNode("?b", "PARENT_OF", "?c")
            },
            new[] {
                new DeriveRelationshipNode("?a", "GRANDPARENT_OF", "?c")
            });

        var result = _parser.Validate(ast);
        Assert.IsTrue(result.IsValid);
    }
}
```

### 7.2 Integration Tests

```csharp
[TestClass]
public class RuleDslIntegrationTests
{
    [TestMethod]
    public void ParseAndCompile_BuiltInRules_AllSucceed()
    {
        var rules = new[]
        {
            BuiltInRuleDsl.GrandparentInference,
            BuiltInRuleDsl.ServiceDependency,
            BuiltInRuleDsl.DocumentDependency,
            BuiltInRuleDsl.AuthPropagation,
            BuiltInRuleDsl.DeprecationPropagation
        };

        foreach (var ruleDsl in rules)
        {
            var result = _parser.Parse(ruleDsl);
            Assert.IsTrue(result.Success, $"Failed to parse rule: {ruleDsl}");
            Assert.IsNull(result.Errors.Any() ? result.Errors[0].Message : null);
        }
    }
}
```

---

## 8. Error Handling

### 8.1 Parsing Errors

| Error Type | Example | Message |
| :--- | :--- | :--- |
| Missing RULE keyword | `WHEN ...` | Expected RULE keyword at start |
| Missing WHEN clause | `RULE "X" THEN ...` | Expected WHEN clause after rule name |
| Invalid variable syntax | `?a2b -> ...` | Invalid variable name, must start with letter or underscore |
| Unbound variable | `DERIVE ?x -> ?y` where ?y not in WHEN | Variable ?y is not bound in WHEN clause |
| Invalid relationship type | `?a -[]-> ?b` | Relationship type cannot be empty |
| Missing property value | `?x HAS prop =` | Property value required after = |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Infinite rule definitions | Low | Parsed as immutable records, no execution yet |
| Code injection via DSL | Medium | Strict tokenization, no code evaluation |
| ReDoS via regex patterns | Medium | Regex validation during compilation (v0.10.2b) |

---

## 10. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Parse rule DSL | <50ms | Hand-written or simple ANTLR grammar |
| Validate rule | <10ms | Single-pass semantic check |
| AST creation | <1ms | Simple object allocation |

---

## 11. License Gating

| Tier | Support |
| :--- | :--- |
| **Core** | Not available |
| **WriterPro** | Read built-in rules (no custom rules) |
| **Teams** | Parse/validate custom rules (up to 50) |
| **Enterprise** | Unlimited rules |

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Rule DSL text | Parsed | Valid RuleNode AST created |
| 2 | Simple transitivity rule | Parsed | All conditions and conclusions captured |
| 3 | Rule with negation | Parsed | NegationNode created correctly |
| 4 | Rule with property match | Parsed | PropertyNode with operator captured |
| 5 | Rule with unbound variable | Parsed | Validation error reported |
| 6 | Built-in rule examples | Parsed | All 5 built-in rules parse successfully |
| 7 | Invalid syntax | Parsed | Appropriate error message with line/column |
| 8 | Duplicate variables | Parsed | Allowed and tracked in AST |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial specification - DSL grammar, AST nodes, built-in rules |
