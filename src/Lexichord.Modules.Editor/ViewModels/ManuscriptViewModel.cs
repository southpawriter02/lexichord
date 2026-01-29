using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Services;
using Lexichord.Abstractions.ViewModels;
using Lexichord.Modules.Editor.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.ViewModels;

/// <summary>
/// ViewModel for a manuscript document with editor bindings.
/// </summary>
/// <remarks>
/// LOGIC: ManuscriptViewModel extends DocumentViewModelBase with:
/// - Two-way text content binding
/// - Caret and selection tracking
/// - Editor settings bindings (font, line numbers, etc.)
/// - Commands for save, search, go-to-line
///
/// Settings are read from IEditorConfigurationService and update
/// when the service raises SettingsChanged.
/// </remarks>
public partial class ManuscriptViewModel : DocumentViewModelBase, IManuscriptViewModel
{
    private readonly IEditorService _editorService;
    private readonly IEditorConfigurationService _configService;
    private readonly ISearchService _searchService;
    private readonly IMediator _mediator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ManuscriptViewModel> _logger;

    private string _id = string.Empty;
    private string _title = "Untitled";
    private string? _filePath;
    private string _content = string.Empty;
    private Encoding _encoding = Encoding.UTF8;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManuscriptViewModel"/> class.
    /// </summary>
    public ManuscriptViewModel(
        IEditorService editorService,
        IEditorConfigurationService configService,
        ISearchService searchService,
        IMediator mediator,
        ILoggerFactory loggerFactory,
        ILogger<ManuscriptViewModel> logger,
        ISaveDialogService? saveDialogService = null)
        : base(saveDialogService)
    {
        _editorService = editorService;
        _configService = configService;
        _searchService = searchService;
        _mediator = mediator;
        _loggerFactory = loggerFactory;
        _logger = logger;

        // LOGIC: Subscribe to settings changes
        _configService.SettingsChanged += OnSettingsChanged;
    }

    #region Document Properties

    /// <inheritdoc/>
    public override string DocumentId => _id;

    /// <inheritdoc/>
    public override string Title => _title;

    /// <inheritdoc/>
    public string? FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    /// <inheritdoc/>
    public string FileExtension =>
        FilePath is not null
            ? Path.GetExtension(FilePath).ToLowerInvariant()
            : string.Empty;

    /// <inheritdoc/>
    public string Content
    {
        get => _content;
        set
        {
            if (SetProperty(ref _content, value))
            {
                MarkDirty();
                OnPropertyChanged(nameof(LineCount));
                OnPropertyChanged(nameof(CharacterCount));
                OnPropertyChanged(nameof(WordCount));
            }
        }
    }

    /// <inheritdoc/>
    public Encoding Encoding => _encoding;

    [ObservableProperty]
    private CaretPosition _caretPosition = CaretPosition.Default;

    [ObservableProperty]
    private TextSelection _selection = TextSelection.Empty;

    /// <inheritdoc/>
    public int LineCount => string.IsNullOrEmpty(Content) ? 1 : Content.Split('\n').Length;

    /// <inheritdoc/>
    public int CharacterCount => Content?.Length ?? 0;

    /// <inheritdoc/>
    public int WordCount => CountWords(Content);

    #endregion

    #region Editor Settings Bindings

    /// <summary>
    /// Gets whether to show line numbers.
    /// </summary>
    public bool ShowLineNumbers => _configService.GetSettings().ShowLineNumbers;

    /// <summary>
    /// Gets whether word wrap is enabled.
    /// </summary>
    public bool WordWrap => _configService.GetSettings().WordWrap;

    /// <summary>
    /// Gets the editor font family.
    /// </summary>
    public string FontFamily => _configService.GetSettings().FontFamily;

    /// <summary>
    /// Gets the editor font size.
    /// </summary>
    public double FontSize => _configService.GetSettings().FontSize;

    /// <summary>
    /// Gets whether to highlight the current line.
    /// </summary>
    public bool HighlightCurrentLine => _configService.GetSettings().HighlightCurrentLine;

    /// <summary>
    /// Gets whether to show whitespace characters.
    /// </summary>
    public bool ShowWhitespace => _configService.GetSettings().ShowWhitespace;

    /// <summary>
    /// Gets whether to show end-of-line markers.
    /// </summary>
    public bool ShowEndOfLine => _configService.GetSettings().ShowEndOfLine;

    /// <summary>
    /// Gets whether to use spaces instead of tabs.
    /// </summary>
    public bool UseSpacesForTabs => _configService.GetSettings().UseSpacesForTabs;

    /// <summary>
    /// Gets the indentation size.
    /// </summary>
    public int IndentSize => _configService.GetSettings().IndentSize;

    #endregion

    #region Search Integration (v0.1.3c)

    [ObservableProperty]
    private bool _isSearchVisible;

    /// <summary>
    /// Gets the search overlay view model.
    /// </summary>
    public SearchOverlayViewModel? SearchViewModel { get; private set; }

    /// <summary>
    /// Gets the search service for editor attachment.
    /// </summary>
    public ISearchService SearchService => _searchService;

