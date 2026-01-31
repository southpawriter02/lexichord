# LCS-DES-047d: Indexing Errors

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-047d                             |
| **Version**      | v0.4.7d                                  |
| **Title**        | Indexing Errors                          |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | WriterPro                                |

---

## 1. Overview

### 1.1 Purpose

This specification defines the error handling and reporting system for document indexing failures. Failed documents are tracked with error messages, displayed in the Index Status View, and can be retried by the user.

### 1.2 Goals

- Add `error_message` column to documents table via migration
- Create `IndexingErrorInfo` record for error details
- Display failed documents with error information
- Implement "Retry" action for failed documents
- Log errors with full context for debugging
- Handle `DocumentIndexingFailedEvent` appropriately

### 1.3 Non-Goals

- Automatic retry policies (future)
- Error aggregation/analytics (future)
- Email notifications for failures (future)

---

## 2. Design

### 2.1 Database Migration

```csharp
namespace Lexichord.Modules.RAG.Migrations;

using FluentMigrator;

[Migration(20260127_001)]
public class Migration_004_AddErrorMessageColumn : Migration
{
    public override void Up()
    {
        Alter.Table("documents")
            .AddColumn("error_message").AsString(2000).Nullable()
            .AddColumn("error_timestamp").AsDateTimeOffset().Nullable()
            .AddColumn("retry_count").AsInt32().WithDefaultValue(0);

        // Add index for querying failed documents
        Create.Index("ix_documents_error_status")
            .OnTable("documents")
            .OnColumn("error_message")
            .Ascending()
            .WithOptions().Filter("error_message IS NOT NULL");
    }

    public override void Down()
    {
        Delete.Index("ix_documents_error_status").OnTable("documents");

        Delete.Column("retry_count").FromTable("documents");
        Delete.Column("error_timestamp").FromTable("documents");
        Delete.Column("error_message").FromTable("documents");
    }
}
```

### 2.2 Updated Document Entity

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Represents an indexed document in the database.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
    public int ChunkCount { get; set; }

    // Error tracking (v0.4.7d)
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ErrorTimestamp { get; set; }
    public int RetryCount { get; set; }

    /// <summary>
    /// Whether this document has a recorded error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Clears the error state (called on successful indexing).
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
        ErrorTimestamp = null;
        // Note: RetryCount is preserved for analytics
    }

    /// <summary>
    /// Records an indexing error.
    /// </summary>
    public void RecordError(string message)
    {
        ErrorMessage = message.Length > 2000 ? message[..2000] : message;
        ErrorTimestamp = DateTimeOffset.UtcNow;
        RetryCount++;
    }
}
```

### 2.3 IndexingErrorInfo Record

```csharp
namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Detailed information about an indexing error.
/// </summary>
public record IndexingErrorInfo
{
    /// <summary>
    /// The document that failed to index.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// File path of the document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// When the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Number of times indexing has been attempted.
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Error category for filtering/grouping.
    /// </summary>
    public IndexingErrorCategory Category { get; init; }

    /// <summary>
    /// Whether this error is likely transient and worth retrying.
    /// </summary>
    public bool IsRetryable => Category is
        IndexingErrorCategory.RateLimit or
        IndexingErrorCategory.NetworkError or
        IndexingErrorCategory.ServiceUnavailable;

    /// <summary>
    /// Suggested action for the user.
    /// </summary>
    public string SuggestedAction => Category switch
    {
        IndexingErrorCategory.RateLimit => "Wait a few minutes and retry",
        IndexingErrorCategory.NetworkError => "Check your internet connection",
        IndexingErrorCategory.ServiceUnavailable => "The embedding service is temporarily unavailable",
        IndexingErrorCategory.InvalidContent => "The document may contain invalid characters",
        IndexingErrorCategory.FileTooLarge => "Split the document into smaller files",
        IndexingErrorCategory.FileNotFound => "The file may have been moved or deleted",
        IndexingErrorCategory.PermissionDenied => "Check file permissions",
        _ => "Try re-indexing the document"
    };
}

