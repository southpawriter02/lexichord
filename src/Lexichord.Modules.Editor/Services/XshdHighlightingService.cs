using System.Reflection;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services;

/// <summary>
/// Service for managing syntax highlighting definitions.
/// </summary>
/// <remarks>
/// LOGIC: ISyntaxHighlightingService abstracts highlighting from the editor.
/// It provides theme-aware definitions that update when the app theme changes.
/// This interface is defined in the module (not Abstractions) because it
/// depends on AvaloniaEdit.IHighlightingDefinition.
/// </remarks>
public interface ISyntaxHighlightingService
{
    /// <summary>
    /// Gets the highlighting definition for a file extension.
    /// </summary>
    /// <param name="fileExtension">File extension including the dot (e.g., ".md").</param>
    /// <returns>The highlighting definition, or null for unsupported extensions.</returns>
    IHighlightingDefinition? GetHighlighting(string fileExtension);

    /// <summary>
    /// Gets a highlighting definition by name.
    /// </summary>
    /// <param name="name">The definition name (e.g., "Markdown", "JSON").</param>
    /// <returns>The highlighting definition, or null if not found.</returns>
    IHighlightingDefinition? GetHighlightingByName(string name);

    /// <summary>
    /// Registers a custom highlighting definition.
    /// </summary>
    void RegisterHighlighting(
        string name,
        IReadOnlyList<string> extensions,
        IHighlightingDefinition definition);

    /// <summary>
    /// Registers a highlighting definition from an XSHD stream.
    /// </summary>
    void RegisterHighlightingFromXshd(
        string name,
        IReadOnlyList<string> extensions,
        Stream xshdStream);

    /// <summary>
    /// Sets the active theme for highlighting colors.
    /// </summary>
    void SetTheme(EditorTheme theme);

    /// <summary>
    /// Gets the current editor theme.
    /// </summary>
    EditorTheme CurrentTheme { get; }

    /// <summary>
    /// Gets the list of available highlighting names.
    /// </summary>
    IReadOnlyList<string> GetAvailableHighlightings();

    /// <summary>
    /// Gets the file extensions supported by a highlighting definition.
    /// </summary>
    IReadOnlyList<string> GetExtensionsForHighlighting(string name);

    /// <summary>
    /// Loads all embedded highlighting definitions.
    /// </summary>
    Task LoadDefinitionsAsync();

    /// <summary>
    /// Event raised when highlighting definitions change.
    /// </summary>
    event EventHandler<HighlightingChangedEventArgs>? HighlightingChanged;
}

/// <summary>
/// Syntax highlighting service that loads definitions from embedded XSHD resources.
/// </summary>
/// <remarks>
/// LOGIC: XshdHighlightingService provides theme-aware syntax highlighting:
/// 1. XSHD files are embedded as resources in the module assembly.
/// 2. Light/dark theme variants are loaded based on current theme.
/// 3. Theme changes invalidate the cache and trigger a reload.
/// 4. Extension mapping allows auto-detection of file types.
/// </remarks>
public sealed class XshdHighlightingService : ISyntaxHighlightingService, IDisposable
{
    private readonly IThemeManager _themeManager;
    private readonly ILogger<XshdHighlightingService> _logger;
    private readonly Dictionary<string, IHighlightingDefinition> _definitionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _extensionMapping = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _highlightingExtensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private EditorTheme _currentTheme = EditorTheme.Light;
    private bool _disposed;

    /// <summary>
    /// Built-in highlighting definitions with their associated file extensions.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> BuiltInHighlightings =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Markdown"] = new[] { ".md", ".markdown", ".mdown", ".mkd" },
            ["JSON"] = new[] { ".json", ".jsonc" },
            ["YAML"] = new[] { ".yml", ".yaml" },
            ["XML"] = new[] { ".xml", ".xsd", ".xsl", ".xslt", ".xaml", ".axaml", ".xshd", ".config", ".csproj", ".props", ".targets" }
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="XshdHighlightingService"/> class.
    /// </summary>
    public XshdHighlightingService(IThemeManager themeManager, ILogger<XshdHighlightingService> logger)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Initialize extension mappings from built-in definitions
        foreach (var (name, extensions) in BuiltInHighlightings)
        {
            _highlightingExtensions[name] = extensions.ToList();
            foreach (var ext in extensions)
            {
                _extensionMapping[ext] = name;
            }
        }

        // LOGIC: Initialize theme from current app theme
        _currentTheme = MapThemeModeToEditorTheme(_themeManager.GetEffectiveTheme());

        // LOGIC: Subscribe to theme changes
        _themeManager.ThemeChanged += OnThemeChanged;

