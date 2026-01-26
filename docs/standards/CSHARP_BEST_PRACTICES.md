# Lexichord C# Best Practices & Coding Standards

**Version:** 1.0.0
**Last Updated:** 2026-01-26
**Framework:** .NET 9 / C# 13

---

## 1. Project Structure

### 1.1 Solution Organization

```
Lexichord/
├── src/
│   ├── Lexichord.Abstractions/     # Tier 0: Interfaces, DTOs, Attributes
│   │   ├── Contracts/              # Service interfaces
│   │   ├── Events/                 # MediatR events
│   │   ├── Models/                 # Shared DTOs and records
│   │   └── Attributes/             # Custom attributes
│   │
│   ├── Lexichord.Host/             # Tier 1: Application shell
│   │   ├── App.axaml               # Avalonia application
│   │   ├── Program.cs              # Entry point
│   │   ├── Services/               # Core services
│   │   ├── ViewModels/             # MVVM ViewModels
│   │   └── Views/                  # AXAML views
│   │
│   └── Lexichord.Modules/          # Tier 2: Feature modules
│       ├── Lexichord.Modules.Style/
│       ├── Lexichord.Modules.Memory/
│       └── Lexichord.Modules.Agents/
│
├── tests/
│   ├── Lexichord.Tests.Unit/
│   ├── Lexichord.Tests.Integration/
│   └── Lexichord.Tests.Architecture/
│
└── docs/
```

### 1.2 Namespace Conventions

```csharp
// Root namespace follows folder structure
namespace Lexichord.Abstractions.Contracts;
namespace Lexichord.Host.Services;
namespace Lexichord.Modules.Style.Services;

// Test namespaces mirror source
namespace Lexichord.Tests.Unit.Host.Services;
```

### 1.3 File Naming

| Type | Pattern | Example |
|------|---------|---------|
| Interface | `I{Name}.cs` | `IStyleEngine.cs` |
| Class | `{Name}.cs` | `StyleEngine.cs` |
| Record | `{Name}.cs` | `StyleViolation.cs` |
| ViewModel | `{Name}ViewModel.cs` | `EditorViewModel.cs` |
| View | `{Name}View.axaml` | `EditorView.axaml` |
| Test | `{Name}Tests.cs` | `StyleEngineTests.cs` |

---

## 2. C# Language Features

### 2.1 Modern C# Idioms (Required)

```csharp
// ✅ USE: Primary Constructors for DI
public class TuningService(
    ILogger<TuningService> logger,
    IStyleRepository repository,
    IEventBus eventBus)
{
    public async Task<TuningResult> AnalyzeAsync(Document document)
    {
        logger.LogInformation("Analyzing document {Id}", document.Id);
        // ...
    }
}

// ❌ AVOID: Traditional constructor injection (unless necessary)
public class TuningService
{
    private readonly ILogger<TuningService> _logger;

    public TuningService(ILogger<TuningService> logger)
    {
        _logger = logger;
    }
}
```

### 2.2 Records for DTOs

```csharp
// ✅ USE: Records for immutable data transfer
public record StyleViolation(
    int Line,
    int Column,
    string Word,
    string Suggestion,
    ViolationType Type,
    string RuleId
);

public record TuningResult(
    int Score,
    IReadOnlyList<StyleViolation> Violations,
    TimeSpan AnalysisDuration
);

// ✅ USE: Records with init for optional mutation
public record DocumentMetadata
{
    public required string Title { get; init; }
    public string? Author { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// ❌ AVOID: Classes for pure data transfer
public class StyleViolationDto  // Don't do this
{
    public int Line { get; set; }
    // ...
}
```

### 2.3 Pattern Matching