/// <summary>
/// Categories of indexing errors.
/// </summary>
public enum IndexingErrorCategory
{
    Unknown,
    RateLimit,
    NetworkError,
    ServiceUnavailable,
    InvalidContent,
    FileTooLarge,
    FileNotFound,
    PermissionDenied,
    TokenLimitExceeded,
    ApiKeyInvalid
}
```

### 2.4 Error Categorization Service

```csharp
namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Categorizes indexing errors for user-friendly display.
/// </summary>
public static class IndexingErrorCategorizer
{
    public static IndexingErrorCategory Categorize(Exception ex)
    {
        return ex switch
        {
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => IndexingErrorCategory.RateLimit,
            HttpRequestException { StatusCode: HttpStatusCode.ServiceUnavailable } => IndexingErrorCategory.ServiceUnavailable,
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => IndexingErrorCategory.ApiKeyInvalid,
            HttpRequestException => IndexingErrorCategory.NetworkError,
            FileNotFoundException => IndexingErrorCategory.FileNotFound,
            UnauthorizedAccessException => IndexingErrorCategory.PermissionDenied,
            TokenLimitExceededException => IndexingErrorCategory.TokenLimitExceeded,
            _ when ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) => IndexingErrorCategory.RateLimit,
            _ when ex.Message.Contains("too large", StringComparison.OrdinalIgnoreCase) => IndexingErrorCategory.FileTooLarge,
            _ => IndexingErrorCategory.Unknown
        };
    }

    public static string GetUserFriendlyMessage(Exception ex, IndexingErrorCategory category)
    {
        return category switch
        {
            IndexingErrorCategory.RateLimit => "Rate limit exceeded. Please wait before retrying.",
            IndexingErrorCategory.NetworkError => "Network error. Check your connection.",
            IndexingErrorCategory.ServiceUnavailable => "Embedding service unavailable. Try again later.",
            IndexingErrorCategory.ApiKeyInvalid => "API key is invalid or expired.",
            IndexingErrorCategory.FileNotFound => "File not found. It may have been moved or deleted.",
            IndexingErrorCategory.PermissionDenied => "Permission denied. Check file access rights.",
            IndexingErrorCategory.TokenLimitExceeded => "Document exceeds token limit.",
            IndexingErrorCategory.FileTooLarge => "File is too large to process.",
            IndexingErrorCategory.InvalidContent => "Document contains invalid content.",
            _ => $"Indexing failed: {ex.Message}"
        };
    }
}
```

### 2.5 DocumentIndexingFailedEvent Handler

```csharp
namespace Lexichord.Modules.RAG.Handlers;

/// <summary>
/// Handles failed indexing events by recording errors to the database.
/// </summary>
public class DocumentIndexingFailedHandler : INotificationHandler<DocumentIndexingFailedEvent>
{
    private readonly IDocumentRepository _documentRepo;
    private readonly ILogger<DocumentIndexingFailedHandler> _logger;

    public DocumentIndexingFailedHandler(
        IDocumentRepository documentRepo,
        ILogger<DocumentIndexingFailedHandler> logger)
    {
        _documentRepo = documentRepo;
        _logger = logger;
    }

    public async Task Handle(DocumentIndexingFailedEvent notification, CancellationToken ct)
    {
        var document = await _documentRepo.GetByPathAsync(notification.FilePath);

        if (document == null)
        {
            // Create placeholder document record for tracking
            document = new Document
            {
                Id = Guid.NewGuid(),
                FilePath = notification.FilePath
            };
        }

        var category = IndexingErrorCategorizer.Categorize(notification.Exception);
        var message = IndexingErrorCategorizer.GetUserFriendlyMessage(notification.Exception, category);

        document.RecordError(message);
        await _documentRepo.UpsertAsync(document);

        _logger.LogWarning(
            notification.Exception,
            "Document indexing failed: {Path}, Category: {Category}, Message: {Message}, RetryCount: {RetryCount}",
            notification.FilePath,
            category,
            message,
            document.RetryCount);
    }
}
```

### 2.6 Repository Updates

```csharp
// Add to IDocumentRepository
public interface IDocumentRepository
{
    // ... existing methods ...

    /// <summary>
    /// Gets all documents with errors.
    /// </summary>
    Task<IReadOnlyList<Document>> GetFailedDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the error state for a document.
    /// </summary>
    Task ClearErrorAsync(Guid documentId, CancellationToken ct = default);
}

// Implementation
public async Task<IReadOnlyList<Document>> GetFailedDocumentsAsync(CancellationToken ct = default)
{
    const string sql = @"
        SELECT * FROM documents
        WHERE error_message IS NOT NULL
        ORDER BY error_timestamp DESC";

    using var conn = await _connectionFactory.CreateConnectionAsync();
    var results = await conn.QueryAsync<Document>(sql);
    return results.ToList();
}

