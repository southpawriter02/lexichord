# Lexichord NUnit Testing Guidelines

**Version:** 1.0.0
**Last Updated:** 2026-01-26
**Framework:** NUnit 4.x / .NET 9

---

## 1. Project Structure

### 1.1 Test Project Organization

```
tests/
├── Lexichord.Tests.Unit/
│   ├── Lexichord.Tests.Unit.csproj
│   ├── GlobalUsings.cs
│   ├── TestBase.cs
│   │
│   ├── Host/
│   │   ├── Services/
│   │   │   ├── ThemeServiceTests.cs
│   │   │   └── ModuleLoaderTests.cs
│   │   └── ViewModels/
│   │       ├── MainWindowViewModelTests.cs
│   │       └── EditorViewModelTests.cs
│   │
│   └── Modules/
│       ├── Style/
│       │   ├── StyleEngineTests.cs
│       │   └── LinterServiceTests.cs
│       └── Memory/
│           └── EmbeddingServiceTests.cs
│
├── Lexichord.Tests.Integration/
│   ├── Lexichord.Tests.Integration.csproj
│   ├── DatabaseFixture.cs
│   │
│   ├── Repositories/
│   │   └── StyleRepositoryTests.cs
│   └── Services/
│       └── RagPipelineTests.cs
│
└── Lexichord.Tests.Architecture/
    ├── Lexichord.Tests.Architecture.csproj
    └── DependencyTests.cs
```

### 1.2 Project File

```xml
<!-- Lexichord.Tests.Unit.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.*" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.*" />
    <PackageReference Include="NUnit.Analyzers" Version="4.*">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="7.*" />
    <PackageReference Include="AutoFixture" Version="4.*" />
    <PackageReference Include="AutoFixture.NUnit3" Version="4.*" />
    <PackageReference Include="Bogus" Version="35.*" />
    <PackageReference Include="coverlet.collector" Version="6.*">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Lexichord.Host\Lexichord.Host.csproj" />
    <ProjectReference Include="..\..\src\Lexichord.Modules.Style\Lexichord.Modules.Style.csproj" />
  </ItemGroup>

</Project>
```

### 1.3 Global Usings

```csharp
// GlobalUsings.cs
global using NUnit.Framework;
global using Moq;
global using FluentAssertions;
global using AutoFixture;
global using AutoFixture.NUnit3;

global using Lexichord.Abstractions.Contracts;
global using Lexichord.Abstractions.Models;
global using Lexichord.Host.Services;
```

---

## 2. Test Naming Conventions

### 2.1 Test Class Names

```csharp
// Pattern: {ClassUnderTest}Tests

[TestFixture]
public class StyleEngineTests { }

[TestFixture]
public class EditorViewModelTests { }

[TestFixture]
public class DocumentRepositoryTests { }
```

### 2.2 Test Method Names

```csharp
// Pattern: {Method}_{Scenario}_{ExpectedBehavior}

[Test]
public async Task AnalyzeAsync_WithValidDocument_ReturnsScore()

[Test]
public async Task AnalyzeAsync_WithEmptyDocument_ReturnsZeroViolations()

[Test]
public async Task AnalyzeAsync_WithForbiddenTerm_ReturnsViolation()

[Test]
public void Constructor_WithNullLogger_ThrowsArgumentNullException()

[Test]
public async Task SaveAsync_WhenCancelled_ThrowsOperationCanceledException()
```

### 2.3 Test Categories

```csharp
// Standard categories
[Category("Unit")]
[Category("Integration")]
[Category("Architecture")]
[Category("Slow")]
[Category("Database")]
[Category("External")]

// Module-specific categories
[Category("Style")]
[Category("Memory")]
[Category("Agents")]

// Usage
[TestFixture]
[Category("Unit")]
[Category("Style")]
public class StyleEngineTests { }

[TestFixture]
[Category("Integration")]
[Category("Database")]
public class StyleRepositoryTests { }
```

---

## 3. Test Structure

### 3.1 Basic Test Structure

