using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for CommandDefinition validation.
/// </summary>
[Trait("Category", "Unit")]
public class CommandDefinitionTests
{
    [Fact]
    public void Validate_ValidCommand_ReturnsEmptyList()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "editor.save",
            Title: "Save",
            Category: "File",
            DefaultShortcut: "Ctrl+S",
            Execute: _ => { }
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyId_ReturnsError()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "",
            Title: "Save",
            Category: "File",
            DefaultShortcut: null,
            Execute: _ => { }
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().Contain("Command Id is required.");
    }

    [Fact]
    public void Validate_IdWithoutDot_ReturnsError()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "save",
            Title: "Save",
            Category: "File",
            DefaultShortcut: null,
            Execute: _ => { }
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().Contain("Command Id should follow 'module.action' convention.");
    }

    [Fact]
    public void Validate_NullExecute_ReturnsError()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "editor.save",
            Title: "Save",
            Category: "File",
            DefaultShortcut: null,
            Execute: null!
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().Contain("Command Execute action is required.");
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "editor.save",
            Title: "",
            Category: "File",
            DefaultShortcut: null,
            Execute: _ => { }
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().Contain("Command Title is required.");
    }

    [Fact]
    public void Validate_EmptyCategory_ReturnsError()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "editor.save",
            Title: "Save",
            Category: "",
            DefaultShortcut: null,
            Execute: _ => { }
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().Contain("Command Category is required.");
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new CommandDefinition(
            Id: "",
            Title: "",
            Category: "",
            DefaultShortcut: null,
            Execute: null!
        );

        // Act
        var errors = command.Validate();

        // Assert
        errors.Should().HaveCount(4);
    }

    [Fact]
    public void AllPropertiesSet_Correctly()
    {
        // Arrange
        var shortcut = "Ctrl+S";
        Action<object?> execute = _ => { };
        Func<bool> canExecute = () => true;
        var tags = new[] { "write", "store" };

        // Act
        var command = new CommandDefinition(
            Id: "editor.save",
            Title: "Save",
            Category: "File",
            DefaultShortcut: shortcut,
            Execute: execute
        )
        {
            Description = "Save the document",
            IconKind = "ContentSave",
            CanExecute = canExecute,
            Context = "editorFocus",
            Tags = tags,
            ShowInMenu = false,
            ShowInPalette = true
        };

        // Assert
        command.Id.Should().Be("editor.save");
        command.Title.Should().Be("Save");
        command.Category.Should().Be("File");
        command.DefaultShortcut.Should().Be(shortcut);
        command.Execute.Should().BeSameAs(execute);
        command.Description.Should().Be("Save the document");
        command.IconKind.Should().Be("ContentSave");
        command.CanExecute.Should().BeSameAs(canExecute);
        command.Context.Should().Be("editorFocus");
        command.Tags.Should().BeEquivalentTo(tags);
        command.ShowInMenu.Should().BeFalse();
        command.ShowInPalette.Should().BeTrue();
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var command = new CommandDefinition(
            Id: "editor.save",
            Title: "Save",
            Category: "File",
            DefaultShortcut: null,
            Execute: _ => { }
        );

        // Assert
        command.Description.Should().BeNull();
        command.IconKind.Should().BeNull();
        command.CanExecute.Should().BeNull();
        command.Context.Should().BeNull();
        command.Tags.Should().BeNull();
        command.ShowInMenu.Should().BeTrue();
        command.ShowInPalette.Should().BeTrue();
    }
}
