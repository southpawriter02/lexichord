# LCS-CL-063a: Detailed Changelog — Template Abstractions

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.3a                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Abstractions / Contracts                     |
| **Parent**   | [v0.6.3 Changelog](../CHANGELOG.md#v063)     |
| **Spec**     | [LCS-DES-063a](../../specs/v0.6.x/v0.6.3/LCS-DES-v0.6.3a.md) |

---

## Summary

This release establishes the core abstractions for the prompt templating system in `Lexichord.Abstractions`. It defines interfaces and immutable data records for prompt template definition, rendering, validation, and context assembly. These abstractions enable plugin implementations (Mustache, Handlebars, Liquid) while maintaining a consistent API surface across all agent implementations.

---

## New Features

### 1. IPromptTemplate Interface

Added `IPromptTemplate` interface defining the contract for reusable prompt templates:

```csharp
public interface IPromptTemplate
{
    string TemplateId { get; }
    string Name { get; }
    string Description { get; }
    string SystemPromptTemplate { get; }
    string UserPromptTemplate { get; }
    IReadOnlyList<string> RequiredVariables { get; }
    IReadOnlyList<string> OptionalVariables { get; }
}
```

**Features:**
- Kebab-case template IDs for uniqueness (e.g., "co-pilot-editor")
- System and user prompt templates with Mustache-style `{{variable}}` placeholders
- Separation of required vs optional variables
- Human-readable name and description for UI display

**File:** `src/Lexichord.Abstractions/Contracts/LLM/IPromptTemplate.cs`

### 2. IPromptRenderer Interface

Added `IPromptRenderer` interface for template rendering with variable substitution:

```csharp
public interface IPromptRenderer
{
    string Render(string template, IDictionary<string, object> variables);
    ChatMessage[] RenderMessages(IPromptTemplate template, IDictionary<string, object> variables);
    ValidationResult ValidateVariables(IPromptTemplate template, IDictionary<string, object> variables);
}
```

**Features:**
- Simple string rendering with `Render()`
- Full template rendering to `ChatMessage[]` with `RenderMessages()`
- Pre-flight validation with `ValidateVariables()`
- Thread-safe design for concurrent rendering

**File:** `src/Lexichord.Abstractions/Contracts/LLM/IPromptRenderer.cs`

### 3. IContextInjector Interface

Added `IContextInjector` interface for async context assembly:

```csharp
public interface IContextInjector
{
    Task<IDictionary<string, object>> AssembleContextAsync(
        ContextRequest request,
        CancellationToken ct = default);
}
```

**Features:**
- Assembles context from style rules, RAG context, and document state
- Returns dictionary suitable for template variable substitution
- Separation of async I/O (context assembly) from sync rendering
- Standard variable names: `style_rules`, `context`, `document_path`, `selected_text`

**File:** `src/Lexichord.Abstractions/Contracts/LLM/IContextInjector.cs`

### 4. PromptTemplate Record

Added `PromptTemplate` immutable record implementing `IPromptTemplate`:

```csharp
public record PromptTemplate(
    string TemplateId,
    string Name,
    string Description,
    string SystemPromptTemplate,
    string UserPromptTemplate,
    IReadOnlyList<string> RequiredVariables,
    IReadOnlyList<string> OptionalVariables) : IPromptTemplate
{
    public static PromptTemplate Create(...);
    public IEnumerable<string> AllVariables { get; }
    public int VariableCount { get; }
    public bool HasVariable(string variableName);
    public bool HasRequiredVariables { get; }
    public bool HasOptionalVariables { get; }
    public bool HasSystemPrompt { get; }
    public bool HasUserPrompt { get; }
    public bool HasDescription { get; }
}
```

**Features:**
- `Create()` factory method for validated construction
- `AllVariables` combines required and optional variables
- `VariableCount` returns total variable count
- `HasVariable()` case-insensitive variable lookup
- Value equality and immutability guarantees
- Property validation (TemplateId, Name cannot be null/whitespace)

**File:** `src/Lexichord.Abstractions/Contracts/LLM/PromptTemplate.cs`

### 5. RenderedPrompt Record

Added `RenderedPrompt` record containing render output:

```csharp
public record RenderedPrompt(
    string SystemPrompt,
    string UserPrompt,
    ChatMessage[] Messages,
    TimeSpan RenderDuration)
{
    public int EstimatedTokens { get; }     // (SystemPrompt + UserPrompt).Length / 4
    public int TotalCharacters { get; }
    public bool WasFastRender { get; }      // RenderDuration < 10ms
    public int MessageCount { get; }
    public bool HasSystemPrompt { get; }
    public bool HasUserPrompt { get; }
}
```

**Features:**
- Raw rendered strings for logging/debugging
- Ready-to-send `ChatMessage[]` for LLM submission
- Token estimation (4 chars ≈ 1 token approximation)
- Performance tracking via `RenderDuration` and `WasFastRender`
- Null-safe defaults for all properties

**File:** `src/Lexichord.Abstractions/Contracts/LLM/RenderedPrompt.cs`

### 6. TemplateVariable Record

Added `TemplateVariable` record for variable metadata:

```csharp
public record TemplateVariable(
    string Name,
    bool IsRequired,
    string? Description = null,
    string? DefaultValue = null)
{
    public static TemplateVariable Required(string name, string? description = null);
    public static TemplateVariable Optional(string name, string? description = null, string? defaultValue = null);
    public bool HasDefaultValue { get; }
    public bool HasDescription { get; }
}
```

**Features:**
- `Required()` factory for required variables
- `Optional()` factory for optional variables with default values
- Name validation (cannot be null/whitespace)
- Suitable for UI display and advanced validation

**File:** `src/Lexichord.Abstractions/Contracts/LLM/TemplateVariable.cs`

### 7. ValidationResult Record

Added `ValidationResult` record for validation outcomes:

```csharp
public record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> MissingVariables,
    IReadOnlyList<string> Warnings)
{
    public static ValidationResult Success();
    public static ValidationResult WithWarnings(IEnumerable<string> warnings);
    public static ValidationResult Failure(IEnumerable<string> missingVariables);
    public static ValidationResult Failure(IEnumerable<string> missingVariables, IEnumerable<string> warnings);
    public void ThrowIfInvalid(string templateId);
    public string ErrorMessage { get; }
    public bool HasWarnings { get; }
    public bool HasMissingVariables { get; }
}
```

**Features:**
- Factory methods for common validation states
- `ThrowIfInvalid()` for exception-based flow
- `ErrorMessage` formatted for display
- Argument validation on factory methods
- Null-safe defaults for collections

**File:** `src/Lexichord.Abstractions/Contracts/LLM/ValidationResult.cs`

### 8. ContextRequest Record

Added `ContextRequest` record for context assembly configuration:

```csharp
public record ContextRequest(
    string? CurrentDocumentPath,
    int? CursorPosition,
    string? SelectedText,
    bool IncludeStyleRules,
    bool IncludeRAGContext,
    int MaxRAGChunks = 3)
{
    public static ContextRequest ForUserInput(string input);
    public static ContextRequest Full(string? documentPath, string? selectedText);
    public static ContextRequest StyleOnly(string? documentPath);
    public static ContextRequest RAGOnly(string query, int maxChunks = 3);
    public bool HasContextSources { get; }
    public bool HasDocumentContext { get; }
    public bool HasSelectedText { get; }
    public bool HasCursorPosition { get; }
}
```

**Features:**
- Factory methods for common request patterns
- `MaxRAGChunks` defaults to 3, clamps non-positive values
- Computed properties for context availability checks
- Document path, cursor position, and selected text support

**File:** `src/Lexichord.Abstractions/Contracts/LLM/ContextRequest.cs`

### 9. TemplateValidationException

Added `TemplateValidationException` for validation failures:

```csharp
public class TemplateValidationException : Exception
{
    public string? TemplateId { get; }
    public IReadOnlyList<string> MissingVariables { get; }

    public TemplateValidationException();
    public TemplateValidationException(string message);
    public TemplateValidationException(string message, Exception innerException);
    public TemplateValidationException(string templateId, IReadOnlyList<string> missingVariables);
    public TemplateValidationException(string templateId, IReadOnlyList<string> missingVariables, Exception innerException);
}
```

**Features:**
- Standard exception pattern with multiple constructors
- Auto-generated message: "Template '{id}' validation failed. Missing required variables: {vars}"
- Shows "(none)" when no missing variables
- `TemplateId` and `MissingVariables` for programmatic access
- Null-safe defaults

**File:** `src/Lexichord.Abstractions/Contracts/LLM/TemplateValidationException.cs`

---

## New Files

| File Path | Description |
| --------- | ----------- |
| `src/Lexichord.Abstractions/Contracts/LLM/IPromptTemplate.cs` | Template definition interface |
| `src/Lexichord.Abstractions/Contracts/LLM/IPromptRenderer.cs` | Rendering interface |
| `src/Lexichord.Abstractions/Contracts/LLM/IContextInjector.cs` | Context assembly interface |
| `src/Lexichord.Abstractions/Contracts/LLM/PromptTemplate.cs` | Immutable template record |
| `src/Lexichord.Abstractions/Contracts/LLM/RenderedPrompt.cs` | Render output record |
| `src/Lexichord.Abstractions/Contracts/LLM/TemplateVariable.cs` | Variable metadata record |
| `src/Lexichord.Abstractions/Contracts/LLM/ValidationResult.cs` | Validation outcome record |
| `src/Lexichord.Abstractions/Contracts/LLM/ContextRequest.cs` | Context assembly request record |
| `src/Lexichord.Abstractions/Contracts/LLM/TemplateValidationException.cs` | Validation exception |

---

## Unit Tests

Added comprehensive unit tests for all template abstraction components:

| Test File | Test Count | Coverage |
| --------- | ---------- | -------- |
| `TemplateVariableTests.cs` | 15 | Constructor, factories, computed properties, validation |
| `ValidationResultTests.cs` | 18 | Success/Failure factories, ThrowIfInvalid, ErrorMessage |
| `ContextRequestTests.cs` | 20 | Factory methods, MaxRAGChunks clamping, computed properties |
| `PromptTemplateTests.cs` | 26 | Create factory, AllVariables, HasVariable, validation, equality |
| `RenderedPromptTests.cs` | 17 | EstimatedTokens, TotalCharacters, WasFastRender, null defaults |
| `TemplateValidationExceptionTests.cs` | 13 | All constructors, message format, null handling |
| **Total** | **~109** | |

Test files location: `tests/Lexichord.Tests.Unit/Abstractions/LLM/`

---

## Usage Examples

### Basic Template Creation

```csharp
var template = PromptTemplate.Create(
    templateId: "simple-assistant",
    name: "Simple Assistant",
    systemPrompt: "You are a helpful assistant.",
    userPrompt: "{{user_input}}",
    requiredVariables: new[] { "user_input" }
);
```

### Template with Optional Variables

```csharp
var template = PromptTemplate.Create(
    templateId: "context-aware",
    name: "Context-Aware Assistant",
    systemPrompt: """
        You are a writing assistant.

        {{#style_rules}}
        Follow these style guidelines:
        {{style_rules}}
        {{/style_rules}}
        """,
    userPrompt: "{{user_input}}",
    requiredVariables: new[] { "user_input" },
    optionalVariables: new[] { "style_rules" }
);
```

### Validation Before Rendering

```csharp
var variables = new Dictionary<string, object>
{
    ["user_input"] = "What is dependency injection?"
};

var validation = renderer.ValidateVariables(template, variables);
if (validation.IsValid)
{
    var messages = renderer.RenderMessages(template, variables);
}
else
{
    Console.WriteLine(validation.ErrorMessage);
    // Or: validation.ThrowIfInvalid(template.TemplateId);
}
```

### Context Assembly

```csharp
// Create a full context request
var request = ContextRequest.Full(
    documentPath: "/path/to/document.md",
    selectedText: "The quick brown fox..."
);

// Assemble context from all sources
var context = await contextInjector.AssembleContextAsync(request);

// Merge with user variables and render
context["user_input"] = "Please review this text.";
var messages = renderer.RenderMessages(template, context);
```

---

## Dependencies

### Internal Dependencies

| Interface | Version | Usage |
| --------- | ------- | ----- |
| `ChatMessage` | v0.6.1a | Output of `RenderMessages`, `RenderedPrompt.Messages` |
| `ChatRole` | v0.6.1a | Role assignment in rendered messages |

### External Dependencies

None. All types are self-contained within `Lexichord.Abstractions` with no NuGet package dependencies beyond the framework.

---

## Breaking Changes

None. This is a new feature addition with no impact on existing code.

---

## Migration Guide

No migration required. To use the new template abstractions, inject the renderer implementation (v0.6.3b) via dependency injection:

```csharp
public class MyService
{
    private readonly IPromptRenderer _renderer;

    public MyService(IPromptRenderer renderer)
    {
        _renderer = renderer;
    }

    public ChatMessage[] CreatePrompt(IPromptTemplate template, IDictionary<string, object> variables)
    {
        var validation = _renderer.ValidateVariables(template, variables);
        validation.ThrowIfInvalid(template.TemplateId);
        return _renderer.RenderMessages(template, variables);
    }
}
```

---

## Design Rationale

### Why Interfaces in Abstractions?

| Design Choice | Rationale |
| ------------- | --------- |
| Interface-first | Enables multiple implementations (Mustache, Handlebars, custom) |
| In Abstractions module | Shared across Host and all modules without circular dependencies |
| Small interface surface | 3 methods on `IPromptRenderer` keeps implementations simple |
| Records for data | Immutability prevents accidental mutation, enables value equality |

### Why Separate Validation?

Explicit `ValidateVariables` method allows:
1. Pre-flight checks before expensive LLM calls
2. UI feedback showing missing variables before submission
3. Logging of validation failures separately from render failures
4. Optional validation for callers who trust their input

### Why Context Injection Separate from Rendering?

| Reason | Explanation |
| ------ | ----------- |
| Separation of concerns | Context assembly is async I/O; rendering is synchronous |
| Flexibility | Context can be cached, modified, or augmented before rendering |
| Testability | Can mock context independently from rendering |
| Reusability | Same context can be used with different templates |
