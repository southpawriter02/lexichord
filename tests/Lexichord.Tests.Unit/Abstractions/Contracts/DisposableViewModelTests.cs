using System;
using System.Reactive.Subjects;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Contracts;

/// <summary>
/// Unit tests for <see cref="DisposableViewModel"/>.
/// </summary>
public class DisposableViewModelTests
{
    #region Test Implementation

    /// <summary>
    /// Test implementation of DisposableViewModel for unit testing.
    /// </summary>
    private sealed class TestDisposableViewModel : DisposableViewModel
    {
        public bool OnDisposedWasCalled { get; private set; }
        public int OnDisposedCallCount { get; private set; }

        public void TrackTestSubscription(IDisposable subscription) => Track(subscription);
        public void TrackTestSubscriptions(params IDisposable[] subscriptions) => TrackAll(subscriptions);
        public int TestSubscriptionCount => SubscriptionCount;

        protected override void OnDisposed()
        {
            OnDisposedWasCalled = true;
            OnDisposedCallCount++;
        }
    }

    #endregion

    #region Track Tests

    [Fact]
    public void Track_SingleSubscription_IncrementsCount()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        var mockDisposable = new Mock<IDisposable>();

        // Act
        sut.TrackTestSubscription(mockDisposable.Object);

        // Assert
        sut.TestSubscriptionCount.Should().Be(1);
    }

    [Fact]
    public void TrackAll_MultipleSubscriptions_IncrementsCount()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        var d1 = Mock.Of<IDisposable>();
        var d2 = Mock.Of<IDisposable>();

        // Act
        sut.TrackTestSubscriptions(d1, d2);

        // Assert
        sut.TestSubscriptionCount.Should().Be(2);
    }

    [Fact]
    public void Track_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        sut.Dispose();

        // Act
        var action = () => sut.TrackTestSubscription(Mock.Of<IDisposable>());

        // Assert
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesAllSubscriptions()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        var d1 = new Mock<IDisposable>();
        var d2 = new Mock<IDisposable>();
        sut.TrackTestSubscription(d1.Object);
        sut.TrackTestSubscription(d2.Object);

        // Act
        sut.Dispose();

        // Assert
        d1.Verify(d => d.Dispose(), Times.Once);
        d2.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CallsOnDisposed()
    {
        // Arrange
        var sut = new TestDisposableViewModel();

        // Act
        sut.Dispose();

        // Assert
        sut.OnDisposedWasCalled.Should().BeTrue();
    }

    [Fact]
    public void Dispose_SetsIsDisposedTrue()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        sut.IsDisposed.Should().BeFalse();

        // Act
        sut.Dispose();

        // Assert
        sut.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        var mockDisposable = new Mock<IDisposable>();
        sut.TrackTestSubscription(mockDisposable.Object);

        // Act
        sut.Dispose();
        sut.Dispose();
        sut.Dispose();

        // Assert
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
        sut.OnDisposedCallCount.Should().Be(1);
    }

    #endregion

    #region Memory Leak Tests

    [Fact]
    public void DisposedViewModel_IsGarbageCollected()
    {
        // Arrange
        WeakReference<TestDisposableViewModel>? weakRef = null;

        void CreateAndDispose()
        {
            var vm = new TestDisposableViewModel();
            var subject = new Subject<int>();
            vm.TrackTestSubscription(subject.Subscribe(_ => { }));
            weakRef = new WeakReference<TestDisposableViewModel>(vm);
            vm.Dispose();
            subject.Dispose();
        }

        // Act
        CreateAndDispose();
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert
        weakRef!.TryGetTarget(out _).Should().BeFalse(
            "Disposed ViewModel should be garbage collected");
    }

    [Fact]
    public void DisposedViewModel_StopsReceivingEvents()
    {
        // Arrange
        var sut = new TestDisposableViewModel();
        var subject = new Subject<int>();
        var receivedValues = new List<int>();

        sut.TrackTestSubscription(subject.Subscribe(v => receivedValues.Add(v)));

        // Act - emit before dispose
        subject.OnNext(1);
        subject.OnNext(2);
        
        sut.Dispose();
        
        // Emit after dispose
        subject.OnNext(3);
        subject.OnNext(4);

        // Assert - should only receive values before dispose
        receivedValues.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void RepeatedCreationAndDisposal_DoesNotLeakMemory()
    {
        // Arrange
        var weakRefs = new List<WeakReference<TestDisposableViewModel>>();
        const int iterations = 100;

        // Act - create and dispose many ViewModels
        for (var i = 0; i < iterations; i++)
        {
            var vm = new TestDisposableViewModel();
            var subject = new Subject<int>();
            vm.TrackTestSubscription(subject.Subscribe(_ => { }));
            weakRefs.Add(new WeakReference<TestDisposableViewModel>(vm));
            vm.Dispose();
            subject.Dispose();
        }

        // Force garbage collection multiple times
        for (var i = 0; i < 5; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // Assert - allow for slight GC timing variations, but most should be collected
        var stillAlive = weakRefs.Count(wr => wr.TryGetTarget(out _));
        var collectedPercentage = (iterations - stillAlive) / (double)iterations * 100;
        
        collectedPercentage.Should().BeGreaterThan(95, 
            "At least 95% of disposed ViewModels should be garbage collected (collected {0}%)", 
            collectedPercentage);
    }

    #endregion
}