public async Task ClearErrorAsync(Guid documentId, CancellationToken ct = default)
{
    const string sql = @"
        UPDATE documents
        SET error_message = NULL, error_timestamp = NULL
        WHERE id = @Id";

    using var conn = await _connectionFactory.CreateConnectionAsync();
    await conn.ExecuteAsync(sql, new { Id = documentId });
}
```

### 2.7 UI Integration

```xml
<!-- Error display in IndexedDocumentViewModel item template -->
<StackPanel IsVisible="{Binding HasError}" Margin="0,4,0,0">
    <!-- Error Badge -->
    <Border Background="{DynamicResource ErrorBackground}"
            CornerRadius="4"
            Padding="8,4">
        <Grid ColumnDefinitions="Auto,*,Auto">
            <PathIcon Grid.Column="0"
                      Data="{StaticResource ErrorIcon}"
                      Width="14" Height="14"
                      Foreground="{DynamicResource ErrorForeground}"
                      Margin="0,0,8,0"/>

            <TextBlock Grid.Column="1"
                       Text="{Binding ErrorMessage}"
                       Foreground="{DynamicResource ErrorForeground}"
                       FontSize="12"
                       TextWrapping="Wrap"/>

            <Button Grid.Column="2"
                    Content="Retry"
                    Command="{Binding $parent[ItemsControl].DataContext.RetryDocumentCommand}"
                    CommandParameter="{Binding Id}"
                    Classes="text-button"
                    Margin="8,0,0,0"/>
        </Grid>
    </Border>

    <!-- Retry Info -->
    <TextBlock IsVisible="{Binding RetryCount, Converter={StaticResource GreaterThanZeroConverter}}"
               FontSize="11"
               Foreground="{DynamicResource SecondaryTextBrush}"
               Margin="0,4,0,0">
        <Run Text="Attempted"/>
        <Run Text="{Binding RetryCount}"/>
        <Run Text="time(s)"/>
    </TextBlock>
</StackPanel>
```

---

## 3. Error Display Specifications

### 3.1 Failed Document Row

```
┌─────────────────────────────────────────────────────────────────┐
│ 📄 research.md                   [Failed]     3h ago    [↻][✕] │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ⚠ Rate limit exceeded. Please wait before retrying. [Retry]│ │
│ └─────────────────────────────────────────────────────────────┘ │
│   Attempted 2 time(s)                                           │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Error Categories and Colors

| Category | Icon | Color | Background |
| :------- | :--- | :---- | :--------- |
| Rate Limit | ⏱ | Yellow | #fef3c7 |
| Network | 🌐 | Orange | #ffedd5 |
| API Key | 🔑 | Red | #fee2e2 |
| File Not Found | 📁 | Gray | #f3f4f6 |
| Permission | 🔒 | Red | #fee2e2 |
| Unknown | ⚠ | Red | #fee2e2 |

---

## 4. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7d")]
public class IndexingErrorCategorizerTests
{
    [Fact]
    public void Categorize_RateLimitException_ReturnsRateLimit()
    {
        var ex = new HttpRequestException("Too many requests", null, HttpStatusCode.TooManyRequests);

        var category = IndexingErrorCategorizer.Categorize(ex);

        category.Should().Be(IndexingErrorCategory.RateLimit);
    }

    [Fact]
    public void Categorize_FileNotFoundException_ReturnsFileNotFound()
    {
        var ex = new FileNotFoundException("File not found");

        var category = IndexingErrorCategorizer.Categorize(ex);

        category.Should().Be(IndexingErrorCategory.FileNotFound);
    }

    [Fact]
    public void Categorize_UnauthorizedAccess_ReturnsPermissionDenied()
    {
        var ex = new UnauthorizedAccessException("Access denied");

        var category = IndexingErrorCategorizer.Categorize(ex);

        category.Should().Be(IndexingErrorCategory.PermissionDenied);
    }

    [Theory]
    [InlineData("rate limit exceeded", IndexingErrorCategory.RateLimit)]
    [InlineData("file is too large", IndexingErrorCategory.FileTooLarge)]
    [InlineData("random error", IndexingErrorCategory.Unknown)]
    public void Categorize_MessagePatterns_MatchesCorrectly(string message, IndexingErrorCategory expected)
    {
        var ex = new Exception(message);

        var category = IndexingErrorCategorizer.Categorize(ex);

        category.Should().Be(expected);
    }

