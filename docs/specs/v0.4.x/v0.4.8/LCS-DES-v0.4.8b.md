# LCS-DES-048b: Integration Tests

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-048b                             |
| **Version**      | v0.4.8b                                  |
| **Title**        | Integration Tests                        |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG.IntegrationTests` |
| **License Tier** | N/A (Development)                        |

---

## 1. Overview

### 1.1 Purpose

This specification defines the integration test suite that validates end-to-end flows of the RAG system against real infrastructure. Tests use Testcontainers to spin up PostgreSQL with pgvector for realistic database interactions.

### 1.2 Goals

- Configure test containers with PostgreSQL + pgvector
- Test complete ingestion pipeline
- Test search roundtrip with real vector queries
- Test change detection and re-indexing
- Test deletion cascade behavior
- Test concurrent access safety
- Ensure tests are isolated and repeatable

### 1.3 Non-Goals

- Load testing (covered separately)
- UI testing
- Testing with production data

---

## 2. Test Infrastructure

### 2.1 Project Configuration

```xml
<!-- tests/Lexichord.Modules.RAG.IntegrationTests/...csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.x" />
    <PackageReference Include="xunit" Version="2.x" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.x" />
    <PackageReference Include="FluentAssertions" Version="6.x" />
    <PackageReference Include="Testcontainers" Version="3.x" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.x" />
    <PackageReference Include="Respawn" Version="6.x" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Lexichord.Modules.RAG\Lexichord.Modules.RAG.csproj" />
  </ItemGroup>
</Project>
```

### 2.2 PostgreSQL Fixture

```csharp
namespace Lexichord.Modules.RAG.IntegrationTests.Fixtures;

using Testcontainers.PostgreSql;
using Respawn;

/// <summary>
/// Fixture that manages a PostgreSQL container with pgvector for integration tests.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();
    public IDbConnectionFactory ConnectionFactory { get; private set; } = null!;
    public IServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container with pgvector
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("lexichord_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();

        // Initialize services
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        ConnectionFactory = Services.GetRequiredService<IDbConnectionFactory>();

        // Run migrations
        await RunMigrationsAsync();

        // Configure Respawn for database reset
        using var conn = await ConnectionFactory.CreateConnectionAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = new[] { "VersionInfo" }
        });
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to a clean state between tests.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var conn = await ConnectionFactory.CreateConnectionAsync();
        await _respawner.ResetAsync(conn);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(ConnectionString));
        services.AddSingleton<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IChunkRepository, ChunkRepository>();

        // Mock embedding service for integration tests
        var mockEmbedder = new Mock<IEmbeddingService>();
        mockEmbedder.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, CancellationToken _) => GenerateDeterministicEmbedding(text));
        mockEmbedder.Setup(e => e.Dimensions).Returns(1536);

        services.AddSingleton(mockEmbedder.Object);
        services.AddSingleton<IChunkingStrategy, FixedSizeChunkingStrategy>();
        services.AddSingleton<IIngestionService, IngestionService>();
        services.AddSingleton<ISemanticSearchService, PgVectorSearchService>();

        services.AddLogging(builder => builder.AddDebug());
    }

    private async Task RunMigrationsAsync()
    {
        using var conn = await ConnectionFactory.CreateConnectionAsync();

        // Enable pgvector extension
        await conn.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS vector;");

        // Run FluentMigrator migrations
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(ConnectionString)
                .ScanIn(typeof(Migration_003_VectorSchema).Assembly).For.Migrations())
            .BuildServiceProvider();

        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    /// <summary>
    /// Generates a deterministic embedding based on text content.
    /// This allows predictable search results in tests.
    /// </summary>
    private static float[] GenerateDeterministicEmbedding(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var embedding = new float[1536];

        for (int i = 0; i < 1536; i++)
        {
            embedding[i] = (float)(hash[i % hash.Length]) / 255f;
        }

        // Normalize
        var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < 1536; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}
```

### 2.3 Collection Definition

```csharp
namespace Lexichord.Modules.RAG.IntegrationTests;

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    // This class has no code, it's just a marker for xUnit
}
```

---

## 3. Integration Test Specifications

### 3.1 Ingestion Pipeline Tests

```csharp
namespace Lexichord.Modules.RAG.IntegrationTests;

