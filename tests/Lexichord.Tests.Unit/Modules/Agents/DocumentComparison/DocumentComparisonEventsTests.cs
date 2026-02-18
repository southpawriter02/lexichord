// -----------------------------------------------------------------------
// <copyright file="DocumentComparisonEventsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.DocumentComparison.Events;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.DocumentComparison;

/// <summary>
/// Unit tests for Document Comparison MediatR events.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6d")]
public class DocumentComparisonEventsTests
{
    // ══════════════════════════════════════════════════════════════════════
    // DocumentComparisonStartedEvent Tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void StartedEvent_Create_SetsCurrentTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var evt = DocumentComparisonStartedEvent.Create(
            "/original.md",
            "/new.md",
            1000,
            1100);

        var after = DateTime.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void StartedEvent_Create_SetsAllProperties()
    {
        // Act
        var evt = DocumentComparisonStartedEvent.Create(
            "/path/to/original.md",
            "/path/to/new.md",
            5000,
            5500);

        // Assert
        evt.OriginalPath.Should().Be("/path/to/original.md");
        evt.NewPath.Should().Be("/path/to/new.md");
        evt.OriginalCharacterCount.Should().Be(5000);
        evt.NewCharacterCount.Should().Be(5500);
    }

    [Fact]
    public void StartedEvent_Create_WithNullPaths_Succeeds()
    {
        // Act
        var evt = DocumentComparisonStartedEvent.Create(null, null, 100, 200);

        // Assert
        evt.OriginalPath.Should().BeNull();
        evt.NewPath.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════════
    // DocumentComparisonCompletedEvent Tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CompletedEvent_Create_SetsCurrentTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var evt = DocumentComparisonCompletedEvent.Create(
            "/original.md",
            "/new.md",
            5,
            0.5,
            TimeSpan.FromSeconds(2));

        var after = DateTime.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void CompletedEvent_Create_SetsAllProperties()
    {
        // Act
        var evt = DocumentComparisonCompletedEvent.Create(
            "/original.md",
            "/new.md",
            10,
            0.75,
            TimeSpan.FromMilliseconds(1500));

        // Assert
        evt.OriginalPath.Should().Be("/original.md");
        evt.NewPath.Should().Be("/new.md");
        evt.ChangeCount.Should().Be(10);
        evt.ChangeMagnitude.Should().Be(0.75);
        evt.Duration.TotalMilliseconds.Should().Be(1500);
    }

    [Fact]
    public void CompletedEvent_AreIdentical_WhenZeroChanges_ReturnsTrue()
    {
        // Arrange
        var evt = DocumentComparisonCompletedEvent.Create(
            "/original.md",
            "/new.md",
            changeCount: 0,
            changeMagnitude: 0.0,
            TimeSpan.Zero);

        // Assert
        evt.AreIdentical.Should().BeTrue();
    }

    [Fact]
    public void CompletedEvent_AreIdentical_WhenChangesExist_ReturnsFalse()
    {
        // Arrange
        var evt = DocumentComparisonCompletedEvent.Create(
            "/original.md",
            "/new.md",
            changeCount: 5,
            changeMagnitude: 0.3,
            TimeSpan.FromSeconds(1));

        // Assert
        evt.AreIdentical.Should().BeFalse();
    }

    [Fact]
    public void CompletedEvent_MagnitudePercentage_CalculatesCorrectly()
    {
        // Arrange
        var evt = DocumentComparisonCompletedEvent.Create(
            "/original.md",
            "/new.md",
            5,
            changeMagnitude: 0.75,
            TimeSpan.FromSeconds(1));

        // Assert
        evt.MagnitudePercentage.Should().Be("75%");
    }

    [Theory]
    [InlineData(0.0, "0%")]
    [InlineData(0.5, "50%")]
    [InlineData(1.0, "100%")]
    [InlineData(0.333, "33%")]
    public void CompletedEvent_MagnitudePercentage_RoundsCorrectly(double magnitude, string expected)
    {
        // Arrange
        var evt = DocumentComparisonCompletedEvent.Create(
            null, null, 1, magnitude, TimeSpan.Zero);

        // Assert
        evt.MagnitudePercentage.Should().Be(expected);
    }

    // ══════════════════════════════════════════════════════════════════════
    // DocumentComparisonFailedEvent Tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FailedEvent_Create_SetsCurrentTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var evt = DocumentComparisonFailedEvent.Create(
            "/original.md",
            "/new.md",
            "Error message");

        var after = DateTime.UtcNow;

        // Assert
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void FailedEvent_Create_SetsAllProperties()
    {
        // Act
        var evt = DocumentComparisonFailedEvent.Create(
            "/path/to/original.md",
            "/path/to/new.md",
            "File not found");

        // Assert
        evt.OriginalPath.Should().Be("/path/to/original.md");
        evt.NewPath.Should().Be("/path/to/new.md");
        evt.ErrorMessage.Should().Be("File not found");
    }

    [Fact]
    public void FailedEvent_Create_WithNullPaths_Succeeds()
    {
        // Act
        var evt = DocumentComparisonFailedEvent.Create(null, null, "Error");

        // Assert
        evt.OriginalPath.Should().BeNull();
        evt.NewPath.Should().BeNull();
        evt.ErrorMessage.Should().Be("Error");
    }

    [Fact]
    public void FailedEvent_FromException_ExtractsMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Operation failed");

        // Act
        var evt = DocumentComparisonFailedEvent.FromException(
            "/original.md",
            "/new.md",
            exception);

        // Assert
        evt.ErrorMessage.Should().Be("Operation failed");
    }

    [Fact]
    public void FailedEvent_FromException_WithNestedMessage_UsesInnerMessage()
    {
        // Arrange
        var innerException = new FileNotFoundException("Inner file error");
        var outerException = new AggregateException("Outer error", innerException);

        // Act
        var evt = DocumentComparisonFailedEvent.FromException(
            "/original.md",
            "/new.md",
            outerException);

        // Assert
        // Uses the outer exception message, not the inner
        evt.ErrorMessage.Should().Contain("Outer error");
    }
}
