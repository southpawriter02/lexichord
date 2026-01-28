using Lexichord.Abstractions.Messaging;
using Lexichord.Host.Infrastructure.Behaviors;
using Lexichord.Host.Infrastructure.Options;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Unit.Host.Behaviors;

/// <summary>
/// Unit tests for LoggingBehavior pipeline behavior.
/// </summary>
[Trait("Category", "Unit")]
public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_LogsRequestStart()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestLoggingCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions());
        var behavior = new LoggingBehavior<TestLoggingCommand, string>(logger, options);
        var request = new TestLoggingCommand { Value = "test" };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("Handling") &&
            log.Message.Contains("TestLoggingCommand"));
    }

    [Fact]
    public async Task Handle_LogsRequestCompletion()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestLoggingCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions());
        var behavior = new LoggingBehavior<TestLoggingCommand, string>(logger, options);
        var request = new TestLoggingCommand { Value = "test" };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Information &&
            log.Message.Contains("Handled") &&
            log.Message.Contains("ms"));
    }

    [Fact]
    public async Task Handle_WarnsOnSlowRequest()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestLoggingCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions { SlowRequestThresholdMs = 10 });
        var behavior = new LoggingBehavior<TestLoggingCommand, string>(logger, options);
        var request = new TestLoggingCommand { Value = "test" };

        // Act
        await behavior.Handle(request, async () =>
        {
            await Task.Delay(50);
            return "result";
        }, CancellationToken.None);

        // Assert
        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Warning &&
            log.Message.Contains("Slow request"));
    }

    [Fact]
    public async Task Handle_LogsExceptionOnFailure()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestLoggingCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions());
        var behavior = new LoggingBehavior<TestLoggingCommand, string>(logger, options);
        var request = new TestLoggingCommand { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.Handle(request, () =>
                throw new InvalidOperationException("Test error"),
                CancellationToken.None));

        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Warning &&
            log.Message.Contains("failed") &&
            log.Message.Contains("Test error"));
    }

    [Fact]
    public async Task Handle_SkipsExcludedRequestTypes()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestLoggingCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions
        {
            ExcludedRequestTypes = new List<string> { "TestLoggingCommand" }
        });
        var behavior = new LoggingBehavior<TestLoggingCommand, string>(logger, options);
        var request = new TestLoggingCommand { Value = "test" };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        logger.Logs.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IncludesCorrelationIdWhenPresent()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestCommandWithCorrelation, string>>();
        var options = Options.Create(new LoggingBehaviorOptions());
        var behavior = new LoggingBehavior<TestCommandWithCorrelation, string>(logger, options);
        var request = new TestCommandWithCorrelation
        {
            Value = "test",
            CorrelationId = "test-correlation-123"
        };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("test-correlation-123"));
    }

    [Fact]
    public async Task Handle_ReturnsResponseFromHandler()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<TestLoggingCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions());
        var behavior = new LoggingBehavior<TestLoggingCommand, string>(logger, options);
        var request = new TestLoggingCommand { Value = "test" };

        // Act
        var result = await behavior.Handle(request, () => Task.FromResult("expected-result"), CancellationToken.None);

        // Assert
        result.Should().Be("expected-result");
    }
}

// Test commands for LoggingBehavior tests
public record TestLoggingCommand : ICommand<string>
{
    public string Value { get; init; } = string.Empty;
}

public record TestCommandWithCorrelation : ICommand<string>
{
    public string Value { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
