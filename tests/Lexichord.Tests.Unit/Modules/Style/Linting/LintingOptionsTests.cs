using FluentAssertions;
using Lexichord.Abstractions.Contracts.Linting;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for LintingOptions.
/// </summary>
/// <remarks>
/// LOGIC: Verifies the configuration record has appropriate defaults
/// and constraints as specified in LCS-DES-023a.
/// </remarks>
public class LintingOptionsTests
{
    [Fact]
    public void DefaultConstructor_HasExpectedDefaults()
    {
        // Act
        var options = new LintingOptions();

        // Assert
        options.DebounceMilliseconds.Should().Be(300);
        options.MaxConcurrentScans.Should().Be(2);
        options.EnableProgressiveResults.Should().BeFalse();
        options.RegexTimeoutMilliseconds.Should().Be(1000);
        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public void DebounceMilliseconds_CanBeCustomized()
    {
        // Act
        var options = new LintingOptions { DebounceMilliseconds = 500 };

        // Assert
        options.DebounceMilliseconds.Should().Be(500);
    }

    [Fact]
    public void MaxConcurrentScans_CanBeCustomized()
    {
        // Act
        var options = new LintingOptions { MaxConcurrentScans = 4 };

        // Assert
        options.MaxConcurrentScans.Should().Be(4);
    }

    [Fact]
    public void EnableProgressiveResults_CanBeEnabled()
    {
        // Act
        var options = new LintingOptions { EnableProgressiveResults = true };

        // Assert
        options.EnableProgressiveResults.Should().BeTrue();
    }

    [Fact]
    public void RegexTimeoutMilliseconds_CanBeCustomized()
    {
        // Act
        var options = new LintingOptions { RegexTimeoutMilliseconds = 2000 };

        // Assert
        options.RegexTimeoutMilliseconds.Should().Be(2000);
    }

    [Fact]
    public void Enabled_CanBeDisabled()
    {
        // Act
        var options = new LintingOptions { Enabled = false };

        // Assert
        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        // Arrange
        var original = new LintingOptions();

        // Act
        var modified = original with { DebounceMilliseconds = 600 };

        // Assert
        original.DebounceMilliseconds.Should().Be(300);
        modified.DebounceMilliseconds.Should().Be(600);
    }
}
