using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for the <see cref="IngestionQueue"/> implementation.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the channel-based queue behavior including
/// enqueue/dequeue operations, priority ordering, duplicate detection,
/// and thread-safety characteristics.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2d")]
public class IngestionQueueTests : IDisposable
{
    private readonly IngestionQueue _sut;
    private readonly IOptions<IngestionQueueOptions> _options;

    public IngestionQueueTests()
    {
        _options = Options.Create(IngestionQueueOptions.Default);
        _sut = new IngestionQueue(_options, NullLogger<IngestionQueue>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngestionQueue(null!, NullLogger<IngestionQueue>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(IngestionQueueOptions.Default);

        // Act
        var act = () => new IngestionQueue(options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_InvalidOptions_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = Options.Create(new IngestionQueueOptions(MaxQueueSize: 0));

        // Act
        var act = () => new IngestionQueue(invalidOptions, NullLogger<IngestionQueue>.Instance);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Count_EmptyQueue_ReturnsZero()
    {
        // Assert
        _sut.Count.Should().Be(0);
    }

    [Fact]
    public async Task Count_AfterEnqueue_ReturnsCorrectCount()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");

        // Act
        await _sut.EnqueueAsync(item);

        // Assert
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public void IsEmpty_EmptyQueue_ReturnsTrue()
    {
        // Assert
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmpty_NonEmptyQueue_ReturnsFalse()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        await _sut.EnqueueAsync(item);

        // Assert
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsCompleted_NewQueue_ReturnsFalse()
    {
        // Assert
        _sut.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsCompleted_AfterComplete_ReturnsTrue()
    {
        // Act
        _sut.Complete();

        // Assert
        _sut.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region EnqueueAsync Tests

    [Fact]
    public async Task EnqueueAsync_ValidItem_ReturnsTrue()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");

        // Act
        var result = await _sut.EnqueueAsync(item);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAsync_NullItem_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.EnqueueAsync((IngestionQueueItem)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("item");
    }

    [Fact]
    public async Task EnqueueAsync_AfterComplete_ThrowsInvalidOperationException()
    {
        // Arrange
        _sut.Complete();
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");

        // Act
        var act = async () => await _sut.EnqueueAsync(item);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EnqueueAsync_RaisesItemEnqueuedEvent()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        IngestionQueueItem? raisedItem = null;
        _sut.ItemEnqueued += (_, e) => raisedItem = e;

        // Act
        await _sut.EnqueueAsync(item);

        // Assert
        raisedItem.Should().Be(item);
    }

    [Fact]
    public async Task EnqueueAsync_ConvenienceOverload_CreatesAndEnqueuesItem()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";

        // Act
        var result = await _sut.EnqueueAsync(projectId, filePath);

        // Assert
        result.Should().BeTrue();
        _sut.Count.Should().Be(1);
    }

    #endregion

    #region Duplicate Detection Tests

    [Fact]
    public async Task EnqueueAsync_DuplicatePath_WithinWindow_ReturnsFalse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";

        // Act
        var first = await _sut.EnqueueAsync(projectId, filePath);
        var second = await _sut.EnqueueAsync(projectId, filePath);

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public async Task EnqueueAsync_DuplicatePath_IsCaseInsensitive()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var first = await _sut.EnqueueAsync(projectId, "/PATH/TO/FILE.md");
        var second = await _sut.EnqueueAsync(projectId, "/path/to/file.md");

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task EnqueueAsync_DifferentPaths_BothSucceed()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var first = await _sut.EnqueueAsync(projectId, "/path/to/file1.md");
        var second = await _sut.EnqueueAsync(projectId, "/path/to/file2.md");

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        _sut.Count.Should().Be(2);
    }

    [Fact]
    public async Task EnqueueAsync_DuplicateDetectionDisabled_AllowsDuplicates()
    {
        // Arrange
        var options = Options.Create(new IngestionQueueOptions(EnableDuplicateDetection: false));
        using var queue = new IngestionQueue(options, NullLogger<IngestionQueue>.Instance);
        var projectId = Guid.NewGuid();
        var filePath = "/path/to/file.md";

        // Act
        var first = await queue.EnqueueAsync(projectId, filePath);
        var second = await queue.EnqueueAsync(projectId, filePath);

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        queue.Count.Should().Be(2);
    }

    #endregion

    #region DequeueAsync Tests

    [Fact]
    public async Task DequeueAsync_SingleItem_ReturnsItem()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        await _sut.EnqueueAsync(item);

        // Act
        var dequeued = await _sut.DequeueAsync();

        // Assert
        dequeued.Should().Be(item);
    }

    [Fact]
    public async Task DequeueAsync_RaisesItemDequeuedEvent()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        await _sut.EnqueueAsync(item);
        IngestionQueueItem? raisedItem = null;
        _sut.ItemDequeued += (_, e) => raisedItem = e;

        // Act
        await _sut.DequeueAsync();

        // Assert
        raisedItem.Should().Be(item);
    }

    [Fact]
    public async Task DequeueAsync_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _sut.DequeueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Priority Ordering Tests

    [Fact]
    public async Task DequeueAsync_PriorityOrdering_ReturnsHighestPriorityFirst()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var lowPriority = IngestionQueueItem.Create(projectId, "/path/low.md", priority: 3);
        var highPriority = IngestionQueueItem.Create(projectId, "/path/high.md", priority: 0);
        var normalPriority = IngestionQueueItem.Create(projectId, "/path/normal.md", priority: 2);

        // Enqueue in random order
        await _sut.EnqueueAsync(lowPriority);
        await _sut.EnqueueAsync(highPriority);
        await _sut.EnqueueAsync(normalPriority);

        // Act
        var first = await _sut.DequeueAsync();
        var second = await _sut.DequeueAsync();
        var third = await _sut.DequeueAsync();

        // Assert
        first.Priority.Should().Be(0);
        second.Priority.Should().Be(2);
        third.Priority.Should().Be(3);
    }

    [Fact]
    public async Task DequeueAsync_SamePriority_FIFO()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var first = IngestionQueueItem.Create(projectId, "/path/first.md", priority: 2);
        await Task.Delay(5); // Ensure different EnqueuedAt timestamps
        var second = IngestionQueueItem.Create(projectId, "/path/second.md", priority: 2);
        await Task.Delay(5);
        var third = IngestionQueueItem.Create(projectId, "/path/third.md", priority: 2);

        await _sut.EnqueueAsync(first);
        await _sut.EnqueueAsync(second);
        await _sut.EnqueueAsync(third);

        // Act
        var dequeue1 = await _sut.DequeueAsync();
        var dequeue2 = await _sut.DequeueAsync();
        var dequeue3 = await _sut.DequeueAsync();

        // Assert - FIFO within same priority
        dequeue1.FilePath.Should().Be("/path/first.md");
        dequeue2.FilePath.Should().Be("/path/second.md");
        dequeue3.FilePath.Should().Be("/path/third.md");
    }

    #endregion

    #region TryDequeueAsync Tests

    [Fact]
    public async Task TryDequeueAsync_EmptyQueue_ReturnsNull()
    {
        // Act
        var result = await _sut.TryDequeueAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryDequeueAsync_NonEmptyQueue_ReturnsItem()
    {
        // Arrange
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        await _sut.EnqueueAsync(item);

        // Act
        var result = await _sut.TryDequeueAsync();

        // Assert
        result.Should().Be(item);
    }

    [Fact]
    public async Task TryDequeueAsync_Cancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _sut.TryDequeueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_MarksQueueAsCompleted()
    {
        // Act
        _sut.Complete();

        // Assert
        _sut.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Complete_CalledMultipleTimes_DoesNotThrow()
    {
        // Act
        var act = () =>
        {
            _sut.Complete();
            _sut.Complete();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_RemovesAllItems()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await _sut.EnqueueAsync(projectId, "/path/file1.md");
        await _sut.EnqueueAsync(projectId, "/path/file2.md");
        await _sut.EnqueueAsync(projectId, "/path/file3.md");

        // Act
        var cleared = _sut.Clear();

        // Assert
        cleared.Should().Be(3);
        _sut.Count.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Clear_EmptyQueue_ReturnsZero()
    {
        // Act
        var cleared = _sut.Clear();

        // Assert
        cleared.Should().Be(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CompletesQueue()
    {
        // Act
        _sut.Dispose();

        // Assert
        _sut.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");

        // Act
        var act = async () => await _sut.EnqueueAsync(item);

        // Assert
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act
        var act = () =>
        {
            _sut.Dispose();
            _sut.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task EnqueueAsync_ConcurrentEnqueues_AllSucceed()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var count = 100;
        var tasks = new List<Task<bool>>();

        // Act
        for (int i = 0; i < count; i++)
        {
            var path = $"/path/to/file{i}.md";
            tasks.Add(_sut.EnqueueAsync(projectId, path));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        _sut.Count.Should().Be(count);
    }

    [Fact]
    public async Task ConcurrentEnqueueAndDequeue_NoDataLoss()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var itemCount = 50;
        var dequeued = new List<IngestionQueueItem>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - producer
        var producerTask = Task.Run(async () =>
        {
            for (int i = 0; i < itemCount; i++)
            {
                await _sut.EnqueueAsync(projectId, $"/path/file{i}.md");
                await Task.Delay(1); // Small delay to interleave
            }
            // Don't complete yet - let consumer drain
        });

        // Consumer
        var consumerTask = Task.Run(async () =>
        {
            while (dequeued.Count < itemCount)
            {
                var item = await _sut.TryDequeueAsync(cts.Token);
                if (item != null)
                {
                    lock (dequeued)
                    {
                        dequeued.Add(item);
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        });

        await Task.WhenAll(producerTask, consumerTask);

        // Assert
        dequeued.Should().HaveCount(itemCount);
    }

    #endregion
}