```csharp
[TestFixture]
public class StyleEngineTests
{
    // System Under Test
    private StyleEngine _sut = null!;

    // Dependencies
    private Mock<IStyleRepository> _mockRepository = null!;
    private Mock<ILogger<StyleEngine>> _mockLogger = null!;
    private Mock<IEventBus> _mockEventBus = null!;

    [SetUp]
    public void SetUp()
    {
        // Create mocks
        _mockRepository = new Mock<IStyleRepository>();
        _mockLogger = new Mock<ILogger<StyleEngine>>();
        _mockEventBus = new Mock<IEventBus>();

        // Create SUT with dependencies
        _sut = new StyleEngine(
            _mockLogger.Object,
            _mockRepository.Object,
            _mockEventBus.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup if needed
    }

    [Test]
    public async Task AnalyzeAsync_WithValidDocument_ReturnsScore()
    {
        // Arrange
        var document = new Document("This is valid content.");
        _mockRepository
            .Setup(r => r.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleRule>());

        // Act
        var result = await _sut.AnalyzeAsync(document);

        // Assert
        Assert.That(result.Score, Is.EqualTo(100));
        Assert.That(result.Violations, Is.Empty);
    }
}
```

### 3.2 AAA Pattern

```csharp
[Test]
public async Task AnalyzeAsync_WithForbiddenTerm_ReturnsViolation()
{
    // ═══════════════════════════════════════════════════════════════
    // ARRANGE - Set up the test scenario
    // ═══════════════════════════════════════════════════════════════
    var document = new Document("Configure the whitelist for trusted IPs.");

    var rules = new List<StyleRule>
    {
        new StyleRule("whitelist", "allowlist", ViolationType.Error, "TERM-001")
    };

    _mockRepository
        .Setup(r => r.GetRulesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(rules);

    // ═══════════════════════════════════════════════════════════════
    // ACT - Execute the method under test
    // ═══════════════════════════════════════════════════════════════
    var result = await _sut.AnalyzeAsync(document);

    // ═══════════════════════════════════════════════════════════════
    // ASSERT - Verify the expected outcome
    // ═══════════════════════════════════════════════════════════════
    Assert.Multiple(() =>
    {
        Assert.That(result.Score, Is.LessThan(100));
        Assert.That(result.Violations, Has.Count.EqualTo(1));
        Assert.That(result.Violations[0].Word, Is.EqualTo("whitelist"));
        Assert.That(result.Violations[0].Suggestion, Is.EqualTo("allowlist"));
    });
}
```

### 3.3 One Assert Per Concept

```csharp
// ✅ GOOD: Group related assertions with Assert.Multiple
[Test]
public async Task AnalyzeAsync_WithMultipleViolations_ReturnsAllViolations()
{
    // Arrange
    var document = new Document("Configure whitelist and blacklist.");
    SetupRules();

    // Act
    var result = await _sut.AnalyzeAsync(document);

    // Assert - Multiple assertions for ONE concept (the violations)
    Assert.Multiple(() =>
    {
        Assert.That(result.Violations, Has.Count.EqualTo(2));
        Assert.That(result.Violations[0].Word, Is.EqualTo("whitelist"));
        Assert.That(result.Violations[1].Word, Is.EqualTo("blacklist"));
    });
}

// ❌ BAD: Testing multiple concepts in one test
[Test]
public async Task AnalyzeAsync_Works() // Too vague
{
    // Testing violations AND score AND events AND timing...
}
```

---

## 4. Assertions

### 4.1 NUnit Constraint Model

