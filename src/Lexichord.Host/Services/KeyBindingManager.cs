namespace Lexichord.Host.Services;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Alias to resolve conflict with Avalonia.Input.KeyBinding
using KeyBindingRecord = Lexichord.Abstractions.Contracts.KeyBinding;

/// <summary>
/// Implementation of <see cref="IKeyBindingService"/>.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5d): Manages keyboard shortcuts for commands.
///
/// Storage:
/// - _bindings: ConcurrentDictionary&lt;commandId, KeyBinding&gt;
/// - _reverseMap: Dictionary&lt;normalizedGesture, List&lt;commandId&gt;&gt;
///
/// Loading Order:
/// 1. Default bindings from ICommandRegistry.GetAllCommands()
/// 2. User overrides from keybindings.json
///
/// Key Event Handling:
/// 1. Create normalized key from KeyEventArgs
/// 2. Look up in _reverseMap
/// 3. Filter by context (matching context or global)
/// 4. Execute first matching command via ICommandRegistry
///
/// Thread Safety:
/// - ConcurrentDictionary for _bindings
/// - Lock for _reverseMap updates
/// - SemaphoreSlim for file operations
/// </remarks>
public sealed class KeyBindingManager : IKeyBindingService, IDisposable
{
    private readonly ConcurrentDictionary<string, KeyBindingRecord> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _reverseMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _reverseLock = new();

    private readonly ICommandRegistry _commandRegistry;
    private readonly IMediator _mediator;
    private readonly ILogger<KeyBindingManager> _logger;
    private readonly LexichordOptions _options;

