# Lexichord Local LLM Integration Roadmap (v0.16.1 - v0.16.5)

In v0.15.x, we delivered **The Marketplace & Extensibility Layer** — enabling community-driven agent development and a thriving ecosystem. In v0.16.x, we introduce **Local LLM Integration** — empowering users to run AI models entirely on their own hardware, ensuring privacy, reducing costs, and enabling offline operation.

**Architectural Note:** This version introduces `Lexichord.LocalLlm` module, providing hardware detection, model management, inference runtime abstraction, and seamless integration with existing orchestration systems. This represents a fundamental expansion of Lexichord's AI capabilities beyond cloud-only providers.

**Total Sub-Parts:** 35 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 274 hours (~6.9 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.16.1-LLM | Hardware & Backends | Hardware detection, backend abstraction, Ollama integration | 54 |
| v0.16.2-LLM | Model Discovery | Hugging Face integration, model search, compatibility checking | 52 |
| v0.16.3-LLM | Download & Storage | Model downloading, storage management, integrity verification | 50 |
| v0.16.4-LLM | Inference Runtime | Model loading, inference execution, resource management | 58 |
| v0.16.5-LLM | Platform Integration | ILanguageModelProvider, orchestration routing, advanced features | 60 |

---

## Local LLM Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Local LLM Integration Layer                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Lexichord Platform Integration (v0.16.5)             │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │  LLM Router  │  │   Fallback   │  │  Cost/Perf   │                  │ │
│  │  │   Service    │  │   Manager    │  │   Optimizer  │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│           ┌────────────────────────┴────────────────────────┐               │
│           ▼                                                  ▼               │
│  ┌─────────────────────┐                      ┌─────────────────────┐       │
│  │  Inference Runtime  │                      │  Model Management   │       │
│  │     (v0.16.4)       │                      │   (v0.16.2-v0.16.3) │       │
│  │                     │                      │                     │       │
│  │ • Model loading     │                      │ • HuggingFace API   │       │
│  │ • Token generation  │                      │ • Model download    │       │
│  │ • Resource monitor  │                      │ • Storage manager   │       │
│  └─────────────────────┘                      └─────────────────────┘       │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                   Backend Abstraction Layer (v0.16.1)                │   │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐    │   │
│  │  │   Ollama   │  │ LM Studio  │  │ llama.cpp  │  │   Custom   │    │   │
│  │  │  Backend   │  │  Backend   │  │  Backend   │  │  Backend   │    │   │
│  │  └────────────┘  └────────────┘  └────────────┘  └────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                   Hardware Detection Layer (v0.16.1)                 │   │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐            │   │
│  │  │   CUDA   │  │  Metal   │  │   ROCm   │  │ CPU-Only │            │   │
│  │  │ (NVIDIA) │  │ (Apple)  │  │  (AMD)   │  │ Fallback │            │   │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.16.1-LLM: Hardware & Backends

**Goal:** Establish hardware detection capabilities and a pluggable backend abstraction layer supporting multiple inference engines, with Ollama as the primary recommended backend.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.16.1e | Hardware Detection Service | 12 |
| v0.16.1f | GPU Capability Analyzer | 10 |
| v0.16.1g | Backend Abstraction Layer | 10 |
| v0.16.1h | Ollama Backend Integration | 10 |
| v0.16.1i | Backend Health Monitor | 6 |
| v0.16.1j | Hardware Settings UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Detects and reports hardware capabilities for local LLM inference.
/// </summary>
public interface IHardwareDetector
{
    /// <summary>
    /// Detect all hardware capabilities of the current system.
    /// </summary>
    Task<HardwareCapabilities> DetectAsync(CancellationToken ct = default);

    /// <summary>
    /// Get detailed information about available GPUs.
    /// </summary>
    Task<IReadOnlyList<GpuInfo>> GetGpusAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if a specific backend is supported on this hardware.
    /// </summary>
    Task<BackendSupport> CheckBackendSupportAsync(
        InferenceBackendType backend,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of hardware changes (GPU added/removed, memory changes).
    /// </summary>
    IObservable<HardwareChangeEvent> HardwareChanges { get; }
}

/// <summary>
/// Complete hardware capabilities of the system.
/// </summary>
public record HardwareCapabilities
{
    // System identification
    public required string OperatingSystem { get; init; }
    public required string Architecture { get; init; }  // x64, arm64

    // CPU
    public required CpuInfo Cpu { get; init; }

    // Memory
    public required long TotalRamBytes { get; init; }
    public required long AvailableRamBytes { get; init; }

    // GPU(s)
    public IReadOnlyList<GpuInfo> Gpus { get; init; } = [];
    public GpuInfo? PrimaryGpu => Gpus.FirstOrDefault();
    public long TotalVramBytes => Gpus.Sum(g => g.VramBytes);

    // Storage
    public required long AvailableDiskBytes { get; init; }
    public required string ModelStoragePath { get; init; }

    // Derived recommendations
    public required ModelSizeRecommendation Recommendation { get; init; }
}

public record CpuInfo
{
    public required string Name { get; init; }
    public required int PhysicalCores { get; init; }
    public required int LogicalCores { get; init; }
    public long? L3CacheBytes { get; init; }
    public bool SupportsAvx { get; init; }
    public bool SupportsAvx2 { get; init; }
    public bool SupportsAvx512 { get; init; }
}

public record GpuInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required GpuVendor Vendor { get; init; }
    public required long VramBytes { get; init; }
    public long? AvailableVramBytes { get; init; }
    public required GpuBackend SupportedBackend { get; init; }
    public string? DriverVersion { get; init; }
    public int? ComputeCapability { get; init; }  // CUDA compute capability
    public bool IsIntegrated { get; init; }
}

public enum GpuVendor
{
    Unknown, Nvidia, Amd, Intel, Apple
}

public enum GpuBackend
{
    None,       // No GPU acceleration
    Cuda,       // NVIDIA CUDA
    Metal,      // Apple Metal
    Rocm,       // AMD ROCm (Linux)
    Vulkan,     // Cross-platform (fallback)
    DirectML,   // Windows DirectX ML
    Sycl        // Intel oneAPI
}

/// <summary>
/// Model size recommendations based on hardware.
/// </summary>
public record ModelSizeRecommendation
{
    public required ModelSizeCategory MaxRecommendedSize { get; init; }
    public required QuantizationType RecommendedQuantization { get; init; }
    public required int RecommendedContextLength { get; init; }
    public required int EstimatedGpuLayers { get; init; }
    public required InferenceMode RecommendedMode { get; init; }
    public string? Explanation { get; init; }

    public IReadOnlyList<ModelSizeOption> AvailableOptions { get; init; } = [];
}

public record ModelSizeOption
{
    public required ModelSizeCategory Size { get; init; }
    public required QuantizationType Quantization { get; init; }
    public required CompatibilityLevel Compatibility { get; init; }
    public required long EstimatedMemoryBytes { get; init; }
    public float? EstimatedTokensPerSecond { get; init; }
}

public enum ModelSizeCategory
{
    Tiny,       // 1-3B parameters
    Small,      // 7-8B parameters
    Medium,     // 13-14B parameters
    Large,      // 30-34B parameters
    XLarge,     // 65-70B parameters
    XXLarge     // 100B+ parameters
}

public enum CompatibilityLevel
{
    Optimal,        // Will run great, recommended
    Comfortable,    // Will run well with good performance
    Constrained,    // Will run but may be slow
    Marginal,       // Might work with aggressive settings
    Incompatible    // Won't run on this hardware
}

public enum InferenceMode
{
    FullGpu,        // All layers on GPU
    HybridGpuCpu,   // Some layers on GPU, rest on CPU
    CpuOnly         // All layers on CPU
}

/// <summary>
/// Quantization types for model compression.
/// </summary>
public enum QuantizationType
{
    F32,        // Full 32-bit precision (largest, most accurate)
    F16,        // Half 16-bit precision
    BF16,       // Brain float 16-bit
    Q8_0,       // 8-bit quantization
    Q6_K,       // 6-bit k-quant
    Q5_K_M,     // 5-bit k-quant medium
    Q5_K_S,     // 5-bit k-quant small
    Q4_K_M,     // 4-bit k-quant medium (recommended balance)
    Q4_K_S,     // 4-bit k-quant small
    Q4_0,       // 4-bit legacy
    Q3_K_M,     // 3-bit k-quant medium
    Q3_K_S,     // 3-bit k-quant small
    Q2_K,       // 2-bit (significant quality loss)
    IQ4_XS,     // Importance-weighted 4-bit extra small
    IQ4_NL,     // Importance-weighted 4-bit non-linear
    IQ3_M,      // Importance-weighted 3-bit medium
    IQ2_XXS     // Importance-weighted 2-bit (experimental)
}

/// <summary>
/// Abstraction over different LLM inference backends.
/// </summary>
public interface IInferenceBackend
{
    /// <summary>
    /// Backend identifier.
    /// </summary>
    InferenceBackendType Type { get; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this backend is currently available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Get backend version and capabilities.
    /// </summary>
    Task<BackendInfo> GetInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// List models available through this backend.
    /// </summary>
    Task<IReadOnlyList<BackendModel>> ListModelsAsync(CancellationToken ct = default);

    /// <summary>
    /// Load a model for inference.
    /// </summary>
    Task<IModelInstance> LoadModelAsync(
        string modelId,
        ModelLoadOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Pull/download a model through this backend.
    /// </summary>
    Task<ModelPullResult> PullModelAsync(
        string modelId,
        IProgress<ModelPullProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a model from this backend.
    /// </summary>
    Task DeleteModelAsync(string modelId, CancellationToken ct = default);

    /// <summary>
    /// Health check for the backend.
    /// </summary>
    Task<BackendHealth> CheckHealthAsync(CancellationToken ct = default);
}

public enum InferenceBackendType
{
    Ollama,         // Ollama server
    LmStudio,       // LM Studio local server
    LlamaCpp,       // Direct llama.cpp integration
    Vllm,           // vLLM server
    TextGenWebUI,   // Text Generation WebUI
    LocalAI,        // LocalAI server
    Custom          // Custom OpenAI-compatible endpoint
}

public record BackendInfo
{
    public required InferenceBackendType Type { get; init; }
    public required string Version { get; init; }
    public IReadOnlyList<GpuBackend> SupportedGpuBackends { get; init; } = [];
    public IReadOnlyList<string> SupportedModelFormats { get; init; } = [];
    public bool SupportsStreaming { get; init; }
    public bool SupportsEmbeddings { get; init; }
    public bool SupportsConcurrentRequests { get; init; }
    public int? MaxConcurrentRequests { get; init; }
    public Uri? Endpoint { get; init; }
}

public record BackendModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public long? SizeBytes { get; init; }
    public string? Family { get; init; }        // llama, mistral, etc.
    public string? ParameterSize { get; init; } // 7B, 13B, etc.
    public QuantizationType? Quantization { get; init; }
    public int? ContextLength { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
}

public record ModelLoadOptions
{
    public int? NumGpuLayers { get; init; }     // -1 for all, 0 for CPU only
    public int? ContextLength { get; init; }
    public int? NumThreads { get; init; }
    public int? BatchSize { get; init; }
    public bool UseMemoryMap { get; init; } = true;
    public bool UseMemoryLock { get; init; } = false;
    public string? GpuDeviceId { get; init; }   // For multi-GPU
    public float? RopeFrequencyBase { get; init; }
    public float? RopeFrequencyScale { get; init; }
}

/// <summary>
/// A loaded model instance ready for inference.
/// </summary>
public interface IModelInstance : IAsyncDisposable
{
    Guid InstanceId { get; }
    string ModelId { get; }
    ModelInstanceStatus Status { get; }
    ModelLoadOptions LoadOptions { get; }

    /// <summary>
    /// Generate a completion (non-streaming).
    /// </summary>
    Task<GenerationResult> GenerateAsync(
        GenerationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a completion with streaming.
    /// </summary>
    IAsyncEnumerable<GenerationToken> GenerateStreamAsync(
        GenerationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generate embeddings for text.
    /// </summary>
    Task<EmbeddingResult> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get current resource usage.
    /// </summary>
    Task<ModelResourceUsage> GetResourceUsageAsync(CancellationToken ct = default);

    /// <summary>
    /// Observable stream of status changes.
    /// </summary>
    IObservable<ModelInstanceStatus> StatusChanges { get; }
}

public enum ModelInstanceStatus
{
    Loading,
    Ready,
    Generating,
    Embedding,
    Unloading,
    Error,
    Disposed
}

public record GenerationRequest
{
    public required string Prompt { get; init; }
    public string? SystemPrompt { get; init; }
    public IReadOnlyList<ChatMessage>? Messages { get; init; }

    // Generation parameters
    public int? MaxTokens { get; init; }
    public float? Temperature { get; init; }
    public float? TopP { get; init; }
    public int? TopK { get; init; }
    public float? RepetitionPenalty { get; init; }
    public float? FrequencyPenalty { get; init; }
    public float? PresencePenalty { get; init; }
    public IReadOnlyList<string>? StopSequences { get; init; }
    public int? Seed { get; init; }

    // Output format
    public ResponseFormat? Format { get; init; }
}

public record ChatMessage
{
    public required ChatRole Role { get; init; }
    public required string Content { get; init; }
}

public enum ChatRole { System, User, Assistant }

public record GenerationResult
{
    public required string Text { get; init; }
    public required GenerationUsage Usage { get; init; }
    public string? FinishReason { get; init; }
    public TimeSpan Duration { get; init; }
    public float TokensPerSecond { get; init; }
}

public record GenerationToken
{
    public required string Text { get; init; }
    public bool IsComplete { get; init; }
    public string? FinishReason { get; init; }
}

public record GenerationUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

public record ModelResourceUsage
{
    public long RamBytes { get; init; }
    public long? VramBytes { get; init; }
    public float? GpuUtilizationPercent { get; init; }
    public float CpuUtilizationPercent { get; init; }
    public int LoadedLayers { get; init; }
    public int GpuLayers { get; init; }
    public int CpuLayers { get; init; }
}

/// <summary>
/// Manages multiple inference backends.
/// </summary>
public interface IBackendManager
{
    /// <summary>
    /// Get all registered backends.
    /// </summary>
    Task<IReadOnlyList<IInferenceBackend>> GetBackendsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get available (running) backends.
    /// </summary>
    Task<IReadOnlyList<IInferenceBackend>> GetAvailableBackendsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get the preferred backend based on configuration and availability.
    /// </summary>
    Task<IInferenceBackend?> GetPreferredBackendAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Register a custom backend.
    /// </summary>
    Task RegisterBackendAsync(
        IInferenceBackend backend,
        CancellationToken ct = default);

    /// <summary>
    /// Set the preferred backend.
    /// </summary>
    Task SetPreferredBackendAsync(
        InferenceBackendType type,
        CancellationToken ct = default);

    /// <summary>
    /// Start a backend if it's a managed service (e.g., bundled Ollama).
    /// </summary>
    Task<BackendStartResult> StartBackendAsync(
        InferenceBackendType type,
        CancellationToken ct = default);

    /// <summary>
    /// Stop a managed backend.
    /// </summary>
    Task StopBackendAsync(
        InferenceBackendType type,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of backend status changes.
    /// </summary>
    IObservable<BackendStatusEvent> StatusChanges { get; }
}

public record BackendStartResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Uri? Endpoint { get; init; }
    public int? ProcessId { get; init; }
}

public record BackendStatusEvent
{
    public required InferenceBackendType Backend { get; init; }
    public required BackendStatus Status { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public enum BackendStatus
{
    Unknown, Starting, Running, Stopping, Stopped, Error
}
```

### Ollama Integration Details

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Ollama Backend Integration                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Option A: Use System Ollama                                            │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • User installs Ollama separately                                  │  │
│  │ • Lexichord connects to http://localhost:11434                     │  │
│  │ • Pros: User manages updates, may already have models              │  │
│  │ • Cons: Requires manual installation                               │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Option B: Bundled Ollama (Recommended for ease of use)                 │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • Ship Ollama binary with Lexichord                                │  │
│  │ • Start/stop as needed (on-demand)                                 │  │
│  │ • Isolated model storage in Lexichord data directory               │  │
│  │ • Pros: Zero-config experience, consistent behavior                │  │
│  │ • Cons: Larger install size, version management                    │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Hybrid Approach (Implemented):                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ 1. Check for system Ollama first                                   │  │
│  │ 2. If not found, offer to download/install bundled version         │  │
│  │ 3. Allow user to switch between system and bundled                 │  │
│  │ 4. Share model library between both if configured                  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Hardware detection; read-only backend status |
| WriterPro | 1 backend; basic model loading |
| Teams | Multiple backends; all configuration options |
| Enterprise | + Managed backend deployment; custom backends |

---

## v0.16.2-LLM: Model Discovery

**Goal:** Integrate with Hugging Face Hub and other model registries to enable searching, browsing, and evaluating models with compatibility recommendations.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.16.2e | Model Registry Abstraction | 10 |
| v0.16.2f | Hugging Face Hub Integration | 12 |
| v0.16.2g | Model Compatibility Analyzer | 10 |
| v0.16.2h | Model Search & Filtering | 8 |
| v0.16.2i | Model Details & Variants | 6 |
| v0.16.2j | Model Browser UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Abstraction over model registries (Hugging Face, Ollama Library, etc.).
/// </summary>
public interface IModelRegistry
{
    /// <summary>
    /// Registry identifier.
    /// </summary>
    ModelRegistryType Type { get; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Search for models.
    /// </summary>
    Task<ModelSearchResult> SearchAsync(
        ModelSearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Get detailed information about a model.
    /// </summary>
    Task<RemoteModel?> GetModelAsync(
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Get available variants/files for a model.
    /// </summary>
    Task<IReadOnlyList<ModelVariant>> GetVariantsAsync(
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Get trending/popular models.
    /// </summary>
    Task<IReadOnlyList<RemoteModel>> GetTrendingAsync(
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Get models by category/task.
    /// </summary>
    Task<IReadOnlyList<RemoteModel>> GetByCategoryAsync(
        ModelCategory category,
        int limit = 20,
        CancellationToken ct = default);
}

public enum ModelRegistryType
{
    HuggingFace,
    OllamaLibrary,
    Custom
}

public record ModelSearchQuery
{
    public string? Query { get; init; }
    public ModelCategory? Category { get; init; }
    public ModelTask? Task { get; init; }
    public ModelSizeCategory? MaxSize { get; init; }
    public IReadOnlyList<ModelFormat>? Formats { get; init; }
    public IReadOnlyList<QuantizationType>? Quantizations { get; init; }
    public string? BaseModel { get; init; }     // llama, mistral, etc.
    public string? Author { get; init; }
    public bool? OnlyGGUF { get; init; }        // Filter to GGUF format
    public bool OnlyCompatible { get; init; }   // Filter by hardware
    public ModelSortOrder Sort { get; init; } = ModelSortOrder.Trending;
    public int Skip { get; init; }
    public int Take { get; init; } = 20;
}

public enum ModelCategory
{
    TextGeneration,
    ChatCompletion,
    CodeGeneration,
    Embedding,
    ImageGeneration,
    SpeechToText,
    TextToSpeech,
    Translation,
    Summarization,
    QuestionAnswering
}

public enum ModelTask
{
    GeneralChat,
    CreativeWriting,
    CodeAssistant,
    DataAnalysis,
    Research,
    Roleplay,
    Instruction,
    Uncensored
}

public enum ModelFormat
{
    GGUF,           // llama.cpp native format
    GPTQ,           // GPU quantized
    AWQ,            // Activation-aware quantized
    GGML,           // Legacy llama.cpp format
    SafeTensors,    // Full precision
    PyTorch,        // .bin files
    ONNX,           // ONNX format
    MLX             // Apple MLX format
}

public enum ModelSortOrder
{
    Trending,
    Downloads,
    Likes,
    Recent,
    Name,
    Size
}

public record ModelSearchResult
{
    public IReadOnlyList<RemoteModel> Models { get; init; } = [];
    public int TotalCount { get; init; }
    public IReadOnlyList<SearchFacet> Facets { get; init; } = [];
}

public record SearchFacet
{
    public required string Name { get; init; }
    public IReadOnlyList<FacetValue> Values { get; init; } = [];
}

public record FacetValue
{
    public required string Value { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// A model available for download from a registry.
/// </summary>
public record RemoteModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ModelRegistryType Registry { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public Uri? Homepage { get; init; }
    public Uri? Repository { get; init; }

    // Model characteristics
    public string? BaseModel { get; init; }         // llama-3, mistral, etc.
    public string? ParameterCount { get; init; }    // 7B, 13B, 70B
    public int? ContextLength { get; init; }
    public string? Architecture { get; init; }
    public IReadOnlyList<ModelTask> Tasks { get; init; } = [];

    // Available variants (different quantizations/formats)
    public IReadOnlyList<ModelVariant> Variants { get; init; } = [];
    public ModelVariant? RecommendedVariant { get; init; }

    // Metrics
    public int Downloads { get; init; }
    public int Likes { get; init; }
    public float? AverageRating { get; init; }

    // Licensing
    public string? License { get; init; }
    public bool CommercialUseAllowed { get; init; }
    public IReadOnlyList<string>? Restrictions { get; init; }

    // Tags and metadata
    public IReadOnlyList<string> Tags { get; init; } = [];
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// A specific variant (quantization/format) of a model.
/// </summary>
public record ModelVariant
{
    public required string VariantId { get; init; }
    public required string Filename { get; init; }
    public required ModelFormat Format { get; init; }
    public QuantizationType? Quantization { get; init; }
    public required long SizeBytes { get; init; }
    public string? Sha256 { get; init; }
    public Uri? DownloadUrl { get; init; }

    // Performance characteristics
    public long? EstimatedVramBytes { get; init; }
    public long? EstimatedRamBytes { get; init; }
    public float? EstimatedQualityScore { get; init; }  // Relative quality 0-1

    // Compatibility
    public int? MinContextLength { get; init; }
    public int? MaxContextLength { get; init; }
}

/// <summary>
/// Analyzes model compatibility with current hardware.
/// </summary>
public interface IModelCompatibilityAnalyzer
{
    /// <summary>
    /// Analyze compatibility of a model with current hardware.
    /// </summary>
    Task<ModelCompatibility> AnalyzeAsync(
        RemoteModel model,
        CancellationToken ct = default);

    /// <summary>
    /// Analyze compatibility of a specific variant.
    /// </summary>
    Task<VariantCompatibility> AnalyzeVariantAsync(
        ModelVariant variant,
        CancellationToken ct = default);

    /// <summary>
    /// Get recommended variants for a model based on hardware.
    /// </summary>
    Task<IReadOnlyList<VariantRecommendation>> GetRecommendationsAsync(
        RemoteModel model,
        CancellationToken ct = default);

    /// <summary>
    /// Estimate performance for a model/variant combination.
    /// </summary>
    Task<PerformanceEstimate> EstimatePerformanceAsync(
        ModelVariant variant,
        InferenceConfig config,
        CancellationToken ct = default);
}

public record ModelCompatibility
{
    public required string ModelId { get; init; }
    public required CompatibilityLevel OverallLevel { get; init; }
    public IReadOnlyList<VariantCompatibility> VariantCompatibilities { get; init; } = [];
    public ModelVariant? RecommendedVariant { get; init; }
    public InferenceConfig? RecommendedConfig { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyList<CompatibilityWarning> Warnings { get; init; } = [];
}

public record VariantCompatibility
{
    public required ModelVariant Variant { get; init; }
    public required CompatibilityLevel Level { get; init; }
    public required InferenceMode RecommendedMode { get; init; }
    public int? RecommendedGpuLayers { get; init; }
    public int? RecommendedContextLength { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<CompatibilityWarning> Warnings { get; init; } = [];
}

public record VariantRecommendation
{
    public required ModelVariant Variant { get; init; }
    public required CompatibilityLevel Compatibility { get; init; }
    public required float Score { get; init; }  // 0-1, higher is better
    public required string Rationale { get; init; }
    public InferenceConfig RecommendedConfig { get; init; } = new();
    public PerformanceEstimate? EstimatedPerformance { get; init; }
}

public record PerformanceEstimate
{
    public float TokensPerSecondMin { get; init; }
    public float TokensPerSecondMax { get; init; }
    public float TokensPerSecondExpected { get; init; }
    public TimeSpan EstimatedLoadTime { get; init; }
    public long EstimatedRamUsage { get; init; }
    public long? EstimatedVramUsage { get; init; }
    public float ConfidenceLevel { get; init; }  // 0-1
}

public record CompatibilityWarning
{
    public required CompatibilityWarningType Type { get; init; }
    public required string Message { get; init; }
    public WarningSeverity Severity { get; init; }
    public string? Suggestion { get; init; }
}

public enum CompatibilityWarningType
{
    InsufficientVram,
    InsufficientRam,
    SlowPerformance,
    ContextLengthLimited,
    NoGpuAcceleration,
    UnsupportedFormat,
    ExperimentalQuantization,
    LicenseRestriction
}

public enum WarningSeverity { Info, Warning, Error }
```

### Hugging Face API Integration

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Hugging Face Hub Integration                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Endpoints Used:                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ GET /api/models                    - Search models                 │  │
│  │ GET /api/models/{model_id}         - Get model details             │  │
│  │ GET /api/models/{model_id}/tree    - List files in repo            │  │
│  │ GET /{model_id}/resolve/{file}     - Download file                 │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Filtering Strategy:                                                     │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ 1. Filter by pipeline_tag: text-generation                        │  │
│  │ 2. Filter by library: transformers, gguf                          │  │
│  │ 3. Search in model card for GGUF files                            │  │
│  │ 4. Parse filenames for quantization type                          │  │
│  │    e.g., "model-7b-q4_k_m.gguf" → Q4_K_M                          │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Rate Limiting:                                                          │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • Anonymous: ~30 requests/hour                                     │  │
│  │ • With token: ~1000 requests/hour                                  │  │
│  │ • Implement caching (1 hour for model lists, 24h for details)      │  │
│  │ • Optional: User can provide their own HF token                    │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Browse Ollama library only |
| WriterPro | + Hugging Face search; 10 searches/day |
| Teams | Unlimited search; compatibility analysis |
| Enterprise | + Custom registries; bulk analysis |

---

## v0.16.3-LLM: Download & Storage

**Goal:** Implement robust model downloading with resume support, integrity verification, and intelligent storage management.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.16.3e | Download Manager | 12 |
| v0.16.3f | Storage Manager | 10 |
| v0.16.3g | Integrity Verification | 8 |
| v0.16.3h | Download Queue & Scheduling | 8 |
| v0.16.3i | Model Import/Export | 6 |
| v0.16.3j | Download Manager UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Manages model downloads with resume support and integrity verification.
/// </summary>
public interface IModelDownloadManager
{
    /// <summary>
    /// Start downloading a model variant.
    /// </summary>
    Task<ModelDownload> StartDownloadAsync(
        RemoteModel model,
        ModelVariant variant,
        DownloadOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Pause an active download.
    /// </summary>
    Task PauseDownloadAsync(Guid downloadId, CancellationToken ct = default);

    /// <summary>
    /// Resume a paused download.
    /// </summary>
    Task ResumeDownloadAsync(Guid downloadId, CancellationToken ct = default);

    /// <summary>
    /// Cancel and remove a download.
    /// </summary>
    Task CancelDownloadAsync(Guid downloadId, CancellationToken ct = default);

    /// <summary>
    /// Get all active and queued downloads.
    /// </summary>
    Task<IReadOnlyList<ModelDownload>> GetDownloadsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get download history.
    /// </summary>
    Task<IReadOnlyList<ModelDownload>> GetHistoryAsync(
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of download progress updates.
    /// </summary>
    IObservable<DownloadProgressEvent> Progress { get; }

    /// <summary>
    /// Retry a failed download.
    /// </summary>
    Task RetryDownloadAsync(Guid downloadId, CancellationToken ct = default);
}

public record DownloadOptions
{
    public int MaxConcurrentConnections { get; init; } = 4;
    public long ChunkSizeBytes { get; init; } = 10 * 1024 * 1024;  // 10MB
    public bool VerifyIntegrity { get; init; } = true;
    public bool AutoRetryOnFailure { get; init; } = true;
    public int MaxRetries { get; init; } = 3;
    public DownloadPriority Priority { get; init; } = DownloadPriority.Normal;
    public string? CustomStoragePath { get; init; }
}

public enum DownloadPriority
{
    Low, Normal, High
}

public record ModelDownload
{
    public Guid Id { get; init; }
    public required RemoteModel Model { get; init; }
    public required ModelVariant Variant { get; init; }
    public required DownloadStatus Status { get; init; }
    public required long TotalBytes { get; init; }
    public long DownloadedBytes { get; init; }
    public float ProgressPercent => TotalBytes > 0 ? (float)DownloadedBytes / TotalBytes * 100 : 0;
    public long BytesPerSecond { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? PausedAt { get; init; }
    public string? Error { get; init; }
    public int RetryCount { get; init; }
    public string? DestinationPath { get; init; }
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Verifying,
    Installing,
    Completed,
    Failed,
    Cancelled
}

public record DownloadProgressEvent
{
    public required Guid DownloadId { get; init; }
    public required DownloadStatus Status { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public long BytesPerSecond { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Manages local model storage and disk usage.
/// </summary>
public interface IModelStorageManager
{
    /// <summary>
    /// Get storage configuration and status.
    /// </summary>
    Task<StorageInfo> GetStorageInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all installed models.
    /// </summary>
    Task<IReadOnlyList<InstalledModel>> GetInstalledModelsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific installed model.
    /// </summary>
    Task<InstalledModel?> GetInstalledModelAsync(
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Delete an installed model.
    /// </summary>
    Task DeleteModelAsync(
        string modelId,
        CancellationToken ct = default);

    /// <summary>
    /// Move model storage to a new location.
    /// </summary>
    Task MoveStorageAsync(
        string newPath,
        IProgress<StorageMoveProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Clean up temporary and partial download files.
    /// </summary>
    Task<CleanupResult> CleanupAsync(CancellationToken ct = default);

    /// <summary>
    /// Import a model from an external file.
    /// </summary>
    Task<InstalledModel> ImportModelAsync(
        string filePath,
        ModelImportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Export an installed model to a file.
    /// </summary>
    Task ExportModelAsync(
        string modelId,
        string destinationPath,
        CancellationToken ct = default);

    /// <summary>
    /// Get recommended models to delete based on usage.
    /// </summary>
    Task<IReadOnlyList<ModelCleanupSuggestion>> GetCleanupSuggestionsAsync(
        long targetFreeBytes,
        CancellationToken ct = default);
}

public record StorageInfo
{
    public required string StoragePath { get; init; }
    public required long TotalDiskBytes { get; init; }
    public required long FreeDiskBytes { get; init; }
    public required long UsedByModelsBytes { get; init; }
    public required long UsedByDownloadsBytes { get; init; }
    public required long UsedByTempBytes { get; init; }
    public int InstalledModelCount { get; init; }
    public int PendingDownloadCount { get; init; }
    public bool IsLowOnSpace { get; init; }
    public long RecommendedFreeBytes { get; init; }
}

public record InstalledModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required ModelFormat Format { get; init; }
    public QuantizationType? Quantization { get; init; }
    public required long SizeBytes { get; init; }
    public string? Sha256 { get; init; }

    // Original source
    public ModelRegistryType? SourceRegistry { get; init; }
    public string? SourceModelId { get; init; }
    public string? SourceVariantId { get; init; }

    // Model info
    public string? BaseModel { get; init; }
    public string? ParameterCount { get; init; }
    public int? ContextLength { get; init; }

    // Usage tracking
    public DateTimeOffset InstalledAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public int UsageCount { get; init; }
    public TimeSpan TotalUsageTime { get; init; }
}

public record ModelImportOptions
{
    public string? CustomName { get; init; }
    public string? CustomId { get; init; }
    public bool CopyFile { get; init; } = true;  // vs. move
    public bool VerifyIntegrity { get; init; } = true;
}

public record ModelCleanupSuggestion
{
    public required InstalledModel Model { get; init; }
    public required CleanupReason Reason { get; init; }
    public string? Explanation { get; init; }
    public float PriorityScore { get; init; }  // Higher = delete first
}

public enum CleanupReason
{
    NeverUsed,
    NotUsedRecently,
    LargeSize,
    BetterVersionAvailable,
    IncompatibleWithHardware,
    DuplicateQuantization
}

/// <summary>
/// Verifies model file integrity.
/// </summary>
public interface IModelIntegrityVerifier
{
    /// <summary>
    /// Verify a model file's integrity.
    /// </summary>
    Task<IntegrityResult> VerifyAsync(
        string filePath,
        string? expectedSha256 = null,
        CancellationToken ct = default);

    /// <summary>
    /// Compute the SHA256 hash of a model file.
    /// </summary>
    Task<string> ComputeHashAsync(
        string filePath,
        IProgress<long>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a GGUF file is valid and readable.
    /// </summary>
    Task<GgufValidation> ValidateGgufAsync(
        string filePath,
        CancellationToken ct = default);
}

public record IntegrityResult
{
    public bool IsValid { get; init; }
    public string? ComputedHash { get; init; }
    public string? ExpectedHash { get; init; }
    public string? Error { get; init; }
}

public record GgufValidation
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public GgufMetadata? Metadata { get; init; }
}

public record GgufMetadata
{
    public string? Architecture { get; init; }
    public long? ParameterCount { get; init; }
    public int? ContextLength { get; init; }
    public int? EmbeddingLength { get; init; }
    public int? HeadCount { get; init; }
    public int? LayerCount { get; init; }
    public QuantizationType? Quantization { get; init; }
    public IReadOnlyDictionary<string, object> RawMetadata { get; init; } =
        new Dictionary<string, object>();
}
```

### Download Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Model Download Pipeline                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. Pre-Download Checks                                                  │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • Check available disk space (model size + 20% buffer)             │  │
│  │ • Verify hardware compatibility                                    │  │
│  │ • Check for existing download/model                                │  │
│  │ • Validate license acceptance if required                          │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                         │                                                │
│                         ▼                                                │
│  2. Chunked Download with Resume                                        │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • Split into 10MB chunks                                           │  │
│  │ • Download chunks in parallel (4 connections default)              │  │
│  │ • Save progress to .partial file                                   │  │
│  │ • Support HTTP Range requests for resume                           │  │
│  │ • Retry individual chunks on failure                               │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                         │                                                │
│                         ▼                                                │
│  3. Integrity Verification                                              │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • Compute SHA256 hash (streaming, low memory)                      │  │
│  │ • Compare against registry-provided hash                           │  │
│  │ • Validate GGUF header and metadata                                │  │
│  │ • Re-download on mismatch                                          │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                         │                                                │
│                         ▼                                                │
│  4. Installation                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │ • Move from temp to model storage                                  │  │
│  │ • Register in local model database                                 │  │
│  │ • Optionally register with backend (Ollama create)                 │  │
│  │ • Cleanup temp files                                               │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘

File Layout:
┌─────────────────────────────────────────────────────────────────────────┐
│  ~/.lexichord/models/                                                    │
│  ├── downloads/                    # Active downloads                    │
│  │   ├── {guid}.partial            # Partial download data              │
│  │   └── {guid}.meta               # Download metadata                   │
│  ├── blobs/                        # Model files by hash                 │
│  │   └── sha256-{hash}             # Actual model file                   │
│  ├── manifests/                    # Model manifests                     │
│  │   └── {model-id}.json           # Model metadata                      │
│  └── temp/                         # Temporary files                     │
└─────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | View installed models only |
| WriterPro | Download up to 3 models; 50GB storage |
| Teams | Unlimited downloads; unlimited storage |
| Enterprise | + Network storage; shared model library |

---

## v0.16.4-LLM: Inference Runtime

**Goal:** Implement robust model loading, inference execution, and resource management with performance monitoring.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.16.4e | Model Loader | 12 |
| v0.16.4f | Inference Engine | 14 |
| v0.16.4g | Context Manager | 10 |
| v0.16.4h | Resource Monitor | 10 |
| v0.16.4i | Performance Profiler | 6 |
| v0.16.4j | Inference Runtime UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// High-level manager for local LLM inference.
/// </summary>
public interface ILocalInferenceManager
{
    /// <summary>
    /// Get currently loaded models.
    /// </summary>
    Task<IReadOnlyList<LoadedModel>> GetLoadedModelsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Load a model for inference.
    /// </summary>
    Task<LoadedModel> LoadModelAsync(
        string modelId,
        ModelLoadOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Unload a model from memory.
    /// </summary>
    Task UnloadModelAsync(
        Guid loadedModelId,
        CancellationToken ct = default);

    /// <summary>
    /// Unload all models.
    /// </summary>
    Task UnloadAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the active model (if any).
    /// </summary>
    Task<LoadedModel?> GetActiveModelAsync(CancellationToken ct = default);

    /// <summary>
    /// Set the active model for inference.
    /// </summary>
    Task SetActiveModelAsync(
        Guid loadedModelId,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a completion using the active model.
    /// </summary>
    Task<LocalGenerationResult> GenerateAsync(
        LocalGenerationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a streaming completion using the active model.
    /// </summary>
    IAsyncEnumerable<LocalGenerationChunk> GenerateStreamAsync(
        LocalGenerationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generate embeddings using the active model.
    /// </summary>
    Task<LocalEmbeddingResult> EmbedAsync(
        LocalEmbeddingRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get current resource usage.
    /// </summary>
    Task<InferenceResourceUsage> GetResourceUsageAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of inference events.
    /// </summary>
    IObservable<InferenceEvent> Events { get; }
}

public record LoadedModel
{
    public Guid Id { get; init; }
    public required InstalledModel Model { get; init; }
    public required ModelLoadOptions Options { get; init; }
    public required LoadedModelStatus Status { get; init; }
    public DateTimeOffset LoadedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public int ActiveRequests { get; init; }
    public LoadedModelMetrics Metrics { get; init; } = new();
    public LoadedModelResources Resources { get; init; } = new();
}

public enum LoadedModelStatus
{
    Loading,
    Ready,
    Busy,
    Unloading,
    Error
}

public record LoadedModelMetrics
{
    public int TotalRequests { get; init; }
    public int TotalTokensGenerated { get; init; }
    public TimeSpan TotalInferenceTime { get; init; }
    public float AverageTokensPerSecond { get; init; }
    public float PeakTokensPerSecond { get; init; }
    public TimeSpan AverageTimeToFirstToken { get; init; }
}

public record LoadedModelResources
{
    public long RamBytes { get; init; }
    public long? VramBytes { get; init; }
    public int LoadedLayers { get; init; }
    public int GpuLayers { get; init; }
    public int CpuLayers { get; init; }
    public int ContextSize { get; init; }
    public int ContextUsed { get; init; }
}

public record LocalGenerationRequest
{
    // Input
    public string? Prompt { get; init; }
    public IReadOnlyList<LocalChatMessage>? Messages { get; init; }
    public string? SystemPrompt { get; init; }

    // Generation parameters
    public int? MaxTokens { get; init; }
    public float Temperature { get; init; } = 0.7f;
    public float TopP { get; init; } = 0.9f;
    public int? TopK { get; init; }
    public float RepetitionPenalty { get; init; } = 1.1f;
    public float FrequencyPenalty { get; init; } = 0.0f;
    public float PresencePenalty { get; init; } = 0.0f;
    public IReadOnlyList<string>? StopSequences { get; init; }
    public int? Seed { get; init; }

    // Advanced
    public float? MinP { get; init; }
    public float? TypicalP { get; init; }
    public int? Mirostat { get; init; }  // 0, 1, or 2
    public float? MirostatTau { get; init; }
    public float? MirostatEta { get; init; }

    // Output format
    public LocalResponseFormat? Format { get; init; }
    public string? GrammarSpec { get; init; }  // GBNF grammar
}

public record LocalChatMessage
{
    public required LocalChatRole Role { get; init; }
    public required string Content { get; init; }
    public IReadOnlyList<string>? Images { get; init; }  // For multimodal
}

public enum LocalChatRole
{
    System,
    User,
    Assistant
}

public record LocalResponseFormat
{
    public LocalFormatType Type { get; init; }
    public string? Schema { get; init; }  // JSON schema for structured output
}

public enum LocalFormatType
{
    Text,
    Json,
    JsonSchema
}

public record LocalGenerationResult
{
    public required string Text { get; init; }
    public required LocalGenerationUsage Usage { get; init; }
    public string? FinishReason { get; init; }
    public LocalGenerationMetrics Metrics { get; init; } = new();
}

public record LocalGenerationChunk
{
    public required string Text { get; init; }
    public bool IsComplete { get; init; }
    public string? FinishReason { get; init; }
    public int TokenIndex { get; init; }
}

public record LocalGenerationUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}

public record LocalGenerationMetrics
{
    public TimeSpan TotalTime { get; init; }
    public TimeSpan TimeToFirstToken { get; init; }
    public TimeSpan GenerationTime { get; init; }
    public float TokensPerSecond { get; init; }
    public float PromptEvalTokensPerSecond { get; init; }
}

public record LocalEmbeddingRequest
{
    public required IReadOnlyList<string> Texts { get; init; }
    public bool Normalize { get; init; } = true;
}

public record LocalEmbeddingResult
{
    public required IReadOnlyList<float[]> Embeddings { get; init; }
    public int Dimensions { get; init; }
    public LocalEmbeddingUsage Usage { get; init; } = new();
}

public record LocalEmbeddingUsage
{
    public int TotalTokens { get; init; }
}

/// <summary>
/// Manages inference context and KV cache.
/// </summary>
public interface IContextManager
{
    /// <summary>
    /// Get context status for a loaded model.
    /// </summary>
    Task<ContextStatus> GetStatusAsync(
        Guid loadedModelId,
        CancellationToken ct = default);

    /// <summary>
    /// Clear the context/KV cache.
    /// </summary>
    Task ClearContextAsync(
        Guid loadedModelId,
        CancellationToken ct = default);

    /// <summary>
    /// Truncate context to free memory.
    /// </summary>
    Task TruncateContextAsync(
        Guid loadedModelId,
        int keepTokens,
        CancellationToken ct = default);

    /// <summary>
    /// Save context state for later restoration.
    /// </summary>
    Task<ContextSnapshot> SaveSnapshotAsync(
        Guid loadedModelId,
        CancellationToken ct = default);

    /// <summary>
    /// Restore context from a snapshot.
    /// </summary>
    Task RestoreSnapshotAsync(
        Guid loadedModelId,
        ContextSnapshot snapshot,
        CancellationToken ct = default);
}

public record ContextStatus
{
    public int MaxContextSize { get; init; }
    public int UsedTokens { get; init; }
    public int AvailableTokens => MaxContextSize - UsedTokens;
    public float UsagePercent => MaxContextSize > 0
        ? (float)UsedTokens / MaxContextSize * 100 : 0;
    public long KvCacheBytes { get; init; }
}

public record ContextSnapshot
{
    public Guid Id { get; init; }
    public Guid ModelId { get; init; }
    public int TokenCount { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public byte[] Data { get; init; } = [];
}

/// <summary>
/// Monitors system resources during inference.
/// </summary>
public interface IInferenceResourceMonitor
{
    /// <summary>
    /// Get current resource usage snapshot.
    /// </summary>
    Task<InferenceResourceUsage> GetCurrentUsageAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of resource usage updates.
    /// </summary>
    IObservable<InferenceResourceUsage> UsageUpdates { get; }

    /// <summary>
    /// Get resource usage history.
    /// </summary>
    Task<IReadOnlyList<InferenceResourceUsage>> GetHistoryAsync(
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Check if resources are sufficient for an operation.
    /// </summary>
    Task<ResourceCheck> CheckResourcesAsync(
        ResourceRequirements requirements,
        CancellationToken ct = default);

    /// <summary>
    /// Set resource alerts.
    /// </summary>
    Task SetAlertsAsync(
        ResourceAlertConfig config,
        CancellationToken ct = default);
}

public record InferenceResourceUsage
{
    public DateTimeOffset Timestamp { get; init; }

    // Memory
    public long SystemRamBytes { get; init; }
    public long SystemRamUsedBytes { get; init; }
    public long ProcessRamBytes { get; init; }
    public long ModelsRamBytes { get; init; }

    // GPU
    public IReadOnlyList<GpuResourceUsage> GpuUsage { get; init; } = [];

    // CPU
    public float CpuUsagePercent { get; init; }
    public float ProcessCpuPercent { get; init; }

    // Inference
    public int ActiveModels { get; init; }
    public int ActiveRequests { get; init; }
    public float CurrentTokensPerSecond { get; init; }
}

public record GpuResourceUsage
{
    public required string GpuId { get; init; }
    public required string GpuName { get; init; }
    public long VramTotalBytes { get; init; }
    public long VramUsedBytes { get; init; }
    public long VramModelsBytes { get; init; }
    public float GpuUtilizationPercent { get; init; }
    public float TemperatureCelsius { get; init; }
    public float PowerWatts { get; init; }
}

public record ResourceRequirements
{
    public long RamBytes { get; init; }
    public long? VramBytes { get; init; }
    public int? GpuLayers { get; init; }
}

public record ResourceCheck
{
    public bool IsSufficient { get; init; }
    public IReadOnlyList<ResourceIssue> Issues { get; init; } = [];
    public ResourceSuggestion? Suggestion { get; init; }
}

public record ResourceIssue
{
    public required ResourceType Resource { get; init; }
    public required string Message { get; init; }
    public long Required { get; init; }
    public long Available { get; init; }
}

public enum ResourceType { Ram, Vram, Disk, Cpu }

/// <summary>
/// Inference events for monitoring.
/// </summary>
public abstract record InferenceEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record ModelLoadStartedEvent(string ModelId, ModelLoadOptions Options) : InferenceEvent;
public record ModelLoadCompletedEvent(Guid LoadedModelId, TimeSpan Duration) : InferenceEvent;
public record ModelLoadFailedEvent(string ModelId, string Error) : InferenceEvent;
public record ModelUnloadedEvent(Guid LoadedModelId) : InferenceEvent;
public record GenerationStartedEvent(Guid LoadedModelId, int PromptTokens) : InferenceEvent;
public record GenerationCompletedEvent(Guid LoadedModelId, LocalGenerationMetrics Metrics) : InferenceEvent;
public record GenerationFailedEvent(Guid LoadedModelId, string Error) : InferenceEvent;
public record ResourceAlertEvent(ResourceType Resource, float UsagePercent, string Message) : InferenceEvent;
```

### Inference Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Inference Runtime Architecture                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    ILocalInferenceManager                        │    │
│  │                                                                  │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │    │
│  │  │ Load Model   │  │  Generate    │  │   Embed      │          │    │
│  │  │              │  │              │  │              │          │    │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │    │
│  │         │                 │                 │                   │    │
│  └─────────┼─────────────────┼─────────────────┼───────────────────┘    │
│            │                 │                 │                         │
│            ▼                 ▼                 ▼                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                      Request Router                              │    │
│  │  ┌───────────────────────────────────────────────────────────┐  │    │
│  │  │ • Select backend based on loaded model                     │  │    │
│  │  │ • Queue management for concurrent requests                 │  │    │
│  │  │ • Timeout and retry handling                               │  │    │
│  │  └───────────────────────────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│            │                                                             │
│            ▼                                                             │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    Backend Implementations                       │    │
│  │                                                                  │    │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐       │    │
│  │  │    Ollama     │  │   LM Studio   │  │   llama.cpp   │       │    │
│  │  │               │  │               │  │               │       │    │
│  │  │ POST /api/    │  │ POST /v1/     │  │ Native API    │       │    │
│  │  │   generate    │  │   completions │  │               │       │    │
│  │  └───────────────┘  └───────────────┘  └───────────────┘       │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    Supporting Services                           │    │
│  │                                                                  │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │    │
│  │  │  Context    │  │  Resource   │  │ Performance │              │    │
│  │  │  Manager    │  │  Monitor    │  │  Profiler   │              │    │
│  │  │             │  │             │  │             │              │    │
│  │  │ • KV cache  │  │ • RAM/VRAM  │  │ • Tokens/s  │              │    │
│  │  │ • Truncate  │  │ • CPU/GPU   │  │ • Latency   │              │    │
│  │  │ • Snapshot  │  │ • Alerts    │  │ • Throughput│              │    │
│  │  └─────────────┘  └─────────────┘  └─────────────┘              │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | 1 loaded model; basic generation |
| Teams | Multiple models; all parameters; context management |
| Enterprise | + Concurrent requests; profiling; snapshots |

---

## v0.16.5-LLM: Platform Integration

**Goal:** Seamlessly integrate local LLM inference with Lexichord's existing AI features, including orchestration, agents, and document interaction.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.16.5e | ILanguageModelProvider Implementation | 12 |
| v0.16.5f | LLM Router & Fallback | 12 |
| v0.16.5g | Orchestration Integration | 10 |
| v0.16.5h | Agent System Integration | 10 |
| v0.16.5i | Cost & Performance Optimizer | 8 |
| v0.16.5j | Local LLM Dashboard UI | 8 |

### Key Interfaces

```csharp
/// <summary>
/// ILanguageModelProvider implementation for local LLMs.
/// Enables drop-in replacement for cloud providers.
/// </summary>
public class LocalLlmProvider : ILanguageModelProvider
{
    public string ProviderId => "local";
    public string ProviderName => "Local LLM";

    public Task<bool> IsAvailableAsync(CancellationToken ct = default);

    public Task<IReadOnlyList<LanguageModel>> GetModelsAsync(
        CancellationToken ct = default);

    public Task<LanguageModelResponse> GenerateAsync(
        LanguageModelRequest request,
        CancellationToken ct = default);

    public IAsyncEnumerable<LanguageModelChunk> GenerateStreamAsync(
        LanguageModelRequest request,
        CancellationToken ct = default);

    public Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken ct = default);

    // Local-specific extensions
    public Task<LocalProviderStatus> GetStatusAsync(
        CancellationToken ct = default);

    public Task<LanguageModel?> GetActiveModelAsync(
        CancellationToken ct = default);

    public Task SetActiveModelAsync(
        string modelId,
        CancellationToken ct = default);
}

public record LocalProviderStatus
{
    public bool IsReady { get; init; }
    public string? ActiveModelId { get; init; }
    public string? ActiveModelName { get; init; }
    public InferenceBackendType? ActiveBackend { get; init; }
    public InferenceResourceUsage? ResourceUsage { get; init; }
    public IReadOnlyList<string> LoadedModels { get; init; } = [];
    public string? Error { get; init; }
}

/// <summary>
/// Routes LLM requests to appropriate providers based on configuration.
/// </summary>
public interface ILlmRouter
{
    /// <summary>
    /// Route a request to the best available provider.
    /// </summary>
    Task<RoutedRequest> RouteAsync(
        LanguageModelRequest request,
        RoutingOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get routing configuration.
    /// </summary>
    Task<RoutingConfig> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Update routing configuration.
    /// </summary>
    Task SetConfigAsync(
        RoutingConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Get routing statistics.
    /// </summary>
    Task<RoutingStats> GetStatsAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

public record RoutingOptions
{
    public RoutingStrategy Strategy { get; init; } = RoutingStrategy.PreferLocal;
    public bool AllowFallback { get; init; } = true;
    public TimeSpan? LocalTimeout { get; init; }
    public int? MinTokensForLocal { get; init; }
    public int? MaxTokensForLocal { get; init; }
}

public enum RoutingStrategy
{
    PreferLocal,        // Use local if available, fallback to cloud
    PreferCloud,        // Use cloud if available, fallback to local
    LocalOnly,          // Only use local, fail if unavailable
    CloudOnly,          // Only use cloud, fail if unavailable
    CostOptimized,      // Choose based on estimated cost
    PerformanceOptimized, // Choose based on expected latency
    QualityOptimized,   // Choose based on model quality for task
    RoundRobin,         // Alternate between providers
    LoadBalanced        // Route based on current load
}

public record RoutingConfig
{
    public RoutingStrategy DefaultStrategy { get; init; } = RoutingStrategy.PreferLocal;
    public bool EnableFallback { get; init; } = true;

    // Local preferences
    public string? PreferredLocalModel { get; init; }
    public int LocalTimeoutSeconds { get; init; } = 120;
    public int LocalMaxConcurrent { get; init; } = 2;

    // Cloud preferences
    public string? PreferredCloudProvider { get; init; }
    public string? PreferredCloudModel { get; init; }

    // Task-specific routing
    public IReadOnlyDictionary<TaskType, RoutingStrategy> TaskRouting { get; init; } =
        new Dictionary<TaskType, RoutingStrategy>();

    // Cost limits
    public decimal? DailyCloudBudget { get; init; }
    public decimal? MonthlyCloudBudget { get; init; }
}

public enum TaskType
{
    Chat,
    Completion,
    Embedding,
    CodeGeneration,
    Summarization,
    Translation,
    Analysis,
    CreativeWriting
}

public record RoutedRequest
{
    public required ILanguageModelProvider Provider { get; init; }
    public required string ModelId { get; init; }
    public required RoutingDecision Decision { get; init; }
}

public record RoutingDecision
{
    public required string ProviderId { get; init; }
    public required string Reason { get; init; }
    public RoutingStrategy UsedStrategy { get; init; }
    public bool WasFallback { get; init; }
    public IReadOnlyList<string> ConsideredProviders { get; init; } = [];
    public decimal? EstimatedCost { get; init; }
    public TimeSpan? EstimatedLatency { get; init; }
}

public record RoutingStats
{
    public TimeSpan Period { get; init; }
    public int TotalRequests { get; init; }
    public int LocalRequests { get; init; }
    public int CloudRequests { get; init; }
    public int FallbackRequests { get; init; }
    public int FailedRequests { get; init; }
    public decimal TotalCloudCost { get; init; }
    public decimal EstimatedLocalSavings { get; init; }
    public TimeSpan AverageLocalLatency { get; init; }
    public TimeSpan AverageCloudLatency { get; init; }
    public IReadOnlyDictionary<string, int> RequestsByModel { get; init; } =
        new Dictionary<string, int>();
}

/// <summary>
/// Manages fallback behavior between providers.
/// </summary>
public interface IFallbackManager
{
    /// <summary>
    /// Execute a request with automatic fallback.
    /// </summary>
    Task<FallbackResult<T>> ExecuteWithFallbackAsync<T>(
        Func<ILanguageModelProvider, CancellationToken, Task<T>> operation,
        FallbackOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get fallback chain for a request type.
    /// </summary>
    Task<IReadOnlyList<ILanguageModelProvider>> GetFallbackChainAsync(
        TaskType taskType,
        CancellationToken ct = default);

    /// <summary>
    /// Configure fallback behavior.
    /// </summary>
    Task ConfigureAsync(
        FallbackConfig config,
        CancellationToken ct = default);
}

public record FallbackOptions
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan InitialTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public float TimeoutMultiplier { get; init; } = 1.5f;
    public bool RetryOnRateLimit { get; init; } = true;
    public bool RetryOnTimeout { get; init; } = true;
    public bool RetryOnError { get; init; } = false;
}

public record FallbackResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? FinalProvider { get; init; }
    public int Attempts { get; init; }
    public IReadOnlyList<FallbackAttempt> AttemptHistory { get; init; } = [];
}

public record FallbackAttempt
{
    public required string ProviderId { get; init; }
    public required string ModelId { get; init; }
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Optimizes LLM usage for cost and performance.
/// </summary>
public interface ICostPerformanceOptimizer
{
    /// <summary>
    /// Get recommended provider/model for a request.
    /// </summary>
    Task<OptimizationRecommendation> GetRecommendationAsync(
        LanguageModelRequest request,
        OptimizationGoal goal,
        CancellationToken ct = default);

    /// <summary>
    /// Estimate cost for a request on different providers.
    /// </summary>
    Task<IReadOnlyList<CostEstimate>> EstimateCostsAsync(
        LanguageModelRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get usage analytics.
    /// </summary>
    Task<UsageAnalytics> GetAnalyticsAsync(
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Get cost savings from local LLM usage.
    /// </summary>
    Task<SavingsReport> GetSavingsReportAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

public enum OptimizationGoal
{
    MinimizeCost,
    MinimizeLatency,
    MaximizeQuality,
    BalanceCostLatency,
    BalanceAll
}

public record OptimizationRecommendation
{
    public required string ProviderId { get; init; }
    public required string ModelId { get; init; }
    public required OptimizationGoal Goal { get; init; }
    public float Score { get; init; }
    public string Rationale { get; init; } = "";
    public CostEstimate? CostEstimate { get; init; }
    public PerformanceEstimate? PerformanceEstimate { get; init; }
    public IReadOnlyList<AlternativeOption> Alternatives { get; init; } = [];
}

public record CostEstimate
{
    public required string ProviderId { get; init; }
    public required string ModelId { get; init; }
    public decimal EstimatedCost { get; init; }
    public string Currency { get; init; } = "USD";
    public int EstimatedInputTokens { get; init; }
    public int EstimatedOutputTokens { get; init; }
    public bool IsLocal { get; init; }
    public decimal? LocalElectricityCost { get; init; }
}

public record SavingsReport
{
    public TimeSpan Period { get; init; }
    public int LocalRequests { get; init; }
    public int LocalTokensGenerated { get; init; }
    public decimal EstimatedCloudCostAvoided { get; init; }
    public decimal ActualCloudCost { get; init; }
    public decimal EstimatedLocalElectricityCost { get; init; }
    public decimal NetSavings { get; init; }
    public float SavingsPercent { get; init; }
    public IReadOnlyList<ModelSavings> ByModel { get; init; } = [];
}

public record ModelSavings
{
    public required string ModelId { get; init; }
    public required string ModelName { get; init; }
    public int Requests { get; init; }
    public int TokensGenerated { get; init; }
    public decimal EstimatedSavings { get; init; }
    public string? ComparedToCloudModel { get; init; }
}

/// <summary>
/// Integrates local LLM with the orchestration system.
/// </summary>
public interface IOrchestrationLlmIntegration
{
    /// <summary>
    /// Configure LLM provider for orchestration.
    /// </summary>
    Task ConfigureProviderAsync(
        OrchestrationLlmConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Get LLM provider for a specific agent.
    /// </summary>
    Task<ILanguageModelProvider> GetProviderForAgentAsync(
        Guid agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Override provider for a workflow execution.
    /// </summary>
    Task SetWorkflowOverrideAsync(
        Guid workflowId,
        LlmOverride @override,
        CancellationToken ct = default);
}

public record OrchestrationLlmConfig
{
    public RoutingStrategy DefaultStrategy { get; init; } = RoutingStrategy.PreferLocal;
    public string? DefaultLocalModel { get; init; }
    public string? DefaultCloudProvider { get; init; }
    public string? DefaultCloudModel { get; init; }

    // Agent-specific overrides
    public IReadOnlyDictionary<string, LlmOverride> AgentOverrides { get; init; } =
        new Dictionary<string, LlmOverride>();

    // Task-specific defaults
    public IReadOnlyDictionary<TaskType, LlmOverride> TaskDefaults { get; init; } =
        new Dictionary<TaskType, LlmOverride>();
}

public record LlmOverride
{
    public string? ProviderId { get; init; }
    public string? ModelId { get; init; }
    public RoutingStrategy? Strategy { get; init; }
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
```

### Platform Integration Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Lexichord Platform Integration                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                       User-Facing Features                              │ │
│  │                                                                         │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐      │ │
│  │  │    Chat     │ │   Agents    │ │  Document   │ │  Workflows  │      │ │
│  │  │  Interface  │ │   System    │ │ Interaction │ │  & Canvas   │      │ │
│  │  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘      │ │
│  │         │               │               │               │              │ │
│  └─────────┼───────────────┼───────────────┼───────────────┼──────────────┘ │
│            │               │               │               │                 │
│            └───────────────┴───────────────┴───────────────┘                 │
│                                    │                                         │
│                                    ▼                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                          ILlmRouter                                     │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │ Route requests based on:                                          │  │ │
│  │  │ • User preference (local vs cloud)                                │  │ │
│  │  │ • Task type (code, creative, analysis)                            │  │ │
│  │  │ • Cost/performance optimization                                   │  │ │
│  │  │ • Model availability                                              │  │ │
│  │  │ • Context length requirements                                     │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│            ┌───────────────────────┴───────────────────────┐                │
│            ▼                                               ▼                │
│  ┌─────────────────────────┐               ┌─────────────────────────────┐ │
│  │    LocalLlmProvider     │               │     Cloud Providers         │ │
│  │                         │               │                             │ │
│  │  ILanguageModelProvider │               │  ┌─────────────────────┐   │ │
│  │                         │               │  │ OpenAI Provider     │   │ │
│  │  ┌───────────────────┐  │               │  ├─────────────────────┤   │ │
│  │  │ LocalInference    │  │               │  │ Anthropic Provider  │   │ │
│  │  │ Manager           │  │               │  ├─────────────────────┤   │ │
│  │  └───────────────────┘  │               │  │ Azure Provider      │   │ │
│  │           │             │               │  └─────────────────────┘   │ │
│  │           ▼             │               │                             │ │
│  │  ┌───────────────────┐  │               │                             │ │
│  │  │ Ollama/LM Studio  │  │               │                             │ │
│  │  │ Backend           │  │               │                             │ │
│  │  └───────────────────┘  │               │                             │ │
│  └─────────────────────────┘               └─────────────────────────────┘ │
│            │                                               │                │
│            ▼                                               ▼                │
│  ┌─────────────────────────┐               ┌─────────────────────────────┐ │
│  │   Local GPU/CPU         │               │   Cloud APIs                │ │
│  │   (User's Hardware)     │               │   (Internet)                │ │
│  └─────────────────────────┘               └─────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     IFallbackManager                                    │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │ Automatic fallback on:                                            │  │ │
│  │  │ • Local model unavailable → Try cloud                             │  │ │
│  │  │ • Local timeout → Retry or fallback                               │  │ │
│  │  │ • Cloud rate limit → Try local                                    │  │ │
│  │  │ • Cloud quota exceeded → Force local                              │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                  ICostPerformanceOptimizer                              │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │ • Track cloud API costs                                           │  │ │
│  │  │ • Estimate local electricity costs                                │  │ │
│  │  │ • Calculate savings from local usage                              │  │ │
│  │  │ • Suggest optimal model for task                                  │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### User Flow Examples

```
Example 1: User preference "Prefer Local"
┌─────────────────────────────────────────────────────────────────────────┐
│ User sends chat message                                                  │
│ → Router checks: Local model loaded? Yes (Llama 3 8B)                   │
│ → Router checks: Context fits? Yes (2k tokens < 8k limit)               │
│ → Route to LocalLlmProvider                                              │
│ → Generate response locally                                              │
│ → Return response (no cloud cost)                                        │
└─────────────────────────────────────────────────────────────────────────┘

Example 2: Local unavailable, fallback to cloud
┌─────────────────────────────────────────────────────────────────────────┐
│ User sends chat message                                                  │
│ → Router checks: Local model loaded? No                                  │
│ → Router checks: Can load model? No (insufficient VRAM)                 │
│ → Fallback enabled? Yes                                                  │
│ → Route to cloud provider (OpenAI)                                       │
│ → Log: "Used cloud due to: Local model unavailable"                     │
│ → Track cloud cost in analytics                                          │
└─────────────────────────────────────────────────────────────────────────┘

Example 3: Task-specific routing
┌─────────────────────────────────────────────────────────────────────────┐
│ Agent needs code generation                                              │
│ → Task type: CodeGeneration                                              │
│ → Config: CodeGeneration → PreferCloud (Claude for quality)             │
│ → Route to Anthropic Claude                                              │
│                                                                          │
│ Same agent needs summarization                                           │
│ → Task type: Summarization                                               │
│ → Config: Summarization → PreferLocal (cost savings)                    │
│ → Route to local Llama 3                                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | Basic routing (prefer local/cloud); manual fallback |
| Teams | Full routing options; automatic fallback; basic analytics |
| Enterprise | + Cost optimization; detailed analytics; task routing |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.16.x |
|:----------|:-------|:-----------------|
| `ILanguageModelProvider` | v0.12.3-AGT | Provider interface to implement |
| `IAgentExecutor` | v0.12.3-AGT | Agent LLM integration |
| `IWorkflowEngine` | v0.13.2-ORC | Workflow LLM routing |
| `IPromptStudio` | v0.14.4-STU | Prompt testing with local models |
| `ISettingsService` | v0.1.6a | User preferences for routing |
| `ILicenseService` | v0.2.1a | Feature gating |
| `IAuditLogger` | v0.11.2-SEC | Usage tracking |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `HardwareDetectedEvent` | v0.16.1 | Hardware capabilities detected |
| `BackendStatusChangedEvent` | v0.16.1 | Backend started/stopped |
| `ModelSearchedEvent` | v0.16.2 | Model search performed |
| `ModelDownloadStartedEvent` | v0.16.3 | Download started |
| `ModelDownloadCompletedEvent` | v0.16.3 | Download completed |
| `ModelInstalledEvent` | v0.16.3 | Model installed |
| `ModelDeletedEvent` | v0.16.3 | Model deleted |
| `ModelLoadedEvent` | v0.16.4 | Model loaded for inference |
| `ModelUnloadedEvent` | v0.16.4 | Model unloaded |
| `LocalGenerationCompletedEvent` | v0.16.4 | Local generation finished |
| `RequestRoutedEvent` | v0.16.5 | Request routed to provider |
| `FallbackTriggeredEvent` | v0.16.5 | Fallback to alternate provider |
| `CostThresholdReachedEvent` | v0.16.5 | Cloud cost limit reached |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Hardware.Info` | 11.x | Cross-platform hardware detection |
| `NvAPIWrapper` | 0.9.x | NVIDIA GPU info (Windows) |
| `Ollama.NET` | 1.x | Ollama API client |
| `Huggingface.NET` | 1.x | Hugging Face Hub API |
| `gguf-parser` | 1.x | GGUF metadata reading |

---

## Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Hardware detection | <2s |
| Backend health check | <500ms |
| Model search (cached) | <200ms |
| Model search (API) | <2s |
| Download speed | >10 MB/s (network permitting) |
| Model load (7B Q4) | <30s |
| Model load (13B Q4) | <60s |
| First token latency | <500ms |
| Generation (7B GPU) | >30 tokens/s |
| Generation (7B CPU) | >5 tokens/s |
| Routing decision | <10ms |
| Fallback switch | <100ms |

---

## What This Enables

With v0.16.x complete, Lexichord users can:

- **Run AI Locally:** Complete privacy, no data leaves the device
- **Reduce Costs:** Eliminate per-token API costs for many tasks
- **Work Offline:** Full functionality without internet connection
- **Choose Models:** Select from thousands of open-source models
- **Optimize Performance:** Automatic routing for best cost/speed/quality
- **Hybrid Operation:** Seamlessly combine local and cloud AI

This positions Lexichord as a flexible AI platform that works however users need it to.

---

## Total Local LLM Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 11: Local LLM | v0.16.1 - v0.16.5 | ~274 |

**Combined with prior phases:** ~2,016 hours (~50 person-months)

---

## Complete Platform Summary

| Phase | Focus | Versions | Hours |
|:------|:------|:---------|:------|
| 1-4 | Foundation (CKVS) | v0.4.x - v0.7.x | ~273 |
| 5 | Advanced Knowledge | v0.10.x | ~214 |
| 6 | Security | v0.11.x | ~209 |
| 7 | Agent Infrastructure | v0.12.x | ~248 |
| 8 | Orchestration | v0.13.x | ~276 |
| 9 | Agent Studio | v0.14.x | ~264 |
| 10 | Marketplace | v0.15.x | ~258 |
| 11 | Local LLM | v0.16.x | ~274 |
| **Total** | | | **~2,016 hours** |

**Approximately 50 person-months** to complete the full platform vision with local LLM support.

---
