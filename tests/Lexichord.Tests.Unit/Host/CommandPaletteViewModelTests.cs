using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Commands;
using Lexichord.Abstractions.Events;
using Lexichord.Host.ViewModels.CommandPalette;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for CommandPaletteViewModel.
/// </summary>
[Trait("Category", "Unit")]
public class CommandPaletteViewModelTests
{
    private readonly Mock<ICommandRegistry> _commandRegistryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<CommandPaletteViewModel>> _loggerMock = new();

    private CommandPaletteViewModel CreateViewModel(List<CommandDefinition>? commands = null)
    {
        _commandRegistryMock.Setup(x => x.GetAllCommands())
            .Returns(commands ?? new List<CommandDefinition>());

        return new CommandPaletteViewModel(
            _commandRegistryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static CommandDefinition CreateCommand(
        string id,
        string title,
        string category = "Test",
        string? shortcut = null)
    {
        return new CommandDefinition(
            Id: id,
            Title: title,
            Category: category,
            DefaultShortcut: shortcut,
            Execute: _ => { });
    }

    [Fact]
    public void Constructor_SetsDefaults()
    {
        var viewModel = CreateViewModel();

        viewModel.IsVisible.Should().BeFalse();
        viewModel.SearchQuery.Should().BeEmpty();
        viewModel.CurrentMode.Should().Be(PaletteMode.Commands);
    }

    [Fact]
    public async Task ShowAsync_SetsIsVisibleToTrue()
    {
        var viewModel = CreateViewModel();

        await viewModel.ShowAsync(PaletteMode.Commands);

        viewModel.IsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task ShowAsync_SetsCurrentMode()
    {
        var viewModel = CreateViewModel();

        await viewModel.ShowAsync(PaletteMode.Files);

        viewModel.CurrentMode.Should().Be(PaletteMode.Files);
    }

    [Fact]
    public async Task ShowAsync_WithInitialQuery_SetsSearchQuery()
    {
        var viewModel = CreateViewModel();

        await viewModel.ShowAsync(PaletteMode.Commands, "test query");

        viewModel.SearchQuery.Should().Be("test query");
    }

    [Fact]
    public async Task ShowAsync_PublishesOpenedEvent()
    {
        var viewModel = CreateViewModel();

        await viewModel.ShowAsync(PaletteMode.Commands);

        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<CommandPaletteOpenedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Hide_SetsIsVisibleToFalse()
    {
        var viewModel = CreateViewModel();
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.IsVisible.Should().BeTrue();

        viewModel.Hide();

        viewModel.IsVisible.Should().BeFalse();
    }

    [Fact]
    public async Task Hide_ClearsSearchQuery()
    {
        var viewModel = CreateViewModel();
        await viewModel.ShowAsync(PaletteMode.Commands, "test");

        viewModel.Hide();

        viewModel.SearchQuery.Should().BeEmpty();
    }

    [Fact]
    public async Task Placeholder_InCommandsMode_ReturnsCommandPlaceholder()
    {
        var viewModel = CreateViewModel();
        await viewModel.ShowAsync(PaletteMode.Commands);

        var placeholder = viewModel.Placeholder;

        placeholder.Should().Contain("command");
    }

    [Fact]
    public async Task Placeholder_InFilesMode_ReturnsFilePlaceholder()
    {
        var viewModel = CreateViewModel();
        await viewModel.ShowAsync(PaletteMode.Files);

        var placeholder = viewModel.Placeholder;

        placeholder.Should().Contain("file");
    }

    [Fact]
    public async Task SearchQuery_WithEmptyString_ShowsAllCommands()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd1", "First Command"),
            CreateCommand("test.cmd2", "Second Command")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);

        viewModel.SearchQuery = "";

        viewModel.FilteredItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchQuery_WithText_FiltersCommands()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.save", "Save File"),
            CreateCommand("test.open", "Open File")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);

        viewModel.SearchQuery = "save";

        viewModel.FilteredItems.Should().HaveCount(1);
        viewModel.FilteredItems.First().Should().BeOfType<CommandSearchResult>();
        ((CommandSearchResult)viewModel.FilteredItems.First()).Title.Should().Be("Save File");
    }

    [Fact]
    public async Task SearchQuery_WithFuzzyMatch_FindsPartialMatches()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.saveall", "Save All Files")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);

        viewModel.SearchQuery = "save all";

        viewModel.FilteredItems.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task MoveSelectionDown_MovesSelectionToNextItem()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd1", "Command 1"),
            CreateCommand("test.cmd2", "Command 2")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.SearchQuery = "";
        var firstItem = viewModel.SelectedItem;

        viewModel.MoveSelectionDown();

        viewModel.SelectedItem.Should().NotBe(firstItem);
    }

