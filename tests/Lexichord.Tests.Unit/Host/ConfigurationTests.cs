using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;
using System.IO;
using System;

namespace Lexichord.Tests.Unit.Host;

public class ConfigurationTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly string _testLocalConfigPath;

    public ConfigurationTests()
    {
        _testConfigPath = "appsettings.test.json";
        _testLocalConfigPath = "appsettings.Local.json";
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath)) File.Delete(_testConfigPath);
        if (File.Exists(_testLocalConfigPath)) File.Delete(_testLocalConfigPath);
    }

    [Fact]
    public void Configuration_ShouldLoadFromAppSettings()
    {
        // Arrange
        var json = @"{ ""Database"": { ""ConnectionString"": ""ValueFromAppSettings"" } }";
        File.WriteAllText(_testConfigPath, json);

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(_testConfigPath, optional: true)
            .AddJsonFile(_testLocalConfigPath, optional: true)
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
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(_testConfigPath, optional: true)
            .AddJsonFile(_testLocalConfigPath, optional: true)
            .AddEnvironmentVariables();

        // Act
        var config = builder.Build();

        // Assert
        config["Database:ConnectionString"].Should().Be("ValueFromLocalSettings");
    }

    [Fact]
    public void Configuration_EnvironmentVariables_ShouldOverrideAll()
    {
        // Arrange
        var jsonApp = @"{ ""Database"": { ""ConnectionString"": ""ValueFromAppSettings"" } }";
        File.WriteAllText(_testConfigPath, jsonApp);

        var jsonLocal = @"{ ""Database"": { ""ConnectionString"": ""ValueFromLocalSettings"" } }";
        File.WriteAllText(_testLocalConfigPath, jsonLocal);

        Environment.SetEnvironmentVariable("Database__ConnectionString", "ValueFromEnvVar");

        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(_testConfigPath, optional: true)
                .AddJsonFile(_testLocalConfigPath, optional: true)
                .AddEnvironmentVariables();

            // Act
            var config = builder.Build();

            // Assert
            config["Database:ConnectionString"].Should().Be("ValueFromEnvVar");
        }
        finally
        {
            Environment.SetEnvironmentVariable("Database__ConnectionString", null);
        }
    }
}
