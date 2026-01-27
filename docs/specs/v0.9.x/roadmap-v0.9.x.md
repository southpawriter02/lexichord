# Lexichord Premiere Roadmap (v0.9.1 - v0.9.8)

In v0.8.x, we built "The Publisher" — Git integration, release notes generation, and documentation export. In v0.9.x, we prepare for **The Premiere**: the final hardening phase before Lexichord v1.0. This phase establishes user profiles, production-ready licensing, automatic updates, distraction-free writing, startup optimization, and security safeguards.

**Architectural Note:** This version touches multiple modules but primarily extends `Lexichord.Host` with production infrastructure. The focus shifts from features to reliability, security, and polish. **License Gating:** Most v0.9.x features are available to all tiers as they improve the core experience.

**Total Sub-Parts:** 32 distinct implementation steps.

---

## v0.9.1: The User Profiles (Multi-Project Support)
**Goal:** Enable multiple local user profiles with distinct API keys, style guides, and preferences per client or project.

*   **v0.9.1a:** **Profile Model.** Define user profile data structures:
    ```csharp
    public record UserProfile(
        Guid ProfileId,
        string Name,
        string? Description,
        string? AvatarPath,
        ProfileType Type,
        DateTime CreatedAt,
        DateTime LastUsedAt
    );

    public enum ProfileType { Personal, Client, Project, Shared }

    public record ProfileSettings(
        Guid ProfileId,
        string? DefaultStyleGuide,
        string? DefaultLLMProvider,
        string? DefaultModel,
        string? WorkspacePath,
        IReadOnlyDictionary<string, string>? CustomSettings
    );

    public interface IProfileService
    {
        Task<IReadOnlyList<UserProfile>> GetProfilesAsync(CancellationToken ct);
        Task<UserProfile> GetActiveProfileAsync(CancellationToken ct);
        Task<UserProfile> CreateProfileAsync(CreateProfileRequest request, CancellationToken ct);
        Task SwitchProfileAsync(Guid profileId, CancellationToken ct);
        Task DeleteProfileAsync(Guid profileId, CancellationToken ct);
    }
    ```
*   **v0.9.1b:** **Profile Storage.** Implement profile persistence:
    *   Store profiles in PostgreSQL `user_profiles` table.
    *   Profile-specific settings in `profile_settings` table.
    *   API keys stored per-profile in `ISecureVault` (v0.0.6a) with profile prefix.
    *   Migration: `Migration_009_UserProfiles.cs`.
    ```csharp
    public class ProfileRepository(IDbConnectionFactory db) : IProfileRepository
    {
        public async Task<UserProfile?> GetByIdAsync(Guid profileId, CancellationToken ct)
        {
            using var conn = await db.OpenAsync(ct);
            return await conn.QuerySingleOrDefaultAsync<UserProfile>(
                "SELECT * FROM user_profiles WHERE profile_id = @profileId",
                new { profileId });
        }
    }
    ```
*   **v0.9.1c:** **Profile Switching.** Implement runtime profile changes:
    *   Publish `ProfileSwitchingEvent` before switch (allow modules to save state).
    *   Clear in-memory caches (style rules, RAG index references).
    *   Reload `ISettingsService` with profile-specific values.
    *   Update `ISecureVault` context for API key retrieval.
    *   Publish `ProfileSwitchedEvent` after switch completes.
    *   Auto-switch when opening workspace with associated profile.
*   **v0.9.1d:** **Profile Management UI.** Create profile selector and manager:
    *   Profile dropdown in title bar (showing current profile name + avatar).
    *   "Manage Profiles" dialog with list of all profiles.
    *   Create profile wizard: Name, type, clone settings from existing.
    *   Profile settings panel: Configure API keys, style guide, workspace.
    *   "Export Profile" / "Import Profile" for backup/sharing (excludes secrets).
    ```csharp
    public partial class ProfileSelectorViewModel : ObservableObject
    {
        [ObservableProperty] private UserProfile? _activeProfile;
        [ObservableProperty] private ObservableCollection<UserProfile> _profiles = [];

        [RelayCommand]
        private async Task SwitchProfileAsync(Guid profileId);

        [RelayCommand]
        private void OpenProfileManager();
    }
    ```

---

## v0.9.2: The License Engine (Production Licensing)
**Goal:** Implement cryptographic license key validation for production deployment with offline support.

