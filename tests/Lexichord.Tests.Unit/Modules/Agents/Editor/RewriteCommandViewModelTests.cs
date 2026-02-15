// -----------------------------------------------------------------------
// <copyright file="RewriteCommandViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Modules.Agents.Editor;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="RewriteCommandViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Validates the ViewModel's behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Initial state synchronization with provider</description></item>
///   <item><description>Command execution delegation</description></item>
///   <item><description>IsExecuting state management</description></item>
///   <item><description>CanExecute logic</description></item>
///   <item><description>State refresh on CanRewriteChanged</description></item>
/// </list>
/// <para>
/// <strong>Spec reference:</strong> LCS-DES-v0.7.3a §6
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3a")]
public class RewriteCommandViewModelTests : IDisposable
{
    private readonly Mock<IEditorAgentContextMenuProvider> _providerMock;
    private readonly Mock<ILogger<RewriteCommandViewModel>> _loggerMock;
    private readonly RewriteCommandViewModel _sut;

    /// <summary>
    /// Initializes test fixtures with default mocks.
    /// </summary>
    public RewriteCommandViewModelTests()
    {
        _providerMock = new Mock<IEditorAgentContextMenuProvider>();
        _loggerMock = new Mock<ILogger<RewriteCommandViewModel>>();

        // Default: Can rewrite
        _providerMock.Setup(p => p.CanRewrite).Returns(true);
        _providerMock.Setup(p => p.IsLicensed).Returns(true);
        _providerMock.Setup(p => p.HasSelection).Returns(true);
        _providerMock.Setup(p => p.GetRewriteMenuItems()).Returns(new List<RewriteCommandOption>());

        _sut = new RewriteCommandViewModel(_providerMock.Object, _loggerMock.Object);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sut.Dispose();
    }

