// -----------------------------------------------------------------------
// <copyright file="ContextPanelViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for the context panel that displays active context sources.
/// </summary>
/// <remarks>
/// <para>
/// The context panel provides visibility into the context that will be
/// injected into AI prompts, including:
/// </para>
/// <list type="bullet">
///   <item><description>Active style rules from the style guide</description></item>
///   <item><description>RAG chunks retrieved from the knowledge base</description></item>
///   <item><description>Current document and selection context</description></item>
///   <item><description>Custom user instructions</description></item>
/// </list>
/// <para>
/// Users can toggle individual context sources on/off and monitor the
/// total token budget usage.
/// </para>
/// </remarks>
/// <seealso cref="StyleRuleContextItem"/>
/// <seealso cref="RagChunkContextItem"/>
/// <seealso cref="ContextSnapshot"/>
public sealed partial class ContextPanelViewModel : ObservableObject, IDisposable
{
    #region Constants

    /// <summary>
    /// Approximate characters per token for estimation.
    /// </summary>
    private const int CharsPerToken = 4;

    /// <summary>
    /// Base token count for document metadata overhead.
    /// </summary>
    private const int DocumentMetadataTokens = 50;

    /// <summary>
    /// Event ID for ViewModel initialization.
    /// </summary>
    private const int EventIdInitialized = 6400;

    /// <summary>
    /// Event ID for token budget calculation.
    /// </summary>
    private const int EventIdTokenBudget = 6401;

    /// <summary>
    /// Event ID for panel toggle.
    /// </summary>
    private const int EventIdToggled = 6402;

    /// <summary>
    /// Event ID for refresh started.
    /// </summary>
    private const int EventIdRefreshStarted = 6403;

    /// <summary>
    /// Event ID for style rules loaded.
    /// </summary>
    private const int EventIdStyleRulesLoaded = 6404;

    /// <summary>
    /// Event ID for RAG chunks loaded.
    /// </summary>
    private const int EventIdRagChunksLoaded = 6405;

    /// <summary>
    /// Event ID for document change.
    /// </summary>
    private const int EventIdDocumentChanged = 6406;

    /// <summary>
    /// Event ID for selection change.
    /// </summary>
    private const int EventIdSelectionChanged = 6407;

    /// <summary>
    /// Event ID for RAG chunk removed.
    /// </summary>
    private const int EventIdRagChunkRemoved = 6408;

    /// <summary>
    /// Event ID for all RAG chunks cleared.
    /// </summary>
    private const int EventIdRagChunksCleared = 6409;

    /// <summary>
    /// Event ID for all sources disabled.
    /// </summary>
    private const int EventIdAllDisabled = 6410;

    /// <summary>
    /// Event ID for all sources enabled.
    /// </summary>
    private const int EventIdAllEnabled = 6411;

    /// <summary>
    /// Event ID for refresh cancelled.
    /// </summary>
    private const int EventIdRefreshCancelled = 6412;

    /// <summary>
    /// Event ID for disposal.
    /// </summary>
    private const int EventIdDisposed = 6413;

    /// <summary>
    /// Event ID for refresh complete.
    /// </summary>
    private const int EventIdRefreshComplete = 6420;

    /// <summary>
    /// Event ID for style rules toggled.
    /// </summary>
    private const int EventIdStyleRulesToggled = 6421;

    /// <summary>
    /// Event ID for RAG context toggled.
    /// </summary>
    private const int EventIdRagContextToggled = 6422;

    /// <summary>
    /// Event ID for document context toggled.
    /// </summary>
    private const int EventIdDocumentContextToggled = 6423;

    /// <summary>
    /// Event ID for custom instructions toggled.
    /// </summary>
    private const int EventIdCustomInstructionsToggled = 6424;

    /// <summary>
    /// Event ID for over budget warning.
    /// </summary>
    private const int EventIdOverBudget = 6430;

    /// <summary>
    /// Event ID for refresh error.
    /// </summary>
    private const int EventIdRefreshError = 6440;

    #endregion

    #region Fields

    private readonly IContextInjector _contextInjector;
    private readonly IStyleEngine _styleEngine;
    private readonly ISemanticSearchService _searchService;
    private readonly ILogger<ContextPanelViewModel> _logger;
    private readonly int _maxContextTokens;

