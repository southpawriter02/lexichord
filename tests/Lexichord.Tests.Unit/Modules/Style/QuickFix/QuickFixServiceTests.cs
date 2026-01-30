using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.Services.QuickFix;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Modules.Style.QuickFix;

/// <summary>
/// Unit tests for <see cref="QuickFixService"/>.
/// </summary>
public sealed class QuickFixServiceTests : IDisposable
{
    private readonly Mock<IViolationProvider> _mockProvider = new();
    private readonly FakeLogger<QuickFixService> _logger = new();
    private readonly QuickFixService _sut;

    public QuickFixServiceTests()
    {
        _sut = new QuickFixService(_mockProvider.Object, _logger);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullViolationProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QuickFixService(null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QuickFixService(_mockProvider.Object, null!));
    }

    #endregion

    #region GetQuickFixesAtOffset Tests

    [Fact]
    public void GetQuickFixesAtOffset_NoViolations_ReturnsEmpty()
    {
        _mockProvider
            .Setup(p => p.GetViolationsAtOffset(10))
            .Returns([]);

        var result = _sut.GetQuickFixesAtOffset(10);

        Assert.Empty(result);
    }

    [Fact]
    public void GetQuickFixesAtOffset_ViolationWithoutSuggestion_ReturnsEmpty()
    {
        var violation = CreateViolation("v1", 10, 5, suggestion: null);
        _mockProvider
            .Setup(p => p.GetViolationsAtOffset(12))
            .Returns([violation]);

        var result = _sut.GetQuickFixesAtOffset(12);

        Assert.Empty(result);
    }

    [Fact]
    public void GetQuickFixesAtOffset_ViolationWithSuggestion_ReturnsFix()
    {
        var violation = CreateViolation("v1", 10, 5, suggestion: "use");
        _mockProvider
            .Setup(p => p.GetViolationsAtOffset(12))
            .Returns([violation]);

        var result = _sut.GetQuickFixesAtOffset(12);

        Assert.Single(result);
        Assert.Equal("v1", result[0].ViolationId);
        Assert.Equal("use", result[0].ReplacementText);
        Assert.Equal(10, result[0].StartOffset);
        Assert.Equal(5, result[0].Length);
    }