*   **v0.9.2a:** **License Key Format.** Define license structure and validation:
    ```csharp
    public record LicenseKey(
        string KeyId,
        LicenseTier Tier,
        LicenseType Type,
        string LicenseeName,
        string? LicenseeEmail,
        DateTime IssuedAt,
        DateTime? ExpiresAt,
        int? SeatCount,
        IReadOnlyList<string>? EnabledFeatures,
        string Signature
    );

    public enum LicenseType { Perpetual, Subscription, Trial, Education, OpenSource }

    public interface ILicenseValidator
    {
        LicenseValidationResult Validate(string licenseKey);
        Task<LicenseValidationResult> ValidateOnlineAsync(string licenseKey, CancellationToken ct);
    }

    public record LicenseValidationResult(
        bool IsValid,
        LicenseKey? License,
        LicenseValidationError? Error,
        DateTime ValidatedAt
    );

    public enum LicenseValidationError
    {
        InvalidFormat, InvalidSignature, Expired, Revoked, SeatLimitExceeded, NetworkError
    }
    ```
*   **v0.9.2b:** **Cryptographic Validation.** Implement Ed25519 signature verification:
    ```csharp
    public class Ed25519LicenseValidator(ILogger<Ed25519LicenseValidator> logger) : ILicenseValidator
    {
        // Public key embedded in application (private key held by Anthropic/issuer)
        private static readonly byte[] PublicKey = Convert.FromBase64String("...");

        public LicenseValidationResult Validate(string licenseKey)
        {
            try
            {
                // 1. Decode Base64 license
                var decoded = DecodeLicense(licenseKey);

                // 2. Extract payload and signature
                var (payload, signature) = SplitPayloadSignature(decoded);

                // 3. Verify Ed25519 signature
                if (!Ed25519.Verify(signature, payload, PublicKey))
                {
                    return new LicenseValidationResult(false, null,
                        LicenseValidationError.InvalidSignature, DateTime.UtcNow);
                }

                // 4. Deserialize and validate claims
                var license = JsonSerializer.Deserialize<LicenseKey>(payload);
                if (license.ExpiresAt.HasValue && license.ExpiresAt < DateTime.UtcNow)
                {
                    return new LicenseValidationResult(false, license,
                        LicenseValidationError.Expired, DateTime.UtcNow);
                }

                return new LicenseValidationResult(true, license, null, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "License validation failed");
                return new LicenseValidationResult(false, null,
                    LicenseValidationError.InvalidFormat, DateTime.UtcNow);
            }
        }
    }
    ```
*   **v0.9.2c:** **License State Management.** Track license state across sessions:
    *   Store validated license in `ISecureVault` (encrypted at rest).
    *   Periodic online validation (daily for subscription, weekly for perpetual).
    *   Grace period: 14 days offline before features degrade.
    *   Revocation list check during online validation.
    *   `ILicenseContext` (v0.0.4c) updated to read from `ILicenseStateService`.
    ```csharp
    public interface ILicenseStateService
    {
        LicenseState CurrentState { get; }
        Task ActivateLicenseAsync(string licenseKey, CancellationToken ct);
        Task DeactivateLicenseAsync(CancellationToken ct);
        Task RefreshLicenseAsync(CancellationToken ct);
    }

    public record LicenseState(
        LicenseKey? License,
        LicenseStatus Status,
        DateTime? LastOnlineValidation,
        int DaysUntilGraceExpiry
    );

    public enum LicenseStatus { Valid, Trial, Expired, GracePeriod, Unlicensed }
    ```
*   **v0.9.2d:** **License Activation UI.** Create activation flow:
    *   Welcome dialog for first-run license entry.
    *   "Enter License Key" in Settings → Licensing.
    *   Visual feedback: Valid (green check), Invalid (red X with reason).
    *   "Activate Online" button for subscription validation.
    *   License details display: Tier, expiry, licensee name.
    *   "Deactivate" option for transferring to another machine.
    *   Trial banner: "X days remaining in trial" with upgrade CTA.

---

## v0.9.3: The Update Engine (Auto-Updates)
**Goal:** Implement seamless background updates using Velopack for delta downloads and atomic installs.

*   **v0.9.3a:** **Update Abstractions.** Define update service interfaces:
    ```csharp
    public interface IUpdateService
    {
        Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct);
        Task<UpdateProgress> DownloadUpdateAsync(UpdateInfo update, IProgress<UpdateProgress> progress, CancellationToken ct);
        Task ApplyUpdateAsync(UpdateInfo update, CancellationToken ct);
        void ScheduleRestart();
    }

    public record UpdateInfo(
        Version CurrentVersion,
        Version NewVersion,
        string ReleaseNotes,
        long DownloadSize,
        DateTime ReleasedAt,
        UpdateUrgency Urgency
    );

    public enum UpdateUrgency { Normal, Recommended, Critical }

    public record UpdateProgress(
        UpdatePhase Phase,
        double PercentComplete,
        long BytesDownloaded,
        long TotalBytes
    );

    public enum UpdatePhase { Checking, Downloading, Verifying, Ready, Applying }
    ```
