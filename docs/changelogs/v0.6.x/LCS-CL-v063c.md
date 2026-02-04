# LCS-CL-063c: Detailed Changelog — Template Repository

## Metadata

| Field        | Value                                                        |
| :----------- | :----------------------------------------------------------- |
| **Version**  | v0.6.3c                                                      |
| **Released** | 2026-02-04                                                   |
| **Category** | Modules / Agents                                             |
| **Parent**   | [v0.6.3 Changelog](../CHANGELOG.md#v063)                     |
| **Spec**     | [LCS-DES-063c](../../specs/v0.6.x/v0.6.3/LCS-DES-v0.6.3c.md) |

---

## Summary

This release implements `IPromptTemplateRepository` for managing prompt templates from multiple sources. The repository loads built-in templates from embedded resources, supports custom templates from global and user directories (license-gated), and provides hot-reload capability via `IFileSystemWatcher` integration. Templates are loaded from YAML files using YamlDotNet.

---

## New Features

### 1. IPromptTemplateRepository Interface (Abstractions)

Added the repository interface for template management:

```csharp
public interface IPromptTemplateRepository
{
    IReadOnlyList<IPromptTemplate> GetAllTemplates();
    IPromptTemplate? GetTemplate(string templateId);
    IReadOnlyList<IPromptTemplate> GetTemplatesByCategory(string category);
    IReadOnlyList<IPromptTemplate> SearchTemplates(string query);
    Task ReloadTemplatesAsync(CancellationToken cancellationToken = default);
    TemplateInfo? GetTemplateInfo(string templateId);
    event EventHandler<TemplateChangedEventArgs>? TemplateChanged;
}
```

**Features:**
- Case-insensitive template ID lookup
- Category-based filtering
- Full-text search across name, description, ID, and tags
- Relevance-ranked search results
- Template metadata retrieval with source information
- Change notification events for UI updates

**File:** `src/Lexichord.Abstractions/Contracts/LLM/IPromptTemplateRepository.cs`

### 2. TemplateSource Enum (Abstractions)

Added priority-based source enumeration:

```csharp
public enum TemplateSource
{
    Embedded = 0,  // Lowest priority (built-in)
    Global = 1,    // Medium priority
    User = 2       // Highest priority
}
```

**Features:**
- Numeric values enable priority comparison
- User templates override global templates with same ID
- Global templates override embedded templates

**File:** `src/Lexichord.Abstractions/Contracts/LLM/TemplateSource.cs`

### 3. TemplateInfo Record (Abstractions)

Added metadata record for template information:

```csharp
public record TemplateInfo(
    string TemplateId,
    string Name,
    string? Category,
    IReadOnlyList<string> Tags,
    TemplateSource Source,
    DateTimeOffset LoadedAt,
    string? FilePath)
{
    public bool IsBuiltIn => Source == TemplateSource.Embedded;
    public bool IsCustom => Source != TemplateSource.Embedded;
    public bool HasFilePath => !string.IsNullOrEmpty(FilePath);
}
```

**File:** `src/Lexichord.Abstractions/Contracts/LLM/TemplateInfo.cs`

### 4. TemplateChangedEventArgs Class (Abstractions)

Added event args for template change notifications:

```csharp
public class TemplateChangedEventArgs : EventArgs
{
    public required string TemplateId { get; init; }
    public required TemplateChangeType ChangeType { get; init; }
    public required TemplateSource Source { get; init; }
    public IPromptTemplate? Template { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

**File:** `src/Lexichord.Abstractions/Contracts/LLM/TemplateChangedEventArgs.cs`

### 5. PromptTemplateRepository Implementation

Implemented the template repository with multi-source loading:

```csharp
public sealed class PromptTemplateRepository : IPromptTemplateRepository, IDisposable
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default);
    public IReadOnlyList<IPromptTemplate> GetAllTemplates();
    public IPromptTemplate? GetTemplate(string templateId);
    public IReadOnlyList<IPromptTemplate> GetTemplatesByCategory(string category);
    public IReadOnlyList<IPromptTemplate> SearchTemplates(string query);
    public async Task ReloadTemplatesAsync(CancellationToken cancellationToken = default);
    public TemplateInfo? GetTemplateInfo(string templateId);
    public event EventHandler<TemplateChangedEventArgs>? TemplateChanged;
}
```

**Features:**
- Thread-safe `ConcurrentDictionary` cache
- Priority-based template override (User > Global > Embedded)
- `SemaphoreSlim` for coordinated reload operations
- License gating:
  - Custom templates: `WriterPro+` or `Feature.CustomTemplates`
  - Hot-reload: `Teams+` or `Feature.HotReload`
- Hot-reload via `IFileSystemWatcher` integration
- Debounced file change processing
- Comprehensive logging

**File:** `src/Lexichord.Modules.Agents/Templates/PromptTemplateRepository.cs`

### 6. PromptTemplateLoader Class

Added YAML template loader:

```csharp
internal sealed class PromptTemplateLoader
{
    public async Task<TemplateEntry?> LoadFromFileAsync(string filePath, TemplateSource source, CancellationToken cancellationToken = default);
    public async Task<IReadOnlyList<TemplateEntry>> LoadFromDirectoryAsync(string directoryPath, TemplateSource source, IReadOnlyList<string> extensions, CancellationToken cancellationToken = default);
    public async Task<IReadOnlyList<TemplateEntry>> LoadEmbeddedTemplatesAsync(CancellationToken cancellationToken = default);
    public TemplateEntry? LoadFromYaml(string yamlContent, TemplateSource source, string? filePath = null);
}
```

**Features:**
- YAML parsing via YamlDotNet with `UnderscoredNamingConvention`
- Embedded resource loading from assembly
- Fault-tolerant design (invalid templates logged and skipped)
- Validation of required fields (template_id, name)

**File:** `src/Lexichord.Modules.Agents/Templates/PromptTemplateLoader.cs`

### 7. PromptTemplateOptions Configuration

Added configuration class for repository options:

```csharp
public class PromptTemplateOptions
{
    public const string SectionName = "Agents:TemplateRepository";
    public bool EnableBuiltInTemplates { get; set; } = true;
    public string GlobalTemplatesPath { get; set; }
    public string UserTemplatesPath { get; set; }
    public bool EnableHotReload { get; set; } = true;
    public IReadOnlyList<string> TemplateExtensions { get; set; } = [".yaml", ".yml"];
    public int FileWatcherDebounceMs { get; set; } = 200;
    public static PromptTemplateOptions Default { get; }
}
```

**Default Paths:**
- Windows Global: `C:\ProgramData\Lexichord\Templates`
- Windows User: `%APPDATA%\Lexichord\Templates`
- macOS Global: `/Library/Application Support/Lexichord/Templates`
- macOS User: `~/Library/Application Support/Lexichord/Templates`

**File:** `src/Lexichord.Modules.Agents/Templates/PromptTemplateOptions.cs`

### 8. YAML DTO Classes

Added deserialization target classes:

```csharp
internal sealed class PromptTemplateYaml
{
    [YamlMember(Alias = "template_id")] public string? TemplateId { get; set; }
    [YamlMember(Alias = "name")] public string? Name { get; set; }
    [YamlMember(Alias = "description")] public string? Description { get; set; }
    [YamlMember(Alias = "category")] public string? Category { get; set; }
    [YamlMember(Alias = "tags")] public List<string>? Tags { get; set; }
    [YamlMember(Alias = "system_prompt")] public string? SystemPrompt { get; set; }
    [YamlMember(Alias = "user_prompt")] public string? UserPrompt { get; set; }
    [YamlMember(Alias = "required_variables")] public List<string>? RequiredVariables { get; set; }
    [YamlMember(Alias = "optional_variables")] public List<string>? OptionalVariables { get; set; }
    [YamlMember(Alias = "variable_metadata")] public Dictionary<string, VariableMetadataYaml>? VariableMetadata { get; set; }
}
```

**File:** `src/Lexichord.Modules.Agents/Templates/PromptTemplateYaml.cs`

### 9. Built-in YAML Templates

Added 5 embedded prompt templates:

| Template ID | Name | Category | Required Variables | Optional Variables |
| :---------- | :--- | :------- | :----------------- | :----------------- |
| `co-pilot-editor` | Co-pilot Editor | editing | `user_input` | `style_rules`, `context` |
| `document-reviewer` | Document Reviewer | review | `document_text` | `style_rules`, `focus_areas` |
| `summarizer` | Summarizer | analysis | `source_text` | `target_length`, `summary_style` |
| `style-checker` | Style Checker | linting | `style_rules`, `text_to_check` | — |
| `translator` | Translator | translation | `target_language`, `source_text` | `preserve_formatting`, `formality`, `context` |

**Location:** `src/Lexichord.Modules.Agents/Resources/Prompts/*.yaml`

### 10. DI Service Registration

Added template repository registration:

```csharp
public static class AgentsServiceCollectionExtensions
{
    public static IServiceCollection AddTemplateRepository(this IServiceCollection services);
    public static IServiceCollection AddTemplateRepository(
        this IServiceCollection services,
        Action<PromptTemplateOptions> configureOptions);
}
```

**File:** `src/Lexichord.Modules.Agents/Extensions/AgentsServiceCollectionExtensions.cs`

---

## New Files

### Abstractions

| File Path | Description |
| :-------- | :---------- |
| `src/Lexichord.Abstractions/Contracts/LLM/TemplateSource.cs` | Enum for template source priority |
| `src/Lexichord.Abstractions/Contracts/LLM/TemplateChangeType.cs` | Enum for change types (Added, Updated, Removed) |
| `src/Lexichord.Abstractions/Contracts/LLM/TemplateChangedEventArgs.cs` | Event args for template changes |
| `src/Lexichord.Abstractions/Contracts/LLM/TemplateInfo.cs` | Template metadata record |
| `src/Lexichord.Abstractions/Contracts/LLM/IPromptTemplateRepository.cs` | Repository interface |

### Agents Module

| File Path | Description |
| :-------- | :---------- |
| `src/Lexichord.Modules.Agents/Templates/PromptTemplateYaml.cs` | YAML DTO for deserialization |
| `src/Lexichord.Modules.Agents/Templates/TemplateEntry.cs` | Internal cache entry record |
| `src/Lexichord.Modules.Agents/Templates/PromptTemplateOptions.cs` | Configuration options |
| `src/Lexichord.Modules.Agents/Templates/PromptTemplateLoader.cs` | YAML template loader |
| `src/Lexichord.Modules.Agents/Templates/PromptTemplateRepository.cs` | Repository implementation |

### Resources

| File Path | Description |
| :-------- | :---------- |
| `src/Lexichord.Modules.Agents/Resources/Prompts/co-pilot-editor.yaml` | General-purpose writing assistant template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/document-reviewer.yaml` | Document review template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/summarizer.yaml` | Text summarization template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/style-checker.yaml` | Style compliance checker template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/translator.yaml` | Translation template |

---

## Modified Files

| File Path | Changes |
| :-------- | :------ |
| `src/Lexichord.Modules.Agents/Lexichord.Modules.Agents.csproj` | Added YamlDotNet 16.3.0, EmbeddedResource glob |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Updated to register template repository |
| `src/Lexichord.Modules.Agents/Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddTemplateRepository()` methods |

---

## Unit Tests

Added comprehensive unit tests in three test files:

| Test File | Test Count | Coverage |
| :-------- | :--------- | :------- |
| `PromptTemplateLoaderTests.cs` | 20 | YAML parsing, validation, embedded resources, file loading |
| `PromptTemplateOptionsTests.cs` | 13 | Default values, path defaults, property modification |
| `PromptTemplateRepositoryTests.cs` | 41 | GetAllTemplates, GetTemplate, GetTemplatesByCategory, SearchTemplates, GetTemplateInfo, ReloadTemplates, license gating, thread safety |

**Total: 74 tests**

Test categories:
- **YAML Parsing** (5 tests): All fields, minimal, tags, multiline prompts
- **Validation** (6 tests): Missing template_id, missing name, empty content, invalid YAML
- **Source & Path** (4 tests): Source setting, embedded file path, user file path
- **Embedded Resources** (5 tests): Loads built-in templates, includes all 5 templates
- **Repository Retrieval** (18 tests): GetAllTemplates, GetTemplate, case insensitivity, category filtering
- **Search** (6 tests): By name, description, template ID, relevance ordering
- **License Gating** (4 tests): Core tier, WriterPro tier, Teams tier, hot-reload gating
- **Thread Safety** (3 tests): Concurrent GetTemplate, GetAllTemplates, SearchTemplates
- **Events & Disposal** (3 tests): TemplateChanged subscription, multiple dispose

---

## Usage Examples

### Basic Template Retrieval

```csharp
// Inject via DI
public class MyService(IPromptTemplateRepository repository, IPromptRenderer renderer)
{
    public ChatMessage[] CreatePrompt(string userInput)
    {
        var template = repository.GetTemplate("co-pilot-editor")
            ?? throw new InvalidOperationException("Template not found");

        return renderer.RenderMessages(template, new Dictionary<string, object>
        {
            ["user_input"] = userInput
        });
    }
}
```

### Search and Category Filtering

```csharp
// Get all editing templates
var editingTemplates = repository.GetTemplatesByCategory("editing");

// Search by keyword
var results = repository.SearchTemplates("writing");

// Get template metadata
var info = repository.GetTemplateInfo("co-pilot-editor");
Console.WriteLine($"Source: {info?.Source}, LoadedAt: {info?.LoadedAt}");
```

### Hot-Reload Event Handling

```csharp
repository.TemplateChanged += (sender, args) =>
{
    switch (args.ChangeType)
    {
        case TemplateChangeType.Added:
            logger.LogInformation("Template added: {Id}", args.TemplateId);
            break;
        case TemplateChangeType.Updated:
            logger.LogInformation("Template updated: {Id}", args.TemplateId);
            break;
        case TemplateChangeType.Removed:
            logger.LogWarning("Template removed: {Id}", args.TemplateId);
            break;
    }
};
```

### Custom Configuration

```csharp
services.AddTemplateRepository(options =>
{
    options.EnableHotReload = true;
    options.FileWatcherDebounceMs = 500;
    options.UserTemplatesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "LexichordTemplates");
});
```

---

## Dependencies

### Internal Dependencies

| Dependency | Version | Usage |
| :--------- | :------ | :---- |
| `Lexichord.Abstractions` | 0.6.3 | `IPromptTemplate`, `IPromptTemplateRepository`, `ILicenseContext`, `IFileSystemWatcher` |

### External Dependencies

| Package | Version | License | Usage |
| :------ | :------ | :------ | :---- |
| `YamlDotNet` | 16.3.0 | MIT | YAML template parsing |
| `Stubble.Core` | 1.10.8 | MIT | Mustache rendering (v0.6.3b) |

---

## Design Rationale

### Priority-Based Override

Templates from higher-priority sources override those with the same ID:

```
User (highest) > Global > Embedded (lowest)
```

This allows:
- Users to customize built-in templates
- Organizations to provide standard templates via global directory
- Built-in templates to serve as fallback

### License Gating Strategy

- **Core tier**: Access to 5 built-in templates (always available)
- **WriterPro tier**: Custom templates from file system
- **Teams tier**: Hot-reload for real-time template updates

### Thread Safety

- `ConcurrentDictionary` for atomic cache operations
- `SemaphoreSlim` for coordinated reload operations
- Event handlers may be invoked from background threads

### YAML Schema Design

Snake_case in YAML maps to PascalCase in C# via `UnderscoredNamingConvention`:

```yaml
template_id: "example"      # → TemplateId
system_prompt: "..."        # → SystemPrompt
required_variables: [...]   # → RequiredVariables
```

---

## Migration Notes

No breaking changes. This release adds new functionality:

1. Register the repository in DI (automatically done by `AgentsModule`)
2. Inject `IPromptTemplateRepository` where template management is needed
3. Built-in templates are automatically available

---

## Known Limitations

1. **Single Directory Watching**: `IFileSystemWatcher` currently watches only one directory (user templates). Global templates require manual reload.
2. **No Lower-Priority Restoration**: When a user template is deleted, the lower-priority version (global/embedded) is not automatically restored. A full reload is required.
3. **Synchronous Initialization Fallback**: If repository is accessed before `InitializeAsync()` completes, synchronous initialization is performed.
