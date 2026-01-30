using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TermExportService.
/// </summary>
/// <remarks>
/// LOGIC: Tests verify JSON/CSV export format and filtering.
/// Version: v0.2.5d
/// </remarks>
[Trait("Category", "Unit")]
public class TermExportServiceTests
{
    private readonly Mock<ITerminologyService> _terminologyServiceMock = new();
    private readonly ILogger<TermExportService> _logger = NullLogger<TermExportService>.Instance;

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTerminologyService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TermExportService(null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TermExportService(_terminologyServiceMock.Object, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region ExportToJsonAsync Tests

    [Fact]
    public async Task ExportToJsonAsync_WithTerms_ReturnsValidJson()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "click on", Replacement = "Use select", Category = "Terminology", Severity = "warning", IsActive = true },
            new StyleTerm { Id = Guid.NewGuid(), Term = "utilize", Replacement = "Use 'use'", Category = "Clarity", Severity = "suggestion", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToJsonAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.ExportedCount.Should().Be(2);
        result.Content.Should().NotBeNullOrEmpty();

        // Verify JSON is valid
        var doc = JsonDocument.Parse(result.Content!);
        doc.RootElement.TryGetProperty("terms", out var terms).Should().BeTrue();
        terms.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ExportToJsonAsync_WithMetadata_IncludesMetadata()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "test", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToJsonAsync(new ExportOptions(IncludeMetadata: true));

        // Assert
        var doc = JsonDocument.Parse(result.Content!);
        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("version", out _).Should().BeTrue();
        metadata.TryGetProperty("exportedAt", out _).Should().BeTrue();
        metadata.TryGetProperty("termCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExportToJsonAsync_WithoutMetadata_ExcludesMetadata()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "test", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToJsonAsync(new ExportOptions(IncludeMetadata: false));

        // Assert - without metadata, the root should be an array
        var doc = JsonDocument.Parse(result.Content!);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ExportToJsonAsync_FilterByCategory_OnlyIncludesMatchingTerms()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "term1", Category = "Terminology", IsActive = true },
            new StyleTerm { Id = Guid.NewGuid(), Term = "term2", Category = "Clarity", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToJsonAsync(new ExportOptions(Categories: new[] { "Terminology" }));

        // Assert
        result.ExportedCount.Should().Be(1);
        result.Content.Should().Contain("term1");
        result.Content.Should().NotContain("term2");
    }

    [Fact]
    public async Task ExportToJsonAsync_ExcludesInactiveByDefault()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "active", IsActive = true },
            new StyleTerm { Id = Guid.NewGuid(), Term = "inactive", IsActive = false }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToJsonAsync();

        // Assert
        result.ExportedCount.Should().Be(1);
        result.Content.Should().Contain("active");
        result.Content.Should().NotContain("inactive");
    }

    [Fact]
    public async Task ExportToJsonAsync_IncludeInactive_IncludesAllTerms()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "active", IsActive = true },
            new StyleTerm { Id = Guid.NewGuid(), Term = "inactive", IsActive = false }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToJsonAsync(new ExportOptions(IncludeInactive: true));

        // Assert
        result.ExportedCount.Should().Be(2);
    }

    #endregion

    #region ExportToCsvAsync Tests

    [Fact]
    public async Task ExportToCsvAsync_WithTerms_ReturnsValidCsv()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "click on", Replacement = "Use select", Category = "Terminology", Severity = "warning", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.ExportedCount.Should().Be(1);
        result.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportToCsvAsync_HasCorrectHeaders()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "test", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        result.Content.Should().Contain("pattern,recommendation,category,severity,match_case,is_active");
    }

    [Fact]
    public async Task ExportToCsvAsync_ContainsTermData()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "click on", Replacement = "Use select", Category = "Terminology", Severity = "warning", MatchCase = false, IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        result.Content.Should().Contain("click on");
        result.Content.Should().Contain("Use select");
        result.Content.Should().Contain("Terminology");
        result.Content.Should().Contain("warning");
    }

    [Fact]
    public async Task ExportToCsvAsync_FilterByCategory_OnlyIncludesMatchingTerms()
    {
        // Arrange
        SetupTerms(new[]
        {
            new StyleTerm { Id = Guid.NewGuid(), Term = "term1", Category = "Terminology", IsActive = true },
            new StyleTerm { Id = Guid.NewGuid(), Term = "term2", Category = "Clarity", IsActive = true }
        });

        var service = CreateService();

        // Act
        var result = await service.ExportToCsvAsync(new ExportOptions(Categories: new[] { "Terminology" }));

        // Assert
        result.ExportedCount.Should().Be(1);
        result.Content.Should().Contain("term1");
        result.Content.Should().NotContain("term2");
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyRepository_ReturnsHeadersOnly()
    {
        // Arrange
        SetupTerms(Array.Empty<StyleTerm>());

        var service = CreateService();

        // Act
        var result = await service.ExportToCsvAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.ExportedCount.Should().Be(0);
        result.Content.Should().Contain("pattern,recommendation");
    }

    #endregion

    #region Helper Methods

    private TermExportService CreateService() =>
        new(_terminologyServiceMock.Object, _logger);

    private void SetupTerms(IEnumerable<StyleTerm> terms)
    {
        _terminologyServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms.ToList());
    }

    #endregion
}
