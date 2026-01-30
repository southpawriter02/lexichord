namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Provides efficient offset-to-line/column mapping using pre-computed line starts.
/// </summary>
/// <remarks>
/// LOGIC: Position calculation is O(log n) using binary search instead of
/// O(n) linear scan through content. For a 10,000 line document, this means
/// ~14 comparisons instead of scanning thousands of characters.
///
/// Construction: O(n) - single pass to find all newlines
/// Lookup: O(log n) - binary search through line starts
///
/// Thread Safety: Immutable after construction, safe for concurrent reads.
///
/// Version: v0.2.3d
/// </remarks>
internal sealed class PositionIndex
{
    private readonly int[] _lineStarts;
    private readonly int _contentLength;

    /// <summary>
    /// Creates a new position index for the given content.
    /// </summary>
    /// <param name="content">The document content to index.</param>
    /// <remarks>
    /// LOGIC: Single pass through content to record start offset of each line.
    /// Line 1 always starts at offset 0.
    /// </remarks>
    public PositionIndex(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            _lineStarts = [0];
            _contentLength = 0;
            return;
        }

        _contentLength = content.Length;
        var lineStarts = new List<int> { 0 };

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                // LOGIC: Next line starts after the newline
                lineStarts.Add(i + 1);
            }
        }

        _lineStarts = lineStarts.ToArray();
    }

    /// <summary>
    /// Gets the total number of lines in the indexed content.
    /// </summary>
    public int LineCount => _lineStarts.Length;

    /// <summary>
    /// Gets the 1-based line and column for a character offset.
    /// </summary>
    /// <param name="offset">Character offset (0-indexed).</param>
    /// <returns>Tuple of (Line, Column), both 1-indexed.</returns>
    /// <remarks>
    /// LOGIC: Uses binary search to find which line contains the offset,
    /// then computes column as distance from line start.
    ///
    /// Edge cases:
    /// - Negative offset → (1, 1)
    /// - Offset beyond content → last valid position
    /// - Empty content → (1, 1)
    /// </remarks>
    public (int Line, int Column) GetPosition(int offset)
    {
        // LOGIC: Handle edge cases
        if (offset <= 0)
        {
            return (1, 1);
        }

        if (_contentLength == 0)
        {
            return (1, 1);
        }

        // LOGIC: Clamp offset to valid range
        if (offset >= _contentLength)
        {
            offset = _contentLength;
        }

        var lineIndex = BinarySearchLine(offset);

        // LOGIC: Convert to 1-based
        var line = lineIndex + 1;
        var column = offset - _lineStarts[lineIndex] + 1;

        return (line, column);
    }

    /// <summary>
    /// Gets the start and end positions for a span.
    /// </summary>
    /// <param name="startOffset">Start of the span.</param>
    /// <param name="endOffset">End of the span (exclusive).</param>
    /// <returns>Start and end position tuples.</returns>
    public ((int Line, int Column) Start, (int Line, int Column) End) GetSpanPositions(
        int startOffset,
        int endOffset)
    {
        var start = GetPosition(startOffset);
        var end = GetPosition(endOffset);
        return (start, end);
    }

    /// <summary>
    /// Binary search to find the line containing an offset.
    /// </summary>
    /// <param name="offset">The offset to locate.</param>
    /// <returns>0-indexed line number.</returns>
    /// <remarks>
    /// LOGIC: Find largest lineIndex where _lineStarts[lineIndex] <= offset.
    /// This is the line containing the offset.
    /// </remarks>
    private int BinarySearchLine(int offset)
    {
        var low = 0;
        var high = _lineStarts.Length - 1;

        while (low <= high)
        {
            var mid = low + (high - low) / 2;

            // LOGIC: Check if offset is in line 'mid'
            // It's in line 'mid' if:
            // - _lineStarts[mid] <= offset, AND
            // - Either mid is the last line, OR offset < _lineStarts[mid + 1]
            if (mid == _lineStarts.Length - 1 ||
                (_lineStarts[mid] <= offset && offset < _lineStarts[mid + 1]))
            {
                return mid;
            }

            if (_lineStarts[mid] > offset)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        // LOGIC: Should not reach here with valid input, but return first line as fallback
        return 0;
    }
}
