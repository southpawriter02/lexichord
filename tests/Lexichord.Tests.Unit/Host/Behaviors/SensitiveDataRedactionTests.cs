using Lexichord.Abstractions.Attributes;
using Lexichord.Abstractions.Messaging;
using Lexichord.Host.Infrastructure.Behaviors;
using Lexichord.Host.Infrastructure.Options;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Unit.Host.Behaviors;

/// <summary>
/// Unit tests for sensitive data redaction in LoggingBehavior.
/// </summary>
[Trait("Category", "Unit")]
public class SensitiveDataRedactionTests
{
    [Fact]
    public async Task Handle_RedactsSensitiveProperties()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<SensitiveCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions { LogRequestProperties = true });
        var behavior = new LoggingBehavior<SensitiveCommand, string>(logger, options);
        var request = new SensitiveCommand
        {
            Username = "john",
            Password = "secret123"
        };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        var requestLog = logger.Logs.First(l => l.Message.Contains("Request:"));
        requestLog.Message.Should().Contain("john");
        requestLog.Message.Should().NotContain("secret123");
        requestLog.Message.Should().Contain("[REDACTED]");
    }

    [Fact]
    public async Task Handle_RedactsPropertiesWithSensitiveNames()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<AutoRedactCommand, string>>();
        var options = Options.Create(new LoggingBehaviorOptions { LogRequestProperties = true });
        var behavior = new LoggingBehavior<AutoRedactCommand, string>(logger, options);
        var request = new AutoRedactCommand
        {
            Name = "test",
            ApiKey = "sk-1234567890"
        };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        var requestLog = logger.Logs.First(l => l.Message.Contains("Request:"));
        requestLog.Message.Should().Contain("test");
        requestLog.Message.Should().NotContain("sk-1234567890");
    }

    [Fact]
    public async Task Handle_ExcludesNoLogProperties()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<CommandWithNoLog, string>>();
        var options = Options.Create(new LoggingBehaviorOptions { LogRequestProperties = true });
        var behavior = new LoggingBehavior<CommandWithNoLog, string>(logger, options);
        var request = new CommandWithNoLog
        {
            FileName = "document.pdf",
            FileContents = new byte[] { 1, 2, 3, 4, 5 }
        };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        var requestLog = logger.Logs.First(l => l.Message.Contains("Request:"));
        requestLog.Message.Should().Contain("document.pdf");
        requestLog.Message.Should().NotContain("FileContents");
    }

    [Fact]
    public async Task Handle_TruncatesLongStrings()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<CommandWithLongString, string>>();
        var options = Options.Create(new LoggingBehaviorOptions { LogRequestProperties = true });
        var behavior = new LoggingBehavior<CommandWithLongString, string>(logger, options);
        var longContent = new string('x', 200);
        var request = new CommandWithLongString
        {
            Title = "Short Title",
            Content = longContent
        };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        var requestLog = logger.Logs.First(l => l.Message.Contains("Request:"));
        requestLog.Message.Should().Contain("Short Title");
        requestLog.Message.Should().Contain("...");
        requestLog.Message.Should().NotContain(longContent);
    }

    [Fact]
    public async Task Handle_UsesCustomRedactedText()
    {
        // Arrange
        var logger = new FakeLogger<LoggingBehavior<CommandWithCustomRedaction, string>>();
        var options = Options.Create(new LoggingBehaviorOptions { LogRequestProperties = true });
        var behavior = new LoggingBehavior<CommandWithCustomRedaction, string>(logger, options);
        var request = new CommandWithCustomRedaction
        {
            PublicInfo = "visible",
            SecretToken = "my-secret-token"
        };

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), CancellationToken.None);

        // Assert
        var requestLog = logger.Logs.First(l => l.Message.Contains("Request:"));
        requestLog.Message.Should().Contain("visible");
        requestLog.Message.Should().Contain("[API_TOKEN]");
        requestLog.Message.Should().NotContain("my-secret-token");
    }
}

// Test commands for sensitive data tests
public record SensitiveCommand : ICommand<string>
{
    public string Username { get; init; } = string.Empty;

    [SensitiveData]
    public string Password { get; init; } = string.Empty;
}

public record AutoRedactCommand : ICommand<string>
{
    public string Name { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty; // Auto-redacted by name
}

public record CommandWithNoLog : ICommand<string>
{
    public string FileName { get; init; } = string.Empty;

    [NoLog]
    public byte[] FileContents { get; init; } = Array.Empty<byte>();
}

public record CommandWithLongString : ICommand<string>
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public record CommandWithCustomRedaction : ICommand<string>
{
    public string PublicInfo { get; init; } = string.Empty;

    [SensitiveData("[API_TOKEN]")]
    public string SecretToken { get; init; } = string.Empty;
}
