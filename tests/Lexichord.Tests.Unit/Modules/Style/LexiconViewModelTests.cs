using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Modules.Style.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for LexiconViewModel (v0.2.5a).
/// </summary>
[Trait("Category", "Unit")]
public class LexiconViewModelTests
{
    private readonly Mock<ITerminologyService> _terminologyServiceMock = new();
    private readonly Mock<ILicenseContext> _licenseContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<LexiconViewModel>> _loggerMock = new();

    private LexiconViewModel CreateViewModel()
    {
        return new LexiconViewModel(
            _terminologyServiceMock.Object,
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static StyleTerm CreateTerm(
        string term = "Test",
        string? replacement = "Replacement",
        string category = "General",
        string severity = "Suggestion",
        bool isActive = true)
    {
        return new StyleTerm
        {
            Id = Guid.NewGuid(),
            Term = term,
            Replacement = replacement,
            Category = category,
            Severity = severity,
            IsActive = isActive
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesEmptyFilteredTerms()
    {
        var viewModel = CreateViewModel();

        viewModel.FilteredTerms.Should().BeEmpty();
        viewModel.TotalCount.Should().Be(0);
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Constructor_SelectedTermIsNull()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedTerm.Should().BeNull();
    }

    #endregion

    #region LoadTermsAsync Tests

    [Fact]
    public async Task LoadTermsAsync_PopulatesFilteredTerms()
    {
        // Arrange
        var terms = new[]
        {
            CreateTerm("term1"),
            CreateTerm("term2"),
            CreateTerm("term3")
        };
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(terms);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadTermsAsync();

        // Assert
        viewModel.FilteredTerms.Should().HaveCount(3);
        viewModel.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task LoadTermsAsync_AppliesDefaultSort_BySeverityThenCategory()
    {
        // Arrange - Create terms with different severities and categories
        var terms = new[]
        {
            CreateTerm("info-term", category: "Zulu", severity: "Info"),
            CreateTerm("error-term", category: "Alpha", severity: "Error"),
            CreateTerm("warning-term", category: "Beta", severity: "Warning"),
            CreateTerm("suggestion-term", category: "Charlie", severity: "Suggestion"),
            CreateTerm("error2-term", category: "Delta", severity: "Error")
        };
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(terms);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadTermsAsync();

        // Assert - Should be ordered by severity (Error=0, Warning=1, Suggestion=2, Info=3)
        // Then by category alphabetically within same severity
        viewModel.FilteredTerms.Should().HaveCount(5);
        viewModel.FilteredTerms[0].Severity.Should().Be("Error");
        viewModel.FilteredTerms[0].Category.Should().Be("Alpha");
        viewModel.FilteredTerms[1].Severity.Should().Be("Error");
        viewModel.FilteredTerms[1].Category.Should().Be("Delta");
        viewModel.FilteredTerms[2].Severity.Should().Be("Warning");
        viewModel.FilteredTerms[3].Severity.Should().Be("Suggestion");
        viewModel.FilteredTerms[4].Severity.Should().Be("Info");
    }

    [Fact]
    public async Task LoadTermsAsync_SetsIsLoadingDuringLoad()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IEnumerable<StyleTerm>>();
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();
        viewModel.IsLoading.Should().BeFalse();

        // Act
        var loadTask = viewModel.LoadTermsAsync();
        var wasLoadingDuringCall = viewModel.IsLoading;

        tcs.SetResult(Array.Empty<StyleTerm>());
        await loadTask;

        // Assert
        wasLoadingDuringCall.Should().BeTrue();
        viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region License Tier Tests

    [Theory]
    [InlineData(LicenseTier.Core, false)]
    [InlineData(LicenseTier.WriterPro, true)]
    [InlineData(LicenseTier.Teams, true)]
    [InlineData(LicenseTier.Enterprise, true)]
    public void CanEdit_ReturnsCorrectValue_BasedOnLicenseTier(LicenseTier tier, bool expectedCanEdit)
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(tier);
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.CanEdit.Should().Be(expectedCanEdit);
    }

    [Fact]
    public void LicenseTierName_ReturnsCurrentTierAsString()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.LicenseTierName.Should().Be("WriterPro");
    }

    #endregion

    #region StatusText Tests

    [Fact]
    public async Task StatusText_ShowsTermCountAndTier_WhenCanEdit()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm(), CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();

        // Act & Assert
        viewModel.StatusText.Should().Be("2 terms • WriterPro");
    }

    [Fact]
    public async Task StatusText_ShowsReadOnly_WhenCannotEdit()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();

        // Act & Assert
        viewModel.StatusText.Should().Be("1 terms • Core (Read-Only)");
    }

    #endregion

    #region Command CanExecute Tests

    [Fact]
    public void EditSelectedCommand_CannotExecute_WhenNoSelection()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.EditSelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task EditSelectedCommand_CannotExecute_WhenNotLicensed()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        viewModel.SelectedTerm = viewModel.FilteredTerms.First();

        // Act & Assert
        viewModel.EditSelectedCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task EditSelectedCommand_CanExecute_WhenLicensedAndHasSelection()
    {
        // Arrange
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        viewModel.SelectedTerm = viewModel.FilteredTerms.First();

        // Act & Assert
        viewModel.EditSelectedCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSelectedCommand_CallsService_WhenExecuted()
    {
        // Arrange
        var term = CreateTerm();
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { term });
        _terminologyServiceMock.Setup(x => x.DeleteAsync(term.Id, default))
            .ReturnsAsync(Result<bool>.Success(true));

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        viewModel.SelectedTerm = viewModel.FilteredTerms.First();

        // Act
        await viewModel.DeleteSelectedCommand.ExecuteAsync(null);

        // Assert
        _terminologyServiceMock.Verify(x => x.DeleteAsync(term.Id, default), Times.Once);
    }

    [Fact]
    public async Task CopyPatternCommand_CanExecute_WhenHasSelection()
    {
        // Arrange - Copy works even for non-licensed users
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        viewModel.SelectedTerm = viewModel.FilteredTerms.First();

        // Act & Assert
        viewModel.CopyPatternCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task CopyPatternCommand_RaisesCopyRequested_WhenExecuted()
    {
        // Arrange
        var term = CreateTerm("TestPattern");
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { term });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        viewModel.SelectedTerm = viewModel.FilteredTerms.First();

        string? copyRequestedText = null;
        viewModel.CopyRequested += (s, text) => copyRequestedText = text;

        // Act
        await viewModel.CopyPatternCommand.ExecuteAsync(null);

        // Assert
        copyRequestedText.Should().Be("TestPattern");
    }

    #endregion

    #region Selection Tests

    [Fact]
    public async Task SelectedTerm_RaisesPropertyChanged()
    {
        // Arrange
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();

        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LexiconViewModel.SelectedTerm))
                propertyChangedRaised = true;
        };

        // Act
        viewModel.SelectedTerm = viewModel.FilteredTerms.First();

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task Dispose_ClearsFilteredTerms()
    {
        // Arrange
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(new[] { CreateTerm(), CreateTerm() });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        viewModel.FilteredTerms.Should().HaveCount(2);

        // Act
        viewModel.Dispose();

        // Assert
        viewModel.FilteredTerms.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadTermsAsync_DoesNothing_AfterDispose()
    {
        // Arrange
        var callCount = 0;
        _terminologyServiceMock.Setup(x => x.GetAllAsync(default))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new[] { CreateTerm() };
            });

        var viewModel = CreateViewModel();
        await viewModel.LoadTermsAsync();
        callCount.Should().Be(1);

        viewModel.Dispose();

        // Act
        await viewModel.LoadTermsAsync();

        // Assert - Should not call service again
        callCount.Should().Be(1);
    }

    #endregion
}
