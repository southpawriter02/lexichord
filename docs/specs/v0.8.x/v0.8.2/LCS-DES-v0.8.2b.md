# LCS-DES-082b: Design Specification — Style Transfer

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `PUB-082b` | Sub-part of PUB-082 |
| **Feature Name** | `Style Transfer (CSS/DOCX Styles)` | Export styling system |
| **Target Version** | `v0.8.2b` | Second sub-part of v0.8.2 |
| **Module Scope** | `Lexichord.Modules.Publishing` | Publishing module |
| **Swimlane** | `Publishing` | Part of Publishing vertical |
| **License Tier** | `Writer Pro` | Style features gated |
| **Feature Gate Key** | `FeatureFlags.Publishing.StyleTransfer` | |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-082-INDEX](./LCS-DES-082-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-082 Section 3.2](./LCS-SBD-082.md#32-v082b-style-transfer) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Writers need consistent, professional formatting across all exported documents:

- Brand guidelines require specific fonts, colors, and layouts
- Different audiences need different styles (academic, technical, minimal)
- CSS styles for HTML/PDF must translate consistently
- DOCX exports need proper Word styles for compatibility

Without style transfer, each export would have inconsistent formatting, and writers would need to manually apply styles in each output format.

> **Goal:** Implement style transfer capabilities that apply CSS styles to HTML/PDF exports and Word styles to DOCX exports for consistent, professional document formatting.

### 2.2 The Proposed Solution

Implement a comprehensive style transfer system that:

1. Defines `IStyleTransferService` for CSS-based styling (HTML/PDF)
2. Defines `IDocxStyleService` for Word-based styling (DOCX)
3. Creates `StyleSheet` records for reusable style definitions
4. Provides built-in style sheets (Default, Academic, Technical, Minimal)
5. Implements `StyleSheetRepository` for managing custom styles
6. Supports style preview in the export dialog

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `ISettingsService` | v0.1.6a | Persist style preferences |
| `IConfigurationService` | v0.0.3d | Configuration storage |
| `ILicenseContext` | v0.0.4c | Gate custom style creation |
| `Serilog` | v0.0.3b | Logging |

#### 3.1.2 Downstream Consumers

| Interface | Consumer Version | Purpose |
| :--- | :--- | :--- |
| `HtmlExportAdapter` | v0.8.2a | Apply CSS styles |
| `PdfExportAdapter` | v0.8.2a | Apply CSS styles |
| `DocxExportAdapter` | v0.8.2a | Apply Word styles |

#### 3.1.3 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `HtmlAgilityPack` | 1.11.x | HTML manipulation for style injection |
| `DocumentFormat.OpenXml` | 3.x | DOCX style manipulation |

### 3.2 Licensing Behavior

| Feature | Core | Writer Pro | Teams |
| :--- | :--- | :--- | :--- |
| Built-in style sheets | - | Yes | Yes |
| Style preview | - | Yes | Yes |
| Custom style sheets | - | - | Yes |
| Style sheet editing | - | - | Yes |
| Import/export styles | - | - | Yes |

---

## 4. Data Contract (The API)

### 4.1 Style Transfer Interfaces

```csharp
namespace Lexichord.Abstractions.Contracts.Publishing;

/// <summary>
/// Service for applying CSS styles to HTML content.
/// </summary>
public interface IStyleTransferService
{
    /// <summary>
    /// Applies a style sheet's CSS to HTML content.
    /// </summary>
    /// <param name="html">The HTML content to style.</param>
    /// <param name="styleSheet">The style sheet to apply.</param>
    /// <returns>HTML with embedded styles.</returns>
    string ApplyToHtml(string html, StyleSheet styleSheet);

    /// <summary>
    /// Gets the raw CSS content for a style sheet.
    /// </summary>
    /// <param name="styleSheet">The style sheet.</param>
    /// <returns>CSS content as a string.</returns>
    string GetCss(StyleSheet styleSheet);

    /// <summary>
    /// Generates a style preview HTML snippet.
    /// </summary>
    /// <param name="styleSheet">The style sheet to preview.</param>
    /// <returns>HTML preview content.</returns>
    string GeneratePreview(StyleSheet styleSheet);

    /// <summary>
    /// Validates CSS syntax.
    /// </summary>
    /// <param name="css">CSS content to validate.</param>
    /// <returns>Validation result.</returns>
    CssValidationResult ValidateCss(string css);
}

/// <summary>
/// Service for applying Word styles to DOCX documents.
/// </summary>
public interface IDocxStyleService
{
    /// <summary>
    /// Applies styles to a DOCX document.
    /// </summary>
    /// <param name="document">The OpenXml document.</param>
    /// <param name="styleSheet">The style sheet to apply.</param>
    void ApplyStyles(WordprocessingDocument document, StyleSheet styleSheet);

    /// <summary>
    /// Gets the style mapping from Markdown elements to Word styles.
    /// </summary>
    /// <param name="styleSheet">The style sheet.</param>
    /// <returns>Mapping dictionary.</returns>
    IReadOnlyDictionary<MarkdownElement, string> GetStyleMappings(StyleSheet styleSheet);

    /// <summary>
    /// Creates Word style definitions from a style sheet.
    /// </summary>
    /// <param name="styleSheet">The style sheet.</param>
    /// <returns>OpenXml style definitions.</returns>
    Styles CreateWordStyles(StyleSheet styleSheet);

    /// <summary>
    /// Applies font to the document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="font">Font definition.</param>
    void ApplyFont(WordprocessingDocument document, FontDefinition font);
}

/// <summary>
/// Repository for managing style sheets.
/// </summary>
public interface IStyleSheetRepository
{
    /// <summary>
    /// Gets all available style sheets.
    /// </summary>
    Task<IReadOnlyList<StyleSheet>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets built-in style sheets.
    /// </summary>
    Task<IReadOnlyList<StyleSheet>> GetBuiltInAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets custom style sheets.
    /// </summary>
    Task<IReadOnlyList<StyleSheet>> GetCustomAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a style sheet by ID.
    /// </summary>
    Task<StyleSheet?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a style sheet by name.
    /// </summary>
    Task<StyleSheet?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Gets the default style sheet.
    /// </summary>
    Task<StyleSheet> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves a custom style sheet.
    /// </summary>
    Task<StyleSheet> SaveAsync(StyleSheet styleSheet, CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom style sheet.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Imports a style sheet from file.
    /// </summary>
    Task<StyleSheet> ImportAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports a style sheet to file.
    /// </summary>
    Task ExportAsync(Guid id, string filePath, CancellationToken ct = default);
}
```

### 4.2 Style Sheet Records

```csharp
namespace Lexichord.Abstractions.Contracts.Publishing;

/// <summary>
/// Represents a reusable style definition for exports.
/// </summary>
public record StyleSheet
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the style sheet.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the style sheet's purpose and appearance.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Style sheet category/type.
    /// </summary>
    public StyleSheetType Type { get; init; } = StyleSheetType.Custom;

    /// <summary>
    /// CSS content for HTML/PDF exports.
    /// </summary>
    public required string CssContent { get; init; }

    /// <summary>
    /// DOCX-specific style definitions.
    /// </summary>
    public DocxStyleDefinition? DocxStyles { get; init; }

    /// <summary>
    /// Whether this is a built-in style sheet.
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Whether the style sheet can be modified.
    /// </summary>
    public bool IsReadOnly => IsBuiltIn;

    /// <summary>
    /// Preview thumbnail path (optional).
    /// </summary>
    public string? ThumbnailPath { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Author of the style sheet.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Version number for tracking changes.
    /// </summary>
    public int Version { get; init; } = 1;
}

/// <summary>
/// Style sheet type/category.
/// </summary>
public enum StyleSheetType
{
    /// <summary>
    /// Default clean style.
    /// </summary>
    Default,

    /// <summary>
    /// Academic/scholarly style.
    /// </summary>
    Academic,

    /// <summary>
    /// Technical documentation style.
    /// </summary>
    Technical,

    /// <summary>
    /// Minimal/plain style.
    /// </summary>
    Minimal,

    /// <summary>
    /// User-created custom style.
    /// </summary>
    Custom
}

/// <summary>
/// DOCX-specific style definitions.
/// </summary>
public record DocxStyleDefinition
{
    /// <summary>
    /// Word style ID for Heading 1.
    /// </summary>
    public string Heading1StyleId { get; init; } = "Heading1";

    /// <summary>
    /// Word style ID for Heading 2.
    /// </summary>
    public string Heading2StyleId { get; init; } = "Heading2";

    /// <summary>
    /// Word style ID for Heading 3.
    /// </summary>
    public string Heading3StyleId { get; init; } = "Heading3";

    /// <summary>
    /// Word style ID for Heading 4.
    /// </summary>
    public string Heading4StyleId { get; init; } = "Heading4";

    /// <summary>
    /// Word style ID for body text.
    /// </summary>
    public string BodyTextStyleId { get; init; } = "Normal";

    /// <summary>
    /// Word style ID for code blocks.
    /// </summary>
    public string CodeBlockStyleId { get; init; } = "Code";

    /// <summary>
    /// Word style ID for inline code.
    /// </summary>
    public string InlineCodeStyleId { get; init; } = "CodeChar";

    /// <summary>
    /// Word style ID for block quotes.
    /// </summary>
    public string BlockQuoteStyleId { get; init; } = "Quote";

    /// <summary>
    /// Word style ID for tables.
    /// </summary>
    public string TableStyleId { get; init; } = "TableGrid";

    /// <summary>
    /// Word style ID for bullet lists.
    /// </summary>
    public string ListBulletStyleId { get; init; } = "ListBullet";

    /// <summary>
    /// Word style ID for numbered lists.
    /// </summary>
    public string ListNumberStyleId { get; init; } = "ListNumber";

    /// <summary>
    /// Default font for the document.
    /// </summary>
    public FontDefinition DefaultFont { get; init; } = FontDefinition.Default;

    /// <summary>
    /// Font for code blocks and inline code.
    /// </summary>
    public FontDefinition CodeFont { get; init; } = FontDefinition.Code;

    /// <summary>
    /// Color palette for the document.
    /// </summary>
    public ColorPalette Colors { get; init; } = ColorPalette.Default;

    /// <summary>
    /// Line spacing (1.0 = single, 1.5 = one-and-half, 2.0 = double).
    /// </summary>
    public double LineSpacing { get; init; } = 1.15;

    /// <summary>
    /// Paragraph spacing after in points.
    /// </summary>
    public double ParagraphSpacingAfter { get; init; } = 10;
}

/// <summary>
/// Font definition for document styling.
/// </summary>
public record FontDefinition
{
    /// <summary>
    /// Font family name.
    /// </summary>
    public required string FontFamily { get; init; }

    /// <summary>
    /// Font size in points.
    /// </summary>
    public double SizePoints { get; init; } = 11;

    /// <summary>
    /// Whether font is bold.
    /// </summary>
    public bool Bold { get; init; }

    /// <summary>
    /// Whether font is italic.
    /// </summary>
    public bool Italic { get; init; }

    /// <summary>
    /// Font color (hex format).
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Default body font (Calibri 11pt).
    /// </summary>
    public static FontDefinition Default => new()
    {
        FontFamily = "Calibri",
        SizePoints = 11
    };

    /// <summary>
    /// Default code font (Consolas 10pt).
    /// </summary>
    public static FontDefinition Code => new()
    {
        FontFamily = "Consolas",
        SizePoints = 10
    };

    /// <summary>
    /// Academic body font (Times New Roman 12pt).
    /// </summary>
    public static FontDefinition Academic => new()
    {
        FontFamily = "Times New Roman",
        SizePoints = 12
    };
}

/// <summary>
/// Color palette for document styling.
/// </summary>
public record ColorPalette
{
    /// <summary>
    /// Primary brand color (hex).
    /// </summary>
    public string PrimaryColor { get; init; } = "#2563eb";

    /// <summary>
    /// Secondary color (hex).
    /// </summary>
    public string SecondaryColor { get; init; } = "#64748b";

    /// <summary>
    /// Accent color for highlights (hex).
    /// </summary>
    public string AccentColor { get; init; } = "#3b82f6";

    /// <summary>
    /// Main text color (hex).
    /// </summary>
    public string TextColor { get; init; } = "#1f2937";

    /// <summary>
    /// Page background color (hex).
    /// </summary>
    public string BackgroundColor { get; init; } = "#ffffff";

    /// <summary>
    /// Code block background color (hex).
    /// </summary>
    public string CodeBackgroundColor { get; init; } = "#f3f4f6";

    /// <summary>
    /// Link color (hex).
    /// </summary>
    public string LinkColor { get; init; } = "#2563eb";

    /// <summary>
    /// Border color (hex).
    /// </summary>
    public string BorderColor { get; init; } = "#e5e7eb";

    /// <summary>
    /// Default color palette.
    /// </summary>
    public static ColorPalette Default => new();

    /// <summary>
    /// Academic color palette (conservative).
    /// </summary>
    public static ColorPalette Academic => new()
    {
        PrimaryColor = "#000000",
        SecondaryColor = "#4b5563",
        AccentColor = "#1f2937",
        TextColor = "#000000",
        LinkColor = "#1e40af",
        CodeBackgroundColor = "#f9fafb"
    };

    /// <summary>
    /// Minimal color palette (black and white).
    /// </summary>
    public static ColorPalette Minimal => new()
    {
        PrimaryColor = "#000000",
        SecondaryColor = "#6b7280",
        AccentColor = "#374151",
        TextColor = "#000000",
        BackgroundColor = "#ffffff",
        CodeBackgroundColor = "#f9fafb",
        LinkColor = "#000000",
        BorderColor = "#d1d5db"
    };
}

/// <summary>
/// Markdown element types for style mapping.
/// </summary>
public enum MarkdownElement
{
    Heading1,
    Heading2,
    Heading3,
    Heading4,
    Heading5,
    Heading6,
    Paragraph,
    CodeBlock,
    InlineCode,
    BlockQuote,
    BulletList,
    NumberedList,
    ListItem,
    Table,
    TableHeader,
    TableCell,
    Link,
    Bold,
    Italic,
    Strikethrough,
    HorizontalRule,
    Image
}
```

---

## 5. Implementation Logic

### 5.1 CSS Style Transfer Service

```csharp
namespace Lexichord.Modules.Publishing.Services;

/// <summary>
/// Service for applying CSS styles to HTML content.
/// </summary>
public class CssStyleTransferService : IStyleTransferService
{
    private readonly ILogger<CssStyleTransferService> _logger;

    public CssStyleTransferService(ILogger<CssStyleTransferService> logger)
    {
        _logger = logger;
    }

    public string ApplyToHtml(string html, StyleSheet styleSheet)
    {
        _logger.LogDebug("Applying style sheet: {Name}", styleSheet.Name);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find or create <head> element
        var head = doc.DocumentNode.SelectSingleNode("//head");
        if (head == null)
        {
            var htmlNode = doc.DocumentNode.SelectSingleNode("//html");
            if (htmlNode != null)
            {
                head = doc.CreateElement("head");
                htmlNode.PrependChild(head);
            }
            else
            {
                // Wrap content in full HTML structure
                return WrapWithStyles(html, styleSheet);
            }
        }

        // Add style element
        var styleNode = doc.CreateElement("style");
        styleNode.InnerHtml = styleSheet.CssContent;
        head.AppendChild(styleNode);

        return doc.DocumentNode.OuterHtml;
    }

    public string GetCss(StyleSheet styleSheet)
    {
        return styleSheet.CssContent;
    }

    public string GeneratePreview(StyleSheet styleSheet)
    {
        var previewMarkdown = """
            # Heading 1
            ## Heading 2
            ### Heading 3

            This is a paragraph with **bold** and *italic* text.

            - Bullet point 1
            - Bullet point 2

            1. Numbered item 1
            2. Numbered item 2

            > This is a blockquote.

            ```csharp
            public void Example() { }
            ```

            | Column 1 | Column 2 |
            |----------|----------|
            | Cell 1   | Cell 2   |
            """;

        // This would use the markdown parser to convert to HTML
        var html = ConvertToHtml(previewMarkdown);
        return ApplyToHtml(html, styleSheet);
    }

    public CssValidationResult ValidateCss(string css)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(css))
        {
            errors.Add("CSS content is empty");
            return new CssValidationResult(false, errors, warnings);
        }

        // Basic syntax validation
        var openBraces = css.Count(c => c == '{');
        var closeBraces = css.Count(c => c == '}');

        if (openBraces != closeBraces)
        {
            errors.Add($"Mismatched braces: {openBraces} opening, {closeBraces} closing");
        }

        // Check for potentially problematic rules
        if (css.Contains("position: fixed"))
        {
            warnings.Add("'position: fixed' may cause issues in PDF export");
        }

        if (css.Contains("@media print"))
        {
            // This is actually good
        }
        else if (!css.Contains("@media"))
        {
            warnings.Add("Consider adding @media print rules for better PDF output");
        }

        return new CssValidationResult(errors.Count == 0, errors, warnings);
    }

    private string WrapWithStyles(string bodyContent, StyleSheet styleSheet)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <style>
                {styleSheet.CssContent}
                </style>
            </head>
            <body>
                {bodyContent}
            </body>
            </html>
            """;
    }

    private static string ConvertToHtml(string markdown)
    {
        // Use Markdig for conversion
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        return Markdown.ToHtml(markdown, pipeline);
    }
}

/// <summary>
/// Result of CSS validation.
/// </summary>
public record CssValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);
```

### 5.2 Built-in Style Sheets

```csharp
namespace Lexichord.Modules.Publishing.Styles;

/// <summary>
/// Provider for built-in style sheets.
/// </summary>
public static class BuiltInStyleSheets
{
    /// <summary>
    /// Default style sheet - clean, modern sans-serif.
    /// </summary>
    public static StyleSheet Default => new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000001"),
        Name = "Default",
        Description = "Clean, modern design with sans-serif fonts and blue accents. Optimized for screen reading.",
        Type = StyleSheetType.Default,
        IsBuiltIn = true,
        CssContent = DefaultCss,
        DocxStyles = new DocxStyleDefinition
        {
            DefaultFont = FontDefinition.Default,
            CodeFont = FontDefinition.Code,
            Colors = ColorPalette.Default,
            LineSpacing = 1.15,
            ParagraphSpacingAfter = 10
        }
    };

    /// <summary>
    /// Academic style sheet - serif fonts, double-spaced.
    /// </summary>
    public static StyleSheet Academic => new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000002"),
        Name = "Academic",
        Description = "Traditional academic style with Times New Roman, double spacing, and wide margins. APA/MLA compatible.",
        Type = StyleSheetType.Academic,
        IsBuiltIn = true,
        CssContent = AcademicCss,
        DocxStyles = new DocxStyleDefinition
        {
            DefaultFont = FontDefinition.Academic,
            CodeFont = FontDefinition.Code,
            Colors = ColorPalette.Academic,
            LineSpacing = 2.0,
            ParagraphSpacingAfter = 0
        }
    };

    /// <summary>
    /// Technical style sheet - monospace code, numbered sections.
    /// </summary>
    public static StyleSheet Technical => new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000003"),
        Name = "Technical",
        Description = "Optimized for API documentation and technical manuals. Prominent code blocks with syntax highlighting.",
        Type = StyleSheetType.Technical,
        IsBuiltIn = true,
        CssContent = TechnicalCss,
        DocxStyles = new DocxStyleDefinition
        {
            DefaultFont = new FontDefinition { FontFamily = "Segoe UI", SizePoints = 10 },
            CodeFont = new FontDefinition { FontFamily = "Cascadia Code", SizePoints = 9 },
            Colors = ColorPalette.Default,
            LineSpacing = 1.15,
            ParagraphSpacingAfter = 8
        }
    };

    /// <summary>
    /// Minimal style sheet - black and white, no decorations.
    /// </summary>
    public static StyleSheet Minimal => new()
    {
        Id = new Guid("00000000-0000-0000-0000-000000000004"),
        Name = "Minimal",
        Description = "Black and white only, no decorative elements. Maximum information density, optimized for printing.",
        Type = StyleSheetType.Minimal,
        IsBuiltIn = true,
        CssContent = MinimalCss,
        DocxStyles = new DocxStyleDefinition
        {
            DefaultFont = new FontDefinition { FontFamily = "Arial", SizePoints = 11 },
            CodeFont = new FontDefinition { FontFamily = "Courier New", SizePoints = 10 },
            Colors = ColorPalette.Minimal,
            LineSpacing = 1.0,
            ParagraphSpacingAfter = 6
        }
    };

    /// <summary>
    /// Gets all built-in style sheets.
    /// </summary>
    public static IReadOnlyList<StyleSheet> All => [Default, Academic, Technical, Minimal];

    #region CSS Content

    private const string DefaultCss = """
        :root {
            --primary-color: #2563eb;
            --secondary-color: #64748b;
            --text-color: #1f2937;
            --background-color: #ffffff;
            --code-background: #f3f4f6;
            --border-color: #e5e7eb;
            --link-color: #2563eb;
        }

        body {
            font-family: 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            font-size: 16px;
            line-height: 1.6;
            color: var(--text-color);
            background-color: var(--background-color);
            max-width: 800px;
            margin: 0 auto;
            padding: 2rem;
        }

        h1, h2, h3, h4, h5, h6 {
            color: var(--text-color);
            margin-top: 1.5em;
            margin-bottom: 0.5em;
            line-height: 1.3;
        }

        h1 { font-size: 2.25rem; border-bottom: 2px solid var(--primary-color); padding-bottom: 0.3em; }
        h2 { font-size: 1.75rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.2em; }
        h3 { font-size: 1.5rem; }
        h4 { font-size: 1.25rem; }

        p { margin: 1em 0; }

        a { color: var(--link-color); text-decoration: none; }
        a:hover { text-decoration: underline; }

        code {
            font-family: 'Cascadia Code', 'Fira Code', Consolas, monospace;
            background-color: var(--code-background);
            padding: 0.2em 0.4em;
            border-radius: 3px;
            font-size: 0.9em;
        }

        pre {
            background-color: var(--code-background);
            padding: 1rem;
            border-radius: 6px;
            overflow-x: auto;
            border: 1px solid var(--border-color);
        }

        pre code {
            background: none;
            padding: 0;
        }

        blockquote {
            border-left: 4px solid var(--primary-color);
            margin: 1em 0;
            padding: 0.5em 1em;
            background-color: #f8fafc;
            color: var(--secondary-color);
        }

        table {
            border-collapse: collapse;
            width: 100%;
            margin: 1em 0;
        }

        th, td {
            border: 1px solid var(--border-color);
            padding: 0.75rem;
            text-align: left;
        }

        th {
            background-color: var(--code-background);
            font-weight: 600;
        }

        ul, ol { margin: 1em 0; padding-left: 2em; }
        li { margin: 0.25em 0; }

        hr {
            border: none;
            border-top: 1px solid var(--border-color);
            margin: 2em 0;
        }

        img {
            max-width: 100%;
            height: auto;
        }

        @media print {
            body { max-width: none; padding: 0; }
            pre { white-space: pre-wrap; }
        }
        """;

    private const string AcademicCss = """
        body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 12pt;
            line-height: 2;
            color: #000000;
            background-color: #ffffff;
            max-width: 6.5in;
            margin: 1in auto;
        }

        h1, h2, h3, h4, h5, h6 {
            font-family: 'Times New Roman', Times, serif;
            color: #000000;
            margin-top: 24pt;
            margin-bottom: 12pt;
        }

        h1 { font-size: 14pt; font-weight: bold; text-align: center; }
        h2 { font-size: 12pt; font-weight: bold; }
        h3 { font-size: 12pt; font-weight: bold; font-style: italic; }
        h4 { font-size: 12pt; font-style: italic; }

        p {
            margin: 0;
            text-indent: 0.5in;
        }

        p:first-of-type { text-indent: 0; }

        a { color: #1e40af; }

        code, pre {
            font-family: 'Courier New', Courier, monospace;
            font-size: 10pt;
        }

        pre {
            background-color: #f9fafb;
            padding: 12pt;
            margin: 12pt 0;
            border: 1px solid #d1d5db;
        }

        blockquote {
            margin: 12pt 0.5in;
            font-style: italic;
        }

        table {
            border-collapse: collapse;
            margin: 12pt auto;
        }

        th, td {
            border: 1px solid #000000;
            padding: 6pt 12pt;
        }

        @media print {
            body { margin: 0; }
        }
        """;

    private const string TechnicalCss = """
        :root {
            --primary-color: #0369a1;
            --code-background: #1e293b;
            --code-text: #e2e8f0;
        }

        body {
            font-family: 'Segoe UI', system-ui, sans-serif;
            font-size: 14px;
            line-height: 1.5;
            color: #374151;
            max-width: 900px;
            margin: 0 auto;
            padding: 1.5rem;
        }

        h1 { font-size: 1.875rem; color: #111827; }
        h2 { font-size: 1.5rem; color: #1f2937; margin-top: 2rem; }
        h3 { font-size: 1.25rem; color: #374151; }
        h4 { font-size: 1.125rem; color: #4b5563; }

        /* Numbered headings */
        body { counter-reset: h2counter; }
        h2 { counter-reset: h3counter; }
        h2::before {
            counter-increment: h2counter;
            content: counter(h2counter) ". ";
        }
        h3::before {
            counter-increment: h3counter;
            content: counter(h2counter) "." counter(h3counter) " ";
        }

        code {
            font-family: 'Cascadia Code', 'JetBrains Mono', monospace;
            background-color: #f1f5f9;
            padding: 0.125rem 0.375rem;
            border-radius: 4px;
            font-size: 0.875em;
        }

        pre {
            background-color: var(--code-background);
            color: var(--code-text);
            padding: 1rem;
            border-radius: 8px;
            overflow-x: auto;
            font-size: 13px;
        }

        pre code {
            background: none;
            color: inherit;
            padding: 0;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 0.875rem;
        }

        th {
            background-color: #f8fafc;
            font-weight: 600;
            text-align: left;
            padding: 0.5rem 0.75rem;
            border-bottom: 2px solid #e2e8f0;
        }

        td {
            padding: 0.5rem 0.75rem;
            border-bottom: 1px solid #e2e8f0;
        }

        .note, .warning, .tip {
            padding: 1rem;
            border-radius: 6px;
            margin: 1rem 0;
        }

        .note { background-color: #eff6ff; border-left: 4px solid #3b82f6; }
        .warning { background-color: #fef3c7; border-left: 4px solid #f59e0b; }
        .tip { background-color: #ecfdf5; border-left: 4px solid #10b981; }

        @media print {
            pre { white-space: pre-wrap; background-color: #f8fafc !important; color: #1f2937 !important; }
        }
        """;

    private const string MinimalCss = """
        body {
            font-family: Arial, Helvetica, sans-serif;
            font-size: 11pt;
            line-height: 1.4;
            color: #000000;
            background-color: #ffffff;
            max-width: 7in;
            margin: 0 auto;
            padding: 0.5in;
        }

        h1, h2, h3, h4, h5, h6 {
            font-family: Arial, Helvetica, sans-serif;
            color: #000000;
            margin-top: 1em;
            margin-bottom: 0.5em;
        }

        h1 { font-size: 16pt; }
        h2 { font-size: 14pt; }
        h3 { font-size: 12pt; }
        h4 { font-size: 11pt; font-weight: bold; }

        p { margin: 0.5em 0; }

        a { color: #000000; text-decoration: underline; }

        code {
            font-family: 'Courier New', Courier, monospace;
            font-size: 10pt;
        }

        pre {
            background-color: #f9fafb;
            border: 1px solid #d1d5db;
            padding: 0.5em;
            font-size: 9pt;
            overflow-x: auto;
        }

        blockquote {
            border-left: 2px solid #6b7280;
            margin: 0.5em 0;
            padding-left: 1em;
            color: #4b5563;
        }

        table {
            border-collapse: collapse;
            width: 100%;
        }

        th, td {
            border: 1px solid #000000;
            padding: 0.25em 0.5em;
            font-size: 10pt;
        }

        th { font-weight: bold; }

        ul, ol { margin: 0.5em 0; padding-left: 1.5em; }

        hr { border: none; border-top: 1px solid #000000; }

        @media print {
            body { padding: 0; margin: 0; }
        }
        """;

    #endregion
}
```

### 5.3 DOCX Style Service

```csharp
namespace Lexichord.Modules.Publishing.Services;

/// <summary>
/// Service for applying Word styles to DOCX documents.
/// </summary>
public class DocxStyleService : IDocxStyleService
{
    private readonly ILogger<DocxStyleService> _logger;

    public DocxStyleService(ILogger<DocxStyleService> logger)
    {
        _logger = logger;
    }

    public void ApplyStyles(WordprocessingDocument document, StyleSheet styleSheet)
    {
        _logger.LogDebug("Applying DOCX styles: {StyleSheet}", styleSheet.Name);

        var docxStyles = styleSheet.DocxStyles ?? new DocxStyleDefinition();

        // Ensure styles part exists
        var stylesPart = document.MainDocumentPart!.StyleDefinitionsPart;
        if (stylesPart == null)
        {
            stylesPart = document.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
        }

        // Add or update styles
        var styles = CreateWordStyles(styleSheet);
        foreach (var style in styles.Elements<Style>())
        {
            var existing = stylesPart.Styles!.Elements<Style>()
                .FirstOrDefault(s => s.StyleId?.Value == style.StyleId?.Value);

            if (existing != null)
            {
                existing.Remove();
            }

            stylesPart.Styles.AppendChild(style.CloneNode(true));
        }

        // Apply default font
        ApplyFont(document, docxStyles.DefaultFont);

        _logger.LogDebug("DOCX styles applied successfully");
    }

    public IReadOnlyDictionary<MarkdownElement, string> GetStyleMappings(StyleSheet styleSheet)
    {
        var docxStyles = styleSheet.DocxStyles ?? new DocxStyleDefinition();

        return new Dictionary<MarkdownElement, string>
        {
            [MarkdownElement.Heading1] = docxStyles.Heading1StyleId,
            [MarkdownElement.Heading2] = docxStyles.Heading2StyleId,
            [MarkdownElement.Heading3] = docxStyles.Heading3StyleId,
            [MarkdownElement.Heading4] = docxStyles.Heading4StyleId,
            [MarkdownElement.Paragraph] = docxStyles.BodyTextStyleId,
            [MarkdownElement.CodeBlock] = docxStyles.CodeBlockStyleId,
            [MarkdownElement.InlineCode] = docxStyles.InlineCodeStyleId,
            [MarkdownElement.BlockQuote] = docxStyles.BlockQuoteStyleId,
            [MarkdownElement.BulletList] = docxStyles.ListBulletStyleId,
            [MarkdownElement.NumberedList] = docxStyles.ListNumberStyleId,
            [MarkdownElement.Table] = docxStyles.TableStyleId
        };
    }

    public Styles CreateWordStyles(StyleSheet styleSheet)
    {
        var docxStyles = styleSheet.DocxStyles ?? new DocxStyleDefinition();
        var colors = docxStyles.Colors;
        var styles = new Styles();

        // Heading 1
        styles.AppendChild(CreateHeadingStyle(
            docxStyles.Heading1StyleId,
            "Heading 1",
            docxStyles.DefaultFont.FontFamily,
            24, // 24pt
            colors.PrimaryColor,
            true));

        // Heading 2
        styles.AppendChild(CreateHeadingStyle(
            docxStyles.Heading2StyleId,
            "Heading 2",
            docxStyles.DefaultFont.FontFamily,
            18,
            colors.TextColor,
            true));

        // Heading 3
        styles.AppendChild(CreateHeadingStyle(
            docxStyles.Heading3StyleId,
            "Heading 3",
            docxStyles.DefaultFont.FontFamily,
            14,
            colors.TextColor,
            true));

        // Code block style
        styles.AppendChild(CreateCodeStyle(
            docxStyles.CodeBlockStyleId,
            "Code",
            docxStyles.CodeFont.FontFamily,
            docxStyles.CodeFont.SizePoints,
            colors.CodeBackgroundColor));

        return styles;
    }

    public void ApplyFont(WordprocessingDocument document, FontDefinition font)
    {
        var settings = document.MainDocumentPart!.DocumentSettingsPart?.Settings
            ?? new Settings();

        // Set default font through theme or direct settings
        // This is simplified - full implementation would modify theme fonts
        _logger.LogDebug("Applying default font: {Font} {Size}pt",
            font.FontFamily, font.SizePoints);
    }

    private static Style CreateHeadingStyle(
        string styleId,
        string styleName,
        string fontFamily,
        double sizePoints,
        string color,
        bool bold)
    {
        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
            StyleName = new StyleName { Val = styleName },
            StyleRunProperties = new StyleRunProperties
            {
                RunFonts = new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily },
                FontSize = new FontSize { Val = (sizePoints * 2).ToString() }, // Half-points
                Bold = bold ? new Bold() : null,
                Color = new Color { Val = color.TrimStart('#') }
            }
        };
    }

    private static Style CreateCodeStyle(
        string styleId,
        string styleName,
        string fontFamily,
        double sizePoints,
        string backgroundColor)
    {
        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = styleId,
            StyleName = new StyleName { Val = styleName },
            StyleRunProperties = new StyleRunProperties
            {
                RunFonts = new RunFonts { Ascii = fontFamily, HighAnsi = fontFamily },
                FontSize = new FontSize { Val = (sizePoints * 2).ToString() }
            },
            StyleParagraphProperties = new StyleParagraphProperties
            {
                Shading = new Shading
                {
                    Fill = backgroundColor.TrimStart('#'),
                    Val = ShadingPatternValues.Clear
                }
            }
        };
    }
}
```

---

## 6. UI/UX Specifications

### 6.1 Style Sheet Selector

```text
+------------------------------------------------------------------+
| Style Sheet                                                       |
| +--------------------------------------------------------------+ |
| | [Default                                              v]     | |
| +--------------------------------------------------------------+ |
| | [x] Preview styles                                           | |
| +--------------------------------------------------------------+ |
|                                                                   |
| +--------------------------------------------------------------+ |
| | Preview                                                      | |
| | +----------------------------------------------------------+ |
| | | # Heading 1                                              | |
| | | ## Heading 2                                             | |
| | |                                                          | |
| | | This is body text with **bold** and *italic*.           | |
| | |                                                          | |
| | | ```code block```                                         | |
| | +----------------------------------------------------------+ |
| +--------------------------------------------------------------+ |
+------------------------------------------------------------------+
```

### 6.2 Style Sheet Management (Teams+)

```text
+------------------------------------------------------------------+
| Style Sheets                                          [+] [Import]|
+------------------------------------------------------------------+
| Built-in                                                          |
| +--------------------------------------------------------------+ |
| | [*] Default    | Clean, modern design with blue accents      | |
| | [ ] Academic   | Times New Roman, double-spaced              | |
| | [ ] Technical  | Monospace code, numbered sections           | |
| | [ ] Minimal    | Black and white, no decorations             | |
| +--------------------------------------------------------------+ |
|                                                                   |
| Custom                                                            |
| +--------------------------------------------------------------+ |
| | [ ] My Brand   | Company branding styles       [Edit][Delete]| |
| | [ ] Light Mode | Light theme variant           [Edit][Delete]| |
| +--------------------------------------------------------------+ |
+------------------------------------------------------------------+
```

---

## 7. Observability & Logging

| Level | Source | Message Template |
| :--- | :--- | :--- |
| Debug | CssStyleTransfer | `"Applying style sheet: {Name}"` |
| Debug | DocxStyleService | `"Applying DOCX styles: {StyleSheet}"` |
| Debug | DocxStyleService | `"Applying default font: {Font} {Size}pt"` |
| Info | StyleSheetRepository | `"Loaded {Count} style sheets"` |
| Info | StyleSheetRepository | `"Saved custom style sheet: {Name}"` |
| Warning | CssStyleTransfer | `"CSS validation warning: {Warning}"` |
| Error | CssStyleTransfer | `"CSS validation error: {Error}"` |

---

## 8. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Default style sheet | Apply to HTML | Blue accents, sans-serif font |
| 2 | Academic style sheet | Apply to HTML | Times New Roman, double spacing |
| 3 | Technical style sheet | Apply to HTML | Dark code blocks, numbered headings |
| 4 | Minimal style sheet | Apply to HTML | Black and white only |
| 5 | Default style sheet | Apply to DOCX | Calibri font, heading styles |
| 6 | Academic style sheet | Apply to DOCX | Times New Roman 12pt, 2.0 spacing |
| 7 | Invalid CSS | Validate | Returns errors |
| 8 | Writer Pro license | Access styles | All built-in styles available |
| 9 | Teams license | Custom styles | Can create and edit |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `IStyleTransferService` interface | [ ] |
| 2 | `IDocxStyleService` interface | [ ] |
| 3 | `IStyleSheetRepository` interface | [ ] |
| 4 | `StyleSheet` and related records | [ ] |
| 5 | `CssStyleTransferService` implementation | [ ] |
| 6 | `DocxStyleService` implementation | [ ] |
| 7 | `StyleSheetRepository` implementation | [ ] |
| 8 | Built-in style sheets (4) | [ ] |
| 9 | Style preview component | [ ] |
| 10 | Unit tests for style services | [ ] |

---

## 10. Verification Commands

```bash
# Run style transfer tests
dotnet test --filter "Version=v0.8.2b" --logger "console;verbosity=detailed"

# Run specific tests
dotnet test --filter "FullyQualifiedName~CssStyleTransferService"
dotnet test --filter "FullyQualifiedName~DocxStyleService"

# Verify built-in styles
dotnet test --filter "FullyQualifiedName~BuiltInStyleSheets"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
