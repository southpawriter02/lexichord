// -----------------------------------------------------------------------
// <copyright file="PromptTemplateRepository.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Repository for loading, caching, and hot-reloading prompt templates from multiple sources.
/// </summary>
/// <remarks>
/// <para>
/// This class provides centralized management of prompt templates with:
/// </para>
/// <list type="bullet">
///   <item><description>Multi-source loading (embedded, global, user)</description></item>
///   <item><description>Priority-based template override (user > global > embedded)</description></item>
///   <item><description>Thread-safe caching via <see cref="ConcurrentDictionary{TKey,TValue}"/></description></item>
///   <item><description>Hot-reload via <see cref="IFileSystemWatcher"/> integration</description></item>
///   <item><description>License-gated features (custom templates, hot-reload)</description></item>
/// </list>
/// <para>
/// <strong>Template Loading Order:</strong>
/// </para>
/// <list type="number">
///   <item><description>Embedded templates (lowest priority, always loaded)</description></item>
///   <item><description>Global templates (medium priority, requires WriterPro+)</description></item>
///   <item><description>User templates (highest priority, requires WriterPro+)</description></item>
/// </list>
/// <para>
/// <strong>License Gating:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Embedded templates: All tiers (Core+)</description></item>
///   <item><description>Custom templates: <see cref="LicenseTier.WriterPro"/> or higher</description></item>
///   <item><description>Hot-reload: <see cref="LicenseTier.Teams"/> or higher</description></item>
/// </list>
/// <para>
/// <strong>Thread Safety:</strong>
/// This implementation is fully thread-safe. All public methods can be called concurrently.
/// Internal operations use <see cref="ConcurrentDictionary{TKey,TValue}"/> for atomic updates
/// and <see cref="SemaphoreSlim"/> for coordinated reload operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage via DI
/// var template = repository.GetTemplate("co-pilot-editor");
/// var messages = renderer.RenderMessages(template, variables);
///
/// // Listen for hot-reload events
/// repository.TemplateChanged += (sender, e) =>
/// {
///     Console.WriteLine($"Template {e.ChangeType}: {e.TemplateId}");
/// };
/// </code>
/// </example>
/// <seealso cref="IPromptTemplateRepository"/>
/// <seealso cref="PromptTemplateLoader"/>
/// <seealso cref="PromptTemplateOptions"/>
public sealed class PromptTemplateRepository : IPromptTemplateRepository, IDisposable
{
    // LOGIC: Feature codes for license gating (matching spec requirements)
    private const string FeatureCustomTemplates = "Feature.CustomTemplates";
    private const string FeatureHotReload = "Feature.HotReload";

    private readonly ConcurrentDictionary<string, TemplateEntry> _templates;
    private readonly PromptTemplateLoader _loader;
    private readonly ILicenseContext _licenseContext;
    private readonly IFileSystemWatcher? _fileWatcher;
    private readonly PromptTemplateOptions _options;
    private readonly ILogger<PromptTemplateRepository> _logger;
    private readonly SemaphoreSlim _reloadLock;
    private readonly Timer? _debounceTimer;
    private readonly ConcurrentQueue<FileSystemChangeInfo> _pendingChanges;

    private bool _disposed;
    private bool _initialized;

    /// <inheritdoc />
    public event EventHandler<TemplateChangedEventArgs>? TemplateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTemplateRepository"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="options">The repository configuration options.</param>
    /// <param name="licenseContext">The license context for feature gating.</param>
    /// <param name="fileWatcher">The file system watcher for hot-reload (optional).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/>, <paramref name="options"/>, or <paramref name="licenseContext"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The constructor initializes the repository but does not load templates.
    /// Templates are loaded lazily on first access or explicitly via <see cref="InitializeAsync"/>.
    /// </para>
    /// <para>
    /// The <paramref name="fileWatcher"/> parameter is optional. If not provided,
    /// hot-reload functionality will be disabled regardless of configuration.
    /// </para>
    /// </remarks>
    public PromptTemplateRepository(
        ILogger<PromptTemplateRepository> logger,
        IOptions<PromptTemplateOptions> options,
        ILicenseContext licenseContext,
        IFileSystemWatcher? fileWatcher = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _fileWatcher = fileWatcher;

        // LOGIC: Use case-insensitive comparer for template IDs (spec requirement)
        _templates = new ConcurrentDictionary<string, TemplateEntry>(StringComparer.OrdinalIgnoreCase);
        _loader = new PromptTemplateLoader(logger);
        _reloadLock = new SemaphoreSlim(1, 1);
        _pendingChanges = new ConcurrentQueue<FileSystemChangeInfo>();

        // LOGIC: Create debounce timer for file watcher events
        _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        _logger.LogDebug(
            "PromptTemplateRepository created with options: " +
            "EnableBuiltIn={EnableBuiltIn}, EnableHotReload={EnableHotReload}, " +
            "UserPath={UserPath}, GlobalPath={GlobalPath}",
            _options.EnableBuiltInTemplates,
            _options.EnableHotReload,
            _options.UserTemplatesPath,
            _options.GlobalTemplatesPath);
    }