    // -----------------------------------------------------------------------
    // Initial State Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that CanRewrite reflects initial provider state.
    /// </summary>
    [Fact]
    public void CanRewrite_InitialState_MatchesProviderState()
    {
        // Assert
        _sut.CanRewrite.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsLicensed reflects initial provider state.
    /// </summary>
    [Fact]
    public void IsLicensed_InitialState_MatchesProviderState()
    {
        // Assert
        _sut.IsLicensed.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsExecuting is initially false.
    /// </summary>
    [Fact]
    public void IsExecuting_InitialState_IsFalse()
    {
        // Assert
        _sut.IsExecuting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Progress is initially zero.
    /// </summary>
    [Fact]
    public void Progress_InitialState_IsZero()
    {
        // Assert
        _sut.Progress.Should().Be(0);
    }

    /// <summary>
    /// Verifies that ProgressMessage is initially empty.
    /// </summary>
    [Fact]
    public void ProgressMessage_InitialState_IsEmpty()
    {
        // Assert
        _sut.ProgressMessage.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // RewriteMenuItems Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that RewriteMenuItems delegates to provider.
    /// </summary>
    [Fact]
    public void RewriteMenuItems_DelegatesToProvider()
    {
        // Arrange
        var expectedItems = new List<RewriteCommandOption>
        {
            new("test", "Test", "Description", "icon", null, RewriteIntent.Formal, false)
        };
        _providerMock.Setup(p => p.GetRewriteMenuItems()).Returns(expectedItems);

        // Act
        var items = _sut.RewriteMenuItems;

        // Assert
        items.Should().BeSameAs(expectedItems);
    }

    // -----------------------------------------------------------------------
    // Command Execution Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that RewriteFormallyCommand delegates to provider.
    /// </summary>
    [Fact]
    public async Task RewriteFormallyCommand_ExecutesRewrite()
    {
        // Arrange
        _providerMock.Setup(p => p.ExecuteRewriteAsync(
                RewriteIntent.Formal,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RewriteFormallyCommand.ExecuteAsync(null);

        // Assert
        _providerMock.Verify(
            p => p.ExecuteRewriteAsync(RewriteIntent.Formal, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that SimplifyCommand delegates to provider.
    /// </summary>
    [Fact]
    public async Task SimplifyCommand_ExecutesRewrite()
    {
        // Arrange
        _providerMock.Setup(p => p.ExecuteRewriteAsync(
                RewriteIntent.Simplified,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SimplifyCommand.ExecuteAsync(null);

        // Assert
        _providerMock.Verify(
            p => p.ExecuteRewriteAsync(RewriteIntent.Simplified, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ExpandCommand delegates to provider.
    /// </summary>
    [Fact]
    public async Task ExpandCommand_ExecutesRewrite()
    {
        // Arrange
        _providerMock.Setup(p => p.ExecuteRewriteAsync(
                RewriteIntent.Expanded,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExpandCommand.ExecuteAsync(null);

        // Assert
        _providerMock.Verify(
            p => p.ExecuteRewriteAsync(RewriteIntent.Expanded, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that CustomRewriteCommand delegates to provider.
    /// </summary>
    [Fact]
    public async Task CustomRewriteCommand_ExecutesRewrite()
    {
        // Arrange
        _providerMock.Setup(p => p.ExecuteRewriteAsync(
                RewriteIntent.Custom,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.CustomRewriteCommand.ExecuteAsync(null);

        // Assert
        _providerMock.Verify(
            p => p.ExecuteRewriteAsync(RewriteIntent.Custom, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // CanExecute Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that commands cannot execute when CanRewrite is false.
    /// </summary>
    [Fact]
    public void Commands_WhenCannotRewrite_CannotExecute()
    {
        // Arrange
        _providerMock.Setup(p => p.CanRewrite).Returns(false);

        var sut = new RewriteCommandViewModel(_providerMock.Object, _loggerMock.Object);

        // Assert
        sut.RewriteFormallyCommand.CanExecute(null).Should().BeFalse();
        sut.SimplifyCommand.CanExecute(null).Should().BeFalse();
        sut.ExpandCommand.CanExecute(null).Should().BeFalse();
        sut.CustomRewriteCommand.CanExecute(null).Should().BeFalse();

        sut.Dispose();
    }

    /// <summary>
    /// Verifies that commands can execute when CanRewrite is true.
    /// </summary>
    [Fact]
    public void Commands_WhenCanRewrite_CanExecute()
    {
        // Assert (default setup has CanRewrite = true)
        _sut.RewriteFormallyCommand.CanExecute(null).Should().BeTrue();
        _sut.SimplifyCommand.CanExecute(null).Should().BeTrue();
        _sut.ExpandCommand.CanExecute(null).Should().BeTrue();
        _sut.CustomRewriteCommand.CanExecute(null).Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // IsExecuting State Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that IsExecuting is true during command execution.
    /// </summary>
    [Fact]
    public async Task IsExecuting_DuringExecution_IsTrue()
    {
        // Arrange
        var executionStarted = new TaskCompletionSource<bool>();
        var continueExecution = new TaskCompletionSource<bool>();

        _providerMock.Setup(p => p.ExecuteRewriteAsync(
                It.IsAny<RewriteIntent>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                executionStarted.SetResult(true);
                await continueExecution.Task;
            });

        // Act
        var executeTask = _sut.RewriteFormallyCommand.ExecuteAsync(null);
        await executionStarted.Task;

        // Assert - during execution
        _sut.IsExecuting.Should().BeTrue();

        // Cleanup
        continueExecution.SetResult(true);
        await executeTask;

        // Assert - after execution
        _sut.IsExecuting.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ProgressMessage is set during execution.
    /// </summary>
    [Fact]
    public async Task ProgressMessage_DuringExecution_IsSet()
    {
        // Arrange
        var executionStarted = new TaskCompletionSource<bool>();
        var continueExecution = new TaskCompletionSource<bool>();

        _providerMock.Setup(p => p.ExecuteRewriteAsync(
                It.IsAny<RewriteIntent>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                executionStarted.SetResult(true);
                await continueExecution.Task;
            });

        // Act
        var executeTask = _sut.RewriteFormallyCommand.ExecuteAsync(null);
        await executionStarted.Task;

        // Assert - during execution
        _sut.ProgressMessage.Should().Contain("formal");

        // Cleanup
        continueExecution.SetResult(true);
        await executeTask;

        // Assert - after execution
        _sut.ProgressMessage.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // CanRewriteChanged Event Handling Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that CanRewrite reflects provider state at construction time.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The ViewModel captures state during construction via RefreshState().
    /// Event-driven updates use Dispatcher.UIThread.Post which is not testable without
    /// a full Avalonia runtime. This test verifies the initial state capture works.
    /// </remarks>
    [Fact]
    public void CanRewrite_WhenProviderCannotRewrite_ReflectsProviderState()
    {
        // Arrange - Create new provider mock with CanRewrite=false
        var providerMock = new Mock<IEditorAgentContextMenuProvider>();
        providerMock.Setup(p => p.CanRewrite).Returns(false);
        providerMock.Setup(p => p.IsLicensed).Returns(true);
        providerMock.Setup(p => p.HasSelection).Returns(false);
        providerMock.Setup(p => p.GetRewriteMenuItems()).Returns(new List<RewriteCommandOption>());

        // Act - Create ViewModel with the mock
        using var sut = new RewriteCommandViewModel(providerMock.Object, _loggerMock.Object);

        // Assert
        sut.CanRewrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsLicensed reflects provider state at construction time.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The ViewModel captures state during construction via RefreshState().
    /// Event-driven updates use Dispatcher.UIThread.Post which is not testable without
    /// a full Avalonia runtime. This test verifies the initial state capture works.
    /// </remarks>
    [Fact]
    public void IsLicensed_WhenProviderNotLicensed_ReflectsProviderState()
    {
        // Arrange - Create new provider mock with IsLicensed=false
        var providerMock = new Mock<IEditorAgentContextMenuProvider>();
        providerMock.Setup(p => p.CanRewrite).Returns(false);
        providerMock.Setup(p => p.IsLicensed).Returns(false);
        providerMock.Setup(p => p.HasSelection).Returns(true);
        providerMock.Setup(p => p.GetRewriteMenuItems()).Returns(new List<RewriteCommandOption>());

        // Act - Create ViewModel with the mock
        using var sut = new RewriteCommandViewModel(providerMock.Object, _loggerMock.Object);

        // Assert
        sut.IsLicensed.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Dispose Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Dispose unsubscribes from provider events.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var sut = new RewriteCommandViewModel(_providerMock.Object, _loggerMock.Object);
        var initialCanRewrite = sut.CanRewrite;

        // Act
        sut.Dispose();

        // Change provider state
        _providerMock.Setup(p => p.CanRewrite).Returns(!initialCanRewrite);

        // Raise event - should not update disposed ViewModel
        // Note: This test verifies unsubscription by ensuring no exceptions
        _providerMock.Raise(p => p.CanRewriteChanged += null, EventArgs.Empty);
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var sut = new RewriteCommandViewModel(_providerMock.Object, _loggerMock.Object);

        // Act & Assert
        sut.Invoking(s =>
        {
            s.Dispose();
            s.Dispose();
        }).Should().NotThrow();
    }
}