    [Theory]
    [InlineData(IndexingErrorCategory.RateLimit, true)]
    [InlineData(IndexingErrorCategory.NetworkError, true)]
    [InlineData(IndexingErrorCategory.ServiceUnavailable, true)]
    [InlineData(IndexingErrorCategory.FileNotFound, false)]
    [InlineData(IndexingErrorCategory.ApiKeyInvalid, false)]
    public void IsRetryable_ReturnsCorrectValue(IndexingErrorCategory category, bool expected)
    {
        var info = new IndexingErrorInfo
        {
            DocumentId = Guid.NewGuid(),
            FilePath = "/test.md",
            Message = "Test error",
            Category = category
        };

        info.IsRetryable.Should().Be(expected);
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7d")]
public class DocumentTests
{
    [Fact]
    public void RecordError_SetsErrorFields()
    {
        var doc = new Document { Id = Guid.NewGuid(), FilePath = "/test.md" };

        doc.RecordError("Test error message");

        doc.ErrorMessage.Should().Be("Test error message");
        doc.ErrorTimestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        doc.RetryCount.Should().Be(1);
    }

    [Fact]
    public void RecordError_IncrementsRetryCount()
    {
        var doc = new Document { Id = Guid.NewGuid(), FilePath = "/test.md" };

        doc.RecordError("Error 1");
        doc.RecordError("Error 2");

        doc.RetryCount.Should().Be(2);
    }

    [Fact]
    public void RecordError_TruncatesLongMessages()
    {
        var doc = new Document { Id = Guid.NewGuid(), FilePath = "/test.md" };
        var longMessage = new string('x', 3000);

        doc.RecordError(longMessage);

        doc.ErrorMessage.Should().HaveLength(2000);
    }

    [Fact]
    public void ClearError_ResetsErrorFields()
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FilePath = "/test.md",
            ErrorMessage = "Some error",
            ErrorTimestamp = DateTimeOffset.UtcNow,
            RetryCount = 3
        };

        doc.ClearError();

        doc.ErrorMessage.Should().BeNull();
        doc.ErrorTimestamp.Should().BeNull();
        doc.RetryCount.Should().Be(3); // Preserved for analytics
    }

    [Fact]
    public void HasError_TrueWhenErrorMessageSet()
    {
        var doc = new Document { ErrorMessage = "Error" };
        doc.HasError.Should().BeTrue();
    }

    [Fact]
    public void HasError_FalseWhenErrorMessageNull()
    {
        var doc = new Document { ErrorMessage = null };
        doc.HasError.Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.7d")]
public class DocumentIndexingFailedHandlerTests
{
    [Fact]
    public async Task Handle_RecordsErrorToDocument()
    {
        // Arrange
        var docRepoMock = new Mock<IDocumentRepository>();
        var existingDoc = new Document { Id = Guid.NewGuid(), FilePath = "/test.md" };
        docRepoMock.Setup(r => r.GetByPathAsync("/test.md")).ReturnsAsync(existingDoc);

        var handler = new DocumentIndexingFailedHandler(
            docRepoMock.Object,
            NullLogger<DocumentIndexingFailedHandler>.Instance);

        var evt = new DocumentIndexingFailedEvent
        {
            FilePath = "/test.md",
            Exception = new HttpRequestException("Rate limit", null, HttpStatusCode.TooManyRequests)
        };

        // Act
        await handler.Handle(evt, CancellationToken.None);

        // Assert
        docRepoMock.Verify(r => r.UpsertAsync(It.Is<Document>(d =>
            d.HasError &&
            d.ErrorMessage!.Contains("Rate limit"))), Times.Once);
    }
}
```

---

## 5. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Warning | "Document indexing failed: {Path}, Category: {Category}, Message: {Message}" | On failure |
| Debug | "Clearing error for document: {Id}" | Before clear |
| Information | "Retrying failed document: {Path}" | On retry |

---

## 6. File Locations

| File | Path |
| :--- | :--- |
| Migration | `src/Lexichord.Modules.RAG/Migrations/Migration_004_AddErrorMessageColumn.cs` |
| IndexingErrorInfo | `src/Lexichord.Modules.RAG/Models/IndexingErrorInfo.cs` |
| IndexingErrorCategorizer | `src/Lexichord.Modules.RAG/Services/IndexingErrorCategorizer.cs` |
| Handler | `src/Lexichord.Modules.RAG/Handlers/DocumentIndexingFailedHandler.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Services/IndexingErrorCategorizerTests.cs` |

---

## 7. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Migration adds error_message column | [ ] |
| 2 | Failed documents have error recorded | [ ] |
| 3 | Error message displays in UI | [ ] |
| 4 | Error category determined correctly | [ ] |
| 5 | Retry count tracks attempts | [ ] |
| 6 | Retry button re-indexes document | [ ] |
| 7 | Successful retry clears error | [ ] |
| 8 | IsRetryable identifies transient errors | [ ] |
| 9 | SuggestedAction provides helpful guidance | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 8. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