    private FileSystemWatcher? _fileWatcher;
    private CancellationTokenSource? _debounceCts;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyBindingManager"/> class.
    /// </summary>
    public KeyBindingManager(
        ICommandRegistry commandRegistry,
        IMediator mediator,
        IOptions<LexichordOptions> options,
        ILogger<KeyBindingManager> logger)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Subscribe to command registration events
        _commandRegistry.CommandRegistered += OnCommandRegistered;
        _commandRegistry.CommandUnregistered += OnCommandUnregistered;
    }

    /// <inheritdoc/>
    public string KeybindingsFilePath
    {
        get
        {
            var dataPath = _options.GetResolvedDataPath();
            return Path.Combine(dataPath, "keybindings.json");
        }
    }

    /// <inheritdoc/>
    public bool HasCustomBindings => File.Exists(KeybindingsFilePath);

    /// <inheritdoc/>
    public string? GetBinding(string commandId)
    {
        if (_bindings.TryGetValue(commandId, out var binding) && !binding.IsDisabled)
        {
            return binding.Gesture;
        }
        return null;
    }

    /// <inheritdoc/>
    public KeyBindingRecord? GetFullBinding(string commandId)
    {
        return _bindings.TryGetValue(commandId, out var binding) ? binding : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<KeyBindingRecord> GetAllBindings()
    {
        return _bindings.Values.Where(b => !b.IsDisabled).ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetConflicts(string gesture, string? context = null)
    {
        var key = NormalizeGestureString(gesture);
        if (key is null)
            return [];

        lock (_reverseLock)
        {
            if (!_reverseMap.TryGetValue(key, out var commandIds))
                return [];

            // Filter by context
            return commandIds
                .Where(id =>
                {
                    if (!_bindings.TryGetValue(id, out var binding))
                        return false;
                    if (binding.IsDisabled)
                        return false;
                    // Match if binding is global or matches the context
                    return binding.When is null || binding.When == context;
                })
                .ToList();
        }
    }

    /// <inheritdoc/>
    public bool IsGestureAvailable(string gesture, string? context = null, string? excludeCommandId = null)
    {
        var conflicts = GetConflicts(gesture, context);
        return conflicts.All(id => id.Equals(excludeCommandId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public void SetBinding(string commandId, string? gesture, string? when = null)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            throw new ArgumentException("Command ID is required.", nameof(commandId));

        string? oldGesture = null;

        // Get old binding for event
        if (_bindings.TryGetValue(commandId, out var oldBinding))
        {
            oldGesture = oldBinding.Gesture;
            RemoveFromReverseMapByString(commandId, oldBinding.Gesture);
        }

        // Check for disable ("-") or null
        var isDisabled = gesture is null or "-";
        var normalizedGesture = isDisabled ? null : gesture;

        var newBinding = new KeyBindingRecord(commandId, normalizedGesture)
        {
            When = when,
            IsUserDefined = true,
            IsDisabled = isDisabled
        };

        _bindings[commandId] = newBinding;

        if (!isDisabled && normalizedGesture is not null)
        {
            AddToReverseMapByString(commandId, normalizedGesture);

            // Check for conflicts
            var conflicts = GetConflicts(normalizedGesture, when);
            if (conflicts.Count > 1)
            {
                _logger.LogWarning(
                    "Keybinding conflict: {Gesture} is bound to multiple commands in context '{Context}': {Commands}",
                    normalizedGesture,
                    when ?? "global",
                    string.Join(", ", conflicts));
            }
        }

        _logger.LogDebug(
            "Set keybinding for {CommandId}: {OldGesture} -> {NewGesture}",
            commandId,
            oldGesture ?? "(none)",
            normalizedGesture ?? "(disabled)");

        // Raise events
        RaiseBindingChanged(commandId, oldGesture, normalizedGesture, isUserChange: true);
    }

    /// <inheritdoc/>
    public void ResetBinding(string commandId)
    {
        if (!_bindings.TryGetValue(commandId, out var oldBinding))
            return;

        if (!oldBinding.IsUserDefined)
            return; // Already at default

        var oldGesture = oldBinding.Gesture;

        // Remove user binding
        RemoveFromReverseMapByString(commandId, oldGesture);

        // Restore default from command definition
        var command = _commandRegistry.GetCommand(commandId);
        if (command?.DefaultShortcut is not null)
        {
            var defaultBinding = new KeyBindingRecord(commandId, command.DefaultShortcut)
            {
                When = command.Context,
                IsUserDefined = false,
                IsDisabled = false
            };
            _bindings[commandId] = defaultBinding;
            AddToReverseMapByString(commandId, command.DefaultShortcut);

            RaiseBindingChanged(commandId, oldGesture, command.DefaultShortcut, isUserChange: true);
            return;
        }

        // No default, just remove
        _bindings.TryRemove(commandId, out _);
        RaiseBindingChanged(commandId, oldGesture, null, isUserChange: true);
    }

    /// <inheritdoc/>
    public void ResetToDefaults()
    {
        _logger.LogInformation("Resetting all keybindings to defaults");

        // Clear all
        _bindings.Clear();
        lock (_reverseLock)
        {
            _reverseMap.Clear();
        }

        // Reload defaults from commands
        foreach (var command in _commandRegistry.GetAllCommands())
        {
            if (string.IsNullOrWhiteSpace(command.DefaultShortcut))
                continue;

            var binding = new KeyBindingRecord(command.Id, command.DefaultShortcut)
            {
                When = command.Context,
                IsUserDefined = false,
                IsDisabled = false
            };

            _bindings[command.Id] = binding;
            AddToReverseMapByString(command.Id, command.DefaultShortcut);
        }

        // Delete user keybindings file
        try
        {
            if (File.Exists(KeybindingsFilePath))
            {
                File.Delete(KeybindingsFilePath);
                _logger.LogDebug("Deleted user keybindings file: {Path}", KeybindingsFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete keybindings file: {Path}", KeybindingsFilePath);
        }

        BindingsReloaded?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task LoadBindingsAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            _logger.LogDebug("Loading keybindings...");

            // Clear existing
            _bindings.Clear();
            lock (_reverseLock)
            {
                _reverseMap.Clear();
            }

            // 1. Load defaults from command definitions
            var commands = _commandRegistry.GetAllCommands();
            var commandsWithBindings = 0;

            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command.DefaultShortcut))
                    continue;

                // Validate gesture string
                if (!IsValidGestureString(command.DefaultShortcut))
                {
                    _logger.LogWarning(
                        "Invalid default shortcut for {CommandId}: {Shortcut}",
                        command.Id,
                        command.DefaultShortcut);
                    continue;
                }

                var binding = new KeyBindingRecord(command.Id, command.DefaultShortcut)
                {
                    When = command.Context,
                    IsUserDefined = false,
                    IsDisabled = false
                };

                _bindings[command.Id] = binding;
                AddToReverseMapByString(command.Id, command.DefaultShortcut);
                commandsWithBindings++;
            }

            _logger.LogDebug("Loaded {Count} default keybindings from commands", commandsWithBindings);

            // 2. Load user overrides
            var userOverrides = 0;
            if (File.Exists(KeybindingsFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(KeybindingsFilePath);
                    var file = JsonSerializer.Deserialize<KeybindingsFile>(json, JsonOptions);

                    if (file?.Bindings is not null)
                    {
                        foreach (var entry in file.Bindings)
                        {
                            if (string.IsNullOrWhiteSpace(entry.Command))
                            {
                                _logger.LogWarning("Skipping keybinding entry with empty command");
                                continue;
                            }

                            // Remove old binding from reverse map
                            if (_bindings.TryGetValue(entry.Command, out var oldBinding))
                            {
                                RemoveFromReverseMapByString(entry.Command, oldBinding.Gesture);
                            }

                            // Check for disable ("-")
                            if (entry.Key == "-")
                            {
                                var disabledBinding = new KeyBindingRecord(entry.Command, null)
                                {
                                    When = entry.When,
                                    IsUserDefined = true,
                                    IsDisabled = true
                                };
                                _bindings[entry.Command] = disabledBinding;
                                userOverrides++;
                                continue;
                            }

                            // Validate gesture string
                            if (!IsValidGestureString(entry.Key))
                            {
                                _logger.LogWarning(
                                    "Invalid user keybinding for {CommandId}: {Key}",
                                    entry.Command,
                                    entry.Key);
                                continue;
                            }

                            var binding = new KeyBindingRecord(entry.Command, entry.Key)
                            {
                                When = entry.When,
                                IsUserDefined = true,
                                IsDisabled = false
                            };

                            _bindings[entry.Command] = binding;
                            AddToReverseMapByString(entry.Command, entry.Key);
                            userOverrides++;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse keybindings.json");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load keybindings.json");
                }
            }

            _logger.LogDebug("Loaded {Count} user keybinding overrides", userOverrides);

            // 3. Detect and log conflicts
            var conflictCount = DetectAndLogConflicts();

            // 4. Set up file watcher
            SetupFileWatcher();

            // 5. Publish event
            await _mediator.Publish(new KeybindingsReloadedEvent(
                TotalBindings: _bindings.Count,
                UserOverrides: userOverrides,
                ConflictCount: conflictCount));

            BindingsReloaded?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation(
                "Keybindings loaded: {Total} total, {UserOverrides} user overrides, {Conflicts} conflicts",
                _bindings.Count,
                userOverrides,
                conflictCount);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SaveBindingsAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            var userBindings = _bindings.Values
                .Where(b => b.IsUserDefined)
                .Select(b => new KeybindingEntry
                {
                    Command = b.CommandId,
                    Key = b.IsDisabled ? "-" : (b.Gesture ?? "-"),
                    When = b.When
                })
                .ToList();

            var file = new KeybindingsFile { Bindings = userBindings };
            var json = JsonSerializer.Serialize(file, JsonOptions);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(KeybindingsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(KeybindingsFilePath, json);

            _logger.LogDebug("Saved {Count} user keybindings to {Path}", userBindings.Count, KeybindingsFilePath);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Handles a key event, attempting to match it to a registered binding.
    /// </summary>
    /// <param name="e">The key event.</param>
    /// <param name="context">Optional context for filtering.</param>
    /// <returns>True if the event was handled.</returns>
    public bool TryHandleKeyEvent(KeyEventArgs e, string? context = null)
    {
        // Skip if already handled or just modifiers
        if (e.Handled)
            return false;

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return false;
        }

        // Build gesture from event and normalize
        var gesture = new KeyGesture(e.Key, e.KeyModifiers);
        var normalizedKey = NormalizeKeyGesture(gesture);

        List<string>? matchingCommandIds;
        lock (_reverseLock)
        {
            if (!_reverseMap.TryGetValue(normalizedKey, out matchingCommandIds))
                return false;
        }

        // Filter and prioritize by context
        // 1. Exact context match (highest priority)
        // 2. Global bindings (null When)
        var candidates = matchingCommandIds
            .Select(id => _bindings.TryGetValue(id, out var b) ? b : null)
            .Where(b => b is not null && !b.IsDisabled)
            .OrderByDescending(b => b!.When == context ? 1 : 0) // Context match first
            .ThenByDescending(b => b!.When is null ? 0 : -1)    // Then global
            .ToList();

        foreach (var binding in candidates)
        {
            if (binding is null)
                continue;

            // Check context
            if (binding.When is not null && binding.When != context)
                continue;

            // Try to execute
            if (_commandRegistry.TryExecute(binding.CommandId))
            {
                e.Handled = true;
                _logger.LogDebug(
                    "Executed command {CommandId} via keybinding {Gesture}",
                    binding.CommandId,
                    binding.Gesture);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Formats an Avalonia KeyGesture to a string.
    /// </summary>
    public static string FormatKeyGesture(KeyGesture gesture)
    {
        var parts = new List<string>();

        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Control))
            parts.Add("Ctrl");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Shift))
            parts.Add("Shift");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Alt))
            parts.Add("Alt");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Meta))
            parts.Add(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Cmd" : "Win");

        parts.Add(gesture.Key.ToString());

        return string.Join("+", parts);
    }

    /// <summary>
    /// Parses a gesture string into an Avalonia KeyGesture.
    /// </summary>
    public static KeyGesture? ParseKeyGesture(string gestureString)
    {
        if (string.IsNullOrWhiteSpace(gestureString) || gestureString == "-")
            return null;

        var parts = gestureString.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        var modifiers = KeyModifiers.None;
        Key? key = null;

        foreach (var part in parts)
        {
            var normalized = part.Trim().ToLowerInvariant();

            switch (normalized)
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Control;
                    break;
                case "cmd":
                case "command":
                    // On macOS, Cmd maps to Control for keyboard shortcuts
                    modifiers |= KeyModifiers.Control;
                    break;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    break;
                case "alt":
                case "option":
                    modifiers |= KeyModifiers.Alt;
                    break;
                case "win":
                case "super":
                case "meta":
                    modifiers |= KeyModifiers.Meta;
                    break;
                default:
                    // This should be the key
                    if (Enum.TryParse<Key>(part.Trim(), ignoreCase: true, out var parsedKey))
                    {
                        key = parsedKey;
                    }
                    else
                    {
                        // Try single character
                        var cleanPart = part.Trim().ToUpperInvariant();
                        if (cleanPart.Length == 1 && Enum.TryParse<Key>(cleanPart, ignoreCase: true, out var charKey))
                        {
                            key = charKey;
                        }
                        else
                        {
                            return null; // Invalid key
                        }
                    }
                    break;
            }
        }

        return key.HasValue ? new KeyGesture(key.Value, modifiers) : null;
    }

    /// <inheritdoc/>
    public event EventHandler<KeyBindingChangedEventArgs>? BindingChanged;

    /// <inheritdoc/>
    public event EventHandler? BindingsReloaded;

    // ========== Private Helpers ==========

    /// <summary>
    /// Validates that a gesture string can be parsed.
    /// </summary>
    private static bool IsValidGestureString(string gestureString)
    {
        return ParseKeyGesture(gestureString) is not null;
    }

    /// <summary>
    /// Normalizes an Avalonia KeyGesture to a consistent string key for lookup.
    /// </summary>
    private static string NormalizeKeyGesture(KeyGesture gesture)
    {
        // Create a normalized string for lookup
        // Format: "Modifiers:Key" where modifiers are sorted
        var modParts = new List<string>();
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Alt))
            modParts.Add("Alt");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Control))
            modParts.Add("Ctrl");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Meta))
            modParts.Add("Meta");
        if (gesture.KeyModifiers.HasFlag(KeyModifiers.Shift))
            modParts.Add("Shift");

        return $"{string.Join("+", modParts)}:{gesture.Key}";
    }

    /// <summary>
    /// Normalizes a gesture string to a consistent key for lookup.
    /// </summary>
    private static string? NormalizeGestureString(string gestureString)
    {
        var gesture = ParseKeyGesture(gestureString);
        return gesture is not null ? NormalizeKeyGesture(gesture) : null;
    }

    private void AddToReverseMapByString(string commandId, string? gestureString)
    {
        if (string.IsNullOrWhiteSpace(gestureString))
            return;

        var key = NormalizeGestureString(gestureString);
        if (key is null)
            return;

        lock (_reverseLock)
        {
            if (!_reverseMap.TryGetValue(key, out var list))
            {
                list = [];
                _reverseMap[key] = list;
            }

            if (!list.Contains(commandId, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(commandId);
            }
        }
    }

    private void RemoveFromReverseMapByString(string commandId, string? gestureString)
    {
        if (string.IsNullOrWhiteSpace(gestureString))
            return;

        var key = NormalizeGestureString(gestureString);
        if (key is null)
            return;

        lock (_reverseLock)
        {
            if (_reverseMap.TryGetValue(key, out var list))
            {
                list.RemoveAll(id => id.Equals(commandId, StringComparison.OrdinalIgnoreCase));
                if (list.Count == 0)
                {
                    _reverseMap.Remove(key);
                }
            }
        }
    }

    private int DetectAndLogConflicts()
    {
        var conflictCount = 0;
        lock (_reverseLock)
        {
            foreach (var (key, commandIds) in _reverseMap)
            {
                if (commandIds.Count <= 1)
                    continue;

                // Group by context to find real conflicts
                var byContext = commandIds
                    .Select(id => _bindings.TryGetValue(id, out var b) ? b : null)
                    .Where(b => b is not null && !b.IsDisabled)
                    .GroupBy(b => b!.When ?? "<global>");

                foreach (var group in byContext)
                {
                    if (group.Count() > 1)
                    {
                        conflictCount++;
                        var commandList = string.Join(", ", group.Select(b => b!.CommandId));
                        _logger.LogWarning(
                            "Keybinding conflict in context '{Context}' for {Gesture}: {Commands}",
                            group.Key,
                            key,
                            commandList);
                    }
                }
            }
        }
        return conflictCount;
    }

    private void SetupFileWatcher()
    {
        try
        {
            _fileWatcher?.Dispose();

            var directory = Path.GetDirectoryName(KeybindingsFilePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                _logger.LogDebug("Keybindings directory does not exist, skipping file watcher");
                return;
            }

            _fileWatcher = new FileSystemWatcher(directory, "keybindings.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;

            _logger.LogDebug("File watcher set up for: {Path}", KeybindingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set up file watcher for keybindings.json");
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: cancel previous reload and start new one
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token); // Debounce delay
                if (token.IsCancellationRequested)
                    return;

                _logger.LogInformation("Reloading keybindings due to file change");
                await LoadBindingsAsync();
            }
            catch (OperationCanceledException)
            {
                // Debounced, ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload keybindings after file change");
            }
        }, token);
    }

    private void OnCommandRegistered(object? sender, CommandRegisteredEventArgs e)
    {
        // Add default binding for new command
        if (e.Command.DefaultShortcut is null)
            return;

        // Don't override if user already has a binding
        if (_bindings.ContainsKey(e.Command.Id))
            return;

        // Validate gesture
        if (!IsValidGestureString(e.Command.DefaultShortcut))
            return;

        var binding = new KeyBindingRecord(e.Command.Id, e.Command.DefaultShortcut)
        {
            When = e.Command.Context,
            IsUserDefined = false,
            IsDisabled = false
        };

        if (_bindings.TryAdd(e.Command.Id, binding))
        {
            AddToReverseMapByString(e.Command.Id, e.Command.DefaultShortcut);
            RaiseBindingChanged(e.Command.Id, null, e.Command.DefaultShortcut, isUserChange: false);
        }
    }

    private void OnCommandUnregistered(object? sender, CommandUnregisteredEventArgs e)
    {
        if (_bindings.TryRemove(e.CommandId, out var binding))
        {
            RemoveFromReverseMapByString(e.CommandId, binding.Gesture);
            RaiseBindingChanged(e.CommandId, binding.Gesture, null, isUserChange: false);
        }
    }

    private void RaiseBindingChanged(string commandId, string? oldGesture, string? newGesture, bool isUserChange)
    {
        var args = new KeyBindingChangedEventArgs
        {
            CommandId = commandId,
            OldGesture = oldGesture,
            NewGesture = newGesture,
            IsUserChange = isUserChange
        };

        BindingChanged?.Invoke(this, args);

        // Publish MediatR event asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _mediator.Publish(new KeybindingChangedMediatREvent(
                    CommandId: commandId,
                    OldGesture: oldGesture,
                    NewGesture: newGesture));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish KeybindingChangedMediatREvent");
            }
        });
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _commandRegistry.CommandRegistered -= OnCommandRegistered;
        _commandRegistry.CommandUnregistered -= OnCommandUnregistered;

        _fileWatcher?.Dispose();
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _loadLock.Dispose();
    }
}