        _logger.LogDebug("XshdHighlightingService initialized with theme: {Theme}", _currentTheme);
    }

    /// <inheritdoc/>
    public EditorTheme CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public event EventHandler<HighlightingChangedEventArgs>? HighlightingChanged;

    /// <inheritdoc/>
    public IHighlightingDefinition? GetHighlighting(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
        {
            _logger.LogDebug("GetHighlighting called with null/empty extension");
            return null;
        }

        // LOGIC: Normalize extension (ensure leading dot, lowercase)
        var ext = fileExtension.StartsWith('.')
            ? fileExtension.ToLowerInvariant()
            : $".{fileExtension.ToLowerInvariant()}";

        if (!_extensionMapping.TryGetValue(ext, out var highlightingName))
        {
            _logger.LogDebug("No highlighting found for extension: {Extension}", ext);
            return null;
        }

        return GetHighlightingByName(highlightingName);
    }

    /// <inheritdoc/>
    public IHighlightingDefinition? GetHighlightingByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        lock (_lock)
        {
            // LOGIC: Return cached definition if available
            if (_definitionCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            // LOGIC: Load from embedded resource
            var definition = LoadDefinitionFromResource(name);
            if (definition != null)
            {
                _definitionCache[name] = definition;
                _logger.LogDebug("Loaded highlighting definition: {Name} ({Theme})", name, _currentTheme);
            }

            return definition;
        }
    }

    /// <inheritdoc/>
    public void RegisterHighlighting(
        string name,
        IReadOnlyList<string> extensions,
        IHighlightingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(extensions);
        ArgumentNullException.ThrowIfNull(definition);

        lock (_lock)
        {
            // LOGIC: Add to cache (overwriting if exists)
            _definitionCache[name] = definition;

            // LOGIC: Update extension mappings
            _highlightingExtensions[name] = extensions.ToList();
            foreach (var ext in extensions)
            {
                var normalizedExt = ext.StartsWith('.')
                    ? ext.ToLowerInvariant()
                    : $".{ext.ToLowerInvariant()}";
                _extensionMapping[normalizedExt] = name;
            }

            _logger.LogInformation(
                "Registered custom highlighting: {Name} for extensions: {Extensions}",
                name,
                string.Join(", ", extensions));
        }

        HighlightingChanged?.Invoke(this, new HighlightingChangedEventArgs
        {
            Reason = HighlightingChangeReason.DefinitionRegistered,
            HighlightingName = name
        });
    }

    /// <inheritdoc/>
    public void RegisterHighlightingFromXshd(
        string name,
        IReadOnlyList<string> extensions,
        Stream xshdStream)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(extensions);
        ArgumentNullException.ThrowIfNull(xshdStream);

        var definition = LoadFromXshdStream(xshdStream);
        if (definition != null)
        {
            RegisterHighlighting(name, extensions, definition);
        }
        else
        {
            _logger.LogWarning("Failed to load XSHD definition from stream for: {Name}", name);
        }
    }

    /// <inheritdoc/>
    public void SetTheme(EditorTheme theme)
    {
        if (_currentTheme == theme)
        {
            return;
        }

        _logger.LogInformation("Setting editor theme to: {Theme}", theme);
        _currentTheme = theme;

        lock (_lock)
        {
            // LOGIC: Clear cache to force reload with new theme colors
            _definitionCache.Clear();
        }

        HighlightingChanged?.Invoke(this, new HighlightingChangedEventArgs
        {
            Reason = HighlightingChangeReason.ThemeChanged
        });
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableHighlightings()
    {
        lock (_lock)
        {
            return _highlightingExtensions.Keys.ToList();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetExtensionsForHighlighting(string name)
    {
        lock (_lock)
        {
            if (_highlightingExtensions.TryGetValue(name, out var extensions))
            {
                return extensions.AsReadOnly();
            }
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc/>
    public Task LoadDefinitionsAsync()
    {
        // LOGIC: Pre-load all built-in definitions
        _logger.LogDebug("Pre-loading all built-in highlighting definitions");

        foreach (var name in BuiltInHighlightings.Keys)
        {
            _ = GetHighlightingByName(name);
        }

        _logger.LogInformation(
            "Loaded {Count} highlighting definitions for {Theme} theme",
            _definitionCache.Count,
            _currentTheme);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads a highlighting definition from embedded resources.
    /// </summary>
    private IHighlightingDefinition? LoadDefinitionFromResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // LOGIC: Try theme-specific resource first (e.g., Markdown.Dark.xshd)
        var resourceName = _currentTheme == EditorTheme.Dark
            ? $"Lexichord.Modules.Editor.Resources.Highlighting.{name}.Dark.xshd"
            : $"Lexichord.Modules.Editor.Resources.Highlighting.{name}.xshd";

        var stream = assembly.GetManifestResourceStream(resourceName);

        // LOGIC: Fall back to base resource if theme-specific not found
        if (stream is null && _currentTheme == EditorTheme.Dark)
        {
            resourceName = $"Lexichord.Modules.Editor.Resources.Highlighting.{name}.xshd";
            stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is not null)
            {
                _logger.LogDebug("Using light theme XSHD for {Name} (no dark variant)", name);
            }
        }

        if (stream is null)
        {
            _logger.LogWarning("Highlighting resource not found: {Name}", name);
            return null;
        }

        using (stream)
        {
            return LoadFromXshdStream(stream);
        }
    }

    /// <summary>
    /// Loads a highlighting definition from an XSHD stream.
    /// </summary>
    private IHighlightingDefinition? LoadFromXshdStream(Stream stream)
    {
        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            var xshd = HighlightingLoader.LoadXshd(reader);
            return HighlightingLoader.Load(xshd, HighlightingManager.Instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading XSHD definition");
            return null;
        }
    }

    /// <summary>
    /// Handles theme change events from the theme manager.
    /// </summary>
    private void OnThemeChanged(object? sender, ThemeMode newTheme)
    {
        var editorTheme = MapThemeModeToEditorTheme(newTheme);
        SetTheme(editorTheme);
    }

    /// <summary>
    /// Maps application ThemeMode to EditorTheme.
    /// </summary>
    private static EditorTheme MapThemeModeToEditorTheme(ThemeMode themeMode)
    {
        return themeMode switch
        {
            ThemeMode.Dark => EditorTheme.Dark,
            _ => EditorTheme.Light
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _themeManager.ThemeChanged -= OnThemeChanged;
        _disposed = true;
    }
}
