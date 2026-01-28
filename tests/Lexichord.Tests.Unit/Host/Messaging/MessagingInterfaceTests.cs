using Lexichord.Abstractions.Messaging;
using MediatR;

namespace Lexichord.Tests.Unit.Host.Messaging;

/// <summary>
/// Unit tests for messaging interface contracts.
/// </summary>
[Trait("Category", "Unit")]
public class MessagingInterfaceTests
{
    [Fact]
    public void ICommand_InheritsFromIRequest()
    {
        // Assert
        typeof(ICommand<string>).Should().Implement<IRequest<string>>();
    }

    [Fact]
    public void ICommand_WithoutResponse_InheritsFromIRequest()
    {
        // Assert
        typeof(ICommand).Should().Implement<IRequest<MediatR.Unit>>();
    }

    [Fact]
    public void ICommand_WithoutResponse_InheritsFromICommandOfUnit()
    {
        // Assert
        typeof(ICommand).Should().Implement<ICommand<MediatR.Unit>>();
    }

    [Fact]
    public void IQuery_InheritsFromIRequest()
    {
        // Assert
        typeof(IQuery<string>).Should().Implement<IRequest<string>>();
    }

    [Fact]
    public void IDomainEvent_InheritsFromINotification()
    {
        // Assert
        typeof(IDomainEvent).Should().Implement<INotification>();
    }

    [Fact]
    public void IDomainEvent_HasEventIdProperty()
    {
        // Assert
        var property = typeof(IDomainEvent).GetProperty("EventId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void IDomainEvent_HasOccurredAtProperty()
    {
        // Assert
        var property = typeof(IDomainEvent).GetProperty("OccurredAt");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(DateTimeOffset));
    }

    [Fact]
    public void IDomainEvent_HasCorrelationIdProperty()
    {
        // Assert
        var property = typeof(IDomainEvent).GetProperty("CorrelationId");
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void TestDomainEvent_CanBeCreated()
    {
        // Arrange & Act
        var @event = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = "test-correlation-id",
            TestData = "Hello"
        };

        // Assert
        @event.EventId.Should().NotBe(Guid.Empty);
        @event.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        @event.CorrelationId.Should().Be("test-correlation-id");
        @event.TestData.Should().Be("Hello");
    }

    [Fact]
    public void TestDomainEvent_CorrelationIdCanBeNull()
    {
        // Arrange & Act
        var @event = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = null,
            TestData = "Test"
        };

        // Assert
        @event.CorrelationId.Should().BeNull();
    }
}

// Test domain event for verification
public record TestDomainEvent : IDomainEvent
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
    public required string TestData { get; init; }
}
