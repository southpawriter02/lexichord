// -----------------------------------------------------------------------
// <copyright file="RewriteUndoableOperationTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Agents.Editor;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Editor;

/// <summary>
/// Unit tests for <see cref="RewriteUndoableOperation"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.3d")]
public class RewriteUndoableOperationTests
{
    private readonly Mock<IEditorService> _editorServiceMock;

    public RewriteUndoableOperationTests()
    {
        _editorServiceMock = new Mock<IEditorService>();
    }

    // ── Constructor Validation Tests ────────────────────────────────────

    [Fact]
    public void Constructor_NullDocumentPath_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteUndoableOperation(
            null!, new TextSpan(0, 5), "hello", "world",
            RewriteIntent.Formal, _editorServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("documentPath");
    }

    [Fact]
    public void Constructor_NullOriginalText_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteUndoableOperation(
            "/test.md", new TextSpan(0, 5), null!, "world",
            RewriteIntent.Formal, _editorServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("originalText");
    }

    [Fact]
    public void Constructor_NullRewrittenText_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteUndoableOperation(
            "/test.md", new TextSpan(0, 5), "hello", null!,
            RewriteIntent.Formal, _editorServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("rewrittenText");
    }

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RewriteUndoableOperation(
            "/test.md", new TextSpan(0, 5), "hello", "world",
            RewriteIntent.Formal, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("editorService");
    }

    // ── Properties Tests ────────────────────────────────────────────────

    [Fact]
    public void Id_IsUnique_AcrossInstances()
    {
        // Arrange
        var op1 = CreateOperation("hello", "world");
        var op2 = CreateOperation("hello", "world");

        // Assert
        op1.Id.Should().NotBe(op2.Id);
    }

    [Fact]
    public void Id_IsValidGuid()
    {
        // Arrange
        var operation = CreateOperation("hello", "world");

        // Assert
        Guid.TryParse(operation.Id, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(RewriteIntent.Formal, "AI Rewrite (Formal)")]
    [InlineData(RewriteIntent.Simplified, "AI Rewrite (Simplified)")]
    [InlineData(RewriteIntent.Expanded, "AI Rewrite (Expanded)")]
    [InlineData(RewriteIntent.Custom, "AI Rewrite (Custom)")]
    public void DisplayName_ReflectsIntent(RewriteIntent intent, string expectedName)
    {
        // Arrange
        var operation = new RewriteUndoableOperation(
            "/test.md", new TextSpan(0, 1), "a", "b",
            intent, _editorServiceMock.Object);

        // Assert
        operation.DisplayName.Should().Be(expectedName);
    }

    [Fact]
    public void Timestamp_IsRecentUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var operation = CreateOperation("hello", "world");
        var after = DateTime.UtcNow;

        // Assert
        operation.Timestamp.Should().BeOnOrAfter(before);
        operation.Timestamp.Should().BeOnOrBefore(after);
    }

