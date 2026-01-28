using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Security;

/// <summary>
/// Unit tests for <see cref="SecretMetadata"/> computed properties.
/// </summary>
[Trait("Category", "Unit")]
public class SecretMetadataTests
{
    [Fact]
    public void Age_ReturnsCorrectDuration()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-7);
        var metadata = new SecretMetadata("test:key", createdAt, null, createdAt);

        // Act
        var age = metadata.Age;

        // Assert
        age.Should().BeCloseTo(TimeSpan.FromDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Age_ForRecentSecret_ReturnsSmallDuration()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddSeconds(-30);
        var metadata = new SecretMetadata("test:key", createdAt, null, createdAt);

        // Act
        var age = metadata.Age;

        // Assert
        age.Should().BeCloseTo(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsUnused_WhenNeverAccessed_ReturnsTrue()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var metadata = new SecretMetadata("test:key", createdAt, null, createdAt);

        // Assert
        metadata.IsUnused.Should().BeTrue();
    }

    [Fact]
    public void IsUnused_WhenAccessed_ReturnsFalse()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var metadata = new SecretMetadata("test:key", createdAt, DateTimeOffset.UtcNow, createdAt);

        // Assert
        metadata.IsUnused.Should().BeFalse();
    }

    [Fact]
    public void TimeSinceLastAccess_WhenNeverAccessed_ReturnsNull()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var metadata = new SecretMetadata("test:key", createdAt, null, createdAt);

        // Assert
        metadata.TimeSinceLastAccess.Should().BeNull();
    }

    [Fact]
    public void TimeSinceLastAccess_WhenAccessed_ReturnsCorrectDuration()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-30);
        var lastAccessed = DateTimeOffset.UtcNow.AddHours(-2);
        var metadata = new SecretMetadata("test:key", createdAt, lastAccessed, createdAt);

        // Act
        var timeSince = metadata.TimeSinceLastAccess;

        // Assert
        timeSince.Should().NotBeNull();
        timeSince!.Value.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void RecordEquality_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var metadata1 = new SecretMetadata("test:key", createdAt, null, createdAt);
        var metadata2 = new SecretMetadata("test:key", createdAt, null, createdAt);

        // Assert
        metadata1.Should().Be(metadata2);
    }

    [Fact]
    public void RecordEquality_WithDifferentKeys_ReturnsFalse()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var metadata1 = new SecretMetadata("test:key1", createdAt, null, createdAt);
        var metadata2 = new SecretMetadata("test:key2", createdAt, null, createdAt);

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void LastModifiedAt_WhenUpdated_DiffersFromCreatedAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var modifiedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var metadata = new SecretMetadata("test:key", createdAt, null, modifiedAt);

        // Assert
        metadata.CreatedAt.Should().Be(createdAt);
        metadata.LastModifiedAt.Should().Be(modifiedAt);
        metadata.LastModifiedAt.Should().BeAfter(metadata.CreatedAt);
    }
}