```csharp
// ═══════════════════════════════════════════════════════════════
// EQUALITY
// ═══════════════════════════════════════════════════════════════
Assert.That(result.Score, Is.EqualTo(100));
Assert.That(result.Score, Is.Not.EqualTo(0));
Assert.That(result.Title, Is.EqualTo("Hello").IgnoreCase);

// ═══════════════════════════════════════════════════════════════
// COMPARISON
// ═══════════════════════════════════════════════════════════════
Assert.That(result.Score, Is.GreaterThan(90));
Assert.That(result.Score, Is.LessThanOrEqualTo(100));
Assert.That(result.Score, Is.InRange(0, 100));

// ═══════════════════════════════════════════════════════════════
// NULLABILITY
// ═══════════════════════════════════════════════════════════════
Assert.That(result, Is.Not.Null);
Assert.That(result.Error, Is.Null);
Assert.That(result.Value, Is.Null.Or.Empty);

// ═══════════════════════════════════════════════════════════════
// STRINGS
// ═══════════════════════════════════════════════════════════════
Assert.That(result.Message, Does.Contain("success"));
Assert.That(result.Message, Does.StartWith("Document"));
Assert.That(result.Message, Does.EndWith("."));
Assert.That(result.Message, Does.Match(@"\d{4}-\d{2}-\d{2}"));
Assert.That(result.Message, Is.Empty);
Assert.That(result.Message, Is.Not.Empty);

// ═══════════════════════════════════════════════════════════════
// COLLECTIONS
// ═══════════════════════════════════════════════════════════════
Assert.That(result.Violations, Is.Empty);
Assert.That(result.Violations, Is.Not.Empty);
Assert.That(result.Violations, Has.Count.EqualTo(3));
Assert.That(result.Violations, Has.Exactly(2).Items);
Assert.That(result.Violations, Contains.Item(expectedViolation));
Assert.That(result.Violations, Has.Some.Matches<StyleViolation>(v => v.Type == ViolationType.Error));
Assert.That(result.Violations, Has.All.Matches<StyleViolation>(v => v.Line > 0));
Assert.That(result.Violations, Is.Ordered.By("Line"));
Assert.That(result.Violations, Is.Unique);

// ═══════════════════════════════════════════════════════════════
// TYPES
// ═══════════════════════════════════════════════════════════════
Assert.That(result, Is.TypeOf<AnalysisResult>());
Assert.That(result, Is.InstanceOf<IResult>());
Assert.That(result, Is.AssignableTo<IResult>());

// ═══════════════════════════════════════════════════════════════
// EXCEPTIONS
// ═══════════════════════════════════════════════════════════════
Assert.That(() => service.Process(null!), Throws.ArgumentNullException);
Assert.That(async () => await service.ProcessAsync(null!), Throws.ArgumentNullException);
Assert.That(() => service.Process(invalid), Throws.TypeOf<ValidationException>());
Assert.That(
    () => service.Process(invalid),
    Throws.TypeOf<ValidationException>()
        .With.Property("ParamName").EqualTo("document"));

// ═══════════════════════════════════════════════════════════════
// BOOLEAN
// ═══════════════════════════════════════════════════════════════
Assert.That(result.IsSuccess, Is.True);
Assert.That(result.HasErrors, Is.False);
```

### 4.2 FluentAssertions (Alternative)

```csharp
// FluentAssertions provides more readable syntax
using FluentAssertions;

[Test]
public async Task AnalyzeAsync_WithForbiddenTerm_ReturnsViolation()
{
    // Arrange
    var document = new Document("Configure the whitelist.");

    // Act
    var result = await _sut.AnalyzeAsync(document);

    // Assert
    result.Should().NotBeNull();
    result.Score.Should().BeLessThan(100);
    result.Violations.Should().HaveCount(1);
    result.Violations.Should().ContainSingle(v => v.Word == "whitelist");
    result.Violations.First().Should().BeEquivalentTo(new
    {
        Word = "whitelist",
        Suggestion = "allowlist",
        Type = ViolationType.Error
    });
}

// Exception assertions
Func<Task> act = async () => await service.ProcessAsync(null!);
await act.Should().ThrowAsync<ArgumentNullException>()
    .WithParameterName("document");

// Collection assertions
result.Violations
    .Should().BeInAscendingOrder(v => v.Line)
    .And.AllSatisfy(v => v.RuleId.Should().NotBeEmpty());
```

---

## 5. Mocking

### 5.1 Basic Mocking with Moq

