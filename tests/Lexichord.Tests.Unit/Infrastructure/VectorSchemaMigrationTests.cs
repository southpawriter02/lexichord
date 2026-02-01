using FluentMigrator;
using Lexichord.Infrastructure.Migrations;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for the vector schema migration metadata.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify migration attributes and inheritance without
/// requiring a database connection. They ensure the migration follows
/// Lexichord conventions and can be discovered by FluentMigrator.
/// </remarks>
public class VectorSchemaMigrationTests
{
    [Fact]
    public void Migration_HasCorrectVersion()
    {
        // Arrange
        var migrationType = typeof(Migration_003_VectorSchema);

        // Act
        var attr = migrationType.GetCustomAttributes(typeof(MigrationAttribute), false)
            .OfType<MigrationAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
        attr!.Version.Should().Be(3);
    }

    [Fact]
    public void Migration_HasDescription()
    {
        // Arrange
        var migrationType = typeof(Migration_003_VectorSchema);

        // Act
        var attr = migrationType.GetCustomAttributes(typeof(MigrationAttribute), false)
            .OfType<MigrationAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
        attr!.Description.Should().NotBeNullOrEmpty();
        attr.Description.Should().Contain("Vector");
    }

    [Fact]
    public void Migration_HasRagTag()
    {
        // Arrange
        var migrationType = typeof(Migration_003_VectorSchema);

        // Act
        var tags = migrationType.GetCustomAttributes(typeof(TagsAttribute), false)
            .OfType<TagsAttribute>()
            .SelectMany(t => t.TagNames)
            .ToList();

        // Assert
        tags.Should().Contain("RAG");
    }

    [Fact]
    public void Migration_HasVectorTag()
    {
        // Arrange
        var migrationType = typeof(Migration_003_VectorSchema);

        // Act
        var tags = migrationType.GetCustomAttributes(typeof(TagsAttribute), false)
            .OfType<TagsAttribute>()
            .SelectMany(t => t.TagNames)
            .ToList();

        // Assert
        tags.Should().Contain("Vector");
    }

    [Fact]
    public void Migration_InheritsFromLexichordMigration()
    {
        // Arrange
        var migrationType = typeof(Migration_003_VectorSchema);

        // Assert
        migrationType.IsSubclassOf(typeof(LexichordMigration)).Should().BeTrue(
            because: "all migrations should inherit from LexichordMigration for consistency");
    }

    [Fact]
    public void Migration_IsNotAbstract()
    {
        // Arrange
        var migrationType = typeof(Migration_003_VectorSchema);

        // Assert
        migrationType.IsAbstract.Should().BeFalse(
            because: "migration classes must be instantiable by FluentMigrator");
    }
}
