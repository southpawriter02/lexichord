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
using Lexichord.Modules.Agents.Context.Strategies;
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
/// v0.7.2a introduced the factory with no registered strategies.
/// v0.7.2b populated registrations with 6 concrete strategies.
/// v0.7.2e added the knowledge strategy (7 total).
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
    /// Verifies that AvailableStrategyIds returns empty list for Core tier
    /// (all strategies require WriterPro or higher).
    /// </summary>
    /// <remarks>
    /// v0.7.2b/e: All 7 registered strategies require at least WriterPro,
    /// so Core tier sees no available strategies.
    /// </remarks>
    [Fact]
    public void AvailableStrategyIds_CoreTier_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Core));

        // Act
        var result = sut.AvailableStrategyIds;

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that AvailableStrategyIds returns WriterPro-tier strategies.
    /// </summary>
    /// <remarks>
    /// v0.7.2b: WriterPro tier provides access to document, selection, cursor, heading.
    /// v0.7.3c: Added surrounding-text and terminology strategies for Editor Agent.
    /// </remarks>
    [Fact]
    public void AvailableStrategyIds_WriterProTier_ReturnsSixStrategies()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.WriterPro));

        // Act
        var result = sut.AvailableStrategyIds;

        // Assert
        // LOGIC: 6 strategies = base(4: document, selection, cursor, heading) + v0.7.3c(surrounding-text, terminology)
        result.Should().HaveCount(6);
        result.Should().Contain("document");
        result.Should().Contain("selection");
        result.Should().Contain("cursor");
        result.Should().Contain("heading");
        result.Should().Contain("surrounding-text");
        result.Should().Contain("terminology");
    }

    /// <summary>
    /// Verifies that AvailableStrategyIds returns all strategies for Teams tier.
    /// </summary>
    /// <remarks>
    /// v0.7.2e: Teams tier provides access to all 7 strategies
    /// (WriterPro strategies + rag + style + knowledge + surrounding-text + terminology).
    /// </remarks>
    [Fact]
    public void AvailableStrategyIds_TeamsTier_ReturnsNineStrategies()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Teams));

        // Act
        var result = sut.AvailableStrategyIds;

        // Assert
        // LOGIC: 9 strategies = base(5) + WriterPro(rag, surrounding-text, terminology) + Teams(style, knowledge)
        result.Should().HaveCount(9);
        result.Should().Contain("rag");
        result.Should().Contain("style");
        result.Should().Contain("knowledge");
        result.Should().Contain("surrounding-text");
        result.Should().Contain("terminology");
    }

    /// <summary>
    /// Verifies that AvailableStrategyIds returns all strategies for Enterprise tier.
    /// </summary>
    [Fact]
    public void AvailableStrategyIds_EnterpriseTier_ReturnsNineStrategies()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        var result = sut.AvailableStrategyIds;

        // Assert
        // LOGIC: 9 strategies including all tiers
        result.Should().HaveCount(9);
    }

    /// <summary>
    /// Verifies that AvailableStrategyIds returns consistent results on multiple calls.
    /// </summary>
    [Fact]
    public void AvailableStrategyIds_MultipleCalls_ReturnsSameList()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Teams));

        // Act
        var result1 = sut.AvailableStrategyIds;
        var result2 = sut.AvailableStrategyIds;

        // Assert
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
    /// Verifies that CreateStrategy returns null when strategy requires higher tier.
    /// </summary>
    /// <remarks>
    /// v0.7.2b: "rag" strategy requires Teams tier. Core tier should be denied.
    /// </remarks>
    [Fact]
    public void CreateStrategy_InsufficientTier_ReturnsNull()
    {
        // Arrange — Core tier cannot access Teams-tier "rag" strategy
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Core));

        // Act
        var result = sut.CreateStrategy("rag");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CreateStrategy logs debug when strategy not available for tier.
    /// </summary>
    /// <remarks>
    /// v0.7.2b: "rag" strategy requires Teams tier. Core tier triggers debug log.
    /// </remarks>
    [Fact]
    public void CreateStrategy_InsufficientTier_LogsDebug()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ContextStrategyFactory>>();
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Core), logger: logger);

        // Act
        sut.CreateStrategy("rag");

        // Assert
        logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("rag") && o.ToString()!.Contains("not available")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Verifies that CreateStrategy handles DI resolution errors gracefully.
    /// </summary>
    /// <remarks>
    /// v0.7.2b: "document" is now a registered strategy. When DI can't resolve it,
    /// the factory returns null instead of throwing.
    /// </remarks>
    [Fact]
    public void CreateStrategy_DIResolutionFails_ReturnsNull()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("Service not found"));

        var sut = CreateFactory(services: services, license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        var result = sut.CreateStrategy("document");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CreateStrategy logs error when DI resolution fails.
    /// </summary>
    /// <remarks>
    /// v0.7.2b: "document" is now a registered strategy. DI failure triggers error log.
    /// </remarks>
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
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("document")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Verifies that WriterPro tier can access "document" but not "rag".
    /// </summary>
    [Fact]
    public void CreateStrategy_WriterProTier_CanAccessDocumentButNotRag()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("Not configured"));

        var sut = CreateFactory(services: services, license: CreateLicenseContext(LicenseTier.WriterPro));

        // Act — "document" is registered and WriterPro has access, so DI is attempted
        var docResult = sut.CreateStrategy("document");

        // Assert — returns null because DI fails, but the strategy IS registered (not an unknown warning)
        docResult.Should().BeNull();

        // Act — "rag" requires Teams tier, so WriterPro is denied before DI is attempted
        var ragResult = sut.CreateStrategy("rag");

        // Assert
        ragResult.Should().BeNull();
    }

    #endregion

    #region CreateAllStrategies Tests

    /// <summary>
    /// Verifies that CreateAllStrategies returns empty list when DI can't resolve strategies.
    /// </summary>
    /// <remarks>
    /// v0.7.2b: Strategies are registered, but DI resolution fails for all.
    /// The factory gracefully returns empty instead of throwing.
    /// </remarks>
    [Fact]
    public void CreateAllStrategies_AllStrategiesFail_ReturnsEmptyList()
    {
        // Arrange
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(_ => throw new InvalidOperationException("Service not found"));

        var sut = CreateFactory(services: services, license: CreateLicenseContext(LicenseTier.Enterprise));

        // Act
        var result = sut.CreateAllStrategies();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that CreateAllStrategies returns empty for Core tier (no strategies available).
    /// </summary>
    [Fact]
    public void CreateAllStrategies_CoreTier_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateFactory(license: CreateLicenseContext(LicenseTier.Core));

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
    /// v0.7.2e: With Enterprise tier and failing DI, all 7 strategies attempt
    /// creation but fail gracefully. Result is empty (all skipped).
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
    /// Verifies tier-based availability for the "document" strategy (requires WriterPro).
    /// </summary>
    /// <remarks>
    /// v0.7.2b: "document" strategy requires WriterPro (tier 1).
    /// Core (0) = false, WriterPro (1) = true, Teams (2) = true, Enterprise (3) = true.
    /// </remarks>
    [Theory]
    [InlineData(LicenseTier.Core, false)]
    [InlineData(LicenseTier.WriterPro, true)]
    [InlineData(LicenseTier.Teams, true)]
    [InlineData(LicenseTier.Enterprise, true)]
    public void IsAvailable_DocumentStrategy_ChecksTierRequirement(LicenseTier tier, bool expected)
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var result = sut.IsAvailable("document", tier);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies tier-based availability for the "rag" strategy (requires Teams).
    /// </summary>
    /// <remarks>
    /// v0.7.2b: "rag" strategy requires Teams (tier 2).
    /// Core (0) = false, WriterPro (1) = false, Teams (2) = true, Enterprise (3) = true.
    /// </remarks>
    [Theory]
    [InlineData(LicenseTier.Core, false)]
    [InlineData(LicenseTier.WriterPro, false)]
    [InlineData(LicenseTier.Teams, true)]
    [InlineData(LicenseTier.Enterprise, true)]
    public void IsAvailable_RagStrategy_ChecksTierRequirement(LicenseTier tier, bool expected)
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var result = sut.IsAvailable("rag", tier);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies tier-based availability for the "knowledge" strategy (requires Teams).
    /// </summary>
    /// <remarks>
    /// v0.7.2e: "knowledge" strategy requires Teams (tier 2).
    /// Core (0) = false, WriterPro (1) = false, Teams (2) = true, Enterprise (3) = true.
    /// </remarks>
    [Theory]
    [InlineData(LicenseTier.Core, false)]
    [InlineData(LicenseTier.WriterPro, false)]
    [InlineData(LicenseTier.Teams, true)]
    [InlineData(LicenseTier.Enterprise, true)]
    public void IsAvailable_KnowledgeStrategy_ChecksTierRequirement(LicenseTier tier, bool expected)
    {
        // Arrange
        var sut = CreateFactory();

        // Act
        var result = sut.IsAvailable("knowledge", tier);

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
