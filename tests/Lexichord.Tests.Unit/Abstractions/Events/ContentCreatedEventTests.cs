using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Messaging;

namespace Lexichord.Tests.Unit.Abstractions.Events;

/// <summary>
/// Unit tests for <see cref="ContentCreatedEvent"/> record.
/// </summary>
[Trait("Category", "Unit")]
public class ContentCreatedEventTests
{
    [Fact]
    public void Constructor_WithRequiredProperties_CreatesValidEvent()
    {
        // Arrange & Act
        var evt = new ContentCreatedEvent
        {
            ContentId = "doc-123",
            ContentType = ContentType.Document,
            Title = "My Document",
            CreatedBy = "user-456"
        };

        // Assert
        evt.ContentId.Should().Be("doc-123");
        evt.ContentType.Should().Be(ContentType.Document);
        evt.Title.Should().Be("My Document");
        evt.CreatedBy.Should().Be("user-456");
        evt.EventId.Should().NotBeEmpty();
        evt.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Event_ImplementsIDomainEvent()
    {
        // Assert
        typeof(ContentCreatedEvent).Should().Implement<IDomainEvent>();
    }

    [Fact]
    public void Event_InheritsFromDomainEventBase()
    {
        // Assert
        typeof(ContentCreatedEvent).Should().BeDerivedFrom<DomainEventBase>();
    }

    [Fact]
    public void Event_IsImmutable()
    {
        // Arrange
        var evt = new ContentCreatedEvent
        {
            ContentId = "doc-123",
            ContentType = ContentType.Document,
            Title = "My Document",
            CreatedBy = "user-456"
        };

        // Act - Create new instance with modified property
        var modified = evt with { Title = "Modified Title" };

        // Assert - Original unchanged
        evt.Title.Should().Be("My Document");
        modified.Title.Should().Be("Modified Title");
    }

    [Fact]
    public void Event_WithOptionalDescription_StoresDescription()
    {
        // Arrange & Act
        var evt = new ContentCreatedEvent
        {
            ContentId = "doc-123",
            ContentType = ContentType.Document,
            Title = "My Document",
            CreatedBy = "user-456",
            Description = "A test document description"
        };

        // Assert
        evt.Description.Should().Be("A test document description");
    }

    [Fact]
    public void Event_WithMetadata_StoresMetadataDictionary()
    {
        // Arrange & Act
        var metadata = new Dictionary<string, string>
        {
            ["wordCount"] = "1500",
            ["language"] = "en-US"
        };

        var evt = new ContentCreatedEvent
        {
            ContentId = "doc-123",
            ContentType = ContentType.Document,
            Title = "My Document",
            CreatedBy = "user-456",
            Metadata = metadata
        };

        // Assert
        evt.Metadata.Should().NotBeNull();
        evt.Metadata.Should().HaveCount(2);
        evt.Metadata!["wordCount"].Should().Be("1500");
        evt.Metadata!["language"].Should().Be("en-US");
    }

    [Fact]
    public void Event_WithCorrelationId_FlowsCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new ContentCreatedEvent
        {
            ContentId = "doc-123",
            ContentType = ContentType.Document,
            Title = "My Document",
            CreatedBy = "user-456",
            CorrelationId = correlationId
        };

        // Assert
        evt.CorrelationId.Should().Be(correlationId);
    }

    [Theory]
    [InlineData(ContentType.Document)]
    [InlineData(ContentType.Chapter)]
    [InlineData(ContentType.Project)]
    [InlineData(ContentType.Note)]
    [InlineData(ContentType.Template)]
    [InlineData(ContentType.StyleGuide)]
    [InlineData(ContentType.WorldbuildingElement)]
    [InlineData(ContentType.Reference)]
    public void Event_WithDifferentContentTypes_StoresContentType(ContentType contentType)
    {
        // Arrange & Act
        var evt = new ContentCreatedEvent
        {
            ContentId = "test-123",
            ContentType = contentType,
            Title = "Test Content",
            CreatedBy = "test-user"
        };

        // Assert
        evt.ContentType.Should().Be(contentType);
    }
}
