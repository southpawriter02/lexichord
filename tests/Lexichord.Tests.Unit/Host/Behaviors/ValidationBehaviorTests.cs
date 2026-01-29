using FluentValidation;
using FluentValidation.Results;
using Lexichord.Abstractions.Messaging;
using Lexichord.Host.Infrastructure.Behaviors;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;

using LexichordValidation = Lexichord.Abstractions.Validation;

namespace Lexichord.Tests.Unit.Host.Behaviors;

/// <summary>
/// Unit tests for ValidationBehavior pipeline behavior.
/// </summary>
[Trait("Category", "Unit")]
public class ValidationBehaviorTests
{
    #region Test Infrastructure

    private static FakeLogger<ValidationBehavior<TestValidationCommand, string>> CreateLogger()
        => new();

    private static ValidationBehavior<TestValidationCommand, string> CreateBehavior(
        IEnumerable<IValidator<TestValidationCommand>> validators,
        ILogger<ValidationBehavior<TestValidationCommand, string>>? logger = null)
        => new(validators, logger ?? CreateLogger());

    #endregion

    [Fact]
    public async Task Handle_WithNoValidators_PassesThrough()
    {
        // Arrange
        var logger = CreateLogger();
        var behavior = CreateBehavior(Enumerable.Empty<IValidator<TestValidationCommand>>(), logger);
        var request = new TestValidationCommand { Title = "Test" };
        var expectedResult = "handler-result";

        // Act
        var result = await behavior.Handle(
            request,
            () => Task.FromResult(expectedResult),
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("No validators registered"));
    }

    [Fact]
    public async Task Handle_WithValidRequest_PassesThrough()
    {
        // Arrange
        var logger = CreateLogger();
        var validator = new PassingValidator();
        var behavior = CreateBehavior(new[] { validator }, logger);
        var request = new TestValidationCommand { Title = "ValidTitle" };
        var expectedResult = "handler-result";

        // Act
        var result = await behavior.Handle(
            request,
            () => Task.FromResult(expectedResult),
            CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Debug &&
            log.Message.Contains("Validation passed"));
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var logger = CreateLogger();
        var validator = new FailingValidator("Title", "Title is required.", "TITLE_REQUIRED");
        var behavior = CreateBehavior(new[] { validator }, logger);
        var request = new TestValidationCommand { Title = "" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LexichordValidation.ValidationException>(async () =>
            await behavior.Handle(
                request,
                () => Task.FromResult("should-not-reach"),
                CancellationToken.None));

        exception.Errors.Should().HaveCount(1);
        exception.Errors.First().PropertyName.Should().Be("Title");
        exception.Errors.First().ErrorMessage.Should().Be("Title is required.");
        exception.Errors.First().ErrorCode.Should().Be("TITLE_REQUIRED");

        logger.Logs.Should().Contain(log =>
            log.Level == LogLevel.Warning &&
            log.Message.Contains("Validation failed"));
    }

    [Fact]
    public async Task Handle_WithMultipleErrors_AggregatesAll()
    {
        // Arrange
        var logger = CreateLogger();
        var validator1 = new FailingValidator("Title", "Title is required.", "TITLE_REQUIRED");
        var validator2 = new FailingValidator("Content", "Content is too long.", "CONTENT_TOO_LONG");
        var behavior = CreateBehavior(new IValidator<TestValidationCommand>[] { validator1, validator2 }, logger);
        var request = new TestValidationCommand { Title = "", Content = new string('x', 1000) };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LexichordValidation.ValidationException>(async () =>
            await behavior.Handle(
                request,
                () => Task.FromResult("should-not-reach"),
                CancellationToken.None));

        // Assert
        exception.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        exception.Errors.Select(e => e.PropertyName).Should().Contain("Title");
        exception.Errors.Select(e => e.PropertyName).Should().Contain("Content");
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_AllAreExecuted()
    {
        // Arrange
        var logger = CreateLogger();
        var validator1 = new TrackingValidator();
        var validator2 = new TrackingValidator();
        var behavior = CreateBehavior(new IValidator<TestValidationCommand>[] { validator1, validator2 }, logger);
        var request = new TestValidationCommand { Title = "Test" };

        // Act
        await behavior.Handle(
            request,
            () => Task.FromResult("result"),
            CancellationToken.None);

        // Assert
        validator1.WasCalled.Should().BeTrue();
        validator2.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        var logger = CreateLogger();
        var validator = new CancellationTrackingValidator();
        var behavior = CreateBehavior(new[] { validator }, logger);
        var request = new TestValidationCommand { Title = "Test" };
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        // Act
        await behavior.Handle(request, () => Task.FromResult("result"), expectedToken);

        // Assert
        validator.ReceivedToken.Should().Be(expectedToken);
    }
}

#region Test Commands and Validators

/// <summary>
/// Test command for validation behavior tests.
/// </summary>
public record TestValidationCommand : ICommand<string>
{
    public string Title { get; init; } = string.Empty;
    public string? Content { get; init; }
}

/// <summary>
/// Validator that always passes.
/// </summary>
public class PassingValidator : AbstractValidator<TestValidationCommand>
{
    public PassingValidator()
    {
        // No rules - always passes
    }
}

/// <summary>
/// Validator that always fails with a configurable error.
/// </summary>
public class FailingValidator : AbstractValidator<TestValidationCommand>
{
    public FailingValidator(string propertyName, string errorMessage, string errorCode)
    {
        RuleFor(x => x)
            .Custom((_, context) =>
            {
                context.AddFailure(new ValidationFailure(propertyName, errorMessage)
                {
                    ErrorCode = errorCode
                });
            });
    }
}

/// <summary>
/// Validator that tracks whether it was called.
/// </summary>
public class TrackingValidator : AbstractValidator<TestValidationCommand>
{
    public bool WasCalled { get; private set; }

    public override Task<ValidationResult> ValidateAsync(
        ValidationContext<TestValidationCommand> context,
        CancellationToken cancellation = default)
    {
        WasCalled = true;
        return Task.FromResult(new ValidationResult());
    }
}

/// <summary>
/// Validator that tracks the cancellation token it received.
/// </summary>
public class CancellationTrackingValidator : AbstractValidator<TestValidationCommand>
{
    public CancellationToken ReceivedToken { get; private set; }

    public override Task<ValidationResult> ValidateAsync(
        ValidationContext<TestValidationCommand> context,
        CancellationToken cancellation = default)
    {
        ReceivedToken = cancellation;
        return Task.FromResult(new ValidationResult());
    }
}

#endregion
