using FluentValidation;
using Lexichord.Abstractions.Messaging;
using Lexichord.Host.Infrastructure;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using LexichordValidation = Lexichord.Abstractions.Validation;

namespace Lexichord.Tests.Integration.MediatR;

/// <summary>
/// Integration tests for the validation pipeline behavior.
/// </summary>
/// <remarks>
/// These tests verify that the validation behavior correctly integrates with
/// MediatR and FluentValidation in a real DI container setup.
/// </remarks>
[Trait("Category", "Integration")]
public class ValidationPipelineIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public ValidationPipelineIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add configuration (required by LoggingBehavior for options binding)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Add logging
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        // Add MediatR with validation behavior using the host's extension method
        services.AddMediatRServices(typeof(ValidationPipelineIntegrationTests).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MediatR_WithInvalidRequest_ThrowsValidationException()
    {
        // Arrange - create a request that violates validation rules
        var invalidRequest = new TestIntegrationCommand { Name = "" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LexichordValidation.ValidationException>(
            () => _mediator.Send(invalidRequest));

        exception.Errors.Should().ContainSingle();
        exception.Errors.First().PropertyName.Should().Be("Name");
        exception.Errors.First().ErrorCode.Should().Be("NAME_REQUIRED");
    }

    [Fact]
    public async Task MediatR_WithValidRequest_PassesThroughToHandler()
    {
        // Arrange
        var validRequest = new TestIntegrationCommand { Name = "Valid Name" };

        // Act
        var result = await _mediator.Send(validRequest);

        // Assert
        result.Should().Be("Handled: Valid Name");
    }

    [Fact]
    public void MediatR_ValidatorsAreAutoDiscovered()
    {
        // Arrange - the validator should be auto-discovered from this assembly
        var validators = _serviceProvider.GetServices<IValidator<TestIntegrationCommand>>().ToList();

        // Assert
        validators.Should().HaveCount(1);
        validators.First().Should().BeOfType<TestIntegrationCommandValidator>();
    }

    [Fact]
    public async Task MediatR_WithMultipleValidationErrors_AggregatesAll()
    {
        // Arrange - request with multiple validation failures
        var invalidRequest = new TestMultipleErrorsCommand
        {
            RequiredField = "",
            PositiveNumber = -5
        };

        // Act
        var exception = await Assert.ThrowsAsync<LexichordValidation.ValidationException>(
            () => _mediator.Send(invalidRequest));

        // Assert - should have errors from both validation rules
        exception.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        exception.Errors.Select(e => e.PropertyName).Should().Contain("RequiredField");
        exception.Errors.Select(e => e.PropertyName).Should().Contain("PositiveNumber");
    }
}

#region Test Commands, Handlers, and Validators

/// <summary>
/// Test command for validation integration tests.
/// </summary>
public record TestIntegrationCommand : ICommand<string>
{
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Handler for TestIntegrationCommand.
/// </summary>
public sealed class TestIntegrationCommandHandler : IRequestHandler<TestIntegrationCommand, string>
{
    public Task<string> Handle(TestIntegrationCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Name}");
    }
}

/// <summary>
/// Validator for TestIntegrationCommand - demonstrates auto-discovery.
/// </summary>
public sealed class TestIntegrationCommandValidator : AbstractValidator<TestIntegrationCommand>
{
    public TestIntegrationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
                .WithErrorCode("NAME_REQUIRED");
    }
}

/// <summary>
/// Test command for multiple validation errors.
/// </summary>
public record TestMultipleErrorsCommand : ICommand<string>
{
    public string RequiredField { get; init; } = string.Empty;
    public int PositiveNumber { get; init; }
}

/// <summary>
/// Handler for TestMultipleErrorsCommand.
/// </summary>
public sealed class TestMultipleErrorsCommandHandler : IRequestHandler<TestMultipleErrorsCommand, string>
{
    public Task<string> Handle(TestMultipleErrorsCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Success");
    }
}

/// <summary>
/// Validator for TestMultipleErrorsCommand with multiple rules.
/// </summary>
public sealed class TestMultipleErrorsCommandValidator : AbstractValidator<TestMultipleErrorsCommand>
{
    public TestMultipleErrorsCommandValidator()
    {
        RuleFor(x => x.RequiredField)
            .NotEmpty()
                .WithMessage("Required field is required.")
                .WithErrorCode("REQUIRED_FIELD_EMPTY");

        RuleFor(x => x.PositiveNumber)
            .GreaterThan(0)
                .WithMessage("Number must be positive.")
                .WithErrorCode("NUMBER_NOT_POSITIVE");
    }
}

#endregion