    [Fact]
    public async Task MoveSelectionUp_MovesSelectionToPreviousItem()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd1", "Command 1"),
            CreateCommand("test.cmd2", "Command 2")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.SearchQuery = "";
        viewModel.MoveSelectionDown();
        var secondItem = viewModel.SelectedItem;

        viewModel.MoveSelectionUp();

        viewModel.SelectedItem.Should().NotBe(secondItem);
    }

    [Fact]
    public async Task MoveSelectionDown_AtEnd_WrapsToStart()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd1", "Command 1"),
            CreateCommand("test.cmd2", "Command 2")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.SearchQuery = "";
        var firstItem = viewModel.SelectedItem;
        viewModel.MoveSelectionDown();

        viewModel.MoveSelectionDown();

        viewModel.SelectedItem.Should().Be(firstItem);
    }

    [Fact]
    public async Task ExecuteSelectedAsync_WithCommandResult_ExecutesCommand()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.execute", "Test Execute")
        };
        _commandRegistryMock.Setup(x => x.TryExecute("test.execute", null)).Returns(true);
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.SearchQuery = "";

        await viewModel.ExecuteSelectedAsync();

        _commandRegistryMock.Verify(x => x.TryExecute("test.execute", null), Times.Once);
    }

    [Fact]
    public async Task ExecuteSelectedAsync_HidesPalette()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd", "Test")
        };
        _commandRegistryMock.Setup(x => x.TryExecute(It.IsAny<string>(), null)).Returns(true);
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.SearchQuery = "";

        await viewModel.ExecuteSelectedAsync();

        viewModel.IsVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteSelectedAsync_PublishesExecutedEvent()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd", "Test")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.SearchQuery = "";

        await viewModel.ExecuteSelectedAsync();

        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<CommandPaletteExecutedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FilteredItems_SortsByScoreDescending()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.abc", "ABC Something"),
            CreateCommand("test.abcd", "ABCD Command")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);

        viewModel.SearchQuery = "ABCD";

        viewModel.FilteredItems.Should().HaveCountGreaterOrEqualTo(1);
        var first = viewModel.FilteredItems.First() as CommandSearchResult;
        first?.Title.Should().Contain("ABCD");
    }

    [Fact]
    public async Task SelectedItem_AutoSelectsFirstResult()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd1", "Command 1"),
            CreateCommand("test.cmd2", "Command 2")
        };
        var viewModel = CreateViewModel(commands);

        await viewModel.ShowAsync(PaletteMode.Commands);

        viewModel.SelectedItem.Should().NotBeNull();
    }

    [Fact]
    public async Task Hide_ClearsFilteredItems()
    {
        var commands = new List<CommandDefinition>
        {
            CreateCommand("test.cmd", "Test")
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);
        viewModel.FilteredItems.Should().NotBeEmpty();

        viewModel.Hide();

        viewModel.FilteredItems.Should().BeEmpty();
    }

    [Fact]
    public async Task CommandSearchResult_ExposesShortcutDisplay()
    {
        var commands = new List<CommandDefinition>
        {
            new(
                Id: "test.save",
                Title: "Save",
                Category: "File",
                DefaultShortcut: "Ctrl+S",
                Execute: _ => { })
        };
        var viewModel = CreateViewModel(commands);
        await viewModel.ShowAsync(PaletteMode.Commands);

        var result = viewModel.FilteredItems.First() as CommandSearchResult;
        result?.ShortcutDisplay.Should().Be("Ctrl+S");
    }
}
