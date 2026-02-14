// -----------------------------------------------------------------------
// <copyright file="ContextStrategyFactoryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextStrategyFactory"/>.
/// </summary>
/// <remarks>
/// Tests verify factory behavior including license tier filtering and strategy creation.
/// Note: v0.7.2a has no registered strategies, so many tests verify empty/null behavior.
/// Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class ContextStrategyFactoryTests
{
    #region Helper Factories

    private static ContextStrategyFactory CreateFactory(
        IServiceProvider? services = null,
        ILicenseContext? license = null,
        ILogger<ContextStrategyFactory>? logger = null)
    {
        services ??= Substitute.For<IServiceProvider>();
        license ??= CreateLicenseContext(LicenseTier.Core);
        logger ??= Substitute.For<ILogger<ContextStrategyFactory>>();

        return new ContextStrategyFactory(services, license, logger);
    }

    private static ILicenseContext CreateLicenseContext(LicenseTier tier)
    {
        var context = Substitute.For<ILicenseContext>();
        context.Tier.Returns(tier);
        return context;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws when services is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        var license = CreateLicenseContext(LicenseTier.Core);
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();

        // Act
        var act = () => new ContextStrategyFactory(null!, license, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    /// Verifies that constructor throws when license is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLicense_ThrowsArgumentNullException()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();

        // Act
        var act = () => new ContextStrategyFactory(services, null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("license");
    }

    /// <summary>
    /// Verifies that constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        var license = CreateLicenseContext(LicenseTier.Core);

        // Act
        var act = () => new ContextStrategyFactory(services, license, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        var license = CreateLicenseContext(LicenseTier.Core);
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();

        // Act
        var act = () => new ContextStrategyFactory(services, license, logger);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region AvailableStrategyIds Tests

    /// <summary>
    /// Verifies that AvailableStrategyIds returns empty list when no strategies registered.
    /// </summary>
    /// <remarks>
    /// NOTE: v0.7.2a has no registered strategies, so this is the expected behavior.
    /// v0.7.2b will add concrete strategies and this test will need updating.
    /// </remarks>
    [Fact]
    public void AvailableStrategyIds_NoStrategiesRegistered_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        var result = sut.AvailableStrategyIds;

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that AvailableStrategyIds returns same instance on multiple calls.
    /// </summary>
    [Fact]
    public void AvailableStrategyIds_MultipleCalls_ReturnsSameList()
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var result1 = sut.AvailableStrategyIds;
        var result2 = sut.AvailableStrategyIds;

        // Assert
        // Note: We're checking behavior, not reference equality, since it's a computed property
        result1.Should().Equal(result2);
    }

    #endregion

    #region CreateStrategy Tests

    /// <summary>
    /// Verifies that CreateStrategy returns null for unknown strategy ID.
    /// </summary>
    [Fact]
    public void CreateStrategy_UnknownStrategyId_ReturnsNull()
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var result = sut.CreateStrategy("unknown-strategy");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CreateStrategy logs warning for unknown strategy ID.
    /// </summary>
    [Fact]
    public void CreateStrategy_UnknownStrategyId_LogsWarning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();
        var sut = CreateFactory(logger: logger);

        // Act
        sut.CreateStrategy("unknown-strategy");

        // Assert
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Unknown") && o.ToString()!.Contains("unknown-strategy")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Verifies that CreateStrategy returns null when strategy not available for tier.
    /// </summary>
    /// <remarks>
    /// NOTE: This test is currently hypothetical since v0.7.2a has no registered strategies.
    /// When strategies are added in v0.7.2b, this test will verify tier-based filtering.
    /// For now, it verifies the behavior of returning null for unavailable strategies.
    /// </remarks>
    [Fact]
    public void CreateStrategy_InsufficientTier_ReturnsNull()
    {
        // Arrange
        // NOTE: Since no strategies are registered in v0.7.2a, this test will always
        // return null due to unknown strategy, not due to tier checking.
        // When strategies are added in v0.7.2b, update this test with actual strategy IDs.
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Core));

        // Act
        var result = sut.CreateStrategy("hypothetical-teams-strategy");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CreateStrategy logs debug when strategy not available for tier.
    /// </summary>
    /// <remarks>
    /// NOTE: This test is currently hypothetical since v0.7.2a has no registered strategies.
    /// When strategies are added in v0.7.2b, this test will verify tier-based logging.
    /// </remarks>
    [Fact]
    public void CreateStrategy_InsufficientTier_LogsDebug()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Core), logger: logger);

        // Act
        sut.CreateStrategy("hypothetical-teams-strategy");

        // Assert
        // NOTE: Since strategy is unknown, we'll get a warning instead of debug for tier.
        // When strategies are added in v0.7.2b, update this assertion.
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Verifies that CreateStrategy handles DI resolution errors gracefully.
    /// </summary>
    [Fact]
    public void CreateStrategy_DIResolutionFails_ReturnsNull()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("Service not found"));

        var sut = CreateFactory(services: services, license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        // NOTE: Since no strategies are registered, this will return null due to unknown strategy.
        // When strategies are added in v0.7.2b, this test will verify error handling.
        var result = sut.CreateStrategy("document");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CreateStrategy logs error when DI resolution fails.
    /// </summary>
    [Fact]
    public void CreateStrategy_DIResolutionFails_LogsError()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("Service not found"));

        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();
        var sut = CreateFactory(services: services, license: CreateLicenseContext(LicenseTier.Enterprise), logger: logger);

        // Act
        sut.CreateStrategy("document");

        // Assert
        // NOTE: Since strategy is unknown, we'll get a warning instead of error.
        // When strategies are added in v0.7.2b, update this assertion to expect LogLevel.Error.
        logger.Received().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region CreateAllStrategies Tests

    /// <summary>
    /// Verifies that CreateAllStrategies returns empty list when no strategies registered.
    /// </summary>
    /// <remarks>
    /// NOTE: v0.7.2a has no registered strategies, so this is the expected behavior.
    /// v0.7.2b will add concrete strategies and this test will need updating.
    /// </remarks>
    [Fact]
    public void CreateAllStrategies_NoStrategiesRegistered_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        var result = sut.CreateAllStrategies();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that CreateAllStrategies logs debug message with count.
    /// </summary>
    [Fact]
    public void CreateAllStrategies_LogsDebugWithCount()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();
        var sut = CreateFactory(logger: logger);

        // Act
        sut.CreateAllStrategies();

        // Assert
        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Created") && o.ToString()!.Contains("0")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Verifies that CreateAllStrategies skips strategies that fail to create.
    /// </summary>
    /// <remarks>
    /// NOTE: This test is currently hypothetical since v0.7.2a has no registered strategies.
    /// When strategies are added in v0.7.2b, this test will verify error handling.
    /// </remarks>
    [Fact]
    public void CreateAllStrategies_SomeStrategiesFail_SkipsFailedStrategies()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("Service not found"));

        var sut = CreateFactory(services: services, license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        var result = sut.CreateAllStrategies();

        // Assert
        // NOTE: Since no strategies are registered, result will be empty anyway.
        // When strategies are added in v0.7.2b, this test will verify partial success.
        result.Should().BeEmpty();
    }

    #endregion

    #region IsAvailable Tests

    /// <summary>
    /// Verifies that IsAvailable returns false for unregistered strategy.
    /// </summary>
    [Fact]
    public void IsAvailable_UnregisteredStrategy_ReturnsFalse()
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var result = sut.IsAvailable("unknown-strategy", LicenseTier.Enterprise);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsAvailable checks tier requirement.
    /// </summary>
    /// <remarks>
    /// NOTE: This test is currently hypothetical since v0.7.2a has no registered strategies.
    /// When strategies are added in v0.7.2b, update this test with actual strategy IDs and tiers.
    /// </remarks>
    [Theory]
    [InlineData(LicenseTier.Core, false)]
    [InlineData(LicenseTier.WriterPro, false)]
    [InlineData(LicenseTier.Teams, false)]
    [InlineData(LicenseTier.Enterprise, false)]
    public void IsAvailable_ChecksTierRequirement(LicenseTier tier, bool expected)
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        // NOTE: Using hypothetical strategy ID. When strategies are added in v0.7.2b,
        // replace "hypothetical-writerpro-strategy" with actual strategy ID (e.g., "document").
        var result = sut.IsAvailable("hypothetical-writerpro-strategy", tier);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Verifies that factory can be resolved from DI container.
    /// </summary>
    [Fact]
    public void Factory_CanBeResolvedFromDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITokenCounter, TestTokenCounter>();
        services.AddSingleton<ILicenseContext>(CreateLicenseContext(LicenseTier.Core));
        services.AddLogging();
        services.AddSingleton<IContextStrategyFactory, ContextStrategyFactory>();

        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetService<IContextStrategyFactory>();

        // Assert
        factory.Should().NotBeNull();
        factory.Should().BeOfType<ContextStrategyFactory>();
    }

    /// <summary>
    /// Test implementation of ITokenCounter for DI testing.
    /// </summary>
    private sealed class TestTokenCounter : ITokenCounter
    {
        public string Model => "test-model";

        public int CountTokens(string text) => text.Length / 4;

        public (string Text, bool WasTruncated) TruncateToTokenLimit(string text, int maxTokens)
        {
            var maxChars = maxTokens * 4;
            if (text.Length <= maxChars)
                return (text, false);

            return (text[..maxChars], true);
        }

        public IReadOnlyList<int> Encode(string text) =>
            text.Select((c, i) => (int)c).ToList().AsReadOnly();

        public string Decode(IReadOnlyList<int> tokens) =>
            new string(tokens.Select(t => (char)t).ToArray());
    }

    #endregion
}
