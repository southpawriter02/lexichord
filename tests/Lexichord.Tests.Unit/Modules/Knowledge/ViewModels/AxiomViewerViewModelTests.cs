// =============================================================================
// File: AxiomViewerViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomViewerViewModel.
// =============================================================================
// v0.4.7i: Tests for Axiom Viewer loading, filtering, and grouping.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Lexichord.Modules.Knowledge.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Knowledge.ViewModels;

/// <summary>
/// Unit tests for <see cref="AxiomViewerViewModel"/>.
/// </summary>
public class AxiomViewerViewModelTests
{
    private readonly Mock<IAxiomStore> _axiomStoreMock;
    private readonly Mock<ISchemaRegistry> _schemaRegistryMock;
    private readonly Mock<ILogger<AxiomViewerViewModel>> _loggerMock;

    public AxiomViewerViewModelTests()
    {
        _axiomStoreMock = new Mock<IAxiomStore>();
        _schemaRegistryMock = new Mock<ISchemaRegistry>();
        _loggerMock = new Mock<ILogger<AxiomViewerViewModel>>();

        // LOGIC: Setup default behavior for GetAllAxioms
        _axiomStoreMock
            .Setup(x => x.GetAllAxioms())
            .Returns(new List<Axiom>());
    }

    private AxiomViewerViewModel CreateSut() =>
        new AxiomViewerViewModel(
            _axiomStoreMock.Object,
            _schemaRegistryMock.Object,
            _loggerMock.Object);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullAxiomStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AxiomViewerViewModel(null!, _schemaRegistryMock.Object, _loggerMock.Object));
        Assert.Equal("axiomStore", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSchemaRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AxiomViewerViewModel(_axiomStoreMock.Object, null!, _loggerMock.Object));
        Assert.Equal("schemaRegistry", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AxiomViewerViewModel(_axiomStoreMock.Object, _schemaRegistryMock.Object, null!));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.Empty(sut.Axioms);
        Assert.Empty(sut.FilteredAxioms);
        Assert.Empty(sut.AvailableTypes);
        Assert.Empty(sut.AvailableCategories);
    }

    [Fact]
    public void Constructor_InitializesDefaultFilterValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.Null(sut.SearchText);
        Assert.Null(sut.TypeFilter);
        Assert.Null(sut.SeverityFilter);
        Assert.Null(sut.CategoryFilter);
        Assert.True(sut.ShowDisabled);
        Assert.Equal(AxiomGroupByMode.TargetType, sut.GroupByMode);
    }

    #endregion

    #region LoadAxiomsAsync Tests

    [Fact]
    public async Task LoadAxiomsAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var sut = CreateSut();
        var loadingStates = new List<bool>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AxiomViewerViewModel.IsLoading))
            {
                loadingStates.Add(sut.IsLoading);
            }
        };

        // Act
        await sut.LoadAxiomsAsync();

        // Assert - Should have set IsLoading to true then false
        Assert.Contains(true, loadingStates);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task LoadAxiomsAsync_PopulatesAxiomsCollection()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();

        // Act
        await sut.LoadAxiomsAsync();

        // Assert
        Assert.Equal(3, sut.Axioms.Count);
        Assert.Equal(3, sut.TotalCount);
    }

    [Fact]
    public async Task LoadAxiomsAsync_PopulatesFilteredAxiomsWithAllByDefault()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();

        // Act
        await sut.LoadAxiomsAsync();

        // Assert
        Assert.Equal(3, sut.FilteredAxioms.Count);
        Assert.Equal(3, sut.FilteredCount);
    }

    [Fact]
    public async Task LoadAxiomsAsync_PopulatesAvailableTypes()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();

        // Act
        await sut.LoadAxiomsAsync();

        // Assert
        Assert.Equal(2, sut.AvailableTypes.Count);
        Assert.Contains("Endpoint", sut.AvailableTypes);
        Assert.Contains("Document", sut.AvailableTypes);
    }

    [Fact]
    public async Task LoadAxiomsAsync_PopulatesAvailableCategories()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();

        // Act
        await sut.LoadAxiomsAsync();

        // Assert
        Assert.Equal(2, sut.AvailableCategories.Count);
        Assert.Contains("Validation", sut.AvailableCategories);
        Assert.Contains("Schema", sut.AvailableCategories);
    }

    [Fact]
    public async Task LoadAxiomsAsync_ClearsExistingDataBeforeLoading()
    {
        // Arrange
        var axioms1 = CreateTestAxioms();
        var axioms2 = new List<Axiom> { CreateAxiom("new-axiom", "New Axiom", "NewType") };

        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms1);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();
        Assert.Equal(3, sut.Axioms.Count);

        // Act - Load different data
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms2);
        await sut.LoadAxiomsAsync();

        // Assert
        Assert.Single(sut.Axioms);
        Assert.Equal("new-axiom", sut.Axioms[0].Id);
    }

    [Fact]
    public async Task LoadAxiomsAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        // LOGIC: Cancellation is checked during iteration, so we need axioms to iterate over.
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var cts = new CancellationTokenSource();
        var sut = CreateSut();

        // LOGIC: Set up to cancel after GetAllAxioms returns but before processing completes
        _axiomStoreMock
            .Setup(x => x.GetAllAxioms())
            .Returns(() =>
            {
                cts.Cancel();
                return axioms;
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.LoadAxiomsAsync(cts.Token));
    }

    #endregion

    #region Filter Tests - SearchText

    [Fact]
    public async Task SearchText_FiltersById()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.SearchText = "axiom-001";

        // Assert
        Assert.Single(sut.FilteredAxioms);
        Assert.Equal("axiom-001", sut.FilteredAxioms[0].Id);
    }

    [Fact]
    public async Task SearchText_FiltersByName()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.SearchText = "Endpoint";

        // Assert
        Assert.Equal(2, sut.FilteredAxioms.Count);
    }

    [Fact]
    public async Task SearchText_FiltersByDescription()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.SearchText = "document";

        // Assert
        Assert.Single(sut.FilteredAxioms);
        Assert.Equal("axiom-003", sut.FilteredAxioms[0].Id);
    }

    [Fact]
    public async Task SearchText_IsCaseInsensitive()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.SearchText = "ENDPOINT";

        // Assert
        Assert.Equal(2, sut.FilteredAxioms.Count);
    }

    #endregion

    #region Filter Tests - TypeFilter

    [Fact]
    public async Task TypeFilter_FiltersByTargetType()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.TypeFilter = "Endpoint";

        // Assert
        Assert.Equal(2, sut.FilteredAxioms.Count);
        Assert.All(sut.FilteredAxioms, a => Assert.Equal("Endpoint", a.TargetType));
    }

    [Fact]
    public async Task TypeFilter_NullShowsAll()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();
        sut.TypeFilter = "Endpoint";
        Assert.Equal(2, sut.FilteredAxioms.Count);

        // Act
        sut.TypeFilter = null;

        // Assert
        Assert.Equal(3, sut.FilteredAxioms.Count);
    }

    #endregion

    #region Filter Tests - SeverityFilter

    [Fact]
    public async Task SeverityFilter_FiltersBySeverity()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.SeverityFilter = AxiomSeverity.Warning;

        // Assert
        Assert.Single(sut.FilteredAxioms);
        Assert.Equal(AxiomSeverity.Warning, sut.FilteredAxioms[0].Severity);
    }

    [Fact]
    public async Task SeverityFilter_NullShowsAll()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();
        sut.SeverityFilter = AxiomSeverity.Error;

        // Act
        sut.SeverityFilter = null;

        // Assert
        Assert.Equal(3, sut.FilteredAxioms.Count);
    }

    #endregion

    #region Filter Tests - CategoryFilter

    [Fact]
    public async Task CategoryFilter_FiltersByCategory()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act
        sut.CategoryFilter = "Schema";

        // Assert
        Assert.Single(sut.FilteredAxioms);
        Assert.Equal("Schema", sut.FilteredAxioms[0].Category);
    }

    #endregion

    #region Filter Tests - ShowDisabled

    [Fact]
    public async Task ShowDisabled_False_HidesDisabledAxioms()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();
        Assert.Equal(3, sut.FilteredAxioms.Count);

        // Act
        sut.ShowDisabled = false;

        // Assert
        Assert.Equal(2, sut.FilteredAxioms.Count);
        Assert.All(sut.FilteredAxioms, a => Assert.True(a.IsEnabled));
    }

    [Fact]
    public async Task ShowDisabled_True_ShowsAllAxioms()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();
        sut.ShowDisabled = false;
        Assert.Equal(2, sut.FilteredAxioms.Count);

        // Act
        sut.ShowDisabled = true;

        // Assert
        Assert.Equal(3, sut.FilteredAxioms.Count);
    }

    #endregion

    #region Filter Tests - Combined Filters

    [Fact]
    public async Task Filters_CombineWithAndLogic()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        // Act - Apply multiple filters
        sut.TypeFilter = "Endpoint";
        sut.SeverityFilter = AxiomSeverity.Error;

        // Assert - Should match only axiom-001 (Error severity and Endpoint type)
        Assert.Single(sut.FilteredAxioms);
        Assert.Equal("axiom-001", sut.FilteredAxioms[0].Id);
    }

    #endregion

    #region ClearFilters Tests

    [Fact]
    public async Task ClearFilters_ResetsAllFiltersToDefaults()
    {
        // Arrange
        var axioms = CreateTestAxioms();
        _axiomStoreMock.Setup(x => x.GetAllAxioms()).Returns(axioms);
        var sut = CreateSut();
        await sut.LoadAxiomsAsync();

        sut.SearchText = "test";
        sut.TypeFilter = "Endpoint";
        sut.SeverityFilter = AxiomSeverity.Error;
        sut.CategoryFilter = "Validation";
        sut.ShowDisabled = false;

        // Act
        sut.ClearFilters();

        // Assert
        Assert.Null(sut.SearchText);
        Assert.Null(sut.TypeFilter);
        Assert.Null(sut.SeverityFilter);
        Assert.Null(sut.CategoryFilter);
        Assert.True(sut.ShowDisabled);
        Assert.Equal(3, sut.FilteredAxioms.Count);
    }

    #endregion

    #region GroupByMode Tests

    [Fact]
    public void GroupByMode_ChangingRaisesPropertyChanged()
    {
        // Arrange
        var sut = CreateSut();
        var propertyChanged = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AxiomViewerViewModel.GroupByMode))
            {
                propertyChanged = true;
            }
        };

        // Act
        sut.GroupByMode = AxiomGroupByMode.Category;

        // Assert
        Assert.True(propertyChanged);
        Assert.Equal(AxiomGroupByMode.Category, sut.GroupByMode);
    }

    #endregion

    #region ApplyFilters Tests

    [Fact]
    public void ApplyFilters_WithNoFilters_ReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var axiom = CreateAxiom("test", "Test", "Endpoint");
        var vm = new AxiomItemViewModel(axiom);

        // Act
        var result = sut.ApplyFilters(vm);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ApplyFilters_DisabledAxiom_WhenShowDisabledFalse_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowDisabled = false;
        var axiom = CreateAxiom("test", "Test", "Endpoint") with { IsEnabled = false };
        var vm = new AxiomItemViewModel(axiom);

        // Act
        var result = sut.ApplyFilters(vm);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Test Helpers

    private static List<Axiom> CreateTestAxioms()
    {
        return new List<Axiom>
        {
            CreateAxiom("axiom-001", "Endpoint Required Fields", "Endpoint")
                with { Severity = AxiomSeverity.Error, Category = "Validation" },
            CreateAxiom("axiom-002", "Endpoint Pattern Validation", "Endpoint")
                with { Severity = AxiomSeverity.Warning, Category = "Validation" },
            CreateAxiom("axiom-003", "Document Type Check", "Document")
                with
                {
                    Severity = AxiomSeverity.Error,
                    Category = "Schema",
                    Description = "Validates document type",
                    IsEnabled = false
                }
        };
    }

    private static Axiom CreateAxiom(string id, string name, string targetType)
    {
        return new Axiom
        {
            Id = id,
            Name = name,
            Description = null,
            TargetType = targetType,
            TargetKind = AxiomTargetKind.Entity,
            Severity = AxiomSeverity.Error,
            Category = null,
            Tags = new List<string>(),
            IsEnabled = true,
            Rules = new List<AxiomRule>
            {
                new AxiomRule
                {
                    Property = "name",
                    Constraint = AxiomConstraintType.Required
                }
            }
        };
    }

    #endregion
}