```csharp
[TestFixture]
public class DocumentServiceTests
{
    private Mock<IDocumentRepository> _mockRepo = null!;
    private Mock<ILogger<DocumentService>> _mockLogger = null!;
    private DocumentService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<IDocumentRepository>();
        _mockLogger = new Mock<ILogger<DocumentService>>();
        _sut = new DocumentService(_mockLogger.Object, _mockRepo.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    // SETUP RETURN VALUES
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public async Task GetAsync_WhenDocumentExists_ReturnsDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var expectedDocument = new Document { Id = documentId, Title = "Test" };

        _mockRepo
            .Setup(r => r.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocument);

        // Act
        var result = await _sut.GetAsync(documentId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedDocument));
    }

    // ═══════════════════════════════════════════════════════════════
    // SETUP SEQUENCES
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public async Task RetryableOperation_RetriesOnFailure()
    {
        // Arrange
        _mockRepo
            .SetupSequence(r => r.SaveAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())  // First call fails
            .ThrowsAsync(new TimeoutException())  // Second call fails
            .Returns(Task.CompletedTask);          // Third call succeeds

        // Act
        await _sut.SaveWithRetryAsync(new Document());

        // Assert
        _mockRepo.Verify(r => r.SaveAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // VERIFY CALLS
    // ═══════════════════════════════════════════════════════════════
    [Test]
    public async Task SaveAsync_CallsRepositoryOnce()
    {
        // Arrange
        var document = new Document { Title = "Test" };

        // Act
        await _sut.SaveAsync(document);

        // Assert
        _mockRepo.Verify(
            r => r.SaveAsync(
                It.Is<Document>(d => d.Title == "Test"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SaveAsync_NeverCallsDeleteOnSuccess()
    {
        // Arrange
        var document = new Document();

        // Act
        await _sut.SaveAsync(document);

        // Assert
        _mockRepo.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

### 5.2 Mock Setup Patterns

```csharp
// ═══════════════════════════════════════════════════════════════
// ARGUMENT MATCHING
// ═══════════════════════════════════════════════════════════════

// Any value
_mockRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync(document);

// Specific value
_mockRepo.Setup(r => r.GetAsync(expectedId)).ReturnsAsync(document);

// Condition
_mockRepo.Setup(r => r.GetAsync(It.Is<Guid>(id => id != Guid.Empty))).ReturnsAsync(document);

// Regex for strings
_mockService.Setup(s => s.Search(It.IsRegex(@"^\w+$"))).ReturnsAsync(results);

// ═══════════════════════════════════════════════════════════════
// CALLBACKS
// ═══════════════════════════════════════════════════════════════