```csharp
// ✅ USE: Pattern matching for type checks
public async Task HandleEventAsync(IEvent @event)
{
    var result = @event switch
    {
        DocumentCreatedEvent created => await HandleCreated(created),
        DocumentUpdatedEvent updated => await HandleUpdated(updated),
        StyleViolationDetectedEvent violation when violation.Severity == Severity.Error
            => await HandleCriticalViolation(violation),
        _ => EventResult.Ignored
    };
}

// ✅ USE: Property patterns
if (document is { IsReadOnly: false, Content.Length: > 0 })
{
    await AnalyzeAsync(document);
}

// ✅ USE: List patterns (C# 11+)
public bool IsValidHeader(string[] lines) => lines switch
{
    ["#", .. var rest] => true,
    ["##", .. var rest] when rest.Length > 0 => true,
    _ => false
};
```

### 2.4 Nullable Reference Types

```csharp
// ✅ ALWAYS: Enable nullable reference types
#nullable enable

public class DocumentService
{
    // Non-nullable: Must be provided
    public async Task<Document> GetAsync(Guid id)
    {
        var document = await _repository.FindAsync(id);
        return document ?? throw new DocumentNotFoundException(id);
    }

    // Nullable: May return null
    public async Task<Document?> TryGetAsync(Guid id)
    {
        return await _repository.FindAsync(id);
    }

    // Required properties
    public required string Name { get; init; }

    // Optional properties
    public string? Description { get; init; }
}
```

### 2.5 Collection Expressions (C# 12+)

```csharp
// ✅ USE: Collection expressions
List<string> terms = ["allowlist", "blocklist", "primary", "replica"];
int[] counts = [1, 2, 3, 4, 5];
Dictionary<string, int> scores = new() { ["doc1"] = 95, ["doc2"] = 87 };

// ✅ USE: Spread operator
string[] allTerms = [..forbiddenTerms, ..deprecatedTerms];

// ❌ AVOID: Old syntax
var terms = new List<string> { "allowlist", "blocklist" };
```

---

## 3. Async/Await Patterns

### 3.1 General Rules

```csharp
// ✅ ALWAYS: Use async/await for I/O operations
public async Task<Document> LoadDocumentAsync(string path)
{
    var content = await File.ReadAllTextAsync(path);
    return Document.Parse(content);
}

// ❌ NEVER: Use .Result or .Wait()
public Document LoadDocument(string path)
{
    var content = File.ReadAllTextAsync(path).Result; // FORBIDDEN
    return Document.Parse(content);
}

// ❌ NEVER: Use async void (except event handlers)
public async void ProcessDocument() // FORBIDDEN
{
    await AnalyzeAsync();
}

// ✅ CORRECT: Return Task for async methods
public async Task ProcessDocumentAsync()
{
    await AnalyzeAsync();
}
```

### 3.2 Cancellation Tokens

```csharp
// ✅ ALWAYS: Accept CancellationToken for long-running operations
public async Task<AnalysisResult> AnalyzeAsync(
    Document document,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    foreach (var paragraph in document.Paragraphs)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ProcessParagraphAsync(paragraph, cancellationToken);
    }

    return result;
}

// ✅ USE: ConfigureAwait(false) in library code
public async Task<string> FetchAsync(string url, CancellationToken ct)
{
    using var client = new HttpClient();
    return await client.GetStringAsync(url, ct).ConfigureAwait(false);
}
```

### 3.3 Parallel Processing

```csharp
// ✅ USE: Parallel.ForEachAsync for CPU-bound parallel work
await Parallel.ForEachAsync(
    documents,
    new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
    async (doc, token) =>
    {
        await ProcessAsync(doc, token);
    });

// ✅ USE: Task.WhenAll for I/O-bound parallel work
var tasks = documents.Select(d => AnalyzeAsync(d, ct));
var results = await Task.WhenAll(tasks);

// ✅ USE: Channel for producer/consumer patterns
var channel = Channel.CreateBounded<Document>(100);

// Producer
await channel.Writer.WriteAsync(document, ct);

// Consumer
await foreach (var doc in channel.Reader.ReadAllAsync(ct))
{
    await ProcessAsync(doc, ct);
}
```

