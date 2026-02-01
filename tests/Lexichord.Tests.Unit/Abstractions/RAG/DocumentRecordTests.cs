using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for the <see cref="Document"/> record.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify record equality semantics, with-expression support,
/// and the CreatePending factory method. They ensure the Document record behaves
/// correctly as an immutable data transfer object.
/// </remarks>
public class DocumentRecordTests
{
    private static readonly Guid TestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTime TestIndexedAt = new(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Document_WithSameValues_AreEqual()
    {
        // Arrange
        var doc1 = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/readme.md",
            Title: "README",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: TestIndexedAt,
            FailureReason: null);

        var doc2 = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/readme.md",
            Title: "README",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: TestIndexedAt,
            FailureReason: null);

        // Assert
        doc1.Should().Be(doc2, because: "records with identical values should be equal");
    }

    [Fact]
    public void Document_WithDifferentId_AreNotEqual()
    {
        // Arrange
        var doc1 = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/readme.md",
            Title: "README",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: TestIndexedAt,
            FailureReason: null);

        var doc2 = doc1 with { Id = Guid.NewGuid() };

        // Assert
        doc1.Should().NotBe(doc2, because: "documents with different IDs should not be equal");
    }

    [Fact]
    public void Document_SupportsWithExpression()
    {
        // Arrange
        var original = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/readme.md",
            Title: "README",
            Hash: "abc123",
            Status: DocumentStatus.Pending,
            IndexedAt: null,
            FailureReason: null);

        // Act
        var updated = original with
        {
            Status = DocumentStatus.Indexed,
            IndexedAt = TestIndexedAt
        };

        // Assert
        updated.Status.Should().Be(DocumentStatus.Indexed);
        updated.IndexedAt.Should().Be(TestIndexedAt);
        updated.FilePath.Should().Be(original.FilePath, because: "unchanged properties should be preserved");
    }

    [Fact]
    public void CreatePending_SetsCorrectDefaults()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "src/main.cs";
        var title = "Main Module";
        var hash = "sha256hash";

        // Act
        var document = Document.CreatePending(projectId, filePath, title, hash);

        // Assert
        document.Id.Should().Be(Guid.Empty, because: "database will assign the ID");
        document.ProjectId.Should().Be(projectId);
        document.FilePath.Should().Be(filePath);
        document.Title.Should().Be(title);
        document.Hash.Should().Be(hash);
        document.Status.Should().Be(DocumentStatus.Pending);
        document.IndexedAt.Should().BeNull(because: "not yet indexed");
        document.FailureReason.Should().BeNull(because: "no failure");
    }

    [Fact]
    public void Document_HasGetHashCode()
    {
        // Arrange
        var doc1 = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/readme.md",
            Title: "README",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: TestIndexedAt,
            FailureReason: null);

        var doc2 = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/readme.md",
            Title: "README",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: TestIndexedAt,
            FailureReason: null);

        // Assert
        doc1.GetHashCode().Should().Be(doc2.GetHashCode(),
            because: "equal records should have equal hash codes");
    }

    [Fact]
    public void Document_FailedStatus_CanHaveFailureReason()
    {
        // Arrange
        var document = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/broken.md",
            Title: "Broken Doc",
            Hash: "def456",
            Status: DocumentStatus.Failed,
            IndexedAt: null,
            FailureReason: "Embedding API timeout after 30s");

        // Assert
        document.Status.Should().Be(DocumentStatus.Failed);
        document.FailureReason.Should().NotBeNullOrEmpty();
        document.FailureReason.Should().Contain("timeout");
    }

    [Fact]
    public void Document_IndexedStatus_HasIndexedAt()
    {
        // Arrange
        var document = new Document(
            Id: TestId,
            ProjectId: TestProjectId,
            FilePath: "docs/success.md",
            Title: "Success Doc",
            Hash: "ghi789",
            Status: DocumentStatus.Indexed,
            IndexedAt: TestIndexedAt,
            FailureReason: null);

        // Assert
        document.Status.Should().Be(DocumentStatus.Indexed);
        document.IndexedAt.Should().NotBeNull();
        document.IndexedAt.Should().Be(TestIndexedAt);
    }
}