[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class IngestionPipelineTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IIngestionService _ingestionService;
    private readonly IDocumentRepository _documentRepo;
    private readonly IChunkRepository _chunkRepo;
    private readonly string _testFilesDir;

    public IngestionPipelineTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _ingestionService = fixture.Services.GetRequiredService<IIngestionService>();
        _documentRepo = fixture.Services.GetRequiredService<IDocumentRepository>();
        _chunkRepo = fixture.Services.GetRequiredService<IChunkRepository>();
        _testFilesDir = Path.Combine(Path.GetTempPath(), $"lexichord_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFilesDir);
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testFilesDir))
            Directory.Delete(_testFilesDir, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IngestFile_CreatesDocumentAndChunks()
    {
        // Arrange
        var filePath = CreateTestFile("# Test Document\n\nThis is test content.");

        // Act
        var result = await _ingestionService.IngestFileAsync(filePath);

        // Assert
        result.Success.Should().BeTrue();

        var doc = await _documentRepo.GetByPathAsync(filePath);
        doc.Should().NotBeNull();
        doc!.ChunkCount.Should().BeGreaterThan(0);

        var chunks = await _chunkRepo.GetByDocumentAsync(doc.Id);
        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(c =>
        {
            c.Embedding.Should().HaveCount(1536);
            c.Content.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task IngestFile_StoresFileHash()
    {
        var filePath = CreateTestFile("Content for hashing");

        await _ingestionService.IngestFileAsync(filePath);

        var doc = await _documentRepo.GetByPathAsync(filePath);
        doc!.FileHash.Should().NotBeNullOrEmpty();
        doc.FileHash.Should().HaveLength(64); // SHA-256 hex
    }

    [Fact]
    public async Task IngestFile_UpdatesExistingDocument()
    {
        // First ingestion
        var filePath = CreateTestFile("Original content");
        await _ingestionService.IngestFileAsync(filePath);
        var originalDoc = await _documentRepo.GetByPathAsync(filePath);

        // Modify file
        await File.WriteAllTextAsync(filePath, "Modified content with more text");

        // Second ingestion
        await _ingestionService.IngestFileAsync(filePath);
        var updatedDoc = await _documentRepo.GetByPathAsync(filePath);

        // Should have same ID but updated content
        updatedDoc!.Id.Should().Be(originalDoc!.Id);
        updatedDoc.FileHash.Should().NotBe(originalDoc.FileHash);
    }

    [Fact]
    public async Task IngestDirectory_ProcessesAllFiles()
    {
        // Arrange
        CreateTestFile("File 1 content", "file1.md");
        CreateTestFile("File 2 content", "file2.md");
        CreateTestFile("File 3 content", "file3.txt");

        // Act
        var result = await _ingestionService.IngestDirectoryAsync(_testFilesDir, recursive: false);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentsProcessed.Should().BeGreaterOrEqualTo(3);

        var docs = await _documentRepo.GetAllAsync();
        docs.Should().HaveCountGreaterOrEqualTo(3);
    }

    private string CreateTestFile(string content, string? fileName = null)
    {
        fileName ??= $"test_{Guid.NewGuid()}.md";
        var path = Path.Combine(_testFilesDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}
```

### 3.2 Search Roundtrip Tests

```csharp
[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class SearchRoundtripTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IIngestionService _ingestionService;
    private readonly ISemanticSearchService _searchService;
    private readonly string _testFilesDir;

    public SearchRoundtripTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _ingestionService = fixture.Services.GetRequiredService<IIngestionService>();
        _searchService = fixture.Services.GetRequiredService<ISemanticSearchService>();
        _testFilesDir = Path.Combine(Path.GetTempPath(), $"lexichord_search_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFilesDir);
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SetupTestDocuments();
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testFilesDir))
            Directory.Delete(_testFilesDir, recursive: true);
        return Task.CompletedTask;
    }

    private async Task SetupTestDocuments()
    {
        var docs = new Dictionary<string, string>
        {
            ["project-management.md"] = @"# Project Management
This document covers project management best practices.
Includes agile methodology, scrum, and kanban.",

            ["software-architecture.md"] = @"# Software Architecture
Design patterns and architectural principles.
Covers microservices, monoliths, and modular design.",

            ["testing-guide.md"] = @"# Testing Guide
Unit testing, integration testing, and end-to-end testing.
Test-driven development and behavior-driven development."
        };

        foreach (var (name, content) in docs)
        {
            var path = Path.Combine(_testFilesDir, name);
            await File.WriteAllTextAsync(path, content);
            await _ingestionService.IngestFileAsync(path);
        }
    }

    [Fact]
    public async Task Search_ReturnsRelevantResults()
    {
        var result = await _searchService.SearchAsync("project management agile", new SearchOptions
        {
            TopK = 5,
            MinScore = 0.1f
        });

        result.Hits.Should().NotBeEmpty();
        result.Hits[0].Document.FilePath.Should().Contain("project-management");
    }

    [Fact]
    public async Task Search_RespectsTopK()
    {
        var result = await _searchService.SearchAsync("software testing", new SearchOptions
        {
            TopK = 2,
            MinScore = 0.1f
        });

        result.Hits.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Search_FiltersbyDocument()
    {
        // Get a specific document ID
        var docs = await _fixture.Services.GetRequiredService<IDocumentRepository>().GetAllAsync();
        var targetDoc = docs.First(d => d.FilePath.Contains("testing"));

        var result = await _searchService.SearchAsync("architecture", new SearchOptions
        {
            DocumentFilter = targetDoc.Id,
            MinScore = 0.0f
        });

        result.Hits.Should().AllSatisfy(h => h.Document.Id.Should().Be(targetDoc.Id));
    }

    [Fact]
    public async Task Search_IncludesSearchDuration()
    {
        var result = await _searchService.SearchAsync("test query", new SearchOptions());

        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Search_ResultsIncludeChunkContent()
    {
        var result = await _searchService.SearchAsync("architecture patterns", new SearchOptions
        {
            TopK = 1,
            MinScore = 0.1f
        });

        result.Hits.Should().NotBeEmpty();
        result.Hits[0].Chunk.Content.Should().NotBeNullOrEmpty();
    }
}
```

### 3.3 Change Detection Tests

```csharp
[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class ChangeDetectionTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IIngestionService _ingestionService;
    private readonly IDocumentRepository _documentRepo;
    private readonly string _testFile;

    public ChangeDetectionTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _ingestionService = fixture.Services.GetRequiredService<IIngestionService>();
        _documentRepo = fixture.Services.GetRequiredService<IDocumentRepository>();
        _testFile = Path.Combine(Path.GetTempPath(), $"change_test_{Guid.NewGuid()}.md");
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        if (File.Exists(_testFile))
            File.Delete(_testFile);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IngestFile_UnchangedFile_SkipsProcessing()
    {
        // First ingestion
        await File.WriteAllTextAsync(_testFile, "Original content");
        await _ingestionService.IngestFileAsync(_testFile);
        var firstDoc = await _documentRepo.GetByPathAsync(_testFile);
        var firstIndexedAt = firstDoc!.IndexedAt;

        // Wait a bit
        await Task.Delay(100);

        // Second ingestion without changes
        var result = await _ingestionService.IngestFileAsync(_testFile);

        var secondDoc = await _documentRepo.GetByPathAsync(_testFile);

        // Should be skipped (same hash)
        secondDoc!.IndexedAt.Should().Be(firstIndexedAt);
    }

    [Fact]
    public async Task IngestFile_ChangedFile_ReindexesContent()
    {
        // First ingestion
        await File.WriteAllTextAsync(_testFile, "Original content");
        await _ingestionService.IngestFileAsync(_testFile);
        var originalHash = (await _documentRepo.GetByPathAsync(_testFile))!.FileHash;

        // Modify file
        await File.WriteAllTextAsync(_testFile, "Modified content that is different");

        // Second ingestion
        await _ingestionService.IngestFileAsync(_testFile);
        var newHash = (await _documentRepo.GetByPathAsync(_testFile))!.FileHash;

        newHash.Should().NotBe(originalHash);
    }
}
```

### 3.4 Deletion Cascade Tests

```csharp
[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class DeletionCascadeTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IIngestionService _ingestionService;
    private readonly IDocumentRepository _documentRepo;
    private readonly IChunkRepository _chunkRepo;
    private readonly string _testFile;

    public DeletionCascadeTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _ingestionService = fixture.Services.GetRequiredService<IIngestionService>();
        _documentRepo = fixture.Services.GetRequiredService<IDocumentRepository>();
        _chunkRepo = fixture.Services.GetRequiredService<IChunkRepository>();
        _testFile = Path.Combine(Path.GetTempPath(), $"delete_test_{Guid.NewGuid()}.md");
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        if (File.Exists(_testFile))
            File.Delete(_testFile);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RemoveDocument_DeletesChunks()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFile, "# Large Document\n\n" + new string('a', 5000));
        await _ingestionService.IngestFileAsync(_testFile);

        var doc = await _documentRepo.GetByPathAsync(_testFile);
        var chunksBefore = await _chunkRepo.GetByDocumentAsync(doc!.Id);
        chunksBefore.Should().NotBeEmpty();

        // Act
        await _ingestionService.RemoveDocumentAsync(_testFile);

        // Assert
        var deletedDoc = await _documentRepo.GetByPathAsync(_testFile);
        deletedDoc.Should().BeNull();

        var chunksAfter = await _chunkRepo.GetByDocumentAsync(doc.Id);
        chunksAfter.Should().BeEmpty();
    }
}
```

### 3.5 Concurrent Access Tests

```csharp
[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class ConcurrentAccessTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly IIngestionService _ingestionService;
    private readonly string _testFilesDir;

    public ConcurrentAccessTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _ingestionService = fixture.Services.GetRequiredService<IIngestionService>();
        _testFilesDir = Path.Combine(Path.GetTempPath(), $"concurrent_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFilesDir);
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        if (Directory.Exists(_testFilesDir))
            Directory.Delete(_testFilesDir, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ConcurrentIngestion_NoDeadlocks()
    {
        // Create test files
        var files = Enumerable.Range(1, 10)
            .Select(i =>
            {
                var path = Path.Combine(_testFilesDir, $"file_{i}.md");
                File.WriteAllText(path, $"Content for file {i}");
                return path;
            })
            .ToList();

        // Ingest concurrently
        var tasks = files.Select(f => _ingestionService.IngestFileAsync(f));
        var results = await Task.WhenAll(tasks);

        // All should succeed without deadlocks
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task ConcurrentSearch_ThreadSafe()
    {
        // Setup some documents first
        for (int i = 0; i < 5; i++)
        {
            var path = Path.Combine(_testFilesDir, $"search_doc_{i}.md");
            await File.WriteAllTextAsync(path, $"Search content number {i}");
            await _ingestionService.IngestFileAsync(path);
        }

        var searchService = _fixture.Services.GetRequiredService<ISemanticSearchService>();

        // Run concurrent searches
        var searchTasks = Enumerable.Range(0, 20)
            .Select(_ => searchService.SearchAsync("content", new SearchOptions { TopK = 5 }));

        var results = await Task.WhenAll(searchTasks);

        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }
}
```

---

## 4. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Information | "Integration test starting: {TestName}" | Test start |
| Debug | "Database reset complete" | After Respawn |
| Debug | "Container started: {ContainerId}" | Testcontainer ready |

---

## 5. File Locations

| File | Path |
| :--- | :--- |
| Project | `tests/Lexichord.Modules.RAG.IntegrationTests/...csproj` |
| Fixture | `tests/Lexichord.Modules.RAG.IntegrationTests/Fixtures/PostgresFixture.cs` |
| Pipeline tests | `tests/Lexichord.Modules.RAG.IntegrationTests/IngestionPipelineTests.cs` |
| Search tests | `tests/Lexichord.Modules.RAG.IntegrationTests/SearchRoundtripTests.cs` |

---

## 6. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | PostgreSQL container starts with pgvector | [ ] |
| 2 | Migrations run successfully | [ ] |
| 3 | Ingestion creates documents and chunks | [ ] |
| 4 | Search returns relevant results | [ ] |
| 5 | Change detection works correctly | [ ] |
| 6 | Deletion cascades to chunks | [ ] |
| 7 | Concurrent access is safe | [ ] |
| 8 | Database resets between tests | [ ] |
| 9 | All integration tests pass | [ ] |

---

## 7. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
