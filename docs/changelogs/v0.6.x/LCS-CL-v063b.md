# LCS-CL-063b: Detailed Changelog — Mustache Renderer

## Metadata

| Field        | Value                                                        |
| :----------- | :----------------------------------------------------------- |
| **Version**  | v0.6.3b                                                      |
| **Released** | 2026-02-04                                                   |
| **Category** | Modules / Agents                                             |
| **Parent**   | [v0.6.3 Changelog](../CHANGELOG.md#v063)                     |
| **Spec**     | [LCS-DES-063b](../../specs/v0.6.x/v0.6.3/LCS-DES-v0.6.3b.md) |

---

## Summary

This release implements the Mustache-based prompt renderer in the new `Lexichord.Modules.Agents` module. The `MustachePromptRenderer` implements the `IPromptRenderer` interface (defined in v0.6.3a) using the Stubble.Core library, providing full Mustache specification support including variable substitution, sections, inverted sections, and raw output.

---

## New Features

### 1. Lexichord.Modules.Agents Module

Created a new module for AI agent orchestration capabilities:

```csharp
public class AgentsModule : IModule
{
    public ModuleInfo Info => new(
        Id: "agents",
        Name: "Agents",
        Version: new Version(0, 6, 3),
        Author: "Lexichord Team",
        Description: "AI agent orchestration with Mustache-based prompt templating");
}
```

**Features:**
- Implements `IModule` interface for automatic discovery by ModuleLoader
- Registers `IPromptRenderer` as singleton via `AddMustacheRenderer()` extension
- Module version 0.6.3 (The Template Engine)

**File:** `src/Lexichord.Modules.Agents/AgentsModule.cs`

### 2. MustachePromptRenderer Implementation

Implemented `IPromptRenderer` using Stubble.Core for Mustache templating:

```csharp
public sealed class MustachePromptRenderer : IPromptRenderer
{
    public string Render(string template, IDictionary<string, object> variables);
    public ChatMessage[] RenderMessages(IPromptTemplate template, IDictionary<string, object> variables);
    public ValidationResult ValidateVariables(IPromptTemplate template, IDictionary<string, object> variables);
}
```

**Features:**
- Full Mustache specification support:
  - Variable substitution: `{{variable}}`
  - Sections (conditional/iteration): `{{#section}}...{{/section}}`
  - Inverted sections: `{{^inverted}}...{{/inverted}}`
  - Raw/unescaped output: `{{{raw}}}` or `{{&raw}}`
  - Comments: `{{! comment }}`
  - List iteration: `{{#items}}{{.}}{{/items}}`
- Thread-safe singleton design for concurrent usage
- Configurable case-insensitive variable lookup (default: enabled)
- HTML escaping disabled by default for prompt content
- Comprehensive logging at Debug/Information levels
- Performance tracking with Stopwatch timing

**File:** `src/Lexichord.Modules.Agents/Templates/MustachePromptRenderer.cs`

### 3. MustacheRendererOptions Configuration

Added configuration record for renderer behavior:

```csharp
public record MustacheRendererOptions(
    bool IgnoreCaseOnKeyLookup = true,
    bool ThrowOnMissingVariables = true,
    int FastRenderThresholdMs = 10)
{
    public const string SectionName = "Agents:MustacheRenderer";
    public static MustacheRendererOptions Default { get; }
    public static MustacheRendererOptions Strict { get; }
    public static MustacheRendererOptions Lenient { get; }
}
```

**Presets:**
- `Default`: Case-insensitive lookup, throws on missing variables, 10ms fast threshold
- `Strict`: Case-sensitive lookup, throws on missing variables
- `Lenient`: Case-insensitive lookup, does not throw on missing variables

**File:** `src/Lexichord.Modules.Agents/Templates/MustacheRendererOptions.cs`

### 4. DI Service Registration Extensions

Added extension methods for service registration:

```csharp
public static class AgentsServiceCollectionExtensions
{
    public static IServiceCollection AddMustacheRenderer(this IServiceCollection services);
    public static IServiceCollection AddMustacheRenderer(
        this IServiceCollection services,
        Action<MustacheRendererOptions> configureOptions);
}
```

**Features:**
- Registers `MustacheRendererOptions` via IOptions pattern
- Registers `MustachePromptRenderer` as singleton implementing `IPromptRenderer`
- Overload for custom options configuration

**File:** `src/Lexichord.Modules.Agents/Extensions/AgentsServiceCollectionExtensions.cs`

---

## New Files

| File Path | Description |
| --------- | ----------- |
| `src/Lexichord.Modules.Agents/Lexichord.Modules.Agents.csproj` | Module project file with Stubble.Core 1.10.8 reference |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | IModule implementation for service registration |
| `src/Lexichord.Modules.Agents/Templates/MustachePromptRenderer.cs` | IPromptRenderer implementation |
| `src/Lexichord.Modules.Agents/Templates/MustacheRendererOptions.cs` | Configuration options record |
| `src/Lexichord.Modules.Agents/Extensions/AgentsServiceCollectionExtensions.cs` | DI registration extensions |

---

## Unit Tests

Added comprehensive unit tests covering all renderer functionality:

| Test File | Test Count | Coverage |
| --------- | ---------- | -------- |
| `MustachePromptRendererTests.cs` | 39 | Variable substitution, sections, inverted sections, raw output, case sensitivity, RenderMessages, ValidateVariables, edge cases, options presets |

Test categories:
- **Variable Substitution** (5 tests): Simple, multiple, missing, null, numeric
- **Section Rendering** (5 tests): Truthy, falsy, empty list, populated list, nested
- **Inverted Sections** (3 tests): Falsy renders, truthy hides, empty list renders
- **Raw Output** (2 tests): Triple mustache, ampersand syntax
- **Case Sensitivity** (3 tests): Case-insensitive, case-sensitive, validation
- **RenderMessages** (5 tests): Both prompts, user only, empty system, missing required throws, trims whitespace
- **ValidateVariables** (6 tests): All required, missing, null value, empty value, whitespace value, unused warning
- **Edge Cases** (4 tests): Null template, null variables, empty template, no placeholders
- **Advanced** (3 tests): Nested objects, newlines preservation
- **Options Presets** (4 tests): Default, Strict, Lenient values, lenient behavior

Test files location: `tests/Lexichord.Tests.Unit/Modules/Agents/Templates/`

---

## Usage Examples

### Basic Rendering

```csharp
var renderer = serviceProvider.GetRequiredService<IPromptRenderer>();

var result = renderer.Render(
    "Hello, {{name}}! You have {{count}} messages.",
    new Dictionary<string, object>
    {
        ["name"] = "Alice",
        ["count"] = 5
    });
// Result: "Hello, Alice! You have 5 messages."
```

### Template Rendering with Validation

```csharp
var template = PromptTemplate.Create(
    templateId: "assistant",
    name: "Assistant",
    systemPrompt: "You are a helpful assistant.{{#style_rules}}\n\nFollow these rules:\n{{style_rules}}{{/style_rules}}",
    userPrompt: "{{user_input}}",
    requiredVariables: new[] { "user_input" },
    optionalVariables: new[] { "style_rules" }
);

var variables = new Dictionary<string, object>
{
    ["user_input"] = "What is dependency injection?",
    ["style_rules"] = "- Be concise\n- Use examples"
};

// Option 1: Validate first
var validation = renderer.ValidateVariables(template, variables);
if (validation.IsValid)
{
    var messages = renderer.RenderMessages(template, variables);
    // Send messages to LLM...
}

// Option 2: Let RenderMessages throw on validation failure
try
{
    var messages = renderer.RenderMessages(template, variables);
}
catch (TemplateValidationException ex)
{
    Console.WriteLine($"Missing variables: {string.Join(", ", ex.MissingVariables)}");
}
```

### Conditional Sections

```csharp
var template = @"
{{#include_context}}
Context from your documents:
{{context}}
{{/include_context}}
{{^include_context}}
No context available.
{{/include_context}}

User question: {{question}}";

var result = renderer.Render(template, new Dictionary<string, object>
{
    ["include_context"] = true,
    ["context"] = "Relevant information from RAG...",
    ["question"] = "What is the answer?"
});
```

---

## Dependencies

### Internal Dependencies

| Interface | Version | Usage |
| --------- | ------- | ----- |
| `IPromptRenderer` | v0.6.3a | Interface being implemented |
| `IPromptTemplate` | v0.6.3a | Input for `RenderMessages` and `ValidateVariables` |
| `ValidationResult` | v0.6.3a | Return type for `ValidateVariables` |
| `TemplateValidationException` | v0.6.3a | Thrown when validation fails |
| `ChatMessage` | v0.6.1a | Element of `ChatMessage[]` return type |
| `ChatRole` | v0.6.1a | Role assignment in rendered messages |

### External Dependencies

| Package | Version | Purpose |
| ------- | ------- | ------- |
| `Stubble.Core` | 1.10.8 | Mustache template rendering engine |

---

## Breaking Changes

None. This is a new feature addition with no impact on existing code.

---

## Migration Guide

No migration required. To use the new renderer:

1. The Agents module is automatically loaded by the ModuleLoader
2. Resolve `IPromptRenderer` via dependency injection:

```csharp
public class MyAgent
{
    private readonly IPromptRenderer _renderer;

    public MyAgent(IPromptRenderer renderer)
    {
        _renderer = renderer;
    }

    public ChatMessage[] CreatePrompt(IPromptTemplate template, IDictionary<string, object> variables)
    {
        return _renderer.RenderMessages(template, variables);
    }
}
```

---

## Design Rationale

### Why Stubble.Core?

| Alternative | Rejected Because |
| ----------- | ---------------- |
| `String.Replace` | No support for sections, escaping, or iteration |
| `Handlebars.Net` | Heavier dependency, more features than needed |
| `Liquid` | More complex, designed for end-user templates |
| `Razor` | Full view engine, extreme overkill for prompts |
| Custom parser | High maintenance, likely bugs vs. proven library |

Stubble.Core provides:
- Full Mustache specification compliance
- Minimal dependencies
- Thread-safe rendering
- High performance with compiled template caching
- Configurable options for case sensitivity and missing key behavior

### Why Singleton Lifetime?

The renderer is registered as singleton because:
1. The underlying Stubble renderer is thread-safe
2. Configuration is immutable after construction
3. No per-request state is maintained
4. Avoids repeated builder construction overhead

### Why Disable HTML Escaping?

Prompts are sent to LLMs as plain text, not rendered in browsers. HTML escaping would:
- Corrupt special characters in user content
- Make prompts harder to read and debug
- Serve no security purpose in this context

---

## Performance Characteristics

| Operation | Template Size | Variables | Expected Time |
| --------- | ------------- | --------- | ------------- |
| Simple render | 100 chars | 2 | < 1ms |
| Medium render | 1 KB | 5 | < 2ms |
| Complex render | 5 KB | 10 | < 5ms |
| Full RenderMessages | 2 KB | 8 | < 10ms |
| Validation only | — | 10 | < 0.5ms |

The `FastRenderThresholdMs` option (default: 10ms) can be used to track fast/slow renders.

---

## Verification Commands

```bash
# Build the Agents module
dotnet build src/Lexichord.Modules.Agents

# Run all MustachePromptRenderer tests
dotnet test --filter "FullyQualifiedName~MustachePromptRenderer"

# Run v0.6.3b tests by trait
dotnet test --filter "Version=v0.6.3b"
```
