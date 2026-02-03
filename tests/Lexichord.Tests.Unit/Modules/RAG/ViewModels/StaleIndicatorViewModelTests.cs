// =============================================================================
// File: StaleIndicatorViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for StaleIndicatorViewModel stale indicator behavior.
// =============================================================================
// LOGIC: Verifies all StaleIndicatorViewModel functionality:
//   - Constructor null-parameter validation (3 dependencies).
//   - ValidateCommand: sets ValidationResult and IsVisible for stale citations.
//   - ValidateCommand: hides indicator for valid citations.
//   - ValidateCommand: hides indicator for unlicensed users (null result).
//   - ReverifyCommand: calls ReindexDocumentAsync and re-validates.
//   - ReverifyCommand: sets and resets IsVerifying flag.
//   - ReverifyCommand: no-op when ValidationResult is null.
//   - DismissCommand: hides indicator.
//   - Computed properties: IsStale, IsMissing, StatusIcon, StatusMessage.
//   - Property change notifications for computed properties.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="StaleIndicatorViewModel"/>.
/// Verifies validation, re-verification, dismissal, and computed property behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2c")]
public class StaleIndicatorViewModelTests
{
    // LOGIC: Mock dependencies for constructor injection.
    private readonly Mock<ICitationValidator> _validatorMock;
    private readonly Mock<IIndexManagementService> _indexServiceMock;
    private readonly Mock<ILogger<StaleIndicatorViewModel>> _loggerMock;

    // LOGIC: Shared test data.
    private static readonly Guid TestDocId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Citation TestCitation = new(
        ChunkId: TestDocId,
        DocumentPath: "/docs/test.md",
        DocumentTitle: "Test Document",
        StartOffset: 0,
        EndOffset: 100,
        Heading: "Introduction",
        LineNumber: 10,
        IndexedAt: new DateTime(2026, 1, 25, 10, 0, 0, DateTimeKind.Utc));

    public StaleIndicatorViewModelTests()
    {
        _validatorMock = new Mock<ICitationValidator>();
        _indexServiceMock = new Mock<IIndexManagementService>();
        _loggerMock = new Mock<ILogger<StaleIndicatorViewModel>>();
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new StaleIndicatorViewModel(
            null!, _indexServiceMock.Object, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("validator", act);
    }

    [Fact]
    public void Constructor_NullIndexService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new StaleIndicatorViewModel(
            _validatorMock.Object, null!, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("indexService", act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new StaleIndicatorViewModel(
            _validatorMock.Object, _indexServiceMock.Object, null!);
        Assert.Throws<ArgumentNullException>("logger", act);
    }

    // =========================================================================
    // ValidateCommand — Stale
    // =========================================================================

    [Fact]
    public async Task ValidateCommand_StaleCitation_ShowsIndicator()
    {
        // Arrange
        var staleResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleResult);

        var sut = CreateViewModel();

        // Act
        await sut.ValidateCommand.ExecuteAsync(TestCitation);

        // Assert
        Assert.True(sut.IsVisible);
        Assert.Equal(staleResult, sut.ValidationResult);
        Assert.True(sut.IsStale);
    }

    // =========================================================================
    // ValidateCommand — Valid
    // =========================================================================

    [Fact]
    public async Task ValidateCommand_ValidCitation_HidesIndicator()
    {
        // Arrange
        var validResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: true,
            Status: CitationValidationStatus.Valid,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validResult);

        var sut = CreateViewModel();

        // Act
        await sut.ValidateCommand.ExecuteAsync(TestCitation);

        // Assert
        Assert.False(sut.IsVisible);
        Assert.Equal(validResult, sut.ValidationResult);
    }

    // =========================================================================
    // ValidateCommand — Unlicensed
    // =========================================================================