---

## 4. Dependency Injection

### 4.1 Service Registration

```csharp
// ✅ USE: Extension methods for module registration
public static class StyleModuleExtensions
{
    public static IServiceCollection AddStyleModule(
        this IServiceCollection services,
        Action<StyleOptions>? configure = null)
    {
        var options = new StyleOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IStyleEngine, StyleEngine>();
        services.AddScoped<ILinterService, LinterService>();
        services.AddTransient<StyleViolationValidator>();

        return services;
    }
}

// Usage in Program.cs
services.AddStyleModule(options =>
{
    options.MaxLineLength = 100;
    options.EnableFuzzyMatching = true;
});
```

### 4.2 Service Lifetimes

| Lifetime | Use Case | Example |
|----------|----------|---------|
| **Singleton** | Stateless services, caches, options | `IStyleEngine`, `IThemeManager` |
| **Scoped** | Per-request/per-document state | `IDocumentContext`, `IUnitOfWork` |
| **Transient** | Lightweight, stateless utilities | Validators, Factories |

```csharp
// ✅ CORRECT: Singleton for configuration
services.AddSingleton<IConfiguration>(configuration);

// ✅ CORRECT: Scoped for database contexts
services.AddScoped<IDbContext, AppDbContext>();

// ✅ CORRECT: Transient for factories
services.AddTransient<IDocumentFactory, DocumentFactory>();
```

### 4.3 Keyed Services (.NET 8+)

```csharp
// ✅ USE: Keyed services for multiple implementations
services.AddKeyedSingleton<ILlmProvider, OpenAiProvider>("openai");
services.AddKeyedSingleton<ILlmProvider, AnthropicProvider>("anthropic");
services.AddKeyedSingleton<ILlmProvider, OllamaProvider>("ollama");

// Injection
public class AgentService(
    [FromKeyedServices("openai")] ILlmProvider openAi,
    [FromKeyedServices("anthropic")] ILlmProvider anthropic)
{
    // ...
}
```

---

## 5. Error Handling

### 5.1 Exception Strategy

```csharp
// ✅ USE: Custom exceptions for domain errors
public class DocumentNotFoundException : Exception
{
    public Guid DocumentId { get; }

    public DocumentNotFoundException(Guid documentId)
        : base($"Document with ID '{documentId}' was not found.")
    {
        DocumentId = documentId;
    }
}

public class StyleViolationException : Exception
{
    public IReadOnlyList<StyleViolation> Violations { get; }

    public StyleViolationException(IReadOnlyList<StyleViolation> violations)
        : base($"Document contains {violations.Count} style violations.")
    {
        Violations = violations;
    }
}
```

### 5.2 Result Pattern

```csharp
// ✅ USE: Result types for expected failures
public record Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new() { Value = value };
    public static Result<T> Failure(string error) => new() { Error = error };
}

// Usage
public async Task<Result<Document>> TryLoadAsync(string path)
{
    if (!File.Exists(path))
        return Result<Document>.Failure($"File not found: {path}");

    try
    {
        var content = await File.ReadAllTextAsync(path);
        return Result<Document>.Success(Document.Parse(content));
    }
    catch (Exception ex)
    {
        return Result<Document>.Failure(ex.Message);
    }
}
```

### 5.3 Exception Handling

```csharp
// ✅ USE: Specific catch blocks
try
{
    await ProcessDocumentAsync(document);
}
catch (DocumentNotFoundException ex)
{
    logger.LogWarning("Document {Id} not found", ex.DocumentId);
    return NotFound();
}
catch (StyleViolationException ex) when (ex.Violations.Any(v => v.Type == ViolationType.Error))
{
    logger.LogError("Critical style violations in document");
    return BadRequest(ex.Violations);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Operation was cancelled");
    throw;
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error processing document");
    throw;
}
```

---

## 6. Logging with Serilog

### 6.1 Configuration

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/lexichord-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}")
    .CreateLogger();