    /// <summary>
    /// Command to show the search overlay.
    /// </summary>
    [RelayCommand]
    private void ShowSearch()
    {
        // LOGIC: Create SearchViewModel lazily on first show
        if (SearchViewModel is null)
        {
            var viewModelLogger = _loggerFactory.CreateLogger<SearchOverlayViewModel>();
            SearchViewModel = new SearchOverlayViewModel(
                _searchService, _mediator, viewModelLogger, DocumentId);
            OnPropertyChanged(nameof(SearchViewModel));
        }

        IsSearchVisible = true;
        _logger.LogDebug("Search overlay shown for {Title}", Title);
    }

    /// <summary>
    /// Command to hide the search overlay.
    /// </summary>
    [RelayCommand]
    private void HideSearch()
    {
        IsSearchVisible = false;
        _searchService.ClearHighlights();
    }

    /// <summary>
    /// Command to go to a specific line (stub for v0.1.3c).
    /// </summary>
    [RelayCommand]
    private void GoToLine()
    {
        _logger.LogDebug("Go to line command executed for {Title}", Title);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to save the document.
    /// </summary>
    [RelayCommand]
    private async Task Save()
    {
        await SaveAsync();
    }

    /// <summary>
    /// Override save to use EditorService.
    /// </summary>
    public override async Task<bool> SaveAsync()
    {
        _logger.LogDebug("Save command executed for {Title}", Title);

        if (FilePath is not null)
        {
            var result = await _editorService.SaveDocumentAsync(this);
            if (result)
            {
                MarkClean();
            }
            return result;
        }
        else
        {
            // TODO: Show Save As dialog (v0.1.3d)
            _logger.LogDebug("No file path, Save As required for {Title}", Title);
            return false;
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the view model with document data.
    /// </summary>
    public void Initialize(
        string id,
        string title,
        string? filePath,
        string content,
        Encoding encoding)
    {
        _id = id;
        _title = title;
        _filePath = filePath;
        _content = content;
        _encoding = encoding;

        // LOGIC: Don't mark as dirty on initial load
        MarkClean();

        NotifyAllPropertiesChanged();
        
        _logger.LogDebug("ManuscriptViewModel initialized: {Title}, {LineCount} lines", title, LineCount);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void Select(int startOffset, int length)
    {
        var endOffset = startOffset + length;
        var selectedText = startOffset >= 0 && endOffset <= Content.Length
            ? Content.Substring(startOffset, length)
            : string.Empty;
        Selection = new TextSelection(startOffset, endOffset, length, selectedText);
    }

    /// <inheritdoc/>
    public void ScrollToLine(int line)
    {
        var offset = CalculateOffsetForLine(line);
        CaretPosition = new CaretPosition(line, 1, offset);
    }

    /// <inheritdoc/>
    public void InsertText(string text)
    {
        var offset = CaretPosition.Offset;
        Content = Content.Insert(offset, text);

        var newOffset = offset + text.Length;
        var (newLine, newColumn) = CalculatePositionFromOffset(newOffset);
        CaretPosition = new CaretPosition(newLine, newColumn, newOffset);
    }

    /// <summary>
    /// Updates caret position from editor.
    /// </summary>
    public void UpdateCaretPosition(int line, int column, int offset)
    {
        CaretPosition = new CaretPosition(line, column, offset);
    }

    /// <summary>
    /// Updates selection from editor.
    /// </summary>
    public void UpdateSelection(int startOffset, int endOffset)
    {
        var length = endOffset - startOffset;
        var selectedText = length > 0 && startOffset >= 0 && endOffset <= Content.Length
            ? Content.Substring(startOffset, length)
            : string.Empty;
        Selection = new TextSelection(startOffset, endOffset, length, selectedText);
    }

    #endregion

    #region Private Methods

    private void OnSettingsChanged(object? sender, EditorSettingsChangedEventArgs e)
    {
        // LOGIC: Notify UI of settings changes
        OnPropertyChanged(nameof(ShowLineNumbers));
        OnPropertyChanged(nameof(WordWrap));
        OnPropertyChanged(nameof(FontFamily));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(HighlightCurrentLine));
        OnPropertyChanged(nameof(ShowWhitespace));
        OnPropertyChanged(nameof(ShowEndOfLine));
        OnPropertyChanged(nameof(UseSpacesForTabs));
        OnPropertyChanged(nameof(IndentSize));
    }

    private void NotifyAllPropertiesChanged()
    {
        OnPropertyChanged(nameof(DocumentId));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(Encoding));
        OnPropertyChanged(nameof(FileExtension));
        OnPropertyChanged(nameof(LineCount));
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(WordCount));
    }

    private int CalculateOffsetForLine(int line)
    {
        var lines = Content.Split('\n');
        var offset = 0;

        for (var i = 0; i < line - 1 && i < lines.Length; i++)
        {
            offset += lines[i].Length + 1; // +1 for newline
        }

        return offset;
    }

    private (int Line, int Column) CalculatePositionFromOffset(int offset)
    {
        var line = 1;
        var column = 1;
        var currentOffset = 0;

        foreach (var ch in Content)
        {
            if (currentOffset >= offset)
                break;

            if (ch == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            currentOffset++;
        }

        return (line, column);
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Regex.Matches(text, @"\b\w+\b").Count;
    }

    #endregion
}