*   **v0.9.3b:** **Velopack Integration.** Implement update logic:
    ```csharp
    public class VelopackUpdateService(
        IOptions<UpdateOptions> options,
        ILogger<VelopackUpdateService> logger) : IUpdateService
    {
        private readonly UpdateManager _updateManager = new(options.Value.UpdateUrl);

        public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct)
        {
            try
            {
                var updateInfo = await _updateManager.CheckForUpdatesAsync();
                if (updateInfo is null) return null;

                return new UpdateInfo(
                    CurrentVersion: _updateManager.CurrentVersion,
                    NewVersion: updateInfo.TargetFullRelease.Version,
                    ReleaseNotes: await FetchReleaseNotesAsync(updateInfo, ct),
                    DownloadSize: updateInfo.DeltasToTarget.Sum(d => d.Size),
                    ReleasedAt: updateInfo.TargetFullRelease.ReleasedAt,
                    Urgency: DetermineUrgency(updateInfo)
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Update check failed");
                return null;
            }
        }

        public async Task<UpdateProgress> DownloadUpdateAsync(
            UpdateInfo update, IProgress<UpdateProgress> progress, CancellationToken ct)
        {
            await _updateManager.DownloadUpdatesAsync(
                update,
                p => progress.Report(new UpdateProgress(
                    UpdatePhase.Downloading, p, 0, update.DownloadSize)));

            return new UpdateProgress(UpdatePhase.Ready, 100, update.DownloadSize, update.DownloadSize);
        }

        public async Task ApplyUpdateAsync(UpdateInfo update, CancellationToken ct)
        {
            _updateManager.ApplyUpdatesAndRestart(update);
        }
    }
    ```
*   **v0.9.3c:** **Background Update Scheduler.** Automatic update checking:
    *   Check for updates on startup (after 30-second delay).
    *   Periodic check every 4 hours while running.
    *   Download updates in background (low priority I/O).
    *   Store downloaded update for next restart.
    *   Respect user preference: "Auto-download", "Notify only", "Disabled".
    ```csharp
    public class UpdateScheduler(
        IUpdateService updateService,
        ISettingsService settings,
        ILogger<UpdateScheduler> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct); // Startup delay

            while (!ct.IsCancellationRequested)
            {
                if (settings.GetValue<bool>("Updates.AutoCheck"))
                {
                    await CheckAndDownloadAsync(ct);
                }
                await Task.Delay(TimeSpan.FromHours(4), ct);
            }
        }
    }
    ```
*   **v0.9.3d:** **Update Notification UI.** User-facing update controls:
    *   Toast notification when update available.
    *   Badge on Settings icon indicating pending update.
    *   Update panel in Settings: Version info, release notes, "Install Now" button.
    *   Progress bar during download.
    *   "Restart to Update" prompt with snooze option.
    *   Critical updates: Modal dialog recommending immediate restart.
    *   Release notes rendered as Markdown with collapsible sections.

---

## v0.9.4: The Zen Mode (Distraction-Free Writing)
**Goal:** Create an immersive writing experience that collapses all UI chrome and focuses on content.

*   **v0.9.4a:** **Zen Mode State.** Define focus mode behavior:
    ```csharp
    public interface IZenModeService
    {
        bool IsZenModeActive { get; }
        ZenModeSettings CurrentSettings { get; }
        Task EnterZenModeAsync(ZenModeSettings? settings = null);
        Task ExitZenModeAsync();
        void ToggleZenMode();
    }

    public record ZenModeSettings(
        bool HideStatusBar = true,
        bool HideTitleBar = false,
        bool FullScreen = false,
        ZenModeTheme Theme = ZenModeTheme.Auto,
        int TextWidthPercent = 70,
        bool TypewriterMode = false,
        bool PlayAmbientSound = false,
        string? AmbientSoundPath = null
    );

    public enum ZenModeTheme { Auto, Light, Dark, Sepia }
    ```
*   **v0.9.4b:** **UI State Preservation.** Save and restore layout:
    *   Capture current dock layout before entering Zen mode.
    *   Store panel visibility states (Co-pilot, Reference, Explorer).
    *   Restore exact layout when exiting.
    *   Handle edge case: Document closed while in Zen mode.
    ```csharp
    public class ZenModeService(
        IRegionManager regionManager,
        IDockLayoutService dockLayout,
        IThemeManager themeManager,
        ILogger<ZenModeService> logger) : IZenModeService
    {
        private DockLayoutSnapshot? _savedLayout;

        public async Task EnterZenModeAsync(ZenModeSettings? settings = null)
        {
            settings ??= new ZenModeSettings();

            // Save current state
            _savedLayout = await dockLayout.CaptureSnapshotAsync();

            // Collapse all panels
            await regionManager.CollapseAllRegionsAsync();

            // Apply Zen theme
            if (settings.Theme != ZenModeTheme.Auto)
            {
                await themeManager.ApplyThemeAsync(MapTheme(settings.Theme));
            }

            // Center editor with constrained width
            await ConfigureEditorForZenMode(settings);

            IsZenModeActive = true;
            await _mediator.Publish(new ZenModeEnteredEvent(settings));
        }
    }
    ```
