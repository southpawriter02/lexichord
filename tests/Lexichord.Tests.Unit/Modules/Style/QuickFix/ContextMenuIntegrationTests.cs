using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.Services.QuickFix;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Modules.Style.QuickFix;

/// <summary>
/// Unit tests for <see cref="ContextMenuIntegration"/>.
/// </summary>
public sealed class ContextMenuIntegrationTests : IDisposable
{
    private readonly Mock<IQuickFixService> _mockQuickFixService = new();
    private readonly FakeLogger<ContextMenuIntegration> _logger = new();
    private readonly ContextMenuIntegration _sut;

    public ContextMenuIntegrationTests()
    {
        _sut = new ContextMenuIntegration(_mockQuickFixService.Object, _logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullQuickFixService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ContextMenuIntegration(null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ContextMenuIntegration(_mockQuickFixService.Object, null!));
    }

    #endregion

    #region BuildContextMenu Tests

    [Fact]
    public void BuildContextMenu_NoFixes_ReturnsNull()
    {
        _mockQuickFixService
            .Setup(s => s.GetQuickFixesAtOffset(10))
            .Returns([]);

        var result = _sut.BuildContextMenu(10);

        Assert.Null(result);
    }

    [Fact]
    public void BuildContextMenu_SingleFix_ReturnsMenuWithItem()
    {
        var fix = new QuickFixAction
        {
            ViolationId = "v1",
            Title = "Replace 'utilize' with 'use'",
            ReplacementText = "use",
            StartOffset = 10,
            Length = 7
        };

        _mockQuickFixService
            .Setup(s => s.GetQuickFixesAtOffset(12))
            .Returns([fix]);

        var result = _sut.BuildContextMenu(12);

        Assert.NotNull(result);
        Assert.Single(result.Items);
    }

    [Fact]
    public void BuildContextMenu_MultipleFixes_ReturnsMenuWithAllItems()
    {
        var fixes = new List<QuickFixAction>
        {
            new()
            {
                ViolationId = "v1",
                Title = "Fix 1",
                ReplacementText = "a",
                StartOffset = 10,
                Length = 5
            },
            new()
            {
                ViolationId = "v2",
                Title = "Fix 2",
                ReplacementText = "b",
                StartOffset = 10,
                Length = 5
            }
        };

        _mockQuickFixService
            .Setup(s => s.GetQuickFixesAtOffset(12))
            .Returns(fixes);

        var result = _sut.BuildContextMenu(12);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void BuildContextMenu_AfterDispose_Throws()
    {
        _sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            _sut.BuildContextMenu(10));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _sut.Dispose();
        var exception = Record.Exception(() => _sut.Dispose());

        Assert.Null(exception);
    }

    #endregion
}