    private bool _disposed;
    private CancellationTokenSource? _refreshCts;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextPanelViewModel"/> class.
    /// </summary>
    /// <param name="contextInjector">The context injector for assembling context.</param>
    /// <param name="styleEngine">The style engine for retrieving active rules.</param>
    /// <param name="searchService">The semantic search service for RAG queries.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    /// <param name="maxContextTokens">Maximum context token budget (default 4096).</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public ContextPanelViewModel(
        IContextInjector contextInjector,
        IStyleEngine styleEngine,
        ISemanticSearchService searchService,
        ILogger<ContextPanelViewModel> logger,
        int maxContextTokens = 4096)
    {
        ArgumentNullException.ThrowIfNull(contextInjector);
        ArgumentNullException.ThrowIfNull(styleEngine);
        ArgumentNullException.ThrowIfNull(searchService);
        ArgumentNullException.ThrowIfNull(logger);

        _contextInjector = contextInjector;
        _styleEngine = styleEngine;
        _searchService = searchService;
        _logger = logger;
        _maxContextTokens = maxContextTokens;

        // Initialize collections
        ActiveStyleRules = new ObservableCollection<StyleRuleContextItem>();
        RagChunks = new ObservableCollection<RagChunkContextItem>();

        // Wire up collection change notifications
        ActiveStyleRules.CollectionChanged += (_, _) => NotifyTokenPropertiesChanged();
        RagChunks.CollectionChanged += (_, _) => NotifyTokenPropertiesChanged();

        _logger.Log(
            LogLevel.Trace,
            new EventId(EventIdInitialized, nameof(ContextPanelViewModel)),
            "ContextPanelViewModel initialized");
    }

    #endregion

    #region Observable Properties - State

    /// <summary>
    /// Gets or sets a value indicating whether the panel is expanded.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    private bool _isExpanded = true;

    /// <summary>
    /// Gets or sets a value indicating whether a refresh is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the last error message from a failed operation.
    /// </summary>
    [ObservableProperty]
    private string? _lastError;

    /// <summary>
    /// Gets or sets the timestamp of the last successful refresh.
    /// </summary>
    [ObservableProperty]
    private DateTime? _lastRefreshTime;

    #endregion

    #region Observable Properties - Toggles

    /// <summary>
    /// Gets or sets a value indicating whether style rules are enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StyleRuleCount))]
    [NotifyPropertyChangedFor(nameof(StyleRulesTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    [NotifyPropertyChangedFor(nameof(EnabledSourceCount))]
    private bool _styleRulesEnabled = true;

    /// <summary>
    /// Gets or sets a value indicating whether RAG context is enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RagChunkCount))]
    [NotifyPropertyChangedFor(nameof(RagContextTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    [NotifyPropertyChangedFor(nameof(EnabledSourceCount))]
    private bool _ragContextEnabled = true;

    /// <summary>
    /// Gets or sets a value indicating whether document context is enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DocumentContextTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    [NotifyPropertyChangedFor(nameof(EnabledSourceCount))]
    private bool _documentContextEnabled = true;

    /// <summary>
    /// Gets or sets a value indicating whether custom instructions are enabled.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomInstructionsTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    [NotifyPropertyChangedFor(nameof(EnabledSourceCount))]
    private bool _customInstructionsEnabled = true;

    #endregion

    #region Observable Properties - Document Context

    /// <summary>
    /// Gets or sets the current document path.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocumentContext))]
    [NotifyPropertyChangedFor(nameof(DocumentFileName))]
    [NotifyPropertyChangedFor(nameof(DocumentContextTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    private string? _currentDocumentPath;

    /// <summary>
    /// Gets or sets the currently selected text.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(SelectionCharCount))]
    [NotifyPropertyChangedFor(nameof(DocumentContextTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    private string? _selectedText;

    /// <summary>
    /// Gets or sets the custom instructions.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomInstructionsTokens))]
    [NotifyPropertyChangedFor(nameof(EstimatedContextTokens))]
    [NotifyPropertyChangedFor(nameof(TokenBudgetPercentage))]
    [NotifyPropertyChangedFor(nameof(IsOverBudget))]
    [NotifyPropertyChangedFor(nameof(RemainingTokenBudget))]
    [NotifyPropertyChangedFor(nameof(ContextSummary))]
    private string? _customInstructions;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the collection of active style rules.
    /// </summary>
    public ObservableCollection<StyleRuleContextItem> ActiveStyleRules { get; }

    /// <summary>
    /// Gets the collection of RAG chunks.
    /// </summary>
    public ObservableCollection<RagChunkContextItem> RagChunks { get; }

    #endregion

    #region Computed Properties - Counts

    /// <summary>
    /// Gets the number of active style rules (0 if disabled).
    /// </summary>
    public int StyleRuleCount => StyleRulesEnabled ? ActiveStyleRules.Count : 0;

    /// <summary>
    /// Gets the number of RAG chunks (0 if disabled).
    /// </summary>
    public int RagChunkCount => RagContextEnabled ? RagChunks.Count : 0;

    /// <summary>
    /// Gets the character count of selected text.
    /// </summary>
    public int SelectionCharCount => SelectedText?.Length ?? 0;

    /// <summary>
    /// Gets the number of enabled context sources.
    /// </summary>
    public int EnabledSourceCount =>
        (StyleRulesEnabled ? 1 : 0) +
        (RagContextEnabled ? 1 : 0) +
        (DocumentContextEnabled ? 1 : 0) +
        (CustomInstructionsEnabled ? 1 : 0);

    #endregion

    #region Computed Properties - Token Estimates

    /// <summary>
    /// Gets the estimated tokens from style rules.
    /// </summary>
    public int StyleRulesTokens => StyleRulesEnabled
        ? ActiveStyleRules.Sum(r => r.EstimatedTokens)
        : 0;

    /// <summary>
    /// Gets the estimated tokens from RAG chunks.
    /// </summary>
    public int RagContextTokens => RagContextEnabled
        ? RagChunks.Sum(c => c.EstimatedTokens)
        : 0;

    /// <summary>
    /// Gets the estimated tokens from document context.
    /// </summary>
    public int DocumentContextTokens
    {
        get
        {
            if (!DocumentContextEnabled)
            {
                return 0;
            }

            var tokens = 0;

            // Add metadata overhead if document is set
            if (CurrentDocumentPath is not null)
            {
                tokens += DocumentMetadataTokens;
            }

            // Add selection tokens
            if (SelectedText is not null)
            {
                tokens += SelectedText.Length / CharsPerToken;
            }

            return tokens;
        }
    }

    /// <summary>
    /// Gets the estimated tokens from custom instructions.
    /// </summary>
    public int CustomInstructionsTokens => CustomInstructionsEnabled && CustomInstructions is not null
        ? CustomInstructions.Length / CharsPerToken
        : 0;

    /// <summary>
    /// Gets the total estimated context tokens.
    /// </summary>
    public int EstimatedContextTokens =>
        StyleRulesTokens + RagContextTokens + DocumentContextTokens + CustomInstructionsTokens;

    /// <summary>
    /// Gets the maximum context token budget.
    /// </summary>
    public int MaxContextTokens => _maxContextTokens;

    /// <summary>
    /// Gets the token budget usage percentage (0-100, capped at 100).
    /// </summary>
    public int TokenBudgetPercentage
    {
        get
        {
            if (_maxContextTokens == 0)
            {
                return 0;
            }

            var percentage = EstimatedContextTokens * 100 / _maxContextTokens;
            return Math.Min(percentage, 100);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the context exceeds the token budget.
    /// </summary>
    public bool IsOverBudget => EstimatedContextTokens > _maxContextTokens;

    /// <summary>
    /// Gets the remaining token budget (never negative).
    /// </summary>
    public int RemainingTokenBudget => Math.Max(0, _maxContextTokens - EstimatedContextTokens);

    #endregion

    #region Computed Properties - UI

    /// <summary>
    /// Gets a value indicating whether document context is available.
    /// </summary>
    public bool HasDocumentContext => CurrentDocumentPath is not null;

    /// <summary>
    /// Gets a value indicating whether text is selected.
    /// </summary>
    public bool HasSelection => !string.IsNullOrEmpty(SelectedText);

    /// <summary>
    /// Gets the document file name extracted from the path.
    /// </summary>
    public string? DocumentFileName => CurrentDocumentPath is not null
        ? Path.GetFileName(CurrentDocumentPath)
        : null;

    /// <summary>
    /// Gets a summary of the current context for display.
    /// </summary>
    public string ContextSummary
    {
        get
        {
            var parts = new List<string>();

            if (StyleRulesEnabled && ActiveStyleRules.Count > 0)
            {
                parts.Add($"{ActiveStyleRules.Count} rules");
            }

            if (RagContextEnabled && RagChunks.Count > 0)
            {
                parts.Add($"{RagChunks.Count} chunks");
            }

            if (DocumentContextEnabled && CurrentDocumentPath is not null)
            {
                parts.Add("doc");
            }

            if (CustomInstructionsEnabled && !string.IsNullOrEmpty(CustomInstructions))
            {
                parts.Add("custom");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "No context";
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to toggle the expanded state of the panel.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdToggled, nameof(ToggleExpanded)),
            "Context panel toggled: {State}",
            IsExpanded ? "Expanded" : "Collapsed");
    }

    /// <summary>
    /// Command to refresh all context sources.
    /// </summary>
    [RelayCommand]
    private async Task RefreshContextAsync(CancellationToken externalCt = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        LastError = null;

        // Cancel any ongoing refresh
        _refreshCts?.Cancel();
        _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = _refreshCts.Token;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdRefreshStarted, nameof(RefreshContextAsync)),
            "Refreshing context panel");

        // LOGIC: Yield to allow UI to remain responsive during refresh
        await Task.Yield();

        try
        {
            // Load style rules from the style engine
            var styleSheet = _styleEngine.GetActiveStyleSheet();
            if (styleSheet is not null)
            {
                var enabledRules = styleSheet.GetEnabledRules();
                ActiveStyleRules.Clear();

                foreach (var rule in enabledRules)
                {
                    ActiveStyleRules.Add(new StyleRuleContextItem(
                        Id: rule.Id,
                        Name: rule.Name,
                        Description: rule.Description ?? string.Empty,
                        Category: rule.Category.ToString(),
                        EstimatedTokens: (rule.Name.Length + (rule.Description?.Length ?? 0)) / CharsPerToken,
                        IsActive: true,
                        Severity: rule.DefaultSeverity));
                }

                _logger.Log(
                    LogLevel.Debug,
                    new EventId(EventIdStyleRulesLoaded, nameof(RefreshContextAsync)),
                    "Loaded {Count} style rules",
                    ActiveStyleRules.Count);
            }

            ct.ThrowIfCancellationRequested();

            LastRefreshTime = DateTime.Now;

            _logger.Log(
                LogLevel.Information,
                new EventId(EventIdRefreshComplete, nameof(RefreshContextAsync)),
                "Context refreshed: {Rules} rules (~{RuleTokens} tk), {Chunks} chunks, ~{Total}",
                ActiveStyleRules.Count,
                StyleRulesTokens,
                RagChunks.Count,
                EstimatedContextTokens);

            if (IsOverBudget)
            {
                _logger.Log(
                    LogLevel.Warning,
                    new EventId(EventIdOverBudget, nameof(RefreshContextAsync)),
                    "Context exceeds token budget: {Current} > {Max}",
                    EstimatedContextTokens,
                    _maxContextTokens);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log(
                LogLevel.Debug,
                new EventId(EventIdRefreshCancelled, nameof(RefreshContextAsync)),
                "Context refresh cancelled");
        }
        catch (Exception ex)
        {
            LastError = ex.Message;

            _logger.Log(
                LogLevel.Error,
                new EventId(EventIdRefreshError, nameof(RefreshContextAsync)),
                ex,
                "Failed to refresh context");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to remove a specific RAG chunk.
    /// </summary>
    /// <param name="chunk">The chunk to remove.</param>
    [RelayCommand]
    private void RemoveRagChunk(RagChunkContextItem? chunk)
    {
        if (chunk is null)
        {
            return;
        }

        RagChunks.Remove(chunk);

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdRagChunkRemoved, nameof(RemoveRagChunk)),
            "RAG chunk removed: {ChunkId}",
            chunk.Id);
    }

    /// <summary>
    /// Command to clear all RAG chunks.
    /// </summary>
    [RelayCommand]
    private void ClearRagChunks()
    {
        RagChunks.Clear();

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdRagChunksCleared, nameof(ClearRagChunks)),
            "All RAG chunks cleared");
    }

    /// <summary>
    /// Command to disable all context sources.
    /// </summary>
    [RelayCommand]
    private void DisableAllSources()
    {
        StyleRulesEnabled = false;
        RagContextEnabled = false;
        DocumentContextEnabled = false;
        CustomInstructionsEnabled = false;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdAllDisabled, nameof(DisableAllSources)),
            "All context sources disabled");
    }

    /// <summary>
    /// Command to enable all context sources.
    /// </summary>
    [RelayCommand]
    private void EnableAllSources()
    {
        StyleRulesEnabled = true;
        RagContextEnabled = true;
        DocumentContextEnabled = true;
        CustomInstructionsEnabled = true;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdAllEnabled, nameof(EnableAllSources)),
            "All context sources enabled");
    }

    #endregion

    #region Partial Methods for Property Changes

    partial void OnStyleRulesEnabledChanged(bool value)
    {
        _logger.Log(
            LogLevel.Information,
            new EventId(EventIdStyleRulesToggled, nameof(OnStyleRulesEnabledChanged)),
            "Style rules toggled: {Enabled}",
            value);
    }

    partial void OnRagContextEnabledChanged(bool value)
    {
        _logger.Log(
            LogLevel.Information,
            new EventId(EventIdRagContextToggled, nameof(OnRagContextEnabledChanged)),
            "RAG context toggled: {Enabled}",
            value);
    }

    partial void OnDocumentContextEnabledChanged(bool value)
    {
        _logger.Log(
            LogLevel.Information,
            new EventId(EventIdDocumentContextToggled, nameof(OnDocumentContextEnabledChanged)),
            "Document context toggled: {Enabled}",
            value);
    }

    partial void OnCustomInstructionsEnabledChanged(bool value)
    {
        _logger.Log(
            LogLevel.Information,
            new EventId(EventIdCustomInstructionsToggled, nameof(OnCustomInstructionsEnabledChanged)),
            "Custom instructions toggled: {Enabled}",
            value);
    }

    partial void OnCurrentDocumentPathChanged(string? value)
    {
        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdDocumentChanged, nameof(OnCurrentDocumentPathChanged)),
            "Active document changed: {Path}",
            value ?? "(none)");
    }

    partial void OnSelectedTextChanged(string? value)
    {
        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdSelectionChanged, nameof(OnSelectedTextChanged)),
            "Selection changed: {CharCount} characters",
            value?.Length ?? 0);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Notifies that all token-related properties have changed.
    /// </summary>
    private void NotifyTokenPropertiesChanged()
    {
        OnPropertyChanged(nameof(StyleRuleCount));
        OnPropertyChanged(nameof(RagChunkCount));
        OnPropertyChanged(nameof(StyleRulesTokens));
        OnPropertyChanged(nameof(RagContextTokens));
        OnPropertyChanged(nameof(EstimatedContextTokens));
        OnPropertyChanged(nameof(TokenBudgetPercentage));
        OnPropertyChanged(nameof(IsOverBudget));
        OnPropertyChanged(nameof(RemainingTokenBudget));
        OnPropertyChanged(nameof(ContextSummary));

        _logger.Log(
            LogLevel.Trace,
            new EventId(EventIdTokenBudget, nameof(NotifyTokenPropertiesChanged)),
            "Token budget calculation: {StyleTokens} + {RagTokens} + {DocTokens} = {Total}",
            StyleRulesTokens,
            RagContextTokens,
            DocumentContextTokens,
            EstimatedContextTokens);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;

        _disposed = true;

        _logger.Log(
            LogLevel.Debug,
            new EventId(EventIdDisposed, nameof(Dispose)),
            "ContextPanelViewModel disposed");
    }

    #endregion
}
