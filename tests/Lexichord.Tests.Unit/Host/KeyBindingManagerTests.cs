using Avalonia.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Configuration;
using Lexichord.Host.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for KeyBindingManager.
/// </summary>
[Trait("Category", "Unit")]
public class KeyBindingManagerTests : IDisposable
{
    private readonly Mock<ICommandRegistry> _commandRegistryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<KeyBindingManager>> _loggerMock = new();
    private readonly LexichordOptions _options;
    private readonly string _tempDir;

    public KeyBindingManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _options = new LexichordOptions { DataPath = _tempDir };

        // Default: no commands
        _commandRegistryMock.Setup(r => r.GetAllCommands()).Returns([]);
    }

    public void Dispose()
    {
        // Cleanup temp directory
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    private KeyBindingManager CreateManager()
    {
        var optionsMock = new Mock<IOptions<LexichordOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        return new KeyBindingManager(
            _commandRegistryMock.Object,
            _mediatorMock.Object,
            optionsMock.Object,
            _loggerMock.Object);
    }

    private static CommandDefinition CreateCommand(
        string id = "test.command",
        string? shortcut = null,
        string? context = null)
    {
        var cmd = new CommandDefinition(
            Id: id,
            Title: "Test Command",
            Category: "Test",
            DefaultShortcut: shortcut,
            Execute: _ => { });

        return context is null ? cmd : cmd with { Context = context };
    }

    #region Loading Tests

    [Fact]
    public async Task LoadBindingsAsync_LoadsDefaultsFromCommands()
    {
        // Arrange
        var commands = new[]
        {
            CreateCommand("file.save", "Ctrl+S"),
            CreateCommand("file.open", "Ctrl+O"),
            CreateCommand("edit.undo", "Ctrl+Z")
        };
        _commandRegistryMock.Setup(r => r.GetAllCommands()).Returns(commands);

        using var manager = CreateManager();

        // Act
        await manager.LoadBindingsAsync();

        // Assert
        manager.GetBinding("file.save").Should().Be("Ctrl+S");
        manager.GetBinding("file.open").Should().Be("Ctrl+O");
        manager.GetBinding("edit.undo").Should().Be("Ctrl+Z");
    }

    [Fact]
    public async Task LoadBindingsAsync_SkipsCommandsWithoutShortcut()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("test.noshortcut", shortcut: null)]);

        using var manager = CreateManager();

        // Act
        await manager.LoadBindingsAsync();

        // Assert
        manager.GetBinding("test.noshortcut").Should().BeNull();
    }

    [Fact]
    public async Task LoadBindingsAsync_OverridesWithUserBindings()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();

        // Create user keybindings file
        var keybindingsPath = manager.KeybindingsFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(keybindingsPath)!);
        await File.WriteAllTextAsync(keybindingsPath, """
            {
                "bindings": [
                    { "command": "file.save", "key": "Ctrl+Shift+S" }
                ]
            }
            """);

        // Act
        await manager.LoadBindingsAsync();

        // Assert
        manager.GetBinding("file.save").Should().Be("Ctrl+Shift+S");
        var binding = manager.GetFullBinding("file.save");
        binding.Should().NotBeNull();
        binding!.IsUserDefined.Should().BeTrue();
    }

    [Fact]
    public async Task LoadBindingsAsync_DisablesBindingWithDash()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();

        // Create user keybindings file with "-" to disable
        var keybindingsPath = manager.KeybindingsFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(keybindingsPath)!);
        await File.WriteAllTextAsync(keybindingsPath, """
            {
                "bindings": [
                    { "command": "file.save", "key": "-" }
                ]
            }
            """);

        // Act
        await manager.LoadBindingsAsync();

        // Assert
        manager.GetBinding("file.save").Should().BeNull();
        var binding = manager.GetFullBinding("file.save");
        binding.Should().NotBeNull();
        binding!.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadBindingsAsync_PublishesReloadedEvent()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();

        // Act
        await manager.LoadBindingsAsync();

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<KeybindingsReloadedEvent>(e =>
                    e.TotalBindings == 1 &&
                    e.UserOverrides == 0 &&
                    e.ConflictCount == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadBindingsAsync_RaisesBindingsReloadedEvent()
    {
        // Arrange
        using var manager = CreateManager();
        var eventRaised = false;
        manager.BindingsReloaded += (_, _) => eventRaised = true;

        // Act
        await manager.LoadBindingsAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region GetBinding Tests

    [Fact]
    public async Task GetBinding_ExistingBinding_ReturnsGesture()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("test.cmd", "Ctrl+T")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        var result = manager.GetBinding("test.cmd");

        // Assert
        result.Should().Be("Ctrl+T");
    }

    [Fact]
    public async Task GetBinding_NonExisting_ReturnsNull()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        var result = manager.GetBinding("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBinding_DisabledBinding_ReturnsNull()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        manager.SetBinding("test.cmd", null);

        // Act
        var result = manager.GetBinding("test.cmd");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllBindings Tests

    [Fact]
    public async Task GetAllBindings_ReturnsNonDisabledBindings()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([
                CreateCommand("cmd.one", "Ctrl+1"),
                CreateCommand("cmd.two", "Ctrl+2")
            ]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Disable one
        manager.SetBinding("cmd.one", null);

        // Act
        var bindings = manager.GetAllBindings();

        // Assert
        bindings.Should().HaveCount(1);
        bindings.Single().CommandId.Should().Be("cmd.two");
    }

    #endregion

    #region SetBinding Tests

    [Fact]
    public async Task SetBinding_UpdatesBinding()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        manager.SetBinding("file.save", "Ctrl+Shift+S");

        // Assert
        manager.GetBinding("file.save").Should().Be("Ctrl+Shift+S");
        manager.GetFullBinding("file.save")!.IsUserDefined.Should().BeTrue();
    }

    [Fact]
    public async Task SetBinding_RaisesBindingChangedEvent()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        KeyBindingChangedEventArgs? capturedArgs = null;
        manager.BindingChanged += (_, args) => capturedArgs = args;

        // Act
        manager.SetBinding("file.save", "Ctrl+Shift+S");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.CommandId.Should().Be("file.save");
        capturedArgs.OldGesture.Should().Be("Ctrl+S");
        capturedArgs.NewGesture.Should().Be("Ctrl+Shift+S");
        capturedArgs.IsUserChange.Should().BeTrue();
    }

    [Fact]
    public async Task SetBinding_NullGesture_DisablesBinding()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        manager.SetBinding("file.save", null);

        // Assert
        manager.GetBinding("file.save").Should().BeNull();
        manager.GetFullBinding("file.save")!.IsDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetBinding_WithContext_SetsContextOnBinding()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        manager.SetBinding("editor.format", "Ctrl+Shift+F", when: "editorFocus");

        // Assert
        var binding = manager.GetFullBinding("editor.format");
        binding.Should().NotBeNull();
        binding!.When.Should().Be("editorFocus");
    }

    [Fact]
    public void SetBinding_EmptyCommandId_ThrowsArgumentException()
    {
        // Arrange
        using var manager = CreateManager();

        // Act
        var act = () => manager.SetBinding("", "Ctrl+S");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region GetConflicts Tests

    [Fact]
    public async Task GetConflicts_NoConflicts_ReturnsEmptyList()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        var conflicts = manager.GetConflicts("Ctrl+O");

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConflicts_WithConflict_ReturnsConflictingCommandIds()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        var conflicts = manager.GetConflicts("Ctrl+S");

        // Assert
        conflicts.Should().Contain("file.save");
    }

    [Fact]
    public async Task GetConflicts_FiltersByContext()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Set two bindings with same gesture but different contexts
        manager.SetBinding("global.cmd", "Ctrl+S", when: null); // global
        manager.SetBinding("editor.cmd", "Ctrl+S", when: "editorFocus");

        // Act - check conflicts in editor context
        var editorConflicts = manager.GetConflicts("Ctrl+S", context: "editorFocus");

        // Assert - both should conflict in editor context (global + editor-specific)
        editorConflicts.Should().Contain("global.cmd");
        editorConflicts.Should().Contain("editor.cmd");
    }

    #endregion

    #region IsGestureAvailable Tests

    [Fact]
    public async Task IsGestureAvailable_NoConflicts_ReturnsTrue()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        var result = manager.IsGestureAvailable("Ctrl+Alt+Shift+F12");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGestureAvailable_WithConflict_ReturnsFalse()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act
        var result = manager.IsGestureAvailable("Ctrl+S");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGestureAvailable_ExcludeCommandId_IgnoresExcluded()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act - gesture is available if we exclude the command that uses it
        var result = manager.IsGestureAvailable("Ctrl+S", excludeCommandId: "file.save");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ResetBinding Tests

    [Fact]
    public async Task ResetBinding_UserDefined_RestoresDefault()
    {
        // Arrange
        var defaultCommand = CreateCommand("file.save", "Ctrl+S");
        _commandRegistryMock.Setup(r => r.GetAllCommands()).Returns([defaultCommand]);
        _commandRegistryMock.Setup(r => r.GetCommand("file.save")).Returns(defaultCommand);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        manager.SetBinding("file.save", "Ctrl+Shift+S"); // User override
        manager.GetBinding("file.save").Should().Be("Ctrl+Shift+S");

        // Act
        manager.ResetBinding("file.save");

        // Assert
        manager.GetBinding("file.save").Should().Be("Ctrl+S");
        manager.GetFullBinding("file.save")!.IsUserDefined.Should().BeFalse();
    }

    [Fact]
    public async Task ResetBinding_NonUserDefined_DoesNothing()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Act (binding is default, not user-defined)
        manager.ResetBinding("file.save");

        // Assert - should still have binding
        manager.GetBinding("file.save").Should().Be("Ctrl+S");
    }

    #endregion

    #region ResetToDefaults Tests

    [Fact]
    public async Task ResetToDefaults_ClearsAllUserBindings()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        manager.SetBinding("file.save", "Ctrl+Shift+S");
        manager.SetBinding("custom.cmd", "Ctrl+K");

        // Act
        manager.ResetToDefaults();

        // Assert
        manager.GetBinding("file.save").Should().Be("Ctrl+S");
        manager.GetBinding("custom.cmd").Should().BeNull();
    }

    [Fact]
    public async Task ResetToDefaults_RaisesBindingsReloadedEvent()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        var eventRaised = false;
        manager.BindingsReloaded += (_, _) => eventRaised = true;

        // Act
        manager.ResetToDefaults();

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region SaveBindingsAsync Tests

    [Fact]
    public async Task SaveBindingsAsync_PersistsUserBindings()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        manager.SetBinding("file.save", "Ctrl+Shift+S");

        // Act
        await manager.SaveBindingsAsync();

        // Assert
        File.Exists(manager.KeybindingsFilePath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(manager.KeybindingsFilePath);
        content.Should().Contain("file.save");
        // JSON serializer may unicode escape '+' as '\u002B', check for either form
        var hasLiteralPlus = content.Contains("Ctrl+Shift+S");
        var hasEscapedPlus = content.Contains("Ctrl") && content.Contains("Shift") && content.Contains("S");
        (hasLiteralPlus || hasEscapedPlus).Should().BeTrue(
            "the saved keybinding should contain the gesture (with or without unicode escaping)");
    }

    [Fact]
    public async Task SaveBindingsAsync_OnlySavesUserDefinedBindings()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        // Don't modify anything - only defaults

        // Act
        await manager.SaveBindingsAsync();

        // Assert
        var content = await File.ReadAllTextAsync(manager.KeybindingsFilePath);
        content.Should().NotContain("file.save"); // Defaults shouldn't be saved
    }

    #endregion

    #region Gesture Parsing Tests

    [Theory]
    [InlineData("Ctrl+S", Key.S, KeyModifiers.Control)]
    [InlineData("Ctrl+Shift+S", Key.S, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("Alt+F4", Key.F4, KeyModifiers.Alt)]
    [InlineData("Ctrl+Shift+Alt+P", Key.P, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)]
    public void ParseKeyGesture_ValidGestures_ReturnsCorrectGesture(
        string gestureString, Key expectedKey, KeyModifiers expectedModifiers)
    {
        // Act
        var result = KeyBindingManager.ParseKeyGesture(gestureString);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(expectedKey);
        result.KeyModifiers.Should().Be(expectedModifiers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("Invalid")]
    [InlineData("Ctrl+")]
    public void ParseKeyGesture_InvalidGestures_ReturnsNull(string gestureString)
    {
        // Act
        var result = KeyBindingManager.ParseKeyGesture(gestureString);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FormatKeyGesture_FormatsCorrectly()
    {
        // Arrange
        var gesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift);

        // Act
        var result = KeyBindingManager.FormatKeyGesture(gesture);

        // Assert
        result.Should().Be("Ctrl+Shift+S");
    }

    #endregion

    #region TryHandleKeyEvent Tests

    [Fact]
    public async Task TryHandleKeyEvent_MatchingBinding_ExecutesCommand()
    {
        // Arrange
        var executed = false;
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);
        _commandRegistryMock.Setup(r => r.TryExecute("file.save", null))
            .Callback(() => executed = true)
            .Returns(true);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        var keyArgs = new KeyEventArgs
        {
            Key = Key.S,
            KeyModifiers = KeyModifiers.Control,
            RoutedEvent = null!,
            Source = null
        };

        // Act
        var result = manager.TryHandleKeyEvent(keyArgs);

        // Assert
        result.Should().BeTrue();
        executed.Should().BeTrue();
        keyArgs.Handled.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleKeyEvent_NoMatchingBinding_ReturnsFalse()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        var keyArgs = new KeyEventArgs
        {
            Key = Key.X,
            KeyModifiers = KeyModifiers.Control,
            RoutedEvent = null!,
            Source = null
        };

        // Act
        var result = manager.TryHandleKeyEvent(keyArgs);

        // Assert
        result.Should().BeFalse();
        keyArgs.Handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleKeyEvent_AlreadyHandled_ReturnsFalse()
    {
        // Arrange
        _commandRegistryMock.Setup(r => r.GetAllCommands())
            .Returns([CreateCommand("file.save", "Ctrl+S")]);

        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        var keyArgs = new KeyEventArgs
        {
            Key = Key.S,
            KeyModifiers = KeyModifiers.Control,
            RoutedEvent = null!,
            Source = null,
            Handled = true // Already handled
        };

        // Act
        var result = manager.TryHandleKeyEvent(keyArgs);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleKeyEvent_ModifierKeyOnly_ReturnsFalse()
    {
        // Arrange
        using var manager = CreateManager();
        await manager.LoadBindingsAsync();

        var keyArgs = new KeyEventArgs
        {
            Key = Key.LeftCtrl,
            KeyModifiers = KeyModifiers.Control,
            RoutedEvent = null!,
            Source = null
        };

        // Act
        var result = manager.TryHandleKeyEvent(keyArgs);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void KeybindingsFilePath_ReturnsCorrectPath()
    {
        // Arrange
        using var manager = CreateManager();

        // Act
        var path = manager.KeybindingsFilePath;

        // Assert
        path.Should().Contain("keybindings.json");
        path.Should().StartWith(_tempDir);
    }

    [Fact]
    public void HasCustomBindings_NoFile_ReturnsFalse()
    {
        // Arrange
        using var manager = CreateManager();

        // Act & Assert
        manager.HasCustomBindings.Should().BeFalse();
    }

    [Fact]
    public async Task HasCustomBindings_WithFile_ReturnsTrue()
    {
        // Arrange
        using var manager = CreateManager();

        // Create keybindings file
        var path = manager.KeybindingsFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{}");

        // Act & Assert
        manager.HasCustomBindings.Should().BeTrue();
    }

    #endregion
}
