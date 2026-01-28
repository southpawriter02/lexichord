using Lexichord.Infrastructure.Migrations;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for VersionTableMetaData configuration.
/// </summary>
public class VersionTableMetaDataTests
{
    private readonly VersionTableMetaData _metadata;

    public VersionTableMetaDataTests()
    {
        _metadata = new VersionTableMetaData();
    }

    [Fact]
    public void TableName_ReturnsSchemaVersions()
    {
        // Assert
        _metadata.TableName.Should().Be("SchemaVersions");
    }

    [Fact]
    public void SchemaName_ReturnsPublic()
    {
        // Assert
        _metadata.SchemaName.Should().Be("public");
    }

    [Fact]
    public void ColumnName_ReturnsVersion()
    {
        // Assert
        _metadata.ColumnName.Should().Be("Version");
    }

    [Fact]
    public void DescriptionColumnName_ReturnsDescription()
    {
        // Assert
        _metadata.DescriptionColumnName.Should().Be("Description");
    }

    [Fact]
    public void AppliedOnColumnName_ReturnsAppliedOn()
    {
        // Assert
        _metadata.AppliedOnColumnName.Should().Be("AppliedOn");
    }

    [Fact]
    public void UniqueIndexName_ReturnsCorrectFormat()
    {
        // Assert
        _metadata.UniqueIndexName.Should().Be("UC_SchemaVersions_Version");
    }

    [Fact]
    public void CreateWithPrimaryKey_ReturnsTrue()
    {
        // Assert
        _metadata.CreateWithPrimaryKey.Should().BeTrue();
    }

    [Fact]
    public void OwnsSchema_ReturnsFalse()
    {
        // Assert
        _metadata.OwnsSchema.Should().BeFalse();
    }
}