*   **v0.9.4c:** **Typewriter Mode.** Keep current line vertically centered:
    *   Active line always at vertical center of viewport.
    *   Smooth scroll animation as user types.
    *   Dim lines above/below active line (configurable opacity).
    *   Disable for non-prose files (code, config).
    ```csharp
    public class TypewriterScrollBehavior : IEditorBehavior
    {
        public void OnCaretPositionChanged(CaretPositionChangedEventArgs e)
        {
            if (!_zenModeService.CurrentSettings.TypewriterMode) return;

            var lineTop = e.Editor.GetVisualLineTop(e.NewPosition.Line);
            var viewportCenter = e.Editor.ViewportHeight / 2;
            var targetScroll = lineTop - viewportCenter;

            e.Editor.AnimateScrollTo(targetScroll, duration: TimeSpan.FromMilliseconds(100));
        }
    }
    ```
*   **v0.9.4d:** **Zen Mode Controls.** Minimal UI for focus mode:
    *   `Escape` key exits Zen mode (with confirmation if unsaved).
    *   Floating toolbar appears on mouse move to top (auto-hides after 2s).
    *   Toolbar actions: Exit, Word count, Time spent, Quick save.
    *   Keyboard shortcut to toggle: `Ctrl+Shift+Z` (configurable).
    *   Session statistics: Words written, time in focus, streak indicator.

---

## v0.9.5: The Startup Optimizer (Performance Polish)
**Goal:** Ensure application startup in under 2 seconds through lazy loading and initialization optimization.

*   **v0.9.5a:** **Startup Profiling.** Instrument initialization timeline:
    ```csharp
    public interface IStartupProfiler
    {
        void BeginPhase(string phaseName);
        void EndPhase(string phaseName);
        StartupProfile GetProfile();
    }

    public record StartupProfile(
        TimeSpan TotalDuration,
        IReadOnlyList<StartupPhase> Phases,
        IReadOnlyList<string> CriticalPath
    );

    public record StartupPhase(string Name, TimeSpan Duration, TimeSpan StartOffset);
    ```
    *   Measure: Host init, DB connection, module loading, UI render.
    *   Log startup profile to Serilog for analysis.
    *   Target breakdown: Host < 500ms, Modules < 1000ms, UI < 500ms.
*   **v0.9.5b:** **Lazy Module Loading.** Defer non-essential modules:
    ```csharp
    public interface ILazyModule : IModule
    {
        ModuleLoadPriority Priority { get; }
        bool LoadOnDemand { get; }
        IEnumerable<string> TriggerCommands { get; } // Commands that trigger load
    }

    public enum ModuleLoadPriority { Critical, High, Normal, Low, Background }
    ```
    *   **Critical (sync):** Host, Abstractions, Licensing.
    *   **High (async, parallel):** Editor, Explorer, Settings.
    *   **Normal (deferred):** RAG, Agents, Style.
    *   **Low (on-demand):** Publisher, Git integration.
    *   Background queue processes Low priority modules after UI ready.
*   **v0.9.5c:** **Database Connection Pooling.** Optimize DB initialization:
    *   Pre-warm connection pool during splash screen.
    *   Minimum pool size: 2 connections.
    *   Maximum pool size: 10 connections.
    *   Connection timeout: 5 seconds.
    *   Health check query on startup (simple SELECT).
    ```csharp
    public class OptimizedConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
    {
        private readonly NpgsqlDataSource _dataSource = new NpgsqlDataSourceBuilder(options.Value.ConnectionString)
        {
            MinPoolSize = 2,
            MaxPoolSize = 10,
            ConnectionIdleLifetime = TimeSpan.FromMinutes(5)
        }.Build();

        public async Task WarmupAsync(CancellationToken ct)
        {
            // Pre-create minimum connections
            var warmupTasks = Enumerable.Range(0, 2)
                .Select(_ => _dataSource.OpenConnectionAsync(ct));
            await Task.WhenAll(warmupTasks);
        }
    }
    ```
*   **v0.9.5d:** **UI Render Optimization.** Accelerate first paint:
    *   Splash screen displayed within 200ms of launch.
    *   Main window created but hidden during initialization.
    *   Shell layout loaded from cached XAML (avoid recompilation).
    *   Defer loading of complex controls (charts, syntax highlighter).
    *   Virtualize all lists from first render.
    *   Profile and eliminate layout thrashing.