```

### 6.2 Structured Logging

```csharp
// ✅ USE: Structured logging with named parameters
logger.LogInformation(
    "Document {DocumentId} analyzed in {Duration}ms with score {Score}",
    document.Id,
    stopwatch.ElapsedMilliseconds,
    result.Score);

// ✅ USE: Log scopes for context
using (logger.BeginScope(new Dictionary<string, object>
{
    ["DocumentId"] = document.Id,
    ["UserId"] = currentUser.Id
}))
{
    logger.LogInformation("Starting analysis");
    await AnalyzeAsync(document);
    logger.LogInformation("Analysis complete");
}

// ❌ AVOID: String interpolation in log messages
logger.LogInformation($"Document {document.Id} analyzed"); // WRONG
```

### 6.3 Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| **Verbose** | Extremely detailed debugging | Variable values in loops |
| **Debug** | Developer diagnostics | Method entry/exit |
| **Information** | Normal application flow | User actions, requests |
| **Warning** | Unexpected but recoverable | Missing optional config |
| **Error** | Failures that don't crash | API call failed |
| **Fatal** | Application crash | Unhandled exception |

```csharp
// Standard log pattern
logger.LogDebug("Entering {Method} with {Parameters}", nameof(AnalyzeAsync), parameters);
logger.LogInformation("User {UserId} opened document {DocumentId}", userId, documentId);
logger.LogWarning("Rate limit approaching for API {ApiName}: {CurrentRate}/{MaxRate}", api, current, max);
logger.LogError(ex, "Failed to save document {DocumentId}", documentId);
logger.LogCritical(ex, "Application startup failed");
```

---

## 7. Testing with NUnit

### 7.1 Test Structure

```csharp
[TestFixture]
public class StyleEngineTests
{
    private IStyleEngine _sut = null!; // System Under Test
    private Mock<IStyleRepository> _mockRepo = null!;
    private Mock<ILogger<StyleEngine>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IStyleRepository>();
        _mockLogger = new Mock<ILogger<StyleEngine>>();
        _sut = new StyleEngine(_mockLogger.Object, _mockRepo.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup if needed
    }
}
```

### 7.2 Test Naming Convention

```csharp
// Pattern: {Method}_{Scenario}_{ExpectedBehavior}

[Test]
public async Task AnalyzeAsync_WithForbiddenTerm_ReturnsViolation()
{
    // Arrange
    var document = new Document("The whitelist contains trusted IPs.");

    // Act
    var result = await _sut.AnalyzeAsync(document);

    // Assert
    Assert.That(result.Violations, Has.Count.EqualTo(1));
    Assert.That(result.Violations[0].Word, Is.EqualTo("whitelist"));
}

[Test]
public async Task AnalyzeAsync_WithEmptyDocument_ReturnsEmptyViolations()
{
    // Arrange
    var document = Document.Empty;

    // Act
    var result = await _sut.AnalyzeAsync(document);

    // Assert
    Assert.That(result.Violations, Is.Empty);
    Assert.That(result.Score, Is.EqualTo(100));
}
```

### 7.3 Test Categories

```csharp
[TestFixture]
[Category("Unit")]
public class StyleEngineTests { }

[TestFixture]
[Category("Integration")]
[Category("Database")]
public class StyleRepositoryIntegrationTests { }

