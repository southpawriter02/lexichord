using Lexichord.Abstractions.Events;
using Lexichord.Abstractions.Messaging;

namespace Lexichord.Tests.Unit.Abstractions.Events;

/// <summary>
/// Unit tests for <see cref="DomainEventBase"/> abstract record.
/// </summary>
[Trait("Category", "Unit")]
public class DomainEventBaseTests
{
    /// <summary>
    /// Concrete implementation for testing the abstract base class.
    /// </summary>
    private record TestDomainEvent : DomainEventBase
    {
        public string TestProperty { get; init; } = string.Empty;
    }

    [Fact]
    public void DomainEventBase_AutoGeneratesEventId()
    {
        // Arrange & Act
        var evt = new TestDomainEvent();

        // Assert
        evt.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void DomainEventBase_AutoGeneratesOccurredAt()
    {
        // Arrange & Act
        var evt = new TestDomainEvent();

        // Assert
        evt.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DomainEventBase_CorrelationIdIsNullByDefault()
    {
        // Arrange & Act
        var evt = new TestDomainEvent();

        // Assert
        evt.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void DomainEventBase_CanSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var evt = new TestDomainEvent { CorrelationId = correlationId };

        // Assert
        evt.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void DomainEventBase_CanOverrideEventId()
    {
        // Arrange
        var specificId = Guid.NewGuid();

        // Act
        var evt = new TestDomainEvent { EventId = specificId };

        // Assert
        evt.EventId.Should().Be(specificId);
    }

    [Fact]
    public void DomainEventBase_CanOverrideOccurredAt()
    {
        // Arrange
        var specificTime = new DateTimeOffset(2026, 1, 28, 12, 0, 0, TimeSpan.Zero);

        // Act
        var evt = new TestDomainEvent { OccurredAt = specificTime };

        // Assert
        evt.OccurredAt.Should().Be(specificTime);
    }

    [Fact]
    public void DomainEventBase_ImplementsIDomainEvent()
    {
        // Assert
        typeof(DomainEventBase).Should().Implement<IDomainEvent>();
    }

    [Fact]
    public void DomainEventBase_EachInstanceGetsUniqueEventId()
    {
        // Arrange & Act
        var evt1 = new TestDomainEvent();
        var evt2 = new TestDomainEvent();

        // Assert
        evt1.EventId.Should().NotBe(evt2.EventId);
    }

    [Fact]
    public void DomainEventBase_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var correlationId = "test-correlation";

        var evt1 = new TestDomainEvent
        {
            EventId = eventId,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            TestProperty = "test"
        };

        var evt2 = new TestDomainEvent
        {
            EventId = eventId,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            TestProperty = "test"
        };

        var evt3 = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            TestProperty = "test"
        };

        // Assert
        evt1.Should().Be(evt2);
        evt1.Should().NotBe(evt3);
    }
}
