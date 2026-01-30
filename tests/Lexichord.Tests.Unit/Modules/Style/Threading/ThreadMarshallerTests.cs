using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Threading;

/// <summary>
/// Unit tests for the IThreadMarshaller contract via TestThreadMarshaller.
/// </summary>
/// <remarks>
/// LOGIC: Verifies that the thread marshaller correctly:
/// - Executes actions synchronously when invoked
/// - Returns values from functions
/// - Posts fire-and-forget actions
/// - Asserts thread context based on SimulateUIThread setting
/// - Reports IsOnUIThread accurately
///
/// Version: v0.2.7a
/// </remarks>
public class ThreadMarshallerTests
{
    [Fact]
    public async Task InvokeOnUIThreadAsync_ExecutesAction()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();
        var executed = false;

        // Act
        await marshaller.InvokeOnUIThreadAsync(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_Func_ReturnsValue()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act
        var result = await marshaller.InvokeOnUIThreadAsync(() => 42);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void PostToUIThread_ExecutesAction()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();
        var executed = false;

        // Act
        marshaller.PostToUIThread(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void AssertUIThread_OnUIThread_NoException()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = true };

        // Act & Assert
        var act = () => marshaller.AssertUIThread("test");
        act.Should().NotThrow();
    }

#if DEBUG
    [Fact]
    public void AssertUIThread_NotOnUIThread_Throws()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = false };

        // Act & Assert
        var act = () => marshaller.AssertUIThread("test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must run on UI thread*");
    }
#endif

    [Fact]
    public void AssertBackgroundThread_NotOnUIThread_NoException()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = false };

        // Act & Assert
        var act = () => marshaller.AssertBackgroundThread("test");
        act.Should().NotThrow();
    }

#if DEBUG
    [Fact]
    public void AssertBackgroundThread_OnUIThread_Throws()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = true };

        // Act & Assert
        var act = () => marshaller.AssertBackgroundThread("test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must run on background thread*");
    }
#endif

    [Fact]
    public void IsOnUIThread_ReturnsSimulatedValue_True()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = true };

        // Assert
        marshaller.IsOnUIThread.Should().BeTrue();
    }

    [Fact]
    public void IsOnUIThread_ReturnsSimulatedValue_False()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = false };

        // Assert
        marshaller.IsOnUIThread.Should().BeFalse();
    }

    [Fact]
    public void IsOnUIThread_CanToggle()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller { SimulateUIThread = true };

        // Assert initial
        marshaller.IsOnUIThread.Should().BeTrue();

        // Act
        marshaller.SimulateUIThread = false;

        // Assert toggled
        marshaller.IsOnUIThread.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_Action_NullAction_Throws()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => marshaller.InvokeOnUIThreadAsync((Action)null!));
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_Func_NullFunc_Throws()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => marshaller.InvokeOnUIThreadAsync((Func<int>)null!));
    }

    [Fact]
    public void PostToUIThread_NullAction_Throws()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act & Assert
        var act = () => marshaller.PostToUIThread(null!);
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_Func_ReturnsStringValue()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act
        var result = await marshaller.InvokeOnUIThreadAsync(() => "hello");

        // Assert
        result.Should().Be("hello");
    }

    [Fact]
    public async Task InvokeOnUIThreadAsync_Action_PropagatesException()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => marshaller.InvokeOnUIThreadAsync(
                () => throw new InvalidOperationException("test error")));
    }

    [Fact]
    public void PostToUIThread_PropagatesException()
    {
        // Arrange
        var marshaller = new TestThreadMarshaller();

        // Act & Assert
        var act = () => marshaller.PostToUIThread(
            () => throw new InvalidOperationException("test error"));
        act.Should().Throw<InvalidOperationException>();
    }
}
