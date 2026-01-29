using Lexichord.Host.Infrastructure;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Unit.Host.Messaging;

/// <summary>
/// Unit tests for MediatR DI registration.
/// </summary>
[Trait("Category", "Unit")]
public class MediatRRegistrationTests
{
    [Fact]
    public void AddMediatRServices_RegistersIMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediatRServices();
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddMediatRServices_RegistersISender()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediatRServices();
        var provider = services.BuildServiceProvider();

        // Assert
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void AddMediatRServices_RegistersIPublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediatRServices();
        var provider = services.BuildServiceProvider();

        // Assert
        var publisher = provider.GetService<IPublisher>();
        publisher.Should().NotBeNull();
    }

    [Fact]
    public async Task AddMediatRServices_DiscoversHandlersInAdditionalAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        // Required for BindConfiguration() in AddMediatRServices
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        // Pass the test assembly to discover test handlers
        var testAssembly = typeof(TestCommand).Assembly;
        services.AddMediatRServices(testAssembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert - Send a test command and verify it is handled
        var result = await mediator.Send(new TestCommand { Value = "test" });
        result.Should().Be("Handled: test");
    }

    [Fact]
    public async Task AddMediatRServices_SupportsTestQuery()
    {
        // Arrange
        var services = new ServiceCollection();
        // Required for BindConfiguration() in AddMediatRServices
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        // Pass the test assembly to discover test handlers
        var testAssembly = typeof(TestQuery).Assembly;
        services.AddMediatRServices(testAssembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new TestQuery { Id = 42 });

        // Assert
        result.Should().Be("Query result for ID: 42");
    }
}

// Test command and handler for verification
public record TestCommand : Lexichord.Abstractions.Messaging.ICommand<string>
{
    public string Value { get; init; } = string.Empty;
}

public class TestCommandHandler : IRequestHandler<TestCommand, string>
{
    public Task<string> Handle(TestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Handled: {request.Value}");
    }
}

// Test query and handler for verification
public record TestQuery : Lexichord.Abstractions.Messaging.IQuery<string>
{
    public int Id { get; init; }
}

public class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public Task<string> Handle(TestQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Query result for ID: {request.Id}");
    }
}