// Capture arguments
Document? capturedDocument = null;
_mockRepo
    .Setup(r => r.SaveAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
    .Callback<Document, CancellationToken>((doc, _) => capturedDocument = doc)
    .Returns(Task.CompletedTask);

// ═══════════════════════════════════════════════════════════════
// PROPERTIES
// ═══════════════════════════════════════════════════════════════

// Setup property
_mockConfig.SetupGet(c => c.MaxRetries).Returns(3);

// Track property changes
_mockConfig.SetupProperty(c => c.IsEnabled, true);

// ═══════════════════════════════════════════════════════════════
// EXCEPTIONS
// ═══════════════════════════════════════════════════════════════

_mockRepo
    .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ThrowsAsync(new NotFoundException());
```

### 5.3 Mock Logger Verification

```csharp
// Verify logging calls
[Test]
public async Task ProcessAsync_WhenFails_LogsError()
{
    // Arrange
    _mockRepo
        .Setup(r => r.SaveAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Database error"));

    // Act
    try { await _sut.ProcessAsync(new Document()); } catch { }

    // Assert
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Database error")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

---

## 6. Test Data

### 6.1 AutoFixture

```csharp
[TestFixture]
public class DocumentServiceTests
{
    private IFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();

        // Customize for specific types
        _fixture.Customize<Document>(c => c
            .With(d => d.Content, "Valid content")
            .Without(d => d.Id)); // Auto-generate ID
    }

    [Test]
    public async Task ProcessAsync_WithDocument_Succeeds()
    {
        // Arrange
        var document = _fixture.Create<Document>();

        // Act & Assert
        await _sut.ProcessAsync(document);
    }

    // Auto-inject test data
    [Test, AutoData]
    public async Task SaveAsync_WithDocument_Succeeds(Document document)
    {
        await _sut.SaveAsync(document);
        _mockRepo.Verify(r => r.SaveAsync(document, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Create collections
    [Test]
    public async Task GetAllAsync_ReturnsDocuments()
    {
        // Arrange
        var documents = _fixture.CreateMany<Document>(10).ToList();
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(10));
    }
}
```

### 6.2 Bogus for Realistic Data

```csharp
using Bogus;

public static class TestDataGenerators
{
    public static readonly Faker<Document> DocumentFaker = new Faker<Document>()
        .RuleFor(d => d.Id, f => Guid.NewGuid())
        .RuleFor(d => d.Title, f => f.Lorem.Sentence(3, 5))
        .RuleFor(d => d.Content, f => f.Lorem.Paragraphs(2, 5))
        .RuleFor(d => d.Author, f => f.Name.FullName())
        .RuleFor(d => d.CreatedAt, f => f.Date.Past(1))
        .RuleFor(d => d.WordCount, (f, d) => d.Content.Split(' ').Length);

    public static readonly Faker<StyleViolation> ViolationFaker = new Faker<StyleViolation>()
        .RuleFor(v => v.Line, f => f.Random.Int(1, 100))
        .RuleFor(v => v.Column, f => f.Random.Int(1, 80))
        .RuleFor(v => v.Word, f => f.PickRandom("whitelist", "blacklist", "master", "slave"))
        .RuleFor(v => v.Suggestion, (f, v) => v.Word switch
        {
            "whitelist" => "allowlist",
            "blacklist" => "blocklist",
            "master" => "primary",
            "slave" => "replica",
            _ => "replacement"
        })
        .RuleFor(v => v.Type, f => f.PickRandom<ViolationType>())
        .RuleFor(v => v.RuleId, f => $"TERM-{f.Random.Int(1, 100):D3}");
}

// Usage
[Test]
public async Task AnalyzeAsync_WithViolations_CalculatesCorrectScore()
{
    // Arrange
    var violations = ViolationFaker.Generate(5);
    var document = DocumentFaker.Generate();

    // ...
}
```

### 6.3 Test Data Builders

```csharp
public class DocumentBuilder
{
    private readonly Document _document = new();

    public DocumentBuilder WithTitle(string title)
    {
        _document.Title = title;
        return this;
    }

    public DocumentBuilder WithContent(string content)
    {
        _document.Content = content;
        return this;
    }

    public DocumentBuilder WithForbiddenTerms(params string[] terms)
    {
        _document.Content = string.Join(" ", terms);
        return this;
    }

    public DocumentBuilder AsReadOnly()
    {
        _document.IsReadOnly = true;
        return this;
    }

    public Document Build() => _document;

    public static DocumentBuilder Create() => new();
}

// Usage
[Test]
public async Task AnalyzeAsync_WithForbiddenTerms_ReturnsViolations()
{
    // Arrange
    var document = DocumentBuilder.Create()
        .WithTitle("API Guide")
        .WithForbiddenTerms("whitelist", "blacklist")
        .Build();

    // Act
    var result = await _sut.AnalyzeAsync(document);

    // Assert
    Assert.That(result.Violations, Has.Count.EqualTo(2));
}
```

---

## 7. Async Testing

### 7.1 Async Test Methods

```csharp
// ✅ CORRECT: Use async Task
[Test]
public async Task ProcessAsync_WithValidInput_Succeeds()
{
    var result = await _sut.ProcessAsync(input);
    Assert.That(result.IsSuccess, Is.True);
}

// ❌ WRONG: async void
[Test]
public async void ProcessAsync_WithValidInput_Succeeds() // NEVER DO THIS
{
    // Test may complete before assertions run
}

// ❌ WRONG: .Result or .Wait()
[Test]
public void ProcessAsync_WithValidInput_Succeeds()
{
    var result = _sut.ProcessAsync(input).Result; // Deadlock risk
}
```

### 7.2 Cancellation Testing

```csharp
[Test]
public async Task ProcessAsync_WhenCancelled_ThrowsOperationCanceled()
{
    // Arrange
    using var cts = new CancellationTokenSource();
    var slowDocument = CreateLargeDocument();

    // Act
    var task = _sut.ProcessAsync(slowDocument, cts.Token);
    cts.Cancel();

    // Assert
    Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
}

[Test]
public async Task ProcessAsync_WithPreCancelledToken_ThrowsImmediately()
{
    // Arrange
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act & Assert
    Assert.ThrowsAsync<OperationCanceledException>(
        async () => await _sut.ProcessAsync(new Document(), cts.Token));
}
```

### 7.3 Timeout Testing

```csharp
[Test]
[Timeout(5000)] // 5 second timeout
public async Task ProcessAsync_CompletesWithinTimeout()
{
    var result = await _sut.ProcessAsync(document);
    Assert.That(result, Is.Not.Null);
}

// Or using Assert.That with timeout constraint
[Test]
public async Task ProcessAsync_CompletesQuickly()
{
    Assert.That(
        async () => await _sut.ProcessAsync(smallDocument),
        Is.Completed.After(1000).MilliSeconds);
}
```

---

## 8. Integration Testing

### 8.1 Database Fixture

```csharp
// Shared database container for integration tests
[SetUpFixture]
public class DatabaseFixture
{
    public static string ConnectionString { get; private set; } = null!;
    private static PostgreSqlContainer _container = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("lexichord_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Run migrations
        using var connection = new NpgsqlConnection(ConnectionString);
        var migrator = new FluentMigrator.Runner.MigrationRunner(/* ... */);
        migrator.MigrateUp();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }
}
```

### 8.2 Integration Test Base

```csharp
[TestFixture]
[Category("Integration")]
[Category("Database")]
public abstract class IntegrationTestBase
{
    protected IServiceProvider Services { get; private set; } = null!;
    protected IDbContext DbContext { get; private set; } = null!;

    [SetUp]
    public virtual async Task SetUp()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(DatabaseFixture.ConnectionString));

        services.AddLexichordServices();

        Services = services.BuildServiceProvider();
        DbContext = Services.GetRequiredService<IDbContext>();

        // Start transaction (rolled back in TearDown)
        await DbContext.Database.BeginTransactionAsync();
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        // Rollback to keep tests isolated
        await DbContext.Database.RollbackTransactionAsync();
    }
}
```

### 8.3 Integration Test Example

```csharp
[TestFixture]
public class StyleRepositoryTests : IntegrationTestBase
{
    private IStyleRepository _sut = null!;

    public override async Task SetUp()
    {
        await base.SetUp();
        _sut = Services.GetRequiredService<IStyleRepository>();
    }

    [Test]
    public async Task GetRulesAsync_ReturnsSeededRules()
    {
        // Act
        var rules = await _sut.GetRulesAsync();

        // Assert
        Assert.That(rules, Is.Not.Empty);
        Assert.That(rules, Has.Some.Matches<StyleRule>(r => r.Term == "whitelist"));
    }

    [Test]
    public async Task AddRuleAsync_PersistsRule()
    {
        // Arrange
        var newRule = new StyleRule("testterm", "replacement", ViolationType.Warning, "TEST-001");

        // Act
        await _sut.AddRuleAsync(newRule);
        var retrieved = await _sut.GetByTermAsync("testterm");

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Suggestion, Is.EqualTo("replacement"));
    }
}
```

---

## 9. Architecture Tests

### 9.1 Dependency Tests

```csharp
[TestFixture]
[Category("Architecture")]
public class DependencyTests
{
    private Assembly _abstractionsAssembly = null!;
    private Assembly _hostAssembly = null!;
    private IEnumerable<Assembly> _moduleAssemblies = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _abstractionsAssembly = typeof(IModule).Assembly;
        _hostAssembly = typeof(App).Assembly;
        _moduleAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("Lexichord.Modules") == true);
    }

    [Test]
    public void Abstractions_ShouldNotReference_Host()
    {
        var references = _abstractionsAssembly.GetReferencedAssemblies()
            .Select(r => r.Name);

        Assert.That(references, Does.Not.Contain("Lexichord.Host"));
    }

    [Test]
    public void Abstractions_ShouldNotReference_AnyModule()
    {
        var references = _abstractionsAssembly.GetReferencedAssemblies()
            .Where(r => r.Name?.StartsWith("Lexichord.Modules") == true);

        Assert.That(references, Is.Empty);
    }

    [Test]
    public void Host_ShouldNotReference_Modules()
    {
        var references = _hostAssembly.GetReferencedAssemblies()
            .Where(r => r.Name?.StartsWith("Lexichord.Modules") == true);

        Assert.That(references, Is.Empty,
            "Host should load modules via reflection, not direct references");
    }

    [Test]
    public void Modules_ShouldNotReference_OtherModules()
    {
        foreach (var assembly in _moduleAssemblies)
        {
            var moduleReferences = assembly.GetReferencedAssemblies()
                .Where(r => r.Name?.StartsWith("Lexichord.Modules") == true)
                .Where(r => r.Name != assembly.GetName().Name);

            Assert.That(moduleReferences, Is.Empty,
                $"{assembly.GetName().Name} should not reference other modules. " +
                $"Use the event bus instead.");
        }
    }

    [Test]
    public void AllServices_ShouldHave_InterfaceInAbstractions()
    {
        var serviceTypes = _hostAssembly.GetTypes()
            .Concat(_moduleAssemblies.SelectMany(a => a.GetTypes()))
            .Where(t => t.Name.EndsWith("Service") && t.IsClass && !t.IsAbstract);

        foreach (var serviceType in serviceTypes)
        {
            var interfaces = serviceType.GetInterfaces()
                .Where(i => i.Assembly == _abstractionsAssembly);

            Assert.That(interfaces, Is.Not.Empty,
                $"{serviceType.Name} should implement an interface from Abstractions");
        }
    }
}
```

---

## 10. Test Organization

### 10.1 Running Tests

```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=Unit"

# Run specific test class
dotnet test --filter "FullyQualifiedName~StyleEngineTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run in parallel
dotnet test --parallel
```

### 10.2 CI Configuration

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: test
          POSTGRES_DB: lexichord_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Unit Tests
        run: dotnet test tests/Lexichord.Tests.Unit --no-build --filter "Category=Unit"

      - name: Integration Tests
        run: dotnet test tests/Lexichord.Tests.Integration --no-build --filter "Category=Integration"
        env:
          ConnectionStrings__Default: "Host=localhost;Database=lexichord_test;Username=postgres;Password=test"

      - name: Architecture Tests
        run: dotnet test tests/Lexichord.Tests.Architecture --no-build

      - name: Coverage Report
        uses: codecov/codecov-action@v4
```

---

## Appendix: Quick Reference

```
╔═══════════════════════════════════════════════════════════════╗
║  NUNIT QUICK REFERENCE                                        ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  NAMING                                                       ║
║  Class:   {ClassUnderTest}Tests                               ║
║  Method:  {Method}_{Scenario}_{ExpectedBehavior}              ║
║                                                               ║
║  ATTRIBUTES                                                   ║
║  [TestFixture]  - Test class                                  ║
║  [Test]         - Test method                                 ║
║  [SetUp]        - Before each test                            ║
║  [TearDown]     - After each test                             ║
║  [OneTimeSetUp] - Before all tests                            ║
║  [Category("X")] - Categorize tests                           ║
║  [TestCase(...)] - Parameterized test                         ║
║                                                               ║
║  ASSERTIONS                                                   ║
║  Assert.That(x, Is.EqualTo(y))                               ║
║  Assert.That(x, Is.Not.Null)                                 ║
║  Assert.That(list, Has.Count.EqualTo(5))                     ║
║  Assert.That(list, Contains.Item(x))                         ║
║  Assert.That(() => x, Throws.TypeOf<T>())                    ║
║  Assert.Multiple(() => { ... })                              ║
║                                                               ║
║  MOCKING (Moq)                                                ║
║  mock.Setup(x => x.Method()).Returns(value)                  ║
║  mock.Setup(x => x.Method()).ReturnsAsync(value)             ║
║  mock.Setup(x => x.Method()).ThrowsAsync(ex)                 ║
║  mock.Verify(x => x.Method(), Times.Once)                    ║
║                                                               ║
║  CATEGORIES                                                   ║
║  Unit         - Fast, isolated tests                          ║
║  Integration  - Tests with real dependencies                  ║
║  Architecture - Dependency/structure tests                    ║
║  Slow         - Long-running tests                            ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```
