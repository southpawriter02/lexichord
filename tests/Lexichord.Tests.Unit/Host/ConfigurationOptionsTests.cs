using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Unit tests for configuration builder and options pattern functionality.
/// </summary>
/// <remarks>
/// Tests verify configuration precedence, environment loading,
/// and strongly-typed options binding.
/// </remarks>
public class ConfigurationOptionsTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigurationOptionsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Configuration_LoadsJsonFile()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        File.WriteAllText(jsonPath, """
            {
              "Lexichord": {
                "ApplicationName": "TestApp",
                "DebugMode": true
              }
            }
            """);

        // Act
        var config = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.json")
            .Build();

        // Assert
        config["Lexichord:ApplicationName"].Should().Be("TestApp");
        config.GetValue<bool>("Lexichord:DebugMode").Should().BeTrue();
    }

    [Fact]
    public void Configuration_EnvironmentOverridesBase()
    {
        // Arrange
        var basePath = Path.Combine(_tempDir, "appsettings.json");
        var devPath = Path.Combine(_tempDir, "appsettings.Development.json");

        File.WriteAllText(basePath, """
            {
              "Lexichord": { "DebugMode": false }
            }
            """);
        File.WriteAllText(devPath, """
            {
              "Lexichord": { "DebugMode": true }
            }
            """);

        // Act
        var config = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json")
            .Build();

        // Assert
        config.GetValue<bool>("Lexichord:DebugMode").Should().BeTrue();
    }

    [Fact]
    public void Configuration_CliOverridesJson()
    {
        // Arrange
        var jsonPath = Path.Combine(_tempDir, "appsettings.json");
        File.WriteAllText(jsonPath, """
            {
              "Lexichord": { "DebugMode": false }
            }
            """);

        var args = new[] { "--debug-mode", "true" };

        // Act
        var config = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.json")
            .AddCommandLine(args, new Dictionary<string, string>
            {
                { "--debug-mode", "Lexichord:DebugMode" }
            })
            .Build();

        // Assert
        config.GetValue<bool>("Lexichord:DebugMode").Should().BeTrue();
    }

    [Fact]
    public void Configuration_EnvironmentVariablesWithPrefix()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LEXICHORD_DEBUGMODE", "true");

        try
        {
            // Act
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "LEXICHORD_")
                .Build();

            // Assert
            config.GetValue<bool>("DEBUGMODE").Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("LEXICHORD_DEBUGMODE", null);
        }
    }

    [Fact]
    public void LexichordOptions_BindsFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Lexichord:ApplicationName", "TestApp" },
                { "Lexichord:Environment", "Testing" },
                { "Lexichord:DebugMode", "true" }
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<LexichordOptions>(config.GetSection("Lexichord"));
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<LexichordOptions>>();

        // Assert
        options.Value.ApplicationName.Should().Be("TestApp");
        options.Value.Environment.Should().Be("Testing");
        options.Value.DebugMode.Should().BeTrue();
    }

    [Fact]
    public void DebugOptions_DefaultValues()
    {
        // Arrange
        var options = new DebugOptions();

        // Assert
        options.ShowDevTools.Should().BeFalse();
        options.EnablePerformanceLogging.Should().BeFalse();
        options.SimulatedNetworkDelayMs.Should().Be(0);
    }

    [Fact]
    public void FeatureFlagOptions_DefaultValues()
    {
        // Arrange
        var options = new FeatureFlagOptions();

        // Assert
        options.EnableExperimentalFeatures.Should().BeFalse();
    }

    [Fact]
    public void LexichordOptions_GetResolvedDataPath_UsesDefault()
    {
        // Arrange
        var options = new LexichordOptions { DataPath = null };

        // Act
        var path = options.GetResolvedDataPath();

        // Assert
        path.Should().Contain("Lexichord");
        path.Should().NotContain("null");
    }

    [Fact]
    public void LexichordOptions_GetResolvedDataPath_UsesConfigured()
    {
        // Arrange
        var customPath = "/custom/path/data";
        var options = new LexichordOptions { DataPath = customPath };

        // Act
        var path = options.GetResolvedDataPath();

        // Assert
        path.Should().Be(customPath);
    }

    [Fact]
    public void LexichordOptions_DefaultValues()
    {
        // Arrange
        var options = new LexichordOptions();

        // Assert
        options.ApplicationName.Should().Be("Lexichord");
        options.Environment.Should().Be("Production");
        options.DataPath.Should().BeNull();
        options.DebugMode.Should().BeFalse();
    }
}