[TestFixture]
[Category("Architecture")]
public class DependencyTests { }
```

### 7.4 Parameterized Tests

```csharp
[TestCase("whitelist", "allowlist", ViolationType.Error)]
[TestCase("blacklist", "blocklist", ViolationType.Error)]
[TestCase("master", "primary", ViolationType.Warning)]
[TestCase("slave", "replica", ViolationType.Warning)]
public async Task AnalyzeAsync_WithProblematicTerm_ReturnsSuggestion(
    string input,
    string expected,
    ViolationType expectedType)
{
    // Arrange
    var document = new Document($"Configure the {input} for this feature.");

    // Act
    var result = await _sut.AnalyzeAsync(document);

    // Assert
    Assert.That(result.Violations, Has.Count.EqualTo(1));
    Assert.That(result.Violations[0].Suggestion, Is.EqualTo(expected));
    Assert.That(result.Violations[0].Type, Is.EqualTo(expectedType));
}
```

### 7.5 Async Testing

```csharp
[Test]
public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceled()
{
    // Arrange
    using var cts = new CancellationTokenSource();
    var longRunningDocument = Document.Generate(10000);

    // Act
    var task = _sut.ProcessAsync(longRunningDocument, cts.Token);
    cts.Cancel();

    // Assert
    Assert.ThrowsAsync<OperationCanceledException>(() => task);
}

[Test]
public async Task AnalyzeAsync_WithTimeout_CompletesWithinLimit()
{
    // Arrange
    var document = Document.Generate(100);

    // Act & Assert
    Assert.That(
        async () => await _sut.AnalyzeAsync(document),
        Is.Completed.After(1000).MilliSeconds);
}
```

---

## 8. Documentation

### 8.1 XML Documentation

```csharp
/// <summary>
/// Analyzes a document for style violations according to the configured ruleset.
/// </summary>
/// <remarks>
/// This method performs both lexical analysis (terminology checks) and
/// semantic analysis (readability metrics). Results are cached for performance.
/// </remarks>
/// <param name="document">The document to analyze. Must not be null.</param>
/// <param name="options">Optional analysis options. Uses defaults if null.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="TuningResult"/> containing the style score and any violations found.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when document is null.</exception>
/// <exception cref="StyleAnalysisException">Thrown when analysis fails unexpectedly.</exception>
/// <example>
/// <code>
/// var result = await styleEngine.AnalyzeAsync(document);
/// if (result.Score &lt; 80)
/// {
///     foreach (var violation in result.Violations)
///     {
///         Console.WriteLine($"Line {violation.Line}: {violation.Word} → {violation.Suggestion}");
///     }
/// }
/// </code>
/// </example>
public async Task<TuningResult> AnalyzeAsync(
    Document document,
    AnalysisOptions? options = null,
    CancellationToken cancellationToken = default)
```

### 8.2 Code Comments

```csharp
public async Task<int> CalculateScoreAsync(Document document)
{
    // LOGIC: Score starts at 100 and is reduced by violations
    // - Error violations: -5 points each
    // - Warning violations: -2 points each
    // - Info violations: -1 point each
    // Minimum score is 0, maximum is 100

    var baseScore = 100;
    var violations = await DetectViolationsAsync(document);

    foreach (var violation in violations)
    {
        baseScore -= violation.Type switch
        {
            ViolationType.Error => 5,
            ViolationType.Warning => 2,
            ViolationType.Info => 1,
            _ => 0
        };
    }

    // TODO: Add bonus points for consistent voice metrics
    // See issue #123 for implementation details

    return Math.Max(0, Math.Min(100, baseScore));
}
```

---

## 9. Naming Conventions (The Lexichord Aesthetic)

### 9.1 Musical/Orchestral Names

Where appropriate, use musical terminology that aligns with Lexichord's metaphor:

| Concept | Musical Name | Avoid |
|---------|--------------|-------|
| Style engine | `TuningEngine` | `StyleChecker` |
| AI agents | `Ensemble` | `AgentManager` |
| Document analysis | `Resonance` | `Analysis` |
| RAG/Memory | `Score` | `KnowledgeBase` |
| Main app | `Podium` | `Shell` |
| Event bus | `Conductor` | `MessageBus` |
| Plugins | `Sections` | `Modules` |

### 9.2 Standard .NET Names

```csharp
// Interfaces: I-prefix
public interface IStyleEngine { }
public interface ITuningService { }

// Abstract classes: Base suffix
public abstract class AgentBase { }
public abstract class ModuleBase { }

