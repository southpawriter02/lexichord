using Lexichord.Abstractions.Contracts.Ingestion;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Unit tests for the <see cref="IngestionQueueItem"/> record.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the immutability, factory methods, priority constants,
/// and validation logic of the queue item record.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2d")]
public class IngestionQueueItemTests
{
    #region Record Construction Tests

    [Fact]
    public void Constructor_ValidArguments_CreatesItem()
    {
        // Arrange
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var priority = 2;
        var enqueuedAt = DateTimeOffset.UtcNow;
        var correlationId = "correlation-123";

        // Act
        var item = new IngestionQueueItem(id, projectId, filePath, priority, enqueuedAt, correlationId);

        // Assert
        item.Id.Should().Be(id);
        item.ProjectId.Should().Be(projectId);
        item.FilePath.Should().Be(filePath);
        item.Priority.Should().Be(priority);
        item.EnqueuedAt.Should().Be(enqueuedAt);
        item.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Constructor_NullCorrelationId_IsAllowed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var priority = 2;
        var enqueuedAt = DateTimeOffset.UtcNow;

        // Act
        var item = new IngestionQueueItem(id, projectId, filePath, priority, enqueuedAt, null);

        // Assert
        item.CorrelationId.Should().BeNull();
    }

    #endregion

    #region Priority Constants Tests

    [Fact]
    public void PriorityConstants_HaveCorrectValues()
    {
        // Assert
        IngestionQueueItem.PriorityUserAction.Should().Be(0);
        IngestionQueueItem.PriorityRecentChange.Should().Be(1);
        IngestionQueueItem.PriorityNormal.Should().Be(2);
        IngestionQueueItem.PriorityBackground.Should().Be(3);
    }

    [Fact]
    public void PriorityConstants_AreOrderedCorrectly()
    {
        // Assert - lower values should be higher priority
        IngestionQueueItem.PriorityUserAction.Should().BeLessThan(IngestionQueueItem.PriorityRecentChange);
        IngestionQueueItem.PriorityRecentChange.Should().BeLessThan(IngestionQueueItem.PriorityNormal);
        IngestionQueueItem.PriorityNormal.Should().BeLessThan(IngestionQueueItem.PriorityBackground);
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Create_ValidArguments_CreatesItemWithNewId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/document.md";

        // Act
        var item = IngestionQueueItem.Create(projectId, filePath);

        // Assert
        item.Id.Should().NotBeEmpty();
        item.ProjectId.Should().Be(projectId);
        item.FilePath.Should().Be(filePath);
        item.Priority.Should().Be(IngestionQueueItem.PriorityNormal);
        item.EnqueuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        item.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_WithPriority_UsesSpecifiedPriority()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var priority = IngestionQueueItem.PriorityUserAction;

        // Act
        var item = IngestionQueueItem.Create(projectId, filePath, priority);

        // Assert
        item.Priority.Should().Be(priority);
    }

    [Fact]
    public void Create_WithCorrelationId_UsesSpecifiedId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var correlationId = "custom-correlation-id";

        // Act
        var item = IngestionQueueItem.Create(projectId, filePath, correlationId: correlationId);

        // Assert
        item.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void Create_NullFilePath_ThrowsArgumentException()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var act = () => IngestionQueueItem.Create(projectId, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void Create_EmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var act = () => IngestionQueueItem.Create(projectId, "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void Create_WhitespaceFilePath_ThrowsArgumentException()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var act = () => IngestionQueueItem.Create(projectId, "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void Create_NegativePriority_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";

        // Act
        var act = () => IngestionQueueItem.Create(projectId, filePath, priority: -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("priority");
    }

    [Fact]
    public void Create_ZeroPriority_IsAllowed()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";

        // Act
        var item = IngestionQueueItem.Create(projectId, filePath, priority: 0);

        // Assert
        item.Priority.Should().Be(0);
    }

    #endregion

    #region WithPriority Tests

    [Fact]
    public void WithPriority_ValidPriority_CreatesNewItemWithUpdatedPriority()
    {
        // Arrange
        var original = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md", priority: 2);

        // Act
        var updated = original.WithPriority(0);

        // Assert
        updated.Priority.Should().Be(0);
        updated.Id.Should().Be(original.Id);
        updated.ProjectId.Should().Be(original.ProjectId);
        updated.FilePath.Should().Be(original.FilePath);
        updated.EnqueuedAt.Should().Be(original.EnqueuedAt);
        updated.CorrelationId.Should().Be(original.CorrelationId);
    }

    [Fact]
    public void WithPriority_NegativePriority_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");

        // Act
        var act = () => item.WithPriority(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("newPriority");
    }

    [Fact]
    public void WithPriority_DoesNotModifyOriginal()
    {
        // Arrange
        var original = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md", priority: 2);
        var originalPriority = original.Priority;

        // Act
        _ = original.WithPriority(0);

        // Assert
        original.Priority.Should().Be(originalPriority);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var priority = 2;
        var enqueuedAt = DateTimeOffset.UtcNow;
        var correlationId = "corr-id";

        var item1 = new IngestionQueueItem(id, projectId, filePath, priority, enqueuedAt, correlationId);
        var item2 = new IngestionQueueItem(id, projectId, filePath, priority, enqueuedAt, correlationId);

        // Assert
        item1.Should().Be(item2);
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var priority = 2;
        var enqueuedAt = DateTimeOffset.UtcNow;

        var item1 = new IngestionQueueItem(Guid.NewGuid(), projectId, filePath, priority, enqueuedAt, null);
        var item2 = new IngestionQueueItem(Guid.NewGuid(), projectId, filePath, priority, enqueuedAt, null);

        // Assert
        item1.Should().NotBe(item2);
    }

    #endregion
}
