# LCS-DES-v0.10.4-KG-c: Design Specification — Query Language

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-104c` | Graph Visualization sub-part c |
| **Feature Name** | `Query Language` | CKVS-QL structured query parser and executor |
| **Target Version** | `v0.10.4c` | Third sub-part of v0.10.4-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | CKVS knowledge graph module |
| **Swimlane** | `Graph Visualization` | Visualization vertical |
| **License Tier** | `Teams` | Available in Teams tier and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVisualization` | Graph visualization feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.4-KG](./LCS-SBD-v0.10.4-KG.md) | Graph Visualization & Search scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.4-KG S2.1](./LCS-SBD-v0.10.4-KG.md#21-sub-parts) | c = Query Language |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.10.4-KG requires a structured query language (CKVS-QL) for advanced graph exploration. The Query Language must:

1. Parse CKVS-QL syntax (entity, relationship, path, and scalar queries)
2. Support WHERE clauses with property conditions
3. Support aggregations (GROUP BY, COUNT, SUM, AVG)
4. Support pattern matching and regex
5. Support graph traversal (EXPAND with depth)
6. Support subqueries and filtering
7. Provide autocomplete suggestions
8. Execute complex queries in <2 seconds

### 2.2 The Proposed Solution

Implement a comprehensive Query Language service with:

1. **IGraphQueryService interface:** Main contract for query operations
2. **QueryResult record:** Query execution results with rows and metadata
3. **QueryParser:** Tokenizer and parser for CKVS-QL syntax
4. **QueryExecutor:** Evaluates parsed query against graph data
5. **QuerySuggester:** Provides autocomplete suggestions

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Graph data access |
| `ILicenseContext` | v0.9.2 | License tier validation for Teams+ |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Text.RegularExpressions` | Built-in | Regex pattern matching |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Full CKVS-QL support
- **Enterprise Tier:** Teams + query optimization, explain plans

---

## 4. Data Contract (The API)

### 4.1 IGraphQueryService Interface

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Executes structured queries against the knowledge graph using CKVS-QL syntax.
/// Supports entity, relationship, path, and scalar queries with aggregations.
/// </summary>
public interface IGraphQueryService
{
    /// <summary>
    /// Executes a CKVS-QL query and returns results.
    /// </summary>
    Task<QueryResult> QueryAsync(
        string query,
        QueryOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates query syntax without executing.
    /// Returns validation errors if syntax is invalid.
    /// </summary>
    Task<QueryValidationResult> ValidateAsync(
        string query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets autocomplete suggestions for partial query.
    /// Based on cursor position in the query string.
    /// </summary>
    Task<IReadOnlyList<QuerySuggestion>> GetSuggestionsAsync(
        string partialQuery,
        int cursorPosition,
        CancellationToken ct = default);
}
```

### 4.2 QueryResult Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Result of executing a CKVS-QL query.
/// Contains rows of results and metadata about execution.
/// </summary>
public record QueryResult
{
    /// <summary>
    /// Type of result (Entities, Relationships, Scalars, Paths).
    /// </summary>
    public QueryResultType ResultType { get; init; }

    /// <summary>
    /// Rows of data matching the query.
    /// Each row is a dictionary of column names to values.
    /// </summary>
    public IReadOnlyList<QueryRow> Rows { get; init; } = [];

    /// <summary>
    /// Column names in each row.
    /// </summary>
    public IReadOnlyList<string> Columns { get; init; } = [];

    /// <summary>
    /// Total count of rows (before LIMIT if applied).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Time to execute query (excluding parsing).
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Query execution plan (for debugging/optimization).
    /// Available when QueryOptions.IncludeExplainPlan = true.
    /// </summary>
    public string? ExplainPlan { get; init; }

    /// <summary>
    /// Any warnings from query execution.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
```

### 4.3 QueryRow Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Single row in query results.
/// Maps column names to values.
/// </summary>
public record QueryRow
{
    /// <summary>
    /// Column values for this row.
    /// Key = column name, Value = data (object, string, number, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?>();
}
```

### 4.4 QueryValidationResult Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Result of validating a query without executing.
/// </summary>
public record QueryValidationResult
{
    /// <summary>
    /// Whether the query is syntactically valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors (if IsValid = false).
    /// </summary>
    public IReadOnlyList<QueryValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Warnings about the query (even if valid).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Estimated execution time (if available).
    /// </summary>
    public TimeSpan? EstimatedExecutionTime { get; init; }
}

public record QueryValidationError
{
    public int Line { get; init; }
    public int Column { get; init; }
    public string Message { get; init; } = "";
}
```

### 4.5 QuerySuggestion Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Autocomplete suggestion for partial query.
/// </summary>
public record QuerySuggestion
{
    /// <summary>
    /// The suggested text to insert/complete.
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// Type of suggestion (keyword, entity type, property, etc.).
    /// </summary>
    public QuerySuggestionType Type { get; init; }

    /// <summary>
    /// Optional description of the suggestion.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Text to show in autocomplete list.
    /// </summary>
    public string DisplayText { get; init; } = "";

    /// <summary>
    /// Character position where text should be inserted.
    /// </summary>
    public int InsertionPosition { get; init; }

    /// <summary>
    /// Length of text to replace (for overwriting).
    /// </summary>
    public int ReplaceLength { get; init; } = 0;
}

public enum QuerySuggestionType
{
    Keyword,
    EntityType,
    PropertyName,
    RelationshipType,
    Function,
    Operator,
    Value
}
```

### 4.6 QueryOptions Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Options for query execution.
/// </summary>
public record QueryOptions
{
    /// <summary>
    /// Maximum number of rows to return.
    /// 0 = all rows.
    /// </summary>
    public int Limit { get; init; } = 1000;

    /// <summary>
    /// Number of rows to skip before returning.
    /// </summary>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// Whether to include explain plan in result.
    /// </summary>
    public bool IncludeExplainPlan { get; init; } = false;

    /// <summary>
    /// Timeout in milliseconds for query execution.
    /// 0 = no timeout.
    /// </summary>
    public int TimeoutMs { get; init; } = 30000; // 30 seconds default

    /// <summary>
    /// Whether to return total count (may be slow for large result sets).
    /// </summary>
    public bool CountTotalRows { get; init; } = false;
}

public enum QueryResultType
{
    /// <summary>Entity objects.</summary>
    Entities = 1,

    /// <summary>Relationship objects.</summary>
    Relationships = 2,

    /// <summary>Scalar values (counts, sums, etc.).</summary>
    Scalars = 3,

    /// <summary>Path objects (list of nodes and edges).</summary>
    Paths = 4
}
```

---

## 5. CKVS-QL Syntax & Examples

### 5.1 Query Structure

```sql
-- Basic entity query
FIND Entity
WHERE condition
[GROUP BY property]
[HAVING aggregate_condition]
[ORDER BY property [ASC|DESC]]
[LIMIT n]
[OFFSET n]

-- Relationship query
FIND e1 -[RelationType]-> e2
WHERE condition

-- Path query
FIND PATH FROM entity1 TO entity2 [VIA relationshipTypes] [MAX depth]

-- Aggregation query
FIND Entity
WHERE condition
GROUP BY property
COUNT AS count_col
[SUM property AS sum_col]
[AVG property AS avg_col]
```

### 5.2 Query Examples

```sql
-- Q1: All Endpoints
FIND Entity WHERE type = "Endpoint"

-- Q2: Services with DEPENDS_ON relationships
FIND e1 -[DEPENDS_ON]-> e2
WHERE e1.type = "Service"

-- Q3: Most connected services
FIND e1 -[*]-> e2
WHERE e1.type = "Service"
GROUP BY e1.id
COUNT AS connections
ORDER BY connections DESC
LIMIT 10

-- Q4: Pattern matching
FIND Entity
WHERE name MATCHES ".*Auth.*"

-- Q5: Endpoints requiring authentication
FIND Entity
WHERE type = "Endpoint" AND requiresAuth = true
ORDER BY name

-- Q6: Graph traversal
FIND Entity
WHERE type = "Service"
EXPAND -[DEPENDS_ON]-> DEPTH 2

-- Q7: Subquery
FIND Entity
WHERE id IN (
    FIND e1 -[CALLS]-> e2
    WHERE e2.name = "AuthService"
    RETURN e1.id
)
```

---

## 6. Implementation Details

### 6.1 Lexer/Tokenizer

```csharp
namespace Lexichord.Modules.CKVS.Services.Visualization;

internal class CkvsqlLexer
{
    private readonly string _input;
    private int _position = 0;

    public List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();

        while (_position < input.Length)
        {
            SkipWhitespace();
            if (_position >= input.Length) break;

            char current = input[_position];

            if (char.IsLetter(current) || current == '_')
                tokens.Add(ReadKeywordOrIdentifier(input));
            else if (char.IsDigit(current))
                tokens.Add(ReadNumber(input));
            else if (current == '"' || current == '\'')
                tokens.Add(ReadString(input));
            else if (IsOperator(current))
                tokens.Add(ReadOperator(input));
            else
                throw new ParseException($"Unexpected character: {current}");
        }

        tokens.Add(new Token { Type = TokenType.EOF, Value = "" });
        return tokens;
    }

    private Token ReadKeywordOrIdentifier(string input)
    {
        var start = _position;
        while (_position < input.Length && (char.IsLetterOrDigit(input[_position]) || input[_position] == '_'))
            _position++;

        var value = input.Substring(start, _position - start);
        var type = IsKeyword(value) ? TokenType.Keyword : TokenType.Identifier;

        return new Token { Type = type, Value = value };
    }

    private bool IsKeyword(string value) =>
        value.ToUpperInvariant() is "FIND" or "WHERE" or "GROUP" or "BY" or "ORDER" or "LIMIT" or "OFFSET"
        or "FIND" or "PATH" or "FROM" or "TO" or "VIA" or "MAX" or "EXPAND" or "DEPTH" or "IN" or "RETURN";
}

internal record Token
{
    public TokenType Type { get; init; }
    public string Value { get; init; } = "";
}

internal enum TokenType
{
    Keyword,
    Identifier,
    Number,
    String,
    Operator,
    LeftParen,
    RightParen,
    Comma,
    EOF
}
```

### 6.2 Query Parser

```csharp
internal class CkvsqlParser
{
    public QueryAst Parse(List<Token> tokens)
    {
        var parser = new CkvsqlParser(tokens);
        return parser.ParseQuery();
    }

    private QueryAst ParseQuery()
    {
        ExpectKeyword("FIND");

        // Determine query type
        if (PeekKeyword("PATH"))
            return ParsePathQuery();
        else if (PeekIdentifier("e1") || PeekIdentifier("e"))
            return ParseRelationshipQuery();
        else
            return ParseEntityQuery();
    }

    private EntityQuery ParseEntityQuery()
    {
        var entityType = _tokens[_position].Value; // e.g., "Entity"
        _position++;

        var conditions = new List<Condition>();
        if (PeekKeyword("WHERE"))
        {
            _position++; // skip WHERE
            conditions.Add(ParseCondition());
        }

        var groupBy = null as string;
        if (PeekKeyword("GROUP"))
        {
            _position += 2; // skip GROUP BY
            groupBy = _tokens[_position].Value;
            _position++;
        }

        return new EntityQuery
        {
            EntityType = entityType,
            Conditions = conditions,
            GroupBy = groupBy
        };
    }

    private RelationshipQuery ParseRelationshipQuery()
    {
        var source = _tokens[_position].Value;
        _position++;

        ExpectSymbol("-[");
        var relationshipType = _tokens[_position].Value;
        _position++;
        ExpectSymbol("]->");

        var target = _tokens[_position].Value;
        _position++;

        var conditions = new List<Condition>();
        if (PeekKeyword("WHERE"))
        {
            _position++;
            conditions.Add(ParseCondition());
        }

        return new RelationshipQuery
        {
            SourceVariable = source,
            RelationshipType = relationshipType,
            TargetVariable = target,
            Conditions = conditions
        };
    }

    private PathQuery ParsePathQuery()
    {
        ExpectKeyword("PATH");
        ExpectKeyword("FROM");
        var source = _tokens[_position].Value;
        _position++;

        ExpectKeyword("TO");
        var target = _tokens[_position].Value;
        _position++;

        var relationshipTypes = new List<string>();
        if (PeekKeyword("VIA"))
        {
            _position++;
            // Parse relationship type list
        }

        var maxDepth = 10;
        if (PeekKeyword("MAX"))
        {
            _position++;
            maxDepth = int.Parse(_tokens[_position].Value);
            _position++;
        }

        return new PathQuery
        {
            SourceId = source,
            TargetId = target,
            MaxDepth = maxDepth
        };
    }

    private Condition ParseCondition()
    {
        var left = _tokens[_position].Value;
        _position++;

        var op = _tokens[_position].Value;
        _position++;

        var right = _tokens[_position].Value;
        _position++;

        return new Condition { Left = left, Operator = op, Right = right };
    }
}

internal record QueryAst;
internal record EntityQuery : QueryAst { }
internal record RelationshipQuery : QueryAst { }
internal record PathQuery : QueryAst { }
internal record Condition { }
```

### 6.3 Query Executor

```csharp
internal class CkvsqlExecutor
{
    public async Task<QueryResult> ExecuteAsync(QueryAst ast, IGraphRepository repository)
    {
        return ast switch
        {
            EntityQuery eq => await ExecuteEntityQuery(eq, repository),
            RelationshipQuery rq => await ExecuteRelationshipQuery(rq, repository),
            PathQuery pq => await ExecutePathQuery(pq, repository),
            _ => throw new InvalidOperationException("Unknown query type")
        };
    }

    private async Task<QueryResult> ExecuteEntityQuery(EntityQuery query, IGraphRepository repository)
    {
        var entities = await repository.GetEntitiesAsync();

        // Apply WHERE conditions
        var filtered = entities.Where(e => EvaluateConditions(e, query.Conditions));

        // Apply GROUP BY if present
        if (!string.IsNullOrEmpty(query.GroupBy))
        {
            var grouped = filtered.GroupBy(e => GetPropertyValue(e, query.GroupBy));
            // Convert groups to rows
        }

        var rows = filtered.Select(e => new QueryRow
        {
            Values = EntityToRow(e)
        }).ToList();

        return new QueryResult
        {
            ResultType = QueryResultType.Entities,
            Rows = rows,
            Columns = new[] { "id", "name", "type" },
            TotalCount = rows.Count,
            ExecutionTime = TimeSpan.FromMilliseconds(0)
        };
    }
}
```

---

## 7. Testing Strategy

### 7.1 Lexer/Parser Tests

```csharp
[TestClass]
public class CkvsqlLexerTests
{
    [TestMethod]
    public void Tokenize_SimpleQuery_ProducesCorrectTokens()
    {
        var lexer = new CkvsqlLexer();
        var tokens = lexer.Tokenize("FIND Entity WHERE type = \"Service\"");

        Assert.AreEqual(TokenType.Keyword, tokens[0].Type);
        Assert.AreEqual("FIND", tokens[0].Value);
    }

    [TestMethod]
    public void Tokenize_StringLiteral_PreservesQuotes()
    {
        var lexer = new CkvsqlLexer();
        var tokens = lexer.Tokenize("\"AuthService\"");

        Assert.AreEqual(TokenType.String, tokens[0].Type);
        Assert.AreEqual("AuthService", tokens[0].Value);
    }
}

[TestClass]
public class CkvsqlParserTests
{
    [TestMethod]
    public void Parse_EntityQuery_CreatesEntityQueryAst()
    {
        var parser = new CkvsqlParser();
        var ast = parser.Parse("FIND Entity WHERE type = \"Service\"");

        Assert.IsInstanceOfType(ast, typeof(EntityQuery));
        var eq = (EntityQuery)ast;
        Assert.AreEqual(1, eq.Conditions.Count);
    }

    [TestMethod]
    public void Parse_RelationshipQuery_CreatesRelationshipQueryAst()
    {
        var parser = new CkvsqlParser();
        var ast = parser.Parse("FIND e1 -[CALLS]-> e2 WHERE e1.type = \"Service\"");

        Assert.IsInstanceOfType(ast, typeof(RelationshipQuery));
    }

    [TestMethod]
    public void Parse_PathQuery_CreatesPathQueryAst()
    {
        var parser = new CkvsqlParser();
        var ast = parser.Parse("FIND PATH FROM \"UserService\" TO \"Database\" MAX 5");

        Assert.IsInstanceOfType(ast, typeof(PathQuery));
    }
}
```

### 7.2 Execution Tests

```csharp
[TestClass]
public class CkvsqlExecutorTests
{
    [TestMethod]
    public async Task Execute_EntityQuery_ReturnsFiltered Entities()
    {
        var executor = new CkvsqlExecutor();
        var repositoryMock = new Mock<IGraphRepository>();

        var entities = new[] {
            new Entity { Id = Guid.NewGuid(), Type = "Service", Name = "UserService" },
            new Entity { Id = Guid.NewGuid(), Type = "Endpoint", Name = "GET /users" }
        };

        repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var ast = new EntityQuery { Conditions = new[] { new Condition { Property = "type", Operator = "=", Value = "Service" } } };
        var result = await executor.ExecuteAsync(ast, repositoryMock.Object);

        Assert.AreEqual(1, result.Rows.Count);
    }

    [TestMethod]
    public async Task Execute_RelationshipQuery_ReturnsRelationships()
    {
        var executor = new CkvsqlExecutor();
        var repositoryMock = new Mock<IGraphRepository>();

        // Setup relationships...

        var ast = new RelationshipQuery { };
        var result = await executor.ExecuteAsync(ast, repositoryMock.Object);

        Assert.AreEqual(QueryResultType.Relationships, result.ResultType);
    }
}
```

---

## 8. Error Handling

### 8.1 Syntax Errors

**Scenario:** Invalid CKVS-QL syntax.

**Handling:**
- Lexer throws ParseException with line/column info
- ValidateAsync catches and returns QueryValidationResult
- QueryAsync propagates exception to caller

### 8.2 Semantic Errors

**Scenario:** Query refers to non-existent property or entity type.

**Handling:**
- Parser accepts syntax but executor reports error
- QueryValidationResult.Errors includes semantic issues

### 8.3 Timeout

**Scenario:** Query execution exceeds timeout.

**Handling:**
- Cancellation token enforced via QueryOptions.TimeoutMs
- Partial results returned if available

---

## 9. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Tokenize 1000-char query | <10ms | Single-pass lexer |
| Parse query | <50ms | Recursive descent parser |
| Simple entity query | <200ms | Index-based lookup |
| Complex join query | <2s | Query optimization, cost estimation |
| Aggregation query | <1s | Streaming aggregation |

---

## 10. Security & Validation

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Injection attacks | Low | Parameterized query execution, no eval() |
| ReDoS (regex explosion) | Medium | Regex timeout, complexity analysis |
| Resource exhaustion | Medium | Limit result set (default 1000), timeout |
| Invalid syntax | Low | Type-safe AST, validation layer |

---

## 11. License Gating

```csharp
public class GraphQueryServiceLicenseCheck : IGraphQueryService
{
    private readonly ILicenseContext _licenseContext;
    private readonly IGraphQueryService _inner;

    public async Task<QueryResult> QueryAsync(string query, QueryOptions options, CancellationToken ct)
    {
        if (!_licenseContext.HasFeatureEnabled(FeatureFlags.CKVS.GraphVisualization))
            throw new LicenseException("CKVS-QL requires Teams tier");

        return await _inner.QueryAsync(query, options, ct);
    }
}
```

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Simple entity query | QueryAsync called | Returns entities matching condition |
| 2 | Relationship query | QueryAsync called | Returns relationships and their endpoints |
| 3 | Path query | QueryAsync called | Returns path nodes and edges |
| 4 | Invalid syntax | ValidateAsync called | Returns QueryValidationResult with errors |
| 5 | Valid query | ValidateAsync called | Returns IsValid=true |
| 6 | Partial query | GetSuggestionsAsync called | Returns relevant suggestions |
| 7 | GROUP BY aggregation | QueryAsync called | Aggregates results by group |
| 8 | Complex query (joins) | QueryAsync called | Completes in <2 seconds |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - CKVS-QL parser, executor, suggestion engine |
