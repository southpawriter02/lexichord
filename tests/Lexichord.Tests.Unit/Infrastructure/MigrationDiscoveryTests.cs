using FluentMigrator;
using Lexichord.Infrastructure.Migrations;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for migration discovery and attributes.
/// </summary>
public class MigrationDiscoveryTests
{
    [Fact]
    public void Assembly_ContainsMigrations()
    {
        // Arrange
        var assembly = typeof(Migration_001_InitSystem).Assembly;

        // Act
        var migrations = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .ToList();

        // Assert
        migrations.Should().NotBeEmpty();
        migrations.Should().Contain(t => t.Name.Contains("001"));
    }

    [Fact]
    public void Migration_001_HasCorrectAttribute()
    {
        // Arrange
        var migrationType = typeof(Migration_001_InitSystem);

        // Act
        var attr = migrationType.GetCustomAttributes(typeof(MigrationAttribute), false)
            .OfType<MigrationAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
        attr!.Version.Should().Be(1);
    }

    [Fact]
    public void Migration_001_HasDescription()
    {
        // Arrange
        var migrationType = typeof(Migration_001_InitSystem);

        // Act
        var attr = migrationType.GetCustomAttributes(typeof(MigrationAttribute), false)
            .OfType<MigrationAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
        attr!.Description.Should().NotBeNullOrEmpty();
        attr.Description.Should().Contain("Users");
    }

    [Fact]
    public void AllMigrations_HaveUniqueVersions()
    {
        // Arrange
        var assembly = typeof(Migration_001_InitSystem).Assembly;

        // Act
        var versions = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .Select(t => t.GetCustomAttributes(typeof(MigrationAttribute), false)
                .OfType<MigrationAttribute>()
                .FirstOrDefault()?.Version)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        // Assert
        versions.Distinct().Should().HaveCount(versions.Count, 
            because: "all migrations must have unique version numbers");
    }

    [Fact]
    public void AllMigrations_InheritFromLexichordMigration()
    {
        // Arrange
        var assembly = typeof(Migration_001_InitSystem).Assembly;

        // Act
        var migrations = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .ToList();

        // Assert
        foreach (var migration in migrations)
        {
            migration.IsSubclassOf(typeof(LexichordMigration)).Should().BeTrue(
                because: $"migration {migration.Name} should inherit from LexichordMigration");
        }
    }
}