    [Fact]
    public void GetQuickFixesAtOffset_MultipleViolations_ReturnsAllWithSuggestions()
    {
        var v1 = CreateViolation("v1", 10, 5, suggestion: "use");
        var v2 = CreateViolation("v2", 10, 7, suggestion: null);
        var v3 = CreateViolation("v3", 10, 3, suggestion: "begin");

        _mockProvider
            .Setup(p => p.GetViolationsAtOffset(12))
            .Returns([v1, v2, v3]);

        var result = _sut.GetQuickFixesAtOffset(12);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.ViolationId == "v1");
        Assert.Contains(result, f => f.ViolationId == "v3");
    }

    [Fact]
    public void GetQuickFixesAtOffset_AfterDispose_Throws()
    {
        _sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            _sut.GetQuickFixesAtOffset(10));
    }

    #endregion

    #region GetQuickFixForViolation Tests

    [Fact]
    public void GetQuickFixForViolation_Null_ReturnsNull()
    {
        var result = _sut.GetQuickFixForViolation(null!);

        Assert.Null(result);
    }

    [Fact]
    public void GetQuickFixForViolation_NoSuggestion_ReturnsNull()
    {
        var violation = CreateViolation("v1", 10, 5, suggestion: null);

        var result = _sut.GetQuickFixForViolation(violation);

        Assert.Null(result);
    }

    [Fact]
    public void GetQuickFixForViolation_WithSuggestion_ReturnsFix()
    {
        var violation = CreateViolation("v1", 10, 5, suggestion: "use", violatingText: "utilize");

        var result = _sut.GetQuickFixForViolation(violation);

        Assert.NotNull(result);
        Assert.Equal("v1", result.ViolationId);
        Assert.Equal("use", result.ReplacementText);
        Assert.Contains("utilize", result.Title);
        Assert.Contains("use", result.Title);
    }

    [Fact]
    public void GetQuickFixForViolation_LongText_UsesShorterTitle()
    {
        var longText = "This is a very long violating text that exceeds twenty characters";
        var violation = CreateViolation("v1", 10, longText.Length, suggestion: "better", violatingText: longText);

        var result = _sut.GetQuickFixForViolation(violation);

        Assert.NotNull(result);
        Assert.StartsWith("Use '", result.Title);
        Assert.DoesNotContain(longText, result.Title);
    }

    [Fact]
    public void GetQuickFixForViolation_AfterDispose_Throws()
    {
        _sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            _sut.GetQuickFixForViolation(CreateViolation("v1", 0, 5, "fix")));
    }

    #endregion

    #region ApplyQuickFix Tests

    [Fact]
    public void ApplyQuickFix_CallsReplaceCallback()
    {
        var fix = new QuickFixAction
        {
            ViolationId = "v1",
            Title = "Test fix",
            ReplacementText = "use",
            StartOffset = 10,
            Length = 7
        };

        int? calledStart = null;
        int? calledLength = null;
        string? calledText = null;

        _sut.ApplyQuickFix(fix, (start, length, text) =>
        {
            calledStart = start;
            calledLength = length;
            calledText = text;
        });

        Assert.Equal(10, calledStart);
        Assert.Equal(7, calledLength);
        Assert.Equal("use", calledText);
    }

    [Fact]
    public void ApplyQuickFix_RaisesQuickFixAppliedEvent()
    {
        var fix = new QuickFixAction
        {
            ViolationId = "v1",
            Title = "Test fix",
            ReplacementText = "use",
            StartOffset = 10,
            Length = 7
        };

        QuickFixAppliedEventArgs? eventArgs = null;
        _sut.QuickFixApplied += (_, e) => eventArgs = e;

        _sut.ApplyQuickFix(fix, (_, _, _) => { });

        Assert.NotNull(eventArgs);
        Assert.Equal("v1", eventArgs.ViolationId);
        Assert.Equal("use", eventArgs.ReplacementText);
        Assert.Equal(10, eventArgs.Offset);
        Assert.Equal(7, eventArgs.OriginalLength);
    }

    [Fact]
    public void ApplyQuickFix_NullAction_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _sut.ApplyQuickFix(null!, (_, _, _) => { }));
    }

    [Fact]
    public void ApplyQuickFix_NullCallback_Throws()
    {
        var fix = new QuickFixAction
        {
            ViolationId = "v1",
            Title = "Test",
            ReplacementText = "x",
            StartOffset = 0,
            Length = 1
        };

        Assert.Throws<ArgumentNullException>(() =>
            _sut.ApplyQuickFix(fix, null!));
    }

    [Fact]
    public void ApplyQuickFix_AfterDispose_Throws()
    {
        _sut.Dispose();
        var fix = new QuickFixAction
        {
            ViolationId = "v1",
            Title = "Test",
            ReplacementText = "x",
            StartOffset = 0,
            Length = 1
        };

        Assert.Throws<ObjectDisposedException>(() =>
            _sut.ApplyQuickFix(fix, (_, _, _) => { }));
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

    #region Helpers

    private static AggregatedStyleViolation CreateViolation(
        string id,
        int startOffset,
        int length,
        string? suggestion,
        string violatingText = "test")
    {
        return new AggregatedStyleViolation
        {
            Id = id,
            DocumentId = "doc1",
            RuleId = "rule1",
            StartOffset = startOffset,
            Length = length,
            Line = 1,
            Column = startOffset + 1,
            EndLine = 1,
            EndColumn = startOffset + length + 1,
            ViolatingText = violatingText,
            Message = "Test message",
            Severity = ViolationSeverity.Warning,
            Category = RuleCategory.Terminology,
            Suggestion = suggestion
        };
    }

    #endregion
}
