using Lexichord.Abstractions.Layout;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lexichord.Tests.Unit.Host.Layout;

/// <summary>
/// Unit tests for LayoutData serialization and deserialization.
/// </summary>
[Trait("Category", "Unit")]
public class LayoutDataSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void LayoutData_RoundTrip_PreservesData()
    {
        // Arrange
        var original = CreateSampleLayoutData();

        // Act
        var json = JsonSerializer.Serialize(original, SerializerOptions);
        var deserialized = JsonSerializer.Deserialize<LayoutData>(json, SerializerOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Metadata.SchemaVersion.Should().Be(original.Metadata.SchemaVersion);
        deserialized.Metadata.ProfileName.Should().Be(original.Metadata.ProfileName);
        deserialized.Metadata.AppVersion.Should().Be(original.Metadata.AppVersion);
        deserialized.Root.Id.Should().Be(original.Root.Id);
        deserialized.Root.Type.Should().Be(original.Root.Type);
        deserialized.Root.Children.Should().HaveCount(3);
    }

    [Fact]
    public void DockNodeType_SerializesAsString()
    {
        // Arrange
        var nodeData = new DockNodeData(
            "test-id",
            DockNodeType.Proportional,
            new DockNodeProperties());

        // Act
        var json = JsonSerializer.Serialize(nodeData, SerializerOptions);

        // Assert
        json.Should().Contain("\"type\": \"proportional\"");
    }

    [Fact]
    public void DockNodeProperties_NullValuesOmitted()
    {
        // Arrange
        var properties = new DockNodeProperties(
            Title: "Test Tool",
            Proportion: null,
            Orientation: null,
            Alignment: null,
            IsCollapsed: null,
            IsActive: null,
            ActiveChildId: null,
            CanClose: true,
            CanFloat: false);

        // Act
        var json = JsonSerializer.Serialize(properties, SerializerOptions);

        // Assert
        json.Should().Contain("\"title\": \"Test Tool\"");
        json.Should().Contain("\"canClose\": true");
        json.Should().Contain("\"canFloat\": false");
        json.Should().NotContain("\"proportion\"");
        json.Should().NotContain("\"orientation\"");
        json.Should().NotContain("\"alignment\"");
    }

    [Fact]
    public void DockOrientation_SerializesAsString()
    {
        // Arrange
        var properties = new DockNodeProperties(Orientation: DockOrientation.Vertical);

        // Act
        var json = JsonSerializer.Serialize(properties, SerializerOptions);

        // Assert
        json.Should().Contain("\"orientation\": \"vertical\"");
    }

    [Fact]
    public void DockAlignment_SerializesAsString()
    {
        // Arrange
        var properties = new DockNodeProperties(Alignment: DockAlignment.Bottom);

        // Act
        var json = JsonSerializer.Serialize(properties, SerializerOptions);

        // Assert
        json.Should().Contain("\"alignment\": \"bottom\"");
    }

    private static LayoutData CreateSampleLayoutData()
    {
        var metadata = new LayoutMetadata(
            LayoutMetadata.CurrentSchemaVersion,
            "Default",
            DateTime.UtcNow,
            "1.0.0");

        var leftTool = new DockNodeData(
            "left-tool",
            DockNodeType.Tool,
            new DockNodeProperties(
                Title: "Navigator",
                Alignment: DockAlignment.Left));

        var centerDoc = new DockNodeData(
            "center-doc",
            DockNodeType.Document,
            new DockNodeProperties());

        var rightTool = new DockNodeData(
            "right-tool",
            DockNodeType.Tool,
            new DockNodeProperties(
                Title: "Properties",
                Alignment: DockAlignment.Right));

        var root = new DockNodeData(
            "root",
            DockNodeType.Root,
            new DockNodeProperties(Orientation: DockOrientation.Horizontal),
            new[] { leftTool, centerDoc, rightTool });

        return new LayoutData(metadata, root);
    }
}
