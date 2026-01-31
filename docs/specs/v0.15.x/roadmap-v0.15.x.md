# Lexichord Agent Marketplace & Extensibility Roadmap (v0.15.1 - v0.15.5)

In v0.14.x, we delivered **The Agent Studio** — a visual environment for designing, debugging, and monitoring agent systems. In v0.15.x, we complete the agentic platform with **The Marketplace & Extensibility Layer** — enabling community-driven agent development, sharing, and a thriving ecosystem of plugins and integrations.

**Architectural Note:** This version introduces `Lexichord.Marketplace` and `Lexichord.Extensibility` modules, creating an open ecosystem while maintaining security and quality standards. This represents the transition from "application" to "platform."

**Total Sub-Parts:** 35 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 258 hours (~6.5 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.15.1-MKT | Plugin Architecture | Plugin SDK, sandboxing, lifecycle | 54 |
| v0.15.2-MKT | Agent Packaging | Agent bundles, versioning, dependencies | 50 |
| v0.15.3-MKT | Marketplace Platform | Discovery, ratings, distribution | 52 |
| v0.15.4-MKT | Integration Hub | Connectors, webhooks, external APIs | 50 |
| v0.15.5-MKT | Enterprise Extensions | Private registries, governance, compliance | 52 |

---

## Marketplace Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Marketplace & Extensibility Layer                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                      Public Marketplace (v0.15.3)                       │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐               │ │
│  │  │  Agents  │  │  Plugins │  │  Prompts │  │ Patterns │               │ │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘               │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│           ┌────────────────────────┴────────────────────────┐               │
│           ▼                                                  ▼               │
│  ┌─────────────────────┐                      ┌─────────────────────┐       │
│  │  Plugin Architecture│                      │ Enterprise Registry │       │
│  │     (v0.15.1)       │                      │     (v0.15.5)       │       │
│  │                     │                      │                     │       │
│  │ • Plugin SDK        │                      │ • Private catalog   │       │
│  │ • Sandboxing        │                      │ • Approval workflow │       │
│  │ • Hot reload        │                      │ • Compliance scan   │       │
│  └─────────────────────┘                      └─────────────────────┘       │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────┐         ┌─────────────────────┐                   │
│  │  Agent Packaging    │         │  Integration Hub    │                   │
│  │     (v0.15.2)       │         │     (v0.15.4)       │                   │
│  │                     │         │                     │                   │
│  │ • Bundle format     │         │ • Connectors        │                   │
│  │ • Dependencies      │         │ • Webhooks          │                   │
│  │ • Signing           │         │ • OAuth providers   │                   │
│  └─────────────────────┘         └─────────────────────┘                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.15.1-MKT: Plugin Architecture

**Goal:** Establish a robust plugin system with SDK, sandboxing, and lifecycle management that enables safe third-party extensions.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.15.1e | Plugin SDK | 12 |
| v0.15.1f | Plugin Sandbox | 12 |
| v0.15.1g | Lifecycle Manager | 10 |
| v0.15.1h | Plugin Configuration | 6 |
| v0.15.1i | Hot Reload | 8 |
| v0.15.1j | Plugin Manager UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Base interface for all Lexichord plugins.
/// </summary>
public interface IPlugin
{
    PluginManifest Manifest { get; }

    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task ActivateAsync(CancellationToken ct = default);
    Task DeactivateAsync(CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}

/// <summary>
/// Plugin manifest describing metadata and capabilities.
/// </summary>
public record PluginManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? License { get; init; }
    public Uri? Homepage { get; init; }
    public Uri? Repository { get; init; }

    // What this plugin provides
    public IReadOnlyList<PluginCapability> Capabilities { get; init; } = [];

    // What this plugin requires
    public PluginRequirements Requirements { get; init; } = new();

    // Entry points
    public IReadOnlyList<PluginEntryPoint> EntryPoints { get; init; } = [];

    // Security
    public IReadOnlyList<PluginPermission> RequestedPermissions { get; init; } = [];
    public string? Signature { get; init; }
}

public record PluginCapability
{
    public required PluginCapabilityType Type { get; init; }
    public required string Id { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }
}

public enum PluginCapabilityType
{
    Agent,              // Provides agents
    Tool,               // Provides tools
    NodeType,           // Custom canvas nodes
    Integration,        // External service integration
    Theme,              // UI theme
    Language,           // Language support
    Exporter,           // Export format
    Importer            // Import format
}

public record PluginRequirements
{
    public string? MinLexichordVersion { get; init; }
    public string? MaxLexichordVersion { get; init; }
    public IReadOnlyList<PluginDependency> Dependencies { get; init; } = [];
    public IReadOnlyList<string> RequiredModules { get; init; } = [];
}

public record PluginDependency
{
    public required string PluginId { get; init; }
    public string? MinVersion { get; init; }
    public string? MaxVersion { get; init; }
    public bool Optional { get; init; }
}

/// <summary>
/// Permissions that plugins can request.
/// </summary>
public enum PluginPermission
{
    // File system
    ReadFiles,
    WriteFiles,
    DeleteFiles,

    // Network
    NetworkAccess,
    WebhookReceive,

    // Lexichord resources
    ReadKnowledge,
    WriteKnowledge,
    ReadDocuments,
    WriteDocuments,
    ReadSettings,
    WriteSettings,

    // Agent system
    SpawnAgents,
    SendMessages,
    RegisterTools,

    // UI
    RegisterMenuItems,
    RegisterPanels,
    ShowNotifications,

    // Sensitive
    AccessCredentials,
    ExecuteCode,
    SystemShell
}

/// <summary>
/// Context provided to plugins during initialization.
/// </summary>
public interface IPluginContext
{
    PluginManifest Manifest { get; }
    string DataDirectory { get; }  // Plugin-specific storage

    // Services
    IServiceProvider Services { get; }
    ILogger Logger { get; }
    IPluginSettings Settings { get; }

    // Inter-plugin communication
    Task<object?> CallPluginAsync(
        string pluginId,
        string method,
        object? args = null,
        CancellationToken ct = default);

    // Event subscription
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}

/// <summary>
/// Manages plugin lifecycle.
/// </summary>
public interface IPluginManager
{
    Task<IReadOnlyList<InstalledPlugin>> GetInstalledAsync(
        CancellationToken ct = default);

    Task<InstallResult> InstallAsync(
        PluginSource source,
        InstallOptions options,
        CancellationToken ct = default);

    Task<UninstallResult> UninstallAsync(
        string pluginId,
        CancellationToken ct = default);

    Task EnableAsync(string pluginId, CancellationToken ct = default);
    Task DisableAsync(string pluginId, CancellationToken ct = default);

    Task<UpdateCheckResult> CheckForUpdatesAsync(
        CancellationToken ct = default);

    Task<UpdateResult> UpdateAsync(
        string pluginId,
        string? targetVersion = null,
        CancellationToken ct = default);

    IObservable<PluginEvent> Events { get; }
}

public record InstalledPlugin
{
    public required string Id { get; init; }
    public required PluginManifest Manifest { get; init; }
    public PluginState State { get; init; }
    public DateTimeOffset InstalledAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public PluginSource Source { get; init; } = null!;
    public IReadOnlyList<PluginPermission> GrantedPermissions { get; init; } = [];
}

public enum PluginState
{
    Installed, Active, Disabled, Error, Updating
}

/// <summary>
/// Plugin sandbox for security isolation.
/// </summary>
public interface IPluginSandbox
{
    Task<SandboxedPlugin> CreateSandboxAsync(
        InstalledPlugin plugin,
        SandboxOptions options,
        CancellationToken ct = default);

    Task<object?> ExecuteAsync(
        SandboxedPlugin plugin,
        string method,
        object?[] args,
        CancellationToken ct = default);

    Task TerminateAsync(
        SandboxedPlugin plugin,
        CancellationToken ct = default);
}

public record SandboxOptions
{
    public long MaxMemoryBytes { get; init; } = 256 * 1024 * 1024;  // 256MB
    public TimeSpan MaxCpuTime { get; init; } = TimeSpan.FromMinutes(5);
    public bool AllowNetworkAccess { get; init; }
    public IReadOnlyList<string>? AllowedHosts { get; init; }
    public bool AllowFileSystem { get; init; }
    public IReadOnlyList<string>? AllowedPaths { get; init; }
}
```

### Plugin Development Workflow

```
1. Create plugin project using SDK
   $ dotnet new lexichord-plugin -n MyPlugin

2. Implement IPlugin interface
   public class MyPlugin : IPlugin { ... }

3. Define manifest.json
   {
     "id": "com.example.myplugin",
     "name": "My Plugin",
     "version": "1.0.0",
     "capabilities": [...]
   }

4. Package plugin
   $ dotnet lexichord pack

5. Publish to marketplace (or install locally)
   $ dotnet lexichord publish
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | 2 plugins |
| WriterPro | 10 plugins |
| Teams | Unlimited + private plugins |
| Enterprise | + Custom SDK + unsigned |

---

## v0.15.2-MKT: Agent Packaging

**Goal:** Define a standard format for packaging, distributing, and versioning agents with their dependencies.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.15.2e | Bundle Format | 10 |
| v0.15.2f | Dependency Resolver | 10 |
| v0.15.2g | Package Builder | 8 |
| v0.15.2h | Signing & Verification | 10 |
| v0.15.2i | Version Manager | 6 |
| v0.15.2j | Package Explorer UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// An agent package bundle.
/// </summary>
public record AgentPackage
{
    public required string PackageId { get; init; }
    public required string Name { get; init; }
    public required SemanticVersion Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? License { get; init; }

    // Contents
    public required AgentManifest AgentManifest { get; init; }
    public IReadOnlyList<PromptTemplate>? Prompts { get; init; }
    public IReadOnlyList<ToolDefinition>? Tools { get; init; }
    public IReadOnlyList<WorkflowDefinition>? Workflows { get; init; }
    public IReadOnlyDictionary<string, byte[]>? Resources { get; init; }

    // Dependencies
    public IReadOnlyList<PackageDependency> Dependencies { get; init; } = [];

    // Distribution
    public PackageSignature? Signature { get; init; }
    public PackageMetadata Metadata { get; init; } = new();
}

public record SemanticVersion(int Major, int Minor, int Patch, string? Prerelease = null)
{
    public override string ToString() =>
        Prerelease is null
            ? $"{Major}.{Minor}.{Patch}"
            : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    public static SemanticVersion Parse(string version) { /* ... */ }

    public bool SatisfiesRange(VersionRange range) { /* ... */ }
}

public record PackageDependency
{
    public required string PackageId { get; init; }
    public required VersionRange VersionRange { get; init; }
    public bool Optional { get; init; }
}

public record VersionRange
{
    public SemanticVersion? MinVersion { get; init; }
    public SemanticVersion? MaxVersion { get; init; }
    public bool MinInclusive { get; init; } = true;
    public bool MaxInclusive { get; init; }

    public static VersionRange Parse(string range) { /* ^1.0.0, >=1.0.0 <2.0.0, etc. */ }
}

/// <summary>
/// Builds agent packages.
/// </summary>
public interface IPackageBuilder
{
    Task<AgentPackage> BuildAsync(
        PackageBuildContext context,
        CancellationToken ct = default);

    Task<Stream> PackAsync(
        AgentPackage package,
        PackOptions options,
        CancellationToken ct = default);

    Task<AgentPackage> UnpackAsync(
        Stream packageStream,
        CancellationToken ct = default);
}

public record PackageBuildContext
{
    public required string SourceDirectory { get; init; }
    public string? OutputPath { get; init; }
    public bool IncludeDebugInfo { get; init; }
    public IReadOnlyDictionary<string, string>? Variables { get; init; }
}

public record PackOptions
{
    public CompressionLevel Compression { get; init; } = CompressionLevel.Optimal;
    public bool Sign { get; init; } = true;
    public SigningCredentials? Credentials { get; init; }
}

/// <summary>
/// Resolves package dependencies.
/// </summary>
public interface IDependencyResolver
{
    Task<DependencyGraph> ResolveAsync(
        IReadOnlyList<PackageDependency> dependencies,
        ResolveOptions options,
        CancellationToken ct = default);

    Task<IReadOnlyList<PackageConflict>> DetectConflictsAsync(
        DependencyGraph graph,
        CancellationToken ct = default);
}

public record DependencyGraph
{
    public IReadOnlyList<ResolvedPackage> Packages { get; init; } = [];
    public IReadOnlyList<DependencyEdge> Edges { get; init; } = [];
    public bool HasConflicts { get; init; }
}

public record ResolvedPackage
{
    public required string PackageId { get; init; }
    public required SemanticVersion Version { get; init; }
    public required PackageSource Source { get; init; }
    public bool IsDirect { get; init; }  // vs transitive
}

/// <summary>
/// Signs and verifies packages.
/// </summary>
public interface IPackageSigner
{
    Task<PackageSignature> SignAsync(
        AgentPackage package,
        SigningCredentials credentials,
        CancellationToken ct = default);

    Task<SignatureVerification> VerifyAsync(
        AgentPackage package,
        VerificationOptions options,
        CancellationToken ct = default);
}

public record PackageSignature
{
    public required string Algorithm { get; init; }  // e.g., "ed25519"
    public required byte[] Signature { get; init; }
    public required string SignedBy { get; init; }
    public DateTimeOffset SignedAt { get; init; }
    public string? CertificateChain { get; init; }
}

public record SignatureVerification
{
    public bool IsValid { get; init; }
    public TrustLevel TrustLevel { get; init; }
    public string? SignedBy { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public enum TrustLevel
{
    Unknown,    // No signature
    Untrusted,  // Invalid or unknown signer
    Community,  // Community-signed
    Verified,   // Marketplace-verified
    Official    // Lexichord official
}

/// <summary>
/// Manages installed package versions.
/// </summary>
public interface IPackageVersionManager
{
    Task<IReadOnlyList<InstalledPackageVersion>> GetInstalledVersionsAsync(
        string packageId,
        CancellationToken ct = default);

    Task SetActiveVersionAsync(
        string packageId,
        SemanticVersion version,
        CancellationToken ct = default);

    Task<SemanticVersion?> GetActiveVersionAsync(
        string packageId,
        CancellationToken ct = default);

    Task PruneOldVersionsAsync(
        string packageId,
        int keepCount = 3,
        CancellationToken ct = default);
}
```

### Package Structure

```
my-agent-1.0.0.lxpkg
├── manifest.json           # Package metadata
├── agent.json             # Agent manifest
├── prompts/
│   ├── main.prompt.md
│   └── helper.prompt.md
├── tools/
│   └── custom-tool.json
├── workflows/
│   └── default.workflow.json
├── resources/
│   └── examples/
├── LICENSE
├── README.md
└── SIGNATURE              # Package signature
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Install only |
| WriterPro | + Package building |
| Teams | + Publishing + signing |
| Enterprise | + Private registry |

---

## v0.15.3-MKT: Marketplace Platform

**Goal:** Build the discovery, rating, and distribution platform for sharing agents, plugins, and patterns.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.15.3e | Catalog Service | 12 |
| v0.15.3f | Search & Discovery | 10 |
| v0.15.3g | Rating System | 8 |
| v0.15.3h | Publisher Portal | 10 |
| v0.15.3i | Download Manager | 6 |
| v0.15.3j | Marketplace UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Marketplace catalog for browsing and discovering content.
/// </summary>
public interface IMarketplaceCatalog
{
    Task<CatalogSearchResult> SearchAsync(
        CatalogSearchQuery query,
        CancellationToken ct = default);

    Task<MarketplaceItem?> GetItemAsync(
        string itemId,
        CancellationToken ct = default);

    Task<IReadOnlyList<MarketplaceItem>> GetFeaturedAsync(
        int limit = 10,
        CancellationToken ct = default);

    Task<IReadOnlyList<MarketplaceItem>> GetTrendingAsync(
        TimeRange period,
        int limit = 10,
        CancellationToken ct = default);

    Task<IReadOnlyList<MarketplaceCategory>> GetCategoriesAsync(
        CancellationToken ct = default);
}

public record CatalogSearchQuery
{
    public string? Query { get; init; }
    public MarketplaceItemType? Type { get; init; }
    public IReadOnlyList<string>? Categories { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public float? MinRating { get; init; }
    public TrustLevel? MinTrustLevel { get; init; }
    public SortOrder Sort { get; init; } = SortOrder.Relevance;
    public int Skip { get; init; }
    public int Take { get; init; } = 20;
}

public enum MarketplaceItemType
{
    Agent, Plugin, Prompt, Pattern, Workflow, Theme, Integration
}

public enum SortOrder
{
    Relevance, Downloads, Rating, Recent, Name
}

public record CatalogSearchResult
{
    public IReadOnlyList<MarketplaceItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public IReadOnlyList<SearchFacet> Facets { get; init; } = [];
}

/// <summary>
/// An item in the marketplace.
/// </summary>
public record MarketplaceItem
{
    public required string Id { get; init; }
    public required MarketplaceItemType Type { get; init; }
    public required string Name { get; init; }
    public required string ShortDescription { get; init; }
    public string? LongDescription { get; init; }
    public required SemanticVersion LatestVersion { get; init; }
    public required PublisherInfo Publisher { get; init; }

    // Metrics
    public int Downloads { get; init; }
    public float Rating { get; init; }
    public int RatingCount { get; init; }

    // Trust & verification
    public TrustLevel TrustLevel { get; init; }
    public bool IsVerified { get; init; }
    public bool IsOfficial { get; init; }

    // Metadata
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? IconUrl { get; init; }
    public IReadOnlyList<string>? ScreenshotUrls { get; init; }
    public Uri? Homepage { get; init; }
    public Uri? Repository { get; init; }

    // Pricing
    public PricingInfo Pricing { get; init; } = new();

    // Compatibility
    public CompatibilityInfo Compatibility { get; init; } = new();

    // Dates
    public DateTimeOffset PublishedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public record PublisherInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsVerified { get; init; }
    public int PublishedItems { get; init; }
}

public record PricingInfo
{
    public PricingModel Model { get; init; } = PricingModel.Free;
    public decimal? Price { get; init; }
    public string? Currency { get; init; }
    public LicenseTier? RequiredTier { get; init; }
}

public enum PricingModel
{
    Free, FreeTier, Paid, Subscription
}

/// <summary>
/// Rating and review system.
/// </summary>
public interface IRatingService
{
    Task<ItemRatings> GetRatingsAsync(
        string itemId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Review>> GetReviewsAsync(
        string itemId,
        ReviewQuery query,
        CancellationToken ct = default);

    Task<Review> SubmitReviewAsync(
        string itemId,
        ReviewSubmission submission,
        CancellationToken ct = default);

    Task<Review> UpdateReviewAsync(
        Guid reviewId,
        ReviewUpdate update,
        CancellationToken ct = default);

    Task DeleteReviewAsync(
        Guid reviewId,
        CancellationToken ct = default);
}

public record ItemRatings
{
    public string ItemId { get; init; } = "";
    public float AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public IReadOnlyDictionary<int, int> Distribution { get; init; } =
        new Dictionary<int, int>();  // Star -> Count
}

public record Review
{
    public Guid Id { get; init; }
    public string ItemId { get; init; } = "";
    public required string AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public int Rating { get; init; }  // 1-5
    public string? Title { get; init; }
    public string? Body { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int HelpfulVotes { get; init; }
    public PublisherResponse? Response { get; init; }
}

/// <summary>
/// Publisher portal for managing marketplace presence.
/// </summary>
public interface IPublisherPortal
{
    Task<PublisherAccount> GetAccountAsync(
        CancellationToken ct = default);

    Task<PublishResult> PublishAsync(
        PublishRequest request,
        CancellationToken ct = default);

    Task<PublishResult> UpdateAsync(
        string itemId,
        PublishRequest request,
        CancellationToken ct = default);

    Task UnpublishAsync(
        string itemId,
        CancellationToken ct = default);

    Task<IReadOnlyList<MarketplaceItem>> GetMyItemsAsync(
        CancellationToken ct = default);

    Task<PublisherAnalytics> GetAnalyticsAsync(
        string? itemId = null,
        TimeRange period = default,
        CancellationToken ct = default);
}

public record PublishRequest
{
    public required Stream PackageStream { get; init; }
    public string? ReleaseNotes { get; init; }
    public IReadOnlyList<string>? Categories { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public PricingInfo? Pricing { get; init; }
    public bool RequestVerification { get; init; }
}

public record PublisherAnalytics
{
    public int TotalDownloads { get; init; }
    public int UniqueUsers { get; init; }
    public float AverageRating { get; init; }
    public IReadOnlyList<DailyStats> DailyStats { get; init; } = [];
    public IReadOnlyDictionary<string, int> DownloadsByVersion { get; init; } =
        new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> DownloadsByRegion { get; init; } =
        new Dictionary<string, int>();
}
```

### Marketplace Features

| Feature | Description |
|:--------|:------------|
| Search & Discovery | Full-text search with filters |
| Categories | Hierarchical categorization |
| Ratings & Reviews | 5-star ratings with reviews |
| Verified Publishers | Trust badges for verified publishers |
| Featured Content | Curated featured section |
| Trending | Popularity-based trending |
| Collections | User-curated collections |
| Compatibility | Version compatibility info |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Browse only |
| WriterPro | Install + rate |
| Teams | + Publish |
| Enterprise | + Analytics + featuring |

---

## v0.15.4-MKT: Integration Hub

**Goal:** Provide a framework for building connectors to external services with OAuth, webhooks, and API management.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.15.4e | Connector Framework | 12 |
| v0.15.4f | OAuth Manager | 10 |
| v0.15.4g | Webhook System | 10 |
| v0.15.4h | API Gateway | 10 |
| v0.15.4i | Credential Vault | 4 |
| v0.15.4j | Integration Manager UI | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Framework for building external service connectors.
/// </summary>
public interface IConnector
{
    ConnectorManifest Manifest { get; }

    Task<ConnectionResult> ConnectAsync(
        ConnectionRequest request,
        CancellationToken ct = default);

    Task DisconnectAsync(CancellationToken ct = default);

    Task<ConnectionHealth> CheckHealthAsync(CancellationToken ct = default);

    Task<object> ExecuteAsync(
        ConnectorAction action,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct = default);
}

public record ConnectorManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public required ConnectorCategory Category { get; init; }

    // Authentication
    public required AuthenticationMethod AuthMethod { get; init; }
    public OAuthConfig? OAuthConfig { get; init; }
    public ApiKeyConfig? ApiKeyConfig { get; init; }

    // Capabilities
    public IReadOnlyList<ConnectorAction> Actions { get; init; } = [];
    public IReadOnlyList<ConnectorTrigger> Triggers { get; init; } = [];

    // Rate limiting
    public RateLimitConfig? RateLimit { get; init; }
}

public enum ConnectorCategory
{
    VersionControl,     // Git, GitHub, GitLab
    ProjectManagement,  // Jira, Linear, Asana
    Documentation,      // Confluence, Notion
    Communication,      // Slack, Discord, Teams
    Storage,           // S3, GCS, Azure Blob
    Database,          // PostgreSQL, MongoDB
    AI,                // OpenAI, Anthropic, Cohere
    Custom
}

public enum AuthenticationMethod
{
    None, ApiKey, OAuth2, BasicAuth, CustomHeader
}

public record OAuthConfig
{
    public required string AuthorizationUrl { get; init; }
    public required string TokenUrl { get; init; }
    public required IReadOnlyList<string> Scopes { get; init; }
    public string? RefreshUrl { get; init; }
    public bool SupportsPKCE { get; init; } = true;
}

public record ConnectorAction
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<ActionParameter> Parameters { get; init; } = [];
    public ActionOutput? Output { get; init; }
    public bool RequiresAuth { get; init; } = true;
}

public record ConnectorTrigger
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public TriggerType Type { get; init; }
    public string? WebhookPath { get; init; }
    public string? CronSchedule { get; init; }
    public IReadOnlyList<TriggerOutput> Outputs { get; init; } = [];
}

/// <summary>
/// Manages OAuth connections.
/// </summary>
public interface IOAuthManager
{
    Task<OAuthFlow> InitiateFlowAsync(
        string connectorId,
        OAuthFlowOptions options,
        CancellationToken ct = default);

    Task<OAuthToken> CompleteFlowAsync(
        string flowId,
        string authorizationCode,
        CancellationToken ct = default);

    Task<OAuthToken> RefreshTokenAsync(
        string connectorId,
        CancellationToken ct = default);

    Task RevokeTokenAsync(
        string connectorId,
        CancellationToken ct = default);

    Task<OAuthToken?> GetTokenAsync(
        string connectorId,
        CancellationToken ct = default);
}

public record OAuthFlow
{
    public required string FlowId { get; init; }
    public required Uri AuthorizationUrl { get; init; }
    public required string State { get; init; }
    public string? CodeVerifier { get; init; }  // PKCE
    public DateTimeOffset ExpiresAt { get; init; }
}

public record OAuthToken
{
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public IReadOnlyList<string> Scopes { get; init; } = [];
}

/// <summary>
/// Webhook handling system.
/// </summary>
public interface IWebhookManager
{
    Task<WebhookRegistration> RegisterAsync(
        WebhookDefinition definition,
        CancellationToken ct = default);

    Task UnregisterAsync(
        Guid webhookId,
        CancellationToken ct = default);

    Task<IReadOnlyList<WebhookRegistration>> GetRegistrationsAsync(
        string? connectorId = null,
        CancellationToken ct = default);

    Task<WebhookDelivery> SimulateAsync(
        Guid webhookId,
        object payload,
        CancellationToken ct = default);
}

public record WebhookDefinition
{
    public required string ConnectorId { get; init; }
    public required string TriggerId { get; init; }
    public required WebhookHandler Handler { get; init; }
    public string? Secret { get; init; }  // For signature verification
    public WebhookRetryPolicy RetryPolicy { get; init; } = new();
}

public record WebhookRegistration
{
    public Guid Id { get; init; }
    public required string Url { get; init; }  // Generated webhook URL
    public required string ConnectorId { get; init; }
    public required string TriggerId { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int DeliveryCount { get; init; }
    public int FailureCount { get; init; }
}

public delegate Task WebhookHandler(WebhookPayload payload, CancellationToken ct);

/// <summary>
/// Manages connections to external services.
/// </summary>
public interface IIntegrationManager
{
    Task<IReadOnlyList<IConnector>> GetAvailableConnectorsAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<ActiveConnection>> GetConnectionsAsync(
        CancellationToken ct = default);

    Task<ActiveConnection> ConnectAsync(
        string connectorId,
        ConnectionConfiguration config,
        CancellationToken ct = default);

    Task DisconnectAsync(
        Guid connectionId,
        CancellationToken ct = default);

    Task<object> ExecuteActionAsync(
        Guid connectionId,
        string actionId,
        IReadOnlyDictionary<string, object> parameters,
        CancellationToken ct = default);
}

public record ActiveConnection
{
    public Guid Id { get; init; }
    public required string ConnectorId { get; init; }
    public required string Name { get; init; }
    public ConnectionStatus Status { get; init; }
    public DateTimeOffset ConnectedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public ConnectionHealth Health { get; init; } = new();
}

public enum ConnectionStatus
{
    Connected, Disconnected, Error, Refreshing
}
```

### Built-in Connectors

| Connector | Category | Actions |
|:----------|:---------|:--------|
| GitHub | Version Control | List repos, get commits, create PR |
| GitLab | Version Control | List projects, get commits, create MR |
| Jira | Project Management | List issues, create issue, update status |
| Linear | Project Management | List issues, create issue, sync |
| Confluence | Documentation | Get pages, create page, update |
| Notion | Documentation | Query database, create page |
| Slack | Communication | Send message, list channels |
| OpenAI | AI | Chat completion, embeddings |
| Anthropic | AI | Messages API |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | 2 connections |
| WriterPro | 5 connections |
| Teams | Unlimited + webhooks |
| Enterprise | + Custom connectors + API |

---

## v0.15.5-MKT: Enterprise Extensions

**Goal:** Provide enterprise-grade features including private registries, governance controls, and compliance tools.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.15.5e | Private Registry | 12 |
| v0.15.5f | Approval Workflows | 10 |
| v0.15.5g | Compliance Scanner | 10 |
| v0.15.5h | Governance Policies | 10 |
| v0.15.5i | Usage Analytics | 6 |
| v0.15.5j | Admin Console UI | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Private enterprise registry for internal distribution.
/// </summary>
public interface IPrivateRegistry
{
    Task<RegistryInfo> GetInfoAsync(CancellationToken ct = default);

    Task<IReadOnlyList<RegistryItem>> ListAsync(
        RegistryListOptions options,
        CancellationToken ct = default);

    Task<RegistryItem> PushAsync(
        Stream packageStream,
        PushOptions options,
        CancellationToken ct = default);

    Task<Stream> PullAsync(
        string itemId,
        SemanticVersion version,
        CancellationToken ct = default);

    Task DeleteAsync(
        string itemId,
        SemanticVersion? version = null,
        CancellationToken ct = default);
}

public record RegistryInfo
{
    public required string Name { get; init; }
    public required Uri Url { get; init; }
    public int ItemCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public RegistryPermissions Permissions { get; init; } = new();
}

/// <summary>
/// Approval workflow for marketplace items.
/// </summary>
public interface IApprovalWorkflow
{
    Task<ApprovalRequest> SubmitForApprovalAsync(
        string itemId,
        ApprovalSubmission submission,
        CancellationToken ct = default);

    Task<ApprovalRequest> GetRequestAsync(
        Guid requestId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ApprovalRequest>> GetPendingRequestsAsync(
        CancellationToken ct = default);

    Task<ApprovalDecision> ApproveAsync(
        Guid requestId,
        string? comment = null,
        CancellationToken ct = default);

    Task<ApprovalDecision> RejectAsync(
        Guid requestId,
        string reason,
        CancellationToken ct = default);
}

public record ApprovalRequest
{
    public Guid Id { get; init; }
    public required string ItemId { get; init; }
    public required string SubmitterId { get; init; }
    public ApprovalStatus Status { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public string? Justification { get; init; }
    public IReadOnlyList<ApprovalStep> Steps { get; init; } = [];
    public ComplianceScanResult? ScanResult { get; init; }
}

public enum ApprovalStatus
{
    Pending, InReview, Approved, Rejected, Cancelled
}

public record ApprovalStep
{
    public required string StepId { get; init; }
    public required string Name { get; init; }
    public ApprovalStepStatus Status { get; init; }
    public string? ApproverId { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? Comment { get; init; }
}

/// <summary>
/// Scans packages for compliance and security issues.
/// </summary>
public interface IComplianceScanner
{
    Task<ComplianceScanResult> ScanAsync(
        Stream packageStream,
        ScanOptions options,
        CancellationToken ct = default);

    Task<ComplianceScanResult> ScanInstalledAsync(
        string itemId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ComplianceRule>> GetRulesAsync(
        CancellationToken ct = default);

    Task SetRulesAsync(
        IReadOnlyList<ComplianceRule> rules,
        CancellationToken ct = default);
}

public record ComplianceScanResult
{
    public bool Passed { get; init; }
    public ComplianceLevel Level { get; init; }
    public IReadOnlyList<ComplianceViolation> Violations { get; init; } = [];
    public IReadOnlyList<SecurityVulnerability> Vulnerabilities { get; init; } = [];
    public IReadOnlyList<LicenseIssue> LicenseIssues { get; init; } = [];
    public DateTimeOffset ScannedAt { get; init; }
}

public enum ComplianceLevel
{
    Compliant, MinorIssues, MajorIssues, Critical, Blocked
}

public record ComplianceViolation
{
    public required string RuleId { get; init; }
    public required string RuleName { get; init; }
    public required ViolationSeverity Severity { get; init; }
    public required string Description { get; init; }
    public string? Location { get; init; }
    public string? Remediation { get; init; }
}

public enum ViolationSeverity { Info, Warning, Error, Critical }

public record SecurityVulnerability
{
    public required string VulnerabilityId { get; init; }  // CVE ID
    public required string Description { get; init; }
    public required VulnerabilitySeverity Severity { get; init; }
    public string? AffectedComponent { get; init; }
    public string? FixedInVersion { get; init; }
    public Uri? ReferenceUrl { get; init; }
}

/// <summary>
/// Governance policies for marketplace usage.
/// </summary>
public interface IGovernanceService
{
    Task<GovernancePolicy> GetPolicyAsync(CancellationToken ct = default);

    Task UpdatePolicyAsync(
        GovernancePolicy policy,
        CancellationToken ct = default);

    Task<PolicyEvaluation> EvaluateAsync(
        string itemId,
        CancellationToken ct = default);

    Task<IReadOnlyList<PolicyViolation>> GetViolationsAsync(
        PolicyViolationQuery query,
        CancellationToken ct = default);
}

public record GovernancePolicy
{
    // Source restrictions
    public IReadOnlyList<string> AllowedSources { get; init; } = [];
    public IReadOnlyList<string> BlockedPublishers { get; init; } = [];
    public TrustLevel MinimumTrustLevel { get; init; } = TrustLevel.Community;

    // Content restrictions
    public IReadOnlyList<string> AllowedCategories { get; init; } = [];
    public IReadOnlyList<string> BlockedCategories { get; init; } = [];

    // Permission restrictions
    public IReadOnlyList<PluginPermission> MaxAllowedPermissions { get; init; } = [];
    public IReadOnlyList<PluginPermission> RequireApprovalPermissions { get; init; } = [];

    // License restrictions
    public IReadOnlyList<string> AllowedLicenses { get; init; } = [];
    public IReadOnlyList<string> BlockedLicenses { get; init; } = [];

    // Security requirements
    public bool RequireSignature { get; init; } = true;
    public bool RequireComplianceScan { get; init; } = true;
    public ComplianceLevel MinimumComplianceLevel { get; init; } = ComplianceLevel.MinorIssues;
}

/// <summary>
/// Usage analytics for administrators.
/// </summary>
public interface IUsageAnalytics
{
    Task<UsageReport> GetReportAsync(
        UsageReportOptions options,
        CancellationToken ct = default);

    Task<IReadOnlyList<UsageEvent>> GetEventsAsync(
        UsageEventQuery query,
        CancellationToken ct = default);

    Task ExportAsync(
        Stream output,
        ExportFormat format,
        ExportOptions options,
        CancellationToken ct = default);
}

public record UsageReport
{
    public TimeRange Period { get; init; }
    public int TotalInstallations { get; init; }
    public int TotalUninstallations { get; init; }
    public int ActivePlugins { get; init; }
    public int ActiveConnections { get; init; }
    public IReadOnlyList<ItemUsage> TopItems { get; init; } = [];
    public IReadOnlyList<UserUsage> TopUsers { get; init; } = [];
    public IReadOnlyDictionary<string, int> UsageByCategory { get; init; } =
        new Dictionary<string, int>();
}
```

### Enterprise Features

| Feature | Description |
|:--------|:------------|
| Private Registry | Self-hosted package repository |
| Approval Workflows | Multi-stage approval process |
| Compliance Scanning | Security and license scanning |
| Governance Policies | Enforce organizational rules |
| Usage Analytics | Track adoption and usage |
| Audit Logging | Complete audit trail |
| SSO Integration | Enterprise identity providers |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | Not available |
| Teams | Basic policies |
| Enterprise | Full enterprise features |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.15.x |
|:----------|:-------|:-----------------|
| `IAgentRegistry` | v0.12.1-AGT | Agent package registration |
| `IToolRegistry` | v0.12.5-AGT | Tool package registration |
| `IPatternLibrary` | v0.13.5-ORC | Pattern packages |
| `IPromptStudio` | v0.14.4-STU | Prompt packages |
| `IAuthorizationService` | v0.11.1-SEC | Permission checks |
| `IAuditLogger` | v0.11.2-SEC | Audit logging |
| `IEncryptionService` | v0.11.3-SEC | Credential encryption |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `PluginInstalledEvent` | v0.15.1 | Plugin installed |
| `PluginActivatedEvent` | v0.15.1 | Plugin activated |
| `PackagePublishedEvent` | v0.15.2 | Package published |
| `PackageDownloadedEvent` | v0.15.3 | Package downloaded |
| `ReviewSubmittedEvent` | v0.15.3 | Review submitted |
| `ConnectionEstablishedEvent` | v0.15.4 | Integration connected |
| `WebhookReceivedEvent` | v0.15.4 | Webhook received |
| `ApprovalRequestedEvent` | v0.15.5 | Approval requested |
| `PolicyViolationEvent` | v0.15.5 | Policy violated |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `NuGet.Protocol` | 6.x | Package format |
| `Microsoft.Extensions.FileProviders` | 8.x | Plugin isolation |
| `OAuth2.Client` | 2.x | OAuth flows |
| `MinVer` | 5.x | Semantic versioning |

---

## Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Plugin load | <500ms |
| Package install | <5s |
| Catalog search | <200ms |
| OAuth flow | <3s |
| Compliance scan | <30s |

---

## What This Enables

With v0.15.x complete, Lexichord becomes a complete platform ecosystem:

- **Extensibility:** Safe, powerful plugin architecture
- **Distribution:** Standard packaging and versioning
- **Discovery:** Rich marketplace for finding content
- **Integration:** Connect to any external service
- **Governance:** Enterprise-grade controls

This completes the transformation from application to platform.

---

## Total Marketplace Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 9: Marketplace | v0.15.1 - v0.15.5 | ~258 |

**Combined with prior phases:** ~1,742 hours (~44 person-months)

---

## Complete Agentic Platform Summary

| Phase | Focus | Versions | Hours |
|:------|:------|:---------|:------|
| 1-4 | Foundation (CKVS) | v0.4.x - v0.7.x | ~273 |
| 5 | Advanced Knowledge | v0.10.x | ~214 |
| 6 | Security | v0.11.x | ~209 |
| 7 | Agent Infrastructure | v0.12.x | ~248 |
| 8 | Orchestration | v0.13.x | ~276 |
| 9 | Agent Studio | v0.14.x | ~264 |
| 10 | Marketplace | v0.15.x | ~258 |
| **Total** | | | **~1,742 hours** |

**Approximately 44 person-months** to complete the full agentic platform vision.

---
