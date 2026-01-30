using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.Services.Tooltip;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Modules.Style.Tooltip;

/// <summary>
/// Unit tests for <see cref="ViolationTooltipService"/>.
/// </summary>
public sealed class ViolationTooltipServiceTests : IDisposable
{
    private readonly Mock<IViolationProvider> _mockProvider = new();
    private readonly Mock<IViolationColorProvider> _mockColorProvider = new();
    private readonly FakeLogger<ViolationTooltipService> _logger = new();
    private readonly ViolationTooltipService _sut;

    public ViolationTooltipServiceTests()
    {
        _mockColorProvider
            .Setup(c => c.GetTooltipBorderColor(It.IsAny<ViolationSeverity>()))
            .Returns(UnderlineColor.ErrorRed);
        _mockColorProvider
            .Setup(c => c.GetSeverityIcon(It.IsAny<ViolationSeverity>()))
            .Returns("M0,0");

        _sut = new ViolationTooltipService(
            _mockProvider.Object,
            _mockColorProvider.Object,
            _logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void IsTooltipVisible_InitiallyFalse()
    {
        Assert.False(_sut.IsTooltipVisible);
    }

    [Fact]
    public void HoverDelayMs_DefaultValue()
    {
        Assert.Equal(500, _sut.HoverDelayMs);
    }

    [Fact]
    public void HoverDelayMs_CanBeChanged()
    {
        _sut.HoverDelayMs = 250;
        Assert.Equal(250, _sut.HoverDelayMs);
    }

    [Fact]
    public void HideTooltip_WhenNotVisible_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.HideTooltip());
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_NullViolationProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ViolationTooltipService(
                null!,
                _mockColorProvider.Object,
                _logger));
    }

    [Fact]
    public void Constructor_NullColorProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ViolationTooltipService(
                _mockProvider.Object,
                null!,
                _logger));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ViolationTooltipService(
                _mockProvider.Object,
                _mockColorProvider.Object,
                null!));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _sut.Dispose();
        var exception = Record.Exception(() => _sut.Dispose());
        Assert.Null(exception);
    }
}
