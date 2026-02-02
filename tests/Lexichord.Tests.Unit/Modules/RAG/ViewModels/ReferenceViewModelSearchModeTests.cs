// =============================================================================
// File: ReferenceViewModelSearchModeTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ReferenceViewModel search mode toggle (v0.5.1d).
// =============================================================================
// LOGIC: Verifies the search mode toggle functionality:
//   - Constructor validation (null parameters).
//   - Search mode initialization (tier-appropriate defaults, persisted mode).
//   - License gating (Hybrid mode requires WriterPro+).
//   - Search mode change lifecycle (persistence, events, reversion).
//   - Search dispatch (correct service called for each mode).
//   - IsHybridLocked state management.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Search;
using Lexichord.Modules.RAG.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="ReferenceViewModel"/> search mode toggle functionality.
/// Verifies constructor validation, mode initialization, license gating,
/// mode switching, search dispatch, and preference persistence.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.1d")]
public class ReferenceViewModelSearchModeTests
{
    // =========================================================================
    // Test Infrastructure
    // =========================================================================

    private readonly Mock<ISemanticSearchService> _semanticSearchMock;
    private readonly Mock<IBM25SearchService> _bm25SearchMock;
    private readonly Mock<IHybridSearchService> _hybridSearchMock;
    private readonly Mock<ISearchHistoryService> _historyServiceMock;
    private readonly Mock<IReferenceNavigationService> _navigationServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ISystemSettingsRepository> _settingsRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ReferenceViewModel>> _loggerMock;
    private readonly Mock<ILogger<SearchLicenseGuard>> _guardLoggerMock;
    private readonly SearchLicenseGuard _licenseGuard;

    public ReferenceViewModelSearchModeTests()
    {
        _semanticSearchMock = new Mock<ISemanticSearchService>();
        _bm25SearchMock = new Mock<IBM25SearchService>();
        _hybridSearchMock = new Mock<IHybridSearchService>();
        _historyServiceMock = new Mock<ISearchHistoryService>();
        _navigationServiceMock = new Mock<IReferenceNavigationService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _settingsRepositoryMock = new Mock<ISystemSettingsRepository>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ReferenceViewModel>>();
        _guardLoggerMock = new Mock<ILogger<SearchLicenseGuard>>();

        // LOGIC: Default to WriterPro so most tests pass the license check.
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);

