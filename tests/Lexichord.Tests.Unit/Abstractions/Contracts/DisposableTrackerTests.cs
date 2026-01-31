using System;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for <see cref="DisposableTracker"/>.
/// </summary>
public class DisposableTrackerTests
{
    #region Track Tests

    [Fact]
    public void Track_SingleSubscription_IncreasesCount()
    {
        // Arrange
        var sut = new DisposableTracker();
        var mockDisposable = new Mock<IDisposable>();

        // Act
        sut.Track(mockDisposable.Object);

        // Assert
        sut.Count.Should().Be(1);
    }

    [Fact]
    public void Track_MultipleSubscriptions_IncreasesCountCorrectly()
    {
        // Arrange
        var sut = new DisposableTracker();

        // Act
        sut.Track(Mock.Of<IDisposable>());
        sut.Track(Mock.Of<IDisposable>());
        sut.Track(Mock.Of<IDisposable>());

        // Assert
        sut.Count.Should().Be(3);
    }

    [Fact]
    public void Track_NullDisposable_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DisposableTracker();

        // Act
        var action = () => sut.Track(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Track_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = new DisposableTracker();
        sut.Dispose();

        // Act
        var action = () => sut.Track(Mock.Of<IDisposable>());

        // Assert
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region TrackAll Tests

    [Fact]
    public void TrackAll_MultipleSubscriptions_IncreasesCount()
    {
        // Arrange
        var sut = new DisposableTracker();
        var d1 = Mock.Of<IDisposable>();
        var d2 = Mock.Of<IDisposable>();
        var d3 = Mock.Of<IDisposable>();

        // Act
        sut.TrackAll(d1, d2, d3);

        // Assert
        sut.Count.Should().Be(3);
    }

    [Fact]
    public void TrackAll_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DisposableTracker();

        // Act
        var action = () => sut.TrackAll(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TrackAll_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = new DisposableTracker();
        sut.Dispose();

        // Act
        var action = () => sut.TrackAll(Mock.Of<IDisposable>());

        // Assert
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region DisposeAll Tests

    [Fact]
    public void DisposeAll_DisposesAllTrackedSubscriptions()
    {
        // Arrange
        var sut = new DisposableTracker();
        var d1 = new Mock<IDisposable>();
        var d2 = new Mock<IDisposable>();
        var d3 = new Mock<IDisposable>();

        sut.TrackAll(d1.Object, d2.Object, d3.Object);

        // Act
        sut.DisposeAll();

        // Assert
        d1.Verify(d => d.Dispose(), Times.Once);
        d2.Verify(d => d.Dispose(), Times.Once);
        d3.Verify(d => d.Dispose(), Times.Once);
        sut.Count.Should().Be(0);
    }

    [Fact]
    public void DisposeAll_ContinuesDisposingOnException()
    {
        // Arrange
        var sut = new DisposableTracker();
        var d1 = new Mock<IDisposable>();
        var d2 = new Mock<IDisposable>();
        var d3 = new Mock<IDisposable>();

        d2.Setup(d => d.Dispose()).Throws(new InvalidOperationException("Test exception"));

        sut.TrackAll(d1.Object, d2.Object, d3.Object);

        // Act - should not throw
        sut.DisposeAll();

        // Assert - all should be attempted
        d1.Verify(d => d.Dispose(), Times.Once);
        d2.Verify(d => d.Dispose(), Times.Once);
        d3.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void DisposeAll_ClearsCount()
    {
        // Arrange
        var sut = new DisposableTracker();
        sut.TrackAll(Mock.Of<IDisposable>(), Mock.Of<IDisposable>());
        sut.Count.Should().Be(2);

        // Act
        sut.DisposeAll();

        // Assert
        sut.Count.Should().Be(0);
    }

    [Fact]
    public void DisposeAll_IsIdempotent()
    {
        // Arrange
        var sut = new DisposableTracker();
        var mockDisposable = new Mock<IDisposable>();
        sut.Track(mockDisposable.Object);

        // Act
        sut.DisposeAll();
        sut.DisposeAll();
        sut.DisposeAll();

        // Assert - should only be called once
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_SetsIsDisposedTrue()
    {
        // Arrange
        var sut = new DisposableTracker();
        sut.IsDisposed.Should().BeFalse();

        // Act
        sut.Dispose();

        // Assert
        sut.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DisposesAllTrackedSubscriptions()
    {
        // Arrange
        var sut = new DisposableTracker();
        var mockDisposable = new Mock<IDisposable>();
        sut.Track(mockDisposable.Object);

        // Act
        sut.Dispose();

        // Assert
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        var sut = new DisposableTracker();
        var mockDisposable = new Mock<IDisposable>();
        sut.Track(mockDisposable.Object);

        // Act
        sut.Dispose();
        sut.Dispose();
        sut.Dispose();

        // Assert
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Track_FromMultipleThreads_IsThreadSafe()
    {
        // Arrange
        var sut = new DisposableTracker();
        const int threadCount = 10;
        const int itemsPerThread = 100;

        // Act
        Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < itemsPerThread; i++)
            {
                sut.Track(Mock.Of<IDisposable>());
            }
        });

        // Assert
        sut.Count.Should().Be(threadCount * itemsPerThread);
    }

    #endregion
}
