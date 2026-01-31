# LCS-DES-088a: Design Specification — Export Format Tests

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `TST-088a` | Sub-part of TST-088 |
| **Feature Name** | `Export Format Fidelity Test Suite` | Format output verification tests |
| **Target Version** | `v0.8.8a` | First sub-part of v0.8.8 |
| **Module Scope** | `Lexichord.Tests.Publishing` | Test project |
| **Swimlane** | `Governance` | Part of Publishing vertical |
| **License Tier** | `Core` | Testing available to all |
| **Feature Gate Key** | N/A | No gating for tests |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-088-INDEX](./LCS-DES-088-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-088 Section 3.1](./LCS-SBD-088.md#31-v088a-export-format-tests) | |

---

## 2. Executive Summary

### 2.1 The Requirement

The export pipelines introduced in v0.8.x must produce correct, consistent output across all supported formats. Without verification tests:

- Markdown exports could lose formatting markers
- DOCX exports could produce invalid document structures
- PDF exports could render with incorrect dimensions or missing content
- HTML exports could fail accessibility or validity standards

> **Goal:** Verify that all export formats produce output matching golden reference files with 99%+ fidelity, preserving all formatting, structure, and content.

### 2.2 The Proposed Solution

Implement a comprehensive test suite using Verify.NET for snapshot testing that:

1. Compares Markdown exports against golden reference files
2. Validates DOCX document structure and styling
3. Verifies PDF rendering dimensions, content, and annotations
4. Ensures HTML output is valid and accessible
5. Tests edge cases including Unicode, complex formatting, and large documents

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IPdfExporter` | v0.8.6a | PDF export abstraction |
| `MarkdownPdfExporter` | v0.8.6b | PDF rendering implementation |
| `IMarkdownParser` | v0.1.3b | Markdown parsing |
| `IHtmlRenderer` | v0.8.6b | HTML rendering |
| `IDiffEngine` | v0.8.4a | Output comparison |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `xunit` | 2.9.x | Test framework |
| `FluentAssertions` | 6.x | Fluent assertions |
| `Verify.Xunit` | 24.x | Snapshot testing (NEW) |
| `DocumentFormat.OpenXml` | 3.x | DOCX validation |
| `PdfPig` | 0.1.x | PDF text extraction |
| `HtmlAgilityPack` | 1.11.x | HTML parsing |

### 3.2 Licensing Behavior

No licensing required. Tests run in development/CI environments only.

---

## 4. Data Contract (The API)

### 4.1 Test Class Structure

```csharp
namespace Lexichord.Tests.Publishing.ExportFormats;

/// <summary>
/// Fidelity tests for Markdown export functionality.
/// Verifies that exported Markdown matches expected output.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
[UsesVerify]
public class MarkdownExportTests
{
    private readonly IMarkdownExporter _exporter;
    private readonly VerifySettings _settings;

    public MarkdownExportTests()
    {
        _exporter = new MarkdownExporter();
        _settings = VerifySettingsFactory.CreateExportSettings();
    }

    // Test methods...
}

/// <summary>
/// Fidelity tests for DOCX export functionality.
/// Verifies document structure, styles, and formatting.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
public class DocxExportTests
{
    private readonly IDocxExporter _exporter;

    // Test methods...
}

/// <summary>
/// Fidelity tests for PDF export functionality.
/// Verifies rendering accuracy, dimensions, and content.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
[UsesVerify]
public class PdfExportTests
{
    private readonly IPdfExporter _exporter;

    // Test methods...
}

/// <summary>
/// Fidelity tests for HTML export functionality.
/// Verifies structure, validity, and accessibility.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
public class HtmlExportTests
{
    private readonly IHtmlExporter _exporter;

    // Test methods...
}
```

---

## 5. Implementation Logic

### 5.1 Snapshot Testing Workflow

```text
SNAPSHOT TEST FLOW:
|
+-- Generate export output
|   +-- Parse input document
|   +-- Apply export settings
|   +-- Render to target format
|
+-- Normalize output
|   +-- Strip timestamps
|   +-- Remove generated IDs
|   +-- Normalize line endings
|
+-- Compare to golden file
|   +-- First run: Create baseline
|   +-- Subsequent runs: Compare
|   +-- Mismatch: Fail with diff
|
+-- Report result
    +-- PASS: Output matches golden
    +-- FAIL: Diff shown, review needed
```

### 5.2 Export Fidelity Verification

```text
FIDELITY CHECKS:
|
+-- Markdown
|   +-- Heading levels (# through ######)
|   +-- Inline formatting (**bold**, *italic*, `code`)
|   +-- Block elements (code blocks, blockquotes)
|   +-- Lists (ordered, unordered, nested)
|   +-- Tables (headers, alignment, cells)
|   +-- Links and images
|
+-- DOCX
|   +-- Paragraph styles (Heading1, Normal, etc.)
|   +-- Character formatting (bold, italic, underline)
|   +-- Table structure (rows, columns, merged cells)
|   +-- Document parts (headers, footers, TOC)
|
+-- PDF
|   +-- Page dimensions (width, height in points)
|   +-- Text content extraction
|   +-- Font embedding
|   +-- Image rendering
|   +-- Annotation presence
|
+-- HTML
|   +-- DOCTYPE declaration
|   +-- Semantic elements (header, nav, main, footer)
|   +-- CSS class application
|   +-- Accessibility attributes (aria, alt)
```

---

## 6. Test Scenarios

### 6.1 MarkdownExportTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
[UsesVerify]
public class MarkdownExportTests
{
    private readonly IMarkdownExporter _exporter = new MarkdownExporter();

    #region Heading Tests

    [Theory]
    [InlineData("# Heading 1", "# Heading 1")]
    [InlineData("## Heading 2", "## Heading 2")]
    [InlineData("### Heading 3", "### Heading 3")]
    [InlineData("#### Heading 4", "#### Heading 4")]
    [InlineData("##### Heading 5", "##### Heading 5")]
    [InlineData("###### Heading 6", "###### Heading 6")]
    public void Export_HeadingLevels_PreservesExactSyntax(string input, string expected)
    {
        var document = ParseDocument(input);
        var result = _exporter.Export(document);
        result.Trim().Should().Be(expected);
    }

    [Fact]
    public async Task Export_HeadingsWithFormatting_PreservesAll()
    {
        var input = "# **Bold** and *Italic* Heading";
        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    #endregion

    #region Inline Formatting Tests

    [Theory]
    [InlineData("**bold text**", "**bold text**")]
    [InlineData("*italic text*", "*italic text*")]
    [InlineData("***bold italic***", "***bold italic***")]
    [InlineData("~~strikethrough~~", "~~strikethrough~~")]
    [InlineData("`inline code`", "`inline code`")]
    public void Export_InlineFormatting_PreservesMarkers(string input, string expected)
    {
        var result = _exporter.Export(ParseDocument(input));
        result.Trim().Should().Be(expected);
    }

    [Fact]
    public async Task Export_NestedFormatting_PreservesHierarchy()
    {
        var input = "This is **bold with *nested italic* text** here.";
        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    #endregion

    #region Code Block Tests

    [Theory]
    [InlineData("csharp")]
    [InlineData("python")]
    [InlineData("javascript")]
    [InlineData("typescript")]
    [InlineData("yaml")]
    [InlineData("json")]
    [InlineData("bash")]
    [InlineData("sql")]
    public void Export_FencedCodeBlock_PreservesLanguage(string language)
    {
        var input = $"```{language}\ncode here\n```";
        var result = _exporter.Export(ParseDocument(input));

        result.Should().Contain($"```{language}");
        result.Should().Contain("code here");
        result.Should().EndWith("```\n");
    }

    [Fact]
    public async Task Export_CodeBlockWithContent_PreservesExactly()
    {
        var input = @"```csharp
public class Example
{
    public void Method()
    {
        var x = 1;
        Console.WriteLine(x);
    }
}
```";
        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public void Export_IndentedCodeBlock_PreservesSpaces()
    {
        var input = "    function foo() {\n        return 42;\n    }";
        var result = _exporter.Export(ParseDocument(input));
        result.Should().Contain("    function foo()");
    }

    #endregion

    #region Table Tests

    [Fact]
    public async Task Export_SimpleTable_PreservesStructure()
    {
        var input = @"| Column A | Column B |
|----------|----------|
| Value 1  | Value 2  |
| Value 3  | Value 4  |";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public void Export_TableWithAlignment_PreservesMarkers()
    {
        var input = @"| Left | Center | Right |
|:-----|:------:|------:|
| A    | B      | C     |";

        var result = _exporter.Export(ParseDocument(input));

        result.Should().Contain("|:--");  // Left align
        result.Should().Contain(":--:");  // Center align
        result.Should().Contain("--:|");  // Right align
    }

    [Fact]
    public async Task Export_TableWithFormatting_PreservesCellContent()
    {
        var input = @"| **Bold** | *Italic* | `Code` |
|----------|----------|--------|
| normal   | **mix**  | *text* |";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task Export_UnorderedList_PreservesBullets()
    {
        var input = @"- Item 1
- Item 2
- Item 3";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public async Task Export_OrderedList_PreservesNumbers()
    {
        var input = @"1. First item
2. Second item
3. Third item";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public async Task Export_NestedList_PreservesIndentation()
    {
        var input = @"- Parent item
  - Child item
    - Grandchild item
  - Another child
- Another parent";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public async Task Export_MixedList_PreservesTypes()
    {
        var input = @"1. Ordered item
   - Unordered subitem
   - Another subitem
2. Second ordered
   1. Nested ordered
   2. Another nested";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    #endregion

    #region Link and Image Tests

    [Theory]
    [InlineData("[Link Text](https://example.com)")]
    [InlineData("[Link](https://example.com \"Title\")")]
    [InlineData("![Alt Text](https://example.com/image.png)")]
    [InlineData("![](https://example.com/image.png)")]
    public void Export_LinksAndImages_PreservesSyntax(string input)
    {
        var result = _exporter.Export(ParseDocument(input));
        result.Trim().Should().Be(input);
    }

    [Fact]
    public void Export_ReferenceLink_PreservesReference()
    {
        var input = @"[Link Text][ref]

[ref]: https://example.com";

        var result = _exporter.Export(ParseDocument(input));

        result.Should().Contain("[Link Text][ref]");
        result.Should().Contain("[ref]: https://example.com");
    }

    #endregion

    #region Blockquote Tests

    [Fact]
    public async Task Export_Blockquote_PreservesMarker()
    {
        var input = @"> This is a quote
> that spans multiple lines
> and has **formatting**.";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public async Task Export_NestedBlockquote_PreservesLevels()
    {
        var input = @"> Level 1 quote
>> Level 2 quote
>>> Level 3 quote";

        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    #endregion

    #region Unicode and Special Character Tests

    [Theory]
    [InlineData("Unicode: \u4e2d\u6587\u6d4b\u8bd5")]  // Chinese
    [InlineData("Emoji: \U0001F60A \U0001F4DD \U0001F680")]  // Emoji
    [InlineData("Symbols: \u2192 \u2190 \u2713 \u2717")]  // Arrows and checks
    [InlineData("Math: \u03b1 \u03b2 \u03b3 \u2211 \u222b")]  // Greek and math
    public void Export_UnicodeContent_PreservesCharacters(string input)
    {
        var result = _exporter.Export(ParseDocument(input));
        result.Should().Contain(input);
    }

    [Fact]
    public void Export_SpecialMarkdownChars_Escaped()
    {
        var input = @"Use \* for bullets and \# for headings.";
        var result = _exporter.Export(ParseDocument(input));
        result.Should().Contain("\\*");
        result.Should().Contain("\\#");
    }

    #endregion

    #region Complex Document Tests

    [Fact]
    public async Task Export_ComplexDocument_PreservesAll()
    {
        var input = TestDocuments.CompleteDocument;
        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    [Fact]
    public async Task Export_ReleaseNotesFormat_MatchesExpected()
    {
        var input = TestDocuments.ReleaseNotesExample;
        var result = _exporter.Export(ParseDocument(input));
        await Verify(result);
    }

    #endregion

    private static Document ParseDocument(string markdown)
    {
        return new MarkdownParser().Parse(markdown);
    }
}
```

### 6.2 DocxExportTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
public class DocxExportTests
{
    private readonly IDocxExporter _exporter = new DocxExporter();

    #region Document Validity Tests

    [Fact]
    public async Task Export_SimpleDocument_GeneratesValidDocx()
    {
        var input = "# Title\n\nSome paragraph text.";
        var bytes = await _exporter.ExportAsync(ParseDocument(input));

        bytes.Should().NotBeEmpty();
        IsValidDocx(bytes).Should().BeTrue();
    }

    [Fact]
    public async Task Export_ComplexDocument_GeneratesValidDocx()
    {
        var input = TestDocuments.CompleteDocument;
        var bytes = await _exporter.ExportAsync(ParseDocument(input));

        bytes.Should().NotBeEmpty();
        IsValidDocx(bytes).Should().BeTrue();
        GetDocxWordCount(bytes).Should().BeGreaterThan(50);
    }

    #endregion

    #region Heading Style Tests

    [Theory]
    [InlineData("# Heading 1", 0, "Heading1")]
    [InlineData("## Heading 2", 0, "Heading2")]
    [InlineData("### Heading 3", 0, "Heading3")]
    [InlineData("#### Heading 4", 0, "Heading4")]
    [InlineData("##### Heading 5", 0, "Heading5")]
    [InlineData("###### Heading 6", 0, "Heading6")]
    public async Task Export_Headings_ApplyCorrectStyles(
        string input, int paragraphIndex, string expectedStyle)
    {
        var bytes = await _exporter.ExportAsync(ParseDocument(input));

        using var doc = WordprocessingDocument.Open(new MemoryStream(bytes), false);
        var paragraphs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Paragraph>().ToList();

        var style = paragraphs[paragraphIndex]
            .ParagraphProperties?.ParagraphStyleId?.Val?.Value;

        style.Should().Be(expectedStyle);
    }

    [Fact]
    public async Task Export_MixedHeadingsAndText_CorrectStyles()
    {
        var input = @"# Title
Some intro text.
## Section 1
Content here.
### Subsection
More content.";

        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var styles = GetParagraphStyles(doc);

        styles[0].Should().Be("Heading1");
        styles[1].Should().Be("Normal");
        styles[2].Should().Be("Heading2");
        styles[3].Should().Be("Normal");
        styles[4].Should().Be("Heading3");
        styles[5].Should().Be("Normal");
    }

    #endregion

    #region Character Formatting Tests

    [Fact]
    public async Task Export_BoldText_AppliesBoldRun()
    {
        var input = "This is **bold** text.";
        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var runs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>().ToList();

        var boldRun = runs.First(r => r.InnerText == "bold");
        boldRun.RunProperties?.Bold.Should().NotBeNull();
    }

    [Fact]
    public async Task Export_ItalicText_AppliesItalicRun()
    {
        var input = "This is *italic* text.";
        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var runs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>().ToList();

        var italicRun = runs.First(r => r.InnerText == "italic");
        italicRun.RunProperties?.Italic.Should().NotBeNull();
    }

    [Fact]
    public async Task Export_InlineCode_AppliesCodeStyle()
    {
        var input = "Use `var x = 1;` for declaration.";
        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var runs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>().ToList();

        var codeRun = runs.First(r => r.InnerText.Contains("var x = 1"));
        var fontName = codeRun.RunProperties?.RunFonts?.Ascii?.Value;

        fontName.Should().BeOneOf("Consolas", "Courier New", "monospace");
    }

    #endregion

    #region Table Tests

    [Fact]
    public async Task Export_SimpleTable_CreatesTableElement()
    {
        var input = @"| A | B |
|---|---|
| 1 | 2 |";

        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var tables = doc.MainDocumentPart!.Document.Body!
            .Descendants<Table>().ToList();

        tables.Should().HaveCount(1);
    }

    [Fact]
    public async Task Export_TableWithHeaders_HasCorrectRowCount()
    {
        var input = @"| Col A | Col B | Col C |
|-------|-------|-------|
| 1     | 2     | 3     |
| 4     | 5     | 6     |
| 7     | 8     | 9     |";

        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var table = doc.MainDocumentPart!.Document.Body!
            .Descendants<Table>().First();
        var rows = table.Descendants<TableRow>().ToList();

        rows.Should().HaveCount(4); // 1 header + 3 data rows
    }

    [Fact]
    public async Task Export_TableWithHeaders_HeaderRowFormatted()
    {
        var input = @"| Header |
|--------|
| Data   |";

        var bytes = await _exporter.ExportAsync(ParseDocument(input));
        using var doc = OpenDocx(bytes);

        var table = doc.MainDocumentPart!.Document.Body!
            .Descendants<Table>().First();
        var headerRow = table.Descendants<TableRow>().First();
        var headerCell = headerRow.Descendants<TableCell>().First();

        // Header cell should have bold text or special styling
        var runs = headerCell.Descendants<Run>().ToList();
        var hasBold = runs.Any(r => r.RunProperties?.Bold != null);

        hasBold.Should().BeTrue("Header row should be bold");
    }

    #endregion

    #region Table of Contents Tests

    [Fact]
    public async Task Export_WithToc_IncludesAllHeadings()
    {
        var input = @"# Title
## Section 1
### Subsection 1.1
## Section 2
### Subsection 2.1";

        var options = new DocxExportOptions { IncludeTableOfContents = true };
        var bytes = await _exporter.ExportAsync(ParseDocument(input), options);
        using var doc = OpenDocx(bytes);

        // TOC should be present as SDT (Structured Document Tag)
        var sdt = doc.MainDocumentPart!.Document.Body!
            .Descendants<SdtBlock>().FirstOrDefault();

        sdt.Should().NotBeNull("Document should contain TOC");

        // Get TOC entries
        var tocText = sdt!.InnerText;
        tocText.Should().Contain("Section 1");
        tocText.Should().Contain("Subsection 1.1");
        tocText.Should().Contain("Section 2");
    }

    #endregion

    #region Helper Methods

    private static bool IsValidDocx(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var doc = WordprocessingDocument.Open(stream, false);
            return doc.MainDocumentPart?.Document != null;
        }
        catch
        {
            return false;
        }
    }

    private static WordprocessingDocument OpenDocx(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return WordprocessingDocument.Open(stream, false);
    }

    private static List<string> GetParagraphStyles(WordprocessingDocument doc)
    {
        return doc.MainDocumentPart!.Document.Body!
            .Descendants<Paragraph>()
            .Select(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "Normal")
            .ToList();
    }

    private static int GetDocxWordCount(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        return doc.MainDocumentPart!.Document.Body!.InnerText
            .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    #endregion
}
```

### 6.3 PdfExportTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
[UsesVerify]
public class PdfExportTests
{
    private readonly IPdfExporter _exporter;
    private readonly string _testDocPath;

    public PdfExportTests()
    {
        _exporter = new MarkdownPdfExporter(/* dependencies */);
        _testDocPath = Path.Combine(TestContext.TestDataPath, "sample.md");
    }

    #region PDF Validity Tests

    [Fact]
    public async Task Export_SimpleDocument_GeneratesValidPdf()
    {
        var request = new PdfExportRequest(_testDocPath, new PdfExportOptions());

        var pdfBytes = await _exporter.ExportAsync(request);

        pdfBytes.Should().NotBeEmpty();
        IsPdfValid(pdfBytes).Should().BeTrue();
    }

    [Fact]
    public async Task Export_ValidPdf_HasPdfHeader()
    {
        var request = new PdfExportRequest(_testDocPath, new PdfExportOptions());
        var pdfBytes = await _exporter.ExportAsync(request);

        // PDF files start with %PDF-
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
        header.Should().Be("%PDF-");
    }

    #endregion

    #region Page Dimension Tests

    [Theory]
    [InlineData(PdfPageSize.A4, 595, 842)]
    [InlineData(PdfPageSize.Letter, 612, 792)]
    [InlineData(PdfPageSize.Legal, 612, 1008)]
    [InlineData(PdfPageSize.A3, 842, 1191)]
    [InlineData(PdfPageSize.A5, 420, 595)]
    public async Task Export_PageSize_MatchesDimensions(
        PdfPageSize size, int expectedWidth, int expectedHeight)
    {
        var options = new PdfExportOptions { PageSize = size };
        var request = new PdfExportRequest(_testDocPath, options);

        var pdfBytes = await _exporter.ExportAsync(request);

        var (width, height) = GetPdfPageDimensions(pdfBytes);

        width.Should().BeApproximately(expectedWidth, 1,
            $"Width for {size} should be ~{expectedWidth}pt");
        height.Should().BeApproximately(expectedHeight, 1,
            $"Height for {size} should be ~{expectedHeight}pt");
    }

    [Fact]
    public async Task Export_LandscapeOrientation_SwapsDimensions()
    {
        var options = new PdfExportOptions
        {
            PageSize = PdfPageSize.A4,
            Orientation = PdfOrientation.Landscape
        };
        var request = new PdfExportRequest(_testDocPath, options);

        var pdfBytes = await _exporter.ExportAsync(request);

        var (width, height) = GetPdfPageDimensions(pdfBytes);

        // Landscape swaps width and height
        width.Should().BeApproximately(842, 1);
        height.Should().BeApproximately(595, 1);
    }

    #endregion

    #region Content Extraction Tests

    [Fact]
    public async Task Export_DocumentContent_ExtractableText()
    {
        var testDoc = Path.Combine(TestContext.TestDataPath, "heading_test.md");
        File.WriteAllText(testDoc, "# Test Heading\n\nTest paragraph content.");

        var request = new PdfExportRequest(testDoc, new PdfExportOptions());
        var pdfBytes = await _exporter.ExportAsync(request);

        var extractedText = ExtractPdfText(pdfBytes);

        extractedText.Should().Contain("Test Heading");
        extractedText.Should().Contain("Test paragraph content");
    }

    [Fact]
    public async Task Export_CodeBlock_PreservesContent()
    {
        var testDoc = Path.Combine(TestContext.TestDataPath, "code_test.md");
        File.WriteAllText(testDoc, "```csharp\nvar x = 42;\n```");

        var request = new PdfExportRequest(testDoc, new PdfExportOptions());
        var pdfBytes = await _exporter.ExportAsync(request);

        var extractedText = ExtractPdfText(pdfBytes);

        extractedText.Should().Contain("var x = 42");
    }

    #endregion

    #region Annotation Tests

    [Fact]
    public async Task Export_WithAnnotations_IncludesComments()
    {
        var annotations = new[]
        {
            new PdfAnnotation(
                new TextSpan(0, 10),
                "This is a style violation",
                PdfAnnotationType.StyleViolation,
                "Linter")
        };
        var options = new PdfExportOptions();
        var request = new PdfExportRequest(_testDocPath, options, annotations);

        var pdfBytes = await _exporter.ExportAsync(request);

        var extractedText = ExtractPdfText(pdfBytes);
        extractedText.Should().Contain("style violation");
    }

    [Fact]
    public async Task Export_MultipleAnnotations_AllIncluded()
    {
        var annotations = new[]
        {
            new PdfAnnotation(new TextSpan(0, 5), "Issue 1", PdfAnnotationType.Comment, "User1"),
            new PdfAnnotation(new TextSpan(10, 15), "Issue 2", PdfAnnotationType.Highlight, "User2"),
            new PdfAnnotation(new TextSpan(20, 25), "Issue 3", PdfAnnotationType.Suggestion, "User3"),
        };
        var request = new PdfExportRequest(_testDocPath, new PdfExportOptions(), annotations);

        var pdfBytes = await _exporter.ExportAsync(request);

        var annotationCount = GetPdfAnnotationCount(pdfBytes);
        annotationCount.Should().BeGreaterOrEqualTo(3);
    }

    #endregion

    #region Page Number Tests

    [Fact]
    public async Task Export_WithPageNumbers_IncludesFooter()
    {
        var testDoc = CreateMultiPageDocument();
        var options = new PdfExportOptions { IncludePageNumbers = true };
        var request = new PdfExportRequest(testDoc, options);

        var pdfBytes = await _exporter.ExportAsync(request);

        var extractedText = ExtractPdfText(pdfBytes);
        // Should contain page numbers like "1 / 3" or "Page 1 of 3"
        extractedText.Should().MatchRegex(@"(\d+\s*/\s*\d+|Page\s+\d+)");
    }

    [Fact]
    public async Task Export_WithCustomFooter_IncludesText()
    {
        var options = new PdfExportOptions
        {
            FooterText = "Confidential - Internal Use Only"
        };
        var request = new PdfExportRequest(_testDocPath, options);

        var pdfBytes = await _exporter.ExportAsync(request);

        var extractedText = ExtractPdfText(pdfBytes);
        extractedText.Should().Contain("Confidential");
    }

    #endregion

    #region Table of Contents Tests

    [Fact]
    public async Task Export_WithToc_IncludesTocPage()
    {
        var testDoc = CreateDocumentWithHeadings();
        var options = new PdfExportOptions { IncludeTableOfContents = true };
        var request = new PdfExportRequest(testDoc, options);

        var pdfBytes = await _exporter.ExportAsync(request);

        var extractedText = ExtractPdfText(pdfBytes);
        extractedText.Should().Contain("Table of Contents");
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public async Task Export_StandardDocument_MatchesSnapshot()
    {
        var testDoc = Path.Combine(TestContext.TestDataPath, "standard.md");
        var request = new PdfExportRequest(testDoc, new PdfExportOptions());

        var pdfBytes = await _exporter.ExportAsync(request);
        var textContent = ExtractPdfText(pdfBytes);

        await Verify(textContent);
    }

    #endregion

    #region Helper Methods

    private static bool IsPdfValid(byte[] bytes)
    {
        try
        {
            using var doc = PdfDocument.Open(bytes);
            return doc.NumberOfPages > 0;
        }
        catch
        {
            return false;
        }
    }

    private static (double Width, double Height) GetPdfPageDimensions(byte[] bytes)
    {
        using var doc = PdfDocument.Open(bytes);
        var page = doc.GetPage(1);
        return (page.Width, page.Height);
    }

    private static string ExtractPdfText(byte[] bytes)
    {
        using var doc = PdfDocument.Open(bytes);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static int GetPdfAnnotationCount(byte[] bytes)
    {
        using var doc = PdfDocument.Open(bytes);
        return doc.GetPages().Sum(p => p.ExperimentalAccess.GetAnnotations().Count());
    }

    private string CreateMultiPageDocument()
    {
        var path = Path.Combine(TestContext.TestDataPath, "multipage.md");
        var content = string.Join("\n\n", Enumerable.Range(1, 20)
            .Select(i => $"# Section {i}\n\n{LoremIpsum.Generate(200)}"));
        File.WriteAllText(path, content);
        return path;
    }

    private string CreateDocumentWithHeadings()
    {
        var path = Path.Combine(TestContext.TestDataPath, "headings.md");
        var content = @"# Main Title
## Chapter 1
Content here.
### Section 1.1
More content.
## Chapter 2
Even more content.";
        File.WriteAllText(path, content);
        return path;
    }

    #endregion
}
```

### 6.4 HtmlExportTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.8.8a")]
public class HtmlExportTests
{
    private readonly IHtmlExporter _exporter = new HtmlExporter();

    #region HTML Validity Tests

    [Fact]
    public async Task Export_SimpleDocument_GeneratesValidHtml()
    {
        var input = "# Title\n\nParagraph text.";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("<html");
        result.Should().Contain("</html>");
        IsValidHtml(result).Should().BeTrue();
    }

    [Fact]
    public async Task Export_HasRequiredMetaTags()
    {
        var input = "# Test";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<meta charset=");
        result.Should().Contain("viewport");
    }

    #endregion

    #region Semantic Structure Tests

    [Fact]
    public async Task Export_Headings_UseSemanticTags()
    {
        var input = @"# H1
## H2
### H3
#### H4
##### H5
###### H6";

        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<h1>");
        result.Should().Contain("<h2>");
        result.Should().Contain("<h3>");
        result.Should().Contain("<h4>");
        result.Should().Contain("<h5>");
        result.Should().Contain("<h6>");
    }

    [Fact]
    public async Task Export_Paragraphs_UsePTags()
    {
        var input = "First paragraph.\n\nSecond paragraph.";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        var pCount = Regex.Matches(result, "<p>").Count;
        pCount.Should().Be(2);
    }

    [Fact]
    public async Task Export_Lists_UseCorrectTags()
    {
        var input = @"- Item 1
- Item 2

1. First
2. Second";

        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<ul>");
        result.Should().Contain("<ol>");
        result.Should().Contain("<li>");
    }

    #endregion

    #region Code Syntax Highlighting Tests

    [Theory]
    [InlineData("csharp", "language-csharp")]
    [InlineData("javascript", "language-javascript")]
    [InlineData("python", "language-python")]
    [InlineData("typescript", "language-typescript")]
    public async Task Export_CodeBlock_HasLanguageClass(string language, string expectedClass)
    {
        var input = $"```{language}\ncode here\n```";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain($"class=\"{expectedClass}\"");
    }

    [Fact]
    public async Task Export_CodeBlock_WrappedInPreAndCode()
    {
        var input = "```\ncode\n```";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<pre>");
        result.Should().Contain("<code>");
        result.Should().Contain("</code>");
        result.Should().Contain("</pre>");
    }

    [Fact]
    public async Task Export_InlineCode_UsesCodeTag()
    {
        var input = "Use `console.log()` for debugging.";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<code>console.log()</code>");
    }

    #endregion

    #region Theme Tests

    [Theory]
    [InlineData("dark", "theme-dark")]
    [InlineData("light", "theme-light")]
    [InlineData("auto", "theme-auto")]
    public async Task Export_ThemeOption_AppliesBodyClass(string theme, string expectedClass)
    {
        var options = new HtmlExportOptions { Theme = theme };
        var result = await _exporter.ExportAsync(
            ParseDocument("# Test"), options);

        result.Should().Contain($"class=\"{expectedClass}");
    }

    [Fact]
    public async Task Export_DefaultTheme_HasLightClass()
    {
        var result = await _exporter.ExportAsync(ParseDocument("# Test"));
        result.Should().Contain("theme-light");
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public async Task Export_HasLangAttribute()
    {
        var result = await _exporter.ExportAsync(ParseDocument("# Test"));
        result.Should().Contain("lang=\"en\"");
    }

    [Fact]
    public async Task Export_Images_HaveAltAttribute()
    {
        var input = "![Alt text](image.png)";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("alt=\"Alt text\"");
    }

    [Fact]
    public async Task Export_Links_HaveHref()
    {
        var input = "[Link](https://example.com)";
        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public async Task Export_ExternalLinks_HaveRelAttributes()
    {
        var input = "[External](https://external.com)";
        var options = new HtmlExportOptions { SecureExternalLinks = true };
        var result = await _exporter.ExportAsync(ParseDocument(input), options);

        result.Should().Contain("rel=\"noopener noreferrer\"");
    }

    #endregion

    #region Table Tests

    [Fact]
    public async Task Export_Table_UsesSemanticTags()
    {
        var input = @"| Header |
|--------|
| Data   |";

        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("<table>");
        result.Should().Contain("<thead>");
        result.Should().Contain("<tbody>");
        result.Should().Contain("<th>");
        result.Should().Contain("<td>");
    }

    [Fact]
    public async Task Export_TableAlignment_HasStyleAttributes()
    {
        var input = @"| Left | Center | Right |
|:-----|:------:|------:|
| A    | B      | C     |";

        var result = await _exporter.ExportAsync(ParseDocument(input));

        result.Should().Contain("text-align: left");
        result.Should().Contain("text-align: center");
        result.Should().Contain("text-align: right");
    }

    #endregion

    #region CSS Inclusion Tests

    [Fact]
    public async Task Export_IncludesDefaultStyles()
    {
        var result = await _exporter.ExportAsync(ParseDocument("# Test"));

        result.Should().Contain("<style>");
        result.Should().Contain("</style>");
    }

    [Fact]
    public async Task Export_WithCustomCss_IncludesCustomStyles()
    {
        var customCss = ".custom { color: red; }";
        var options = new HtmlExportOptions { CustomCss = customCss };
        var result = await _exporter.ExportAsync(ParseDocument("# Test"), options);

        result.Should().Contain(customCss);
    }

    #endregion

    #region Helper Methods

    private static bool IsValidHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.ParseErrors.Count() == 0;
    }

    private static Document ParseDocument(string markdown)
    {
        return new MarkdownParser().Parse(markdown);
    }

    #endregion
}
```

---

## 7. Test Fixtures

### 7.1 Golden File Structure

```text
TestFixtures/
+-- GoldenFiles/
|   +-- Markdown/
|   |   +-- simple_document.verified.md
|   |   +-- complex_document.verified.md
|   |   +-- code_blocks.verified.md
|   |   +-- tables.verified.md
|   |   +-- lists.verified.md
|   +-- Pdf/
|   |   +-- simple_document.verified.txt
|   |   +-- annotated_document.verified.txt
|   +-- Html/
|   |   +-- semantic_structure.verified.html
+-- TestDocuments/
    +-- sample.md
    +-- complex.md
    +-- release_notes.md
```

### 7.2 Test Documents

```csharp
namespace Lexichord.Tests.Publishing.TestFixtures;

/// <summary>
/// Standard test documents for export fidelity testing.
/// </summary>
public static class TestDocuments
{
    public static readonly string CompleteDocument = @"
# Main Title

This is an introductory paragraph with **bold**, *italic*, and `code` formatting.

## Section 1: Lists

### Unordered List
- Item one
- Item two
  - Nested item
- Item three

### Ordered List
1. First item
2. Second item
3. Third item

## Section 2: Code

```csharp
public class Example
{
    public void Method()
    {
        Console.WriteLine(""Hello, World!"");
    }
}
```

## Section 3: Table

| Column A | Column B | Column C |
|:---------|:--------:|---------:|
| Left     | Center   | Right    |
| Data 1   | Data 2   | Data 3   |

## Section 4: Blockquote

> This is a blockquote.
> It can span multiple lines.

## Conclusion

Final paragraph with a [link](https://example.com) and an image:

![Alt text](https://example.com/image.png)
";

    public static readonly string ReleaseNotesExample = @"
# Release Notes v1.0.0

## Features

- **New Export Pipeline**: Added support for Markdown, DOCX, PDF, and HTML exports
- **Channel Integrations**: WordPress, GitHub, and Confluence publishing
- **Template System**: Customizable release notes and documentation templates

## Bug Fixes

- Fixed issue with table alignment in Markdown export
- Resolved PDF rendering problems on Linux

## Breaking Changes

- Removed deprecated `ExportToFile()` method
- Changed `IExporter` interface signature

## Contributors

Thanks to all contributors who made this release possible!
";
}
```

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Generating {Format} export for document: {DocumentPath}"` |
| Debug | `"Snapshot comparison: {Result}"` |
| Info | `"Export test suite completed: {PassCount}/{TotalCount} passed"` |
| Warning | `"Snapshot mismatch detected for: {TestName}"` |
| Error | `"Export failed: {Format} - {Error}"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Test data contains PII | None | Use synthetic test data only |
| File system pollution | Low | Use temp directories, clean up in teardown |
| Large file generation | Low | Limit document size in tests |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Markdown document | Exporting to Markdown | All formatting markers preserved |
| 2 | Document with headings | Exporting to DOCX | Correct heading styles applied |
| 3 | Document with tables | Exporting to PDF | Table structure preserved |
| 4 | Any document | Exporting to HTML | Valid HTML5 output |
| 5 | Document with code blocks | Exporting any format | Code content unchanged |
| 6 | PDF export request | With page size option | Correct dimensions in output |
| 7 | PDF export request | With annotations | Annotations visible in PDF |
| 8 | HTML export request | With theme option | Theme class applied |

### 10.2 CI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | Any export test fails | CI runs tests | Build fails |
| 10 | Snapshot mismatch | CI runs tests | Build fails with diff |
| 11 | All export tests pass | CI runs tests | Build succeeds |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `MarkdownExportTests.cs` with 40+ test cases | [ ] |
| 2 | `DocxExportTests.cs` with 30+ test cases | [ ] |
| 3 | `PdfExportTests.cs` with 35+ test cases | [ ] |
| 4 | `HtmlExportTests.cs` with 25+ test cases | [ ] |
| 5 | Golden reference files for Markdown | [ ] |
| 6 | Golden reference files for PDF (text) | [ ] |
| 7 | Test document fixtures | [ ] |
| 8 | Verify.NET configuration | [ ] |
| 9 | Test trait configuration | [ ] |

---

## 12. Verification Commands

```bash
# Run all export format tests
dotnet test --filter "Version=v0.8.8a" --logger "console;verbosity=detailed"

# Run only Markdown export tests
dotnet test --filter "FullyQualifiedName~MarkdownExportTests"

# Run only PDF export tests
dotnet test --filter "FullyQualifiedName~PdfExportTests"

# Run only DOCX export tests
dotnet test --filter "FullyQualifiedName~DocxExportTests"

# Run only HTML export tests
dotnet test --filter "FullyQualifiedName~HtmlExportTests"

# Update golden files (when intentional changes made)
dotnet test --filter "Version=v0.8.8a" -- Verify.AutoVerify=true

# Run with coverage
dotnet test --filter "Version=v0.8.8a" --collect:"XPlat Code Coverage"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