    /// <summary>
    /// Initializes the repository by loading templates from all available sources.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <remarks>
    /// <para>
    /// This method loads templates in the following order:
    /// </para>
    /// <list type="number">
    ///   <item><description>Embedded templates (if enabled)</description></item>
    ///   <item><description>Global templates (if licensed)</description></item>
    ///   <item><description>User templates (if licensed)</description></item>
    /// </list>
    /// <para>
    /// After loading, if hot-reload is enabled and licensed, file system watching is started.
    /// </para>
    /// <para>
    /// This method is idempotent. Subsequent calls will not reload templates unless
    /// <see cref="ReloadTemplatesAsync"/> is called first.
    /// </para>
    /// </remarks>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            _logger.LogDebug("Repository already initialized, skipping");
            return;
        }

        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            _logger.LogInformation("Initializing PromptTemplateRepository");

            // Step 1: Load embedded templates (always available)
            if (_options.EnableBuiltInTemplates)
            {
                await LoadEmbeddedTemplatesAsync(cancellationToken);
            }

            // Step 2: Check license for custom templates
            var canLoadCustom = CanLoadCustomTemplates();
            if (canLoadCustom)
            {
                // Load global templates first (lower priority)
                await LoadGlobalTemplatesAsync(cancellationToken);

                // Load user templates (higher priority, overwrites)
                await LoadUserTemplatesAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "Custom template loading requires {RequiredTier} tier or {FeatureCode} feature. " +
                    "Using built-in templates only.",
                    LicenseTier.WriterPro,
                    FeatureCustomTemplates);
            }

            // Step 3: Setup hot-reload if enabled and licensed
            SetupFileWatcher();

            _initialized = true;

            _logger.LogInformation(
                "PromptTemplateRepository initialized with {Count} templates " +
                "(Embedded={EmbeddedCount}, Global={GlobalCount}, User={UserCount})",
                _templates.Count,
                _templates.Values.Count(e => e.Source == TemplateSource.Embedded),
                _templates.Values.Count(e => e.Source == TemplateSource.Global),
                _templates.Values.Count(e => e.Source == TemplateSource.User));
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    #region IPromptTemplateRepository Implementation

    /// <inheritdoc />
    public IReadOnlyList<IPromptTemplate> GetAllTemplates()
    {
        EnsureInitialized();

        var templates = _templates.Values
            .Select(e => e.Template)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogDebug("GetAllTemplates returning {Count} templates", templates.Count);
        return templates.AsReadOnly();
    }

    /// <inheritdoc />
    public IPromptTemplate? GetTemplate(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        EnsureInitialized();

        if (_templates.TryGetValue(templateId, out var entry))
        {
            _logger.LogDebug(
                "GetTemplate({TemplateId}) found template from {Source}",
                templateId,
                entry.Source);
            return entry.Template;
        }

        _logger.LogDebug("GetTemplate({TemplateId}) not found", templateId);
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<IPromptTemplate> GetTemplatesByCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        EnsureInitialized();

        var templates = _templates.Values
            .Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Template)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogDebug(
            "GetTemplatesByCategory({Category}) returning {Count} templates",
            category,
            templates.Count);

        return templates.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<IPromptTemplate> SearchTemplates(string query)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAllTemplates();
        }

        var queryLower = query.ToLowerInvariant();

        // LOGIC: Search across multiple fields with relevance scoring
        var results = _templates.Values
            .Select(entry => new
            {
                Entry = entry,
                Score = CalculateSearchScore(entry, queryLower)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Template.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Entry.Template)
            .ToList();

        _logger.LogDebug(
            "SearchTemplates({Query}) returning {Count} templates",
            query,
            results.Count);

        return results.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task ReloadTemplatesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Reloading all templates");

            var previousTemplates = new Dictionary<string, TemplateEntry>(_templates, StringComparer.OrdinalIgnoreCase);
            _templates.Clear();

            // Reload in priority order
            if (_options.EnableBuiltInTemplates)
            {
                await LoadEmbeddedTemplatesAsync(cancellationToken);
            }

            if (CanLoadCustomTemplates())
            {
                await LoadGlobalTemplatesAsync(cancellationToken);
                await LoadUserTemplatesAsync(cancellationToken);
            }

            // Detect and notify changes
            NotifyTemplateChanges(previousTemplates);

            _logger.LogInformation(
                "Template reload complete. Total: {Count} templates",
                _templates.Count);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    /// <inheritdoc />
    public TemplateInfo? GetTemplateInfo(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        EnsureInitialized();

        if (_templates.TryGetValue(templateId, out var entry))
        {
            _logger.LogDebug(
                "GetTemplateInfo({TemplateId}) found: Source={Source}, Category={Category}",
                templateId,
                entry.Source,
                entry.Category ?? "(none)");

            return entry.ToTemplateInfo();
        }

        _logger.LogDebug("GetTemplateInfo({TemplateId}) not found", templateId);
        return null;
    }

    #endregion

    #region Template Loading

    /// <summary>
    /// Loads embedded templates from assembly resources.
    /// </summary>
    private async Task LoadEmbeddedTemplatesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading embedded templates");

        var entries = await _loader.LoadEmbeddedTemplatesAsync(cancellationToken);

        foreach (var entry in entries)
        {
            CacheTemplate(entry);
        }

        _logger.LogDebug("Loaded {Count} embedded templates", entries.Count);
    }

    /// <summary>
    /// Loads global templates from the global templates directory.
    /// </summary>
    private async Task LoadGlobalTemplatesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.GlobalTemplatesPath))
        {
            _logger.LogDebug("Global templates path not configured");
            return;
        }

        if (!Directory.Exists(_options.GlobalTemplatesPath))
        {
            _logger.LogDebug(
                "Global templates directory does not exist: {Path}",
                _options.GlobalTemplatesPath);
            return;
        }

        _logger.LogDebug("Loading global templates from {Path}", _options.GlobalTemplatesPath);

        var entries = await _loader.LoadFromDirectoryAsync(
            _options.GlobalTemplatesPath,
            TemplateSource.Global,
            _options.TemplateExtensions,
            cancellationToken);

        foreach (var entry in entries)
        {
            CacheTemplate(entry);
        }

        _logger.LogDebug("Loaded {Count} global templates", entries.Count);
    }

    /// <summary>
    /// Loads user templates from the user templates directory.
    /// </summary>
    private async Task LoadUserTemplatesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.UserTemplatesPath))
        {
            _logger.LogDebug("User templates path not configured");
            return;
        }

        if (!Directory.Exists(_options.UserTemplatesPath))
        {
            _logger.LogDebug(
                "User templates directory does not exist: {Path}",
                _options.UserTemplatesPath);
            return;
        }

        _logger.LogDebug("Loading user templates from {Path}", _options.UserTemplatesPath);

        var entries = await _loader.LoadFromDirectoryAsync(
            _options.UserTemplatesPath,
            TemplateSource.User,
            _options.TemplateExtensions,
            cancellationToken);

        foreach (var entry in entries)
        {
            CacheTemplate(entry);
        }

        _logger.LogDebug("Loaded {Count} user templates", entries.Count);
    }

    /// <summary>
    /// Caches a template entry, respecting source priority.
    /// </summary>
    /// <param name="entry">The template entry to cache.</param>
    private void CacheTemplate(TemplateEntry entry)
    {
        var templateId = entry.Template.TemplateId;

        _templates.AddOrUpdate(
            templateId,
            entry,
            (key, existing) =>
            {
                // LOGIC: Higher priority source wins (User > Global > Embedded)
                if (entry.Source >= existing.Source)
                {
                    _logger.LogDebug(
                        "Template '{TemplateId}' overridden: {OldSource} -> {NewSource}",
                        key,
                        existing.Source,
                        entry.Source);
                    return entry;
                }

                _logger.LogDebug(
                    "Template '{TemplateId}' kept: {ExistingSource} > {NewSource}",
                    key,
                    existing.Source,
                    entry.Source);
                return existing;
            });
    }

    #endregion

    #region Hot-Reload

    /// <summary>
    /// Sets up file system watching for hot-reload.
    /// </summary>
    private void SetupFileWatcher()
    {
        if (_fileWatcher == null)
        {
            _logger.LogDebug("File watcher not available, hot-reload disabled");
            return;
        }

        if (!_options.EnableHotReload)
        {
            _logger.LogDebug("Hot-reload disabled in configuration");
            return;
        }

        if (!CanUseHotReload())
        {
            _logger.LogInformation(
                "Hot-reload requires {RequiredTier} tier or {FeatureCode} feature. " +
                "Hot-reload disabled.",
                LicenseTier.Teams,
                FeatureHotReload);
            return;
        }

        // Subscribe to file watcher events
        _fileWatcher.ChangesDetected += OnFileSystemChangesDetected;
        _fileWatcher.Error += OnFileWatcherError;
        _fileWatcher.BufferOverflow += OnFileWatcherBufferOverflow;

        // Set debounce delay from options
        _fileWatcher.DebounceDelayMs = _options.FileWatcherDebounceMs;

        // Watch user templates directory
        if (!string.IsNullOrWhiteSpace(_options.UserTemplatesPath) &&
            Directory.Exists(_options.UserTemplatesPath))
        {
            _fileWatcher.StartWatching(
                _options.UserTemplatesPath,
                "*.*",  // Watch all files, filter by extension in handler
                includeSubdirectories: false);

            _logger.LogInformation(
                "Hot-reload enabled for user templates: {Path}",
                _options.UserTemplatesPath);
        }

        // Watch global templates directory if different from user
        if (!string.IsNullOrWhiteSpace(_options.GlobalTemplatesPath) &&
            Directory.Exists(_options.GlobalTemplatesPath) &&
            !string.Equals(_options.GlobalTemplatesPath, _options.UserTemplatesPath, StringComparison.OrdinalIgnoreCase))
        {
            // Note: Current IFileSystemWatcher only supports watching one path
            // For multiple paths, we'd need multiple watchers or a different implementation
            _logger.LogDebug(
                "Global templates directory will require manual reload: {Path}",
                _options.GlobalTemplatesPath);
        }
    }

    /// <summary>
    /// Handles file system change events from the watcher.
    /// </summary>
    private void OnFileSystemChangesDetected(object? sender, FileSystemChangeBatchEventArgs e)
    {
        // Filter for template file extensions
        var templateChanges = e.Changes
            .Where(c => IsTemplateFile(c.FullPath))
            .ToList();

        if (templateChanges.Count == 0)
        {
            _logger.LogTrace("File changes detected but none are template files");
            return;
        }

        _logger.LogDebug(
            "File system changes detected: {Count} template files",
            templateChanges.Count);

        // Queue changes for debounced processing
        foreach (var change in templateChanges)
        {
            _pendingChanges.Enqueue(change);
        }

        // Reset debounce timer
        _debounceTimer?.Change(_options.FileWatcherDebounceMs, Timeout.Infinite);
    }

    /// <summary>
    /// Handles the debounce timer elapsed event.
    /// </summary>
    private async void OnDebounceTimerElapsed(object? state)
    {
        try
        {
            await ProcessPendingChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending template changes");
        }
    }

    /// <summary>
    /// Processes accumulated file changes after debounce period.
    /// </summary>
    private async Task ProcessPendingChangesAsync()
    {
        // Drain the queue
        var changes = new List<FileSystemChangeInfo>();
        while (_pendingChanges.TryDequeue(out var change))
        {
            changes.Add(change);
        }

        if (changes.Count == 0) return;

        _logger.LogDebug("Processing {Count} accumulated template file changes", changes.Count);

        // Deduplicate and process
        var uniqueFiles = changes
            .GroupBy(c => c.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())  // Take most recent change for each file
            .ToList();

        foreach (var change in uniqueFiles)
        {
            await ProcessFileChangeAsync(change);
        }
    }

    /// <summary>
    /// Processes a single file change.
    /// </summary>
    private async Task ProcessFileChangeAsync(FileSystemChangeInfo change)
    {
        try
        {
            _logger.LogDebug(
                "Processing file change: {ChangeType} - {Path}",
                change.ChangeType,
                change.FullPath);

            switch (change.ChangeType)
            {
                case FileSystemChangeType.Created:
                case FileSystemChangeType.Changed:
                    await HandleTemplateFileChangedAsync(change.FullPath);
                    break;

                case FileSystemChangeType.Deleted:
                    HandleTemplateFileDeleted(change.FullPath);
                    break;

                case FileSystemChangeType.Renamed:
                    // Handle as delete + create
                    if (!string.IsNullOrEmpty(change.OldPath))
                    {
                        HandleTemplateFileDeleted(change.OldPath);
                    }
                    await HandleTemplateFileChangedAsync(change.FullPath);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process file change: {Path}", change.FullPath);
        }
    }

    /// <summary>
    /// Handles a template file being created or modified.
    /// </summary>
    private async Task HandleTemplateFileChangedAsync(string filePath)
    {
        // Determine source based on path
        var source = DetermineSourceFromPath(filePath);

        var entry = await _loader.LoadFromFileAsync(filePath, source);
        if (entry == null)
        {
            _logger.LogWarning("Failed to load template from: {Path}", filePath);
            return;
        }

        var isUpdate = _templates.ContainsKey(entry.Template.TemplateId);
        CacheTemplate(entry);

        var changeType = isUpdate ? TemplateChangeType.Updated : TemplateChangeType.Added;
        OnTemplateChanged(new TemplateChangedEventArgs
        {
            TemplateId = entry.Template.TemplateId,
            ChangeType = changeType,
            Source = entry.Source,
            Template = entry.Template,
            Timestamp = DateTimeOffset.UtcNow
        });

        _logger.LogInformation(
            "Template hot-reloaded: {TemplateId} ({ChangeType}) from {Path}",
            entry.Template.TemplateId,
            changeType,
            filePath);
    }

    /// <summary>
    /// Handles a template file being deleted.
    /// </summary>
    private void HandleTemplateFileDeleted(string filePath)
    {
        // Find template that came from this file
        var entry = _templates.Values.FirstOrDefault(e =>
            string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            _logger.LogDebug("Deleted file was not a cached template: {Path}", filePath);
            return;
        }

        var templateId = entry.Template.TemplateId;

        if (_templates.TryRemove(templateId, out _))
        {
            OnTemplateChanged(new TemplateChangedEventArgs
            {
                TemplateId = templateId,
                ChangeType = TemplateChangeType.Removed,
                Source = entry.Source,
                Template = null,
                Timestamp = DateTimeOffset.UtcNow
            });

            _logger.LogInformation(
                "Template removed: {TemplateId} (file deleted: {Path})",
                templateId,
                filePath);

            // LOGIC: Check if there's a lower-priority version to restore
            // This would require tracking all versions, which is not implemented
            // in this version. A full reload would restore the lower-priority version.
        }
    }

    /// <summary>
    /// Handles file watcher errors.
    /// </summary>
    private void OnFileWatcherError(object? sender, FileSystemWatcherErrorEventArgs e)
    {
        if (e.IsRecoverable)
        {
            _logger.LogWarning(
                e.Exception,
                "Recoverable file watcher error occurred");
        }
        else
        {
            _logger.LogError(
                e.Exception,
                "Non-recoverable file watcher error. Hot-reload may be unavailable.");
        }
    }

    /// <summary>
    /// Handles file watcher buffer overflow.
    /// </summary>
    private void OnFileWatcherBufferOverflow(object? sender, EventArgs e)
    {
        _logger.LogWarning(
            "File watcher buffer overflow detected. " +
            "Some changes may have been missed. Consider triggering a full reload.");
    }

    /// <summary>
    /// Determines the template source based on file path.
    /// </summary>
    private TemplateSource DetermineSourceFromPath(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(_options.UserTemplatesPath) &&
            filePath.StartsWith(_options.UserTemplatesPath, StringComparison.OrdinalIgnoreCase))
        {
            return TemplateSource.User;
        }

        if (!string.IsNullOrWhiteSpace(_options.GlobalTemplatesPath) &&
            filePath.StartsWith(_options.GlobalTemplatesPath, StringComparison.OrdinalIgnoreCase))
        {
            return TemplateSource.Global;
        }

        // Default to user for unknown paths
        return TemplateSource.User;
    }

    /// <summary>
    /// Checks if a file path is a template file based on extension.
    /// </summary>
    private bool IsTemplateFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return _options.TemplateExtensions.Any(ext =>
            string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region License Checking

    /// <summary>
    /// Checks if custom templates can be loaded.
    /// </summary>
    private bool CanLoadCustomTemplates()
    {
        var tier = _licenseContext.GetCurrentTier();
        if (tier >= LicenseTier.WriterPro)
        {
            return true;
        }

        // Also check feature flag for granular control
        return _licenseContext.IsFeatureEnabled(FeatureCustomTemplates);
    }

    /// <summary>
    /// Checks if hot-reload can be used.
    /// </summary>
    private bool CanUseHotReload()
    {
        var tier = _licenseContext.GetCurrentTier();
        if (tier >= LicenseTier.Teams)
        {
            return true;
        }

        // Also check feature flag for granular control
        return _licenseContext.IsFeatureEnabled(FeatureHotReload);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ensures the repository is initialized before operations.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            // Synchronous initialization fallback
            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Calculates search relevance score for a template entry.
    /// </summary>
    private static int CalculateSearchScore(TemplateEntry entry, string queryLower)
    {
        var score = 0;
        var template = entry.Template;

        // Exact ID match = highest score
        if (template.TemplateId.Equals(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }
        // ID contains query
        else if (template.TemplateId.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 50;
        }

        // Name starts with query = high score
        if (template.Name.StartsWith(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }
        // Name contains query
        else if (template.Name.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        // Description contains query
        if (!string.IsNullOrEmpty(template.Description) &&
            template.Description.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        // Tags contain query
        if (entry.Tags.Any(t => t.Contains(queryLower, StringComparison.OrdinalIgnoreCase)))
        {
            score += 15;
        }

        // Category matches query
        if (!string.IsNullOrEmpty(entry.Category) &&
            entry.Category.Contains(queryLower, StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
        }

        return score;
    }

    /// <summary>
    /// Notifies listeners of template changes detected during reload.
    /// </summary>
    private void NotifyTemplateChanges(Dictionary<string, TemplateEntry> previousTemplates)
    {
        // Find removed templates
        foreach (var (templateId, oldEntry) in previousTemplates)
        {
            if (!_templates.ContainsKey(templateId))
            {
                OnTemplateChanged(new TemplateChangedEventArgs
                {
                    TemplateId = templateId,
                    ChangeType = TemplateChangeType.Removed,
                    Source = oldEntry.Source,
                    Template = null,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        // Find added and updated templates
        foreach (var (templateId, newEntry) in _templates)
        {
            if (previousTemplates.TryGetValue(templateId, out var oldEntry))
            {
                // Check if updated (different source or load time)
                if (oldEntry.Source != newEntry.Source ||
                    oldEntry.LoadedAt != newEntry.LoadedAt)
                {
                    OnTemplateChanged(new TemplateChangedEventArgs
                    {
                        TemplateId = templateId,
                        ChangeType = TemplateChangeType.Updated,
                        Source = newEntry.Source,
                        Template = newEntry.Template,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            }
            else
            {
                OnTemplateChanged(new TemplateChangedEventArgs
                {
                    TemplateId = templateId,
                    ChangeType = TemplateChangeType.Added,
                    Source = newEntry.Source,
                    Template = newEntry.Template,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }
    }

    /// <summary>
    /// Raises the <see cref="TemplateChanged"/> event.
    /// </summary>
    private void OnTemplateChanged(TemplateChangedEventArgs args)
    {
        _logger.LogDebug(
            "Template changed: {TemplateId}, Type={ChangeType}, Source={Source}",
            args.TemplateId,
            args.ChangeType,
            args.Source);

        TemplateChanged?.Invoke(this, args);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing PromptTemplateRepository");

        // Unsubscribe from file watcher events
        if (_fileWatcher != null)
        {
            _fileWatcher.ChangesDetected -= OnFileSystemChangesDetected;
            _fileWatcher.Error -= OnFileWatcherError;
            _fileWatcher.BufferOverflow -= OnFileWatcherBufferOverflow;
        }

        _debounceTimer?.Dispose();
        _reloadLock.Dispose();
        _templates.Clear();

        _disposed = true;
        _logger.LogDebug("PromptTemplateRepository disposed");
    }

    #endregion
}
