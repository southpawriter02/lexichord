// =============================================================================
// File: FootnoteCitationFormatterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for FootnoteCitationFormatter.
// =============================================================================
// LOGIC: Verifies footnote citation formatting:
//   - Format with line number produces [^id]: /path:line.
//   - Format without line number omits :line suffix.
//   - Footnote ID is first 8 characters of ChunkId hex.
//   - FormatForClipboard returns same output as Format.
//   - Style property returns CitationStyle.Footnote.
//   - DisplayName, Description, and Example properties are non-empty.
//   - Null citation throws ArgumentNullException.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Formatters;

namespace Lexichord.Tests.Unit.Modules.RAG.Formatters;

/// <summary>
/// Unit tests for <see cref="FootnoteCitationFormatter"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2b")]
public class FootnoteCitationFormatterTests
{
    private readonly FootnoteCitationFormatter _sut = new();

    // LOGIC: Known GUID for predictable footnote identifier testing.
    // "aabbccdd-1111-2222-3333-444455556666" → "N" format → "aabbccdd111122223333444455556666"
    // First 8 chars: "aabbccdd"
    private static readonly Guid KnownChunkId = Guid.Parse("aabbccdd-1111-2222-3333-444455556666");

    // =========================================================================
    // Metadata Properties
    // =========================================================================

    /// <summary>
    /// Verifies the Style property returns CitationStyle.Footnote.
    /// </summary>
    [Fact]
    public void Style_ReturnsFootnote()
    {
        // Assert
        Assert.Equal(CitationStyle.Footnote, _sut.Style);
    }

    /// <summary>
    /// Verifies the DisplayName property is "Footnote".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsFootnote()
    {
        // Assert
        Assert.Equal("Footnote", _sut.DisplayName);
    }

    /// <summary>
    /// Verifies the Description property is non-null and non-empty.
    /// </summary>
    [Fact]
    public void Description_IsNotEmpty()
    {
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(_sut.Description));
    }

    /// <summary>
    /// Verifies the Example property is non-null and non-empty.
    /// </summary>
    [Fact]
    public void Example_IsNotEmpty()
    {
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(_sut.Example));
    }

    // =========================================================================
    // Format Tests
    // =========================================================================

    /// <summary>
    /// Verifies formatting with line number produces [^id]: /path:line.
    /// </summary>
    [Fact]
    public void Format_WithLineNumber_IncludesLineSuffix()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/api/auth.md",
            lineNumber: 42);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[^aabbccdd]: /docs/api/auth.md:42", result);
    }

    /// <summary>
    /// Verifies formatting without line number omits :line suffix.
    /// </summary>
    [Fact]
    public void Format_WithoutLineNumber_OmitsLineSuffix()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/api/auth.md",
            lineNumber: null);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[^aabbccdd]: /docs/api/auth.md", result);
    }

    /// <summary>
    /// Verifies the footnote ID is the first 8 hex characters of ChunkId.
    /// </summary>
    [Fact]
    public void Format_UsesFirst8HexCharsOfChunkId()
    {
        // Arrange
        var citation = CreateCitation(documentPath: "/test.md", lineNumber: 1);

        // Act
        var result = _sut.Format(citation);

        // Assert — verify the footnote starts with [^aabbccdd]
        Assert.StartsWith("[^aabbccdd]:", result);
    }

    /// <summary>
    /// Verifies format uses the full document path (not just filename).
    /// </summary>
    [Fact]
    public void Format_UsesFullDocumentPath()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/very/long/path/to/document.md",
            lineNumber: 10);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Contains("/very/long/path/to/document.md", result);
    }

    /// <summary>
    /// Verifies FormatForClipboard returns the same output as Format.
    /// </summary>
    [Fact]
    public void FormatForClipboard_ReturnsSameAsFormat()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/test.md",
            lineNumber: 15);

        // Act
        var format = _sut.Format(citation);
        var clipboard = _sut.FormatForClipboard(citation);

        // Assert
        Assert.Equal(format, clipboard);
    }

    // =========================================================================
    // Null Validation
    // =========================================================================

    /// <summary>
    /// Verifies Format throws ArgumentNullException for null citation.
    /// </summary>
    [Fact]
    public void Format_NullCitation_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.Format(null!));
    }

    /// <summary>
    /// Verifies FormatForClipboard throws ArgumentNullException for null citation.
    /// </summary>
    [Fact]
    public void FormatForClipboard_NullCitation_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sut.FormatForClipboard(null!));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a test Citation with the specified parameters.
    /// </summary>
    private static Citation CreateCitation(
        string documentPath = "/docs/test.md",
        string documentTitle = "Test Document",
        string? heading = "Introduction",
        int? lineNumber = 10)
    {
        return new Citation(
            ChunkId: KnownChunkId,
            DocumentPath: documentPath,
            DocumentTitle: documentTitle,
            StartOffset: 100,
            EndOffset: 200,
            Heading: heading,
            LineNumber: lineNumber,
            IndexedAt: DateTime.UtcNow);
    }
}