    // ── ExecuteAsync Tests ──────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DeletesOriginalAndInsertsRewritten()
    {
        // Arrange
        var operation = CreateOperation("hello", "Hello, World!");

        // Act
        await operation.ExecuteAsync();

        // Assert — Verifies the correct delete + insert sequence
        _editorServiceMock.Verify(
            e => e.DeleteText(0, 5), // "hello".Length = 5
            Times.Once);
        _editorServiceMock.Verify(
            e => e.InsertText(0, "Hello, World!"),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WrapsInUndoGroup()
    {
        // Arrange
        var operation = CreateOperation("hello", "world");
        var callOrder = new List<string>();

        _editorServiceMock
            .Setup(e => e.BeginUndoGroup(It.IsAny<string>()))
            .Callback<string>(_ => callOrder.Add("BeginUndoGroup"));
        _editorServiceMock
            .Setup(e => e.DeleteText(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((_, _) => callOrder.Add("DeleteText"));
        _editorServiceMock
            .Setup(e => e.InsertText(It.IsAny<int>(), It.IsAny<string>()))
            .Callback<int, string>((_, _) => callOrder.Add("InsertText"));
        _editorServiceMock
            .Setup(e => e.EndUndoGroup())
            .Callback(() => callOrder.Add("EndUndoGroup"));

        // Act
        await operation.ExecuteAsync();

        // Assert — Verify the call order: BeginUndoGroup → DeleteText → InsertText → EndUndoGroup
        callOrder.Should().Equal("BeginUndoGroup", "DeleteText", "InsertText", "EndUndoGroup");
    }

    [Fact]
    public async Task ExecuteAsync_UsesDisplayNameForUndoGroup()
    {
        // Arrange
        var operation = new RewriteUndoableOperation(
            "/test.md", new TextSpan(0, 5), "hello", "world",
            RewriteIntent.Simplified, _editorServiceMock.Object);

        // Act
        await operation.ExecuteAsync();

        // Assert
        _editorServiceMock.Verify(
            e => e.BeginUndoGroup("AI Rewrite (Simplified)"),
            Times.Once);
    }

    // ── UndoAsync Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task UndoAsync_RestoresOriginalText()
    {
        // Arrange
        var operation = CreateOperation("hello", "Hello, World!");

        // Act
        await operation.UndoAsync();

        // Assert — Deletes the rewritten text length and inserts original
        _editorServiceMock.Verify(
            e => e.DeleteText(0, 13), // "Hello, World!".Length = 13
            Times.Once);
        _editorServiceMock.Verify(
            e => e.InsertText(0, "hello"),
            Times.Once);
    }

    [Fact]
    public async Task UndoAsync_UsesRewrittenTextLengthForSpan()
    {
        // Arrange — Rewrite at offset 10, original "abc" (3 chars) → "abcdef" (6 chars)
        var operation = new RewriteUndoableOperation(
            "/test.md", new TextSpan(10, 3), "abc", "abcdef",
            RewriteIntent.Expanded, _editorServiceMock.Object);

        // Act
        await operation.UndoAsync();

        // Assert — Should delete 6 chars (rewritten length) starting at offset 10
        _editorServiceMock.Verify(
            e => e.DeleteText(10, 6),
            Times.Once);
        _editorServiceMock.Verify(
            e => e.InsertText(10, "abc"),
            Times.Once);
    }

    // ── RedoAsync Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task RedoAsync_ReappliesRewrittenText()
    {
        // Arrange
        var operation = CreateOperation("hello", "Hello, World!");

        // Act
        await operation.RedoAsync();

        // Assert — Same as ExecuteAsync: deletes original length, inserts rewritten
        _editorServiceMock.Verify(
            e => e.DeleteText(0, 5),
            Times.Once);
        _editorServiceMock.Verify(
            e => e.InsertText(0, "Hello, World!"),
            Times.Once);
    }

    // ── Full Cycle Test ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteUndoRedoCycle_MaintainsCorrectState()
    {
        // Arrange
        var operation = CreateOperation("hello", "Hello, World!");

        // Act — Execute → Undo → Redo
        await operation.ExecuteAsync();
        await operation.UndoAsync();
        await operation.RedoAsync();

        // Assert — Execute: delete 5, insert "Hello, World!"
        //          Undo: delete 13, insert "hello"
        //          Redo: delete 5, insert "Hello, World!"
        _editorServiceMock.Verify(
            e => e.DeleteText(0, 5),
            Times.Exactly(2)); // Execute + Redo
        _editorServiceMock.Verify(
            e => e.InsertText(0, "Hello, World!"),
            Times.Exactly(2)); // Execute + Redo
        _editorServiceMock.Verify(
            e => e.DeleteText(0, 13),
            Times.Once); // Undo only
        _editorServiceMock.Verify(
            e => e.InsertText(0, "hello"),
            Times.Once); // Undo only
    }

    // ── ToString Test ───────────────────────────────────────────────────

    [Fact]
    public void ToString_ContainsOperationInfo()
    {
        // Arrange
        var operation = CreateOperation("hello", "Hello, World!");

        // Act
        var result = operation.ToString();

        // Assert
        result.Should().Contain("RewriteOperation(");
        result.Should().Contain("Formal");
        result.Should().Contain("5 -> 13 chars");
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    private RewriteUndoableOperation CreateOperation(
        string original,
        string rewritten,
        RewriteIntent intent = RewriteIntent.Formal) =>
        new(
            "/test.md",
            new TextSpan(0, original.Length),
            original,
            rewritten,
            intent,
            _editorServiceMock.Object);
}
