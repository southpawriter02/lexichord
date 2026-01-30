using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Style.Rendering;

/// <summary>
/// Unit tests for <see cref="ViolationProvider"/>.
/// </summary>
public sealed class ViolationProviderTests : IDisposable
{
    private readonly FakeLogger<ViolationProvider> _logger = new();
    private readonly ViolationProvider _sut;

    public ViolationProviderTests()
    {
        _sut = new ViolationProvider(_logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Initial State

    [Fact]
    public void AllViolations_Initially_ReturnsEmptyList()
    {
        // Act
        var violations = _sut.AllViolations;

        // Assert
        Assert.Empty(violations);
    }

    #endregion

    #region UpdateViolations

    [Fact]
    public void UpdateViolations_WithValidList_UpdatesAllViolations()
    {
        // Arrange
        var violations = CreateTestViolations(3);

        // Act
        _sut.UpdateViolations(violations);

        // Assert
        Assert.Equal(3, _sut.AllViolations.Count);
    }

    [Fact]
    public void UpdateViolations_ReplacesExistingViolations()
    {
        // Arrange
        var initial = CreateTestViolations(2);
        var replacement = CreateTestViolations(5);
        _sut.UpdateViolations(initial);

        // Act
        _sut.UpdateViolations(replacement);

        // Assert
        Assert.Equal(5, _sut.AllViolations.Count);
    }

    [Fact]
    public void UpdateViolations_RaisesViolationsChangedEvent()
    {
        // Arrange
        var violations = CreateTestViolations(3);
        ViolationsChangedEventArgs? eventArgs = null;
        _sut.ViolationsChanged += (_, e) => eventArgs = e;

        // Act
        _sut.UpdateViolations(violations);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ViolationChangeType.Replaced, eventArgs.ChangeType);
        Assert.Equal(3, eventArgs.TotalCount);
    }

    [Fact]
    public void UpdateViolations_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.UpdateViolations(null!));
    }

    [Fact]
    public void UpdateViolations_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            _sut.UpdateViolations(CreateTestViolations(1)));
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllViolations()
    {
        // Arrange
        _sut.UpdateViolations(CreateTestViolations(5));

        // Act
        _sut.Clear();

        // Assert
        Assert.Empty(_sut.AllViolations);
    }

    [Fact]
    public void Clear_RaisesViolationsChangedEvent()
    {
        // Arrange
        _sut.UpdateViolations(CreateTestViolations(3));
        ViolationsChangedEventArgs? eventArgs = null;
        _sut.ViolationsChanged += (_, e) => eventArgs = e;

        // Act
        _sut.Clear();

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(ViolationChangeType.Cleared, eventArgs.ChangeType);
        Assert.Equal(0, eventArgs.TotalCount);
    }

    [Fact]
    public void Clear_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.Clear());
    }

    #endregion

    #region GetViolationsInRange

    [Fact]
    public void GetViolationsInRange_ReturnsViolationsInRange()
    {
        // Arrange
        var violations = new List<AggregatedStyleViolation>
        {
            CreateViolation("1", 0, 10),   // Fully in range 0-20
            CreateViolation("2", 15, 10),  // Partially in range 0-20
            CreateViolation("3", 25, 10),  // Outside range 0-20
        };
        _sut.UpdateViolations(violations);

        // Act
        var result = _sut.GetViolationsInRange(0, 20);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Id == "1");
        Assert.Contains(result, v => v.Id == "2");
    }

    [Fact]
    public void GetViolationsInRange_ReturnsEmpty_WhenNoViolationsInRange()
    {
        // Arrange
        var violations = new List<AggregatedStyleViolation>
        {
            CreateViolation("1", 100, 10),
            CreateViolation("2", 200, 10),
        };
        _sut.UpdateViolations(violations);

        // Act
        var result = _sut.GetViolationsInRange(0, 50);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetViolationsInRange_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.GetViolationsInRange(0, 100));
    }

    #endregion

    #region GetViolationAt

    [Fact]
    public void GetViolationAt_ReturnsViolationContainingOffset()
    {
        // Arrange
        var violations = new List<AggregatedStyleViolation>
        {
            CreateViolation("1", 0, 10),
            CreateViolation("2", 20, 10),
        };
        _sut.UpdateViolations(violations);

        // Act
        var result = _sut.GetViolationAt(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
    }

    [Fact]
    public void GetViolationAt_ReturnsNull_WhenNoViolationAtOffset()
    {
        // Arrange
        var violations = new List<AggregatedStyleViolation>
        {
            CreateViolation("1", 0, 10),
            CreateViolation("2", 20, 10),
        };
        _sut.UpdateViolations(violations);

        // Act
        var result = _sut.GetViolationAt(15);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetViolationAt_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _sut.GetViolationAt(0));
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task ConcurrentUpdates_DoNotCauseExceptions()
    {
        // Arrange
        const int iterations = 100;
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < iterations; i++)
        {
            var count = i;
            tasks.Add(Task.Run(() => _sut.UpdateViolations(CreateTestViolations(count % 10))));
            tasks.Add(Task.Run(() => _ = _sut.AllViolations));
            tasks.Add(Task.Run(() => _ = _sut.GetViolationsInRange(0, 100)));
        }

        // Assert - should complete without exceptions
        await Task.WhenAll(tasks);
        Assert.True(true);
    }

    #endregion

    #region Helpers

    private static List<AggregatedStyleViolation> CreateTestViolations(int count)
    {
        var violations = new List<AggregatedStyleViolation>();
        for (var i = 0; i < count; i++)
        {
            violations.Add(CreateViolation($"v{i}", i * 20, 10));
        }
        return violations;
    }

    private static AggregatedStyleViolation CreateViolation(string id, int startOffset, int length)
    {
        return new AggregatedStyleViolation
        {
            Id = id,
            DocumentId = "test-doc",
            RuleId = "test-rule",
            StartOffset = startOffset,
            Length = length,
            Line = 1,
            Column = startOffset + 1,
            EndLine = 1,
            EndColumn = startOffset + length + 1,
            ViolatingText = "test",
            Message = "Test violation",
            Severity = ViolationSeverity.Warning,
            Category = RuleCategory.Terminology
        };
    }

    #endregion
}
