using Lexichord.Host.Layout;

namespace Lexichord.Tests.Unit.Host.Layout;

/// <summary>
/// Unit tests for <see cref="LexichordDocument"/>.
/// </summary>
public class LexichordDocumentTests
{
    [Fact]
    public void DisplayTitle_ReturnsTitle_WhenNotDirty()
    {
        // Arrange
        var document = new LexichordDocument { Title = "Test Document" };

        // Act
        var result = document.DisplayTitle;

        // Assert
        result.Should().Be("Test Document");
    }

    [Fact]
    public void DisplayTitle_ReturnsTitle_WithAsterisk_WhenDirty()
    {
        // Arrange
        var document = new LexichordDocument
        {
            Title = "Test Document",
            IsDirty = true
        };

        // Act
        var result = document.DisplayTitle;

        // Assert
        result.Should().Be("Test Document*");
    }

    [Fact]
    public async Task CanCloseAsync_ReturnsFalse_WhenDirty()
    {
        // Arrange
        var document = new LexichordDocument { IsDirty = true };

        // Act
        var result = await document.CanCloseAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanCloseAsync_ReturnsTrue_WhenNotDirty()
    {
        // Arrange
        var document = new LexichordDocument { IsDirty = false };

        // Act
        var result = await document.CanCloseAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DocumentId_ReturnsId()
    {
        // Arrange
        var document = new LexichordDocument { Id = "doc.unique.id" };

        // Act
        var result = document.DocumentId;

        // Assert
        result.Should().Be("doc.unique.id");
    }

    [Fact]
    public void IsPinned_CanBeSet()
    {
        // Arrange
        var document = new LexichordDocument();

        // Act
        document.IsPinned = true;

        // Assert
        document.IsPinned.Should().BeTrue();
    }
}