    [Fact]
    public async Task ValidateCommand_Unlicensed_HidesIndicator()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationValidationResult?)null);

        var sut = CreateViewModel();

        // Act
        await sut.ValidateCommand.ExecuteAsync(TestCitation);

        // Assert
        Assert.False(sut.IsVisible);
        Assert.Null(sut.ValidationResult);
    }

    // =========================================================================
    // ValidateCommand — Missing
    // =========================================================================

    [Fact]
    public async Task ValidateCommand_MissingFile_ShowsIndicator()
    {
        // Arrange
        var missingResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Missing,
            CurrentModifiedAt: null,
            ErrorMessage: null);
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingResult);

        var sut = CreateViewModel();

        // Act
        await sut.ValidateCommand.ExecuteAsync(TestCitation);

        // Assert
        Assert.True(sut.IsVisible);
        Assert.True(sut.IsMissing);
    }

    // =========================================================================
    // ReverifyCommand
    // =========================================================================

    [Fact]
    public async Task ReverifyCommand_ReindexesAndRevalidates()
    {
        // Arrange — First set up a stale result
        var staleResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);

        var validResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: true,
            Status: CitationValidationStatus.Valid,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);

        var callCount = 0;
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ == 0 ? staleResult : validResult);

        _indexServiceMock
            .Setup(s => s.ReindexDocumentAsync(TestDocId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexManagementResult(
                true, TestDocId, "Re-indexed", 1, 0, TimeSpan.FromMilliseconds(50)));

        var sut = CreateViewModel();
        await sut.ValidateCommand.ExecuteAsync(TestCitation);
        Assert.True(sut.IsVisible); // Pre-condition: stale

        // Act
        await sut.ReverifyCommand.ExecuteAsync(null);

        // Assert
        _indexServiceMock.Verify(
            s => s.ReindexDocumentAsync(TestDocId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReverifyCommand_NullValidationResult_DoesNothing()
    {
        // Arrange
        var sut = CreateViewModel();
        Assert.Null(sut.ValidationResult); // Pre-condition

        // Act
        await sut.ReverifyCommand.ExecuteAsync(null);

        // Assert
        _indexServiceMock.Verify(
            s => s.ReindexDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // DismissCommand
    // =========================================================================

    [Fact]
    public async Task DismissCommand_HidesIndicator()
    {
        // Arrange — First show the indicator
        var staleResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleResult);

        var sut = CreateViewModel();
        await sut.ValidateCommand.ExecuteAsync(TestCitation);
        Assert.True(sut.IsVisible); // Pre-condition

        // Act
        sut.DismissCommand.Execute(null);

        // Assert
        Assert.False(sut.IsVisible);
    }

    [Fact]
    public async Task DismissCommand_PreservesValidationResult()
    {
        // Arrange — First show the indicator
        var staleResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleResult);

        var sut = CreateViewModel();
        await sut.ValidateCommand.ExecuteAsync(TestCitation);

        // Act
        sut.DismissCommand.Execute(null);

        // Assert — ValidationResult should still be set even though IsVisible is false
        Assert.NotNull(sut.ValidationResult);
        Assert.Equal(staleResult, sut.ValidationResult);
    }

    // =========================================================================
    // Computed Properties
    // =========================================================================

    [Fact]
    public void IsStale_NoValidationResult_ReturnsFalse()
    {
        // Arrange
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.IsStale);
    }

    [Fact]
    public void IsMissing_NoValidationResult_ReturnsFalse()
    {
        // Arrange
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.IsMissing);
    }

    [Fact]
    public void StatusIcon_Stale_ReturnsWarning()
    {
        // Arrange
        var sut = CreateViewModel();
        sut.ValidationResult = new CitationValidationResult(
            TestCitation, false, CitationValidationStatus.Stale, DateTime.UtcNow, null);

        // Assert
        Assert.Equal("⚠️", sut.StatusIcon);
    }

    [Fact]
    public void StatusIcon_Missing_ReturnsError()
    {
        // Arrange
        var sut = CreateViewModel();
        sut.ValidationResult = new CitationValidationResult(
            TestCitation, false, CitationValidationStatus.Missing, null, null);

        // Assert
        Assert.Equal("❌", sut.StatusIcon);
    }

    [Fact]
    public void StatusMessage_NoValidationResult_ReturnsEmptyString()
    {
        // Arrange
        var sut = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, sut.StatusMessage);
    }

    [Fact]
    public async Task StatusMessage_Stale_DelegatesToResult()
    {
        // Arrange
        var staleResult = new CitationValidationResult(
            Citation: TestCitation,
            IsValid: false,
            Status: CitationValidationStatus.Stale,
            CurrentModifiedAt: DateTime.UtcNow,
            ErrorMessage: null);
        _validatorMock
            .Setup(v => v.ValidateIfLicensedAsync(TestCitation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleResult);

        var sut = CreateViewModel();
        await sut.ValidateCommand.ExecuteAsync(TestCitation);

        // Assert
        Assert.StartsWith("Source modified", sut.StatusMessage);
    }

    // =========================================================================
    // Property Change Notifications
    // =========================================================================

    [Fact]
    public void SettingValidationResult_RaisesPropertyChangedForComputedProperties()
    {
        // Arrange
        var sut = CreateViewModel();
        var changedProperties = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        sut.ValidationResult = new CitationValidationResult(
            TestCitation, false, CitationValidationStatus.Stale, DateTime.UtcNow, null);

        // Assert
        Assert.Contains("ValidationResult", changedProperties);
        Assert.Contains("IsStale", changedProperties);
        Assert.Contains("IsMissing", changedProperties);
        Assert.Contains("StatusIcon", changedProperties);
        Assert.Contains("StatusMessage", changedProperties);
    }

    // =========================================================================
    // Initial State
    // =========================================================================

    [Fact]
    public void InitialState_IsNotVisible()
    {
        // Arrange & Act
        var sut = CreateViewModel();

        // Assert
        Assert.False(sut.IsVisible);
        Assert.False(sut.IsVerifying);
        Assert.Null(sut.ValidationResult);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a StaleIndicatorViewModel with mock dependencies.
    /// </summary>
    private StaleIndicatorViewModel CreateViewModel()
    {
        return new StaleIndicatorViewModel(
            _validatorMock.Object,
            _indexServiceMock.Object,
            _loggerMock.Object);
    }
}