---

## v0.9.6: The PII Scrubber (Security Safeguard)
**Goal:** Prevent accidental exposure of sensitive information in LLM prompts.

*   **v0.9.6a:** **PII Detection Engine.** Implement pattern-based detection:
    ```csharp
    public interface IPiiDetector
    {
        PiiDetectionResult Detect(string text);
        string Redact(string text, PiiRedactionOptions options);
    }

    public record PiiDetectionResult(
        bool ContainsPii,
        IReadOnlyList<PiiMatch> Matches
    );

    public record PiiMatch(
        PiiType Type,
        string Value,
        int StartIndex,
        int Length,
        float Confidence
    );

    public enum PiiType
    {
        CreditCard,
        SocialSecurityNumber,
        EmailAddress,
        PhoneNumber,
        IpAddress,
        ApiKey,
        Password,
        BankAccount,
        PassportNumber,
        DriversLicense
    }
    ```
*   **v0.9.6b:** **Detection Patterns.** Implement regex-based matchers:
    ```csharp
    public class RegexPiiDetector : IPiiDetector
    {
        private static readonly Dictionary<PiiType, Regex> Patterns = new()
        {
            [PiiType.CreditCard] = new Regex(
                @"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|6(?:011|5[0-9]{2})[0-9]{12})\b",
                RegexOptions.Compiled),
            [PiiType.SocialSecurityNumber] = new Regex(
                @"\b(?!000|666|9\d{2})\d{3}-(?!00)\d{2}-(?!0000)\d{4}\b",
                RegexOptions.Compiled),
            [PiiType.IpAddress] = new Regex(
                @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                RegexOptions.Compiled),
            [PiiType.ApiKey] = new Regex(
                @"\b(?:sk-[a-zA-Z0-9]{32,}|AKIA[0-9A-Z]{16}|ghp_[a-zA-Z0-9]{36})\b",
                RegexOptions.Compiled),
            [PiiType.EmailAddress] = new Regex(
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                RegexOptions.Compiled),
            [PiiType.PhoneNumber] = new Regex(
                @"\b(?:\+?1[-.\s]?)?\(?[2-9]\d{2}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b",
                RegexOptions.Compiled)
        };

        public PiiDetectionResult Detect(string text)
        {
            var matches = new List<PiiMatch>();
            foreach (var (type, regex) in Patterns)
            {
                foreach (Match match in regex.Matches(text))
                {
                    matches.Add(new PiiMatch(type, match.Value, match.Index, match.Length, 1.0f));
                }
            }
            return new PiiDetectionResult(matches.Count > 0, matches);
        }
    }
    ```
*   **v0.9.6c:** **LLM Gateway Integration.** Intercept prompts before sending:
    ```csharp
    public class PiiSafetyFilter(
        IPiiDetector detector,
        ISettingsService settings,
        ILogger<PiiSafetyFilter> logger) : IChatCompletionMiddleware
    {
        public async Task<ChatRequest> ProcessRequestAsync(ChatRequest request, CancellationToken ct)
        {
            var piiSettings = settings.GetValue<PiiFilterSettings>("Security.PiiFilter");
            if (!piiSettings.Enabled) return request;

            foreach (var message in request.Messages)
            {
                var result = detector.Detect(message.Content);
                if (result.ContainsPii)
                {
                    logger.LogWarning("PII detected in prompt: {Types}",
                        string.Join(", ", result.Matches.Select(m => m.Type)));

                    if (piiSettings.BlockOnDetection)
                    {
                        throw new PiiDetectedException(result.Matches);
                    }

                    if (piiSettings.AutoRedact)
                    {
                        message = message with { Content = detector.Redact(message.Content, piiSettings.RedactionOptions) };
                    }
                }
            }

            return request;
        }
    }
    ```
*   **v0.9.6d:** **PII Warning UI.** Alert users to detected sensitive data:
    *   Warning banner in chat panel when PII detected.
    *   List detected items with "Reveal" toggle (masked by default).
    *   Actions: "Send Anyway", "Redact and Send", "Cancel".
    *   Checkbox: "Don't warn for emails" (per-type suppression).
    *   Settings panel: Configure enabled patterns, auto-redact behavior.
    *   Audit log: Record PII detection events (without actual values).

---

## v0.9.7: The Final Polish (UX Refinement)
**Goal:** Address accumulated UX debt and ensure consistent, delightful user experience.

