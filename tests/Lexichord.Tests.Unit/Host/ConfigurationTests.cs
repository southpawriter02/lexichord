using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;
using System.IO;
using System;

namespace Lexichord.Tests.Unit.Host;

public class ConfigurationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _testConfigPath;
    private readonly string _testLocalConfigPath;

    public ConfigurationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _testConfigPath = Path.Combine(_tempDir, "appsettings.test.json");
        _testLocalConfigPath = Path.Combine(_tempDir, "appsettings.Local.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Configuration_ShouldLoadFromAppSettings()
    {
        // Arrange
        var json = @"{ ""Database"": { ""ConnectionString"": ""ValueFromAppSettings"" } }";
        File.WriteAllText(_testConfigPath, json);

        var builder = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:ConnectionString"].Should().Be("ValueFromAppSettings");
    }

    [Fact]
    public void Configuration_LocalSettings_ShouldOverrideAppSettings()
    {
        // Arrange
        var jsonApp = @"{ ""Database"": { ""ConnectionString"": ""ValueFromAppSettings"" } }";
        File.WriteAllText(_testConfigPath, jsonApp);

        var jsonLocal = @"{ ""Database"": { ""ConnectionString"": ""ValueFromLocalSettings"" } }";
        File.WriteAllText(_testLocalConfigPath, jsonLocal);

        var builder = new ConfigurationBuilder()
            .SetBasePath(_tempDir)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:ConnectionString"].Should().Be("ValueFromLocalSettings");
    }

}
