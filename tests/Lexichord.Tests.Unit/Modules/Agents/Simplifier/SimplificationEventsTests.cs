// -----------------------------------------------------------------------
// <copyright file="SimplificationEventsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Agents.Simplifier.Events;
using MediatR;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for the Simplifier Agent v0.7.4c events:
/// <see cref="SimplificationAcceptedEvent"/>,
/// <see cref="SimplificationRejectedEvent"/>, and
/// <see cref="ResimplificationRequestedEvent"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4c")]
public class SimplificationEventsTests
{
    // ══════════════════════════════════════════════════════════════════════
    // SimplificationAcceptedEvent Tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SimplificationAcceptedEvent_ImplementsINotification()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: "/path/to/doc.md",
            OriginalText: "original",
            SimplifiedText: "simplified",
            AcceptedChangeCount: 5,
            TotalChangeCount: 7,
            GradeLevelReduction: 4.2);

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void SimplificationAcceptedEvent_IsPartialAcceptance_TrueWhenFewerAccepted()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: "/path/to/doc.md",
            OriginalText: "original",
            SimplifiedText: "simplified",
            AcceptedChangeCount: 3,
            TotalChangeCount: 5,
            GradeLevelReduction: 2.0);

        // Assert
        evt.IsPartialAcceptance.Should().BeTrue();
    }

    [Fact]
    public void SimplificationAcceptedEvent_IsPartialAcceptance_FalseWhenAllAccepted()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: "/path/to/doc.md",
            OriginalText: "original",
            SimplifiedText: "simplified",
            AcceptedChangeCount: 5,
            TotalChangeCount: 5,
            GradeLevelReduction: 3.0);

        // Assert
        evt.IsPartialAcceptance.Should().BeFalse();
    }

    [Fact]
    public void SimplificationAcceptedEvent_AcceptanceRate_CalculatedCorrectly()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: "/path/to/doc.md",
            OriginalText: "original",
            SimplifiedText: "simplified",
            AcceptedChangeCount: 3,
            TotalChangeCount: 6,
            GradeLevelReduction: 2.5);

        // Assert
        evt.AcceptanceRate.Should().BeApproximately(0.5, 0.001);
    }

    [Fact]
    public void SimplificationAcceptedEvent_AcceptanceRate_ReturnsOneWhenZeroTotal()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: "/path/to/doc.md",
            OriginalText: "original",
            SimplifiedText: "simplified",
            AcceptedChangeCount: 0,
            TotalChangeCount: 0,
            GradeLevelReduction: 0);

        // Assert
        evt.AcceptanceRate.Should().Be(1.0);
    }

    [Fact]
    public void SimplificationAcceptedEvent_AllowsNullDocumentPath()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: null,
            OriginalText: "original",
            SimplifiedText: "simplified",
            AcceptedChangeCount: 2,
            TotalChangeCount: 2,
            GradeLevelReduction: 1.5);

        // Assert
        evt.DocumentPath.Should().BeNull();
    }

    [Fact]
    public void SimplificationAcceptedEvent_PreservesAllProperties()
    {
        // Arrange
        var evt = new SimplificationAcceptedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "The original text.",
            SimplifiedText: "The simple text.",
            AcceptedChangeCount: 4,
            TotalChangeCount: 5,
            GradeLevelReduction: 3.2);

        // Assert
        evt.DocumentPath.Should().Be("/doc.md");
        evt.OriginalText.Should().Be("The original text.");
        evt.SimplifiedText.Should().Be("The simple text.");
        evt.AcceptedChangeCount.Should().Be(4);
        evt.TotalChangeCount.Should().Be(5);
        evt.GradeLevelReduction.Should().BeApproximately(3.2, 0.001);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SimplificationRejectedEvent Tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SimplificationRejectedEvent_ImplementsINotification()
    {
        // Arrange
        var evt = new SimplificationRejectedEvent(
            DocumentPath: "/path/to/doc.md",
            Reason: "User cancelled");

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void SimplificationRejectedEvent_HasStandardReasonConstants()
    {
        // Assert
        SimplificationRejectedEvent.ReasonUserCancelled.Should().Be("User cancelled");
        SimplificationRejectedEvent.ReasonPreviewClosed.Should().Be("Preview closed");
        SimplificationRejectedEvent.ReasonDocumentClosed.Should().Be("Document closed");
        SimplificationRejectedEvent.ReasonLicenseExpired.Should().Be("License expired");
    }

    [Fact]
    public void SimplificationRejectedEvent_UserCancelled_CreatesCorrectEvent()
    {
        // Arrange
        const string documentPath = "/path/to/document.md";

        // Act
        var evt = SimplificationRejectedEvent.UserCancelled(documentPath);

        // Assert
        evt.DocumentPath.Should().Be(documentPath);
        evt.Reason.Should().Be(SimplificationRejectedEvent.ReasonUserCancelled);
    }

    [Fact]
    public void SimplificationRejectedEvent_PreviewClosed_CreatesCorrectEvent()
    {
        // Arrange
        const string documentPath = "/another/path.md";

        // Act
        var evt = SimplificationRejectedEvent.PreviewClosed(documentPath);

        // Assert
        evt.DocumentPath.Should().Be(documentPath);
        evt.Reason.Should().Be(SimplificationRejectedEvent.ReasonPreviewClosed);
    }

    [Fact]
    public void SimplificationRejectedEvent_AllowsNullDocumentPath()
    {
        // Act
        var evt = SimplificationRejectedEvent.UserCancelled(null);

        // Assert
        evt.DocumentPath.Should().BeNull();
    }

    [Fact]
    public void SimplificationRejectedEvent_AllowsNullReason()
    {
        // Act
        var evt = new SimplificationRejectedEvent(
            DocumentPath: "/doc.md",
            Reason: null);

        // Assert
        evt.Reason.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════════
    // ResimplificationRequestedEvent Tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ResimplificationRequestedEvent_ImplementsINotification()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/path/to/doc.md",
            OriginalText: "Original text",
            NewPresetId: "general-public");

        // Assert
        evt.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void ResimplificationRequestedEvent_IsPresetChange_TrueWhenPresetIdProvided()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Text",
            NewPresetId: "technical",
            NewStrategy: null);

        // Assert
        evt.IsPresetChange.Should().BeTrue();
    }

    [Fact]
    public void ResimplificationRequestedEvent_IsPresetChange_FalseWhenPresetIdNull()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Text",
            NewPresetId: null,
            NewStrategy: SimplificationStrategy.Aggressive);

        // Assert
        evt.IsPresetChange.Should().BeFalse();
    }

    [Fact]
    public void ResimplificationRequestedEvent_IsStrategyChange_TrueWhenStrategyProvided()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Text",
            NewPresetId: null,
            NewStrategy: SimplificationStrategy.Conservative);

        // Assert
        evt.IsStrategyChange.Should().BeTrue();
    }

    [Fact]
    public void ResimplificationRequestedEvent_IsStrategyChange_FalseWhenStrategyNull()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Text",
            NewPresetId: "general-public",
            NewStrategy: null);

        // Assert
        evt.IsStrategyChange.Should().BeFalse();
    }

    [Fact]
    public void ResimplificationRequestedEvent_BothChanges_WhenBothProvided()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Text",
            NewPresetId: "executive",
            NewStrategy: SimplificationStrategy.Balanced);

        // Assert
        evt.IsPresetChange.Should().BeTrue();
        evt.IsStrategyChange.Should().BeTrue();
    }

    [Fact]
    public void ResimplificationRequestedEvent_AllowsNullDocumentPath()
    {
        // Act
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: null,
            OriginalText: "Untitled text",
            NewPresetId: "international");

        // Assert
        evt.DocumentPath.Should().BeNull();
    }

    [Fact]
    public void ResimplificationRequestedEvent_PreservesAllProperties()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Complex text here",
            NewPresetId: "general-public",
            NewStrategy: SimplificationStrategy.Aggressive);

        // Assert
        evt.DocumentPath.Should().Be("/doc.md");
        evt.OriginalText.Should().Be("Complex text here");
        evt.NewPresetId.Should().Be("general-public");
        evt.NewStrategy.Should().Be(SimplificationStrategy.Aggressive);
    }

    [Fact]
    public void ResimplificationRequestedEvent_DefaultStrategy_IsNull()
    {
        // Arrange
        var evt = new ResimplificationRequestedEvent(
            DocumentPath: "/doc.md",
            OriginalText: "Text",
            NewPresetId: "technical");

        // Assert
        evt.NewStrategy.Should().BeNull();
    }
}
