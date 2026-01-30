using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for ProblemsPanelViewModel (v0.2.6a).
/// </summary>
[Trait("Category", "Unit")]
public class ProblemsPanelViewModelTests : IDisposable
{
    private readonly Mock<IViolationAggregator> _violationAggregatorMock = new();
    private readonly Mock<ILogger<ProblemsPanelViewModel>> _loggerMock = new();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Test Helpers

    /// <summary>
    /// Creates the ViewModel under test.
    /// </summary>
    private ProblemsPanelViewModel CreateViewModel()
    {
        return new ProblemsPanelViewModel(
            _violationAggregatorMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Creates a test violation.
    /// </summary>
    private static AggregatedStyleViolation CreateViolation(
        string documentId = "doc-1",
        ViolationSeverity severity = ViolationSeverity.Warning,
        int line = 1)
    {
        return new AggregatedStyleViolation
        {
            Id = Guid.NewGuid().ToString(),
            DocumentId = documentId,
            RuleId = "rule-1",
            StartOffset = 0,
            Length = 5,
            Line = line,
            Column = 1,
            EndLine = line,
            EndColumn = 6,
            ViolatingText = "test",
            Message = "Test message",
            Severity = severity,
            Category = RuleCategory.Terminology
        };
    }

    /// <summary>
    /// Creates a LintingCompletedEvent for testing.
    /// </summary>
    private static LintingCompletedEvent CreateLintingEvent(
        string documentId = "doc-1",
        int violationCount = 0)
    {
        var violations = new List<StyleViolation>();
        var result = LintResult.Success(documentId, violations, TimeSpan.FromMilliseconds(50));
        return new LintingCompletedEvent(result);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.TotalCount.Should().Be(0);
        viewModel.ErrorCount.Should().Be(0);
        viewModel.WarningCount.Should().Be(0);
        viewModel.InfoCount.Should().Be(0);
        viewModel.HintCount.Should().Be(0);
        viewModel.IsLoading.Should().BeFalse();
        viewModel.SelectedItem.Should().BeNull();
        viewModel.ActiveDocumentId.Should().BeNull();
    }

    [Fact]
    public void Constructor_InitializesFourSeverityGroups()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Groups.Should().HaveCount(4);
        viewModel.Groups.Select(g => g.Severity).Should().ContainInOrder(
            ViolationSeverity.Error,
            ViolationSeverity.Warning,
            ViolationSeverity.Info,
            ViolationSeverity.Hint);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenAggregatorIsNull()
    {
        // Act
        var act = () => new ProblemsPanelViewModel(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("violationAggregator");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new ProblemsPanelViewModel(_violationAggregatorMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region ScopeMode Tests

    [Fact]
    public void ScopeMode_IsCurrentFile()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ScopeMode.Should().Be(ScopeModeType.CurrentFile);
    }

    #endregion

    #region Handle LintingCompletedEvent Tests

    [Fact]
    public async Task Handle_UpdatesActiveDocumentId()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var lintEvent = CreateLintingEvent("my-document");
        _violationAggregatorMock
            .Setup(x => x.GetViolations("my-document"))
            .Returns(new List<AggregatedStyleViolation>());

        // Act
        await viewModel.Handle(lintEvent, CancellationToken.None);

        // Assert
        viewModel.ActiveDocumentId.Should().Be("my-document");
    }

    [Fact]
    public async Task Handle_PopulatesGroupsWithViolations()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var violations = new List<AggregatedStyleViolation>
        {
            CreateViolation(severity: ViolationSeverity.Error),
            CreateViolation(severity: ViolationSeverity.Error),
            CreateViolation(severity: ViolationSeverity.Warning),
            CreateViolation(severity: ViolationSeverity.Info),
            CreateViolation(severity: ViolationSeverity.Hint)
        };

        _violationAggregatorMock
            .Setup(x => x.GetViolations("doc-1"))
            .Returns(violations);

        var lintEvent = CreateLintingEvent("doc-1");

        // Act
        await viewModel.Handle(lintEvent, CancellationToken.None);

        // Assert
        viewModel.ErrorCount.Should().Be(2);
        viewModel.WarningCount.Should().Be(1);
        viewModel.InfoCount.Should().Be(1);
        viewModel.HintCount.Should().Be(1);
        viewModel.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ClearsExistingViolations_BeforeAddingNew()
    {
        // Arrange
        var viewModel = CreateViewModel();
        
        // First event with violations
        var firstViolations = new List<AggregatedStyleViolation>
        {
            CreateViolation(severity: ViolationSeverity.Error),
            CreateViolation(severity: ViolationSeverity.Error)
        };
        _violationAggregatorMock
            .Setup(x => x.GetViolations("doc-1"))
            .Returns(firstViolations);

        await viewModel.Handle(CreateLintingEvent("doc-1"), CancellationToken.None);
        viewModel.ErrorCount.Should().Be(2);

        // Second event with different violations
        var secondViolations = new List<AggregatedStyleViolation>
        {
            CreateViolation(severity: ViolationSeverity.Warning)
        };
        _violationAggregatorMock
            .Setup(x => x.GetViolations("doc-1"))
            .Returns(secondViolations);

        // Act
        await viewModel.Handle(CreateLintingEvent("doc-1"), CancellationToken.None);

        // Assert - old errors cleared, only new warning present
        viewModel.ErrorCount.Should().Be(0);
        viewModel.WarningCount.Should().Be(1);
        viewModel.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SkipsIfCancelled()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var cancelledResult = LintResult.Cancelled("doc-1", TimeSpan.FromMilliseconds(10));
        var lintEvent = new LintingCompletedEvent(cancelledResult);

        // Act
        await viewModel.Handle(lintEvent, CancellationToken.None);

        // Assert
        viewModel.TotalCount.Should().Be(0);
        _violationAggregatorMock.Verify(x => x.GetViolations(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SkipsIfError()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var errorResult = LintResult.Failed("doc-1", "Some error", TimeSpan.FromMilliseconds(10));
        var lintEvent = new LintingCompletedEvent(errorResult);

        // Act
        await viewModel.Handle(lintEvent, CancellationToken.None);

        // Assert
        viewModel.TotalCount.Should().Be(0);
        _violationAggregatorMock.Verify(x => x.GetViolations(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SetsIsLoadingDuringProcessing()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var wasLoading = false;

        _violationAggregatorMock
            .Setup(x => x.GetViolations(It.IsAny<string>()))
            .Returns(() =>
            {
                wasLoading = viewModel.IsLoading;
                return new List<AggregatedStyleViolation>();
            });

        // Act
        await viewModel.Handle(CreateLintingEvent("doc-1"), CancellationToken.None);

        // Assert
        wasLoading.Should().BeTrue();
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RaisesPropertyChangedForCounts()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        _violationAggregatorMock
            .Setup(x => x.GetViolations("doc-1"))
            .Returns(new List<AggregatedStyleViolation>());

        // Act
        await viewModel.Handle(CreateLintingEvent("doc-1"), CancellationToken.None);

        // Assert
        changedProperties.Should().Contain(nameof(ProblemsPanelViewModel.TotalCount));
        changedProperties.Should().Contain(nameof(ProblemsPanelViewModel.ErrorCount));
        changedProperties.Should().Contain(nameof(ProblemsPanelViewModel.WarningCount));
        changedProperties.Should().Contain(nameof(ProblemsPanelViewModel.InfoCount));
        changedProperties.Should().Contain(nameof(ProblemsPanelViewModel.HintCount));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ClearsAllState()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.Dispose();

        // Assert
        viewModel.TotalCount.Should().Be(0);
        viewModel.SelectedItem.Should().BeNull();
        viewModel.ActiveDocumentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DoesNothing_AfterDispose()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Dispose();

        _violationAggregatorMock
            .Setup(x => x.GetViolations(It.IsAny<string>()))
            .Returns(new List<AggregatedStyleViolation>
            {
                CreateViolation(severity: ViolationSeverity.Error)
            });

        // Act
        await viewModel.Handle(CreateLintingEvent("doc-1"), CancellationToken.None);

        // Assert - Should not process violations after dispose
        viewModel.ErrorCount.Should().Be(0);
        _violationAggregatorMock.Verify(x => x.GetViolations(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region IProblemsPanelViewModel Interface Tests

    [Fact]
    public void ImplementsIProblemsPanelViewModel()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Should().BeAssignableTo<IProblemsPanelViewModel>();
    }

    #endregion
}
