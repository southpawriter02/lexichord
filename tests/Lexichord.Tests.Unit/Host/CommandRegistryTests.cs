using Avalonia.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for CommandRegistry.
/// </summary>
[Trait("Category", "Unit")]
public class CommandRegistryTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<CommandRegistry>> _loggerMock = new();

    private CommandRegistry CreateRegistry() => new(_mediatorMock.Object, _loggerMock.Object);

    private static CommandDefinition CreateValidCommand(
        string id = "test.command",
        string title = "Test Command",
        string category = "Test",
        Action<object?>? execute = null)
    {
        return new CommandDefinition(
            Id: id,
            Title: title,
            Category: category,
            DefaultShortcut: null,
            Execute: execute ?? (_ => { })
        );
    }

    #region Registration Tests

    [Fact]
    public void Register_ValidCommand_AddsToRegistry()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand();

        // Act
        registry.Register(command);

        // Assert
        registry.CommandCount.Should().Be(1);
        registry.HasCommand("test.command").Should().BeTrue();
    }

    [Fact]
    public void Register_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var act = () => registry.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_DuplicateId_ThrowsArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();
        var command1 = CreateValidCommand(id: "test.duplicate");
        var command2 = CreateValidCommand(id: "test.duplicate", title: "Different Title");
        registry.Register(command1);

        // Act
        var act = () => registry.Register(command2);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*already registered*");
    }

    [Fact]
    public void Register_InvalidCommand_ThrowsArgumentException()
    {
        // Arrange
        var registry = CreateRegistry();
        var invalidCommand = new CommandDefinition(
            Id: "",
            Title: "Test",
            Category: "Test",
            DefaultShortcut: null,
            Execute: _ => { }
        );

        // Act
        var act = () => registry.Register(invalidCommand);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_RaisesCommandRegisteredEvent()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand();
        CommandRegisteredEventArgs? capturedArgs = null;
        registry.CommandRegistered += (_, args) => capturedArgs = args;

        // Act
        registry.Register(command);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Command.Should().Be(command);
    }

    [Fact]
    public void Register_PublishesMediatREvent()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand();

        // Act
        registry.Register(command);

        // Assert - need to wait briefly for fire-and-forget
        Thread.Sleep(100);
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<CommandRegisteredMediatREvent>(e =>
                    e.CommandId == "test.command" &&
                    e.CommandTitle == "Test Command" &&
                    e.Category == "Test"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RegisterRange Tests

    [Fact]
    public void RegisterRange_ValidCommands_RegistersAll()
    {
        // Arrange
        var registry = CreateRegistry();
        var commands = new[]
        {
            CreateValidCommand(id: "test.one"),
            CreateValidCommand(id: "test.two"),
            CreateValidCommand(id: "test.three")
        };

        // Act
        registry.RegisterRange(commands);

        // Assert
        registry.CommandCount.Should().Be(3);
    }

    [Fact]
    public void RegisterRange_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var act = () => registry.RegisterRange(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterRange_WithDuplicate_ThrowsAndRegistersNone()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand(id: "test.existing"));

        var commands = new[]
        {
            CreateValidCommand(id: "test.new"),
            CreateValidCommand(id: "test.existing") // duplicate
        };

        // Act
        var act = () => registry.RegisterRange(commands);

        // Assert
        act.Should().Throw<ArgumentException>();
        registry.CommandCount.Should().Be(1); // only the pre-existing one
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Unregister_ExistingCommand_ReturnsTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand());

        // Act
        var result = registry.Unregister("test.command");

        // Assert
        result.Should().BeTrue();
        registry.CommandCount.Should().Be(0);
    }

    [Fact]
    public void Unregister_NonExisting_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.Unregister("nonexistent.command");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Unregister_RaisesCommandUnregisteredEvent()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand());
        CommandUnregisteredEventArgs? capturedArgs = null;
        registry.CommandUnregistered += (_, args) => capturedArgs = args;

        // Act
        registry.Unregister("test.command");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.CommandId.Should().Be("test.command");
    }

    #endregion

    #region Lookup Tests

    [Fact]
    public void GetAllCommands_ReturnsOrderedByCategory()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand(id: "test.z", category: "Zebra"));
        registry.Register(CreateValidCommand(id: "test.a", category: "Alpha"));
        registry.Register(CreateValidCommand(id: "test.m", category: "Mike"));

        // Act
        var commands = registry.GetAllCommands();

        // Assert
        commands.Should().HaveCount(3);
        commands[0].Category.Should().Be("Alpha");
        commands[1].Category.Should().Be("Mike");
        commands[2].Category.Should().Be("Zebra");
    }

    [Fact]
    public void GetCommandsByCategory_FiltersCorrectly()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand(id: "file.save", category: "File"));
        registry.Register(CreateValidCommand(id: "file.open", category: "File"));
        registry.Register(CreateValidCommand(id: "edit.undo", category: "Edit"));

        // Act
        var fileCommands = registry.GetCommandsByCategory("File");

        // Assert
        fileCommands.Should().HaveCount(2);
        fileCommands.Should().OnlyContain(c => c.Category == "File");
    }

    [Fact]
    public void GetCategories_ReturnsUniqueCategories()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand(id: "file.save", category: "File"));
        registry.Register(CreateValidCommand(id: "file.open", category: "File"));
        registry.Register(CreateValidCommand(id: "edit.undo", category: "Edit"));

        // Act
        var categories = registry.GetCategories();

        // Assert
        categories.Should().HaveCount(2);
        categories.Should().Contain("File", "Edit");
    }

    [Fact]
    public void GetCommand_ExistingId_ReturnsCommand()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand();
        registry.Register(command);

        // Act
        var result = registry.GetCommand("test.command");

        // Assert
        result.Should().Be(command);
    }

    [Fact]
    public void GetCommand_NonExisting_ReturnsNull()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.GetCommand("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HasCommand_ExistingId_ReturnsTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand());

        // Act & Assert
        registry.HasCommand("test.command").Should().BeTrue();
    }

    [Fact]
    public void HasCommand_NonExisting_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act & Assert
        registry.HasCommand("nonexistent").Should().BeFalse();
    }

    #endregion

    #region Execution Tests

    [Fact]
    public void TryExecute_ExistingCommand_ExecutesAndReturnsTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        var executed = false;
        var command = CreateValidCommand(execute: _ => executed = true);
        registry.Register(command);

        // Act
        var result = registry.TryExecute("test.command");

        // Assert
        result.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public void TryExecute_NonExisting_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.TryExecute("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryExecute_CanExecuteFalse_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();
        var executed = false;
        var command = CreateValidCommand(execute: _ => executed = true) with
        {
            CanExecute = () => false
        };
        registry.Register(command);

        // Act
        var result = registry.TryExecute("test.command");

        // Assert
        result.Should().BeFalse();
        executed.Should().BeFalse();
    }

    [Fact]
    public void TryExecute_PassesParameter()
    {
        // Arrange
        var registry = CreateRegistry();
        object? capturedParam = null;
        var command = CreateValidCommand(execute: p => capturedParam = p);
        registry.Register(command);

        // Act
        registry.TryExecute("test.command", "test-param");

        // Assert
        capturedParam.Should().Be("test-param");
    }

    [Fact]
    public void TryExecute_WithException_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand(execute: _ => throw new InvalidOperationException("Test error"));
        registry.Register(command);

        // Act
        var result = registry.TryExecute("test.command");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryExecute_RaisesCommandExecutedEvent()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand());
        CommandExecutedEventArgs? capturedArgs = null;
        registry.CommandExecuted += (_, args) => capturedArgs = args;

        // Act
        registry.TryExecute("test.command");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.CommandId.Should().Be("test.command");
        capturedArgs.Success.Should().BeTrue();
        capturedArgs.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void TryExecute_WithException_EventContainsException()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand(execute: _ => throw new InvalidOperationException("Test"));
        registry.Register(command);
        CommandExecutedEventArgs? capturedArgs = null;
        registry.CommandExecuted += (_, args) => capturedArgs = args;

        // Act
        registry.TryExecute("test.command");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Success.Should().BeFalse();
        capturedArgs.Exception.Should().BeOfType<InvalidOperationException>();
    }

    #endregion

    #region CanExecute Tests

    [Fact]
    public void CanExecute_NoPredicate_ReturnsTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand());

        // Act
        var result = registry.CanExecute("test.command");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_PredicateTrue_ReturnsTrue()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand() with { CanExecute = () => true };
        registry.Register(command);

        // Act
        var result = registry.CanExecute("test.command");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_PredicateFalse_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand() with { CanExecute = () => false };
        registry.Register(command);

        // Act
        var result = registry.CanExecute("test.command");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_PredicateThrows_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();
        var command = CreateValidCommand() with { CanExecute = () => throw new Exception() };
        registry.Register(command);

        // Act
        var result = registry.CanExecute("test.command");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_NonExisting_ReturnsFalse()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var result = registry.CanExecute("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    public void HasCommand_CaseInsensitive()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand(id: "test.Command"));

        // Act & Assert
        registry.HasCommand("TEST.COMMAND").Should().BeTrue();
        registry.HasCommand("test.command").Should().BeTrue();
    }

    [Fact]
    public void GetCommandsByCategory_CaseInsensitive()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register(CreateValidCommand(category: "File"));

        // Act
        var result = registry.GetCommandsByCategory("FILE");

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion
}
