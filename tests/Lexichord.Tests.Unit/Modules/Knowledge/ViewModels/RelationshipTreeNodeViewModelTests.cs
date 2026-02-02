// =============================================================================
// File: RelationshipTreeNodeViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the Relationship Tree Node ViewModel.
// =============================================================================
// LOGIC: Tests factory methods, property initialization, and node types for
//   the RelationshipTreeNodeViewModel.
//
// v0.4.7h: Relationship Viewer (Knowledge Graph Browser)
// Dependencies: xUnit, FluentAssertions
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.UI.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.ViewModels;

/// <summary>
/// Unit tests for <see cref="RelationshipTreeNodeViewModel"/>.
/// </summary>
public class RelationshipTreeNodeViewModelTests
{
    #region CreateGroupNode Tests

    [Fact]
    public void CreateGroupNode_CreatesNodeWithCorrectProperties()
    {
        // Arrange & Act
        var node = RelationshipTreeNodeViewModel.CreateGroupNode(
            type: "RELATES_TO",
            direction: RelationshipDirection.Outgoing,
            count: 5);

        // Assert
        node.Label.Should().Be("RELATES_TO (5)");
        node.Type.Should().Be("RELATES_TO");
        node.Direction.Should().Be(RelationshipDirection.Outgoing);
        node.IsGroup.Should().BeTrue();
        node.IsExpanded.Should().BeFalse();
        node.EntityId.Should().BeNull();
        node.EntityName.Should().BeEmpty();
        node.EntityType.Should().BeEmpty();
        node.Children.Should().BeEmpty();
    }

    [Fact]
    public void CreateGroupNode_WithDifferentDirection_SetsDirectionCorrectly()
    {
        // Act
        var incomingNode = RelationshipTreeNodeViewModel.CreateGroupNode(
            "Test", RelationshipDirection.Incoming, 1);
        var outgoingNode = RelationshipTreeNodeViewModel.CreateGroupNode(
            "Test", RelationshipDirection.Outgoing, 2);
        var bothNode = RelationshipTreeNodeViewModel.CreateGroupNode(
            "Test", RelationshipDirection.Both, 3);

        // Assert
        incomingNode.Direction.Should().Be(RelationshipDirection.Incoming);
        outgoingNode.Direction.Should().Be(RelationshipDirection.Outgoing);
        bothNode.Direction.Should().Be(RelationshipDirection.Both);
    }

    [Fact]
    public void CreateGroupNode_IncludesCountInLabel()
    {
        // Act
        var node = RelationshipTreeNodeViewModel.CreateGroupNode(
            "DEPENDS_ON", RelationshipDirection.Outgoing, 10);

        // Assert
        node.Label.Should().Be("DEPENDS_ON (10)");
    }

    #endregion

    #region CreateLeafNode Tests

    [Fact]
    public void CreateLeafNode_CreatesNodeWithCorrectProperties()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var node = RelationshipTreeNodeViewModel.CreateLeafNode(
            entityId: entityId,
            entityName: "Test Entity",
            entityType: "Concept",
            relationshipType: "RELATES_TO",
            direction: RelationshipDirection.Outgoing);

        // Assert
        node.Label.Should().Be("Concept: Test Entity");
        node.EntityId.Should().Be(entityId);
        node.EntityName.Should().Be("Test Entity");
        node.EntityType.Should().Be("Concept");
        node.Type.Should().Be("RELATES_TO");
        node.Direction.Should().Be(RelationshipDirection.Outgoing);
        node.IsGroup.Should().BeFalse();
        node.IsExpanded.Should().BeFalse();
        node.Children.Should().BeEmpty();
    }

    [Fact]
    public void CreateLeafNode_FormatsLabelCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var node = RelationshipTreeNodeViewModel.CreateLeafNode(
            entityId, "My API Endpoint", "Endpoint", "CONTAINS", RelationshipDirection.Both);

        // Assert
        node.Label.Should().Be("Endpoint: My API Endpoint");
    }

    #endregion

    #region CreateDirectionHeader Tests

    [Fact]
    public void CreateDirectionHeader_Incoming_CreatesCorrectLabel()
    {
        // Act
        var node = RelationshipTreeNodeViewModel.CreateDirectionHeader(
            RelationshipDirection.Incoming);

        // Assert
        node.Label.Should().Be("← Incoming");
        node.IsGroup.Should().BeTrue();
        node.Direction.Should().Be(RelationshipDirection.Incoming);
        node.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void CreateDirectionHeader_Outgoing_CreatesCorrectLabel()
    {
        // Act
        var node = RelationshipTreeNodeViewModel.CreateDirectionHeader(
            RelationshipDirection.Outgoing);

        // Assert
        node.Label.Should().Be("Outgoing →");
        node.IsGroup.Should().BeTrue();
        node.Direction.Should().Be(RelationshipDirection.Outgoing);
        node.IsExpanded.Should().BeTrue();
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void Label_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var node = new RelationshipTreeNodeViewModel();
        var propertyChangedRaised = false;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(node.Label))
                propertyChangedRaised = true;
        };

        // Act
        node.Label = "New Label";

        // Assert
        propertyChangedRaised.Should().BeTrue();
        node.Label.Should().Be("New Label");
    }

    [Fact]
    public void IsExpanded_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var node = new RelationshipTreeNodeViewModel();
        var propertyChangedRaised = false;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(node.IsExpanded))
                propertyChangedRaised = true;
        };

        // Act
        node.IsExpanded = true;

        // Assert
        propertyChangedRaised.Should().BeTrue();
        node.IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void IsGroup_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var node = new RelationshipTreeNodeViewModel();
        var propertyChangedRaised = false;
        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(node.IsGroup))
                propertyChangedRaised = true;
        };

        // Act
        node.IsGroup = true;

        // Assert
        propertyChangedRaised.Should().BeTrue();
        node.IsGroup.Should().BeTrue();
    }

    #endregion

    #region Children Collection Tests

    [Fact]
    public void Children_CanAddNodes()
    {
        // Arrange
        var parent = RelationshipTreeNodeViewModel.CreateGroupNode(
            "RELATES_TO", RelationshipDirection.Outgoing, 1);
        var child = RelationshipTreeNodeViewModel.CreateLeafNode(
            Guid.NewGuid(), "Child", "Concept", "RELATES_TO", RelationshipDirection.Outgoing);

        // Act
        parent.Children.Add(child);

        // Assert
        parent.Children.Should().Contain(child);
        parent.Children.Should().HaveCount(1);
        parent.ChildCount.Should().Be(1);
    }

    [Fact]
    public void Children_SupportsMultipleNodes()
    {
        // Arrange
        var parent = RelationshipTreeNodeViewModel.CreateGroupNode(
            "RELATES_TO", RelationshipDirection.Outgoing, 5);

        // Act
        for (int i = 0; i < 5; i++)
        {
            parent.Children.Add(RelationshipTreeNodeViewModel.CreateLeafNode(
                Guid.NewGuid(), $"Child {i}", "Concept", "RELATES_TO", RelationshipDirection.Outgoing));
        }

        // Assert
        parent.Children.Should().HaveCount(5);
        parent.ChildCount.Should().Be(5);
    }

    #endregion
}