*   **v0.9.7a:** **Keyboard Navigation Audit.** Ensure full keyboard accessibility:
    *   Tab order logical in all dialogs.
    *   Focus indicators visible on all interactive elements.
    *   `Escape` closes modals and dialogs.
    *   `Enter` activates default button.
    *   Arrow keys navigate lists and trees.
    *   Document all keyboard shortcuts in Help.
    *   Implement keyboard shortcut overlay (`Ctrl+/`).
    ```csharp
    public class KeyboardShortcutOverlay
    {
        public void Show()
        {
            var shortcuts = _keyBindingService.GetAllBindings();
            var grouped = shortcuts.GroupBy(s => s.Category);
            // Display overlay with all shortcuts
        }
    }
    ```
*   **v0.9.7b:** **Error Message Improvements.** User-friendly error handling:
    *   Replace technical errors with actionable messages.
    *   Include "What happened" + "What to do" in all errors.
    *   Add "Copy Error Details" for support requests.
    *   Implement error recovery suggestions where possible.
    *   Log correlation IDs for support tracing.
    ```csharp
    public record UserFriendlyError(
        string Title,
        string Description,
        string? RecoveryAction,
        string? TechnicalDetails,
        string CorrelationId
    );

    public class ErrorMessageService
    {
        public UserFriendlyError Translate(Exception ex) => ex switch
        {
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } =>
                new UserFriendlyError(
                    "Rate Limit Reached",
                    "The AI service is temporarily busy. Your request will be retried automatically.",
                    "Wait a moment or try again later.",
                    ex.Message,
                    Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                ),
            NpgsqlException =>
                new UserFriendlyError(
                    "Database Connection Issue",
                    "Unable to connect to the local database.",
                    "Ensure PostgreSQL is running and restart Lexichord.",
                    ex.Message,
                    Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()
                ),
            _ => // Generic fallback
        };
    }
    ```
*   **v0.9.7c:** **Loading States.** Consistent progress indication:
    *   Skeleton screens for content areas.
    *   Spinner with label for operations > 500ms.
    *   Progress bars for determinate operations.
    *   Cancel buttons for long operations where possible.
    *   Prevent double-clicks on action buttons.
    *   Optimistic UI updates where safe.
*   **v0.9.7d:** **Onboarding Flow.** First-run experience:
    *   Welcome dialog with Lexichord overview.
    *   Step-by-step: License activation → Profile creation → API key setup.
    *   Optional: Import existing style guide.
    *   Quick tour highlighting key features (tooltips).
    *   "Sample Project" option to explore with demo content.
    *   Skip option for experienced users.

---

## v0.9.8: The Hardening (Release Candidate)
**Goal:** Final testing, security audit, and release preparation for v1.0.

*   **v0.9.8a:** **Security Audit.** Comprehensive security review:
    *   Audit all `ISecureVault` usage for proper encryption.
    *   Verify license validation cannot be bypassed.
    *   Check for SQL injection in all Dapper queries.
    *   Ensure PII scrubber covers all LLM pathways.
    *   Validate update package signatures.
    *   Review dependency vulnerabilities (Dependabot/Snyk).
    *   Penetration testing of license server communication.
*   **v0.9.8b:** **Performance Regression Tests.** Automated benchmarks:
    ```csharp
    [MemoryDiagnoser]
    public class StartupBenchmarks
    {
        [Benchmark]
        public async Task ColdStart()
        {
            // Measure full startup time
            var app = new LexichordApplication();
            await app.StartAsync();
            await app.WaitForReadyAsync();
        }

        [Benchmark]
        public async Task WarmStart()
        {
            // Measure with cached module assemblies
        }
    }
    ```
    *   Startup time: < 2 seconds (cold), < 1 second (warm).
    *   Memory usage at idle: < 200 MB.
    *   Document open time (1000 lines): < 500ms.
    *   Search query (50K chunks): < 300ms.
    *   Agent response (first token): < 1 second.
*   **v0.9.8c:** **Integration Test Suite.** End-to-end workflow tests:
    *   Full user journey: Install → Activate → Create document → Use agent → Export.
    *   Profile switching: Verify settings isolation.
    *   Update flow: Download → Verify → Install → Restart.
    *   Offline mode: Verify graceful degradation.
    *   Error recovery: Crash → Restart → Document recovery.
    *   Cross-platform: Windows, macOS, Linux verification.
*   **v0.9.8d:** **Release Preparation.** Final v1.0 checklist:
    *   Freeze feature development (code freeze).
    *   Update all user-facing strings for consistency.
    *   Finalize CHANGELOG.md with all v0.x changes.
    *   Prepare release notes and marketing copy.
    *   Create installer packages for all platforms.
    *   Set up crash reporting (Sentry integration).
    *   Configure analytics opt-in flow.
    *   Prepare support documentation and FAQ.

---

## Dependencies on Prior Versions

