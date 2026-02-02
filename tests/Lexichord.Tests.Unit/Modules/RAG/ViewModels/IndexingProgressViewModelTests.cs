// =============================================================================
// File: IndexingProgressViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexingProgressViewModel.
// Version: v0.4.7c
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="IndexingProgressViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover initial state, progress text formatting, cancellation, and elapsed time formatting.
/// </remarks>
[Trait("Feature", "v0.4.7")]
[Trait("Category", "Unit")]
public class IndexingProgressViewModelTests : IDisposable
{
    private readonly IndexingProgressViewModel _sut;

    public IndexingProgressViewModelTests()
    {
        _sut = new IndexingProgressViewModel(NullLogger<IndexingProgressViewModel>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexingProgressViewModel(null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        Assert.False(_sut.IsVisible);
        Assert.Equal(string.Empty, _sut.OperationTitle);
        Assert.Equal(string.Empty, _sut.CurrentDocument);
        Assert.Equal(0, _sut.ProcessedCount);
        Assert.Equal(0, _sut.TotalCount);
        Assert.Equal(0, _sut.PercentComplete);
        Assert.False(_sut.IsComplete);
        Assert.False(_sut.WasCancelled);
    }

    #endregion

    #region Show Tests

    [Fact]
    public void Show_SetsInitialState()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        _sut.Show("Test Operation", 10, cts);

        // Assert
        Assert.True(_sut.IsVisible);
        Assert.Equal("Test Operation", _sut.OperationTitle);
        Assert.Equal(10, _sut.TotalCount);
        Assert.Equal(0, _sut.ProcessedCount);
        Assert.Equal(0, _sut.PercentComplete);
        Assert.False(_sut.IsComplete);
        Assert.False(_sut.WasCancelled);
    }

    [Fact]
    public void Show_SetsCanCancelToTrue()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        _sut.Show("Test", 10, cts);

        // Assert
        Assert.True(_sut.CanCancel);
    }

    #endregion

    #region ProgressText Tests

    [Fact]
    public void ProgressText_FormatsCorrectly()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 10, cts);

        // Assert initial state
        Assert.Equal("0 / 10", _sut.ProgressText);
    }

    [Fact]
    public void ProgressText_FormatsWithProcessedAndTotal()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 20, cts);

        // Assert - verify format is "X / Y"
        Assert.Matches(@"^\d+ / \d+$", _sut.ProgressText);
        Assert.Equal("0 / 20", _sut.ProgressText);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_RequestsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 10, cts);

        // Act
        _sut.CancelCommand.Execute(null);

        // Assert
        Assert.True(cts.Token.IsCancellationRequested);
        Assert.True(_sut.WasCancelled);
    }

    [Fact]
    public void CanCancel_FalseWhenNotVisible()
    {
        // Assert - not visible by default
        Assert.False(_sut.IsVisible);
        Assert.False(_sut.CanCancel);
    }

    [Fact]
    public void CanCancel_FalseWhenAlreadyCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _sut.Show("Test", 10, cts);
        _sut.CancelCommand.Execute(null);

        // Assert
        Assert.True(_sut.WasCancelled);
        Assert.False(_sut.CanCancel);
    }

    #endregion

    #region FormatElapsedTime Tests

    [Theory]
    [InlineData(30, "30s")]
    [InlineData(90, "1m 30s")]
    [InlineData(3700, "1h 1m")]
    [InlineData(0, "0s")]
    [InlineData(59, "59s")]
    [InlineData(60, "1m 0s")]
    [InlineData(3600, "1h 0m")]
    [InlineData(7200, "2h 0m")]
    public void FormatElapsedTime_FormatsCorrectly(int totalSeconds, string expected)
    {
        // Arrange
        var elapsed = TimeSpan.FromSeconds(totalSeconds);

        // Act
        var result = IndexingProgressViewModel.FormatElapsedTime(elapsed);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IDisposable Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - should not throw
        _sut.Dispose();
        _sut.Dispose();
    }

    #endregion
}
