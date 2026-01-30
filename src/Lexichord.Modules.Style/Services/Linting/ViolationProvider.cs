using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Provides style violations to the rendering layer.
/// </summary>
/// <remarks>
/// LOGIC: ViolationProvider acts as the bridge between the linting pipeline
/// and the editor rendering. It maintains the current set of violations for
/// a document and notifies renderers when violations change.
///
/// Thread Safety:
/// - Uses lock for write operations (UpdateViolations, Clear)
/// - Returns immutable snapshots for read operations
/// - ViolationsChanged event is raised synchronously on calling thread
///
/// Version: v0.2.4a
/// </remarks>
public sealed class ViolationProvider : IViolationProvider
{
    private readonly ILogger<ViolationProvider> _logger;
    private readonly object _lock = new();
    private IReadOnlyList<AggregatedStyleViolation> _violations = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViolationProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public ViolationProvider(ILogger<ViolationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public event EventHandler<ViolationsChangedEventArgs>? ViolationsChanged;

    /// <inheritdoc />
    public IReadOnlyList<AggregatedStyleViolation> AllViolations
    {
        get
        {
            lock (_lock)
            {
                return _violations;
            }
        }
    }

    /// <inheritdoc />
    public void UpdateViolations(IReadOnlyList<AggregatedStyleViolation> violations)
    {
        ArgumentNullException.ThrowIfNull(violations);
        ThrowIfDisposed();

        lock (_lock)
        {
            _violations = violations;
        }

        _logger.LogDebug(
            "Violations updated: {Count} total",
            violations.Count);

        // LOGIC: Create event args with counts
        var args = ViolationsChangedEventArgs.ForReplaced(violations);
        OnViolationsChanged(args);
    }

    /// <inheritdoc />
    public void Clear()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            _violations = [];
        }

        _logger.LogDebug("Violations cleared");

        var args = ViolationsChangedEventArgs.ForCleared();
        OnViolationsChanged(args);
    }

    /// <inheritdoc />
    public IReadOnlyList<AggregatedStyleViolation> GetViolationsInRange(
        int startOffset,
        int endOffset)
    {
        ThrowIfDisposed();

        // LOGIC: Get snapshot under lock, then filter without lock
        IReadOnlyList<AggregatedStyleViolation> snapshot;
        lock (_lock)
        {
            snapshot = _violations;
        }

        // LOGIC: Find violations that overlap [startOffset, endOffset)
        var result = snapshot
            .Where(v => v.StartOffset < endOffset && v.EndOffset > startOffset)
            .ToList();

        _logger.LogDebug(
            "Query violations in range [{Start}, {End}): {Count} found",
            startOffset,
            endOffset,
            result.Count);

        return result;
    }

    /// <inheritdoc />
    public AggregatedStyleViolation? GetViolationAt(int offset)
    {
        ThrowIfDisposed();

        // LOGIC: Get snapshot under lock, then search without lock
        IReadOnlyList<AggregatedStyleViolation> snapshot;
        lock (_lock)
        {
            snapshot = _violations;
        }

        return snapshot.FirstOrDefault(v => v.ContainsOffset(offset));
    }

    /// <inheritdoc />
    public IReadOnlyList<AggregatedStyleViolation> GetViolationsAtOffset(int offset)
    {
        ThrowIfDisposed();

        // LOGIC: Get snapshot under lock, then filter without lock
        IReadOnlyList<AggregatedStyleViolation> snapshot;
        lock (_lock)
        {
            snapshot = _violations;
        }

        var result = snapshot.Where(v => v.ContainsOffset(offset)).ToList();

        _logger.LogDebug(
            "Query violations at offset {Offset}: {Count} found",
            offset,
            result.Count);

        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_lock)
        {
            _violations = [];
        }

        _logger.LogDebug("ViolationProvider disposed");
    }

    /// <summary>
    /// Raises the ViolationsChanged event.
    /// </summary>
    private void OnViolationsChanged(ViolationsChangedEventArgs args)
    {
        ViolationsChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Throws if the provider has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
