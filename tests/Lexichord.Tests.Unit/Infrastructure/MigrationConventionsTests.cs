using Lexichord.Infrastructure.Migrations;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for migration naming conventions.
/// </summary>
public class MigrationConventionsTests
{
    [Fact]
    public void IndexName_SingleColumn_FormatsCorrectly()
    {
        // Act
        var result = LexichordMigration.IndexName("Users", "Email");

        // Assert
        result.Should().Be("IX_Users_Email");
    }

    [Fact]
    public void IndexName_MultipleColumns_FormatsCorrectly()
    {
        // Act
        var result = LexichordMigration.IndexName("Documents", "UserId", "CreatedAt");

        // Assert
        result.Should().Be("IX_Documents_UserId_CreatedAt");
    }

    [Fact]
    public void ForeignKeyName_FormatsCorrectly()
    {
        // Act
        var result = LexichordMigration.ForeignKeyName("Documents", "Users");

        // Assert
        result.Should().Be("FK_Documents_Users");
    }

    [Fact]
    public void MigrationConventions_HasCorrectTimestampType()
    {
        // Assert
        MigrationConventions.TimestampType.Should().Be("TIMESTAMPTZ");
    }

    [Fact]
    public void MigrationConventions_HasCorrectUuidType()
    {
        // Assert
        MigrationConventions.UuidType.Should().Be("UUID");
    }

    [Fact]
    public void MigrationConventions_HasCorrectUuidDefault()
    {
        // Assert
        MigrationConventions.UuidDefault.Should().Be("gen_random_uuid()");
    }

    [Fact]
    public void MigrationConventions_HasCorrectSchemaName()
    {
        // Assert
        MigrationConventions.SchemaName.Should().Be("public");
    }
}
