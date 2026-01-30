using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="ViolationDiff"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3d
/// </remarks>
public sealed class ViolationDiffTests
{
    private static AggregatedStyleViolation CreateViolation(
        string id,
        int line = 1,
        string? message = null) =>
        new()
        {
            Id = id,
            DocumentId = "doc-001",
            RuleId = "rule-001",
            StartOffset = 0,
            Length = 5,
            Line = line,
            Column = 1,
            EndLine = line,
            EndColumn = 6,
            ViolatingText = "text",
            Message = message ?? $"Violation {id}",
            Severity = ViolationSeverity.Warning,
            Category = RuleCategory.Syntax
        };

    #region Empty Lists

    [Fact]
    public void Calculate_BothEmpty_ReturnsNoChanges()
    {
        // Act
        var changes = ViolationDiff.Calculate([], []);

        // Assert
        changes.HasChanges.Should().BeFalse();
        changes.Added.Should().BeEmpty();
        changes.Removed.Should().BeEmpty();
        changes.Modified.Should().BeEmpty();
        changes.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Calculate_PreviousEmpty_AllAdded()
    {
        // Arrange
        var current = new List<AggregatedStyleViolation>
        {
            CreateViolation("v1"),
            CreateViolation("v2")
        };

        // Act
        var changes = ViolationDiff.Calculate([], current);

        // Assert
        changes.HasChanges.Should().BeTrue();
        changes.Added.Should().HaveCount(2);
        changes.Removed.Should().BeEmpty();
        changes.Modified.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_CurrentEmpty_AllRemoved()
    {
        // Arrange
        var previous = new List<AggregatedStyleViolation>
        {
            CreateViolation("v1"),
            CreateViolation("v2")
        };

        // Act
        var changes = ViolationDiff.Calculate(previous, []);

        // Assert
        changes.HasChanges.Should().BeTrue();
        changes.Added.Should().BeEmpty();
        changes.Removed.Should().HaveCount(2);
        changes.Modified.Should().BeEmpty();
    }

    #endregion

    #region Added Violations

    [Fact]
    public void Calculate_NewViolation_DetectedAsAdded()
    {
        // Arrange
        var previous = new List<AggregatedStyleViolation>
        {
            CreateViolation("existing")
        };
        var current = new List<AggregatedStyleViolation>
        {
            CreateViolation("existing"),
            CreateViolation("new-one")
        };

        // Act
        var changes = ViolationDiff.Calculate(previous, current);

        // Assert
        changes.Added.Should().ContainSingle()
            .Which.Id.Should().Be("new-one");
        changes.Removed.Should().BeEmpty();
        changes.Modified.Should().BeEmpty();
    }

    #endregion

    #region Removed Violations

    [Fact]
    public void Calculate_RemovedViolation_DetectedAsRemoved()
    {
        // Arrange
        var previous = new List<AggregatedStyleViolation>
        {
            CreateViolation("will-remain"),
            CreateViolation("will-be-removed")
        };
        var current = new List<AggregatedStyleViolation>
        {
            CreateViolation("will-remain")
        };

        // Act
        var changes = ViolationDiff.Calculate(previous, current);

        // Assert
        changes.Removed.Should().ContainSingle()
            .Which.Id.Should().Be("will-be-removed");
        changes.Added.Should().BeEmpty();
        changes.Modified.Should().BeEmpty();
    }

    #endregion

    #region Modified Violations

    [Fact]
    public void Calculate_ModifiedMessage_DetectedAsModified()
    {
        // Arrange
        var previous = new List<AggregatedStyleViolation>
        {
            CreateViolation("same-id", message: "Old message")
        };
        var current = new List<AggregatedStyleViolation>
        {
            CreateViolation("same-id", message: "New message")
        };

        // Act
        var changes = ViolationDiff.Calculate(previous, current);

        // Assert
        changes.Modified.Should().ContainSingle()
            .Which.Message.Should().Be("New message");
        changes.Added.Should().BeEmpty();
        changes.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ModifiedLine_DetectedAsModified()
    {
        // Arrange
        var previous = new List<AggregatedStyleViolation>
        {
            CreateViolation("same-id", line: 5)
        };
        var current = new List<AggregatedStyleViolation>
        {
            CreateViolation("same-id", line: 10)
        };

        // Act
        var changes = ViolationDiff.Calculate(previous, current);

        // Assert
        changes.Modified.Should().ContainSingle()
            .Which.Line.Should().Be(10);
    }

    [Fact]
    public void Calculate_IdenticalViolation_NotModified()
    {
        // Arrange
        var violation = CreateViolation("same-id");
        var previous = new List<AggregatedStyleViolation> { violation };
        var current = new List<AggregatedStyleViolation> { violation };

        // Act
        var changes = ViolationDiff.Calculate(previous, current);

        // Assert
        changes.HasChanges.Should().BeFalse();
        changes.Modified.Should().BeEmpty();
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    public void Calculate_MixedChanges_DetectsAll()
    {
        // Arrange
        var previous = new List<AggregatedStyleViolation>
        {
            CreateViolation("unchanged"),
            CreateViolation("to-remove"),
            CreateViolation("to-modify", message: "Old")
        };
        var current = new List<AggregatedStyleViolation>
        {
            CreateViolation("unchanged"),
            CreateViolation("newly-added"),
            CreateViolation("to-modify", message: "New")
        };

        // Act
        var changes = ViolationDiff.Calculate(previous, current);

        // Assert
        changes.HasChanges.Should().BeTrue();
        changes.Added.Should().ContainSingle().Which.Id.Should().Be("newly-added");
        changes.Removed.Should().ContainSingle().Which.Id.Should().Be("to-remove");
        changes.Modified.Should().ContainSingle().Which.Id.Should().Be("to-modify");
        changes.TotalChanges.Should().Be(3);
    }

    #endregion

    #region ViolationChanges Struct

    [Fact]
    public void ViolationChanges_Empty_HasNoChanges()
    {
        // Act
        var empty = ViolationChanges.Empty;

        // Assert
        empty.HasChanges.Should().BeFalse();
        empty.TotalChanges.Should().Be(0);
    }

    #endregion
}
