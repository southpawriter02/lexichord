using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the Lexicon grid view displaying style terminology.
/// </summary>
/// <remarks>
/// LOGIC: Manages the terminology grid with:
/// - Loading terms from ITerminologyService
/// - Default sorting by Severity (desc) then Category (asc)
/// - Selection handling with license-gated commands
/// - Status bar text showing term counts and license tier
///
/// Commands are gated by license tier - WriterPro required for editing.
///
/// Version: v0.2.5a
/// </remarks>
public partial class LexiconViewModel : ObservableObject, INotificationHandler<LexiconChangedEvent>, IDisposable
{
    private readonly ITerminologyService _terminologyService;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<LexiconViewModel> _logger;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the LexiconViewModel.
    /// </summary>
    public LexiconViewModel(
        ITerminologyService terminologyService,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<LexiconViewModel> logger)
    {
        _terminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        FilteredTerms = new ObservableCollection<StyleTermRowViewModel>();
    }

    #region Properties

    /// <summary>
    /// Gets the collection of terms displayed in the grid.
    /// </summary>
    public ObservableCollection<StyleTermRowViewModel> FilteredTerms { get; }

    /// <summary>
    /// Gets or sets the currently selected term.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleActiveCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyPatternCommand))]
    private StyleTermRowViewModel? _selectedTerm;

    /// <summary>
    /// Gets or sets whether the grid is currently loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets the total number of terms.
    /// </summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// Gets whether editing is allowed based on license tier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Editing requires WriterPro tier or higher.
    /// </remarks>
    public bool CanEdit => _licenseContext.GetCurrentTier() >= LicenseTier.WriterPro;

    /// <summary>
    /// Gets the current license tier name for display.
    /// </summary>
    public string LicenseTierName => _licenseContext.GetCurrentTier().ToString();

    /// <summary>
    /// Gets the status bar text showing term counts and license tier.
    /// </summary>
    public string StatusText => GenerateStatusText();

    #endregion

    #region Commands

    /// <summary>
    /// Command to edit the selected term.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task EditSelectedAsync()
    {
        if (SelectedTerm is null) return;

        _logger.LogDebug("Opening edit dialog for term {TermId}", SelectedTerm.Id);
        // TODO: v0.2.5c - Open TermEditorDialog
        await Task.CompletedTask;
    }

    /// <summary>
    /// Command to delete the selected term.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedTerm is null) return;

        _logger.LogInformation("Deleting term {TermId}: {Term}", SelectedTerm.Id, SelectedTerm.Term);
        var result = await _terminologyService.DeleteAsync(SelectedTerm.Id);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete term {TermId}: {Error}", SelectedTerm.Id, result.Error);
        }
    }

    /// <summary>
    /// Command to toggle the active state of the selected term.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task ToggleActiveAsync()
    {
        if (SelectedTerm is null) return;

        _logger.LogDebug("Toggling active state for term {TermId}", SelectedTerm.Id);

        if (SelectedTerm.IsActive)
        {
            await _terminologyService.DeleteAsync(SelectedTerm.Id);  // Soft-delete sets IsActive = false
        }
        else
        {
            await _terminologyService.ReactivateAsync(SelectedTerm.Id);
        }
    }

    /// <summary>
    /// Command to copy the pattern to clipboard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task CopyPatternAsync()
    {
        if (SelectedTerm is null) return Task.CompletedTask;

        _logger.LogDebug("Copying pattern for term {TermId}: {Pattern}", SelectedTerm.Id, SelectedTerm.Term);

        // LOGIC: Raise event for the view to handle clipboard access
        // Avalonia 11 requires TopLevel context which must be provided from the view layer
        CopyRequested?.Invoke(this, SelectedTerm.Term);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Event raised when a copy to clipboard is requested.
    /// </summary>
    public event EventHandler<string>? CopyRequested;

    /// <summary>
    /// Command to add a new term.
    /// </summary>
    [RelayCommand]
    private async Task AddTermAsync()
    {
        if (!CanEdit)
        {
            _logger.LogDebug("Add term blocked - license tier insufficient");
            return;
        }

        _logger.LogDebug("Opening add term dialog");
        // TODO: v0.2.5c - Open TermEditorDialog in create mode
        await Task.CompletedTask;
    }

    private bool CanEditSelected() => SelectedTerm is not null && CanEdit;
    private bool HasSelection() => SelectedTerm is not null;

    #endregion

    #region Data Loading

    /// <summary>
    /// Loads all terms from the terminology service.
    /// </summary>
    public async Task LoadTermsAsync()
    {
        if (_isDisposed) return;

        try
        {
            IsLoading = true;
            _logger.LogDebug("Loading terminology from service");

            var terms = await _terminologyService.GetAllAsync();
            var termList = terms.ToList();

            // LOGIC: Apply default sort - Severity (desc) then Category (asc)
            var sorted = termList
                .Select(t => new StyleTermRowViewModel(t))
                .OrderBy(t => t.SeveritySortOrder)
                .ThenBy(t => t.Category, StringComparer.OrdinalIgnoreCase)
                .ToList();

            FilteredTerms.Clear();
            foreach (var term in sorted)
            {
                FilteredTerms.Add(term);
            }

            TotalCount = FilteredTerms.Count;
            OnPropertyChanged(nameof(StatusText));

            _logger.LogInformation("Loaded {Count} terms into Lexicon grid", TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load terminology");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Handles LexiconChangedEvent to refresh the grid.
    /// </summary>
    public async Task Handle(LexiconChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received LexiconChangedEvent, refreshing grid");
        await LoadTermsAsync();
    }

    #endregion

    #region Helpers

    private string GenerateStatusText()
    {
        var tierText = CanEdit ? LicenseTierName : $"{LicenseTierName} (Read-Only)";
        return $"{TotalCount} terms • {tierText}";
    }

    partial void OnSelectedTermChanged(StyleTermRowViewModel? value)
    {
        _logger.LogTrace("Selection changed to {TermId}", value?.Id);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        FilteredTerms.Clear();
        GC.SuppressFinalize(this);
    }

    #endregion
}
