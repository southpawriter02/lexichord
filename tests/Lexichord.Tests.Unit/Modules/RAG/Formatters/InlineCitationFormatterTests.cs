// =============================================================================
// File: InlineCitationFormatterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for InlineCitationFormatter.
// =============================================================================
// LOGIC: Verifies inline citation formatting:
//   - Format with heading produces [filename.md, §Heading].
//   - Format without heading produces [filename.md].
//   - Format with empty heading is treated as no heading.
//   - FormatForClipboard returns same output as Format.
//   - Style property returns CitationStyle.Inline.
//   - DisplayName, Description, and Example properties are non-empty.
//   - Null citation throws ArgumentNullException.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Formatters;

namespace Lexichord.Tests.Unit.Modules.RAG.Formatters;

/// <summary>
/// Unit tests for <see cref="InlineCitationFormatter"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2b")]
public class InlineCitationFormatterTests
{
    private readonly InlineCitationFormatter _sut = new();

    // =========================================================================
    // Metadata Properties
    // =========================================================================

    /// <summary>
    /// Verifies the Style property returns CitationStyle.Inline.
    /// </summary>
    [Fact]
    public void Style_ReturnsInline()
    {
        // Assert
        Assert.Equal(CitationStyle.Inline, _sut.Style);
    }

    /// <summary>
    /// Verifies the DisplayName property is "Inline".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsInline()
    {
        // Assert
        Assert.Equal("Inline", _sut.DisplayName);
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
    /// Verifies formatting with a heading produces [filename.md, §Heading].
    /// </summary>
    [Fact]
    public void Format_WithHeading_IncludesHeadingSection()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/auth-guide.md",
            heading: "Authentication");

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[auth-guide.md, §Authentication]", result);
    }

    /// <summary>
    /// Verifies formatting without a heading produces [filename.md].
    /// </summary>
    [Fact]
    public void Format_WithoutHeading_OmitsHeadingSection()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/guide.md",
            heading: null);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[guide.md]", result);
    }

    /// <summary>
    /// Verifies formatting with empty heading is treated as no heading.
    /// </summary>
    [Fact]
    public void Format_WithEmptyHeading_OmitsHeadingSection()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/guide.md",
            heading: "");

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[guide.md]", result);
    }

    /// <summary>
    /// Verifies that Format extracts filename correctly from full path.
    /// </summary>
    [Fact]
    public void Format_UsesFileNameNotFullPath()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/very/long/path/to/document.md",
            heading: "Section");

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[document.md, §Section]", result);
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
            heading: "Overview");

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
            ChunkId: Guid.Parse("aabbccdd-1111-2222-3333-444455556666"),
            DocumentPath: documentPath,
            DocumentTitle: documentTitle,
            StartOffset: 100,
            EndOffset: 200,
            Heading: heading,
            LineNumber: lineNumber,
            IndexedAt: DateTime.UtcNow);
    }
}
