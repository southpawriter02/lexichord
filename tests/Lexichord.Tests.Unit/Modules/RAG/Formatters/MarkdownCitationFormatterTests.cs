// =============================================================================
// File: MarkdownCitationFormatterTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for MarkdownCitationFormatter.
// =============================================================================
// LOGIC: Verifies Markdown citation formatting:
//   - Format with line number produces [Title](file:///path#Lline).
//   - Format without line number omits #Lline fragment.
//   - Spaces in path are URL-encoded as %20.
//   - Uses DocumentTitle as link text.
//   - FormatForClipboard returns same output as Format.
//   - Style property returns CitationStyle.Markdown.
//   - DisplayName, Description, and Example properties are non-empty.
//   - Null citation throws ArgumentNullException.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Formatters;

namespace Lexichord.Tests.Unit.Modules.RAG.Formatters;

/// <summary>
/// Unit tests for <see cref="MarkdownCitationFormatter"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2b")]
public class MarkdownCitationFormatterTests
{
    private readonly MarkdownCitationFormatter _sut = new();

    // =========================================================================
    // Metadata Properties
    // =========================================================================

    /// <summary>
    /// Verifies the Style property returns CitationStyle.Markdown.
    /// </summary>
    [Fact]
    public void Style_ReturnsMarkdown()
    {
        // Assert
        Assert.Equal(CitationStyle.Markdown, _sut.Style);
    }

    /// <summary>
    /// Verifies the DisplayName property is "Markdown Link".
    /// </summary>
    [Fact]
    public void DisplayName_ReturnsMarkdownLink()
    {
        // Assert
        Assert.Equal("Markdown Link", _sut.DisplayName);
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
    /// Verifies formatting with line number includes #Lline fragment.
    /// </summary>
    [Fact]
    public void Format_WithLineNumber_IncludesLineAnchor()
    {
        // Arrange
        var citation = CreateCitation(
            documentTitle: "OAuth Guide",
            documentPath: "/docs/auth.md",
            lineNumber: 42);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[OAuth Guide](file:///docs/auth.md#L42)", result);
    }

    /// <summary>
    /// Verifies formatting without line number omits fragment.
    /// </summary>
    [Fact]
    public void Format_WithoutLineNumber_OmitsLineAnchor()
    {
        // Arrange
        var citation = CreateCitation(
            documentTitle: "OAuth Guide",
            documentPath: "/docs/auth.md",
            lineNumber: null);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Equal("[OAuth Guide](file:///docs/auth.md)", result);
    }

    /// <summary>
    /// Verifies that spaces in paths are URL-encoded as %20.
    /// </summary>
    [Fact]
    public void Format_WithSpacesInPath_UrlEncodesSpaces()
    {
        // Arrange
        var citation = CreateCitation(
            documentTitle: "My Guide",
            documentPath: "/docs/my guide.md",
            lineNumber: 10);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Contains("/docs/my%20guide.md", result);
        Assert.DoesNotContain("/docs/my guide.md", result);
    }

    /// <summary>
    /// Verifies the document title is used as the link text.
    /// </summary>
    [Fact]
    public void Format_UsesDocumentTitleAsLinkText()
    {
        // Arrange
        var citation = CreateCitation(
            documentTitle: "Authentication Reference",
            documentPath: "/docs/auth.md",
            lineNumber: 1);

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.StartsWith("[Authentication Reference]", result);
    }

    /// <summary>
    /// Verifies the file:// scheme is used in the URI.
    /// </summary>
    [Fact]
    public void Format_UsesFileScheme()
    {
        // Arrange
        var citation = CreateCitation(documentPath: "/docs/test.md");

        // Act
        var result = _sut.Format(citation);

        // Assert
        Assert.Contains("file://", result);
    }

    /// <summary>
    /// Verifies FormatForClipboard returns the same output as Format.
    /// </summary>
    [Fact]
    public void FormatForClipboard_ReturnsSameAsFormat()
    {
        // Arrange
        var citation = CreateCitation(
            documentTitle: "Test",
            documentPath: "/docs/test.md",
            lineNumber: 5);

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
