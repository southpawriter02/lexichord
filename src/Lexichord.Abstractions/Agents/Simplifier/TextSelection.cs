// -----------------------------------------------------------------------
// <copyright file="TextSelection.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents a text selection within a document for batch processing.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="TextSelection"/> record encapsulates a contiguous
/// region of text within a document, identified by its character offset and length.
/// This enables precise targeting of specific text regions for batch simplification
/// operations via <see cref="IBatchSimplificationService.SimplifySelectionsAsync"/>.
/// </para>
/// <para>
/// <b>Offset Semantics:</b>
/// <list type="bullet">
///   <item><description><see cref="StartOffset"/>: Zero-based character index from document start</description></item>
///   <item><description><see cref="Length"/>: Number of characters in the selection</description></item>
///   <item><description>EndOffset = <c>StartOffset + Length</c> (exclusive)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Validation:</b>
/// Selections must have positive length. Overlapping selections are not automatically
/// detected—callers should ensure non-overlapping regions when applying changes.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a selection from editor coordinates
/// var selection = new TextSelection(
///     DocumentPath: "/documents/report.md",
///     StartOffset: 1500,
///     Length: 450,
///     Text: "The original text content...");
///
/// // Processing multiple selections
/// var selections = new List&lt;TextSelection&gt;
/// {
///     new("/doc.md", 0, 200, firstParagraph),
///     new("/doc.md", 250, 180, secondParagraph)
/// };
/// var result = await batchService.SimplifySelectionsAsync(selections, target);
/// </code>
/// </example>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="BatchSimplificationOptions"/>
/// <param name="DocumentPath">
/// The path to the document containing the selection.
/// Used for context gathering and change tracking.
/// </param>
/// <param name="StartOffset">
/// The zero-based character offset where the selection begins.
/// Must be non-negative and within the document bounds.
/// </param>
/// <param name="Length">
/// The number of characters in the selection.
/// Must be positive.
/// </param>
/// <param name="Text">
/// The text content of the selection.
/// Used for simplification processing.
/// </param>
public record TextSelection(
    string DocumentPath,
    int StartOffset,
    int Length,
    string Text)
{
    /// <summary>
    /// Gets the exclusive end offset of the selection.
    /// </summary>
    /// <value>
    /// The character offset immediately after the last character of the selection.
    /// Calculated as <c>StartOffset + Length</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The end offset follows the standard half-open interval convention
    /// [StartOffset, EndOffset) used throughout .NET for string operations.
    /// </remarks>
    public int EndOffset => StartOffset + Length;

    /// <summary>
    /// Gets a preview of the selection text, truncated for display.
    /// </summary>
    /// <value>
    /// The first 50 characters of the selection text, followed by "..."
    /// if the text exceeds 50 characters.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Preview text is useful for progress reporting and logging
    /// without including the full selection content.
    /// </remarks>
    public string TextPreview =>
        Text.Length <= 50 ? Text : Text[..47] + "...";

    /// <summary>
    /// Validates the selection and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the selection is invalid.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Performs the following validation checks:
    /// </para>
    /// <list type="number">
    ///   <item><description><see cref="DocumentPath"/> is not null or empty</description></item>
    ///   <item><description><see cref="StartOffset"/> is non-negative</description></item>
    ///   <item><description><see cref="Length"/> is positive</description></item>
    ///   <item><description><see cref="Text"/> is not null or empty</description></item>
    ///   <item><description><see cref="Text"/> length matches <see cref="Length"/></description></item>
    /// </list>
    /// </remarks>
    public void Validate()
    {
        // LOGIC: Validate DocumentPath is not null or empty
        if (string.IsNullOrWhiteSpace(DocumentPath))
        {
            throw new ArgumentException(
                "Document path is required and cannot be empty or whitespace.",
                nameof(DocumentPath));
        }

        // LOGIC: Validate StartOffset is non-negative
        if (StartOffset < 0)
        {
            throw new ArgumentException(
                $"Start offset must be non-negative. Current value: {StartOffset}.",
                nameof(StartOffset));
        }

        // LOGIC: Validate Length is positive
        if (Length <= 0)
        {
            throw new ArgumentException(
                $"Length must be positive. Current value: {Length}.",
                nameof(Length));
        }

        // LOGIC: Validate Text is not null or empty
        if (string.IsNullOrEmpty(Text))
        {
            throw new ArgumentException(
                "Text content is required and cannot be null or empty.",
                nameof(Text));
        }

        // LOGIC: Validate Text length matches Length
        if (Text.Length != Length)
        {
            throw new ArgumentException(
                $"Text length ({Text.Length}) does not match specified length ({Length}).",
                nameof(Text));
        }
    }
}