| Component | Source Version | Usage in v0.9.x |
|:----------|:---------------|:----------------|
| `ISecureVault` | v0.0.6a | Store profiles, licenses, API keys |
| `ILicenseContext` | v0.0.4c | Read license state for feature gating |
| `IMediator` | v0.0.7a | Publish profile and license events |
| `IRegionManager` | v0.1.1b | Zen mode panel collapse |
| `ISettingsService` | v0.1.6a | Store user preferences per profile |
| `IThemeManager` | v0.0.2c | Zen mode theme switching |
| `IKeyBindingService` | v0.1.5b | Zen mode and shortcut overlay |
| `IChatCompletionService` | v0.6.1a | PII filter middleware integration |
| `IDockLayoutService` | v0.1.1a | Save/restore layout for Zen mode |
| `IEditorService` | v0.1.3a | Typewriter mode integration |
| `Polly` | v0.0.5d | Update download retry policy |

---

## MediatR Events Introduced

| Event | Description |
|:------|:------------|
| `ProfileCreatedEvent` | New user profile created |
| `ProfileSwitchingEvent` | Profile switch initiated (save state) |
| `ProfileSwitchedEvent` | Profile switch completed |
| `ProfileDeletedEvent` | User profile deleted |
| `LicenseActivatedEvent` | License key validated and activated |
| `LicenseDeactivatedEvent` | License deactivated for transfer |
| `LicenseExpiredEvent` | License expiration detected |
| `UpdateAvailableEvent` | New version discovered |
| `UpdateDownloadedEvent` | Update package ready to install |
| `UpdateAppliedEvent` | Update installed successfully |
| `ZenModeEnteredEvent` | User entered focus mode |
| `ZenModeExitedEvent` | User exited focus mode |
| `PiiDetectedEvent` | Sensitive data found in prompt |
| `PiiRedactedEvent` | PII automatically removed |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Velopack` | 0.0.x | Auto-update framework |
| `NSec.Cryptography` | 24.x | Ed25519 signature verification |
| `BenchmarkDotNet` | 0.14.x | Performance benchmarking (dev only) |

---

## License Gating Summary

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| Single user profile | ✓ | ✓ | ✓ | ✓ |
| Multiple profiles | — | ✓ | ✓ | ✓ |
| Profile export/import | — | ✓ | ✓ | ✓ |
| License activation | ✓ | ✓ | ✓ | ✓ |
| Auto-updates | ✓ | ✓ | ✓ | ✓ |
| Zen mode | ✓ | ✓ | ✓ | ✓ |
| Typewriter mode | — | ✓ | ✓ | ✓ |
| PII detection | ✓ | ✓ | ✓ | ✓ |
| PII auto-redaction | — | ✓ | ✓ | ✓ |
| Usage analytics | — | — | ✓ | ✓ |

---

## Implementation Guide: Sample Workflow for v0.9.2b (License Validation)

**LCS-01 (Design Composition)**
*   **Algorithm:** Ed25519 digital signature verification.
*   **Format:** Base64-encoded JSON payload + signature.
*   **Offline:** Full validation possible without network.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Valid license with future expiry. Assert `IsValid = true`.
*   **Test:** Valid license with past expiry. Assert `Error = Expired`.
*   **Test:** Invalid signature. Assert `Error = InvalidSignature`.
*   **Test:** Malformed license string. Assert `Error = InvalidFormat`.

**LCS-03 (Performance Log)**
1.  **Implement `Ed25519LicenseValidator`** (see v0.9.2b code block above).
2.  **License format:**
    ```
    Base64(JSON_PAYLOAD + "." + Ed25519_SIGNATURE)

    JSON_PAYLOAD = {
        "kid": "license-key-id",
        "tier": "Teams",
        "type": "Subscription",
        "name": "Acme Corp",
        "email": "admin@acme.com",
        "iat": 1704067200,
        "exp": 1735689600,
        "seats": 10,
        "features": ["rag", "agents", "publisher"]
    }
    ```
3.  **Register:** Add to DI in `HostModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<ILicenseValidator, Ed25519LicenseValidator>();
    services.AddSingleton<ILicenseStateService, LicenseStateService>();
    ```

---

## Implementation Guide: Sample Workflow for v0.9.4b (Zen Mode)

**LCS-01 (Design Composition)**
*   **Service:** `IZenModeService` for focus mode orchestration.
*   **State:** Capture layout snapshot, collapse panels, apply theme.
*   **Restoration:** Exact layout restored on exit.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Enter Zen mode. Assert all panels collapsed.
*   **Test:** Exit Zen mode. Assert original layout restored.
*   **Test:** Enter with custom theme. Assert theme applied.
*   **Test:** Toggle typewriter mode. Assert caret stays centered.

**LCS-03 (Performance Log)**
1.  **Implement `ZenModeService`:**
    ```csharp
    public class ZenModeService(
        IRegionManager regionManager,
        IDockLayoutService dockLayout,
        IThemeManager themeManager,
        IEditorService editorService,
        IMediator mediator,
        ILogger<ZenModeService> logger) : IZenModeService
    {
        private DockLayoutSnapshot? _savedLayout;
        private string? _savedTheme;

        public bool IsZenModeActive { get; private set; }
        public ZenModeSettings CurrentSettings { get; private set; } = new();

        public async Task EnterZenModeAsync(ZenModeSettings? settings = null)
        {
            if (IsZenModeActive) return;

            settings ??= new ZenModeSettings();
            CurrentSettings = settings;

            logger.LogInformation("Entering Zen mode with settings: {@Settings}", settings);

            // 1. Save current state
            _savedLayout = await dockLayout.CaptureSnapshotAsync();
            _savedTheme = themeManager.CurrentTheme;

            // 2. Collapse all panels
            await regionManager.CollapseRegionAsync(ShellRegion.Left);
            await regionManager.CollapseRegionAsync(ShellRegion.Right);
            await regionManager.CollapseRegionAsync(ShellRegion.Bottom);

            if (settings.HideStatusBar)
                await regionManager.CollapseRegionAsync(ShellRegion.StatusBar);

            // 3. Apply theme
            if (settings.Theme != ZenModeTheme.Auto)
            {
                await themeManager.ApplyThemeAsync(settings.Theme.ToString());
            }

            // 4. Configure editor
            await editorService.SetTextWidthAsync(settings.TextWidthPercent);
            if (settings.TypewriterMode)
            {
                await editorService.EnableTypewriterModeAsync();
            }

            // 5. Fullscreen if requested
            if (settings.FullScreen)
            {
                // Platform-specific fullscreen
            }

            IsZenModeActive = true;
            await mediator.Publish(new ZenModeEnteredEvent(settings));
        }

        public async Task ExitZenModeAsync()
        {
            if (!IsZenModeActive) return;

            logger.LogInformation("Exiting Zen mode");

            // Restore layout
            if (_savedLayout is not null)
            {
                await dockLayout.RestoreSnapshotAsync(_savedLayout);
            }

            // Restore theme
            if (_savedTheme is not null)
            {
                await themeManager.ApplyThemeAsync(_savedTheme);
            }

            // Reset editor
            await editorService.SetTextWidthAsync(100);
            await editorService.DisableTypewriterModeAsync();

            IsZenModeActive = false;
            CurrentSettings = new();
            await mediator.Publish(new ZenModeExitedEvent());
        }
    }
    ```
2.  **Register:** Add to DI in `HostModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IZenModeService, ZenModeService>();
    ```

---

## Implementation Guide: Sample Workflow for v0.9.6c (PII Filter)

**LCS-01 (Design Composition)**
*   **Middleware:** `IChatCompletionMiddleware` intercepts all LLM requests.
*   **Detection:** Regex-based pattern matching for known PII types.
*   **Action:** Block, redact, or warn based on user settings.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Prompt with credit card number. Assert detection.
*   **Test:** Prompt with SSN. Assert detection.
*   **Test:** Auto-redact enabled. Assert PII replaced with `[REDACTED]`.
*   **Test:** Block mode. Assert `PiiDetectedException` thrown.

**LCS-03 (Performance Log)**
1.  **Implement `PiiSafetyFilter`** (see v0.9.6c code block above).
2.  **Redaction implementation:**
    ```csharp
    public string Redact(string text, PiiRedactionOptions options)
    {
        var result = text;
        var matches = Detect(text).Matches.OrderByDescending(m => m.StartIndex);

        foreach (var match in matches)
        {
            var replacement = options.RedactionStyle switch
            {
                RedactionStyle.Full => "[REDACTED]",
                RedactionStyle.Partial => PartialRedact(match),
                RedactionStyle.TypeLabeled => $"[{match.Type}]",
                _ => "[REDACTED]"
            };

            result = result.Remove(match.StartIndex, match.Length).Insert(match.StartIndex, replacement);
        }

        return result;
    }

    private string PartialRedact(PiiMatch match) => match.Type switch
    {
        PiiType.CreditCard => $"****-****-****-{match.Value[^4..]}",
        PiiType.EmailAddress => $"{match.Value[0]}***@***",
        PiiType.PhoneNumber => $"***-***-{match.Value[^4..]}",
        _ => "[REDACTED]"
    };
    ```
3.  **Register:** Add to DI in `AgentsModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IPiiDetector, RegexPiiDetector>();
    services.AddSingleton<IChatCompletionMiddleware, PiiSafetyFilter>();
    ```