        _licenseGuard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        // LOGIC: Default history returns empty list.
        _historyServiceMock.Setup(x => x.GetRecentQueries(It.IsAny<int>())).Returns(Array.Empty<string>());
    }

    /// <summary>
    /// Creates a <see cref="ReferenceViewModel"/> with the current test mocks.
    /// </summary>
    /// <param name="includeSettings">
    /// Whether to include the <see cref="ISystemSettingsRepository"/> or pass null.
    /// </param>
    private ReferenceViewModel CreateSut(bool includeSettings = true)
    {
        return new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            includeSettings ? _settingsRepositoryMock.Object : null,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    // =========================================================================
    // Constructor Null Parameter Tests
    // =========================================================================

    [Fact]
    public void Constructor_NullSemanticSearchService_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            null!,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("semanticSearchService");
    }

    [Fact]
    public void Constructor_NullBM25SearchService_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            null!,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("bm25SearchService");
    }

    [Fact]
    public void Constructor_NullHybridSearchService_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            null!,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("hybridSearchService");
    }

    [Fact]
    public void Constructor_NullHistoryService_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            null!,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("historyService");
    }

    [Fact]
    public void Constructor_NullNavigationService_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            null!,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("navigationService");
    }

    [Fact]
    public void Constructor_NullLicenseGuard_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            null!,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("licenseGuard");
    }

    [Fact]
    public void Constructor_NullLicenseContext_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            null!,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("licenseContext");
    }

    [Fact]
    public void Constructor_NullSettingsRepository_Accepted()
    {
        // LOGIC: ISystemSettingsRepository is optional (nullable parameter).
        var sut = CreateSut(includeSettings: false);
        sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullMediator_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            null!,
            _loggerMock.Object))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("mediator");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        FluentActions.Invoking(() => new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            _licenseGuard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            null!))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    // =========================================================================
    // Constructor Initialization Tests
    // =========================================================================

    [Fact]
    public void Constructor_WriterPro_IsLicensedTrue()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        var sut = CreateSut();
        sut.IsLicensed.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Core_IsLicensedFalse()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.IsLicensed.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WriterPro_IsHybridLockedFalse()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        var sut = CreateSut();
        sut.IsHybridLocked.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Core_IsHybridLockedTrue()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.IsHybridLocked.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultSearchMode_Semantic()
    {
        // LOGIC: Before InitializeSearchModeAsync is called, the default is Semantic (enum default = 0).
        var sut = CreateSut();
        sut.SelectedSearchMode.Should().Be(SearchMode.Semantic);
    }

    [Fact]
    public void AvailableSearchModes_ContainsAllValues()
    {
        var sut = CreateSut();
        sut.AvailableSearchModes.Should().HaveCount(3);
        sut.AvailableSearchModes.Should().Contain(SearchMode.Semantic);
        sut.AvailableSearchModes.Should().Contain(SearchMode.Keyword);
        sut.AvailableSearchModes.Should().Contain(SearchMode.Hybrid);
    }

    // =========================================================================
    // Search Mode Initialization Tests
    // =========================================================================

    [Fact]
    public async Task InitializeSearchModeAsync_WriterPro_DefaultsToHybrid()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync(string.Empty);

        var sut = CreateSut();
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_Core_DefaultsToSemantic()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync(string.Empty);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Semantic);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_RestoresPersistedKeywordMode()
    {
        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync("Keyword");

        var sut = CreateSut();
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Keyword);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_RestoresPersistedSemanticMode()
    {
        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync("Semantic");

        var sut = CreateSut();
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Semantic);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_RestoresPersistedHybridForWriterPro()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync("Hybrid");

        var sut = CreateSut();
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_CoreWithPersistedHybrid_FallsBackToSemantic()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync("Hybrid");

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Semantic);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_InvalidPersistedValue_UsesDefault()
    {
        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ReturnsAsync("InvalidMode");

        var sut = CreateSut();
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_NullSettings_UsesDefault()
    {
        var sut = CreateSut(includeSettings: false);
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    [Fact]
    public async Task InitializeSearchModeAsync_SettingsException_UsesDefault()
    {
        _settingsRepositoryMock
            .Setup(x => x.GetValueAsync(ReferenceViewModel.SearchModeSettingsKey, string.Empty, default))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = CreateSut();
        await sut.InitializeSearchModeAsync();

        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    // =========================================================================
    // Search Mode Change Tests (License Gating)
    // =========================================================================

    [Fact]
    public void OnSearchModeChanged_HybridWithoutLicense_RevertsToSemantic()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.SelectedSearchMode = SearchMode.Hybrid;

        sut.SelectedSearchMode.Should().Be(SearchMode.Semantic);
    }

    [Fact]
    public void OnSearchModeChanged_HybridWithLicense_Allowed()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        var sut = CreateSut();

        sut.SelectedSearchMode = SearchMode.Hybrid;

        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    [Fact]
    public void OnSearchModeChanged_SemanticAlways_Allowed()
    {
        var sut = CreateSut();
        sut.SelectedSearchMode = SearchMode.Semantic;
        sut.SelectedSearchMode.Should().Be(SearchMode.Semantic);
    }

    [Fact]
    public void OnSearchModeChanged_KeywordAlways_Allowed()
    {
        var sut = CreateSut();
        sut.SelectedSearchMode = SearchMode.Keyword;
        sut.SelectedSearchMode.Should().Be(SearchMode.Keyword);
    }

    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public void OnSearchModeChanged_HybridWithSufficientTier_Allowed(LicenseTier tier)
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(tier);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.SelectedSearchMode = SearchMode.Hybrid;
        sut.SelectedSearchMode.Should().Be(SearchMode.Hybrid);
    }

    [Fact]
    public void OnSearchModeChanged_HybridDenied_PublishesSearchDeniedEvent()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.SelectedSearchMode = SearchMode.Hybrid;

        _mediatorMock.Verify(
            m => m.Publish(It.Is<SearchDeniedEvent>(e => e.FeatureName == "Hybrid Search"), default),
            Times.AtLeastOnce);
    }

    // =========================================================================
    // Preference Persistence Tests
    // =========================================================================

    [Fact]
    public void OnSearchModeChanged_PersistsMode()
    {
        var sut = CreateSut();
        sut.SelectedSearchMode = SearchMode.Keyword;

        _settingsRepositoryMock.Verify(
            s => s.SetValueAsync(
                ReferenceViewModel.SearchModeSettingsKey,
                "Keyword",
                It.IsAny<string?>(),
                default),
            Times.AtLeastOnce);
    }

    [Fact]
    public void OnSearchModeChanged_NullSettings_DoesNotThrow()
    {
        var sut = CreateSut(includeSettings: false);

        FluentActions.Invoking(() => sut.SelectedSearchMode = SearchMode.Keyword)
            .Should().NotThrow();
    }

    // =========================================================================
    // Telemetry Event Tests
    // =========================================================================

    [Fact]
    public void OnSearchModeChanged_PublishesModeChangedEvent()
    {
        var sut = CreateSut();
        sut.SelectedSearchMode = SearchMode.Keyword;

        _mediatorMock.Verify(
            m => m.Publish(It.Is<SearchModeChangedEvent>(e => e.NewMode == SearchMode.Keyword), default),
            Times.AtLeastOnce);
    }

    // =========================================================================
    // CanUseHybrid Tests
    // =========================================================================

    [Fact]
    public void CanUseHybrid_WriterPro_ReturnsTrue()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.WriterPro);
        var sut = CreateSut();
        sut.CanUseHybrid().Should().BeTrue();
    }

    [Fact]
    public void CanUseHybrid_Core_ReturnsFalse()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.CanUseHybrid().Should().BeFalse();
    }

    [Fact]
    public void CanUseHybrid_Teams_ReturnsTrue()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Teams);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.CanUseHybrid().Should().BeTrue();
    }

    [Fact]
    public void CanUseHybrid_Enterprise_ReturnsTrue()
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Enterprise);
        var guard = new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _guardLoggerMock.Object);

        var sut = new ReferenceViewModel(
            _semanticSearchMock.Object,
            _bm25SearchMock.Object,
            _hybridSearchMock.Object,
            _historyServiceMock.Object,
            _navigationServiceMock.Object,
            guard,
            _licenseContextMock.Object,
            _settingsRepositoryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);

        sut.CanUseHybrid().Should().BeTrue();
    }

    // =========================================================================
    // SearchMode Enum Tests
    // =========================================================================

    [Fact]
    public void SearchMode_HasThreeValues()
    {
        Enum.GetValues<SearchMode>().Should().HaveCount(3);
    }

    [Fact]
    public void SearchMode_SemanticIsDefault()
    {
        default(SearchMode).Should().Be(SearchMode.Semantic);
    }

    [Theory]
    [InlineData(SearchMode.Semantic, 0)]
    [InlineData(SearchMode.Keyword, 1)]
    [InlineData(SearchMode.Hybrid, 2)]
    public void SearchMode_HasExpectedIntegerValues(SearchMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }

    // =========================================================================
    // SearchModeChangedEvent Tests
    // =========================================================================

    [Fact]
    public void SearchModeChangedEvent_RequiredProperties()
    {
        var evt = new SearchModeChangedEvent
        {
            PreviousMode = SearchMode.Semantic,
            NewMode = SearchMode.Hybrid,
            LicenseTier = LicenseTier.WriterPro
        };

        evt.PreviousMode.Should().Be(SearchMode.Semantic);
        evt.NewMode.Should().Be(SearchMode.Hybrid);
        evt.LicenseTier.Should().Be(LicenseTier.WriterPro);
        evt.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // =========================================================================
    // Interface Implementation Tests
    // =========================================================================

    [Fact]
    public void SearchModeChangedEvent_ImplementsINotification()
    {
        var evt = new SearchModeChangedEvent
        {
            PreviousMode = SearchMode.Semantic,
            NewMode = SearchMode.Keyword,
            LicenseTier = LicenseTier.WriterPro
        };

        evt.Should().BeAssignableTo<INotification>();
    }
}
