using System.Text;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for managing document lifecycle in the editor.
/// </summary>
/// <remarks>
/// LOGIC: Handles:
/// - Opening files from disk with encoding detection
/// - Creating new untitled documents with incrementing names
/// - Saving documents back to disk
/// - Tracking all open documents
/// </remarks>
public class EditorService : IEditorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEditorConfigurationService _configService;
    private readonly ILogger<EditorService> _logger;
    private readonly List<IManuscriptViewModel> _openDocuments = new();
    private readonly object _lock = new();
    private int _untitledCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorService"/> class.
    /// </summary>
    public EditorService(
        IServiceProvider serviceProvider,
        IEditorConfigurationService configService,
        ILogger<EditorService> logger)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IManuscriptViewModel> OpenDocumentAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // LOGIC: Check if already open
        var existing = GetDocumentByPath(filePath);
        if (existing is not null)
        {
            _logger.LogDebug("Document already open: {FilePath}", filePath);
            return existing;
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        _logger.LogDebug("Opening document: {FilePath}", filePath);

        // LOGIC: Read file with encoding detection
        var content = await File.ReadAllTextAsync(filePath);
        var encoding = DetectEncoding(filePath);
        var title = Path.GetFileName(filePath);
        var id = filePath; // Use path as unique ID

        var viewModel = CreateViewModel();
        viewModel.Initialize(id, title, filePath, content, encoding);

        lock (_lock)
        {
            _openDocuments.Add(viewModel);
        }

        _logger.LogInformation("Opened document: {Title} ({LineCount} lines)", title, viewModel.LineCount);
        return viewModel;
    }

    /// <inheritdoc/>
    public Task<IManuscriptViewModel> CreateDocumentAsync(string? title = null)
    {
        int number;
        lock (_lock)
        {
            number = ++_untitledCounter;
        }

        var documentTitle = title ?? $"Untitled-{number}";
        var id = $"untitled:{number}";

        _logger.LogDebug("Creating new document: {Title}", documentTitle);

        var viewModel = CreateViewModel();
        viewModel.Initialize(id, documentTitle, null, string.Empty, Encoding.UTF8);

        lock (_lock)
        {
            _openDocuments.Add(viewModel);
        }

        _logger.LogInformation("Created new document: {Title}", documentTitle);
        return Task.FromResult<IManuscriptViewModel>(viewModel);
    }

    /// <inheritdoc/>
    public async Task<bool> SaveDocumentAsync(IManuscriptViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrEmpty(document.FilePath))
        {
            _logger.LogWarning("Cannot save document without file path: {Title}", document.Title);
            return false;
        }

        try
        {
            _logger.LogDebug("Saving document: {FilePath}", document.FilePath);
            await File.WriteAllTextAsync(document.FilePath, document.Content, document.Encoding);
            _logger.LogInformation("Saved document: {FilePath}", document.FilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document: {FilePath}", document.FilePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<IManuscriptViewModel> GetOpenDocuments()
    {
        lock (_lock)
        {
            return _openDocuments.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc/>
    public IManuscriptViewModel? GetDocumentByPath(string filePath)
    {
        lock (_lock)
        {
            return _openDocuments.FirstOrDefault(d =>
                string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CloseDocumentAsync(IManuscriptViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // LOGIC: Check if can close (may prompt for save)
        if (!await document.CanCloseAsync())
        {
            _logger.LogDebug("Document close cancelled: {Title}", document.Title);
            return false;
        }

        lock (_lock)
        {
            _openDocuments.Remove(document);
        }

        _logger.LogInformation("Closed document: {Title}", document.Title);
        return true;
    }

    #region v0.2.6b Navigation Support

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Searches open documents by their unique ID.
    /// IDs are typically the file path for saved documents or
    /// "untitled:N" for unsaved documents.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    public IManuscriptViewModel? GetDocumentById(string documentId)
    {
        if (string.IsNullOrEmpty(documentId))
        {
            return null;
        }

        lock (_lock)
        {
            return _openDocuments.FirstOrDefault(d =>
                string.Equals(d.DocumentId, documentId, StringComparison.Ordinal));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Activates a document by ensuring it's the currently
    /// displayed tab. This is a prerequisite for navigation.
    ///
    /// Current implementation: Always returns true since we don't
    /// have direct access to the tab control here. The actual tab
    /// activation would be handled by a region manager in a full
    /// implementation. For v0.2.6b, this serves as a hook point.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    public Task<bool> ActivateDocumentAsync(IManuscriptViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // LOGIC: Verify document is in our open list
        lock (_lock)
        {
            if (!_openDocuments.Contains(document))
            {
                _logger.LogWarning("Cannot activate document not in open list: {Title}", document.Title);
                return Task.FromResult(false);
            }
        }

        _logger.LogDebug("Activated document: {Title}", document.Title);

        // LOGIC: In a full implementation, this would interact with
        // IRegionManager to bring the document tab to the front.
        // For v0.2.6b, the document is already accessible since we
        // only support CurrentFile mode in the Problems Panel.
        return Task.FromResult(true);
    }

    #endregion

    private ManuscriptViewModel CreateViewModel()
    {
        return ActivatorUtilities.CreateInstance<ManuscriptViewModel>(_serviceProvider, this);
    }

    private static Encoding DetectEncoding(string filePath)
    {
        // LOGIC: Simple BOM detection, default to UTF8
        var bom = new byte[4];
        using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        _ = file.Read(bom, 0, 4);

        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
            return Encoding.UTF8;
        if (bom[0] == 0xff && bom[1] == 0xfe)
            return Encoding.Unicode;
        if (bom[0] == 0xfe && bom[1] == 0xff)
            return Encoding.BigEndianUnicode;
        if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xfe && bom[3] == 0xff)
            return Encoding.UTF32;

        return Encoding.UTF8;
    }
}