// Async methods: Async suffix
public Task<Result> AnalyzeAsync();
public Task ProcessDocumentAsync();

// Event handlers: On prefix
private void OnDocumentChanged(object sender, EventArgs e);
private async Task OnStyleViolationDetectedAsync(StyleViolationEvent e);

// Boolean properties/methods: Is/Has/Can prefix
public bool IsReadOnly { get; }
public bool HasViolations { get; }
public bool CanEdit { get; }
```

---

## 10. Architecture Rules

### 10.1 Dependency Enforcement

```csharp
// In Lexichord.Tests.Architecture
[TestFixture]
[Category("Architecture")]
public class DependencyTests
{
    [Test]
    public void Abstractions_ShouldNotReference_Host()
    {
        var assembly = typeof(IModule).Assembly;
        var references = assembly.GetReferencedAssemblies();

        Assert.That(references.Select(r => r.Name),
            Does.Not.Contain("Lexichord.Host"));
    }

    [Test]
    public void Host_ShouldNotReference_Modules()
    {
        var assembly = typeof(App).Assembly;
        var references = assembly.GetReferencedAssemblies();

        Assert.That(references.Select(r => r.Name),
            Has.None.StartWith("Lexichord.Modules"));
    }

    [Test]
    public void Modules_ShouldNotReference_EachOther()
    {
        var moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Lexichord.Modules") == true);

        foreach (var assembly in moduleAssemblies)
        {
            var references = assembly.GetReferencedAssemblies()
                .Where(r => r.Name?.StartsWith("Lexichord.Modules") == true)
                .Where(r => r.Name != assembly.GetName().Name);

            Assert.That(references, Is.Empty,
                $"{assembly.GetName().Name} should not reference other modules");
        }
    }
}
```

### 10.2 License Attribute

```csharp
// In Lexichord.Abstractions
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequiresLicenseAttribute(LicenseTier tier) : Attribute
{
    public LicenseTier Tier { get; } = tier;
}

public enum LicenseTier
{
    Core = 0,
    WriterPro = 1,
    Teams = 2,
    Enterprise = 3
}

// Usage in Modules
[RequiresLicense(LicenseTier.Teams)]
public class ReleaseNotesAgent : IAgent
{
    // ...
}
```

---

## Appendix: Quick Reference

```
╔═══════════════════════════════════════════════════════════════╗
║  LEXICHORD C# QUICK REFERENCE                                 ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  REQUIRED FEATURES                                            ║
║  ✓ Primary constructors for DI                                ║
║  ✓ Records for DTOs                                           ║
║  ✓ Nullable reference types                                   ║
║  ✓ Collection expressions                                     ║
║  ✓ Pattern matching                                           ║
║  ✓ Async/await for all I/O                                    ║
║                                                               ║
║  FORBIDDEN PATTERNS                                           ║
║  ✗ .Result or .Wait() on Tasks                               ║
║  ✗ async void (except UI event handlers)                      ║
║  ✗ String interpolation in log messages                       ║
║  ✗ Cross-module references                                    ║
║  ✗ Host → Module dependencies                                 ║
║                                                               ║
║  NAMING                                                       ║
║  Interfaces:     I{Name}              IStyleEngine            ║
║  Async methods:  {Name}Async          AnalyzeAsync            ║
║  Booleans:       Is/Has/Can{Name}     IsReadOnly              ║
║  Events:         On{Name}             OnDocumentChanged       ║
║                                                               ║
║  SERVICE LIFETIMES                                            ║
║  Singleton:  Stateless, config, caches                        ║
║  Scoped:     Per-request, DbContext                           ║
║  Transient:  Factories, validators                            ║
║                                                               ║
║  LOG LEVELS                                                   ║
║  Debug:      Developer diagnostics                            ║
║  Info:       Normal flow, user actions                        ║
║  Warning:    Recoverable issues                               ║
║  Error:      Failures (non-crash)                             ║
║  Fatal:      Application crash                                ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```
