# Lexichord Changelog

All notable changes to the Lexichord project are documented here.

This changelog is written for stakeholders and users, focusing on **what changed** rather than technical implementation details. For detailed technical changes, see the version-specific changelogs in this directory.

---

## [v0.7.7] - 2026-02 (In Progress)

### Agent Workflows (Visual Workflow Designer)

This release introduces the Workflow Designer — a visual, drag-and-drop builder for creating multi-step agent pipelines with conditional execution, YAML import/export, and comprehensive validation.

#### What's New

- **Workflow Designer UI (v0.7.7a)** — Core infrastructure for the visual workflow designer in `Lexichord.Modules.Agents.Workflows` and `Lexichord.Modules.Agents.ViewModels`. Added `WorkflowDefinition` record with `WorkflowStepDefinition`, `WorkflowStepCondition`, `ConditionType` enum, `WorkflowMetadata` (8 properties including author, tags, category), and `WorkflowCategory` enum (5 categories). Added `IWorkflowDesignerService` interface with 10 methods for CRUD, validation, YAML export/import, duplication, and listing; `WorkflowDesignerService` implementation using `ConcurrentDictionary` in-memory store with 5 validation error rules (`MISSING_NAME`, `EMPTY_WORKFLOW`, `UNKNOWN_AGENT`, `UNKNOWN_PERSONA`, `DUPLICATE_STEP_ID`) and 3 warning rules (`SINGLE_STEP`, `MISSING_DESCRIPTION`, `ALL_SAME_AGENT`). Added `WorkflowYamlSerializer` with YamlDotNet snake_case DTO mapping and `WorkflowImportException` custom exception. Added `IWorkflowDesignerViewModel` interface with 10 commands (`NewWorkflow`, `Save`, `Load`, `Validate`, `AddStep`, `RemoveStep`, `ReorderStep`, `ExportYaml`, `ImportYaml`, `RunWorkflow`); `WorkflowDesignerViewModel` implementation with agent palette population from `IAgentRegistry`, step management, license gating (`LicenseTier.Teams`+), and `BuildCurrentDefinition` helper. Added `WorkflowStepViewModel` (ObservableObject) with identity, configuration, condition, and UI state properties; `AgentPaletteItemViewModel` and `PersonaOption` records. Added `IConfigurationService` minimal abstraction. DI registration: `IWorkflowDesignerService → WorkflowDesignerService` (Singleton), `WorkflowDesignerViewModel` (Transient). License gating: editing requires Teams+ tier. Includes 19 unit tests with 100% pass rate (0 regressions on 11,047-test suite). [Detailed changelog](v0.7.x/LCS-CL-v0.7.7a.md)
- **Workflow Engine (v0.7.7b)** — Runtime execution engine for workflow definitions created by the Workflow Designer. Added `IWorkflowEngine` interface with `ExecuteAsync`, `ExecuteStreamingAsync`, `ValidateExecution`, and `EstimateTokens`; `WorkflowEngine` implementation with sequential agent execution, condition evaluation (Always, PreviousSuccess, PreviousFailed, Expression), data passing via shared variables, `StopOnFirstFailure` handling, per-step timeout, and cancellation support. Added `IExpressionEvaluator` interface and `ExpressionEvaluator` implementation using DynamicExpresso.Core for sandboxed condition evaluation with built-in functions (`count`, `any`, `isEmpty`, `contains`). Added 10 execution records/enums (`WorkflowExecutionContext`, `WorkflowExecutionOptions`, `WorkflowExecutionResult`, `WorkflowExecutionStatus`, `WorkflowStepExecutionResult`, `WorkflowStepStatus`, `WorkflowUsageMetrics`, `AgentUsageMetrics`, `WorkflowExecutionValidation`, `WorkflowTokenEstimate`). Added 5 MediatR events (`WorkflowStartedEvent`, `WorkflowStepStartedEvent`, `WorkflowStepCompletedEvent`, `WorkflowCompletedEvent`, `WorkflowCancelledEvent`). DI registration: `IExpressionEvaluator → ExpressionEvaluator` (Singleton), `IWorkflowEngine → WorkflowEngine` (Singleton). Includes 13 unit tests with 100% pass rate (0 regressions on 11,061-test suite). [Detailed changelog](v0.7.x/LCS-CL-v0.7.7b.md)
- **Preset Workflows (v0.7.7c)** — 5 production-ready preset workflows as embedded YAML resources. Added `IPresetWorkflowRepository` interface with `GetAll`, `GetById`, `GetByCategory`, `IsPreset`, `GetSummaries` methods; `PresetWorkflowRepository` implementation loading embedded YAML resources at construction with YamlDotNet/snake_case deserialization and internal DTOs (`WorkflowYamlDto`, `WorkflowStepYamlDto`, `WorkflowConditionYamlDto`, `WorkflowMetadataYamlDto`). Added `PresetWorkflowSummary` record (9 properties) for gallery display. Added 5 preset YAML workflows: `preset-technical-review` (4 steps: editor→simplifier→tuning→summarizer, WriterPro), `preset-marketing-polish` (4 steps: simplifier→editor→tuning→summarizer, WriterPro), `preset-quick-edit` (1 step: editor, WriterPro), `preset-academic-review` (3 steps: editor→tuning→summarizer, Teams), `preset-executive-summary` (3 steps: simplifier→editor→summarizer, Teams). DI registration: `IPresetWorkflowRepository → PresetWorkflowRepository` (Singleton). Includes 16 unit tests with 100% pass rate (0 regressions on 11,076-test suite). [Detailed changelog](v0.7.x/LCS-CL-v0.7.7c.md)
- **Workflow Execution UI (v0.7.7d)** — ViewModel and history service for workflow execution progress tracking, cancellation, and result actions. Added `IWorkflowExecutionViewModel` interface (12 properties, 6 commands); `WorkflowExecutionViewModel` implementation with progress calculation (§5.1: completed/running weights), estimated remaining time (§5.2: ≥2 steps required), event handling for `WorkflowStepStartedEvent`/`WorkflowStepCompletedEvent`/`WorkflowCompletedEvent`/`WorkflowCancelledEvent`, cancellation flow with double-click prevention. Added `WorkflowStepExecutionState` (ObservableObject) with step identity, mutable status, output preview (200-char truncation), and usage metrics. Added `IWorkflowExecutionHistoryService` interface (4 methods: Record, GetHistory, GetStatistics, ClearHistory); `WorkflowExecutionHistoryService` implementation using in-memory `ConcurrentDictionary` with 100-entry bounded store. Added `WorkflowExecutionSummary` record (10 properties) and `WorkflowExecutionStatistics` record (12 properties). Result actions: Apply to document via `IEditorInsertionService.ReplaceSelectionAsync`, Copy to clipboard via `IClipboardService.SetTextAsync`. DI registration: `IWorkflowExecutionHistoryService → WorkflowExecutionHistoryService` (Singleton), `WorkflowExecutionViewModel` (Transient). Includes 21 unit tests with 100% pass rate (0 regressions on 11,098-test suite). [Detailed changelog](v0.7.x/LCS-CL-v0.7.7d.md)
- **Validation Step Types (v0.7.7e)** — Unified validation step types for the Validation Workflows system (CKVS Phase 4d). Added `IWorkflowStep` base interface (identity, ordering, timeout, execute, validate); `IValidationWorkflowStep` extended interface (StepType, Options, FailureAction, FailureSeverity, IsAsync, GetValidationRulesAsync, ExecuteValidationAsync); `ValidationWorkflowStep` implementation with `IUnifiedValidationService` delegation, linked `CancellationTokenSource` timeout enforcement, exhaustive logging, and configuration validation. Added `ValidationWorkflowStepFactory` for dependency-injected step creation. Added 4 enums (`ValidationStepType` with 7 types: Schema/CrossReference/Consistency/Custom/Grammar/KnowledgeGraphAlignment/Metadata; `ValidationFailureAction` with 4 actions: Halt/Continue/Branch/Notify; `ValidationFailureSeverity` with 4 levels: Info/Warning/Error/Critical; `ValidationTrigger` with 6 triggers). Added 4 records (`ValidationRule`, `ValidationStepResult` with `ValidationStepError`/`ValidationStepWarning`, `ValidationWorkflowStepOptions`, `ValidationWorkflowContext`). Added `WorkflowStepResult` base result record and `ValidationConfigurationError` record. DI registration: `ValidationWorkflowStepFactory` (Singleton). License gating: WriterPro (Schema, Grammar), Teams (all types), Enterprise (unlimited rules). Includes 10 unit tests with 100% pass rate (0 regressions on 11,108-test suite). [Detailed changelog](v0.7.x/LCS-CL-v0.7.7e.md)

---

## [v0.7.6] - 2026-02 (Complete)

### The Summarizer Agent (Multi-Mode Document Summarization)

This release introduces the Summarizer Agent system, enabling multi-mode document summarization with six output formats, natural language command parsing, intelligent document chunking, and mode-specific prompt templates.

#### What's New

- **Summarization Modes (v0.7.6a)** — Multi-mode summarization engine in `Lexichord.Abstractions.Agents.Summarizer` and `Lexichord.Modules.Agents.Summarizer`. Added `ISummarizerAgent` interface extending `IAgent` with methods for file-based summarization (`SummarizeAsync()`), content-based summarization (`SummarizeContentAsync()`), natural language command parsing (`ParseCommand()`), and mode-specific defaults (`GetDefaultOptions()`). Added `SummarizationMode` enum with six output formats: `Abstract` (formal academic prose, 150-300 words), `TLDR` (single paragraph, 50-100 words), `BulletPoints` (configurable "•" list, default 5 items), `KeyTakeaways` (numbered "**Takeaway N:**" items with explanations), `Executive` (business prose, 100-200 words), `Custom` (user-defined prompt). Added `SummarizationOptions` record with `Mode`, `MaxItems` (1-10, default 5), `TargetWordCount` (10-1000, nullable), `CustomPrompt` (required for Custom mode), `IncludeSectionSummaries`, `TargetAudience`, `PreserveTechnicalTerms` (default true), `MaxResponseTokens` (default 2048); `Validate()` method enforcing range constraints and Custom mode requirements. Added `SummarizationResult` record with `Summary` (required), `Mode`, `Items` (for list modes), `OriginalReadingMinutes` (200 wpm), `OriginalWordCount`, `SummaryWordCount`, `CompressionRatio`, `Usage` (required UsageMetrics), `GeneratedAt`, `Model`, `WasChunked`, `ChunkCount`, `Success`, `ErrorMessage`; static `Failed()` factory for error results. Added `SummarizerAgent` sealed class implementing `IAgent` and `ISummarizerAgent` with `[RequiresLicense(LicenseTier.WriterPro, FeatureCode = FeatureCodes.SummarizerAgent)]` and `[AgentDefinition("summarizer", Priority = 102)]` attributes; constructor dependencies: `IChatCompletionService`, `IPromptRenderer`, `IPromptTemplateRepository`, `IContextOrchestrator`, `IFileService`, `ILicenseContext`, `IMediator`, `ILogger<SummarizerAgent>`; agent properties AgentId="summarizer", Name="The Summarizer", Description="Summarizes documents in multiple formats: abstract, TLDR, bullet points, key takeaways, and executive summary.", Capabilities=Chat|DocumentContext|Summarization; invocation flow: validate options → publish SummarizationStartedEvent → estimate tokens (content.Length / 4) → chunk if needed (4000 tokens/chunk, 100-token overlap, split on headings → paragraphs → sentences) → assemble context via IContextOrchestrator → render mode-specific prompt → invoke LLM via IChatCompletionService → parse response and extract items → calculate metrics (word counts, compression ratio, reading time) → publish SummarizationCompletedEvent → return SummarizationResult; 3-catch error handling (user cancellation → timeout → generic) with SummarizationFailedEvent publishing; regex-based ParseCommand with keyword detection for mode, numeric extraction for MaxItems, audience phrase extraction for TargetAudience, false positive filtering for common pronouns. Added MediatR events: `SummarizationStartedEvent` (Mode, CharacterCount, DocumentPath, Timestamp), `SummarizationCompletedEvent` (Mode, OriginalWordCount, SummaryWordCount, CompressionRatio, WasChunked, Duration, Timestamp), `SummarizationFailedEvent` (Mode, ErrorMessage, DocumentPath, Timestamp); all with static `Create()` factory methods. Added `specialist-summarizer.yaml` Mustache prompt template with mode-specific instructions, audience adaptation, technical term handling, and chunking context. Added `FeatureCodes.SummarizerAgent = "Feature.SummarizerAgent"` constant. Added `AddSummarizerAgentPipeline()` DI extension registering `ISummarizerAgent → SummarizerAgent` as Singleton with `IAgent` forwarding. Updated `AgentsModule` with registration and init verification. License gating: requires WriterPro tier. No new NuGet packages. Includes 44 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6a.md)

- **Metadata Extraction (v0.7.6b)** — Automated metadata extraction service in `Lexichord.Abstractions.Agents.MetadataExtraction` and `Lexichord.Modules.Agents.MetadataExtraction`. Added `IMetadataExtractor` interface extending `IAgent` with methods for file-based extraction (`ExtractAsync()`), content-based extraction (`ExtractFromContentAsync()`), tag suggestion (`SuggestTagsAsync()` with existing taxonomy alignment), algorithmic reading time calculation (`CalculateReadingTime()` without LLM), key term extraction (`ExtractKeyTermsAsync()`), and default options retrieval (`GetDefaultOptions()`). Added `DocumentType` enum with 15 document classification types: `Unknown`, `Article`, `Tutorial`, `HowTo`, `Reference`, `APIDocumentation`, `Specification`, `Report`, `Whitepaper`, `Proposal`, `Meeting`, `Notes`, `Readme`, `Changelog`, `Other`. Added `KeyTerm` record with `Term` (required), `Importance` (0.0-1.0, default 0.5), `Frequency` (default 1), `IsTechnical` (default false), `Definition?`, `Category?`, `RelatedTerms?`; factory methods `Create()` for technical terms and `CreateSimple()` for basic terms. Added `MetadataExtractionOptions` record with `MaxKeyTerms` (1-50, default 10), `MaxConcepts` (1-20, default 5), `MaxTags` (1-20, default 5), `MinimumTermImportance` (0.0-1.0, default 0.3), `WordsPerMinute` (100-400, default 200), `ExistingTags?`, `ExtractNamedEntities` (default true), `InferAudience` (default true), `CalculateComplexity` (default true), `DetectDocumentType` (default true), `IncludeDefinitions` (default false), `MaxResponseTokens` (default 2048); `Validate()` method; static `Default` property. Added `DocumentMetadata` record with `Title?`, `Description?`, `KeyTerms` (required), `Concepts` (required), `SuggestedTags` (required), `ReadingTimeMinutes`, `ComplexityScore` (1-10), `WordCount`, `DocumentType`, `TargetAudience?`, `NamedEntities?`, `Usage` (required UsageMetrics), `ExtractedAt`, `Success`, `ErrorMessage?`; factory methods `Failed()` and `CreateMinimal()`. Added `MetadataExtractor` class implementing `IAgent` and `IMetadataExtractor` with `[RequiresLicense(LicenseTier.WriterPro, FeatureCode = FeatureCodes.MetadataExtraction)]`, `[AgentDefinition("metadata-extractor", Priority = 103)]`; AgentId "metadata-extractor", Name "The Metadata Extractor"; Capabilities Chat|DocumentContext|Summarization|StructureAnalysis; reading time algorithm with word count base + code block adjustment (0.5 min each) + table adjustment (0.5 min each) + image adjustment (0.2 min each) + complexity multipliers (+10% for avg sentence >25 words, +20% for >10% technical density); complexity scoring algorithm with base 5 ± technical density adjustment ± sentence length adjustment ± structure adjustment clamped to 1-10; Levenshtein distance for fuzzy tag matching (>0.8 similarity); JSON response parsing with fallback extraction; 3-catch error handling pattern; safe event publishing helper. Added MediatR events: `MetadataExtractionStartedEvent` (CharacterCount, DocumentPath, Timestamp), `MetadataExtractionCompletedEvent` (DocumentType, KeyTermCount, ComplexityScore, Duration, Timestamp), `MetadataExtractionFailedEvent` (ErrorMessage, DocumentPath, Timestamp); all with static `Create()` factory methods. Added `metadata-extractor.yaml` Mustache prompt template with JSON output schema for key terms, concepts, tags, document type, target audience, and named entities. Added `FeatureCodes.MetadataExtraction = "Feature.MetadataExtraction"` constant. Added `AddMetadataExtractionPipeline()` DI extension registering `IMetadataExtractor → MetadataExtractor` as Singleton with `IAgent` forwarding. Updated `AgentsModule` with registration and init verification. License gating: requires WriterPro tier. No new NuGet packages. Includes 121 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6b.md)

- **Export Formats (v0.7.6c)** — Multi-destination summary export service in `Lexichord.Abstractions.Agents.SummaryExport` and `Lexichord.Modules.Agents.SummaryExport`. Added `ISummaryExporter` interface with methods for destination-based export (`ExportAsync()`), metadata-only export (`ExportMetadataAsync()`), frontmatter updates (`UpdateFrontmatterAsync()`), cache operations (`GetCachedSummaryAsync()`, `CacheSummaryAsync()`, `ClearCacheAsync()`), and panel display (`ShowInPanelAsync()`). Added `ExportDestination` enum with 5 targets: `Panel` (Summary Panel UI), `Frontmatter` (YAML frontmatter injection), `File` (standalone .summary.md), `Clipboard` (system clipboard), `InlineInsert` (cursor position with callout formatting). Added `FrontmatterFields` flags enum with selective field inclusion: `Abstract`, `Tags`, `KeyTerms`, `ReadingTime`, `Category`, `Audience`, `GeneratedAt`, `All`. Added `SummaryExportOptions` record with `Destination`, `OutputPath` (nullable, auto-generates for File), `Fields`, `Overwrite`, `IncludeMetadata`, `IncludeSourceReference`, `ExportTemplate` (custom Mustache template), `ClipboardAsMarkdown`, `UseCalloutBlock`, `CalloutType` (info/note/tip/warning); `Validate()` method; `ForDestination()` factory. Added `SummaryExportResult` record with `Success`, `Destination`, `OutputPath`, `ErrorMessage`, `BytesWritten`, `CharactersWritten`, `DidOverwrite`, `ExportedAt`; factory methods `Succeeded()`, `Failed()`. Added `CachedSummary` record with `DocumentPath`, `ContentHash` (SHA256), `Summary` (SummarizationResult), `Metadata` (nullable DocumentMetadata), `CachedAt`, `ExpiresAt` (default +7 days); computed `IsExpired`, `Age`; `Create()` factory with validation. Added `IClipboardService` general clipboard abstraction with `SetTextAsync()`, `GetTextAsync()`, `ClearAsync()`, `ContainsTextAsync()`. Added `SummaryExporter` implementation with `IFileService`, `IEditorService`, `IClipboardService`, `ISummaryCacheService`, `ILicenseContext`, `IMediator`, `ILogger` dependencies; destination-specific handlers for Panel (cache + event), Frontmatter (YAML parse/merge/serialize), File (template-based Markdown generation), Clipboard (optional Markdown stripping), InlineInsert (callout-wrapped insertion at cursor); 3-catch error handling pattern. Added `SummaryCacheService` hybrid caching with `IMemoryCache` (4-hour sliding) + JSON file persistence (`.lexichord/cache/summaries/{hash}.json`, 7-day expiration); SHA256 content-hash invalidation. Added `ClipboardService` Avalonia wrapper with `Dispatcher.UIThread` marshalling. Added MediatR events: `SummaryExportStartedEvent`, `SummaryExportedEvent`, `SummaryExportFailedEvent`, `SummaryPanelOpenedEvent`; all with static `Create()` factory methods. Added `SummaryPanelViewModel` (CommunityToolkit.Mvvm) with `RefreshCommand`, `CopySummaryCommand`, `AddToFrontmatterCommand`, `ExportFileCommand`, `CopyKeyTermsCommand`, `ClearCacheCommand`, `CloseCommand`; observable `KeyTerms` collection with importance visualization. Added `SummaryPanelView.axaml` Avalonia panel with header, content area, metadata section, key term chips, action bar; keyboard shortcuts (Ctrl+R/C/S, Escape). Added `KeyTermChip.axaml` component with filled/empty dot importance display. Added `FeatureCodes.SummaryExport = "Feature.SummaryExport"` constant. Added `AddSummaryExportPipeline()` DI extension registering `ISummaryCacheService`, `IClipboardService`, `ISummaryExporter` as Singletons, `SummaryPanelViewModel` as Transient. Updated `AgentsModule` with registration and init verification. License gating: requires WriterPro tier. No new NuGet packages. Includes 160+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6c.md)

- **Document Comparison (v0.7.6d)** — Semantic document comparison service in `Lexichord.Abstractions.Agents.DocumentComparison` and `Lexichord.Modules.Agents.DocumentComparison`. Added `IDocumentComparer` interface with methods for file-based comparison (`CompareAsync()`), content-based comparison (`CompareContentAsync()`), git version comparison (`CompareWithGitVersionAsync()`), and pure text diff (`GetTextDiff()`). Added `ChangeCategory` enum with 8 change types: `Added` (new content), `Removed` (deleted content), `Modified` (changed content), `Restructured` (moved content), `Clarified` (rewritten for clarity), `Formatting` (presentation changes), `Correction` (error fixes), `Terminology` (naming changes). Added `ChangeSignificance` enum with 4 levels: `Low` (0.0-0.3), `Medium` (0.3-0.6), `High` (0.6-0.8), `Critical` (0.8-1.0); with `FromScore()`, `GetMinimumScore()`, `GetDisplayLabel()` extension methods. Added `LineRange` record with `Start`, `End`, computed `LineCount`, `IsSingleLine`, `IsValid`; methods `Validate()`, `Contains()`, `Overlaps()`, `ToString()`. Added `DocumentChange` record with `Category`, `Section`, `Description`, `Significance` (0.0-1.0), `OriginalText`, `NewText`, `Impact`, `OriginalLineRange`, `NewLineRange`, `RelatedChangeIndices`; computed `SignificanceLevel`, `HasDiff`; `Validate()` method. Added `ComparisonOptions` record with `SignificanceThreshold` (default 0.2), `IncludeFormattingChanges` (default false), `GroupBySection` (default true), `MaxChanges` (default 20), `FocusSections`, `IncludeTextDiff` (default false), `IdentifyRelatedChanges` (default true), `OriginalVersionLabel`, `NewVersionLabel`, `MaxResponseTokens` (default 4096); `Validate()` method; `WithLabels()` factory; static `Default` property. Added `ComparisonResult` record with `Summary`, `Changes`, `ChangeMagnitude` (0.0-1.0), `AffectedSections`, `Usage`, `ComparedAt`, `OriginalPath`, `NewPath`, `TextDiff`; computed `AreIdentical`, `ChangeCount`, `AdditionCount`, `DeletionCount`, `ModificationCount`, `CriticalCount`, `HighCount`, `MediumCount`, `LowCount`; factory methods `Identical()`, `Failed()`. Added `DocumentComparer` implementation with `IChatCompletionService`, `IPromptRenderer`, `IPromptTemplateRepository`, `IFileService`, `ILicenseContext`, `IMediator`, `ILogger` dependencies; hybrid DiffPlex + LLM semantic analysis; JSON response parsing with category/significance extraction; significance filtering; 3-catch error handling pattern. Added MediatR events: `DocumentComparisonStartedEvent` (OriginalPath, NewPath, OriginalCharacterCount, NewCharacterCount, Timestamp), `DocumentComparisonCompletedEvent` (OriginalPath, NewPath, ChangeCount, ChangeMagnitude, Duration, Timestamp) with `AreIdentical`, `MagnitudePercentage` computed properties, `DocumentComparisonFailedEvent` (OriginalPath, NewPath, ErrorMessage, Timestamp); all with static `Create()` factory methods. Added `ComparisonViewModel` (CommunityToolkit.Mvvm) with `CompareCommand`, `RefreshCommand`, `CopyDiffCommand`, `ExportReportCommand`, `CloseCommand`; observable `Result`, `IsLoading`, `ErrorMessage`, `ChangeCards`, `CriticalChanges`, `HighChanges`, `MediumChanges`, `LowChanges`. Added `ChangeCardViewModel` wrapping `DocumentChange` with `IsExpanded`, `CategoryIcon`, `CategoryColor`, `SignificanceColor`, `ExpandCollapseIcon`, `HasDiffContent`, `ShowDiffArrow`, `ToggleExpandCommand`. Added `ComparisonView.axaml` Avalonia panel with header, summary, magnitude bar, change groups by significance; keyboard shortcuts (Escape, Ctrl+R/C/E). Added `ChangeCard.axaml` component with category badge, significance indicator, expandable diff view, keyboard accessibility. Added `FeatureCodes.DocumentComparison = "Feature.DocumentComparison"` constant. Added `AddDocumentComparisonPipeline()` DI extension registering `IDocumentComparer → DocumentComparer` as Singleton, `ComparisonViewModel` and `ChangeCardViewModel` as Transient. Updated `AgentsModule` with registration and init verification. License gating: requires WriterPro tier. Uses existing DiffPlex 1.7.2 dependency. Includes 164 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6d.md)

- **Sync Service Core (v0.7.6e)** — Bidirectional synchronization service between documents and the knowledge graph in `Lexichord.Abstractions.Contracts.Knowledge.Sync` and `Lexichord.Modules.Knowledge.Sync`. Added `ISyncService` interface with methods for document-to-graph synchronization (`SyncDocumentToGraphAsync()`), finding affected documents from graph changes (`GetAffectedDocumentsAsync()`), status retrieval (`GetSyncStatusAsync()`), conflict resolution (`ResolveConflictAsync()`), and sync need checking (`NeedsSyncAsync()`). Added `ISyncOrchestrator` internal interface with methods for pipeline execution (`ExecuteDocumentToGraphAsync()`, `ExecuteGraphToDocumentAsync()`) and conflict detection (`DetectConflictsAsync()`). Added `ISyncStatusTracker` interface for per-document sync state tracking (`GetStatusAsync()`, `UpdateStatusAsync()`). Added `ISyncConflictDetector` interface for extraction-to-graph conflict detection (`DetectAsync()`). Added `IConflictResolver` interface for applying resolution strategies (`ResolveAsync()`). Added 7 enums: `SyncOperationStatus` (Success, SuccessWithConflicts, PartialSuccess, Failed, NoChanges), `SyncState` (InSync, PendingSync, NeedsReview, Conflict, NeverSynced), `ConflictType` (ValueMismatch, MissingInGraph, MissingInDocument, RelationshipMismatch, ConcurrentEdit), `ConflictSeverity` (Low, Medium, High), `ConflictResolutionStrategy` (UseDocument, UseGraph, Manual, Merge, DiscardDocument, DiscardGraph), `ChangeType` (EntityCreated, EntityUpdated, EntityDeleted, RelationshipCreated, RelationshipDeleted, PropertyChanged), `SyncDirection` (DocumentToGraph, GraphToDocument, Bidirectional). Added 5 records: `SyncResult` (Status, EntitiesAffected, ClaimsAffected, RelationshipsAffected, Conflicts, Duration, ErrorMessage, CompletedAt), `SyncStatus` (DocumentId, State, LastSyncAt, PendingChanges, LastAttemptAt, LastError, UnresolvedConflicts, IsSyncInProgress), `SyncConflict` (ConflictTarget, DocumentValue, GraphValue, DetectedAt, Type, Severity, Description), `SyncContext` (UserId, Document, WorkspaceId, AutoResolveConflicts, DefaultConflictStrategy, PublishEvents, Timeout), `GraphChange` (EntityId, ChangeType, PreviousValue, NewValue, ChangedBy, ChangedAt). Added `SyncService` implementation with `ISyncOrchestrator`, `ISyncStatusTracker`, `IGraphRepository`, `IDocumentRepository`, `ILicenseContext`, `IMediator`, `ILogger` dependencies; license gating (WriterPro for doc-to-graph, Teams for full bidirectional); status tracking with in-progress flags; MediatR event publishing; 3-catch error handling pattern. Added `SyncOrchestrator` internal implementation coordinating the sync pipeline: entity extraction via `IEntityExtractionPipeline`, conflict detection via `ISyncConflictDetector`, auto-resolution of low-severity conflicts, entity upsert via `IGraphRepository`, duration tracking. Added `SyncStatusTracker` implementation using `ConcurrentDictionary` for thread-safe in-memory status storage with default `NeverSynced` state. Added `SyncConflictDetector` implementation comparing extraction results against existing graph state for value mismatches and missing entities. Added `ConflictResolver` implementation applying resolution strategies (UseDocument, UseGraph, Merge via timestamp comparison). Added `SyncCompletedEvent` MediatR notification with `DocumentId`, `Result`, `Direction`, `Timestamp` properties and static `Create()` factory method. Added `FeatureCodes.SyncService = "Feature.SyncService"` constant. Added `AddSyncServicePipeline()` DI extension registering `SyncStatusTracker`, `SyncConflictDetector`, `ConflictResolver`, `SyncOrchestrator`, `SyncService` as Singletons with interface forwarding. Updated `KnowledgeModule` with registration and init verification. License gating: WriterPro for document-to-graph sync, Teams for full bidirectional sync and conflict resolution. No new NuGet packages. Includes 18+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6e.md)

- **Doc-to-Graph Sync (v0.7.6f)** — Document-to-graph synchronization pipeline in `Lexichord.Abstractions.Contracts.Knowledge.Sync.DocToGraph` and `Lexichord.Modules.Knowledge.Sync.DocToGraph`. Added `IDocumentToGraphSyncProvider` interface with methods for document-to-graph sync (`SyncAsync()`), extraction validation (`ValidateExtractionAsync()`), lineage retrieval (`GetExtractionLineageAsync()`), and rollback (`RollbackSyncAsync()`). Added `IExtractionTransformer` interface for data transformation from extraction results to graph format (`TransformAsync()`, `TransformEntitiesAsync()`, `DeriveRelationshipsAsync()`, `EnrichEntitiesAsync()`). Added `IExtractionValidator` interface for schema compliance validation (`ValidateAsync()`, `ValidateEntityAsync()`, `ValidateRelationshipsAsync()`). Added `ValidationSeverity` enum (Warning, Error, Critical). Added 9 records: `DocToGraphSyncOptions` (ValidateBeforeUpsert, AutoCorrectErrors, PreserveLineage, MaxEntities, CreateRelationships, ExtractClaims, EnrichWithGraphContext, Timeout), `DocToGraphSyncResult` (Status, UpsertedEntities, CreatedRelationships, ExtractedClaims, ValidationErrors, ExtractionRecord, Duration, TotalEntitiesAffected, Message), `ExtractionRecord` (ExtractionId, DocumentId, DocumentHash, ExtractedAt, ExtractedBy, EntityIds, ClaimIds, RelationshipIds, ExtractionHash), `GraphIngestionData` (Entities, Relationships, Claims, SourceDocumentId, Metadata), `ValidationResult` (IsValid, Errors, Warnings, EntitiesValidated, RelationshipsValidated), `ValidationError` (Code, Message, EntityId, RelationshipId, Severity), `ValidationWarning` (Code, Message, EntityId), `EntityValidationResult` (EntityId, IsValid, Errors), `RelationshipValidationResult` (AllValid, InvalidRelationships), `DocToGraphValidationContext` (DocumentId, StrictMode, AllowedEntityTypes). Added `DocumentToGraphSyncProvider` implementation with entity extraction via `IEntityExtractionPipeline`, transformation via `IExtractionTransformer`, validation via `IExtractionValidator`, graph upsert via `IGraphRepository`, lineage tracking via `ExtractionLineageStore`, license gating (WriterPro for basic sync, Teams for enrichment), MediatR event publishing, 3-catch error handling. Added `ExtractionTransformer` with entity type normalization (api→Endpoint, function→Method, class→Component), relationship derivation from co-occurrence, and entity enrichment via graph context lookup. Added `ExtractionValidator` with 9 validation codes (VAL-001 through VAL-009) for entity type/value validation, schema compliance, low confidence detection, and relationship reference validation. Added `ExtractionLineageStore` with thread-safe `ConcurrentDictionary` storage for extraction history and rollback capability. Added `DocToGraphSyncCompletedEvent` MediatR notification with `DocumentId`, `Result`, `Timestamp`, `InitiatedBy` properties and `Create()` factory. Added `FeatureCodes.DocToGraphSync = "Feature.DocToGraphSync"` constant. Registered all services as singletons in `KnowledgeModule`. License gating: WriterPro for basic sync, Teams for full enrichment. No new NuGet packages. Includes 76 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6f.md)

- **Graph-to-Doc Sync (v0.7.6g)** — Graph-to-document synchronization module in `Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc` and `Lexichord.Modules.Knowledge.Sync.GraphToDoc`. Added `IGraphToDocumentSyncProvider` interface with methods for handling graph changes (`OnGraphChangeAsync()`), retrieving affected documents (`GetAffectedDocumentsAsync()`), managing flags (`GetPendingFlagsAsync()`, `ResolveFlagAsync()`), and subscribing to changes (`SubscribeToGraphChangesAsync()`). Added `IAffectedDocumentDetector` interface for document detection based on graph changes (`DetectAsync()`, `DetectBatchAsync()`, `GetRelationshipAsync()`). Added `IDocumentFlagger` interface for flag management (`FlagDocumentAsync()`, `FlagDocumentsAsync()`, `ResolveFlagAsync()`, `ResolveFlagsAsync()`, `GetFlagAsync()`, `GetPendingFlagsAsync()`). Added 6 enums: `DocumentEntityRelationship` (ExplicitReference, ImplicitReference, DerivedFrom, IndirectReference), `FlagReason` (EntityValueChanged, EntityPropertiesUpdated, EntityDeleted, NewRelationship, RelationshipRemoved, ManualSyncRequested, ConflictDetected), `FlagPriority` (Low, Medium, High, Critical), `FlagStatus` (Pending, Acknowledged, Resolved, Dismissed, Escalated), `FlagResolution` (UpdatedWithGraphChanges, RejectedGraphChanges, ManualMerge, Dismissed), `ActionType` (UpdateReferences, AddInformation, RemoveInformation, ManualReview). Added 7 records: `GraphToDocSyncResult` (Status, AffectedDocuments, FlagsCreated, TotalDocumentsNotified, TriggeringChange, Duration, ErrorMessage), `GraphToDocSyncOptions` (AutoFlagDocuments, SendNotifications, ReasonPriorities, BatchSize, MaxDocumentsPerChange, MinActionConfidence, IncludeSuggestedActions, DeduplicateNotifications, DeduplicationWindow, Timeout), `AffectedDocument` (DocumentId, DocumentName, Relationship, ReferenceCount, SuggestedAction, LastModifiedAt, LastSyncedAt), `DocumentFlag` (FlagId, DocumentId, TriggeringEntityId, Reason, Description, Priority, Status, CreatedAt, ResolvedAt, ResolvedBy, Resolution, NotificationSent, NotificationSentAt), `DocumentFlagOptions` (Priority, TriggeringEntityId, CreatedBy, IncludeSuggestedActions, Tags, SendNotification, Context), `SuggestedAction` (ActionType, Description, SuggestedText, Confidence), `GraphChangeSubscription` (DocumentId, EntityIds, ChangeTypes, NotifyUser, CreatedAt, IsActive). Added `GraphToDocumentSyncProvider` implementation with affected document detection via `IAffectedDocumentDetector`, flag creation via `IDocumentFlagger`, license gating (Teams+ required), MediatR event publishing, 3-catch error handling. Added `AffectedDocumentDetector` with entity lookup via `IGraphRepository`, document retrieval via `IDocumentRepository`, source document detection, batch deduplication. Added `DocumentFlagger` with flag creation, priority assignment, resolution tracking, MediatR notification publishing. Added `DocumentFlagStore` with thread-safe `ConcurrentDictionary` storage, pending flag filtering, priority-based ordering. Added `GraphToDocSyncCompletedEvent` and `DocumentFlaggedEvent` MediatR notifications with `Create()` factories. Added `FeatureCodes.GraphToDocSync = "Feature.GraphToDocSync"` constant. Registered all services as singletons in `KnowledgeModule`. License gating: Teams+ for full graph-to-doc sync functionality. No new NuGet packages. Includes 75 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6g.md)

- **Conflict Resolver (v0.7.6h)** — Enhanced conflict resolution module in `Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict` and `Lexichord.Modules.Knowledge.Sync.Conflict`. Added `IConflictDetector` interface for enhanced conflict detection with value and structural analysis (`DetectAsync()`, `DetectValueConflictsAsync()`, `DetectStructuralConflictsAsync()`, `EntitiesChangedAsync()`). Added `IConflictMerger` interface for intelligent value merging (`MergeAsync()`, `GetMergeStrategy()`). Added `IEntityComparer` interface for property-by-property entity comparison (`CompareAsync()`). Added 2 enums: `MergeStrategy` (DocumentFirst, GraphFirst, Combine, MostRecent, HighestConfidence, RequiresManualMerge), `MergeType` (Selection, Intelligent, Weighted, Manual, Temporal). Added 9 records: `PropertyDifference` (PropertyName, DocumentValue, GraphValue, Confidence), `EntityComparison` (DocumentEntity, GraphEntity, PropertyDifferences, computed HasDifferences/DifferenceCount), `ConflictDetail` (ConflictId, Entity, ConflictField, DocumentValue, GraphValue, Type, Severity, DetectedAt, timestamps, SuggestedStrategy, ResolutionConfidence), `MergeContext` (Entity, Document, ConflictType, UserId, ContextData), `MergeResult` (Success, MergedValue, UsedStrategy, Confidence, ErrorMessage, MergeType; factories DocumentWins/GraphWins/RequiresManual/Failed), `ConflictMergeResult` (extends MergeResult with DocumentValue, GraphValue, Explanation; FromMergeResult factory), `ConflictResolutionResult` (Conflict, Strategy, Succeeded, ResolvedValue, ErrorMessage, ResolvedAt, ResolvedBy, IsAutomatic; factories Success/Failure/RequiresManualIntervention), `ConflictResolutionOptions` (DefaultStrategy, StrategyByType, AutoResolveLow/Medium/High, MinMergeConfidence, PreserveConflictHistory, MaxResolutionAttempts, ResolutionTimeout; methods CanAutoResolve/GetStrategy; static Default). Added `EntityComparer` implementation with standard property comparison, custom property comparison, confidence scoring, and tolerance for small differences. Added `ConflictStore` with thread-safe ConcurrentDictionary storage, document-based grouping, resolution history tracking. Added `ConflictDetector` with value conflict detection via IEntityComparer, structural conflict detection, entity change tracking, severity assignment. Added `ConflictMerger` with 6 merge strategies: DocumentFirst, GraphFirst, Combine (string/list merging), MostRecent (timestamp-based), HighestConfidence (confidence-based), RequiresManualMerge. Added `EnhancedConflictResolver` with license gating (Core=none, WriterPro=Low+Manual, Teams=Low/Medium+Merge, Enterprise=all), detailed ConflictResolutionResult, ConflictStore integration, MediatR event publishing, ResolveAllAsync with options. Added `ConflictResolvedEvent` MediatR notification with `Create()` factory. Added `FeatureCodes.ConflictResolver = "Feature.ConflictResolver"` constant. Registered all services as singletons in `KnowledgeModule`. License gating: tiered access from WriterPro through Enterprise. No new NuGet packages. Includes 80+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6h.md)

- **Sync Status Tracker (v0.7.6i)** — Enhanced sync status tracking module in `Lexichord.Abstractions.Contracts.Knowledge.Sync.Status` and `Lexichord.Modules.Knowledge.Sync.Status`. Added `ISyncStatusRepository` interface for sync status persistence with CRUD operations (`GetAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`), query support (`QueryAsync()`, `CountByStateAsync()`), history tracking (`AddHistoryAsync()`, `GetHistoryAsync()`), and operation recording (`AddOperationRecordAsync()`, `GetOperationRecordsAsync()`). Enhanced `ISyncStatusTracker` interface with batch operations (`UpdateStatusBatchAsync()`, `GetStatusesAsync()`), state-based filtering (`GetDocumentsByStateAsync()`), history retrieval (`GetStatusHistoryAsync()`), metrics computation (`GetMetricsAsync()`), and operation recording (`RecordSyncOperationAsync()`). Added `SortOrder` enum with 7 values: `ByDocumentNameAscending`, `ByDocumentNameDescending`, `ByLastSyncAscending`, `ByLastSyncDescending`, `ByStateAscending`, `ByStateDescending`, `ByPendingChangesDescending`. Added 4 records: `SyncStatusQuery` (State, WorkspaceId, LastSyncBefore/After, MinPendingChanges, MinUnresolvedConflicts, SyncInProgress, SortOrder, PageSize, PageOffset), `SyncStatusHistory` (HistoryId, DocumentId, PreviousState, NewState, ChangedAt, ChangedBy, Reason, SyncOperationId, Metadata), `SyncOperationRecord` (OperationId, DocumentId, Direction, Status, StartedAt, CompletedAt, Duration, InitiatedBy, affected counts, conflict stats, ErrorMessage, ErrorCode, Metadata), `SyncMetrics` (DocumentId, TotalOperations, SuccessfulOperations, FailedOperations, AverageDuration, LongestDuration, ShortestDuration, LastSuccessfulSync, LastFailedSync, conflict counts, SuccessRate, averages, CurrentState, TimeInCurrentState). Added `SyncStatusUpdatedEvent` MediatR notification with `Create()` factory for state transitions. Added `SyncStatusRepository` in-memory implementation with `ConcurrentDictionary` for thread-safe storage, query filtering by all criteria, sorting by all sort orders, pagination support, and lock-based list modifications. Added enhanced `SyncStatusTracker` implementation with repository persistence, in-memory caching, automatic history recording on state changes, MediatR event publishing, metrics computation from operation records, and license-tier-based history retention limits (WriterPro=100, Teams=300, Enterprise=unlimited). Added `FeatureCodes.SyncStatusTracker = "Feature.SyncStatusTracker"` constant. Registered all services as singletons in `KnowledgeModule`. License gating: Core (no access), WriterPro (basic operations, 30-day history), Teams (batch operations, 90-day history, full metrics), Enterprise (unlimited history, advanced metrics). No new NuGet packages. Includes 55+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6i.md)

- **Sync Event Publisher (v0.7.6j)** — Centralized event publishing infrastructure for the Knowledge Synchronization system in `Lexichord.Abstractions.Contracts.Knowledge.Sync.Events` and `Lexichord.Modules.Knowledge.Sync.Events`. Added `ISyncEvent` base interface extending `INotification` for type-safe event handling with `EventId`, `PublishedAt`, `DocumentId`, `Metadata` properties. Added `EventPriority` enum with 4 levels (Low, Normal, High, Critical) for publication ordering. Added `EventSortOrder` enum with 5 values (ByPublishedAscending, ByPublishedDescending, ByDocumentAscending, ByDocumentDescending, ByEventTypeAscending) for query ordering. Added 4 records: `SyncEventRecord` (audit trail), `SyncEventOptions` (publication configuration), `SyncEventSubscriptionOptions` (dynamic subscriptions), `SyncEventQuery` (history filtering and pagination). Added 6 new sync event types: `SyncConflictDetectedEvent` (conflict detection with suggested strategies), `SyncConflictResolvedEvent` (conflict resolution tracking), `SyncStatusChangedEvent` (state transition notifications), `SyncFailedEvent` (failure details with retry recommendations), `SyncRetryEvent` (retry attempt tracking), `GraphToDocumentSyncedEvent` (graph-to-document sync notifications). Modified existing events (`SyncCompletedEvent`, `DocumentFlaggedEvent`) to implement `ISyncEvent`. Added `IEventStore` interface for event persistence with `StoreAsync()`, `GetAsync()`, `QueryAsync()`, `GetByDocumentAsync()` methods. Added `ISyncEventPublisher` interface for unified event publishing with `PublishAsync()`, `PublishBatchAsync()`, `SubscribeAsync()`, `UnsubscribeAsync()`, `GetEventsAsync()` methods. Added `SyncEventStore` in-memory implementation with `ConcurrentDictionary` storage, document indexing, query filtering, and sorting. Added `SyncEventPublisher` implementation with MediatR integration, event history storage, subscription management, license gating, and tier-based retention limits. Added `FeatureCodes.SyncEventPublisher = "Feature.SyncEventPublisher"` constant. Registered all services as singletons in `KnowledgeModule`. License gating: Core (no access), WriterPro (publish + 7-day history), Teams (full access + 30-day history + subscriptions + batching), Enterprise (unlimited history). No new NuGet packages. Includes 62 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.6j.md)

#### Sub-Part Changelogs

| Version                               | Title                        | Status      |
| ------------------------------------- | ---------------------------- | ----------- |
| [v0.7.6a](v0.7.x/LCS-CL-v0.7.6a.md) | Summarization Modes          | ✅ Complete |
| [v0.7.6b](v0.7.x/LCS-CL-v0.7.6b.md) | Metadata Extraction          | ✅ Complete |
| [v0.7.6c](v0.7.x/LCS-CL-v0.7.6c.md) | Export Formats               | ✅ Complete |
| [v0.7.6d](v0.7.x/LCS-CL-v0.7.6d.md) | Document Comparison          | ✅ Complete |
| [v0.7.6e](v0.7.x/LCS-CL-v0.7.6e.md) | Sync Service Core            | ✅ Complete |
| [v0.7.6f](v0.7.x/LCS-CL-v0.7.6f.md) | Doc-to-Graph Sync            | ✅ Complete |
| [v0.7.6g](v0.7.x/LCS-CL-v0.7.6g.md) | Graph-to-Doc Sync            | ✅ Complete |
| [v0.7.6h](v0.7.x/LCS-CL-v0.7.6h.md) | Conflict Resolver            | ✅ Complete |
| [v0.7.6i](v0.7.x/LCS-CL-v0.7.6i.md) | Sync Status Tracker          | ✅ Complete |
| [v0.7.6j](v0.7.x/LCS-CL-v0.7.6j.md) | Sync Event Publisher         | ✅ Complete |

---

## [v0.7.5] - 2026-02 (In Progress)

### The Tuning Agent (Proactive Style Harmony)

This release introduces the Tuning Agent system, enabling proactive style scanning that bridges the linting infrastructure with AI-powered fix generation.

#### What's New

- **Style Deviation Scanner (v0.7.5a)** — Foundation for AI-powered style fix generation in `Lexichord.Abstractions.Contracts.Agents` and `Lexichord.Modules.Agents.Tuning`. Added `IStyleDeviationScanner` interface with methods for document scanning (`ScanDocumentAsync()`), range scanning (`ScanRangeAsync()`), cache management (`GetCachedResultAsync()`, `InvalidateCache()`, `InvalidateAllCaches()`), and real-time event notification via `DeviationsDetected` event. Added `StyleDeviation` record with `DeviationId` (Guid), `Violation` (StyleViolation from v0.2.1b), `Location` (TextSpan), `OriginalText`, `SurroundingContext` (for AI context window), `ViolatedRule` (nullable StyleRule), `IsAutoFixable` (bool), `Priority` (DeviationPriority); computed properties `Category`, `RuleId`, `Message`, `LinterSuggestedFix`. Added `DeviationScanResult` record with `DocumentPath`, `Deviations`, `ScannedAt`, `ScanDuration`, `IsCached`, `ContentHash`, `RulesVersion`; computed properties `TotalCount`, `AutoFixableCount`, `ManualOnlyCount`; grouping helpers `ByCategory`, `ByPriority`, `ByPosition`; factory methods `Empty()`, `LicenseRequired()`. Added `DeviationPriority` enum mapping lint severity to priority (Low=Hint, Normal=Information, High=Warning, Critical=Error). Added `DeviationsDetectedEventArgs` class with `DocumentPath`, `NewDeviations`, `TotalDeviationCount`, `IsIncremental`; computed properties `NewDeviationCount`, `HasNewDeviations`. Added `ScannerOptions` configuration class with `ContextWindowSize=500`, `CacheTtlMinutes=5`, `MaxDeviationsPerScan=100`, `IncludeManualOnly=true`, `MinimumSeverity=Hint`, `EnableRealTimeUpdates=true`, `ExcludedCategories=[]`. Added `StyleDeviationScanner` implementation as Singleton with `ILintingOrchestrator`, `IEditorService`, `IMemoryCache`, `ILicenseContext`, `IOptions<ScannerOptions>`, `ILogger` dependencies; implements `INotificationHandler<LintingCompletedEvent>` and `INotificationHandler<StyleSheetReloadedEvent>` for real-time updates and cache invalidation; content-hash-based cache key generation; auto-fixability determination based on rule properties (IsAiFixable, RuleType, Category, Complexity); semaphore-based thread safety; context extraction with sentence boundary adjustment. Added `AddStyleDeviationScanner()` DI extension in `TuningServiceCollectionExtensions`. Updated `AgentsModule` to v0.7.5 with registration and init verification. License gating: requires WriterPro tier, returns `LicenseRequired()` empty result for lower tiers. No new NuGet packages. Includes 33 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5a.md)

- **Automatic Fix Suggestions (v0.7.5b)** — AI-powered fix generation for style deviations in `Lexichord.Abstractions.Contracts.Agents` and `Lexichord.Modules.Agents.Tuning`. Added `IFixSuggestionGenerator` interface with methods for single fix generation (`GenerateFixAsync()`), batch processing (`GenerateFixesAsync()` with parallel execution), user-guided regeneration (`RegenerateFixAsync()`), and fix validation (`ValidateFixAsync()`). Added `FixSuggestion` record with `SuggestionId`, `DeviationId`, `OriginalText`, `SuggestedText`, `Explanation`, `Confidence` (0.0-1.0), `QualityScore` (0.0-1.0), `Diff` (TextDiff), `TokenUsage`, `GenerationTime`, `Alternatives` (list of AlternativeSuggestion), `IsValidated`, `ValidationResult`; computed property `IsHighConfidence` (confidence ≥0.8, quality ≥0.7, validated); factory methods `Failed()`, `LicenseRequired()`. Added `AlternativeSuggestion` record with `SuggestedText`, `Explanation`, `Confidence`. Added `FixGenerationOptions` record with `MaxAlternatives=2`, `IncludeExplanations=true`, `MinConfidence=0.7`, `ValidateFixes=true`, `Tone` (TonePreference), `PreserveVoice=true`, `MaxParallelism=5`, `UseLearningContext=true`. Added `FixValidationResult` record with `ResolvesViolation`, `IntroducesNewViolations`, `NewViolations`, `SemanticSimilarity`, `Status` (ValidationStatus), `Message`; factory methods `Valid()`, `Invalid()`, `Failed()`. Added `TextDiff` record with `Operations` (IReadOnlyList<DiffOperation>), `UnifiedDiff`, `HtmlDiff`; computed properties `Additions`, `Deletions`, `Unchanged`, `TotalChanges`, `HasChanges`; static `Empty` factory. Added `DiffOperation` record with `Type` (DiffType), `Text`, `StartIndex`, `Length`. Added enums `TonePreference` (Neutral, Formal, Casual, Technical, Simplified), `ValidationStatus` (Valid, ValidWithWarnings, Invalid, ValidationFailed), `DiffType` (Unchanged, Addition, Deletion). Added `FixSuggestionGenerator` implementation with `IChatCompletionService`, `IPromptRenderer`, `IPromptTemplateRepository`, `IStyleEngine`, `ILicenseContext`, `DiffGenerator`, `FixValidator`, `ILogger` dependencies; prompt template rendering with rule context; JSON response parsing; semaphore-based parallel generation; optional fix validation via re-linting. Added `DiffGenerator` helper using DiffPlex 1.7.2 `InlineDiffBuilder` for word-level diff generation with unified and HTML output formats. Added `FixValidator` helper using `IStyleEngine.AnalyzeAsync()` for re-linting, semantic similarity via Jaccard word overlap with length ratio weighting (70% Jaccard + 30% length ratio). Added `tuning-agent-fix.yaml` Mustache prompt template with rule context, violation details, learning context integration, tone preferences, and structured JSON output format. Added `AddFixSuggestionGenerator()` DI extension in `TuningServiceCollectionExtensions`. Updated `AgentsModule` with registration and init verification. License gating: requires WriterPro tier, returns `LicenseRequired()` for lower tiers. Uses DiffPlex 1.7.2 (already in project from v0.7.4c). Includes 40 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5b.md)

- **Accept/Reject UI (v0.7.5c)** — Interactive review panel for AI-generated fix suggestions in `Lexichord.Modules.Agents.Tuning` and `Lexichord.Abstractions.Contracts.Agents`. Added `TuningPanelViewModel` extending `DisposableViewModel` as the panel orchestrator with `IStyleDeviationScanner`, `IFixSuggestionGenerator`, `IEditorService`, `IUndoRedoService?` (nullable), `ILicenseContext`, `IMediator`, `ILogger` dependencies; commands for `ScanDocumentAsync()` (full scan-generate-display pipeline with license gating via `FeatureCodes.TuningAgent`), `AcceptSuggestionAsync()` (applies fix via sync `BeginUndoGroup`/`DeleteText`/`InsertText`/`EndUndoGroup` editor APIs with optional `IUndoRedoService.Push()` for labeled undo history), `RejectSuggestionAsync()` (status change without document modification), `SkipSuggestion()` (deferral without feedback), `AcceptAllHighConfidenceAsync()` (bulk accept in reverse document order within single editor undo group to preserve text offsets), `NavigateNext()`/`NavigatePrevious()` (keyboard navigation with pending-skip and wrap-around); public methods `ModifySuggestionAsync()` (applies user-modified text with `SuggestionStatus.Modified` status) and `RegenerateSuggestionAsync()` (regenerates via `IFixSuggestionGenerator.RegenerateFixAsync()` with optional user guidance); observable properties `Suggestions` (ObservableCollection), `SelectedSuggestion`, `IsScanning`, `IsGeneratingFixes`, `IsBulkProcessing`, `TotalDeviations`, `ReviewedCount`, `AcceptedCount`, `RejectedCount`, `CurrentFilter` (SuggestionFilter), `StatusMessage`, `ProgressPercent`, `HasWriterProLicense`, `HasTeamsLicense`; computed properties `HighConfidenceCount`, `RemainingCount`, `FilteredSuggestions` (filter-aware view supporting All/Pending/HighConfidence/HighPriority); `CloseRequested` event; `InitializeAsync()` for license checks. Added `SuggestionCardViewModel` extending `ObservableObject` as per-suggestion UI wrapper with observable `IsExpanded`, `IsReviewed`, `Status` (SuggestionStatus), `ModifiedText`, `ShowAlternatives`, `SelectedAlternative`, `IsRegenerating`; computed display properties `RuleName`, `RuleCategory`, `Priority`, `Confidence`, `QualityScore`, `IsHighConfidence`, `Diff`, `OriginalText`, `SuggestedText`, `Explanation`, `Alternatives`, `HasAlternatives`, `ConfidenceDisplay`, `QualityDisplay`, `PriorityDisplay`; `UpdateSuggestion()` method with property change notifications; `ToggleAlternativesCommand`. Added `TuningUndoableOperation` implementing `IUndoableOperation` with `ExecuteAsync()`/`UndoAsync()`/`RedoAsync()` using sync editor `BeginUndoGroup`/`DeleteText`/`InsertText`/`EndUndoGroup` pattern; `DisplayName` format "Tuning Fix ({RuleId})". Added `SuggestionStatus` enum (Pending=0, Accepted=1, Rejected=2, Modified=3, Skipped=4) and `SuggestionFilter` enum (All=0, Pending=1, HighConfidence=2, HighPriority=3). Added MediatR events `SuggestionAcceptedEvent` with `Deviation`, `Suggestion`, `ModifiedText`, `IsModified`, `Timestamp`, computed `AppliedText`, factory methods `Create()` and `CreateModified()`; `SuggestionRejectedEvent` with `Deviation`, `Suggestion`, `Timestamp`, factory method `Create()`. Added `FeatureCodes.TuningAgent = "Feature.TuningAgent"` constant. Added `AddTuningReviewUI()` DI extension registering `TuningPanelViewModel` as Transient. Updated `AgentsModule` with registration and init verification. Spec adaptations: sync `IEditorService` APIs (spec references async variants that don't exist), nullable `IUndoRedoService`, omitted `ILearningLoopService` (v0.7.5d), `DisposableViewModel`/`ObservableObject` base classes (no ViewModelBase), `ShowUpgradeModalEvent` (no ShowUpgradePromptEvent), `Lexichord.Modules.Agents.Tuning` namespace (not Host.ViewModels.Agents), public methods for multi-parameter operations (CommunityToolkit.Mvvm `[RelayCommand]` supports max 1 parameter). No new NuGet packages. Includes 76 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5c.md)

- **Learning Loop (v0.7.5d)** — Feedback persistence, pattern analysis, and prompt enhancement for the Tuning Agent in `Lexichord.Modules.Agents.Tuning` and `Lexichord.Abstractions.Contracts.Agents`. Added `ILearningLoopService` interface with methods for feedback recording (`RecordFeedbackAsync()`), learning context retrieval (`GetLearningContextAsync()` returning `LearningContext` with `AcceptedPatterns`, `RejectedPatterns`, `UserModifications`, and `PromptEnhancement`), statistics aggregation (`GetStatisticsAsync()` returning `LearningStatistics` with per-rule and per-category breakdowns), data export/import (`ExportLearningDataAsync()`, `ImportLearningDataAsync()` for team sharing), data clearing (`ClearLearningDataAsync()` with selective options), and privacy controls (`GetPrivacyOptions()`, `SetPrivacyOptionsAsync()`). Added `LearningLoopService` as internal singleton implementing `ILearningLoopService`, `INotificationHandler<SuggestionAcceptedEvent>`, and `INotificationHandler<SuggestionRejectedEvent>` with `IFeedbackStore`, `PatternAnalyzer`, `ISettingsService`, `ILicenseContext`, `ILogger` dependencies; MediatR handlers convert accepted/rejected/modified events to `FixFeedback` records and silently skip when Teams license not available; public API methods throw `InvalidOperationException` for non-Teams users; SHA256 content anonymization when privacy enabled; retention policy enforcement after each recording. Added `PatternAnalyzer` internal class extracting accepted patterns (success rate ≥ 0.7, count ≥ 3, top 5), rejected patterns (success rate ≤ 0.3, count ≥ 3, top 5), and user modifications (top 3 most recent); generates structured prompt enhancement text requiring 10+ total samples with preferred/avoided/modification sections and confidence calibration. Added `SqliteFeedbackStore` internal class implementing `IFeedbackStore` with `Microsoft.Data.Sqlite` v9.0.0 raw ADO.NET (following `SqliteEmbeddingCache` pattern); `feedback` table (15 columns, 3 indexes) and `pattern_cache` table (composite PK); parameterized queries; pooled connections; `IDisposable` with `SqliteConnection.ClearAllPools()`. Added 16 public contract types: `FeedbackDecision` enum (Accepted, Rejected, Modified), `FixFeedback`, `LearningContext`, `AcceptedPattern`, `RejectedPattern`, `UserModificationExample`, `LearningStatistics`, `RuleLearningStats`, `CategoryLearningStats`, `LearningStatisticsFilter`, `LearningSummaryExportOptions`, `LearningExport`, `ExportedPattern`, `ClearLearningDataOptions`, `LearningPrivacyOptions`. Added `LearningStorageOptions` configuration with `DatabasePath`, `RetentionDays` (365), `MaxPatternCacheSize` (100), `PatternMinFrequency` (2). Added `FeatureCodes.LearningLoop = "Feature.LearningLoop"` constant. Added `AddLearningLoop()` DI extension with singleton MediatR handler forwarding to same `LearningLoopService` instance. Updated `AgentsModule` with DI registration and `IFeedbackStore.InitializeAsync()` in init verification. Integrated into `TuningPanelViewModel` (nullable `ILearningLoopService?` parameter) and `FixSuggestionGenerator` (learning context injection in `BuildPromptContext()` with try/catch resilience, `learning_enhancement` template key). Spec adaptations: `Microsoft.Data.Sqlite` v9.0.0 (not sqlite-net-pcl), reused existing `DateRange(DateTime?, DateTime?)` (not DateTimeOffset), `ISettingsService` (not IPrivacySettingsService), all storage/analyzer types internal (accessible via InternalsVisibleTo). Requires Teams license tier. Uses `Microsoft.Data.Sqlite` 9.0.0 (**new** NuGet package). Includes 94 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5d.md)

- **Unified Issue Model (v0.7.5e)** — Normalized validation finding representation for the Unified Validation feature in `Lexichord.Abstractions.Contracts.Validation`. Added `IssueCategory` enum (Style=0, Grammar=1, Knowledge=2, Structure=3, Custom=4) for unified categorization across Style Linter, Grammar Linter, and CKVS Validation Engine sources. Added `FixType` enum (Replacement=0, Insertion=1, Deletion=2, Rewrite=3, NoFix=4) for fix operation types. Added `UnifiedFix` record with `FixId` (Guid), `Location` (TextSpan from v0.6.7b), `OldText?`, `NewText?`, `Type` (FixType), `Description`, `Confidence` (0.0-1.0), `CanAutoApply` (bool); computed `IsHighConfidence` (≥0.8), `HasChanges`; factory methods `Replacement()`, `Insertion()`, `Deletion()`, `Rewrite()`, `NoFixAvailable()`. Added `UnifiedIssue` record with `IssueId` (Guid), `SourceId` (rule ID/code), `Category` (IssueCategory), `Severity` (UnifiedSeverity from v0.6.5j), `Message`, `Location` (TextSpan), `OriginalText?`, `Fixes` (IReadOnlyList<UnifiedFix>), `SourceType` ("StyleLinter"/"GrammarLinter"/"Validation"), `OriginalSource?`; computed `HasFixes`, `BestFix`, `HasHighConfidenceFix`, `CanAutoFix`, `SeverityOrder`, `IsFromStyleLinter`/`IsFromGrammarLinter`/`IsFromValidation`; methods `WithFixes()`, `WithAdditionalFix()`; static `Empty` factory. Added `SeverityMapper` static class mapping `ViolationSeverity` (direct 1:1), `ValidationSeverity` (inverted: Info=0→Info(2), Error=2→Error(0)), and `DeviationPriority` (reverse: Critical→Error, Low→Hint) to/from `UnifiedSeverity`. Added `UnifiedIssueFactory` static class with `FromStyleDeviation()`, `FromStyleViolation()`, `FromUnifiedFinding()`, `FromValidationFinding()` single-item factories; `FromDeviations()`, `FromViolations()`, `FromFindings()` batch factories; internal helpers for category mapping (`RuleCategory`→`IssueCategory`, `FindingCategory`→`IssueCategory`) and fix creation. Coexists with v0.6.5j `UnifiedFinding`, `UnifiedFix` in `Knowledge.Validation.Integration` namespace for backward compatibility. Spec adaptations: reused `Editor.TextSpan` (not new `Text.TextSpan` with LineNumber/ColumnNumber), reused `UnifiedSeverity` from v0.6.5j, adapted type names (`StyleViolation`/`ViolationSeverity` not `LintViolation`/`LintSeverity`, `ValidationFinding` not `ValidationIssue`). No new NuGet packages. No DI registration (pure data contracts). Includes 107 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5e.md)

- **Issue Aggregator (v0.7.5f)** — Unified validation service that aggregates results from multiple validators in `Lexichord.Abstractions.Contracts.Validation` and `Lexichord.Modules.Agents.Tuning`. Added `IUnifiedValidationService` interface with methods for full document validation (`ValidateAsync()`), range-specific validation (`ValidateRangeAsync()`), cache management (`GetCachedResultAsync()`, `InvalidateCache()`, `InvalidateAllCaches()`), and `ValidationCompleted` event for UI notification. Added `UnifiedValidationOptions` record with validator selection (`IncludeStyleLinter`, `IncludeGrammarLinter`, `IncludeValidationEngine`), filtering (`MinimumSeverity`, `FilterByCategory`), caching (`EnableCaching`, `CacheTtlMs=300000`), execution (`ParallelValidation=true`, `ValidatorTimeoutMs=30000`), and processing options (`EnableDeduplication=true`, `MaxIssuesPerDocument=1000`, `IncludeFixes=true`); helper methods `PassesSeverityFilter()`, `PassesCategoryFilter()`; computed properties `CacheTtl`, `ValidatorTimeout`. Added `UnifiedValidationResult` record with `DocumentPath`, `Issues` (IReadOnlyList<UnifiedIssue>), `Duration`, `ValidatedAt`, `IsCached`, `Options`, `ValidatorDetails`; computed groupings `ByCategory`, `BySeverity`, `CountBySeverity`, `CountBySourceType`; computed metrics `TotalIssueCount`, `ErrorCount`, `WarningCount`, `InfoCount`, `HintCount`, `AutoFixableCount`, `CanPublish`, `IsEmpty`, `HasIssues`; factory methods `Empty()`, `AsCached()`. Added `ValidationCompletedEventArgs` class with `DocumentPath`, `Result`, `SuccessfulValidators`, `FailedValidators`, `CompletedAt`; computed `AllValidatorsSucceeded`, `HasFailures`; factory methods `Success()`, `WithFailures()`. Added `UnifiedValidationService` implementation with `IStyleDeviationScanner`, `IValidationEngine`, `ILicenseContext`, `IMemoryCache`, `ILogger` dependencies; license gating (Core=Style, WriterPro=Style+Grammar, Teams+=All); parallel execution with per-validator timeout; result caching with TTL and options validation; deduplication across validators (±1 char tolerance); severity/category filtering; issue limit truncation; graceful error handling (validator failures don't block others); thread-safe via `SemaphoreSlim`. Added `AddUnifiedValidationService()` DI extension registering singleton. Updated `AgentsModule` with registration and init verification. Spec adaptations: used `(string documentPath, string content)` pair (no `Document` class), `IStyleDeviationScanner` (not `IStyleLinter`), nullable grammar linter placeholder, `ValidateDocumentAsync(ValidationContext)` (not `ValidateAsync(Document)`), sync `GetCurrentTier()` (not async). No new NuGet packages. Includes 80+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5f.md)

- **Unified Issues Panel (v0.7.5g)** — UI panel displaying all validation issues from `IUnifiedValidationService` (v0.7.5f) in `Lexichord.Modules.Agents.Tuning` and `Lexichord.Abstractions.Contracts.Validation.Events`. Added `FixRequestedEventArgs` class with `Issue` (UnifiedIssue), `Fix` (UnifiedFix), `Timestamp`; factory method `CreateForBestFix()`. Added `IssueDismissedEventArgs` class with `Issue`, `Reason?`, `Timestamp`; factory methods `Create()`, `CreateWithReason()`. Added `IssuePresentationGroup` class with observable `IsExpanded`; immutable `Severity` (UnifiedSeverity), `Label`, `Icon`, `Items` (ObservableCollection<IssuePresentation>); computed `Count`, `HasItems`, `AutoFixableCount`, `ActiveCount`; factory methods `Errors()`, `Warnings()`, `Infos()`, `Hints()`. Added `IssuePresentation` class wrapping `UnifiedIssue` with observable `IsExpanded`, `IsSuppressed`, `IsFixed`, `IsSelected`; computed display properties `CategoryLabel`, `SeverityLabel`, `LocationDisplay`, `SourceDisplay`, `Message`, `HasFix`, `CanAutoApply`, `ConfidenceDisplay`, `FixTypeLabel`, `IsActionable`, `DisplayOpacity`; commands `ToggleExpandedCommand`, `ToggleSuppressedCommand`; method `MarkAsFixed()`. Added `UnifiedIssuesPanelViewModel` extending `DisposableViewModel` with `IUnifiedValidationService`, `IEditorService`, `IUndoRedoService?` (nullable), `ILicenseContext`, `IMediator`, `ILogger` dependencies; observable `IssueGroups`, `SelectedIssue`, `IsLoading`, `IsBulkProcessing`, `StatusMessage`, `ProgressPercent`, filters `SelectedCategoryFilter`, `SelectedSeverityFilter`; computed `TotalIssueCount`, `ErrorCount`, `WarningCount`, `AutoFixableCount`, `CanPublish`, `HasIssues`; commands `RefreshCommand`, `FixIssueCommand`, `FixAllCommand` (reverse document order with single undo group), `FixErrorsOnlyCommand`, `DismissIssueCommand`, `NavigateToIssueCommand`, `ClearCommand`, `CloseCommand`; events `CloseRequested`, `FixRequested`, `IssueDismissed`; methods `InitializeAsync()` (subscribe to ValidationCompleted), `RefreshWithResultAsync()`; internal `UnifiedIssuesUndoOperation` implementing `IUndoableOperation`. Added `UnifiedIssuesPanelView.axaml` + `.axaml.cs` Avalonia panel with header (title, counts, toolbar), filter bar (category/severity dropdowns), content (ScrollViewer with severity group Expanders containing issue items with expandable details), footer (status, duration); value converters `SeverityConverters` (ToIcon, IsError/Warning/Info/Hint), `ExpanderConverters` (ToChevron), `PluralConverter`. Added `AddUnifiedIssuesPanel()` DI extension registering `UnifiedIssuesPanelViewModel` as Transient. Updated `AgentsModule` with registration and init verification. Spec adaptations: Avalonia 11 (not WPF), `DisposableViewModel` (not ViewModelBase), CommunityToolkit.Mvvm (not Prism/MvvmLight), direct `IEditorService` calls (no `IFixOrchestrator` until v0.7.5h), `CaretOffset` + `ActivateDocumentAsync()` (no `GoToLineAsync()`), `TextSpan.Start/Length` character offsets (no line/column). No new NuGet packages. Includes 60+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5g.md)

- **Combined Fix Workflow (v0.7.5h)** — Orchestrated fix application with conflict detection and atomic undo in `Lexichord.Abstractions.Contracts.Validation` and `Lexichord.Modules.Agents.Tuning.FixOrchestration`. Added `ConflictHandlingStrategy` enum (ThrowException, SkipConflicting, PromptUser, PriorityBased), `FixConflictType` enum (OverlappingPositions, ContradictorySuggestions, DependentFixes, CreatesNewIssue, InvalidLocation), `FixConflictSeverity` enum (Info, Warning, Error). Added `FixWorkflowOptions` record with `DryRun`, `ConflictStrategy`, `ReValidateAfterFixes`, `MaxFixIterations`, `EnableUndo`, `Timeout`, `Verbose`; static `Default` property. Added `FixConflictCase` record with `Type`, `ConflictingIssueIds`, `Description`, `SuggestedResolution`, `Severity`. Added `FixApplyResult` record with `Success`, `AppliedCount`, `SkippedCount`, `FailedCount`, `ModifiedContent` (string?), `ResolvedIssues`, `RemainingIssues`, `ConflictingIssues`, `ErrorsByIssueId`, `DetectedConflicts`, `Duration`, `TransactionId`, `OperationTrace`; nested `FixOperationTrace` record; static `Empty()` factory. Added `FixTransaction` record with `Id` (Guid), `DocumentPath`, `DocumentBefore`, `DocumentAfter`, `FixedIssueIds`, `AppliedAt`, `IsUndone`. Added exceptions `FixConflictException` (with `Conflicts` property), `FixApplicationTimeoutException` (with `AppliedCount`), `DocumentCorruptionException`. Added `FixesAppliedEventArgs` with `DocumentPath`, `Result`, `Timestamp`; `FixConflictDetectedEventArgs` with `DocumentPath`, `Conflicts`, `Timestamp`. Added `IUnifiedFixWorkflow` interface with `FixAllAsync()`, `FixByCategoryAsync()`, `FixBySeverityAsync()`, `FixByIdAsync()`, `DetectConflicts()`, `DryRunAsync()`, `UndoLastFixesAsync()`; events `FixesApplied`, `ConflictDetected`. Added `FixPositionSorter` internal static class with `SortBottomToTop()` (filters auto-fixable, sorts `Start` descending then `End` descending to prevent offset drift) and `SortBottomToTopUnfiltered()`. Added `FixConflictDetector` internal class with `ILogger` dependency; `Detect()` for overlapping positions (via `TextSpan.OverlapsWith()`), contradictory suggestions (same location, different `NewText`), dependent fixes (Style+Grammar within 200 chars proximity); `ValidateLocations()` for bounds checking. Added `FixGrouper` internal static class with `GroupByCategory()` returning ordered groups (Knowledge=0→Structure=1→Grammar=2→Style=3→Custom=4) and `FlattenGroups()`; `CategoryGroup` record. Added `UnifiedFixOrchestrator` internal class implementing `IUnifiedFixWorkflow`, `IDisposable` with `IEditorService`, `IUnifiedValidationService`, `FixConflictDetector`, `IUndoRedoService?` (nullable), `ILicenseContext`, `ILogger` dependencies; `FixAllAsync` pipeline: validate args → check license (WriterPro+) → get document text → extract auto-fixable issues → validate locations → detect conflicts → handle per `ConflictStrategy` → sort bottom-to-top → group by category → apply atomically via `BeginUndoGroup`/`DeleteText`/`InsertText`/`EndUndoGroup` → record `FixTransaction` → push to `IUndoRedoService` → re-validate via `IUnifiedValidationService.ValidateAsync()` → raise `FixesApplied` event; `FixTransaction` undo stack (max 50); `SemaphoreSlim` thread safety; inner `FixWorkflowUndoOperation` implementing `IUndoableOperation`; `DryRun` mode applies to string copy without editor mutations. Integrated into `UnifiedIssuesPanelViewModel` with nullable `IUnifiedFixWorkflow?` parameter — `FixIssueCommand` delegates to `FixByIdAsync()`, `FixAllCommand` delegates to `FixAllAsync()`, `FixErrorsOnlyCommand` delegates to `FixBySeverityAsync()`, all with graceful fallback to direct editor calls when orchestrator unavailable. Added `AddUnifiedFixWorkflow()` DI extension registering `FixConflictDetector` and `IUnifiedFixWorkflow → UnifiedFixOrchestrator` as Singletons. Updated `AgentsModule` with registration and init verification. Spec adaptations: no `Document` class (uses `string documentPath` + `IEditorService.GetDocumentText()`), no `ReplaceContentAsync()` (uses sync undo group pattern), `BestFix`/`IssueId`/`SourceId` (not `Fix`/`Id`/`Code`), `ModifiedContent` string (not `ModifiedDocument`), simplified conflict detection (no per-fix re-validation), `TextSpan(Start, Length)` construction. No new NuGet packages. Includes 135 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5h.md)

- **Issue Filters (v0.7.5i)** — Composable in-memory filtering, multi-criteria sorting, text search, and preset management for unified validation issues in `Lexichord.Abstractions.Contracts.Validation` and `Lexichord.Modules.Agents.Tuning`. Added `FilterCriteria` abstract record with `Matches(UnifiedIssue)` method as composable predicate base; 5 subclasses: `CategoryFilterCriterion(IEnumerable<IssueCategory>)` matching by category, `SeverityFilterCriterion(UnifiedSeverity)` with corrected inverted-enum comparison (`(int)issue.Severity <= (int)MinimumSeverity`), `LocationFilterCriterion(TextSpan, bool IncludePartialOverlaps)` with `TextSpan.OverlapsWith()` or strict containment, `TextSearchFilterCriterion(string)` case-insensitive across Message/SourceId/SourceType, `CodeFilterCriterion(IEnumerable<string>)` with `*` wildcard-to-regex matching on SourceId. Added `IssueFilterOptions` record (renamed from `FilterOptions` to avoid collision with `Contracts.FilterOptions` from v0.2.5b) with 15 properties: `Categories`, `MinimumSeverity` (default Hint=include all), `MaximumSeverity` (default Error=include all), `IssueCodes`, `ExcludeCodes`, `SearchText`, `LocationSpan` (TextSpan?), `LineRange` ((int,int)?), `OnlyAutoFixable`, `OnlyManual`, `SortBy`, `SortAscending`, `ValidatorNames`, `Limit`, `Offset`; static `Default` property. Added `SortCriteria` enum (Severity, Location, Category, Message, Code, ValidatorName, AutoFixable) mapping to actual properties (`SourceId` for Code, `SourceType` for ValidatorName, `CanAutoFix` for AutoFixable). Added `SortOptions` record with `Primary` (required), `Secondary?`, `Tertiary?`, `Ascending` (default true). Added `IIssueFilterService` interface with 14 methods: `FilterAsync()` (AND-composed criteria with pagination and sorting), `SearchAsync()` (text search with optional additional filters), `FilterByCategoryAsync()`, `FilterBySeverityAsync()`, `FilterByLocationAsync()` (with partial overlap mode), `FilterByLineRangeAsync()` (approximate char-offset-to-line conversion), `SortAsync()` (multi-criteria OrderBy/ThenBy chain), `CountBySeverity()`, `CountByCategory()`, `SavePreset()`, `LoadPreset()`, `ListPresets()`, `DeletePreset()`. Added `IssueFilterService` internal sealed class as Singleton with `ILogger` dependency; `MatchesAllCriteria()` with AND-composition and early exit ordered by cost (enum/bool first, regex/string last); severity range check `MaximumSeverity <= issue.Severity <= MinimumSeverity` (corrected from spec's inverted comparison); `SortIssues()` using `OrderBy`/`ThenBy` chain (corrected from spec's `OrderBy` loop that overwrites); `GetSortKey()` mapping criteria to properties; `GetApproximateLineNumber()` using `Start / 80 + 1`; 6 default presets: "Errors Only" (Error only, sorted by location), "Warnings and Errors" (Error+Warning, sorted by severity then location), "Auto-Fixable Only" (CanAutoFix=true, sorted by category then location), "Style Issues" (Style category, sorted by severity then location), "Grammar Issues" (Grammar category, sorted by location), "Knowledge Issues" (Knowledge category, sorted by severity then location); Stopwatch-based performance logging. Added `AddIssueFilterService()` DI extension registering `IIssueFilterService` → `IssueFilterService` as Singleton. Updated `AgentsModule` with registration and init verification. Spec adaptations: `issue.Code` → `issue.SourceId`, `issue.ValidatorName` → `issue.SourceType`, `issue.Fix` → `issue.CanAutoFix`, non-nullable `TextSpan` Location, `IssueFilterOptions` naming, inverted severity comparison, `OrderBy`/`ThenBy` sort chain, `TextSpan` reference type pattern matching. No new NuGet packages. Includes 84 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.5i.md)

#### Sub-Part Changelogs

| Version                               | Title                        | Status      |
| ------------------------------------- | ---------------------------- | ----------- |
| [v0.7.5a](v0.7.x/LCS-CL-v0.7.5a.md) | Style Deviation Scanner      | ✅ Complete |
| [v0.7.5b](v0.7.x/LCS-CL-v0.7.5b.md) | Automatic Fix Suggestions    | ✅ Complete |
| [v0.7.5c](v0.7.x/LCS-CL-v0.7.5c.md) | Accept/Reject UI             | ✅ Complete |
| [v0.7.5d](v0.7.x/LCS-CL-v0.7.5d.md) | Learning Loop                | ✅ Complete |
| [v0.7.5e](v0.7.x/LCS-CL-v0.7.5e.md) | Unified Issue Model          | ✅ Complete |
| [v0.7.5f](v0.7.x/LCS-CL-v0.7.5f.md) | Issue Aggregator             | ✅ Complete |
| [v0.7.5g](v0.7.x/LCS-CL-v0.7.5g.md) | Unified Issues Panel         | ✅ Complete |
| [v0.7.5h](v0.7.x/LCS-CL-v0.7.5h.md) | Combined Fix Workflow        | ✅ Complete |
| [v0.7.5i](v0.7.x/LCS-CL-v0.7.5i.md) | Issue Filters                | ✅ Complete |

---

## [v0.7.4] - 2026-02 (Complete)

### The Simplifier Agent (Readability-Targeted Simplification)

This release introduces the Simplifier Agent system, enabling writers to simplify text to target specific readability levels based on audience presets or Voice Profile settings.

#### What's New

- **Readability Target Service (v0.7.4a)** — Foundation for readability-targeted text simplification in `Lexichord.Abstractions.Agents.Simplifier` and `Lexichord.Modules.Agents.Simplifier`. Added `IReadabilityTargetService` interface with methods for preset management (`GetAllPresetsAsync()`, `GetPresetByIdAsync()`), target resolution (`GetTargetAsync()` with priority: explicit params > preset > Voice Profile > defaults), target validation (`ValidateTarget()` with achievability levels: Achievable, Challenging, Unlikely, AlreadyMet), and custom preset CRUD (`CreateCustomPresetAsync()`, `UpdateCustomPresetAsync()`, `DeleteCustomPresetAsync()` requiring WriterPro/Teams). Added `AudiencePreset` record with `Id`, `Name`, `TargetGradeLevel`, `MaxSentenceLength`, `AvoidJargon`, `Description`, `IsBuiltIn` properties; `IsValid()` quick check; `Validate()` detailed errors; `CloneWithId()` for customization. Added `ReadabilityTarget` record with computed `MinAcceptableGrade`/`MaxAcceptableGrade` (target ± tolerance), `IsGradeLevelAcceptable()` check, `FromPreset()` and `FromExplicit()` factory methods, and `ReadabilityTargetSource` enum (VoiceProfile, Preset, Explicit, Default). Added `TargetValidationResult` record with `Achievability` (via `TargetAchievability` enum), `GradeLevelDelta`, `Warnings` list, `SuggestedPreset`, and `IsAchievable`/`IsAlreadyMet`/`HasWarnings` computed properties with factory methods. Added `BuiltInPresets` static class with 4 presets: General Public (Grade 8, 20 words, avoid jargon), Technical (Grade 12, 25 words, explain jargon), Executive (Grade 10, 18 words, avoid jargon), International/ESL (Grade 6, 15 words, avoid jargon). Added `ReadabilityTargetService` implementation as Singleton with `IVoiceProfileService`, `IReadabilityService`, `ISettingsService`, `ILicenseContext` dependencies; custom preset persistence via `SimplifierAgent:CustomPresets` settings key; thread-safe locking for preset operations. Added `FeatureCodes.SimplifierAgent` and `FeatureCodes.CustomAudiencePresets` constants (both WriterPro tier). Added `AddReadabilityTargetService()` DI extension. Updated `AgentsModule` to v0.7.4 with registration and init verification. Spec adaptations: used `TargetGradeLevel` directly (no `VoiceProfile.TargetAudience`), used `ComplexWordRatio * 100` for percentage (no `ComplexWordPercentage`), omitted passive voice validation (no `PassiveVoicePercentage`). No new NuGet packages. Includes 78 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.4a.md)

- **Simplification Pipeline (v0.7.4b)** — Core Simplifier Agent with LLM-powered text simplification in `Lexichord.Modules.Agents.Simplifier`. Added `ISimplificationPipeline` interface with `SimplifyAsync()` for batch simplification, `SimplifyStreamingAsync()` for real-time streaming, and `ValidateRequest()` for pre-execution validation. Added `SimplificationRequest` record with `OriginalText`, `Target` (ReadabilityTarget), `DocumentPath`, `Strategy` (Conservative/Balanced/Aggressive), `GenerateGlossary`, `PreserveFormatting`, `AdditionalInstructions`, `Timeout` properties; `MaxTextLength` (50,000 chars) and timeout constants; `Validate()` method. Added `SimplificationResult` record with `SimplifiedText`, `OriginalMetrics`, `SimplifiedMetrics`, `Changes`, `Glossary`, `TokenUsage`, `ProcessingTime`, `StrategyUsed`, `TargetUsed`, `Success`, `ErrorMessage` properties; computed `GradeLevelReduction`, `WordCountDifference`, `TargetAchieved`; `Failed()` factory method. Added `SimplificationStrategy` enum (Conservative=0.3 temp, Balanced=0.4 temp, Aggressive=0.5 temp) and `SimplificationChangeType` enum (SentenceSplit, JargonReplacement, PassiveToActive, WordSimplification, ClauseReduction, TransitionAdded, RedundancyRemoved, Combined). Added `SimplificationChange` record with `OriginalText`, `SimplifiedText`, `ChangeType`, `Explanation`, nullable `Location` (TextLocation), `Confidence`. Added `SimplificationChunk` record for streaming with `TextDelta`, `IsComplete`, `CompletedChange`; `Text()` and `Complete()` factories. Added `SimplificationValidation` record with `IsValid`, `Errors`, `Warnings`, `HasWarnings`; factory methods `Valid()`, `ValidWithWarnings()`, `Invalid()`, `InvalidWithWarnings()`. Added `ISimplificationResponseParser` interface with `Parse()` method; `SimplificationParseResult` record with `SimplifiedText`, `Changes`, `Glossary`, `HasChanges`, `HasGlossary`; `Empty()` and `WithTextOnly()` factories. Added `SimplificationResponseParser` implementation using compiled regex patterns for ```simplified, ```changes, ```glossary blocks; supports arrow variations (→, ->, =>); case-insensitive block names; multi-hyphen change types (passive-to-active). Added `SimplifierAgent` class implementing `IAgent` and `ISimplificationPipeline` with `[RequiresLicense(LicenseTier.WriterPro)]`, `[AgentDefinition("simplifier", Priority = 101)]`; AgentId "simplifier", Name "The Simplifier"; Capabilities Chat|DocumentContext|StyleEnforcement|Streaming; 4000-token context budget; strategy-specific temperatures; integrated with `IContextOrchestrator`, `IReadabilityService`, `IChatCompletionService`; user vs timeout cancellation distinction; graceful context failure handling. Added `specialist-simplifier.yaml` prompt template with target readability, current metrics, strategy guidelines, output format specification. Added `AddSimplifierAgentPipeline()` DI extension registering parser and agent as singletons with IAgent forwarding. Updated `AgentsModule` with pipeline registration and init verification. Spec adaptations: implements `IAgent` directly (no BaseAgent), uses nullable `TextLocation` (no TextSpan), omits `PassiveVoicePercentage`. No new NuGet packages. Includes 70 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.4b.md)

- **Preview/Diff UI (v0.7.4c)** — Interactive preview interface for simplification changes in `Lexichord.Modules.Agents.Simplifier`. Added `SimplificationPreviewViewModel` orchestrating preview experience with `AcceptAllCommand`, `AcceptSelectedCommand`, `RejectAllCommand`, `ResimplifyCommand`, and view mode switching; undo group integration via `IEditorService`; partial acceptance via `BuildMergedText()` applying selected changes in position order. Added `SimplificationChangeViewModel` wrapping `SimplificationChange` with `IsSelected`, `IsExpanded`, `IsHighlighted` observables and computed display properties (`ChangeTypeDisplay`, `ChangeTypeIcon`, `ChangeTypeBadgeClass` for color-coded badges). Added 3 MediatR events: `SimplificationAcceptedEvent` (with `IsPartialAcceptance`, `AcceptanceRate` computed), `SimplificationRejectedEvent` (with standard reason constants and factory methods), `ResimplificationRequestedEvent` (with `IsPresetChange`, `IsStrategyChange` computed). Added `DiffViewMode` enum (SideBySide, Inline, ChangesOnly) and `CloseRequestedEventArgs` for view close coordination. Added DiffPlex 1.7.2 integration with `DiffTextBox` (side-by-side using `SideBySideDiffBuilder`), `InlineDiffView` (unified using `InlineDiffBuilder`), `ChangesOnlyView` (card-based with checkboxes). Added `ReadabilityComparisonPanel` showing before/after metrics with grade reduction badge. Added `SimplificationPreviewView` main panel with preset selector, view mode tabs, keyboard shortcuts (Ctrl+Enter=Accept All, Escape=Reject), and license warning for non-WriterPro users. Added `AddSimplifierPreviewUI()` DI extension registering `SimplificationPreviewViewModel` as transient. Updated `AgentsModule` with DI call. DiffPlex 1.7.2 is the only new NuGet package. Includes 71 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.4c.md)

- **Batch Simplification (v0.7.4d)** — Document-wide paragraph-by-paragraph simplification with progress tracking in `Lexichord.Modules.Agents.Simplifier`. Added `IBatchSimplificationService` interface with `SimplifyDocumentAsync()`, `SimplifySelectionsAsync()`, `SimplifyDocumentStreamingAsync()`, and `EstimateCostAsync()` methods. Added `BatchSimplificationService` implementation orchestrating paragraph-by-paragraph processing with skip detection (already simple, too short, headings, code blocks, blockquotes, list items), progress reporting with time estimation, cancellation support, and atomic undo via `IEditorService.BeginUndoGroup()`. Added `ParagraphParser` for document parsing with offset tracking and paragraph type detection (Normal, Heading, CodeBlock, Blockquote, ListItem). Added enums: `BatchSimplificationScope` (EntireDocument, Selection, CurrentSection, ComplexParagraphsOnly), `BatchSimplificationPhase` (Initializing through Failed), `ParagraphSkipReason` (None through IsListItem), `ParagraphType`. Added records: `TextSelection`, `BatchSimplificationOptions`, `BatchSimplificationProgress`, `BatchSimplificationEstimate`, `ParsedParagraph`, `ParagraphSimplificationResult`, `BatchSimplificationResult` with aggregate metrics and factory methods. Added 3 MediatR events: `SimplificationCompletedEvent` (batch completion), `ParagraphSimplifiedEvent` (per-paragraph progress), `BatchSimplificationCancelledEvent`. Added `BatchProgressViewModel` for progress dialog with cancellation support and time estimation. Added `BatchCompletionViewModel` for completion summary with before/after metrics. Added `BatchProgressView.axaml` and `BatchCompletionView.axaml` UI dialogs. Added `AddBatchSimplificationService()` DI extension registering service as singleton, ViewModels as transient. Updated `AgentsModule` with DI call. No new NuGet packages. Includes 48+ unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.4d.md)

#### Sub-Part Changelogs

| Version                               | Title                        | Status      |
| ------------------------------------- | ---------------------------- | ----------- |
| [v0.7.4a](v0.7.x/LCS-CL-v0.7.4a.md) | Readability Target Service   | ✅ Complete |
| [v0.7.4b](v0.7.x/LCS-CL-v0.7.4b.md) | Simplification Pipeline      | ✅ Complete |
| [v0.7.4c](v0.7.x/LCS-CL-v0.7.4c.md) | Preview/Diff UI              | ✅ Complete |
| [v0.7.4d](v0.7.x/LCS-CL-v0.7.4d.md) | Batch Simplification         | ✅ Complete |

---

## [v0.7.3] - 2026-02 (Complete)

### The Editor Agent (AI-Powered Text Rewriting)

This release introduces the Editor Agent system, enabling writers to select text and apply AI-powered rewriting transformations directly from the editor context menu.

#### What's New

- **EditorViewModel Integration (v0.7.3a)** — Context menu integration for AI rewrite commands in `Lexichord.Modules.Agents.Editor`. Added `RewriteIntent` enum (Formal, Simplified, Expanded, Custom) and `RewriteCommandOption` record for command metadata with kebab-case validation. Added `IEditorAgentContextMenuProvider` interface with `GetRewriteMenuItems()`, `CanRewrite`, `HasSelection`, `IsLicensed`, `CanRewriteChanged`, and `ExecuteRewriteAsync()`. Added `EditorAgentContextMenuProvider` implementation subscribing to `IEditorService.SelectionChanged` and `ILicenseContext.LicenseChanged` events, with MediatR event publishing for decoupled command handling. Added 3 MediatR events: `RewriteRequestedEvent` (carries intent, selected text, span, document path, custom instruction), `ShowUpgradeModalEvent` (triggers license upgrade modal), `ShowCustomRewriteDialogEvent` (opens custom instruction dialog). Added `RewriteCommandViewModel` with `RewriteFormallyCommand`, `SimplifyCommand`, `ExpandCommand`, `CustomRewriteCommand` async RelayCommands, `IsExecuting`/`Progress`/`ProgressMessage` state, and `CanRewriteChanged` event handling. Added `RewriteKeyboardShortcuts` implementing `IKeyBindingConfiguration` with Ctrl+Shift+R/S/E/C bindings conditioned on EditorHasSelection. Added `AddEditorAgentContextMenu()` DI extension registering provider as singleton, ViewModel as transient, shortcuts as singleton. Added `FeatureCodes.EditorAgent` constant for license gating. Updated `AgentsModule` to v0.7.3 with `services.AddEditorAgentContextMenu()` call and initialization verification. Design decision: uses MediatR events to decouple v0.7.3a from v0.7.3b (IRewriteCommandHandler doesn't exist yet), avoiding circular dependencies. No new NuGet packages. Includes 34 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.3a.md)

- **Agent Command Pipeline (v0.7.3b)** — Complete rewrite pipeline from event to LLM invocation in `Lexichord.Modules.Agents.Editor`. Added `RewriteRequest` record with validation (empty text, max 50,000 chars, Custom without instruction), `TimeSpan Timeout` (default 30s), and computed `EstimatedTokens`. Added `RewriteResult` record with `Failed()` factory method, `UsageMetrics` tracking, and `TimeSpan Duration`. Added `RewriteProgressUpdate` record with `RewriteProgressState` enum (Initializing, GatheringContext, GeneratingRewrite, Completed, Failed) for streaming support. Added `IEditorAgent` interface extending `IAgent` with `RewriteAsync()`, `RewriteStreamingAsync()`, and `GetTemplateId()`. Added `IRewriteCommandHandler` interface with `ExecuteAsync()`, `ExecuteStreamingAsync()`, `Cancel()`, and `IsExecuting`. Added `IRewriteApplicator` interface (forward-declared for v0.7.3d) with `ApplyRewriteAsync()`, `PreviewRewriteAsync()`, `CommitPreviewAsync()`, `CancelPreviewAsync()`. Added `EditorAgent` implementation (`[AgentDefinition("editor", Priority = 101)]`, `[RequiresLicense(LicenseTier.WriterPro)]`) with `IContextOrchestrator` integration (4000-token budget, required ["style", "terminology"] strategies), intent-specific temperatures (Formal=0.3, Simplified=0.4, Expanded/Custom=0.5), `UsageMetrics.Calculate()` for cost tracking, user cancellation vs timeout distinction, and graceful context assembly failure degradation. Added `RewriteCommandHandler` orchestrating the pipeline: license gating via `ILicenseContext.IsFeatureEnabled(FeatureCodes.EditorAgent)`, `IsExecuting` state tracking with linked `CancellationTokenSource`, `RewriteStartedEvent`/`RewriteCompletedEvent` MediatR publishing, optional `IRewriteApplicator?` delegation (null until v0.7.3d), and finally-block cleanup. Added `RewriteRequestedEventHandler` implementing `INotificationHandler<RewriteRequestedEvent>` bridging v0.7.3a events to the pipeline with error swallowing for MediatR safety. Added 4 YAML prompt templates (editor-rewrite-formal, editor-rewrite-simplify, editor-rewrite-expand, editor-rewrite-custom) with Mustache syntax (`{{selection}}`, `{{#style_rules}}`, `{{#terminology}}`, `{{#surrounding_context}}`, `{{custom_instruction}}`). Added `AddEditorAgentPipeline()` DI extension registering `EditorAgent` as singleton (dual `IEditorAgent`/`IAgent`), `RewriteCommandHandler` as scoped. Updated `AgentsModule` with `services.AddEditorAgentPipeline()` and initialization verification. Spec adaptations: no `BaseAgent` (implements `IAgent` directly), `FeatureCodes.EditorAgent` (not `FeatureFlags`), `UsageMetrics(int, int, decimal)` (not 3 ints), `ChatOptions.Temperature` is `double?` (not float). No new NuGet packages. Includes 53 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.3b.md)

- **Context-Aware Rewriting (v0.7.3c)** — Context strategies for tone-consistent, terminology-aware rewrites in `Lexichord.Modules.Agents.Editor.Context`. Added `SurroundingTextContextStrategy` (StrategyId `"surrounding-text"`, priority Critical=100, 1500 tokens, WriterPro) gathering surrounding paragraph context via `IEditorService.GetDocumentByPath()?.Content` with active-document fallback, paragraph splitting by `"\n\n"`, cursor-to-paragraph index mapping, 2-before/2-after paragraph collection with 3000-char budget guard, `[SELECTION IS HERE]` marker formatting, and 0.9f relevance. Added `EditorTerminologyContextStrategy` (StrategyId `"terminology"`, priority Medium=60, 800 tokens, WriterPro) scanning selected text against `ITerminologyRepository.GetAllActiveTermsAsync()` with `StyleTerm.MatchCase` support (ordinal vs case-insensitive), severity-ordered results (Error > Warning > Suggestion), 15-term limit, category-grouped formatting with replacement suggestions and notes, and 0.8f relevance. Both strategies extend `ContextStrategyBase` using `CreateFragment()`, `TruncateToMaxTokens()`, `ValidateRequest()`, and return null gracefully on missing prerequisites or exceptions. Registered 2 new entries in `ContextStrategyFactory._registrations` (9 total, both WriterPro tier). Added `AddEditorAgentContextStrategies()` DI extension registering both as transient. Updated `EditorAgent.GatherRewriteContextAsync()` RequiredStrategies from `["style", "terminology"]` to `["surrounding-text", "style", "terminology"]`. Updated `AgentsModule` with DI call and initialization verification. Spec adaptations: skipped `EditorRewriteContextStrategy` coordinator (orchestrator handles coordination), skipped `StyleRulesContextStrategy` (existing `StyleContextStrategy` at Teams tier), skipped `TokenBudgetManager` (base class handles it), adapted spec's `TermEntry` to actual `StyleTerm` entity, skipped optional `DocumentMetadataStrategy`. No new NuGet packages. Includes 45 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.3c.md)

- **Undo/Redo Integration (v0.7.3d)** — Undo/redo support for AI rewrites in `Lexichord.Modules.Agents.Editor`, completing the `IRewriteApplicator` interface forward-declared in v0.7.3b. Created `IUndoableOperation` and `IUndoRedoService` abstractions in `Lexichord.Abstractions.Contracts.Undo` (referenced as v0.1.4a in specs but never previously implemented), with `UndoRedoChangedEventArgs` for stack change notifications. Added `RewriteUndoableOperation` implementing `IUndoableOperation` using sync `IEditorService` APIs (`BeginUndoGroup` → `DeleteText` → `InsertText` → `EndUndoGroup`) for atomic text replacement, `DisplayName` format `"AI Rewrite ({Intent})"`, constructor null validation via `ArgumentNullException.ThrowIfNull`, and `Task.CompletedTask` return (sync editor APIs). Added `RewriteApplicator` implementing `IRewriteApplicator` + `IDisposable` with nullable `IUndoRedoService?` (mirroring v0.7.3b nullable `IRewriteApplicator?` pattern), preview lifecycle (`PreviewRewriteAsync`/`CommitPreviewAsync`/`CancelPreviewAsync`) with `PreviewState` record, lock-based thread safety, 5-minute auto-cancel timeout via `CancellationTokenSource`, text retrieval via `GetDocumentByPath()?.Content?.Substring()` with `GetDocumentText()` fallback, and `ApplyRewriteAsync` with undo push and event publishing. Added `IsPreviewActive` property to `IRewriteApplicator`. Added 6 MediatR events: `RewriteAppliedEvent` (DocumentPath, OriginalText, RewrittenText, Intent, OperationId, Timestamp), `RewriteUndoneEvent`, `RewriteRedoneEvent`, `RewritePreviewStartedEvent`, `RewritePreviewCommittedEvent`, `RewritePreviewCancelledEvent` — all with `static Create()` factory. Added `AddEditorAgentUndoIntegration()` DI extension registering `IRewriteApplicator` → `RewriteApplicator` as Scoped. Updated `AgentsModule` with DI call, init verification, and updated description. Spec adaptations: `ReplaceTextAsync` → sync `DeleteText`+`InsertText` in undo group, `GetTextAsync` → `GetDocumentByPath()?.Content?.Substring()`, `IUndoRedoService` accepted as nullable, namespace adapted to `Contracts.Undo`, events enriched with `Timestamp` and `Create()` factory per codebase pattern. No new NuGet packages. Includes 49 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.3d.md)

#### Sub-Part Changelogs

| Version                               | Title                       | Status      |
| ------------------------------------- | --------------------------- | ----------- |
| [v0.7.3a](v0.7.x/LCS-CL-v0.7.3a.md) | EditorViewModel Integration | ✅ Complete |
| [v0.7.3b](v0.7.x/LCS-CL-v0.7.3b.md) | Agent Command Pipeline      | ✅ Complete |
| [v0.7.3c](v0.7.x/LCS-CL-v0.7.3c.md) | Context-Aware Rewriting     | ✅ Complete |
| [v0.7.3d](v0.7.x/LCS-CL-v0.7.3d.md) | History & Undo              | ✅ Complete |

---

## [v0.7.1] - 2026-02 (In Progress)

### The Agent Registry (Persona Management)

This release introduces the Agent Registry system, enabling specialized AI agents with configurable personality variants (personas).

#### What's New

- **Agent Configuration Model (v0.7.1a)** — Foundational data contracts for agent definitions in `Lexichord.Abstractions.Agents`. Added `AgentConfiguration` partial record with agent identity (AgentId, Name, Description, Icon), behavior configuration (TemplateId, Capabilities, DefaultOptions), persona management (Personas collection, DefaultPersona property, GetPersona() method), licensing (RequiredTier), and extensibility (CustomSettings dictionary). Added `AgentPersona` partial record with persona identity (PersonaId, DisplayName, Tagline), behavioral overrides (Temperature, SystemPromptOverride), and optional VoiceDescription. Both records include comprehensive validation via `Validate()` method with kebab-case regex patterns (`^[a-z0-9]+(-[a-z0-9]+)*$`) for IDs. Extended `AgentCapabilities` enum with 6 new flags: CodeGeneration (32), ResearchAssistance (64), Summarization (128), StructureAnalysis (256), Brainstorming (512), Translation (1024), updating All flag from 31 to 2047. Updated `AgentCapabilitiesExtensions.GetCapabilityNames()` to include shortened display names for new capabilities ("CodeGen", "Research", "Summary", "Structure", "Brainstorm", "Translate"). Design decision: Used `double` instead of `float` for `AgentPersona.Temperature` to match `ChatOptions.Temperature` type and avoid casting. No new NuGet packages required. Includes 25 unit tests (17 new, 8 updated) with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.1a.md)

- **Agent Registry Implementation (v0.7.1b)** — Factory-based agent registration and runtime persona management in `Lexichord.Modules.Agents`. Extended `IAgentRegistry` with 8 new methods: `RegisterAgent(AgentConfiguration, Func<IServiceProvider, IAgent>)` for factory-based registration with lazy instantiation, `GetAgentWithPersona(agentId, personaId)` for persona-specific retrieval, `UpdateAgent(AgentConfiguration)` for hot-reload without restart, `SwitchPersona(agentId, personaId)` for runtime persona switching, `GetActivePersona(agentId)` for active persona query, `CanAccess(agentId)` for license checks, `GetConfiguration(agentId)` for config retrieval, and `AvailablePersonas` property for all personas across agents. Added `IPersonaAwareAgent` marker interface with `ActivePersona` property, `ApplyPersona(AgentPersona)` method, and `ResetToDefaultPersona()` method. Added declarative registration via `AgentDefinitionAttribute` (`[AgentDefinition("agent-id", Priority = 100)]`) and `AgentDefinitionScanner` for assembly scanning with priority-based ordering (0-50: critical, 51-100: core, 101-200: specialist, 201+: experimental). Added 3 MediatR events (`AgentRegisteredEvent`, `PersonaSwitchedEvent`, `AgentConfigReloadedEvent`) published fire-and-forget on registry operations. Added 2 new exceptions (`PersonaNotFoundException`, `AgentAccessDeniedException`) for error handling. Extended `AgentRegistry` implementation with singleton agent caching (`ConcurrentDictionary<string, IAgent>`), factory storage, and persona tracking. Marked v0.6.6c `RegisterCustomAgent(IAgent)` as [Obsolete] with v0.8.0 removal warning. Registered `AgentDefinitionScanner` as singleton in `AddAgentRegistry()`. Uses existing MediatR dependency (12.x) for event publishing. Includes 20 unit tests (15 AgentRegistryTests, 5 AgentDefinitionScannerTests) with 100% coverage. [Detailed changelog](v0.7.x/LCS-CL-v0.7.1b.md)

- **Agent Configuration Files (v0.7.1c)** — YAML-based agent configuration loading with hot-reload support in `Lexichord.Modules.Agents`. Added `IAgentConfigLoader` interface in `Lexichord.Abstractions.Agents` with methods for loading built-in agents from embedded resources (`LoadBuiltInAgentsAsync()`), workspace agents from `.lexichord/agents/` (`LoadWorkspaceAgentsAsync()`), single file loading (`LoadFromFileAsync()`), YAML parsing (`ParseYaml()`), and file watching (`StartWatching()/StopWatching()`, `AgentFileChanged` event). Added `YamlAgentConfigLoader` implementation using YamlDotNet 15.1.6 with `UnderscoredNamingConvention`, embedded resource loading, workspace directory scanning, and event-based file watching with 300ms Timer debouncing (following `FileSystemStyleWatcher` pattern, not Rx). Added `AgentConfigValidator` with validation for kebab-case IDs, required fields, temperature ranges (0.0-2.0), max tokens (warning at 128k), and duplicate persona detection. Added validation result types (`AgentConfigValidationResult`, `AgentConfigError`, `AgentConfigWarning`, `AgentFileChangedEventArgs`, `AgentFileChangeType` enum) in Abstractions. Added 3 internal YAML model classes (`AgentYamlModel`, `DefaultOptionsYamlModel`, `PersonaYamlModel`) in `Configuration.Yaml` namespace for deserialization. Included 4 built-in agent YAML definitions as embedded resources: `general-chat.yaml` (Core tier, versatile assistant), `editor.yaml` (WriterPro, grammar & clarity), `researcher.yaml` (WriterPro, research & citations), `storyteller.yaml` (WriterPro, creative fiction). License enforcement: workspace agents require WriterPro tier (`LicenseTier >= WriterPro`). Schema versioning: supports v1, rejects higher versions. Registered via `AddAgentConfigLoading()` extension method (called by `AddAgentRegistry()`). All services singleton for shared caching and file watching. YamlDotNet 15.1.6 already present. No new NuGet packages. Build successful with zero warnings. [Detailed changelog](v0.7.x/LCS-CL-v0.7.1c.md)

- **Agent Selector UI (v0.7.1d)** — Avalonia-based agent discovery and selection component for the Co-pilot panel in `Lexichord.Modules.Agents`. Added 3 ViewModels: `AgentSelectorViewModel` (dropdown orchestration with MediatR event handlers for `AgentRegisteredEvent`, `PersonaSwitchedEvent`, `AgentConfigReloadedEvent`; favorites/recents persistence via `ISettingsService`; search filtering; license-aware agent access; 6 RelayCommands), `AgentItemViewModel` (agent display wrapper with 5 computed properties: `TierBadgeText`, `ShowTierBadge`, `IsLocked`, `CapabilitiesSummary`, `AccessibilityLabel`), `PersonaItemViewModel` (persona display wrapper with temperature-based `TemperatureLabel`: <0.3="Focused", 0.3-0.6="Balanced", 0.6-0.9="Creative", ≥0.9="Experimental"). Added 2 Avalonia views: `AgentSelectorView.axaml` (dropdown with trigger button, Popup, search TextBox, ScrollViewer with 3 sections: Favorites/Recents/All Agents), `AgentCardView.axaml` (agent card with icon, name/description, tier badge, favorite toggle, lock/checkmark indicator, inline persona radio buttons). Added `FavoriteIconConverter` (boolean → star icon SVG path data: filled vs outline). Extended `AgentsServiceCollectionExtensions` with `AddAgentSelectorUI()` registering all 3 ViewModels as transient for per-instance isolation. Settings persistence keys: `agent.last_used`, `agent.last_persona`, `agent.favorites` (List<string>), `agent.recent` (List<string>, max 5). UI features: DynamicResource theming, AutomationProperties for accessibility, keyboard navigation, tier badges with pill styling, real-time MediatR updates. No new NuGet packages. Includes 33 unit tests (17 AgentSelectorViewModelTests, 12 AgentItemViewModelTests, 4 PersonaItemViewModelTests) with Moq/FluentAssertions/Xunit. [Detailed changelog](v0.7.x/LCS-CL-v0.7.1d.md)

#### Sub-Part Changelogs

| Version                             | Title                         | Status      |
| ----------------------------------- | ----------------------------- | ----------- |
| [v0.7.1a](v0.7.x/LCS-CL-v0.7.1a.md) | Agent Configuration Model     | ✅ Complete |
| [v0.7.1b](v0.7.x/LCS-CL-v0.7.1b.md) | Agent Registry Implementation | ✅ Complete |
| [v0.7.1c](v0.7.x/LCS-CL-v0.7.1c.md) | Agent Configuration Files     | ✅ Complete |
| [v0.7.1d](v0.7.x/LCS-CL-v0.7.1d.md) | Agent Selector UI             | ✅ Complete |

---

## [v0.7.2] - 2026-02 (In Progress)

### The Context Assembler (Intelligent Context Gathering)

This release introduces the Context Assembler system, enabling intelligent, priority-based gathering of contextual information from multiple sources to provide to AI agents during request processing.

#### What's New

- **Context Strategy Interface (v0.7.2a)** — Pluggable abstraction layer for context gathering in `Lexichord.Abstractions.Agents.Context` and `Lexichord.Modules.Agents.Context`. Added 6 core abstractions: `StrategyPriority` static class with 5 priority constants (Critical=100, High=80, Medium=60, Low=40, Optional=20); `ContextGatheringRequest` record with DocumentPath, CursorPosition, SelectedText, AgentId, Hints, plus `Empty()` factory, type-safe `GetHint<T>()` method, and computed properties (HasDocument, HasSelection, HasCursor); `ContextFragment` record with SourceId, Label, Content, TokenEstimate, Relevance, plus `Empty()` factory, `TruncateTo()` smart paragraph-aware truncation, and HasContent property; `ContextBudget` record with MaxTokens, RequiredStrategies, ExcludedStrategies, plus `Default` (8000 tokens) and `WithLimit()` factories, and filtering helpers (IsRequired, IsExcluded, ShouldExecute); `IContextStrategy` interface with StrategyId, DisplayName, Priority, MaxTokens properties and `GatherAsync()` method (returns null when no context, throws on errors); `IContextStrategyFactory` interface with AvailableStrategyIds property and methods for strategy creation (CreateStrategy, CreateAllStrategies) and license checking (IsAvailable). Added 2 implementations: `ContextStrategyBase` abstract class with protected constructor taking ITokenCounter and ILogger, plus helper methods `CreateFragment()` (auto token estimation with logging), `TruncateToMaxTokens()` (smart paragraph truncation with warnings), and `ValidateRequest()` (prerequisite checking with debug logging); `ContextStrategyFactory` with static registration dictionary (intentionally empty in v0.7.2a), license tier filtering via ILicenseContext.Tier, and DI-based strategy resolution. Extended `AgentCapabilitiesExtensions` with `ToDisplayString()` method (joins GetCapabilityNames with commas, returns "None" for AgentCapabilities.None). License tier mapping: WriterPro (document, selection, cursor, heading), Teams (all WriterPro + rag, style). Registered via `AddContextStrategies()` extension method. Design decisions: Coexists with v0.6.3d IContextProvider (different use cases), token counting via constructor injection, factory-level license gating, null return for no context vs exceptions for errors, singleton factory with transient strategies. No concrete strategies in v0.7.2a (added in v0.7.2b). Reuses v0.6.1b ITokenCounter and v0.0.6a ILicenseContext. No new NuGet packages. Includes 88 unit tests (6 interface contract tests, 4 record tests, 2 implementation tests) with 100% code coverage and FluentAssertions/NSubstitute. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2a.md)

- **Context Orchestrator (v0.7.2c)** — Central coordination component in `Lexichord.Modules.Agents.Context` that orchestrates parallel strategy execution, content deduplication, priority-based sorting, token budget enforcement, and event publishing. Added `IContextOrchestrator` interface in `Lexichord.Abstractions.Agents.Context` with `AssembleAsync()` (8-step pipeline: filter→execute→collect→deduplicate→sort→trim→extract→publish), `GetStrategies()`, `SetStrategyEnabled()`, and `IsStrategyEnabled()`. Added `AssembledContext` record with `Empty` property, `HasContext`, `GetCombinedContent()` (markdown section formatting), `GetFragment()`, and `HasFragmentFrom()`. Added `ContextOrchestrator` sealed implementation using `IContextStrategyFactory`, `ITokenCounter`, `IMediator`, `IOptions<ContextOptions>`, with `ConcurrentDictionary<string,bool>` for runtime strategy toggle state, `Parallel.ForEachAsync` for concurrent execution with per-strategy `CancellationTokenSource.CancelAfter()` timeout, Jaccard word-set similarity deduplication via `ContentDeduplicator`, orchestrator-level priority mapping (document=100, selection=80, cursor=70, heading=70, rag=60, style=50), and smart budget trimming with 100-token minimum truncation threshold. Added `ContextOptions` sealed class with `DefaultBudget=8000`, `StrategyTimeout=5000ms`, `EnableDeduplication=true`, `DeduplicationThreshold=0.85f`, `MaxParallelism=6`. Added `ContentDeduplicator` public static class with `CalculateJaccardSimilarity()` using normalized word-set comparison (lowercase, punctuation stripping, ≤2-char filtering). Added `ContextAssembledEvent` and `StrategyToggleEvent` MediatR `INotification` records for UI synchronization. Registered via `AddContextOrchestrator()` extension method (called from `AddContextStrategies()`): `AddOptions<ContextOptions>()` with defaults, `AddSingleton<IContextOrchestrator, ContextOrchestrator>()`. Spec adaptation: uses `IMediator.Publish()` instead of spec's `IEventBus` (which does not exist in codebase). No new NuGet packages. Includes 48 unit tests (13 AssembledContext, 12 ContentDeduplicator, 23 ContextOrchestrator) with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2c.md)

- **Context Preview Panel (v0.7.2d)** — Real-time UI panel for displaying assembled context fragments, token budget usage, and strategy toggle controls in `Lexichord.Modules.Agents.Chat`. Added `ContextPreviewBridge` singleton MediatR notification handler that bridges `ContextAssembledEvent` and `StrategyToggleEvent` to C# events for transient ViewModel consumption. Added `FragmentViewModel` with content truncation (200 chars), expand/collapse toggling, source-specific icons, and formatted token/relevance display. Added `StrategyToggleItem` with callback-based orchestrator integration via `partial void OnIsEnabledChanged()` and strategy-specific tooltips. Added `ContextPreviewViewModel` as the main panel orchestrator with `ObservableCollection<FragmentViewModel>` and `ObservableCollection<StrategyToggleItem>`, computed `BudgetPercentage`/`BudgetStatusText`/`DurationText` properties, `EnableAll`/`DisableAll` commands, injectable `Action<Action>` dispatch delegate for UI thread marshaling, and `IDisposable` lifecycle with event unsubscription. Added `ContextPreviewView.axaml` with token budget ProgressBar, strategy CheckBox WrapPanel, scrollable fragment cards with monospace content, loading overlay, and DynamicResource theming. Added `AddContextPreviewPanel()` DI registration and `services.AddContextStrategies()` call in `AgentsModule.RegisterServices()` to wire up the complete v0.7.2 pipeline. Spec adaptations: `ContextPreviewBridge` replaces `IEventBus`, `Action<Action>` replaces `IDispatcher`, standard `IDisposable` replaces `CompositeDisposable`. No new NuGet packages. Includes 58 unit tests with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2d.md)

- **Knowledge Context Strategy (v0.7.2e)** — Knowledge graph context strategy in `Lexichord.Modules.Agents.Context.Strategies`, bridging the v0.6.6e `IKnowledgeContextProvider` pipeline into the v0.7.2 Context Assembler. Added `KnowledgeContextConfig` sealed record with agent-specific configuration (MaxTokens, MaxEntities, IncludeEntityTypes, MinRelevanceScore, IncludeRelationships, IncludeAxioms, Format, MaxPropertiesPerEntity) and `with`-expression support for license-tier restriction application. Added `KnowledgeContextStrategy` extending `ContextStrategyBase` (priority 30, 4000 tokens, Teams+) with static agent configuration dictionary mapping 5 agent IDs (editor, simplifier, tuning, summarizer, co-pilot) to tailored configs, 5 hint keys (MaxEntities, MinRelevanceScore, IncludeAxioms, IncludeRelationships, KnowledgeFormat) for per-request customization, WriterPro tier restrictions (max 10 entities, no axioms, no relationships), and graceful error handling (`FeatureNotLicensedException` → null, `OperationCanceledException` → rethrow). GatherAsync pipeline: build search query from selected text (preferred) or document filename → resolve agent config → apply hints → apply license restrictions → build `KnowledgeContextOptions` → delegate to `IKnowledgeContextProvider.GetContextAsync()` → truncate → create fragment with relevance heuristic (0.7f if truncated, entity count ratio otherwise). Key spec adaptation: uses `IKnowledgeContextProvider` (single Abstractions-layer interface) instead of directly depending on `IGraphRepository`, `IEntityRelevanceRanker`, `IKnowledgeContextFormatter`, and `IAxiomStore` to respect module boundary (Agents references only Abstractions). Registered as 7th strategy in `ContextStrategyFactory._registrations` and `AddContextStrategies()` DI extension. No new NuGet packages. Includes 46 unit tests with 100% pass rate plus updated factory tests. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2e.md)

- **Entity Relevance Scorer (v0.7.2f)** — Multi-signal entity relevance scoring system in `Lexichord.Modules.Knowledge.Copilot.Context.Scoring`, extending the v0.6.6e term-based `EntityRelevanceRanker` with five weighted signals: semantic similarity (35%, cosine via `IEmbeddingService` embeddings), document mention counting (25%, saturates at 5 mentions), entity type matching (20%, preferred=1.0/non-preferred=0.3/neutral=0.5), recency decay (10%, linear over configurable window), and name matching (10%, substring + term-level). Added 4 abstraction records in `Lexichord.Abstractions.Contracts.Knowledge.Copilot`: `ScoringConfig` (immutable signal weights + `PreferredTypes` + `RecencyDecayDays`), `RelevanceSignalScores` (per-signal breakdown for diagnostic transparency), `ScoringRequest` (focused scorer input with Query, DocumentContent, PreferredEntityTypes), `ScoredEntity` (entity with composite score, per-signal breakdown, and matched terms). Added `IEntityRelevanceScorer` async interface with `ScoreEntitiesAsync` and `ScoreEntityAsync` methods. Added `CosineSimilarity` static utility (dot-product / norms, clamped [0,1], handles null/empty/mismatched/zero-magnitude). Added `EntityRelevanceScorer` implementation with batch embedding via `IEmbeddingService.EmbedBatchAsync()`, nullable `IEmbeddingService` with graceful fallback (redistributes semantic weight proportionally to remaining signals: Mention≈0.385, Type≈0.308, Recency≈0.154, Name≈0.154). Integrated into `KnowledgeContextProvider` as optional `IEntityRelevanceScorer?` — when available, preferred over term-based ranker; converts `ScoredEntity` → `RankedEntity` for existing `SelectWithinBudget`. Registered in `KnowledgeModule` as singleton via factory lambda resolving `IEmbeddingService?` optionally. No new NuGet packages. Includes 57 unit tests (16 CosineSimilarity, 41 EntityRelevanceScorer) with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2f.md)

- **Knowledge Context Formatter (v0.7.2g)** — Enhanced knowledge context formatter in `Lexichord.Modules.Knowledge.Copilot.Context`, extending the v0.6.6e `IKnowledgeContextFormatter` with metadata-rich output and token budget enforcement. Added `FormattedContext` sealed record wrapping formatter output with metadata (token count, entity/relationship/axiom counts, truncation flag) in `Lexichord.Abstractions.Contracts.Knowledge.Copilot`. Added `FormatWithMetadata()` returning `FormattedContext` and `TruncateToTokenBudget()` for clean-boundary truncation with 5% safety buffer and `[Context truncated due to token limit]` marker. Added optional `ITokenCounter` integration for accurate token estimation (falls back to char/4 heuristic when unavailable). Enhanced all four output formats: YAML (`# Knowledge Context` header, `EscapeYaml()` for string safety, `FormatPropertyValue()` type-aware formatting, axiom severity field, entity name resolution for relationships), Markdown (entity grouping by type with `**TypeName**` headers, entity name resolution for relationships, axiom severity display), JSON (`JsonIgnoreCondition.WhenWritingNull` for null suppression, axiom severity, entity names in relationships), Plain (`KNOWLEDGE CONTEXT:`, `RELATIONSHIPS:`, `RULES:` section headers, entity name resolution). Entity name resolution via ID→name dictionary built from provided entities, with short GUID prefix fallback for unknown IDs. Updated `KnowledgeContextProvider` to use `FormatWithMetadata()` in both `GetContextAsync()` and `GetContextForEntitiesAsync()`. DI registration updated to factory lambda resolving optional `ITokenCounter?`. No new NuGet packages. Includes 24 new unit tests (19 enhanced formatter tests, 5 FormattedContext record tests) plus 12 updated existing tests. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2g.md)

- **Context Assembler Integration (v0.7.2h)** — Final integration step completing CKVS Phase 4a (Graph Context Strategy) by wiring the v0.7.2e `KnowledgeContextStrategy` into the Context Assembler's UI and orchestration pipeline in `Lexichord.Modules.Agents`. Added explicit "knowledge" priority mapping (30 = Optional + 10) to `ContextOrchestrator.GetPriorityForSource()`, correcting the previous fallthrough to Low (40) which was higher than the strategy's intended priority. Added "knowledge" → "🧠" (brain emoji) icon mapping to `FragmentViewModel.SourceIcon` for Context Preview panel display. Added "knowledge" → "Include knowledge graph entities and relationships" tooltip to `StrategyToggleItem.Tooltip` for toggle hover text. Spec adaptation: the proposed `IContextAssemblerBuilder`, `ContextAssemblerExtensions`, `KnowledgeContextOptions`, and `ScoringWeights` were unnecessary — the existing `ContextStrategyFactory` and `AddContextStrategies()` DI extension already handle registration from v0.7.2e. No new NuGet packages. Includes 16 new integration tests (priority mapping, fragment display, toggle behavior, pipeline inclusion/exclusion, deduplication, event publishing, variable extraction) plus 2 updated existing tests. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2h.md)

- **Built-in Context Strategies (v0.7.2b)** — 6 concrete context strategy implementations in `Lexichord.Modules.Agents.Context.Strategies`, populating the v0.7.2a factory with real data-gathering logic. Added `DocumentContextStrategy` (priority 100, 4000 tokens, WriterPro) providing full document content via `IEditorService.GetDocumentByPath()` with active-document fallback and heading-aware smart truncation. Added `SelectionContextStrategy` (priority 80, 1000 tokens, WriterPro) wrapping user-selected text in `<<SELECTED_TEXT>>` markers with surrounding paragraph context. Added `CursorContextStrategy` (priority 80, 500 tokens, WriterPro) extracting a configurable text window around cursor position, expanding to word boundaries, and inserting `▌` cursor marker. Added `HeadingContextStrategy` (priority 70, 300 tokens, WriterPro) resolving document path to `Document` via `IDocumentRepository`, building heading tree via `IHeadingHierarchyService`, and formatting as indented outline with optional breadcrumb. Added `RAGContextStrategy` (priority 60, 2000 tokens, Teams) performing semantic search using selected text or document file name as query, with configurable `TopK`/`MinScore` hints, graceful `FeatureNotLicensedException` handling, and `OperationCanceledException` propagation. Added `StyleContextStrategy` (priority 50, 1000 tokens, Teams) retrieving active `StyleSheet` from `IStyleEngine`, filtering rules by agent type (editor→Syntax/Terminology, simplifier→Formatting/Syntax, tuning→Terminology, others→all), and formatting grouped by `RuleCategory`. Populated `ContextStrategyFactory._registrations` with 6 entries mapping strategy IDs to types and license tiers. Added 6 transient DI registrations in `AddContextStrategies()`. Adapted design spec to actual codebase interfaces: `IEditorService` sync API (vs spec's async), `IHeadingHierarchyService` (vs spec's `IDocumentStructureService`), `IStyleEngine` (vs spec's `IStyleRuleRepository`). Updated `ContextStrategyFactoryTests` for real strategy IDs with tier-based filtering verification. No new NuGet packages. Includes 107 unit tests (85 new strategy tests, 22 updated factory tests) with 100% pass rate. [Detailed changelog](v0.7.x/LCS-CL-v0.7.2b.md)

#### Sub-Part Changelogs

| Version                             | Title                        | Status      |
| ----------------------------------- | ---------------------------- | ----------- |
| [v0.7.2a](v0.7.x/LCS-CL-v0.7.2a.md) | Context Strategy Interface   | ✅ Complete |
| [v0.7.2b](v0.7.x/LCS-CL-v0.7.2b.md) | Built-in Context Strategies  | ✅ Complete |
| [v0.7.2c](v0.7.x/LCS-CL-v0.7.2c.md) | Context Orchestrator          | ✅ Complete |
| [v0.7.2d](v0.7.x/LCS-CL-v0.7.2d.md) | Context Preview Panel         | ✅ Complete |
| [v0.7.2e](v0.7.x/LCS-CL-v0.7.2e.md) | Knowledge Context Strategy    | ✅ Complete |
| [v0.7.2f](v0.7.x/LCS-CL-v0.7.2f.md) | Entity Relevance Scorer       | ✅ Complete |
| [v0.7.2g](v0.7.x/LCS-CL-v0.7.2g.md) | Knowledge Context Formatter   | ✅ Complete |
| [v0.7.2h](v0.7.x/LCS-CL-v0.7.2h.md) | Context Assembler Integration  | ✅ Complete |

---

## [v0.6.8] - 2026-02 (In Progress)

### The Hardening (Reliability & Performance)

This release focuses on production-readiness with comprehensive testing, error handling, and optimization for the Agents module.

#### What's New

- **Unit Test Suite (v0.6.8a)** — 96 unit tests for all Agents module core services in `Lexichord.Tests.Unit`. Added 4 shared test fixtures (`MockHttpMessageHandler`, `TestChatResponses`, `TestPromptTemplates`, `TestStreamingTokens`) and 8 test classes covering `SSEParser` (OpenAI + Anthropic formats), `CoPilotAgent` lifecycle, `AgentRegistry` discovery and license gating, `StreamingChatHandler` state management, `ContextInjector` provider orchestration, `UsageTracker` accumulation and event publishing, and `MustachePromptRenderer` edge cases. All tests pass with deterministic mocks (Moq) and assertions (FluentAssertions). Includes SSE malformed JSON resilience, provider timeout handling, parallel execution verification, and graceful degradation testing.

- **Integration Tests (v0.6.8b)** — 22 integration tests for end-to-end `CoPilotAgent` invocation workflows in `Lexichord.Tests.Unit`. Added `MockLLMServer` fixture wrapping configurable `Mock<IChatCompletionService>` with fluent API for batch responses, streaming tokens, and error scenarios. Added `IntegrationTestBase` shared base class pre-configuring all 9 `CoPilotAgent` dependencies. 4 test classes: `AgentWorkflowTests` (7 tests — message/response, document context, selection, multi-turn history, validation, usage metrics, template verification), `StreamingIntegrationTests` (5 tests — token relay, content assembly, completion signal, Teams license gating, cancellation), `ContextInjectionIntegrationTests` (5 tests — RAG context, style rules, graceful degradation, citation production, no-citation case), `ErrorScenarioTests` (5 tests — HTTP timeout, rate limiting, auth failure, cancellation propagation, streaming error). Spec deviation: uses Moq instead of WireMock.Net since `CoPilotAgent` consumes `IChatCompletionService` directly. All tests pass deterministically (~1s total).

- **Performance Optimization (v0.6.8c)** — Performance optimization subsystem for the Agents module. Added 3 interfaces (`IConversationMemoryManager`, `IRequestCoalescer`, `ICachedContextAssembler`) and their implementations (`ConversationMemoryManager`, `RequestCoalescer`, `CachedContextAssembler`) in `Lexichord.Modules.Agents.Performance`. Added `PerformanceOptions` record for configurable limits (max messages: 50, max memory: 5MB, coalescing window: 100ms, cache duration: 30s, max compiled templates: 100) and `PerformanceBaseline` record for measurement tracking. ConversationMemoryManager preserves system messages during trimming with UTF-16 byte estimation. RequestCoalescer uses background batch processing with `TaskCompletionSource` bridging. CachedContextAssembler wraps `IContextInjector` with `MemoryCache` (size limit: 100) and document-path-keyed invalidation. Added `ObjectPool<StringBuilder>` for GC pressure reduction. DI registration: PerformanceOptions via `IOptions`, all services as singletons. Added NuGet packages: `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.ObjectPool`, `System.IO.Pipelines`. Includes 30 unit tests across 3 test classes and BenchmarkDotNet performance benchmarks targeting <5ms cache hit, <10ms conversation trim (100 messages), <1ms memory estimation at P95.

- **Error Handling & Recovery (v0.6.8d)** — Error handling and recovery subsystem for the Agents module. Added structured exception hierarchy (`AgentException`, `ProviderException`, `AgentRateLimitException`, `AgentAuthenticationException`, `ProviderUnavailableException`, `InvalidResponseException`, `TokenLimitException`, `ContextAssemblyException`) with `RecoveryStrategy` enum (Retry, Queue, Truncate, Fallback, None). Added 3 interfaces (`IErrorRecoveryService`, `IRateLimitQueue`, `ITokenBudgetManager`) and their implementations (`ErrorRecoveryService`, `RateLimitQueue`, `TokenBudgetManager`) in `Lexichord.Modules.Agents.Resilience`. Added `ResilientChatService` decorator wrapping `IChatCompletionService` with Polly v8 resilience pipeline (exponential retry with jitter, circuit breaker, timeout). Rate-limited requests are queued via `System.Threading.Channels` with `TaskCompletionSource` bridging. Token budget enforcement preserves system messages and newest conversation history during truncation. MediatR `AgentErrorEvent` notifications provide telemetry. DI registration: all services as singletons, `ResilientChatService` wraps default provider from `ILLMProviderRegistry`. Added `Microsoft.Extensions.Resilience` NuGet package. Includes 102 unit tests across 4 test classes.

#### Sub-Part Changelogs

| Version                        | Title                    | Status      |
| ------------------------------ | ------------------------ | ----------- |
| [v0.6.8a](v0.6.x/v0.6.8a.md) | Unit Test Suite          | ✅ Complete |
| [v0.6.8b](v0.6.x/v0.6.8b.md) | Integration Tests        | ✅ Complete |
| [v0.6.8c](v0.6.x/v0.6.8c.md) | Performance Optimization | ✅ Complete |
| [v0.6.8d](v0.6.x/v0.6.8d.md) | Error Handling & Recovery | ✅ Complete |

---

## [v0.6.7] - 2026-02 (In Progress)

### Selection Context (Editor → Co-pilot Bridge)

This release enables sending selected editor text to the Co-pilot chat panel, with smart default prompt generation based on selection characteristics.

#### What's New

- **Selection Context (v0.6.7a)** — Editor-to-Co-pilot text selection bridge in `Lexichord.Modules.Agents`. Added `ISelectionContextService` interface with `SendSelectionToCoPilotAsync`, `GenerateDefaultPrompt`, `HasActiveSelection`, `CurrentSelection`, and `ClearSelectionContext`. Implemented `SelectionContextService` with license gating (WriterPro+ via `GetCurrentTier()`), stale context detection via `IEditorService.SelectionChanged`, and ViewModel coordination. Added `DefaultPromptGenerator` with selection-aware prompt logic: code patterns → "Review this code:" (checked first for short code snippets), short <50 chars → "Explain this:", long >500 chars → "Summarize this:", default → "Improve this:". Added `SelectionContextCommand` (`ICommand`) for `Ctrl+Shift+A` shortcut and context menu binding, publishing `SelectionContextSetEvent` MediatR notification on success. Added `EditorContextMenuExtensions` registering "Ask Co-pilot about selection" menu item (license-gated, selection-enabled). Added `SelectionContextKeyBindings` (`IKeyBindingConfiguration`) for `Ctrl+Shift+A` → `copilot.sendSelection` with `EditorHasSelection` context condition. Added `SelectionContextIndicator.axaml` Avalonia UserControl displaying context summary, preview, and clear button. Extended `CoPilotViewModel` with `SetSelectionContext`, `ClearSelectionContext`, `FocusChatInput`, `HasSelectionContext`, `SelectionSummary`, `SelectionPreview`, and `FocusChatInputRequested` event. Extended `IEditorService` with `GetSelectedText()`, `SelectionChanged` event, and `RegisterContextMenuItem()`. Added `SelectionChangedEventArgs`, `ContextMenuItem`, and `IKeyBindingConfiguration` abstractions. Adapted spec's `LicenseRequiredException` to existing `LicenseTierException` and `HasFeature(LicenseFeature)` to `GetCurrentTier()` tier comparison. Registered in `AgentsModule` DI. Includes 13 unit tests.

- **Inline Suggestions (v0.6.7b)** — AI-generated text preview and insertion system in `Lexichord.Modules.Agents`. Added `IEditorInsertionService` interface with `InsertAtCursorAsync`, `ReplaceSelectionAsync`, `ShowPreviewAsync`, `AcceptPreviewAsync`, `RejectPreviewAsync`, `IsPreviewActive`, `CurrentPreviewText`, `CurrentPreviewLocation`, and `PreviewStateChanged` event. Implemented `EditorInsertionService` with preview lifecycle management, transaction-based undo grouping via `IEditorService.BeginUndoGroup`/`EndUndoGroup`, and `Dispatcher.UIThread` marshalling. Added `TextSpan` record with `Start`, `Length`, `End`, `Contains`, `OverlapsWith`, `Intersect`, and `FromStartEnd` factory. Added `PreviewStateChangedEventArgs` with `IsActive`, `PreviewText`, and `Location`. Added `InsertAtCursorCommand` (`ICommand`) with WriterPro license gating via `GetCurrentTier()`, string parameter or `TextToInsert` property binding, and optional preview mode via `UsePreview` flag. Added `ReplaceSelectionCommand` (`ICommand`) with selection guard via `IEditorService.HasSelection`, WriterPro license gating, and preview mode support. Added `PreviewOverlayControl.axaml` Avalonia UserControl with ghost text display and Accept/Reject buttons. Added `PreviewOverlayViewModel` with `[ObservableProperty]` bindings subscribed to `PreviewStateChanged` events and `RelayCommand` accept/reject actions. Extended `IEditorService` with `CaretOffset`, `InsertText(int, string)`, `DeleteText(int, int)`, `BeginUndoGroup`, `EndUndoGroup`, `ClearSelection`, `HasSelection`, `SelectionStart`, and `SelectionLength`. Added stub implementations in `EditorService.cs` delegating to active `IManuscriptViewModel`. Registered in `AgentsModule` DI. Includes 21 unit tests.

- **Document-Aware Prompting (v0.6.7c)** — Markdown AST-based document structure analysis in `Lexichord.Modules.Agents`. Added `IDocumentContextAnalyzer` interface with `AnalyzeAtPositionAsync`, `DetectContentType`, `GetCurrentSectionHeading`, `GetLocalContext`, and `InvalidateCache`. Implemented `DocumentContextAnalyzer` with Markdig AST parsing via `ASTCacheProvider` (`ConcurrentDictionary<string, MarkdownDocument>` cache with `GetOrParseAsync`, `GetCached`, `Invalidate`, `ClearAll`), content block type detection (9-value `ContentBlockType` enum: Prose, CodeBlock, Table, List, Heading, Blockquote, FrontMatter, InlineCode, Link), section heading discovery via `HeadingBlock` traversal with recursive inline text extraction, local context extraction (±500 chars around cursor), and agent suggestion based on content type and heading keywords. Added `DocumentContext` record with `DocumentPath`, `CursorPosition`, `ContentType`, `CurrentSection`, `LocalContext`, `SuggestedAgentId`, `EditorContext`, plus `Empty` static property and `HasSection`/`HasSuggestedAgent` computed properties. Added `EditorContext` record with `FromEditorService(IEditorService)` factory method. Added `PromptSuggestion` record for UI suggestion display. Added `ContextAwarePromptSelector` with `SelectPromptAsync` mapping content types to template IDs (`context-code-review`, `context-table-help`, `context-list-expand`, `context-general-improve`) and `GetSuggestionsAsync` returning context-specific `PromptSuggestion` lists. Added `DocumentChangedEventArgs` in Abstractions for document change notifications. Extended `IEditorService` with `CurrentDocumentPath`, `GetDocumentText()`, `CurrentLine`, `CurrentColumn`, and `DocumentChanged` event. Added stub implementations in `EditorService.cs`. Added Markdig 0.37.0 dependency. Registered `ASTCacheProvider`, `IDocumentContextAnalyzer`/`DocumentContextAnalyzer`, and `ContextAwarePromptSelector` as singletons in `AgentsModule` DI. Includes 16 unit tests.

- **Quick Actions Panel (v0.6.7d)** — Floating quick actions toolbar with one-click AI operations in `Lexichord.Modules.Agents`. Added `IQuickActionsService` interface with `AllActions`, `GetAvailableActionsAsync`, `ExecuteAsync`, `RegisterAction`, `UnregisterAction`, `ShouldShowPanel`, and `ActionExecuted` event. Implemented `QuickActionsService` with 8 constructor dependencies, built-in action registry (8 actions: Improve, Simplify, Expand, Summarize, Fix for prose; Explain, Comment for code; Add Row for tables), context-aware filtering by `ContentBlockType`/selection state/`LicenseTier` via `GetCurrentTier()`, two-tier template resolution (`IPromptTemplateRepository` first, `BuiltInQuickActions.PromptTemplates` fallback), agent invocation via `IAgentRegistry.GetDefaultAgent()`, and `QuickActionExecutedEvent` MediatR publishing. Added `QuickAction` record with 11 parameters (`ActionId`, `Name`, `Description`, `Icon`, `PromptTemplateId`, `AgentId?`, `KeyboardShortcut?`, `RequiresSelection`, `SupportedContentTypes?`, `MinimumLicenseTier`, `Order`) plus `AppliesTo(ContentBlockType)` and `AppliesToAllContentTypes` computed members. Added `PromptTemplateDefinition` record for lightweight built-in prompt definitions. Added `QuickActionResult` record (`Success`, `Text`, `ErrorMessage?`, `Duration`). Added `QuickActionExecutedEvent` `INotification` and `QuickActionExecutedEventArgs`. Added `BuiltInQuickActions` static class with `All` (8 actions), `PromptTemplates` (8 definitions), and `QuickActionPromptTemplateAdapter` implementing `IPromptTemplate`. Added `QuickActionsPanelViewModel` with debounced 500ms `SelectionChanged` handling, `ShowAsync`/`Hide`/`ExecuteActionAsync` methods, and `IEditorInsertionService.ShowPreviewAsync` integration. Added `QuickActionItemViewModel` with `[ObservableProperty] IsExecuting` and `[RelayCommand] ExecuteAsync`. Added `QuickActionsPanel.axaml` floating toolbar with horizontal action buttons and drop shadow. Registered `IQuickActionsService`/`QuickActionsService` (singleton) and `QuickActionsPanelViewModel` (transient) in `AgentsModule` DI. Includes 17 unit tests.

#### Sub-Part Changelogs

| Version                        | Title                    | Status      |
| ------------------------------ | ------------------------ | ----------- |
| v0.6.7a                        | Selection Context        | ✅ Complete |
| [v0.6.7b](v0.6.x/v0.6.7b.md) | Inline Suggestions       | ✅ Complete |
| [v0.6.7c](v0.6.x/v0.6.7c.md) | Document-Aware Prompting | ✅ Complete |
| [v0.6.7d](v0.6.x/v0.6.7d.md) | Quick Actions Panel      | ✅ Complete |

---

## [v0.6.6] - 2026-02 (In Progress)

### The Co-pilot Agent (Conversational Assistant)

This release introduces the Co-pilot Agent — a conversational AI writing assistant that integrates chat, document context, RAG, and style enforcement into a unified agent framework.

#### What's New

- **Agent Abstractions (v0.6.6a)** — Core agent abstraction layer in `Lexichord.Abstractions.Agents`. Added `IAgent` interface defining the contract for all AI-powered assistants with `AgentId`, `Name`, `Description`, `Template` (IPromptTemplate), `Capabilities` (AgentCapabilities), and `InvokeAsync(AgentRequest, CancellationToken)`. Added `AgentCapabilities` `[Flags]` enum with `Chat` (1), `DocumentContext` (2), `RAGContext` (4), `StyleEnforcement` (8), `Streaming` (16), `None` (0), and `All` (31), plus `AgentCapabilitiesExtensions` with `HasCapability()`, `SupportsContext()`, and `GetCapabilityNames()`. Added `AgentRequest` immutable record with `UserMessage`, `History`, `DocumentPath`, `Selection`, `Validate()` guard, and helper properties (`HasDocumentContext`, `HasSelection`, `HasHistory`, `HistoryCount`). Added `AgentResponse` immutable record with `Content`, `Citations`, `Usage`, `Empty` and `Error()` factories, and computed properties (`HasCitations`, `CitationCount`, `TotalTokens`). Added `UsageMetrics` immutable record with `PromptTokens`, `CompletionTokens`, `EstimatedCost`, `TotalTokens`, `Zero` sentinel, `Add()`, `ToDisplayString()`, and `Calculate()` factory. Includes 47 unit tests.

- **Co-Pilot Agent (v0.6.6b)** — First concrete agent implementation built on v0.6.6a abstractions. Added `CoPilotAgent` with 9 injected dependencies, 5 capability flags, license-based batch/streaming routing (WriterPro=batch, Teams+=streaming), graceful degradation on context failure, citation extraction from RAG results, and cost-based usage metrics. Added `AgentInvocationException` for wrapping agent failures and `ISettingsService` minimal settings interface. Registered as scoped `IAgent` via `AddCoPilotAgent()`. Includes 11 unit tests.

- **Agent Registry (v0.6.6c)** — Singleton agent discovery and management service. Added `IAgentRegistry` interface with `AvailableAgents`, `GetAgent`, `TryGetAgent`, `GetDefaultAgent`, `RegisterCustomAgent`, `UnregisterCustomAgent`, `Refresh`, and `AgentsChanged` event. Added `AgentListChangedEventArgs`, `AgentListChangeReason` enum, and 4 custom exceptions (`AgentNotFoundException`, `NoAgentAvailableException`, `AgentAlreadyRegisteredException`, `LicenseTierException`). Implemented `AgentRegistry` with DI-based agent discovery, `RequiresLicenseAttribute` reflection, `ConcurrentDictionary` thread safety, license-based filtering via `GetCurrentTier()`, default agent fallback chain (settings → co-pilot → first available), Teams-only custom agent registration with `ISettingsService` persistence, and automatic refresh on `LicenseChanged` events. Includes 11 unit tests.

- **Usage Tracking (v0.6.6d)** — Per-conversation and session-level usage tracking. Added `AgentInvocationEvent` MediatR notification, `UsageTracker` scoped service with conversation/session accumulation and event publishing, `SessionUsageCoordinator` singleton for cross-conversation session totals, `UsageRepository` with in-memory storage and CSV/JSON export, `UsageDisplayViewModel` with throttled 500ms UI updates and threshold-based states (Normal/Warning/Critical), and `AgentInvocationHandler` forwarding telemetry breadcrumbs via `ITelemetryService`. License-gated monthly summary and export (Teams only). Includes 15 unit tests.

- **Graph Context Provider (v0.6.6e)** — Knowledge graph context retrieval pipeline for Co-pilot prompt injection (CKVS Phase 3b). Added `IKnowledgeContextProvider` orchestrator with `GetContextAsync(query)` and `GetContextForEntitiesAsync(ids)`, `IEntityRelevanceRanker` with term-based scoring (name 3×, type 2×, properties 1×) and greedy token-budget selection, `IKnowledgeContextFormatter` with Markdown/YAML/JSON/Plain output and character-based token estimation (~4 chars/token). Added `EntitySearchQuery`, `KnowledgeContext`, `KnowledgeContextOptions` (with `ContextFormat` enum), and `RankedEntity` data records. Extended `IGraphRepository` with `SearchEntitiesAsync` (in-memory filtering; future Cypher full-text index). Pipeline: search → rank → select → enrich (relationships, axioms, claims) → format. Claims bounded to 5 entities × 3 claims for performance. Renamed spec's `IContextFormatter` to `IKnowledgeContextFormatter` to avoid collision with `Lexichord.Modules.Agents`. Registered Ranker/Formatter as singletons, Provider as scoped in `KnowledgeModule`. Includes 16 unit tests.

- **Pre-Generation Validator (v0.6.6f)** — Pre-generation validation of knowledge context and user requests before LLM generation (CKVS Phase 3b). Added `IPreGenerationValidator` with `ValidateAsync(AgentRequest, KnowledgeContext)` and `CanProceedAsync`, `IContextConsistencyChecker` with `CheckConsistency(KnowledgeContext)` and `CheckRequestConsistency(AgentRequest, KnowledgeContext)`. Added `PreValidationResult` record with `CanProceed`, `Issues`, computed `BlockingIssues`/`Warnings`, `SuggestedModifications`, `UserMessage`, and `Pass()`/`Block()` factories. Added `ContextIssue` record with `Code`, `Message`, `Severity`, optional `RelatedEntity`/`RelatedAxiom`/`Resolution`. Added `ContextIssueSeverity` enum (Error/Warning/Info), `ContextIssueCodes` with 8 `PREVAL_*` constants, `ContextModification` record, and `ContextModificationType` enum (AddEntity/RemoveEntity/UpdateEntity/RefreshContext). Implemented `PreGenerationValidator` orchestrating empty-context check → consistency → request alignment → axiom compliance → aggregation with graceful degradation on service failure. Implemented `ContextConsistencyChecker` with duplicate entity detection (case-insensitive), boolean property conflict detection, relationship endpoint validation, and request ambiguity checking. Adapted spec's `CopilotRequest` to `AgentRequest` (v0.6.6a) and `AxiomSeverity.Must` to `AxiomSeverity.Error`. Includes 22 unit tests.

- **Post-Generation Validator (v0.6.6g)** — Post-generation validation of LLM content against the Knowledge Graph (CKVS Phase 3b). Added `IPostGenerationValidator` with `ValidateAsync(content, context, request)` and `ValidateAndFixAsync`, `IHallucinationDetector` with `DetectAsync(content, context)`. Added `PostValidationResult` record with `IsValid`, `Status`, `Findings`, `Hallucinations`, `SuggestedFixes`, `CorrectedContent`, `VerifiedEntities`, `ExtractedClaims`, `ValidationScore`, `UserMessage`, and `Valid()`/`Inconclusive()` factories. Added `HallucinationFinding` record with `ClaimText`, `Location`, `Confidence`, `Type`, `SuggestedCorrection`. Added `PostValidationStatus` enum (Valid/ValidWithWarnings/Invalid/Inconclusive), `HallucinationType` enum (UnknownEntity/ContradictoryValue/UnsupportedRelationship/UnverifiableFact), `ValidationFix` record (Description/ReplaceSpan/ReplacementText/Confidence/CanAutoApply). Implemented `PostGenerationValidator` orchestrating entity verification → claim extraction → validation → hallucination detection → score computation → fix generation with graceful degradation. Implemented `HallucinationDetector` with regex-based contradiction detection and Levenshtein distance matching. Adapted spec's `CopilotRequest` to `AgentRequest` (v0.6.6a), `IEntityLinkingService`/`IEntityRecognizer` to direct name matching, `ValidateClaimsAsync` to `ValidateDocumentAsync`. Includes 21 unit tests.

- **Entity Citation Renderer (v0.6.6h)** — Knowledge Graph citation transparency layer for Co-pilot responses (CKVS Phase 3b). Added `IEntityCitationRenderer` with `GenerateCitations(ValidatedGenerationResult, CitationOptions)` and `GetCitationDetail(KnowledgeEntity, ValidatedGenerationResult)`. Added `ValidatedGenerationResult` record aggregating `Content`, `SourceEntities`, and `PostValidation`. Added `CitationMarkup` record with `Citations`, `ValidationStatus`, `Icon`, `FormattedMarkup`. Added `EntityCitation` record with `EntityId`, `EntityType`, `EntityName`, `DisplayLabel`, `Confidence`, `IsVerified`, `TypeIcon`. Added `CitationOptions` record with `Format`, `MaxCitations`, `ShowValidationStatus`, `ShowConfidence`, `GroupByType`. Added `EntityCitationDetail` record with `Entity`, `UsedProperties`, `CitedRelationships`, `DerivedClaims`, `BrowserLink`. Added `ValidationIcon` enum (CheckMark/Warning/Error/Question) and `CitationFormat` enum (Compact/Detailed/TreeView/Inline). Implemented `EntityCitationRenderer` with type icon mapping (Endpoint→🔗, Parameter→📝, Response→📤, Schema→📋, Entity→📦, Error→⚠️), entity verification via validation findings, display label formatting, and Compact/Detailed/TreeView output formats. Added `CitationPanel.axaml` Avalonia UserControl, `CitationViewModel` with `[ObservableProperty]` bindings, and `CitationItemViewModel`. Adapted spec's WPF XAML to Avalonia, `ValidationStatus` to `PostValidationStatus`, `Brush` to `IBrush`. Includes 22 unit tests.

- **Knowledge-Aware Prompts (v0.6.6i)** — Template-based prompt construction with Knowledge Graph context injection (CKVS Phase 3b). Added `IKnowledgePromptBuilder` with `BuildPrompt(AgentRequest, KnowledgeContext, PromptOptions)`, `GetTemplates()`, `RegisterTemplate(KnowledgePromptTemplate)`. Added `KnowledgePrompt` record with `SystemPrompt`, `UserPrompt`, `EstimatedTokens`, `TemplateId`, `IncludedEntityIds`, `IncludedAxiomIds`. Added `KnowledgePromptTemplate` record with `Id`, `Name`, `Description`, `SystemTemplate`, `UserTemplate`, `DefaultOptions`, `Requirements`. Added `PromptRequirements` record with `RequiresEntities`, `RequiresRelationships`, `RequiresAxioms`, `RequiresClaims`, `MinEntities`. Added `PromptOptions` record with `TemplateId`, `MaxContextTokens`, `IncludeAxioms`, `IncludeRelationships`, `ContextFormat`, `GroundingLevel`, `AdditionalInstructions`. Added `GroundingLevel` enum (Strict/Moderate/Flexible). Implemented `KnowledgePromptBuilder` with 3 built-in templates (copilot-knowledge-aware, copilot-strict, copilot-documentation), entity/axiom/relationship formatting, grounding instructions, and Mustache rendering via `IPromptRenderer`. Adapted spec's `CopilotRequest` to `AgentRequest`, Handlebars to Mustache, `PromptTemplate` to `KnowledgePromptTemplate`. Includes 28 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                     | Status      |
| --------------------------------- | ------------------------- | ----------- |
| [v0.6.6a](v0.6.x/LCS-CL-v066a.md) | Agent Abstractions        | ✅ Complete |
| [v0.6.6b](v0.6.x/LCS-CL-v066b.md) | Co-Pilot Agent            | ✅ Complete |
| [v0.6.6c](v0.6.x/LCS-CL-v066c.md) | Agent Registry            | ✅ Complete |
| [v0.6.6d](v0.6.x/LCS-CL-v066d.md) | Usage Tracking            | ✅ Complete |
| [v0.6.6e](v0.6.x/LCS-CL-v066e.md) | Graph Context Provider    | ✅ Complete |
| [v0.6.6f](v0.6.x/LCS-CL-v066f.md) | Pre-Generation Validator  | ✅ Complete |
| [v0.6.6g](v0.6.x/LCS-CL-v066g.md) | Post-Generation Validator | ✅ Complete |
| [v0.6.6h](v0.6.x/LCS-CL-v066h.md) | Entity Citation Renderer  | ✅ Complete |
| [v0.6.6i](v0.6.x/LCS-CL-v066i.md) | Knowledge-Aware Prompts   | ✅ Complete |

---

## [v0.6.5] - 2026-02 (In Progress)

### The Stream (Real-Time Streaming)

This release delivers real-time streaming LLM responses, enabling token-by-token display with progressive UI updates, cancellation, and error recovery.

#### What's New

- **Streaming Token Model (v0.6.5a)** — Core data contracts for streaming LLM responses in `Lexichord.Modules.Agents`. Added `StreamingChatToken` immutable record with `Text`, `Index`, `IsComplete`, and `FinishReason` properties, plus `Content(text, index)` and `Complete(index, finishReason)` factory methods and `HasContent` computed property. Added `StreamingState` enum with 6 lifecycle states (`Idle`, `Connecting`, `Streaming`, `Completed`, `Cancelled`, `Error`) and `StreamingStateExtensions` with 5 methods (`IsActive`, `IsTerminal`, `CanCancel`, `ShowTypingIndicator`, `InputEnabled`) for UI binding. Added `IStreamingChatHandler` interface defining the streaming callback contract with `OnTokenReceived(StreamingChatToken)`, `OnStreamComplete(ChatResponse)`, and `OnStreamError(Exception)`. Includes 9 unit tests.

- **SSE Parser (v0.6.5b)** — Server-Sent Events parser for extracting streaming tokens from LLM provider HTTP response streams in `Lexichord.Modules.Agents`. Added `ISSEParser` interface with `ParseSSEStreamAsync(Stream, string, CancellationToken)` returning `IAsyncEnumerable<StreamingChatToken>`. Implemented `SSEParser` sealed class as stateless, thread-safe singleton with provider-dispatched JSON parsing: OpenAI format (`choices[0].delta.content` with `[DONE]` termination) and Anthropic format (`content_block_delta` events with `message_stop` termination). Features malformed JSON resilience (logged at Warning, skipped), case-insensitive provider matching, `ReadOnlySpan` prefix slicing, `Stopwatch`-based completion metrics, and comprehensive guard clauses (`ArgumentNullException` for null stream, `NotSupportedException` for unknown providers). Registered as `ISSEParser` → `SSEParser` singleton in `AgentsModule`. Includes 16 unit tests (36 with Theory expansion).

- **Streaming UI Handler (v0.6.5c)** — Progressive display handler for LLM-generated tokens in the Co-Pilot chat panel in `Lexichord.Modules.Agents`. Added `StreamingChatHandler` sealed class implementing `IStreamingChatHandler` and `IDisposable` with `StringBuilder`-based token buffering, 50ms throttled `System.Threading.Timer` (~20 UI updates/sec), `MaxBufferTokens=20` forced-flush threshold, and injectable `DispatchAction` for Avalonia UI thread dispatch (overridable for testing). Manages full stream lifecycle: `StartStreamingAsync()` creates placeholder message and sets `Connecting` state, `OnTokenReceived()` buffers tokens and transitions to `Streaming` on first token, `OnStreamComplete()` finalizes message with completion metadata, `OnStreamError()` preserves partial content and marks error state. Added minimal `CoPilotViewModel` and `ChatMessageViewModel` stubs as v0.6.4 forward declarations with streaming properties. Added `CoPilotView.axaml` with animated typing indicator (3 staggered-opacity ellipses) and cancel button, plus `CoPilotView.axaml.cs` code-behind wiring `ScrollToBottomRequested` to `ScrollViewer.ScrollToEnd()`. Includes 11 unit tests.

- **License Gating (v0.6.5d)** — License-based feature gating for the streaming chat interface. Routes requests based on `ILicenseContext` tier: Teams/Enterprise authorized for real-time streaming, WriterPro falls back to batch completion. Added `ShouldUseStreaming()` routing, `SendBatchAsync` fallback, UI upgrade hint, and parameterless backward-compatible constructor. Includes 9 license gating tests.

- **Validation Orchestrator (v0.6.5e)** — Pluggable, parallel document validation engine for the Knowledge Graph module (CKVS Phase 3a). Added `ValidationMode` flags enum, `ValidationSeverity` enum, `ValidationFinding` record with factory methods, `ValidationResult` aggregated result record (follows `AxiomValidationResult` pattern), `ValidationOptions` config record, `ValidationContext` document context record, `IValidator` pluggable interface, and `IValidationEngine` public orchestrator interface. Implemented `ValidatorRegistry` (thread-safe `ConcurrentDictionary`, mode filtering, license-tier gating, priority ordering), `ValidationPipeline` (parallel execution via `Task.WhenAll`, per-validator timeout, exception isolation), and `ValidationEngine` orchestrator. Registered as singletons in `KnowledgeModule`. Includes 42 unit tests.

- **Schema Validator (v0.6.5f)** — Schema-based entity validation bridging the v0.6.5e validation pipeline with v0.4.5f schema logic (CKVS Phase 3a). Added `SchemaFindingCodes` static constants (11 `SCHEMA_*` codes), `IPropertyTypeChecker` interface with `TypeCheckResult` record, `IConstraintEvaluator` interface, and `ISchemaValidatorService` extending `IValidator`. Implemented `PropertyTypeChecker` (CLR type mapping for String/Number/Boolean/DateTime/Enum/Reference/Array), `ConstraintEvaluator` (numeric range, string length, regex pattern checks), `SchemaValidatorService` (entity extraction from context metadata, required property validation, type/enum/constraint checking, Levenshtein-based enum fix suggestions), and `PredefinedSchemas` (built-in Endpoint and Parameter schemas). Registered in `KnowledgeModule` DI. Includes ~74 unit tests.

- **Axiom Validator (v0.6.5g)** — Axiom-based entity validation bridging the v0.6.5e validation pipeline with v0.4.6h axiom evaluation (CKVS Phase 3a). Added `AxiomFindingCodes` static constants (12 `AXIOM_*` codes) and `IAxiomValidatorService` extending `IValidator` with entity-level validation and axiom query methods. Implemented `AxiomMatcher` (static helper filtering axioms by `IsEnabled`, `TargetKind`, and `TargetType`, ordered by severity) and `AxiomValidatorService` (entity extraction from context metadata, axiom matching, `IAxiomEvaluator` delegation, `AxiomViolation` → `ValidationFinding` conversion with severity and constraint-to-code mapping). Registered in `KnowledgeModule` DI. Includes 21 unit tests.

- **Consistency Checker (v0.6.5h)** — Claim consistency validation detecting contradictions between new claims and existing knowledge (CKVS Phase 3a). Added `ConsistencyFindingCodes` static constants (7 `CONSISTENCY_*` codes), `IConflictDetector` interface with `ConflictResult` record and `ConflictType` enum (7 values: `ValueContradiction`, `PropertyConflict`, `RelationshipContradiction`, `TemporalConflict`, `SemanticConflict`, `TypeMismatch`, `Unknown`), `IContradictionResolver` interface with `ConflictResolution` record and `ResolutionStrategy` enum (6 values: `AcceptNew`, `KeepExisting`, `MergeValues`, `DeferToUser`, `AcceptHigherConfidence`, `ManualReview`), and `IConsistencyChecker` extending `IValidator` with `CheckClaimConsistencyAsync`, `CheckClaimsConsistencyAsync`, and `GetPotentialConflictsAsync` methods. Added `ConsistencyFinding` record extending `ValidationFinding` with conflict metadata (`ExistingClaim`, `ConflictType`, `ConflictConfidence`, `Resolution`). Implemented `ConflictDetector` (subject/predicate/object structural comparison with numeric equality tolerance), `ContradictionResolver` (strategy routing by conflict type with temporal, version, and source-based resolution), and `ConsistencyChecker` (claim extraction from `ValidationContext.Metadata`, `IClaimRepository` querying, internal batch consistency, severity mapping by confidence threshold >0.8=Error/≤0.8=Warning). Registered in `KnowledgeModule` DI. Includes 39 unit tests.

- **Validation Result Aggregator (v0.6.5i)** — Final aggregation stage for the CKVS validation pipeline (CKVS Phase 3a). Added `FindingFilter` record (MinSeverity, ValidatorIds, Codes, FixableOnly criteria), `FindingGroupBy` enum (Validator, Severity, Code), `ConsolidatedFix` record with `TextEdit` for span-based edits, `FixAllAction` batch fix record with warnings, `ValidationSummary` statistics record, `IResultAggregator` interface (Aggregate, FilterFindings, GroupFindings), `IFindingDeduplicator` interface (Deduplicate, AreDuplicates), and `IFixConsolidator` interface (ConsolidateFixes, CreateFixAllAction). Implemented `ResultAggregator` (dedup → severity sort → MaxFindings limit → ValidationResult.WithFindings), `FindingDeduplicator` (O(n²) comparison by Code+ValidatorId+PropertyPath/Message similarity), `FixConsolidator` (grouping by identical SuggestedFix text), and `ValidationSummaryGenerator` static class. Registered in `KnowledgeModule` DI. Includes 38 unit tests.

- **Linter Integration (v0.6.5j)** — Bridges the CKVS Validation Engine (v0.6.5e) with Lexichord's style linter (v0.3.x), providing a unified findings view (CKVS Phase 3a). Added `FindingSource`, `UnifiedSeverity`, `FindingCategory`, `UnifiedStatus` enums, `UnifiedFinding` record (bridging `ValidationFinding`/`StyleViolation`), `UnifiedFix` record, `UnifiedFindingResult` (with `ByCategory`/`BySeverity` summaries), `UnifiedFindingOptions`, `FixConflict`/`FixConflictResult`, `FixApplicationResult` records, and `ILinterIntegration`, `IUnifiedFindingAdapter`, `ICombinedFixWorkflow` interfaces (13 abstraction files). Implemented `UnifiedFindingAdapter` (severity normalization, ValidatorId→Category mapping, fix creation with source-specific confidence), `CombinedFixWorkflow` (FindingId conflict detection, deterministic ordering), and `LinterIntegration` (parallel `Task.WhenAll` orchestration, severity/category filtering, sorting, maxFindings cap, error-resilient partial results). Registered as singletons in `KnowledgeModule`. Includes 38 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                        | Status      |
| --------------------------------- | ---------------------------- | ----------- |
| [v0.6.5a](v0.6.x/LCS-CL-v065a.md) | Streaming Token Model        | ✅ Complete |
| [v0.6.5b](v0.6.x/LCS-CL-v065b.md) | SSE Parser                   | ✅ Complete |
| [v0.6.5c](v0.6.x/LCS-CL-v065c.md) | Streaming UI Handler         | ✅ Complete |
| [v0.6.5d](v0.6.x/LCS-CL-v065d.md) | License Gating               | ✅ Complete |
| [v0.6.5e](v0.6.x/LCS-CL-v065e.md) | Validation Orchestrator      | ✅ Complete |
| [v0.6.5f](v0.6.x/LCS-CL-v065f.md) | Schema Validator             | ✅ Complete |
| [v0.6.5g](v0.6.x/LCS-CL-v065g.md) | Axiom Validator              | ✅ Complete |
| [v0.6.5h](v0.6.x/LCS-CL-v065h.md) | Consistency Checker          | ✅ Complete |
| [v0.6.5i](v0.6.x/LCS-CL-v065i.md) | Validation Result Aggregator | ✅ Complete |
| [v0.6.5j](v0.6.x/LCS-CL-v065j.md) | Linter Integration           | ✅ Complete |

---

## [v0.6.4] - 2026-02 (In Progress)

### The Dashboards (P1 UI Components)

This release delivers two critical P1 UI components: the Knowledge Hub unified dashboard for RAG functionality, and the Issues by Category bar chart for style violation visualization.

#### What's New

- **Knowledge Hub Dashboard (v0.6.4a)** — Unified dashboard view for all RAG functionality in `Lexichord.Modules.RAG`. Added `IKnowledgeHubViewModel` interface with index statistics (`TotalDocuments`, `TotalChunks`, `IndexedDocuments`, `PendingDocuments`, `StorageSizeBytes`, `LastIndexedAt`), recent search history (limited to 10 entries), indexing progress tracking (0-100%), and re-index all command. Created `KnowledgeHubStatistics` record with computed properties (`FormattedStorageSize` for human-readable B/KB/MB/GB, `IndexingProgress` percentage, `HasIndexedContent` flag). Created `RecentSearch` record with `RelativeTime` property for display ("just now", "5 min ago", "3 hr ago"). Implemented `KnowledgeHubViewModel` with async initialization, MediatR `DocumentIndexedEvent` handling for real-time statistics updates, and license gating for WriterPro tier. Created `KnowledgeHubView.axaml` with two-column layout: statistics sidebar (280px) with cards, quick actions, and recent searches; main content area for embedded search interface. Features 15 structured log events (2000-2014 range). Includes ~24 unit tests.

- **Issues by Category Bar Chart (v0.6.4b)** — Horizontal bar chart visualization of style violations grouped by category in `Lexichord.Modules.Style`. Added `IIssueCategoryChartViewModel` interface with category collection (`Categories`), total issue count, category selection event, and manual refresh. Created `IssueCategoryData` record with predefined category colors (Terminology=#F87171, Passive Voice=#FBBF24, Sentence Length=#60A5FA, Readability=#34D399, Grammar=#A78BFA, Style=#FB923C, Structure=#2DD4BF, Custom=#94A3B8). Implemented `IssueCategoryChartViewModel` with MediatR `LintingCompletedEvent` handling, category inference from rule IDs (pattern matching for term/passive/sentence/read/grammar/struct), sorted aggregation by count descending, and LiveCharts2 `RowSeries` for horizontal bars. Created `IssueCategoryChartView.axaml` with header (section title + total badge), loading indicator, empty state ("No issues found"), and LiveCharts2 `CartesianChart`. Features 8 structured log events (2100-2107 range). Includes ~20 unit tests.

- **Conversation Management (v0.6.4c)** — Core conversation lifecycle management service in `Lexichord.Modules.Agents`. Added `IConversationManager` interface with 15 methods for conversation creation, message management, history tracking, search, and Markdown export. Created `Conversation` immutable record with factory methods (`Empty`, `WithMetadata`) and 8 computed properties (`MessageCount`, `HasMessages`, `UserMessageCount`, `AssistantMessageCount`, `TotalCharacterCount`, `FirstMessage`, `LastMessage`, `MatchesSearch`). Created `ConversationMetadata` record with factory methods (`Default`, `ForDocument`, `ForModel`) and helper methods (`WithTokens`, `AddTokens`) for tracking document path, selected model, and token count. Added `ConversationChangedEventArgs` with 8 event types (Created, MessageAdded, MessagesAdded, Cleared, TitleChanged, Truncated, Switched, Deleted). Created `ConversationSearchResult` record for search results with highlighted snippets. Created `ConversationSummaryExportOptions` record with 3 presets (Default, Minimal, Full). Implemented `ConversationManager` with automatic title generation from first user message (truncated at 50 chars), history truncation at 50 messages (removes oldest), recent conversation tracking (up to 10, ordered by recency), full-text search across current and recent conversations with context snippets, and Markdown export with emoji role indicators (👤 User, 🤖 Assistant, ⚙️ System). Registered `IConversationManager` as scoped service via `AddConversationManagement()` extension. Includes 60+ unit tests across 3 test classes.

#### Sub-Part Changelogs

| Version                           | Title                        | Status      |
| --------------------------------- | ---------------------------- | ----------- |
| [v0.6.4a](v0.6.x/LCS-CL-v064a.md) | Knowledge Hub Dashboard      | ✅ Complete |
| [v0.6.4b](v0.6.x/LCS-CL-v064b.md) | Issues by Category Bar Chart | ✅ Complete |
| [v0.6.4c](v0.6.x/LCS-CL-v064c.md) | Conversation Management      | ✅ Complete |

---

## [v0.6.3] - 2026-02 (In Progress)

### The Templates (Prompt Engineering)

This release delivers the prompt templating infrastructure that enables structured, reusable AI prompts with variable substitution, validation, and context assembly.

#### What's New

- **Template Abstractions (v0.6.3a)** — Core abstractions for the prompt templating system in `Lexichord.Abstractions`. Added `IPromptTemplate` interface defining reusable prompt templates with system/user prompts, required/optional variables, and kebab-case template IDs. Added `IPromptRenderer` interface with `Render` for string substitution, `RenderMessages` for ChatMessage[] production, and `ValidateVariables` for pre-flight validation. Added `IContextInjector` interface for async context assembly from style rules, RAG context, and document state. Created `PromptTemplate` immutable record implementing `IPromptTemplate` with `Create` factory method, `AllVariables`/`VariableCount`/`HasVariable` computed properties. Added `RenderedPrompt` record with `EstimatedTokens` (4 chars/token approximation), `TotalCharacters`, and `WasFastRender` (under 10ms) computed properties. Added `TemplateVariable` record with `Required`/`Optional` factory methods and `HasDefaultValue`/`HasDescription` computed properties. Added `ValidationResult` record with `Success`/`WithWarnings`/`Failure` factory methods, `ThrowIfInvalid(templateId)`, `ErrorMessage`, and `HasWarnings`/`HasMissingVariables` computed properties. Added `ContextRequest` record with `ForUserInput`/`Full`/`StyleOnly`/`RAGOnly` factory methods, `MaxRAGChunks` default of 3, and `HasContextSources`/`HasDocumentContext`/`HasSelectedText`/`HasCursorPosition` computed properties. Added `TemplateValidationException` with `TemplateId` and `MissingVariables` properties for validation failure reporting. No external NuGet dependencies. Includes 85+ unit tests.

- **Mustache Renderer (v0.6.3b)** — Implemented `MustachePromptRenderer` in new `Lexichord.Modules.Agents` module using Stubble.Core 1.10.8. Implements `IPromptRenderer` interface with full Mustache specification support: variable substitution (`{{variable}}`), sections (`{{#section}}...{{/section}}`), inverted sections (`{{^inverted}}...{{/inverted}}`), raw output (`{{{raw}}}`), list iteration (`{{#items}}{{.}}{{/items}}`), and nested object access (`{{user.name}}`). Added `MustacheRendererOptions` configuration record with `Default`/`Strict`/`Lenient` presets controlling case sensitivity, validation behavior, and performance thresholds. Created `AgentsModule` implementing `IModule` for automatic discovery and service registration. Added `AddMustacheRenderer()` DI extension methods for singleton registration. Features thread-safe design for concurrent usage, HTML escaping disabled by default for prompt content, configurable case-insensitive variable lookup (default: enabled), comprehensive logging at Debug/Information levels. Includes 39 unit tests.

- **Template Repository (v0.6.3c)** — Implemented `IPromptTemplateRepository` for managing prompt templates from multiple sources with priority-based loading. Added `TemplateSource` enum (Embedded, Global, User) for priority ordering, `TemplateInfo` record for template metadata, `TemplateChangedEventArgs` for change notifications. Implemented `PromptTemplateRepository` with thread-safe `ConcurrentDictionary` cache, priority-based template override (User > Global > Embedded), hot-reload via `IFileSystemWatcher` integration, and license gating (custom templates at WriterPro+, hot-reload at Teams+). Created `PromptTemplateLoader` for YAML template parsing using YamlDotNet 16.3.0 with `UnderscoredNamingConvention`. Added 5 built-in YAML templates: `co-pilot-editor` (editing), `document-reviewer` (review), `summarizer` (analysis), `style-checker` (linting), `translator` (translation). Added `PromptTemplateOptions` configuration with `EnableBuiltInTemplates`, `EnableHotReload`, `FileWatcherDebounceMs`, and platform-specific default paths. Added `AddTemplateRepository()` DI extension methods. Includes 74 unit tests.

- **Context Injection Service (v0.6.3d)** — Implemented `IContextInjector` for automatic context assembly from multiple sources with parallel provider execution. Added `IContextProvider` interface defining the contract for context providers with `ProviderName`, `Priority`, `RequiredLicenseFeature`, `IsEnabled(request)`, and `GetContextAsync(request, ct)` methods. Created `ContextResult` record with `Ok`/`Empty`/`Error`/`Timeout` factory methods and `VariableCount`/`HasData`/`IsTimeout` computed properties. Implemented three context providers: `DocumentContextProvider` (priority 50, produces `document_path`/`document_name`/`document_extension`/`cursor_position`/`selected_text`/`selection_length`/`selection_word_count`), `StyleRulesContextProvider` (priority 100, produces `style_rules`/`style_rule_count` from `IStyleEngine.GetActiveStyleSheet().GetEnabledRules()`), and `RAGContextProvider` (priority 200, produces `context`/`context_source_count`/`context_sources` from `ISemanticSearchService`, requires `Feature.RAGContext` license). Added `IContextFormatter` interface with `FormatStyleRules` and `FormatRAGChunks` methods. Implemented `DefaultContextFormatter` with bullet-list style rules and source-attributed RAG chunks. Created `ContextInjectorOptions` configuration record with `Default`/`Fast`/`Thorough` presets controlling `RAGTimeoutMs` (default 5000), `ProviderTimeoutMs` (default 2000), `MaxStyleRules` (default 20), `MaxChunkLength` (default 1000), and `MinRAGRelevanceScore` (default 0.5). Implemented `ContextInjector` orchestrating parallel provider execution via `Task.WhenAll()`, individual provider timeouts via linked `CancellationTokenSource`, license feature checking via `ILicenseContext.IsFeatureEnabled()`, and priority-ordered result merging. Added `AddContextInjection()` DI extension methods and updated `AgentsModule` registration. Added `Feature.RAGContext` to `FeatureCodes.cs`. Includes ~50 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                     | Status      |
| --------------------------------- | ------------------------- | ----------- |
| [v0.6.3a](v0.6.x/LCS-CL-v063a.md) | Template Abstractions     | ✅ Complete |
| [v0.6.3b](v0.6.x/LCS-CL-v063b.md) | Mustache Renderer         | ✅ Complete |
| [v0.6.3c](v0.6.x/LCS-CL-v063c.md) | Template Repository       | ✅ Complete |
| [v0.6.3d](v0.6.x/LCS-CL-v063d.md) | Context Injection Service | ✅ Complete |

---

## [v0.6.2] - 2026-02 (In Progress)

### The Providers (LLM Integrations)

This release delivers production-ready LLM provider integrations, implementing the `IChatCompletionService` interface defined in v0.6.1 with proper error handling, retry logic, and streaming support.

#### What's New

- **OpenAI Connector (v0.6.2a)** — Production-ready OpenAI Chat Completions API integration. Implemented `OpenAIChatCompletionService` with `CompleteAsync` for synchronous completion and `StreamAsync` for SSE-based streaming responses. Added `OpenAIOptions` configuration record with VaultKey pattern, HttpClientName, and SupportedModels (gpt-4o, gpt-4o-mini, gpt-4-turbo, gpt-3.5-turbo). Created `OpenAIResponseParser` for parsing success responses, streaming chunks, and error responses with proper exception mapping (401→AuthenticationException, 429→RateLimitException with RetryAfter support, 5xx→ChatCompletionException). Added `OpenAIServiceCollectionExtensions` with Polly resilience policies (exponential backoff retry with Retry-After header support, circuit breaker with 5-failure threshold and 30-second break). Uses existing `OpenAIParameterMapper.ToRequestBody()` for request building and `SseParser.ParseStreamAsync()` for streaming. Includes 16 new structured log events (1600-1615 range). Includes 54 unit tests.

- **Anthropic Connector (v0.6.2b)** — Production-ready Anthropic Messages API integration for Claude models. Implemented `AnthropicChatCompletionService` with `CompleteAsync` for synchronous completion and `StreamAsync` for SSE-based streaming with Anthropic-specific event types (message_start, content_block_delta, message_delta, message_stop, ping, error). Added `AnthropicOptions` configuration record with VaultKey ("anthropic:api-key"), HttpClientName ("Anthropic"), ApiVersion ("2024-01-01"), and SupportedModels (claude-3-5-sonnet-20241022, claude-3-opus-20240229, claude-3-sonnet-20240229, claude-3-haiku-20240307). Created `AnthropicResponseParser` for parsing content blocks array responses, streaming events, and error responses with proper exception mapping (401/403→AuthenticationException, rate_limit_error/429→RateLimitException, overloaded_error→ChatCompletionException, 5xx→ChatCompletionException). Added `AnthropicServiceCollectionExtensions` with Polly resilience policies (exponential backoff retry with jitter + Retry-After header support, handles 429 and Anthropic-specific 529 overloaded status, circuit breaker with 5-failure threshold and 30-second break). Uses Anthropic-specific authentication (`x-api-key` header, not Bearer) and version header (`anthropic-version`). Uses existing `AnthropicParameterMapper.ToRequestBody()` for request building with system message separation. Includes 16 new structured log events (1700-1715 range). Includes 62 unit tests.

- **Retry Policy Implementation (v0.6.2c)** — Centralized Polly-based resilience infrastructure for LLM providers. Added `ResilienceOptions` configuration record with 8 tunable parameters (RetryCount, RetryBaseDelaySeconds, RetryMaxDelaySeconds, CircuitBreakerThreshold, CircuitBreakerDurationSeconds, TimeoutSeconds, BulkheadMaxConcurrency, BulkheadMaxQueue) and 3 static presets (Default, Aggressive, Minimal). Created `IResiliencePipeline` interface with `ExecuteAsync`, `CircuitState` property, and `OnPolicyEvent` for telemetry. Implemented `LLMResiliencePipeline` wrapping 4 policy layers: Bulkhead (concurrency limiting), Timeout (per-request limits), Circuit Breaker (fail-fast during outages), and Retry (transient error recovery with exponential backoff + jitter). Added `ResiliencePolicyBuilder` for constructing policies with logging callbacks. Created `ResilienceTelemetry` for metrics collection (success/failure counts, retry counts, circuit breaker opens/resets, latency percentiles P50/P90/P99). Added `LLMCircuitBreakerHealthCheck` implementing `IHealthCheck` (Healthy=Closed, Degraded=HalfOpen, Unhealthy=Open/Isolated). Refactored `OpenAIServiceCollectionExtensions` and `AnthropicServiceCollectionExtensions` to use centralized `AddLLMResiliencePolicies()` extension. Includes 19 new structured log events (1800-1819 range). Includes ~160 unit tests.

- **Token Counting Service (v0.6.2d)** — Comprehensive token counting service with model-specific tokenization. Added `ILLMTokenCounter` interface for text/message token counting, response estimation (60% of max tokens heuristic), context window queries, cost estimation, and tokenizer accuracy check. Implemented `LLMTokenCounter` with internal abstraction layer: `ITokenizer` interface, `MlTokenizerWrapper` using Microsoft.ML.Tokenizers for exact GPT tokenization (o200k_base for GPT-4o, cl100k_base for GPT-4/3.5), and `ApproximateTokenizer` for Claude models (~4 chars/token). Added `TokenizerFactory` for model-specific tokenizer creation and `TokenizerCache` with thread-safe `ConcurrentDictionary<string, Lazy<ITokenizer>>` for efficient instance reuse. Created `ModelTokenLimits` static class with context windows and pricing data for 9 models (GPT-4o, GPT-4o-mini, GPT-4-turbo, GPT-4, GPT-3.5-turbo, Claude 3.5 Sonnet, Claude 3 Opus, Claude 3 Sonnet, Claude 3 Haiku). Added `AddTokenCounting()` DI extension. Includes 13 new structured log events (1900-1912 range). Includes ~104 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                       | Status      |
| --------------------------------- | --------------------------- | ----------- |
| [v0.6.2a](v0.6.x/LCS-CL-v062a.md) | OpenAI Connector            | ✅ Complete |
| [v0.6.2b](v0.6.x/LCS-CL-v062b.md) | Anthropic Connector         | ✅ Complete |
| [v0.6.2c](v0.6.x/LCS-CL-v062c.md) | Retry Policy Implementation | ✅ Complete |
| [v0.6.2d](v0.6.x/LCS-CL-v062d.md) | Token Counting Service      | ✅ Complete |

---

## [v0.6.1] - 2026-02 (In Progress)

### The Gateway (LLM Abstractions)

This release establishes the foundational abstraction layer for Large Language Model (LLM) communication, enabling provider-agnostic integration with various AI providers (OpenAI, Anthropic, etc.).

#### What's New

- **Chat Completion Abstractions (v0.6.1a)** — Core data contracts and interfaces for LLM communication. Added `ChatRole` enum, `ChatMessage`/`ChatOptions`/`ChatRequest`/`ChatResponse`/`StreamingChatToken` immutable records with factory methods, presets, and fluent APIs. Added `IChatCompletionService` interface for provider implementations, `ChatRequestBuilder` for complex request construction, `ChatSerialization` for JSON utilities, `SseParser` for streaming SSE support, and exception hierarchy (`ChatCompletionException`, `AuthenticationException`, `RateLimitException`, `ProviderNotConfiguredException`). Added `ChatRoleExtensions` for provider-specific role mapping. Includes 135 unit tests.

- **Chat Options Model (v0.6.1b)** — Extended chat options with FluentValidation, model discovery, token estimation, and provider-specific mapping. Added 5 new presets (`CodeGeneration`, `Conversational`, `Summarization`, `Editing`, `Brainstorming`) to `ChatOptions`. Added `ChatOptionsValidator` with FluentValidation rules, `ChatOptionsValidationException` and `ContextWindowExceededException`. Added `IModelProvider` interface and `ModelInfo`/`TokenEstimate` records for model discovery. Created new `Lexichord.Modules.LLM` module with `LLMOptions`/`ProviderOptions`/`ChatOptionsDefaults` configuration classes, `ModelDefaults` static model registry, `ModelRegistry` caching service, `TokenEstimator` for context window management, `ChatOptionsResolver` resolution pipeline, `OpenAIParameterMapper`/`AnthropicParameterMapper` provider mappers, and `ProviderAwareChatOptionsValidator` for provider-specific validation. Includes structured logging via `LLMLogEvents`. Includes 89 unit tests.

- **Provider Registry (v0.6.1c)** — Central management for LLM provider instances and selection. Added `ILLMProviderRegistry` interface with provider discovery (`AvailableProviders`), resolution (`GetProvider`, `GetDefaultProvider`), configuration (`SetDefaultProvider`, `IsProviderConfigured`). Added `LLMProviderInfo` record with factory methods (`Unconfigured`, `Create`) and helper methods (`SupportsModel`, `WithConfigurationStatus`, `WithModels`). Added `ProviderNotFoundException` exception for registry-level errors. Implemented `LLMProviderRegistry` with thread-safe `ConcurrentDictionary`, .NET 8+ keyed services for provider resolution, `ISystemSettingsRepository` for default persistence, `ISecureVault` for API key checking, and case-insensitive name matching. Added `AddLLMProviderRegistry()` and `AddChatCompletionProvider<T>()` DI extensions. Includes 16 new structured log events (1400-1415 range). Includes 72 unit tests.

- **API Key Management UI (v0.6.1d)** — Settings dialog integration for LLM provider configuration. Added `ConnectionStatus` enum (Unknown, Checking, Connected, Failed) for connection state tracking. Added `ProviderConfigViewModel` for per-provider configuration with API key masking (`MaskApiKey` showing first 4 + 12 mask + last 4 chars), async vault operations (`LoadApiKeyAsync`, `SaveApiKeyAsync`, `DeleteApiKeyAsync`), and connection status updates. Added `LLMSettingsViewModel` as main settings page ViewModel with provider loading, connection testing via minimal chat request, default provider selection, and license gating (view at Core, configure at WriterPro+). Added `LLMSettingsPage` implementing `ISettingsPage` with category "llm.providers", sort order 75, and AI-related search keywords. Added `ConnectionStatusBadge` Avalonia control with color-coded status indicator (gray/blue/green/red). Added `LLMSettingsView` with split-panel layout showing provider list and configuration panel. Includes 11 new structured log events (1500-1510 range). Includes 108 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                        | Status      |
| --------------------------------- | ---------------------------- | ----------- |
| [v0.6.1a](v0.6.x/LCS-CL-v061a.md) | Chat Completion Abstractions | ✅ Complete |
| [v0.6.1b](v0.6.x/LCS-CL-v061b.md) | Chat Options Model           | ✅ Complete |
| [v0.6.1c](v0.6.x/LCS-CL-v061c.md) | Provider Registry            | ✅ Complete |
| [v0.6.1d](v0.6.x/LCS-CL-v061d.md) | API Key Management UI        | ✅ Complete |

---

## [v0.5.7] - 2026-02 (In Progress)

### The Panel Redesign (Reference Dock UX)

This release introduces a keyboard-centric Reference Panel experience with dismissible filter chips, streamlined navigation, and document-grouped results.

#### What's New

- **Panel Redesign (v0.5.7a)** — Enhanced Reference Panel with keyboard navigation commands (`MoveSelectionUp/Down`, `OpenSelectedResult`), dismissible `FilterChip` record with factory methods (`ForPath`, `ForExtension`, `ForDateRange`, `ForTag`), and `ActiveFilterChips` collection management. Added filter chips UI area with "Clear all filters" button, enhanced code-behind keyboard handlers for arrow key navigation and Escape/Enter key actions. Includes 31 unit tests.

- **Result Grouping (v0.5.7b)** — Implemented document-grouped results display. Search hits now organized by source document with collapsible headers showing match counts (~X matches~) and relevance scores. Added `IResultGroupingService` with LINQ-based grouping, `GroupedResultsViewModel` with expand/collapse commands, and multi-mode sorting (Relevance, Document Path, Match Count). Includes 36 unit tests.

- **Preview Pane (v0.5.7c)** — Split-view preview pane shows expanded context for selected search hits. Features async content loading, heading breadcrumb display with unicode arrows, visibility toggle, clipboard copy commands (matched content or full context), and license gating for Writer Pro. Added `IPreviewContentBuilder` service and `PreviewPaneViewModel`. Includes 26 unit tests.

- **Search Result Actions (v0.5.7d)** — Bulk operations on search results: copy (PlainText, CitationFormatted, Markdown, JSON), export (JSON, CSV, Markdown, BibTeX), and open-all documents. Added `ISearchActionsService` interface with 7 supporting types in Abstractions, `SearchActionsService` implementation in RAG module, and `SearchResultsExportedEvent` for telemetry. Export and citation-formatted copy gated to Writer Pro tier. Includes 24 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                 | Status      |
| --------------------------------- | --------------------- | ----------- |
| [v0.5.7a](v0.5.x/LCS-CL-v057a.md) | Panel Redesign        | ✅ Complete |
| [v0.5.7b](v0.5.x/LCS-CL-v057b.md) | Result Grouping       | ✅ Complete |
| [v0.5.7c](v0.5.x/LCS-CL-v057c.md) | Preview Pane          | ✅ Complete |
| [v0.5.7d](v0.5.x/LCS-CL-v057d.md) | Search Result Actions | ✅ Complete |

---

## [v0.5.9] - 2026-02 (In Progress)

### The Consolidator (Semantic Memory Deduplication)

This release establishes the infrastructure for detecting and consolidating near-duplicate content within the knowledge base, enabling cleaner search results and more efficient storage.

#### What's New

- **Similarity Detection Infrastructure (v0.5.9a)** — Core foundation for semantic deduplication. Added `ISimilarityDetector` interface with single (`FindSimilarAsync`) and batch (`FindSimilarBatchAsync`) similarity detection methods, leveraging existing pgvector infrastructure. Features configurable similarity thresholds (default 0.95 for conservative matching), batch processing with configurable batch sizes (default 10), and cross-document match detection. Includes `SimilarChunkResult` record with match metadata and `SimilarityDetectorOptions` for tunable detection parameters. Includes 16 unit tests.

- **Relationship Classification (v0.5.9b)** — Hybrid classification of semantic relationships between similar chunks. Added `IRelationshipClassifier` interface with single and batch classification methods. Features rule-based fast-path for high-confidence matches (similarity >= 0.95), in-memory caching with configurable TTL (default 1 hour), and license gating for Writer Pro. Classifies relationships as Equivalent, Complementary, Contradictory, Superseding, Subset, or Distinct. Includes `RelationshipClassification`, `ChunkPair`, `ClassificationOptions` records and `RelationshipType`/`ClassificationMethod` enums. Includes 20 unit tests.

- **Canonical Record Management (v0.5.9c)** — Data model and service layer for tracking authoritative chunks and merged duplicates. Added `ICanonicalManager` interface with CRUD operations for canonical records, variant merging, promotion, detachment, and provenance tracking. Features atomic database transactions, 4 MediatR events (`CanonicalRecordCreatedEvent`, `ChunkDeduplicatedEvent`, `VariantPromotedEvent`, `VariantDetachedEvent`), and license gating for Writer Pro. Includes `CanonicalRecord`, `ChunkVariant`, `ChunkProvenance` records and database migration for 3 new tables. Includes 24 unit tests.

- **Deduplication Service (v0.5.9d)** — Central orchestrator for the full deduplication pipeline during chunk ingestion. Added `IDeduplicationService` interface with `ProcessChunkAsync` for automatic deduplication, `FindDuplicatesAsync` for preview/dry-run, `ProcessManualDecisionAsync` for review queue handling, and `GetPendingReviewsAsync` for admin access. Features license gating bypass for unlicensed users, configurable thresholds via `DeduplicationOptions`, routing logic for all relationship types (merge, link, flag, queue), and manual review queue. Includes 8 data contracts (`DeduplicationAction`, `DeduplicationResult`, `DeduplicationOptions`, `DuplicateCandidate`, `ManualMergeDecision`, `ManualDecisionType`, `PendingReview`, `IDeduplicationService`) and database migration for pending_reviews table. Includes 23 unit tests.

- **Contradiction Detection (v0.5.9e)** — Complete workflow for detecting and resolving contradictory content between chunks. Added `IContradictionService` interface with `FlagAsync` for detection, `ResolveAsync` for resolution, `DismissAsync` for false positives, and `AutoResolveAsync` for system-initiated resolution. Features 5 resolution types (KeepOlder, KeepNewer, KeepBoth, CreateSynthesis, DeleteBoth), lifecycle status tracking (Pending, UnderReview, Resolved, Dismissed, AutoResolved), and MediatR event publishing. Includes `Contradiction`, `ContradictionResolution`, `ContradictionStatus`, `ContradictionResolutionType` records, `ContradictionDetectedEvent`/`ContradictionResolvedEvent` events, and `Migration_010_Contradictions` for the Contradictions table with unique normalized chunk pair index. Includes 54 unit tests.

- **Retrieval Integration (v0.5.9f)** — Search layer integration for deduplication-aware results. Added `SearchSimilarWithDeduplicationAsync` method to `IChunkRepository` with canonical-aware filtering, variant metadata loading, contradiction status flags, and optional provenance history. Features `DeduplicatedSearchResult` record with extended metadata (`CanonicalRecordId`, `VariantCount`, `HasContradictions`, `Provenance`) and helper properties (`IsCanonical`, `IsStandalone`, `HasVariants`, `HasProvenance`). Extended `SearchOptions` with deduplication controls (`RespectCanonicals`, `IncludeVariantMetadata`, `IncludeArchived`, `IncludeProvenance`). Includes `Migration_011_RetrievalIntegrationIndexes` for query optimization. Includes 28 unit tests.

- **Batch Deduplication (v0.5.9g)** — Background job for processing existing chunks through the deduplication pipeline. Added `IBatchDeduplicationJob` interface with `StartAsync`, `StopAsync`, `WaitForCompletionAsync` for job lifecycle management. Features `BatchDeduplicationRequest` for configuring scope (all, project, document), batch sizing, and statistics collection. Includes `BatchDeduplicationResult` with comprehensive success/failure tracking (chunks processed, duplicates found, merged, linked, contradictions, errors) and duration metrics. Added `BatchDeduplicationProgressedEvent` and `BatchDeduplicationCompletedEvent` for real-time monitoring. Gated to Writer Pro tier. Includes 30 unit tests.

- **Hardening & Metrics (v0.5.9h)** — Production readiness infrastructure with comprehensive observability for deduplication operations. Added static `DeduplicationMetrics` class with thread-safe counters (chunks processed by action type, similarity queries, classification requests, contradictions detected, batch jobs) and histogram sampling for P50/P90/P99 latency calculations. Added `IDeduplicationMetricsService` interface with recording methods (`RecordChunkProcessed`, `RecordSimilarityQuery`, `RecordClassification`, `RecordContradictionDetected`, `RecordBatchJobCompleted`) and query methods (`GetDashboardDataAsync`, `GetTrendsAsync`, `GetHealthStatusAsync`). Features `DeduplicationDashboardData` with deduplication rate, storage savings, operation breakdown, pending items counts. Added `DeduplicationHealthStatus` with P99 latency targets and warning generation. Integrated metrics recording into `SimilarityDetector`, `RelationshipClassifier`, `DeduplicationService`, `ContradictionService`, and `BatchDeduplicationJob`. Dashboard access gated to Writer Pro tier. Includes 59 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                       | Status      |
| --------------------------------- | --------------------------- | ----------- |
| [v0.5.9a](v0.5.x/LCS-CL-v059a.md) | Similarity Detection        | ✅ Complete |
| [v0.5.9b](v0.5.x/LCS-CL-v059b.md) | Relationship Classification | ✅ Complete |
| [v0.5.9c](v0.5.x/LCS-CL-v059c.md) | Canonical Record Management | ✅ Complete |
| [v0.5.9d](v0.5.x/LCS-CL-v059d.md) | Deduplication Service       | ✅ Complete |
| [v0.5.9e](v0.5.x/LCS-CL-v059e.md) | Contradiction Detection     | ✅ Complete |
| [v0.5.9f](v0.5.x/LCS-CL-v059f.md) | Retrieval Integration       | ✅ Complete |
| [v0.5.9g](v0.5.x/LCS-CL-v059g.md) | Batch Deduplication         | ✅ Complete |
| [v0.5.9h](v0.5.x/LCS-CL-v059h.md) | Hardening & Metrics         | ✅ Complete |

---

## [v0.5.8] - 2026-02 (In Progress)

### The Hardening (Quality & Performance)

This release hardens the RAG retrieval system with quality metrics infrastructure, performance profiling, caching optimization, and comprehensive testing.

#### What's New

- **Retrieval Quality Tests (v0.5.8a)** — Comprehensive test infrastructure for measuring retrieval accuracy. Includes curated test corpus (50 queries across 5 categories and 3 difficulty levels), `IRetrievalMetricsCalculator` interface with P@K, R@K, F1@K, MRR, NDCG@K metrics, `QualityReport` generation with category/difficulty stratification, and gold-standard relevance judgments. Includes 24 unit tests.

- **Search Performance Tests (v0.5.8b)** — BenchmarkDotNet performance benchmarks for all search operations. Features `SearchBenchmarks` class with 6 benchmark methods (HybridSearch, BM25Search, SemanticSearchOnly, FilteredSearch, QuerySuggestions, ContextExpansion) across 3 corpus sizes (1K, 10K, 50K chunks). Includes `BenchmarkDataSeeder` with Testcontainers PostgreSQL/pgvector, `BenchmarkConfig` with CI mode detection, P95/Max latency columns, and JSON/Markdown exporters. CI integration via GitHub Actions workflow with regression detection script (10% threshold).

- **Caching Strategy (v0.5.8c)** — Multi-layer in-memory caching for search and context operations. Features `IQueryResultCache` with LRU+TTL eviction (MaxEntries=100, TTL=5min), `IContextExpansionCache` with session-scoped isolation, `CacheInvalidationHandler` for automatic invalidation on DocumentIndexedEvent/DocumentRemovedFromIndexEvent, and `CacheKeyGenerator` for deterministic SHA256 keys. Both caches use thread-safe concurrent data structures. Includes 44+ unit tests.

- **Error Resilience (v0.5.8d)** — Polly-based resilience patterns for graceful degradation. Features `IResilientSearchService` with circuit breaker, retry with exponential backoff + jitter, and automatic fallback hierarchy (Hybrid → BM25 → Cache → Unavailable). Added `SearchHealthStatus` for dependency monitoring, `ResilientSearchResult` wrapper with degradation metadata, and `ResilienceOptions` for policy configuration. Includes 27 unit tests.

#### Sub-Part Changelogs

| Version                           | Title                    | Status      |
| --------------------------------- | ------------------------ | ----------- |
| [v0.5.8a](v0.5.x/LCS-CL-v058a.md) | Retrieval Quality Tests  | ✅ Complete |
| [v0.5.8b](v0.5.x/LCS-CL-v058b.md) | Search Performance Tests | ✅ Complete |
| [v0.5.8c](v0.5.x/LCS-CL-v058c.md) | Caching Strategy         | ✅ Complete |
| [v0.5.8d](v0.5.x/LCS-CL-v058d.md) | Error Resilience         | ✅ Complete |

---

## [v0.5.6] - 2026-02 (In Progress)

### The Answer Preview (Snippet Generation)

This release introduces the Answer Preview — contextual snippet extraction from search results that highlights matching query terms. Snippets are intelligently centered on the most relevant portions of each result, improving scan-ability and helping users quickly assess document relevance.

#### What's New

- **Snippet Extraction** — Implemented `ISnippetService` interface and `SnippetService` class for extracting contextual snippets from `TextChunk` content. Features match density calculation to center snippets on query-relevant regions, sliding window algorithm for optimal placement, highlight span generation for exact and fuzzy matches, and sentence-aware boundary expansion. Includes `Snippet`, `HighlightSpan`, `HighlightType`, and `SnippetOptions` records with factory methods (`Empty`, `FromPlainText`) and preset configurations (`Default`, `Compact`, `Extended`). Added `ISentenceBoundaryDetector` interface for boundary detection. Includes 46 unit tests.

- **Query Term Highlighting** — Implemented `IHighlightRenderer` interface and `HighlightRenderer` for converting snippet highlight spans into styled text runs. Added `HighlightThemeStyle` configuration record with 4 built-in theme presets (Dark, Light, HighContrast, Monokai). Created `HighlightedSnippetControl` Avalonia control for rendering styled text with support for match type colors and optional underlines. Includes 27 unit tests.

- **Smart Truncation** — Implemented `SentenceBoundaryDetector` with 50+ abbreviation recognition (titles, Latin, initialisms like U.S.A.), decimal number detection, and forward-looking pattern matching. Added `SentenceBoundary` record with `Contains()` and `OverlapsWith()` helpers. Created `MatchDensityCalculator` with sliding window algorithm and HighlightType weights (QueryMatch: 2.0, FuzzyMatch: 1.0, other: 0.5). Replaces placeholder implementation. Includes 68 unit tests.

#### Sub-Part Changelogs

| Version                          | Title                   | Status      |
| -------------------------------- | ----------------------- | ----------- |
| [v0.5.6a](v0.5.x/LCS-CL-056a.md) | Snippet Extraction      | ✅ Complete |
| [v0.5.6b](v0.5.x/LCS-CL-056b.md) | Query Term Highlighting | ✅ Complete |
| [v0.5.6c](v0.5.x/LCS-CL-056c.md) | Smart Truncation        | ✅ Complete |
| [v0.5.6d](v0.5.x/LCS-CL-056d.md) | Multi-Snippet Results   | ✅ Complete |

#### Knowledge Graph Sub-Parts (v0.5.6-KG)

- **Claim Data Model** — Implemented core data contracts for claim extraction pipeline. Records include `Claim` (subject-predicate-object assertions with confidence and validation), `ClaimEntity` (entity references with resolved/unresolved states), `ClaimObject` (entity or literal values), `ClaimEvidence` (source text provenance), and supporting enums (`ClaimValidationStatus`, `ClaimExtractionMethod`, `ClaimRelationType`). Added `ClaimPredicate` static class with 26 standard predicates and inverse lookup. License-gated to WriterPro tier. Includes 23 unit tests.

- **Sentence Parser** — Implemented `ISentenceParser` interface with `SpacySentenceParser` for parsing text into linguistic structures. Features sentence segmentation, tokenization with POS tagging and lemmatization, dependency parsing for grammatical relations (`Token`, `DependencyNode`, `DependencyRelation`), and semantic role labeling (`SemanticFrame`, `SemanticArgument`, `SemanticRole` enum). Includes helper methods on `ParsedSentence` for extracting root verbs, subjects, and objects. Supports caching via `IMemoryCache` and multi-language (en, de, fr, es). License-gated to WriterPro tier. Includes 35 unit tests.

- **Claim Extractor** — Implemented `IClaimExtractionService` interface with `ClaimExtractionService` orchestrating claim extraction from parsed text. Features pattern-based extraction (regex/template patterns) via `PatternClaimExtractor`, dependency-based extraction via `DependencyClaimExtractor`, semantic deduplication via `ClaimDeduplicator`, and confidence scoring via `ConfidenceScorer`. Includes `ClaimExtractionContext` configuration, `ExtractedClaim` intermediate representation, and YAML-based pattern loading. Built-in patterns for common API documentation structures (ACCEPTS, RETURNS, REQUIRES, etc.). License-gated to WriterPro tier. Includes 22 unit tests.

- **Claim Repository** — Implemented `IClaimRepository` interface with `ClaimRepository` providing PostgreSQL-backed claim persistence. Features full CRUD operations (GetAsync, CreateAsync, UpdateAsync, DeleteAsync), advanced search with `ClaimSearchCriteria` (filtering by project, document, entity, predicate, confidence range, date range), pagination via `ClaimQueryResult` (Page/PageSize or Skip/Take), bulk operations via `UpsertManyAsync` with `BulkUpsertResult`, and MediatR events (`ClaimCreatedEvent`, `ClaimUpdatedEvent`, `ClaimDeletedEvent`, `DocumentClaimsReplacedEvent`). Includes full-text search using PostgreSQL `tsvector/tsquery`, 5-minute memory caching, and comprehensive logging. License-gated to Teams tier. Includes 27 unit tests.

- **Claim Diff Service** — Implemented `IClaimDiffService` interface with `ClaimDiffService` for comparing claims between document versions. Features ID-based and semantic matching (Jaro-Winkler similarity) via `SemanticMatcher`, field-level change detection (`ClaimModification`, `FieldChange`), impact assessment (`ChangeImpact` enum), and change grouping by entity. Includes baseline snapshots (`ClaimSnapshot`), history tracking (`ClaimHistoryEntry`), and cross-document contradiction detection (`ClaimContradiction`). Data contracts include `DiffOptions`, `ClaimDiffResult`, `DiffStats`, `ClaimChange`, and `ClaimChangeGroup`. License-gated to Teams tier. Includes 34 unit tests.

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.5.6e](v0.5.x/LCS-CL-056e.md) | Claim Data Model   | ✅ Complete |
| [v0.5.6f](v0.5.x/LCS-CL-056f.md) | Sentence Parser    | ✅ Complete |
| [v0.5.6g](v0.5.x/LCS-CL-056g.md) | Claim Extractor    | ✅ Complete |
| [v0.5.6h](v0.5.x/LCS-CL-056h.md) | Claim Repository   | ✅ Complete |
| [v0.5.6i](v0.5.x/LCS-CL-056i.md) | Claim Diff Service | ✅ Complete |
| v0.5.6j                          | Entity Linker      | 🔜 Planned  |

---

## [v0.5.5] - 2026-02 (In Progress)

### The Filter System (Scoped Search)

This release introduces the Filter System — scoped search that allows users to narrow results to specific documents, folders, file types, or metadata criteria. Filters can be saved as presets for quick reuse.

#### What's New

- **Filter Model** — Implemented `SearchFilter` record with path patterns (glob syntax), file extensions, date ranges, and heading filters. Includes `DateRange` record with factory methods (`LastDays`, `LastHours`, `Today`, `ForMonth`) for common temporal filters. Added `FilterPreset` record for saved filter configurations with Create/Rename/UpdateFilter methods. Implemented `IFilterValidator` interface and `FilterValidator` service for security validation (path traversal, null bytes) and structural checks. Available to all users (Core tier). Includes 60 unit tests.

- **Filter UI Component** — Implemented the filter panel UI with folder tree (checkbox selection with propagation), extension toggles (md, txt, json, yaml, rst), date range picker (preset and custom ranges), and saved presets dropdown. Added `DateRangeOption` and `FilterChipType` enums, `FilterChipViewModel` record for visual chip display, `ExtensionToggleViewModel` and `FolderNodeViewModel` for tree/toggle management, and `SearchFilterPanelViewModel` for orchestrating all filter state. Features license-gated date filtering and presets (WriterPro+) with lock icon display for Core tier. Filter chips show active filters with one-click removal. Includes 66 unit tests.

- **Filter Query Builder** — Implemented `IFilterQueryBuilder` interface and `FilterQueryBuilder` service for translating `SearchFilter` criteria into parameterized SQL. Features glob-to-SQL conversion (`**`→`%`, `*`→`%`, `?`→`_`), path pattern LIKE clauses, extension array ANY() clauses, date range conditions, and heading IS NOT NULL checks. Uses CTE (Common Table Expression) pattern for efficient filtered vector search while preserving HNSW index performance. Added `FilterQueryResult` record with `WhereClause`, `Parameters`, `CteClause`, `JoinClause`, and summary properties. Thread-safe and stateless design. Includes 35 unit tests.

- **Linking Review UI** — Implemented human review interface for entity links. Added `ILinkingReviewService` interface for managing the review queue with `GetPendingAsync`, `SubmitDecisionAsync`, `SubmitDecisionsBatchAsync`, and `GetStatsAsync` methods. Created `PendingLinkItem` record for pending links with mention details, candidates, and group support; `LinkReviewDecision` record for capturing reviewer decisions; `ReviewStats` record for queue statistics; and `ReviewFilter`/`ReviewSortOrder` for queue filtering. Implemented `LinkingReviewViewModel` with Accept, Reject, Skip, SelectAlternate, CreateNew, and MarkNotEntity commands. Created `LinkingReviewPanel.axaml` with three-column layout (queue, context, candidates), keyboard shortcuts (Ctrl+A/R/S/N), and group decision support. License-gated: WriterPro (view-only), Teams+ (full review). Includes 35 unit tests.

#### Sub-Part Changelogs

| Version                              | Title                                    | Status      |
| ------------------------------------ | ---------------------------------------- | ----------- |
| [v0.5.5a](v0.5.x/LCS-CL-055a.md)     | Filter Model                             | ✅ Complete |
| [v0.5.5b](v0.5.x/LCS-CL-055b.md)     | Filter UI Component                      | ✅ Complete |
| [v0.5.5c-i](v0.5.x/LCS-CL-055c-i.md) | Filter Query Builder + Linking Review UI | ✅ Complete |
| v0.5.5d                              | Saved Filters                            | 🔜 Planned  |

---

## [v0.5.4] - 2026-02 (In Progress)

### The Relevance Tuner (Query Intelligence)

This release introduces the Relevance Tuner — intelligent query analysis and enhancement for more precise search results. Queries are now analyzed for keywords, entities, and intent, expanded with synonyms and morphological variants, and tracked for analytics and content gap identification.

#### What's New

- **Query Analyzer** — Implemented `IQueryAnalyzer` interface and `QueryAnalyzer` service for extracting keywords (with stop-word filtering), recognizing entities (code identifiers, file paths, versions, error codes), detecting query intent (Factual, Procedural, Conceptual, Navigational), and calculating specificity scores (0.0-1.0). Uses regex-based entity recognition and heuristic intent detection. Available to all users (Core tier). Includes 30 unit tests.

- **Query Expander** — Implemented `IQueryExpander` interface and `QueryExpander` service for enhancing queries with synonyms from the terminology database and built-in technical abbreviations (50+ mappings like api→application programming interface, db→database). Includes Porter stemming for morphological variants (e.g., running→run, runs, runner). Results are cached via ConcurrentDictionary. License-gated to WriterPro+ tier. Includes 20 unit tests.

- **Query Suggestions** — Implemented `IQuerySuggestionService` interface and `QuerySuggestionService` for autocomplete suggestions from multiple sources: query history, document headings, content n-grams, and domain terms. Features prefix matching with frequency-weighted scoring, 30-second cache TTL, and automatic cleanup on document re-index. Database schema includes `query_suggestions` table with prefix index (text_pattern_ops) for efficient LIKE queries. License-gated to WriterPro+ tier. Includes 15 unit tests.

- **Query History & Analytics** — Implemented `IQueryHistoryService` interface and `QueryHistoryService` for tracking executed queries with metadata (intent, result count, duration). Features SHA256 query hashing for anonymous aggregation, 60-second deduplication window, and zero-result query identification for content gap analysis. Database schema includes `query_history` table with partial index for zero-result queries. Publishes `QueryAnalyticsEvent` via MediatR for opt-in telemetry. Includes 24 unit tests.

#### Sub-Part Changelogs

| Version                          | Title                     | Status      |
| -------------------------------- | ------------------------- | ----------- |
| [v0.5.4a](v0.5.x/LCS-CL-054a.md) | Query Analyzer            | ✅ Complete |
| [v0.5.4b](v0.5.x/LCS-CL-054b.md) | Query Expansion           | ✅ Complete |
| [v0.5.4c](v0.5.x/LCS-CL-054c.md) | Query Suggestions         | ✅ Complete |
| [v0.5.4d](v0.5.x/LCS-CL-054d.md) | Query History & Analytics | ✅ Complete |

---

## [v0.5.3] - 2026-02 (In Progress)

### The Context Window (Relevance Expansion)

This release introduces the Context Window — intelligent context expansion for search results. Each retrieved chunk can now be expanded to include surrounding content and the document's heading hierarchy, providing writers with more contextual information without leaving the search results.

#### What's New

- **Context Expansion Service** — Implemented `IContextExpansionService` interface and `ContextExpansionService` class with LRU-style caching (FIFO eviction, 100-entry limit). Retrieves sibling chunks before and after a match (configurable 0-5 chunks each direction) via the new `IChunkRepository.GetSiblingsAsync` method. Includes graceful degradation for heading resolution and MediatR-based telemetry via `ContextExpandedEvent`. License-gated to WriterPro+ tier. Includes 31 unit tests.

- **Context Abstractions** — Added `ContextOptions` record for configuring expansion parameters with validation and clamping (max 5 chunks per direction), `ExpandedChunk` record with computed properties (`HasBefore`, `HasAfter`, `HasBreadcrumb`, `TotalChunks`) and a `FormatBreadcrumb()` method, and `IHeadingHierarchyService` interface for heading resolution (stub implementation until v0.5.3c).

- **Sibling Chunk Retrieval** — Implemented dedicated `SiblingCache` class providing LRU-based caching for sibling chunk queries with document-level invalidation. Cache holds up to 500 entries with 50-entry batch eviction. Subscribes to `DocumentIndexedEvent` and `DocumentRemovedFromIndexEvent` for automatic cache invalidation when documents are re-indexed or removed. Added `SiblingCacheKey` record struct for efficient dictionary lookup. Integrated with `ChunkRepository.GetSiblingsAsync()` for transparent caching with cache-first lookup and automatic population on miss. Includes 27 unit tests.

- **Heading Hierarchy Service** — Implemented `HeadingHierarchyService` to resolve heading breadcrumb trails for document chunks, replacing the stub implementation from v0.5.3a. Uses stack-based tree construction from chunk heading metadata and recursive depth-first search for breadcrumb resolution. Extended `Chunk` model with `Heading` and `HeadingLevel` properties, added `ChunkHeadingInfo` record for efficient heading queries, and updated all `ChunkRepository` queries to include heading columns. Includes document-level caching (50 entries) with automatic invalidation via MediatR event handlers. Handles edge cases including multiple H1 headings, skipped levels (H1→H3), and chunks before/after headings. Includes 30 unit tests.

- **Context Preview UI** — Implemented expandable context previews in search results with `ContextPreviewViewModel` for state management, custom `BreadcrumbControl` and `ContextChunkControl` Avalonia controls, and `ContextPreviewViewModelFactory` for TextChunk to Chunk conversion. Features include expand/collapse with cached context data, heading breadcrumb display, before/after chunk visualization with muted styling, and license gating (WriterPro+) with upgrade prompt. Added `LicenseTooltipConverter` for license-aware tooltips and `UpgradePromptRequestedEvent` for cross-module upgrade dialog communication. Includes 44 unit tests.

#### Sub-Part Changelogs

| Version                          | Title                     | Status      |
| -------------------------------- | ------------------------- | ----------- |
| [v0.5.3a](v0.5.x/LCS-CL-053a.md) | Context Expansion Service | ✅ Complete |
| [v0.5.3b](v0.5.x/LCS-CL-053b.md) | Sibling Chunk Retrieval   | ✅ Complete |
| [v0.5.3c](v0.5.x/LCS-CL-053c.md) | Heading Hierarchy Service | ✅ Complete |
| [v0.5.3d](v0.5.x/LCS-CL-053d.md) | Context Preview UI        | ✅ Complete |

---

## [v0.5.2] - 2026-02 (In Progress)

### The Citation Engine (Source Attribution)

This release introduces the Citation Engine — comprehensive source attribution for every retrieved chunk. Every search result now carries complete provenance information, enabling writers to trace information back to its exact source location.

#### What's New

- **Citation Model** — Implemented `Citation` record with complete provenance fields (document path, heading, line number, indexing timestamp) and `ICitationService` interface for creating, formatting, and validating citations. Citations are created from search hits with automatic line number calculation from character offsets. Three formatting styles (Inline, Footnote, Markdown) are supported with license gating (WriterPro+). Includes 56 unit tests.

- **Citation Styles** — Implemented `ICitationFormatter` interface and three built-in formatters: `InlineCitationFormatter` ([filename.md, §Heading]), `FootnoteCitationFormatter` ([^id]: /path:line), and `MarkdownCitationFormatter` ([Title](file:///path#L42)). Added `CitationFormatterRegistry` for style lookup and user preference persistence via `ISystemSettingsRepository`, with `CitationSettingsKeys` constants. Includes 53 unit tests.

- **Stale Citation Detection** — Implemented `ICitationValidator` interface and `CitationValidator` service for detecting when source documents have changed since indexing. Compares file modification timestamps against citation IndexedAt values. Supports single and batch validation with throttled parallel execution (up to 10 concurrent). License-gated to WriterPro+ via `FeatureCodes.CitationValidation`. Publishes `CitationValidationFailedEvent` for stale and missing citations. Added `StaleIndicatorViewModel` with Validate, Re-verify (re-indexes document), and Dismiss commands, with computed properties for stale/missing status icons and messages. Includes 58 unit tests.

- **Citation Copy Actions** — Implemented `ICitationClipboardService` interface and `CitationClipboardService` for copying citation data to the clipboard. Supports four copy formats: formatted citation (user's preferred or specified style), raw chunk text, document path, and file:// URI. Extended `SearchResultItemViewModel` with copy commands and added a context menu to `SearchResultItemView` with "Copy Citation" (Ctrl+C), "Copy as..." submenu (Inline/Footnote/Markdown styles), "Copy Chunk Text", and "Copy Path" options. All copy operations publish `CitationCopiedEvent` for telemetry. Includes 10 unit tests.

#### Sub-Part Changelogs

| Version                          | Title                    | Status      |
| -------------------------------- | ------------------------ | ----------- |
| [v0.5.2a](v0.5.x/LCS-CL-052a.md) | Citation Model           | ✅ Complete |
| [v0.5.2b](v0.5.x/LCS-CL-052b.md) | Citation Styles          | ✅ Complete |
| [v0.5.2c](v0.5.x/LCS-CL-052c.md) | Stale Citation Detection | ✅ Complete |
| [v0.5.2d](v0.5.x/LCS-CL-052d.md) | Citation Copy Actions    | ✅ Complete |

---

## [v0.5.1] - 2026-02 (Complete)

### The Hybrid Engine (BM25 + Semantic)

This release introduces hybrid search combining BM25 keyword search with semantic vector similarity for more accurate document retrieval.

#### What's New

- **BM25 Index Schema** — Extended the `Chunks` table with a generated `ContentTsvector` column and GIN index for PostgreSQL full-text search. Enables fast keyword matching using `@@` operator with automatic stemming and stop word removal via the 'english' text search configuration.

- **BM25 Search Implementation** — Implemented `IBM25SearchService` interface and `BM25SearchService` class for keyword-based full-text search using PostgreSQL's `ts_rank()` function. Includes license gating (WriterPro+), query preprocessing, telemetry events, and comprehensive unit tests.

- **Hybrid Fusion Algorithm** — Implemented `IHybridSearchService` and `HybridSearchService` using Reciprocal Rank Fusion (RRF) to combine BM25 keyword search and semantic vector search results. Executes both searches in parallel and merges ranked lists with configurable weights (default: 0.7 semantic, 0.3 BM25). Chunks appearing in both result sets are naturally boosted. Includes WriterPro+ license gating, telemetry events, and 52 unit tests.

- **Search Mode Toggle** — Added a search mode dropdown to the Reference Panel, allowing users to switch between Semantic, Keyword, and Hybrid search strategies. Hybrid mode is license-gated to WriterPro+ with automatic fallback to Semantic for Core tier. Mode selection is persisted across sessions and emits telemetry events. Includes 39 unit tests.

#### Sub-Part Changelogs

| Version                          | Title                      | Status      |
| -------------------------------- | -------------------------- | ----------- |
| [v0.5.1a](v0.5.x/LCS-CL-051a.md) | BM25 Index Schema          | ✅ Complete |
| [v0.5.1b](v0.5.x/LCS-CL-051b.md) | BM25 Search Implementation | ✅ Complete |
| [v0.5.1c](v0.5.x/LCS-CL-051c.md) | Hybrid Fusion Algorithm    | ✅ Complete |
| [v0.5.1d](v0.5.x/LCS-CL-051d.md) | Search Mode Toggle         | ✅ Complete |

---

## [v0.2.4] - 2026-01 (In Progress)

### Red Pen (Editor Integration)

This release integrates the linting engine with the editor, providing visual feedback for style violations.

#### What's New

- **Style Violation Renderer** — DocumentColorizingTransformer that identifies violations within visible lines and registers underline segments for rendering. Subscribes to violation changes and invalidates the view for real-time updates.

- **Wavy Underline Renderer** — AvaloniaEdit IBackgroundRenderer that draws wavy underlines using quadratic bezier curves. Supports severity-based colors (red for errors, yellow for warnings, blue for info, gray for hints).

- **Theme-Aware Underline Colors** — Underlines automatically adapt to light and dark themes with distinct color palettes optimized for visibility. Light theme uses high-contrast colors on white; dark theme uses softer tones that remain visible without being harsh. Includes pen caching for performance and severity-based z-ordering (errors always draw on top).

- **Violation Provider** — Thread-safe bridge between the linting pipeline and editor rendering. Maintains current violations per document with change notification events for efficient UI updates.

- **Violation Color Provider** — Maps violation severity to theme-aware underline colors following IDE conventions (matching VS Code/Roslyn color schemes). Provides background colors, tooltip borders, and Material Design severity icons.

- **Hover Tooltips** — Contextual tooltips that appear when hovering over style violations. Displays rule name, message, and optional recommendation. Supports multi-violation navigation when violations overlap, and can be triggered via keyboard shortcuts (Ctrl+K, Ctrl+I).

- **Context Menu Quick-Fixes** — Right-click on style violations or press Ctrl+. to see available fixes. Selecting a fix applies the suggested replacement with full undo support. Works with overlapping violations, showing all available fixes in the menu.

#### Sub-Part Changelogs

| Version                          | Title                        | Status      |
| -------------------------------- | ---------------------------- | ----------- |
| [v0.2.4a](v0.2.x/LCS-CL-024a.md) | Rendering Transformer        | ✅ Complete |
| [v0.2.4b](v0.2.x/LCS-CL-024b.md) | Theme-Aware Underline Colors | ✅ Complete |
| [v0.2.4c](v0.2.x/LCS-CL-024c.md) | Hover Tooltips               | ✅ Complete |
| [v0.2.4d](v0.2.x/LCS-CL-024d.md) | Context Menu Quick-Fixes     | ✅ Complete |

---

## [v0.4.1] - 2026-01 (In Progress)

### The Vector Foundation

This release introduces PostgreSQL with pgvector for vector storage, enabling semantic search capabilities.

#### What's New

- **pgvector Docker Configuration** — PostgreSQL 16 with pgvector extension for high-dimensional vector storage. Includes automated extension setup, health checks, and performance tuning for vector workloads.

- **Schema Migration** — FluentMigrator migration creating `Documents` and `Chunks` tables with pgvector embeddings. Features HNSW index for efficient similarity search, cascade delete behavior, and auto-updating timestamps.

- **Repository Abstractions** — `IDocumentRepository` and `IChunkRepository` interfaces defining the data access contracts for the RAG subsystem. Includes `Document`, `Chunk`, `ChunkSearchResult` records and `DocumentStatus` enum with comprehensive XML documentation.

#### Sub-Part Changelogs

| Version                             | Title                   | Status      |
| ----------------------------------- | ----------------------- | ----------- |
| [v0.4.1a](v0.4.x/LCS-CL-v0.4.1a.md) | pgvector Docker Config  | ✅ Complete |
| [v0.4.1b](v0.4.x/LCS-CL-v0.4.1b.md) | Schema Migration        | ✅ Complete |
| [v0.4.1c](v0.4.x/LCS-CL-v0.4.1c.md) | Repository Abstractions | ✅ Complete |
| v0.4.1d                             | Dapper Implementation   | 🔜 Planned  |

---

## [v0.4.2] - 2026-01 (In Progress)

### The Watcher (File Ingestion Pipeline)

This release establishes the file ingestion pipeline for processing documents into the RAG system.

#### What's New

- **Ingestion Service Interface** — `IIngestionService` contract defining single file and directory ingestion, document removal, and real-time progress reporting via events.

- **Ingestion Data Contracts** — `IngestionResult` record with factory methods for success/failure/skipped outcomes, `IngestionProgressEventArgs` with auto-calculated completion percentage, and `IngestionOptions` for configurable file filtering and concurrency.

- **Pipeline Phase Model** — `IngestionPhase` enum defining 7 discrete stages (Scanning → Hashing → Reading → Chunking → Embedding → Storing → Complete) enabling granular progress tracking.

- **Hash-Based Change Detection** — `IFileHashService` with SHA-256 streaming hash computation and tiered change detection (size → timestamp → hash) to minimize unnecessary re-indexing of unchanged files.

- **File Watcher Integration** — `FileWatcherIngestionHandler` bridges the workspace file watcher with the RAG pipeline, publishing `FileIndexingRequestedEvent` for files matching configured extensions. Features per-file debouncing and directory exclusion.

- **Ingestion Queue** — Priority-based queue using `System.Threading.Channels` for thread-safe file processing. Features configurable priority levels (user action → recent change → normal → background), duplicate detection with sliding window, backpressure handling, and graceful shutdown. Background service continuously processes queue items through the ingestion pipeline.

#### Sub-Part Changelogs

| Version                             | Title                       | Status      |
| ----------------------------------- | --------------------------- | ----------- |
| [v0.4.2a](v0.4.x/LCS-CL-v0.4.2a.md) | Ingestion Service Interface | ✅ Complete |
| [v0.4.2b](v0.4.x/LCS-CL-v0.4.2b.md) | Hash-Based Change Detection | ✅ Complete |
| [v0.4.2c](v0.4.x/LCS-CL-v0.4.2c.md) | File Watcher Integration    | ✅ Complete |
| [v0.4.2d](v0.4.x/LCS-CL-v0.4.2d.md) | Ingestion Queue             | ✅ Complete |

---

## [v0.4.7] - 2026-02 (In Progress)

### The Index Manager (Corpus Administration)

This release introduces index status visibility and manual control over the document corpus.

#### What's New

- **Index Status View** — Settings page displaying all indexed documents with status (Indexed, Stale, Failed, Pending), file metadata, and aggregate statistics. Supports filtering by text and status. (v0.4.7a)

- **Manual Indexing Controls** — Commands for managing the document index: Re-index Document, Remove from Index, and Re-index All. Includes confirmation dialogs for destructive operations and progress reporting for bulk operations. (v0.4.7b)

- **Indexing Progress Toast** — Real-time progress overlay for indexing operations with document name, progress bar, elapsed time, and cancellation support. Auto-dismisses after completion with success/cancelled icons. (v0.4.7c)

- **Indexing Error Categorization** — Error handling for failed indexing operations with 10 error categories (RateLimit, NetworkError, FileNotFound, etc.). `IndexingErrorCategorizer` classifies exceptions for targeted retry logic and user-friendly messaging. Failed documents show `IsRetryable` status and `SuggestedAction` guidance. (v0.4.7d)

- **Entity List View** — Knowledge Graph browser displaying all extracted entities with type icons, confidence badges, relationship/mention counts. Supports filtering by type, search text, and minimum confidence. `IGraphRepository` abstraction for entity queries. (v0.4.7e)

- **Entity Detail View** — Comprehensive entity details panel showing properties, relationships, and source documents. Supports navigation to related entities and opening source documents in the editor. Properties display with schema metadata and copy-to-clipboard. Edit actions license-gated to Teams tier. (v0.4.7f)

- **Entity CRUD Operations** — Service layer for entity management with create, update, and delete operations. `IEntityCrudService` abstraction with full validation and error handling. (v0.4.7g)

- **Relationship Viewer** — Hierarchical tree view displaying entity relationships grouped by direction and type. Supports filtering by direction (Incoming/Outgoing/Both) and relationship type. Integrated into Entity Detail View. (v0.4.7h)

- **Axiom Viewer** — Read-only panel displaying all axioms with human-readable rule formatting. Supports five-dimension filtering (search, type, severity, category, disabled status) and grouping. Human-readable formatting for all 14 constraint types and 9 condition operators. (v0.4.7i)

- **Index Management Telemetry** — MediatR events for tracking manual indexing operations: `DocumentReindexedEvent`, `DocumentRemovedFromIndexEvent`, and `AllDocumentsReindexedEvent`.

#### Sub-Part Changelogs

| Version                             | Title                    | Status      |
| ----------------------------------- | ------------------------ | ----------- |
| [v0.4.7a](v0.4.x/LCS-CL-v0.4.7a.md) | Index Status View        | ✅ Complete |
| [v0.4.7b](v0.4.x/LCS-CL-v0.4.7b.md) | Manual Indexing Controls | ✅ Complete |
| [v0.4.7c](v0.4.x/LCS-CL-v0.4.7c.md) | Indexing Progress        | ✅ Complete |
| [v0.4.7d](v0.4.x/LCS-CL-v0.4.7d.md) | Indexing Errors          | ✅ Complete |
| [v0.4.7e](v0.4.x/LCS-CL-v0.4.7e.md) | Entity List View         | ✅ Complete |
| [v0.4.7f](v0.4.x/LCS-CL-v0.4.7f.md) | Entity Detail View       | ✅ Complete |
| [v0.4.7g](v0.4.x/LCS-CL-v0.4.7g.md) | Entity CRUD Operations   | ✅ Complete |
| [v0.4.7h](v0.4.x/LCS-CL-v0.4.7h.md) | Relationship Viewer      | ✅ Complete |
| [v0.4.7i](v0.4.x/LCS-CL-v0.4.7i.md) | Axiom Viewer             | ✅ Complete |

---

## [v0.4.8] - 2026-02 (In Progress)

### The Hardening (Performance & Testing)

This release hardens the RAG subsystem with comprehensive unit tests, integration tests, and performance benchmarks.

#### What's New

- **Unit Test Suite** — Comprehensive test coverage for RAG module with 755+ verified tests. Coverage for chunking strategies, embedding service (mock HTTP), token counter edge cases, search logic, and ingestion pipeline. `coverage.runsettings` for consistent coverage reporting targeting ≥80%. (v0.4.8a)

- **Integration Tests** — End-to-end test suite using Testcontainers with PostgreSQL + pgvector. Full database roundtrip verification for ingestion, search, change detection, and deletion cascade. VectorEnabledConnectionFactory with `UseVector()` for pgvector type mapping. (v0.4.8b)

- **Performance Benchmarks** — BenchmarkDotNet suite with 21 benchmarks covering chunking throughput, token counting, vector search latency, and memory allocation. Parameterized corpus sizes for scaling analysis. (v0.4.8c)

- **Embedding Cache** — Local SQLite-based embedding cache to reduce API costs and improve latency for repeated queries. `SqliteEmbeddingCache` with LRU eviction, `CachedEmbeddingService` decorator for transparent caching, and configurable options (max size, cache path, compaction interval). (v0.4.8d)

#### Sub-Part Changelogs

| Version                             | Title                  | Status      |
| ----------------------------------- | ---------------------- | ----------- |
| [v0.4.8a](v0.4.x/LCS-CL-v0.4.8a.md) | Unit Test Suite        | ✅ Complete |
| [v0.4.8b](v0.4.x/LCS-CL-v0.4.8b.md) | Integration Tests      | ✅ Complete |
| [v0.4.8c](v0.4.x/LCS-CL-v0.4.8c.md) | Performance Benchmarks | ✅ Complete |
| [v0.4.8d](v0.4.x/LCS-CL-v0.4.8d.md) | Embedding Cache        | ✅ Complete |

---

## [v0.4.6] - 2026-02 (In Progress)

### The Reference Panel (Search UI)

This release implements the user-facing interface for semantic search, completing the RAG subsystem's search experience.

#### What's New

- **Search History Service** — `ISearchHistoryService` with thread-safe LinkedList-based implementation for O(1) operations. Case-insensitive deduplication, automatic eviction at max size (10), and query trimming.

- **Reference Panel View** — `ReferenceView.axaml` with AutoCompleteBox for search with history dropdown, virtualized results list using `VirtualizingStackPanel`, loading indicator, no-results state, and status bar showing result count and duration. License gating displays warning for unlicensed users.

- **ViewModels** — `ReferenceViewModel` orchestrates search execution, history management, and license checking via `SearchLicenseGuard`. `SearchResultItemViewModel` wraps `SearchHit` with formatted display properties including document name, preview text, relevance score, and section heading.

- **Search Result Item View** — Dedicated `SearchResultItemView` UserControl with score badge coloring (green/amber/orange/gray based on relevance), query term highlighting via `HighlightedTextBlock` custom control, document icon, section heading, and double-click navigation to source.

- **Source Navigation** — `IReferenceNavigationService` with `ReferenceNavigationService` implementation that bridges RAG search results with the editor's navigation infrastructure. Opens closed documents, scrolls to chunk offsets, and highlights matched text spans. Publishes `ReferenceNavigatedEvent` for telemetry tracking. `HighlightStyle` enum for categorizing editor highlights (SearchResult, Error, Warning, Reference).

- **Search History Service** — Enhanced `ISearchHistoryService` with persistence via `ISearchHistoryRepository`, query removal, and `IObservable<string>` change notifications. Thread-safe LinkedList implementation with O(1) operations, case-insensitive deduplication, and automatic eviction.

- **Axiom Data Model** — Foundational data model for the Axiom Store (CKVS Phase 1b). Defines `Axiom`, `AxiomRule`, `AxiomViolation`, and `AxiomValidationResult` records with 4 supporting enums (`AxiomSeverity`, `AxiomTargetKind`, `AxiomConstraintType`, `ConditionOperator`) for domain rule governance.

- **Axiom Repository** — Persistent storage for domain axioms using PostgreSQL via Dapper. `IAxiomRepository` contract defines CRUD operations with `AxiomFilter` for querying by type/category/tags and `AxiomStatistics` for aggregate counts. `AxiomCacheService` provides 5-minute sliding / 30-minute absolute expiration via `IMemoryCache`. License-gated: WriterPro+ for reads, Teams+ for writes.

- **Axiom Loader** — YAML-based axiom loading from embedded resources and workspace files (`.lexichord/knowledge/axioms/`). `AxiomYamlParser` deserializes axiom files with constraint type mapping, `AxiomSchemaValidator` validates target types against `ISchemaRegistry`, and `AxiomLoader` orchestrates loading with file watching for hot-reload. License-gated: WriterPro+ for built-in, Teams+ for workspace.

- **Axiom Query API** — In-memory axiom store with O(1) retrieval indexed by target type. `AxiomEvaluator` supports 9 constraint types (Required, OneOf, Range, Pattern, Cardinality, NotBoth, RequiresTogether, Equals, NotEquals) with 13 condition operators. `AxiomStore` provides validation, statistics tracking, and `AxiomsLoaded` event publishing for hot-reload. License-gated: WriterPro+ for queries, Teams+ for validation.

#### Sub-Part Changelogs

| Version                             | Title                  | Status      |
| ----------------------------------- | ---------------------- | ----------- |
| [v0.4.6a](v0.4.x/LCS-CL-v0.4.6a.md) | Reference Panel View   | ✅ Complete |
| [v0.4.6b](v0.4.x/LCS-CL-v0.4.6b.md) | Search Result Item     | ✅ Complete |
| [v0.4.6c](v0.4.x/LCS-CL-v0.4.6c.md) | Source Navigation      | ✅ Complete |
| [v0.4.6d](v0.4.x/LCS-CL-v0.4.6d.md) | Search History Service | ✅ Complete |
| [v0.4.6e](v0.4.x/LCS-CL-v0.4.6e.md) | Axiom Data Model       | ✅ Complete |
| [v0.4.6f](v0.4.x/LCS-CL-v0.4.6f.md) | Axiom Repository       | ✅ Complete |
| [v0.4.6g](v0.4.x/LCS-CL-v0.4.6g.md) | Axiom Loader           | ✅ Complete |
| [v0.4.6h](v0.4.x/LCS-CL-v0.4.6h.md) | Axiom Query API        | ✅ Complete |

---

## [v0.4.5] - 2026-02 (In Progress)

### The Searcher (Semantic Query)

This release implements semantic search capabilities enabling natural language queries against the indexed document corpus.

#### What's New

- **Search Abstractions** — `ISemanticSearchService` interface defining the semantic search contract with `SearchAsync` method. `SearchOptions` record for configurable TopK, MinScore, DocumentFilter, abbreviation expansion, and embedding caching. `SearchResult` container with ranked hits, timing, and truncation metadata. `SearchHit` for individual matches with score formatting helpers (`ScorePercent`, `ScoreDecimal`) and content previews.

- **Vector Search Query** — `PgVectorSearchService` implementing `ISemanticSearchService` using pgvector's cosine similarity (`<=>` operator) for semantic search against indexed document chunks. Includes `SearchLicenseGuard` for WriterPro tier enforcement with three validation modes (throw, try/publish, property check), `IQueryPreprocessor` interface with passthrough stub, `ChunkSearchRow` for Dapper result mapping, and MediatR events (`SemanticSearchExecutedEvent`, `SearchDeniedEvent`) for telemetry and license denial tracking.

- **Query Preprocessing** — Full `QueryPreprocessor` implementation replacing the `PassthroughQueryPreprocessor` stub from v0.4.5b. Four-stage normalization pipeline (whitespace trimming, whitespace collapsing, Unicode NFC normalization, optional abbreviation expansion) with 35 technical abbreviations across 6 categories. SHA256-based query embedding caching with 5-minute sliding expiration via `IMemoryCache`. Added `ClearCache()` method to `IQueryPreprocessor` interface.

- **License Gating** — Enhanced `SearchLicenseGuard` with publicly accessible `FeatureName` and `RequiredTier` constants for UI consumers, and `GetUpgradeMessage()` method providing tier-specific upgrade guidance for the Reference Panel (v0.4.6). Added `UsedCachedEmbedding` telemetry flag to `SemanticSearchExecutedEvent` for cache efficiency tracking. Updated `PgVectorSearchService` to track and report embedding cache hits in telemetry events.

- **Graph Database Integration** — Neo4j 5.x Community Edition integration for the Knowledge Graph Foundation (CKVS Phase 1). `IGraphConnectionFactory` with license-gated session creation (Teams for write, WriterPro for read-only, Core denied). `Neo4jGraphSession` with Cypher query execution, Stopwatch timing, slow query warnings (>100ms), and exception wrapping to `GraphQueryException`. `Neo4jHealthCheck` for application health monitoring. Docker Compose Neo4j container with APOC plugin. PostgreSQL metadata tables (`GraphMetadata`, `DocumentEntities`) for cross-system linkage.

- **Schema Registry Service** — YAML-driven schema system for knowledge graph entity and relationship type governance. `ISchemaRegistry` with in-memory registry, case-insensitive lookups, and comprehensive validation producing typed error codes (13 error codes, 1 warning code). `SchemaLoader` parses YAML files with underscore naming convention and flexible From/To handling. `SchemaValidator` enforces type existence, required properties, type correctness, and constraint validation (length, pattern, numeric range). Built-in technical documentation schema with 6 entity types (Product, Component, Endpoint, Parameter, Response, Concept) and 6 relationship types.

- **Entity Abstraction Layer** — Pluggable entity extraction pipeline for identifying structured entities in document text. `IEntityExtractor` interface with priority ordering and confidence scoring. `IEntityExtractionPipeline` coordinator with error isolation, confidence filtering, mention deduplication (overlapping spans resolved by highest confidence), and entity aggregation. Three built-in regex-based extractors: `EndpointExtractor` (API endpoints with 3 patterns, confidence 0.7–1.0), `ParameterExtractor` (parameters from paths, queries, code, JSON with 5 patterns, 0.6–1.0), and `ConceptExtractor` (domain terms, acronyms, glossary entries with 4 patterns, 0.5–0.95). `MentionAggregator` groups mentions by type and normalized value into `AggregatedEntity` records for graph node creation.

#### Sub-Part Changelogs

| Version                             | Title                      | Status      |
| ----------------------------------- | -------------------------- | ----------- |
| [v0.4.5a](v0.4.x/LCS-CL-v0.4.5a.md) | Search Abstractions        | ✅ Complete |
| [v0.4.5b](v0.4.x/LCS-CL-v0.4.5b.md) | Vector Search Query        | ✅ Complete |
| [v0.4.5c](v0.4.x/LCS-CL-v0.4.5c.md) | Query Preprocessing        | ✅ Complete |
| [v0.4.5d](v0.4.x/LCS-CL-v0.4.5d.md) | License Gating             | ✅ Complete |
| [v0.4.5e](v0.4.x/LCS-CL-v0.4.5e.md) | Graph Database Integration | ✅ Complete |
| [v0.4.5f](v0.4.x/LCS-CL-v0.4.5f.md) | Schema Registry Service    | ✅ Complete |
| [v0.4.5g](v0.4.x/LCS-CL-v0.4.5g.md) | Entity Abstraction Layer   | ✅ Complete |

---

## [v0.4.4] - 2026-02 (In Progress)

### The Embedder (Vector Generation)

This release introduces the embedding infrastructure for generating semantic vectors from text, enabling similarity search and RAG capabilities.

#### What's New

- **Embedding Abstractions** — Core `IEmbeddingService` interface defining vector generation operations with single and batch embedding support. `EmbeddingOptions` configuration record with validation, `EmbeddingResult` for operation outcomes with diagnostics, and `EmbeddingException` for error classification.

- **OpenAI Connector** — `OpenAIEmbeddingService` implementation using OpenAI's text-embedding-3-small model. Features Polly-based exponential backoff retry (2^attempt seconds) for rate limits (429) and server errors (5xx). Secure API key retrieval via `ISecureVault`.

- **Token Counting** — `ITokenCounter` interface with `TiktokenTokenCounter` implementation using cl100k_base encoding. Provides accurate token counting, truncation with word boundary preservation, and encode/decode round-trip support.

- **Document Indexing Pipeline** — `DocumentIndexingPipeline` orchestrates the complete indexing workflow: chunk → validate tokens → embed (batched) → store. Features license gating (WriterPro), MediatR events (`DocumentIndexedEvent`, `DocumentIndexingFailedEvent`), and graceful error handling.

#### Sub-Part Changelogs

| Version                             | Title                  | Status      |
| ----------------------------------- | ---------------------- | ----------- |
| [v0.4.4a](v0.4.x/LCS-CL-v0.4.4a.md) | Embedding Abstractions | ✅ Complete |
| [v0.4.4b](v0.4.x/LCS-CL-v0.4.4b.md) | OpenAI Connector       | ✅ Complete |
| [v0.4.4c](v0.4.x/LCS-CL-v0.4.4c.md) | Token Counting         | ✅ Complete |
| [v0.4.4d](v0.4.x/LCS-CL-v0.4.4d.md) | Embedding Pipeline     | ✅ Complete |

---

## [v0.4.3] - 2026-02 (In Progress)

### The Splitter (Chunking Strategies)

This release introduces a flexible chunking system for breaking documents into semantically meaningful text segments suitable for embedding.

#### What's New

- **Chunking Abstractions** — Core interfaces, records, and enums defining the chunking contract. `IChunkingStrategy` enables pluggable algorithms, `TextChunk` and `ChunkMetadata` carry output with position and context, `ChunkingOptions` provides configurable behavior with validation, and `ChunkingPresets` offers ready-to-use configurations for common use cases.

- **Fixed-Size Chunker** — `FixedSizeChunkingStrategy` splits text into chunks of configurable target size with overlap for context continuity. Two-phase word boundary search (backward 20%, forward 10%) avoids mid-word splits. Supports whitespace trimming, Unicode characters, and proper metadata (Index, TotalChunks, IsFirst/IsLast).

- **Paragraph Chunker** — `ParagraphChunkingStrategy` splits text on paragraph boundaries (double newlines). Short paragraphs are merged until reaching `TargetSize`, while oversized paragraphs use `FixedSizeChunkingStrategy` as fallback.

#### Sub-Part Changelogs

| Version                             | Title                   | Status      |
| ----------------------------------- | ----------------------- | ----------- |
| [v0.4.3a](v0.4.x/LCS-CL-v0.4.3a.md) | Chunking Abstractions   | ✅ Complete |
| [v0.4.3b](v0.4.x/LCS-CL-v0.4.3b.md) | Fixed-Size Chunker      | ✅ Complete |
| [v0.4.3c](v0.4.x/LCS-CL-v0.4.3c.md) | Paragraph Chunker       | ✅ Complete |
| [v0.4.3d](v0.4.x/LCS-CL-v0.4.3d.md) | Markdown Header Chunker | ✅ Complete |

---

## [v0.3.8] - 2026-01 (In Progress)

### The Hardening (Unit Testing)

This release introduces comprehensive unit testing to verify algorithm accuracy and prevent regressions.

#### What's New

- **Fuzzy Algorithm Accuracy Tests** — 20 tests verifying Levenshtein distance calculations including symmetry, edge cases, known distance values, and Unicode handling.

- **Threshold Behavior Tests** — 14 tests covering boundary precision, threshold sweep at 0-100%, and partial ratio matching behavior.

- **Typo Detection Pattern Tests** — 22 tests validating real-world typo detection including substitution, transposition, omission, insertion, and common misspelling patterns.

- **Scanner Accuracy Tests** — 5 new tests for multi-term scanning, position calculation, and ratio reporting accuracy.

- **Readability Corpus Infrastructure** — Standard corpus with 5 entries (Gettysburg Address, simple children's text, academic prose, Hemingway-style, mixed complexity) containing pre-calculated reference values for validation.

- **Syllable Counter Accuracy Tests** — 19 tests validating syllable counting against CMU dictionary reference words, corpus entries, silent 'e' patterns, and compound words.

- **Sentence Tokenizer Accuracy Tests** — 17 tests verifying sentence and word count extraction accuracy across corpus entries.

- **Readability Formula Accuracy Tests** — 50 tests ensuring Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease calculations match reference values within tolerance.

- **Passive Voice Detection Accuracy Tests** — 9 new tests validating passive voice detection with 95% accuracy threshold on labeled corpus. Includes false positive prevention for linking verbs, adjectives, and active progressive patterns.

- **Weak Word Scanner Enhanced Tests** — 8 new tests verifying intensity adverbs, hedging words, and vague qualifier detection with word boundary matching.

- **LabeledSentenceCorpus Test Fixture** — Reusable corpus with 8 categories (ClearPassive, ClearActive, LinkingVerb, AdjectiveComplement, ProgressiveActive, GetPassive, ModalPassive, EdgeCase) for accuracy testing.

- **Performance Benchmark Baseline** — BenchmarkDotNet integration with benchmarks for Readability, Fuzzy Scanning, Voice Analysis, and Full Pipeline operations. Includes 32 CI threshold tests with 10% regression tolerance and throughput validation.

#### Sub-Part Changelogs

| Version                                    | Title                    | Status      |
| ------------------------------------------ | ------------------------ | ----------- |
| [v0.3.8a](v0.3.x/v0.3.8/LCS-CL-v0.3.8a.md) | Fuzzy Test Suite         | ✅ Complete |
| [v0.3.8b](v0.3.x/v0.3.8/LCS-CL-v0.3.8b.md) | Readability Test Suite   | ✅ Complete |
| [v0.3.8c](v0.3.x/v0.3.8/LCS-CL-v0.3.8c.md) | Passive Voice Test Suite | ✅ Complete |
| [v0.3.8d](v0.3.x/v0.3.8/LCS-CL-v0.3.8d.md) | Benchmark Baseline       | ✅ Complete |

---

## [v0.3.1] - 2026-01 (In Progress)

### The Fuzzy Engine (Advanced Matching)

This release introduces fuzzy string matching to detect typos and variations of forbidden terminology.

#### What's New

- **Fuzzy Match Service** — New `IFuzzyMatchService` provides Levenshtein distance-based string matching using FuzzySharp. Normalized inputs (trimmed, lowercase) ensure consistent matching. Thread-safe and stateless.

- **Similarity Calculation** — `CalculateRatio()` returns 0-100 similarity percentage. `CalculatePartialRatio()` finds best substring matches for pattern detection within longer text.

- **Threshold-Based Matching** — `IsMatch()` convenience method combines ratio calculation with threshold comparison. Threshold specified as decimal (0.0 to 1.0).

- **Fuzzy Term Properties** — `StyleTerm` entity now includes `FuzzyEnabled` and `FuzzyThreshold` properties for per-term fuzzy matching configuration. Default threshold is 80%.

- **Fuzzy Repository Methods** — `ITerminologyRepository` extended with `GetFuzzyEnabledTermsAsync()` for cached fuzzy term retrieval and `InvalidateFuzzyTermsCache()` for explicit cache control.

- **Inclusive Language Defaults** — Seeder includes 5 fuzzy-enabled terms for inclusive language (whitelist→allowlist, blacklist→denylist, master→main, slave→replica, sanity check→confidence check).

- **Fuzzy Scanner** — New `IFuzzyScanner` service scans documents for approximate terminology matches. Tokenizes text preserving hyphenated words, applies per-term thresholds, and prevents double-counting of exact matches. Requires Writer Pro license.

- **StyleViolation Fuzzy Metadata** — `StyleViolation` now includes `IsFuzzyMatch` flag and `FuzzyRatio` percentage for fuzzy match identification.

#### Sub-Part Changelogs

| Version                                 | Title                 | Status      |
| --------------------------------------- | --------------------- | ----------- |
| [v0.3.1a](v0.3.x/v0.3.1/LCS-CL-031a.md) | Algorithm Integration | ✅ Complete |
| [v0.3.1b](v0.3.x/v0.3.1/LCS-CL-031b.md) | Repository Update     | ✅ Complete |
| [v0.3.1c](LCS-CL-031c.md)               | The Fuzzy Scanner     | ✅ Complete |
| [v0.3.1d](LCS-CL-031d.md)               | License Gating        | ✅ Complete |

---

## [v0.3.3] - 2026-01 (In Progress)

### The Readability Engine (Text Analysis)

This release introduces algorithmic readability analysis to compute text complexity metrics.

#### What's New

- **Sentence Tokenizer** — New `ISentenceTokenizer` splits text into sentences while respecting abbreviations (Mr., Dr., Inc., U.S.A., etc.). Dictionary of 50+ common abbreviations avoids false sentence breaks. Handles ellipsis, initials, and edge cases.

- **Syllable Counter** — New `ISyllableCounter` counts syllables using vowel-group heuristics and an exception dictionary of 40+ irregular words. Applies rules for silent 'e', '-ed', and '-es' suffixes. Provides `IsComplexWord()` for Gunning Fog Index calculation (3+ meaningful syllables, excluding common suffix inflation).

- **Readability Calculator** — New `IReadabilityService` computes Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease scores. Uses Sentence Tokenizer and Syllable Counter for accurate analysis. Includes `ReadabilityMetrics` record with computed properties and human-readable interpretation of reading ease.

- **Readability HUD Widget** — New `ReadabilityHudViewModel` and `ReadabilityHudView` display real-time readability metrics in the Problems Panel header. Features color-coded grade levels, fog index, and reading ease with tooltip descriptions. Writer Pro tier license gating via `FeatureCodes.ReadabilityHud`.

#### Sub-Part Changelogs

| Version                                 | Title                  | Status      |
| --------------------------------------- | ---------------------- | ----------- |
| [v0.3.3a](v0.3.x/v0.3.3/LCS-CL-033a.md) | Sentence Tokenizer     | ✅ Complete |
| [v0.3.3b](v0.3.x/v0.3.3/LCS-CL-033b.md) | Syllable Counter       | ✅ Complete |
| [v0.3.3c](v0.3.x/v0.3.3/LCS-CL-033c.md) | Readability Calculator | ✅ Complete |
| [v0.3.3d](v0.3.x/v0.3.3/LCS-CL-033d.md) | HUD Widget             | ✅ Complete |

---

## [v0.3.4] - 2026-01 (In Progress)

### The Writing Coach (Voice Profiles)

This release introduces Voice Profiles for context-aware style enforcement based on content type.

#### What's New

- **Voice Profile Definition** — New `VoiceProfile` record defines target style constraints including grade level, sentence length, passive voice tolerance, and adverb/weasel word flagging. Includes validation for constraint boundaries.

- **Built-In Profiles** — 5 predefined profiles tailored to different writing styles:
    - **Technical** — Direct, precise documentation (Grade 11, no passive voice, flags adverbs/weasel words)
    - **Marketing** — Engaging, persuasive copy (Grade 9, allows passive voice, flags weasel words)
    - **Academic** — Formal, scholarly writing (Grade 13, allows passive voice, no style flags)
    - **Narrative** — Creative, flowing prose (Grade 9, allows passive voice, no style flags)
    - **Casual** — Conversational, friendly content (Grade 7, allows passive voice, no style flags)

- **Voice Profile Service** — New `IVoiceProfileService` manages profile selection and CRUD operations. Includes in-memory caching, license gating for custom profiles (Teams+), and MediatR event publishing on profile changes.

- **Profile Changed Event** — New `ProfileChangedEvent` notification enables reactive UI updates when the active profile changes.

- **Passive Voice Detector** — New `IPassiveVoiceDetector` uses regex pattern matching with confidence scoring to identify passive constructions (to-be, modal, progressive, get-passive). Distinguishes true passive voice from predicate adjectives like "The door is closed" using a confidence threshold (≥0.5).

- **Weak Word Scanner** — New `IWeakWordScanner` detects adverbs (~40), weasel words (~30), and filler expressions (~15) in text. Respects Voice Profile settings for selective flagging while always detecting fillers. Provides specific suggestions for over 50 common weak words.

- **Profile Selector UI** — New `ProfileSelectorWidget` in the status bar displays the active voice profile name with rich tooltips. Context menu enables quick profile switching. Auto-initializes on load with graceful error handling.

#### Sub-Part Changelogs

| Version                                 | Title                    | Status      |
| --------------------------------------- | ------------------------ | ----------- |
| [v0.3.4a](v0.3.x/v0.3.4/LCS-CL-034a.md) | Voice Profile Definition | ✅ Complete |
| [v0.3.4b](v0.3.x/v0.3.4/LCS-CL-034b.md) | Passive Voice Detector   | ✅ Complete |
| [v0.3.4c](v0.3.x/v0.3.4/LCS-CL-034c.md) | Weak Word Scanner        | ✅ Complete |
| [v0.3.4d](v0.3.x/v0.3.4/LCS-CL-034d.md) | Profile Selector UI      | ✅ Complete |

---

## [v0.3.5] - 2026-01 (In Progress)

### The Resonance Dashboard (Writing Metrics Visualization)

This release introduces the charting infrastructure for visualizing writing metrics in a spider chart format.

#### What's New

- **Chart Data Service** — New `IChartDataService` aggregates metrics from readability, passive voice, and weak word services into normalized 0-100 scale data points. Thread-safe caching with event notification on recomputation.

- **Axis Configuration** — New `IResonanceAxisProvider` and `DefaultAxisProvider` define 6 axes for the spider chart: Readability (Flesch Reading Ease), Clarity (Passive Voice%), Precision (Weak Word%), Accessibility (FK Grade), Density (Words/Sentence), and Flow (Sentence Variance).

- **Normalization Logic** — `ResonanceAxisDefinition` record handles value normalization with clamping, configurable ranges, and inverted scales for metrics where "lower is better" (passive voice, weak words, grade level).

- **Theme Configuration** — `ChartThemeConfiguration` provides light and dark color palettes for chart rendering, integrated with Lexichord's theme system.

- **LiveCharts2 Integration** — New dependency on `LiveChartsCore.SkiaSharpView.Avalonia` (2.0.0-rc6.1) for high-performance Avalonia-native charting. Registered in `StyleModule`.

- **Spider Chart with Target Overlay** — New `ResonanceDashboardView` displays writing metrics on an interactive polar chart. Features current values (solid fill) and target overlay (dashed line) based on active Voice Profile. Includes `ISpiderChartSeriesBuilder` for theme-aware series construction and `ITargetOverlayService` for profile-based target computation with caching.

- **Real-Time Chart Updates** — New `IResonanceUpdateService` provides a reactive pipeline for chart updates. Uses Rx-based debouncing (300ms) to coalesce rapid analysis events while providing immediate response for profile changes. License-gated to Writer Pro tier.

#### Sub-Part Changelogs

| Version                                 | Title                   | Status      |
| --------------------------------------- | ----------------------- | ----------- |
| [v0.3.5a](v0.3.x/v0.3.5/LCS-CL-035a.md) | Charting Infrastructure | ✅ Complete |
| [v0.3.5b](v0.3.x/v0.3.5/LCS-CL-035b.md) | Spider Chart            | ✅ Complete |
| [v0.3.5c](v0.3.x/v0.3.5/LCS-CL-035c.md) | Real-Time Updates       | ✅ Complete |
| [v0.3.5d](v0.3.x/v0.3.5/LCS-CL-035d.md) | Target Overlays         | ✅ Complete |

---

## [v0.3.6] - 2026-01 (In Progress)

### The Global Dictionary (Project Settings)

This release introduces project-level configuration management with hierarchical settings.

#### What's New

- **Layered Configuration Provider** — New `ILayeredConfigurationProvider` merges configuration from three sources (System → User → Project) with "higher wins" precedence. Enables project-specific style customization stored in `.lexichord/style.yaml`.

- **Style Configuration Record** — New `StyleConfiguration` record captures all style settings including readability constraints, voice analysis thresholds, and terminology overrides. Immutable design ensures thread safety.

- **License-Gated Project Config** — Project-level configuration is gated behind Writer Pro license (`Feature.GlobalDictionary`). Core users receive merged System + User configuration only.

- **Configuration Caching** — 5-second cache with manual invalidation via `InvalidateCache()`. Reduces file I/O overhead for frequently accessed configuration.

- **Security Safeguards** — 100KB file size limit prevents denial-of-service via oversized config files. Graceful YAML parse error handling with fallback to defaults.

- **Conflict Resolution** — New `IConflictResolver` detects and logs configuration conflicts between layers. Project-wins semantics ensure higher-priority sources always override. Term override logic handles exclusions vs additions with repository fallback. Rule ignore patterns support wildcards (`PASSIVE-*`, `*-WARNINGS`).

- **Override UI Infrastructure** — New `IProjectConfigurationWriter` enables users to ignore rules and exclude terms via project configuration. Atomic YAML writes prevent file corruption. `OverrideMenuViewModel` provides license-gated commands for context menu integration.

- **Ignored Files** — New `IIgnorePatternService` enables `.lexichordignore` files with glob pattern support (`*.log`, `build/**`, `**/temp/**`). Negation patterns (`!important.log`) un-ignore specific files. License-gated limits (Core: 5 patterns, Writer Pro: unlimited). Hot-reload via FileSystemWatcher with 100ms debounce.

#### Sub-Part Changelogs

| Version                                 | Title                 | Status      |
| --------------------------------------- | --------------------- | ----------- |
| [v0.3.6a](v0.3.x/v0.3.6/LCS-CL-036a.md) | Layered Configuration | ✅ Complete |
| [v0.3.6b](v0.3.x/v0.3.6/LCS-CL-036b.md) | Conflict Resolution   | ✅ Complete |
| [v0.3.6c](v0.3.x/v0.3.6/LCS-CL-036c.md) | Override UI           | ✅ Complete |
| [v0.3.6d](v0.3.x/v0.3.6/LCS-CL-036d.md) | Ignored Files         | ✅ Complete |

---

## [v0.3.7] - 2026-01 (In Progress)

### The Performance Tuning (Async Pipelines)

This release introduces infrastructure for optimizing typing responsiveness with reactive request buffering.

#### What's New

- **Analysis Request Buffering** — New `IAnalysisBuffer` service debounces document analysis requests using System.Reactive. Per-document debouncing ensures rapid edits only trigger analysis after a configurable idle period (default 300ms). Latest-wins semantics automatically discard intermediate requests.

- **Parallel Scanner Execution** — New `IParallelAnalysisPipeline` executes all four scanners (Regex, Fuzzy, Readability, Voice) concurrently via `Task.WhenAll()`. Reduces analysis latency from sequential sum to longest scanner duration. Error isolation ensures partial results when individual scanners fail.

- **Snapshot Semantics** — New `AnalysisRequest` record captures document content at request time with associated cancellation token. Immutable design ensures thread-safe sharing across the async pipeline.

- **Configurable Buffer Options** — New `AnalysisBufferOptions` configuration class controls idle period timing, maximum buffered documents, and enable/disable toggle for bypass mode.

- **Problems Panel Virtualization** — Added `ScrollOffset` property to preserve scroll position during list updates. Virtualization diagnostics logging helps monitor performance. Panel efficiently handles 5,000+ violations via collapsed severity groups.

- **Memory Leak Prevention** — New `DisposableViewModel` base class with automatic subscription cleanup via Composite Disposable pattern. Thread-safe `IDisposableTracker` ensures proper disposal on document close, preventing memory growth during long sessions.

#### Sub-Part Changelogs

| Version                                    | Title                  | Status      |
| ------------------------------------------ | ---------------------- | ----------- |
| [v0.3.7a](v0.3.x/v0.3.7/LCS-CL-037a.md)    | Background Buffering   | ✅ Complete |
| [v0.3.7b](v0.3.x/v0.3.7/LCS-CL-037b.md)    | Parallelization        | ✅ Complete |
| [v0.3.7c](v0.3.x/v0.3.7/LCS-CL-037c.md)    | Virtualization         | ✅ Complete |
| [v0.3.7d](v0.3.x/v0.3.7/LCS-CL-v0.3.7d.md) | Memory Leak Prevention | ✅ Complete |

---

## [v0.2.7] - 2026-01 (In Progress)

### The Turbo (Performance Optimization)

This release focuses on performance optimizations to ensure smooth UI responsiveness during intensive linting operations.

#### What's New

- **Async Offloading** — CPU-intensive regex scanning now runs on background threads via `Task.Run`, ensuring the UI never freezes during document analysis. Thread marshalling ensures violation updates are safely dispatched to the UI thread.

- **Thread Marshaller Abstraction** — New `IThreadMarshaller` interface abstracts Avalonia's `Dispatcher` for testable thread-aware code. Methods for async invocation, fire-and-forget posting, and DEBUG-only thread assertions.

- **Code Block Ignoring** — Markdown code blocks (fenced and inline) are now excluded from style linting, preventing false positives when code samples contain terms like "whitelist" or technical jargon. State machine detects fenced blocks (``` and ~~~), regex handles inline code spans.

- **Frontmatter Ignoring** — YAML, TOML, and JSON frontmatter at the start of Markdown documents is now automatically excluded from style scanning. Supports UTF-8 BOM handling, Windows/Unix line endings, and unclosed frontmatter detection. Priority ordering ensures frontmatter is detected before code blocks.

- **Content Filter Architecture** — New `IContentFilter` interface enables pluggable pre-scan content filtering. Binary-search-based match filtering ensures O(log n) performance for large documents.

- **Large File Stress Testing** — Performance testing infrastructure validates linting on 5MB+ documents. Chunked scanning with viewport prioritization (<100ms viewport response), adaptive debounce monitoring (200ms-1000ms), synthetic test corpus generation, and comprehensive stress test suite.

- **Module Version Update** — Style module version updated from 0.2.6 to 0.2.7.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.2.7a](v0.2.x/LCS-CL-027a.md) | Async Offloading       | ✅ Complete |
| [v0.2.7b](v0.2.x/LCS-CL-027b.md) | Code Block Ignoring    | ✅ Complete |
| [v0.2.7c](v0.2.x/LCS-CL-027c.md) | Frontmatter Ignoring   | ✅ Complete |
| [v0.2.7d](v0.2.x/LCS-CL-027d.md) | Large File Stress Test | ✅ Complete |

---

## [v0.2.6] - 2026-01 (In Progress)

### The Sentinel (Real-Time Feedback Sidebar)

This release introduces **The Sentinel**, a Problems Panel sidebar providing centralized visibility into style violations during editing.

#### What's New

- **Problems Panel** — Sidebar view displaying style violations grouped by severity (Error, Warning, Info, Hint). Shows header with scope badge ("Current File"), severity count badges, loading indicator, and empty state. Collapsible groups with violation details (location, message, rule ID).

- **Linting Event Integration** — Subscribes to `LintingCompletedEvent` via MediatR to receive real-time violation updates. Clears previous violations on new events, transforms aggregated violations to display items, and updates counts via PropertyChanged.

- **Severity Grouping** — Violations organized into four severity groups with Unicode icons (⛔ Error, ⚠ Warning, ℹ Info, 💡 Hint). Groups are collapsible with expand/collapse state tracking.

- **Navigation Sync** — Double-click any problem item to navigate directly to its location in the editor. The editor scrolls to the target line, positions the caret at the exact column, and applies a temporary highlight animation (2 second fade). Supports navigation within the active document.

- **Scorecard Widget** — Gamification widget displaying compliance score (0-100%), letter grade (A-F), and trend indicator. Score calculated from weighted penalties: errors (5 pts), warnings (2 pts), info (0.5 pts). Grade colors and trend arrows provide instant visual feedback on document health.

- **Dock Region Integration** — ProblemsPanelView registered in Right dock region alongside LexiconView.

#### Sub-Part Changelogs

| Version                          | Title          | Status      |
| -------------------------------- | -------------- | ----------- |
| [v0.2.6a](v0.2.x/LCS-CL-026a.md) | Problems Panel | ✅ Complete |
| [v0.2.6b](v0.2.x/LCS-CL-026b.md) | Navigation     | ✅ Complete |
| [v0.2.6c](v0.2.x/LCS-CL-026c.md) | Scorecard      | ✅ Complete |
| [v0.2.6d](v0.2.x/LCS-CL-026d.md) | Filter Scope   | ✅ Complete |

---

## [v0.2.5] - 2026-01 (In Progress)

### The Librarian (Terminology Management)

This release introduces **The Librarian**, a grid-based UI for managing style terminology with license-gated editing.

#### What's New

- **Terminology Grid View** — Avalonia DataGrid for viewing and managing style terms. Supports sorting by severity and category, row selection, and context menu operations.

- **Search & Filter** — Real-time filtering with debounced search (300ms) across term, replacement, and notes fields. Includes inactive term toggle, category dropdown, and severity dropdown. All filters apply as AND logic.

- **Term Editor Dialog** — Modal dialog for adding and editing terminology rules. Features real-time regex validation, pattern testing against sample text, MatchCase toggle for case-sensitive matching, and dirty state tracking.

- **Bulk Import/Export** — CSV and Excel import with validation, duplicate detection, and progress reporting. JSON and CSV export with optional metadata and category filtering. Protects against oversized files (10MB max) and excessive rows (10,000 max).

- **License-Gated Editing** — Edit, delete, and toggle active commands require WriterPro tier or higher. Copy Pattern is available to all tiers. Status bar displays current license tier with read-only indicator for lower tiers.

- **Default Sorting** — Terms are sorted by severity (Error → Warning → Suggestion → Info) then by category alphabetically, prioritizing the most critical issues.

- **Dock Region Integration** — LexiconView is registered in the Right dock region, accessible alongside the document editor.

#### Sub-Part Changelogs

| Version                          | Title                 | Status      |
| -------------------------------- | --------------------- | ----------- |
| [v0.2.5a](v0.2.x/LCS-CL-025a.md) | Terminology Grid View | ✅ Complete |
| [v0.2.5b](v0.2.x/LCS-CL-025b.md) | Search & Filter       | ✅ Complete |
| [v0.2.5c](v0.2.x/LCS-CL-025c.md) | Term Editor Dialog    | ✅ Complete |
| [v0.2.5d](v0.2.x/LCS-CL-025d.md) | Bulk Import/Export    | ✅ Complete |

---

## [v0.2.2] - 2026-01 (In Progress)

### The Lexicon (Terminology Database)

This release introduces **The Lexicon**, a persistent database for managing style terminology with full CRUD operations.

#### What's New

- **Style Terms Schema** — Database table for storing terminology entries within style sheets. Supports term-replacement pairs, categorization, severity levels, and soft-disable via `IsActive` flag. Includes fuzzy search via PostgreSQL's `pg_trgm` extension.

- **Terminology Repository** — High-performance data access layer with `IMemoryCache` integration. Provides O(1) active term lookups via cached HashSet, with automatic cache invalidation on write operations.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.2.2a](v0.2.x/LCS-CL-022a.md) | Style Schema Migration | ✅ Complete |
| [v0.2.2b](v0.2.x/LCS-CL-022b.md) | Repository Layer       | ✅ Complete |
| [v0.2.2c](v0.2.x/LCS-CL-022c.md) | Terminology Seeding    | ✅ Complete |
| [v0.2.2d](v0.2.x/LCS-CL-022d.md) | CRUD Service           | ✅ Complete |

---

## [v0.2.3] - 2026-01 (In Progress)

### The Critic (Linter Engine)

This release introduces **The Critic**, a reactive linting engine that provides real-time document analysis.

#### What's New

- **Reactive Pipeline** — System.Reactive-based infrastructure for debounced, concurrent document linting. Documents subscribe to the orchestrator and receive throttled analysis with configurable debounce (300ms default) and concurrency limits (2 concurrent scans default).

- **Linting Orchestrator** — Central hub managing document subscriptions, coordinating scans with IStyleEngine, and publishing results via observable stream and MediatR domain events.

- **Domain Contracts** — Immutable records for linting configuration (`LintingOptions`), per-document state tracking (`DocumentLintState`), and operation results (`LintResult`) with factory methods.

- **Violation Aggregator** — Transforms raw scanner matches into user-facing violations with line/column positions, deduplication (higher severity wins), sorting by document position, and per-document caching for efficient query support.

#### Sub-Part Changelogs

| Version                          | Title                | Status      |
| -------------------------------- | -------------------- | ----------- |
| [v0.2.3a](v0.2.x/LCS-CL-023a.md) | Reactive Pipeline    | ✅ Complete |
| [v0.2.3b](v0.2.x/LCS-CL-023b.md) | Debounce Logic       | ✅ Complete |
| [v0.2.3c](v0.2.x/LCS-CL-023c.md) | Scanner (Regex)      | ✅ Complete |
| [v0.2.3d](v0.2.x/LCS-CL-023d.md) | Violation Aggregator | ✅ Complete |

---

## [v0.2.1] - 2026-01 (In Progress)

### The Rulebook (Style Module)

This release introduces **The Rulebook**, the Style Module that enables governed writing environments with consistent style enforcement.

#### What's New

- **Style Module Foundation** — New `Lexichord.Modules.Style` project establishing the infrastructure for style checking. Implements `IModule` with "The Rulebook" identity and proper service registration.

- **IStyleEngine Interface** — Core contract for style analysis. Features active style sheet management, thread-safe analysis methods, and event-driven sheet change notifications.

- **Rule Object Model** — Full domain object implementations for style governance:
    - `RuleCategory` enum: Terminology, Formatting, Syntax
    - `ViolationSeverity` enum: Error, Warning, Info, Hint (ordered by importance)
    - `PatternType` enum: Regex, Literal, LiteralIgnoreCase, StartsWith, EndsWith, Contains
    - `StyleRule` record with `FindViolationsAsync()`, lazy regex compilation (100ms timeout), position calculation
    - `StyleViolation` record with line/column positions, `GetSurroundingContext()` for match preview
    - `StyleSheet` record with `MergeWith()` for rule inheritance, filtering methods, and `Empty` singleton

- **YAML Deserializer** — Full YamlDotNet integration for loading style sheets:
    - YAML DTOs with snake_case to PascalCase mapping
    - Schema validation with helpful error messages (required fields, kebab-case IDs, enum validation)
    - Embedded `lexichord.yaml` with 26 default rules across Terminology, Formatting, and Syntax categories
    - `ValidateYaml()` for editor-side syntax checking before save

- **Configuration Watcher** — Live reload of `.lexichord/style.yaml` without restart:
    - `FileSystemWatcher` with 300ms debouncing for rapid save handling
    - License-gated to WriterPro tier and above
    - Graceful fallback keeps previous valid rules on YAML errors
    - MediatR events (`StyleSheetReloadedEvent`, `StyleWatcherErrorEvent`) for system-wide notification

#### Philosophy: Concordance

> "Rules over improvisation"

The Rulebook provides governed writing environments where style guides are applied consistently across all content. Writers focus on content while the system enforces house style.

#### Sub-Part Changelogs

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.2.1a](v0.2.x/LCS-CL-021a.md) | Module Scaffolding | ✅ Complete |
| [v0.2.1b](v0.2.x/LCS-CL-021b.md) | Rule Object Model  | ✅ Complete |
| [v0.2.1c](v0.2.x/LCS-CL-021c.md) | YAML Deserializer  | ✅ Complete |
| [v0.2.1d](v0.2.x/LCS-CL-021d.md) | Hot Reload         | ✅ Complete |

---

## [v0.1.7] - 2026-01 (In Progress)

### Distribution & Updates

This release integrates Velopack for native platform packaging and auto-updates.

#### What's New

- **Velopack Integration** — Native auto-update framework replacing stub implementation. Supports delta updates for efficient downloads, channel-specific update feeds (Stable/Insider), and in-app update installation with automatic restart.

- **Update Service Enhancements** — Extended `IUpdateService` with real download/apply functionality including progress reporting, ready-state tracking, and platform-aware behavior (skips updates in development mode).

- **Build Scripts** — Cross-platform packaging scripts (`pack-windows.ps1`, `pack-macos.sh`) for creating distributable installers via Velopack CLI.

- **Signing Infrastructure** — CI pipeline for code signing Windows executables (PFX/SignTool) and macOS applications (Developer ID/Notarization). Signed binaries avoid SmartScreen and Gatekeeper security warnings.

- **Release Notes Viewer** — Automatic display of `CHANGELOG.md` after updates. First-run detection compares stored vs. current version, opening release notes in a new editor tab. Settings persist to track run history and installation ID.

- **Telemetry Hooks** — Opt-in crash reporting via Sentry SDK integration. User-configurable toggle in Settings > Privacy with clear data disclosure. Robust PII scrubbing ensures file paths and email addresses are never transmitted. Disabled by default until explicitly enabled.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.1.7a](v0.1.x/LCS-CL-017a.md) | Velopack Integration   | ✅ Complete |
| [v0.1.7b](v0.1.x/LCS-CL-017b.md) | Signing Infrastructure | ✅ Complete |
| [v0.1.7c](v0.1.x/LCS-CL-017c.md) | Release Notes Viewer   | ✅ Complete |
| [v0.1.7d](v0.1.x/LCS-CL-017d.md) | Telemetry Hooks        | ✅ Complete |

---

## [v0.1.6] - 2026-01 (In Progress)

### The Settings Framework

This release establishes the centralized settings UI framework, enabling modules to contribute configuration pages to a unified Settings dialog, with live theme preview as the first module-contributed settings page.

#### What's New

- **Settings Page Registry** — Central registry for module-contributed settings pages with hierarchical organization, license tier filtering, and full-text search capabilities.

- **Settings ViewModel** — State management for the Settings dialog with tree navigation, search filtering, error handling for page loading failures, and MediatR event integration.

- **Settings Window** — Modal dialog with split-panel layout (220px navigation tree, flexible content area), search box, TreeView navigation, and keyboard shortcuts (Ctrl+,, Escape).

- **Live Theme Preview** — Real-time theme switching from the Appearance settings page. Select Light, Dark, or System themes with instant visual feedback. OS theme synchronization for System mode.

- **License Management UI** — Account settings page for license activation and tier management. Three-stage validation (format, checksum, server), secure key storage via `ISecureVault`, tier-colored badges, expiration warnings, and feature availability matrix.

- **Update Channel Selector** — Updates settings page for switching between Stable and Insider channels. Assembly-driven version display, manual update checking (stub), and local settings persistence.

- **Module Integration** — Modules implement `ISettingsPage` interface and register via `ISettingsPageRegistry` during initialization. Pages support hierarchical nesting via `ParentCategoryId`.

#### Sub-Part Changelogs

| Version                   | Title                     | Status      |
| ------------------------- | ------------------------- | ----------- |
| [v0.1.6a](LCS-CL-016a.md) | Settings Dialog Framework | ✅ Complete |
| [v0.1.6b](LCS-CL-016b.md) | Live Theme Preview        | ✅ Complete |
| [v0.1.6c](LCS-CL-016c.md) | License Management UI     | ✅ Complete |
| [v0.1.6d](LCS-CL-016d.md) | Update Channel Selector   | ✅ Complete |

---

## [v0.1.5] - 2026-01 (In Progress)

### The Conductor's Baton (Command Palette)

This release establishes the keyboard-centric command system that enables rapid access to all application functionality through commands, shortcuts, and a searchable palette.

#### What's New

- **Command Registry** — Centralized system for registering, querying, and executing application commands. Commands use `module.action` naming convention with metadata for display titles, categories, icons, and keyboard shortcuts.

- **Command Palette UI** — Keyboard-centric modal overlay for discovering and executing commands. Features fuzzy search via FuzzySharp, keyboard navigation (↑/↓, Page Up/Down, Home/End), and mode switching between Commands and Files. Shortcuts: Ctrl+Shift+P (commands), Ctrl+P (files).

- **File Jumper** — Workspace file indexing service enabling fast file search via the Command Palette. Features background indexing with progress reporting, fuzzy file name matching, LRU-based recent files tracking, and incremental updates from file system watcher events. Configurable ignore patterns and binary file filtering.

- **Keybinding Service** — User-customizable keyboard shortcuts for commands with JSON-based configuration persistence. Features conflict detection with context-aware filtering, file watching for hot-reload, and platform-agnostic gesture processing. Gestures like Ctrl+S, Ctrl+Shift+P are routed to commands registered in the Command Registry.

#### Sub-Part Changelogs

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.1.5a](LCS-CL-015a.md)        | Command Registry   | ✅ Complete |
| [v0.1.5b](LCS-CL-015b.md)        | Command Palette UI | ✅ Complete |
| [v0.1.5c](v0.1.x/LCS-CL-015c.md) | File Jumper        | ✅ Complete |
| [v0.1.5d](LCS-CL-015d.md)        | Keybinding Service | ✅ Complete |

---

## [v0.1.4] - 2026-01 (In Progress)

### Scribe (IO & Persistence)

This release establishes file I/O and document persistence capabilities, starting with dirty state tracking and atomic saves.

#### What's New

- **Dirty State Tracking** — Visual "\*" indicator in tab titles when documents have unsaved changes. Debounced updates (50ms) prevent excessive state changes during rapid typing. Content hashing enables detection when undo returns to saved state.

- **Atomic Saves** — Corruption-proof file saves using Write-Temp-Delete-Rename strategy. Three-phase execution ensures original file is preserved on any failure. Includes encoding detection, error recovery, and MediatR events for save success/failure.

- **Safe Close Workflow** — Window close interception prompts users to save unsaved changes. Modal dialog offers Save All, Discard All, or Cancel options. Prevents accidental data loss when closing with dirty documents.

- **Recent Files History** — MRU (Most Recently Used) file tracking with database persistence. Files automatically tracked when opened and displayed in "File > Open Recent" menu. Missing files shown as disabled with option to prune.

#### Sub-Part Changelogs

| Version                   | Title                | Status      |
| ------------------------- | -------------------- | ----------- |
| [v0.1.4a](LCS-CL-014a.md) | Dirty State Tracking | ✅ Complete |
| [v0.1.4b](LCS-CL-014b.md) | Atomic Saves         | ✅ Complete |
| [v0.1.4c](LCS-CL-014c.md) | Safe Close Workflow  | ✅ Complete |
| [v0.1.4d](LCS-CL-014d.md) | Recent Files History | ✅ Complete |

---

## [v0.1.3] - 2026-01 (In Progress)

### The Manuscript (Editor Module)

This release establishes the core text editing infrastructure using AvaloniaEdit, providing a high-performance editor foundation for manuscript creation and editing.

#### What's New

- **AvaloniaEdit Integration** — High-performance text editor based on AvaloniaEdit 11.1.0, supporting large documents with syntax highlighting.

- **Syntax Highlighting Service** — Theme-aware syntax highlighting for Markdown, JSON, YAML, and XML with automatic theme adaptation. XSHD definitions loaded from embedded resources with light/dark variants.

- **Search & Replace Overlay** — Inline search and replace with live highlighting, match navigation (F3/Shift+F3), and replace all. Supports case-sensitive, whole word, and regex modes with ReDoS protection.

- **ManuscriptViewModel** — Document ViewModel extending `DocumentViewModelBase` with full editor state management including caret position, selection tracking, and document statistics.

- **EditorService** — Document lifecycle management with open/create/save/close operations, thread-safe document tracking, and file path deduplication.

- **Editor Configuration** — Configurable editor settings including font family, font size, line numbers, word wrap, and whitespace display with JSON file persistence. Font fallback chain resolves missing fonts automatically.

- **Ctrl+Scroll Zoom** — Real-time font size adjustment via Ctrl+Mouse Wheel with debounced persistence. Keyboard shortcuts: Ctrl+0 (reset), Ctrl++/- (zoom in/out).

- **Keyboard Shortcuts** — Ctrl+S (save), Ctrl+F (search overlay), Ctrl+H (toggle replace), F3/Shift+F3 (find next/previous), Escape (hide search), Ctrl+0/+/- (zoom).

#### Why This Matters

This work enables:

- Professional-quality text editing experience for technical writers
- Syntax highlighting for common technical writing formats
- Foundation for search and other editor features
- Live document statistics (line/word/character counts) for writers
- Customizable editor appearance with persistent settings

#### Sub-Part Changelogs

| Version                   | Title                            | Status      |
| ------------------------- | -------------------------------- | ----------- |
| [v0.1.3a](LCS-CL-013a.md) | AvalonEdit Integration           | ✅ Complete |
| [v0.1.3b](LCS-CL-013b.md) | Syntax Highlighting              | ✅ Complete |
| [v0.1.3c](LCS-CL-013c.md) | Search Overlay                   | ✅ Complete |
| [v0.1.3d](LCS-CL-013d.md) | Editor Configuration Persistence | ✅ Complete |

---

## [v0.1.2] - 2026-01-28 (In Progress)

### The Explorer (Project Management)

This release establishes workspace and project management capabilities, enabling Lexichord to open folders and provide file system awareness.

#### What's New

- **Workspace Service** — Core service managing the current open folder state. Tracks active workspace path, persists recent workspaces (up to 10), and coordinates workspace lifecycle.

- **Workspace Events** — MediatR notifications (`WorkspaceOpenedEvent`, `WorkspaceClosedEvent`) enable cross-module reactions to workspace changes without direct dependencies.

- **Path Validation** — `WorkspaceInfo.ContainsPath()` provides security-aware path validation, preventing directory traversal attacks when filtering files.

- **Robust File System Watcher** — Production-ready file change detection with:
    - **Debouncing** — Accumulates rapid changes (default 100ms) to prevent event storms
    - **Batching** — Emits grouped changes as single events for efficiency
    - **Ignore Patterns** — Configurable glob patterns to filter `.git`, `node_modules`, temp files, etc.
    - **Buffer Overflow Detection** — Signals need for directory rescan when OS buffer overflows
    - **Error Recovery** — Automatic restart attempts (up to 3) on watcher failures

- **External File Changes Event** — `ExternalFileChangesEvent` MediatR notification enables modules to react to file system changes detected by the watcher.

- **Tree View UI (Project Explorer)** — Hierarchical file browser with:
    - **Material Design Icons** — File type icons based on extension (programming, documents, etc.)
    - **Lazy Loading** — Directory contents loaded on-demand when expanded
    - **Smart Sorting** — Directories before files, alphabetical within each category
    - **Live Updates** — Tree updates automatically when external file changes are detected
    - **MediatR Integration** — Publishes `FileOpenRequestedEvent` when files are opened

- **Context Menu Actions** — Full file and folder management in the Project Explorer:
    - **New File/Folder** — Create new items with inline name editing (Ctrl+N, Ctrl+Shift+N)
    - **Rename** — Inline renaming with validation (F2)
    - **Delete** — Remove files/folders with recursive support
    - **Reveal in Explorer** — Open containing folder in OS file browser (Windows/macOS/Linux)
    - **Protected Paths** — Prevents modification of `.git` folders and workspace root
    - **Name Validation** — Blocks invalid characters, reserved names, and path traversal

#### Why This Matters

This work enables:

1. **Project Context** — The application can track which project folder is open, enabling file-aware features.
2. **Recent Projects** — Users can quickly reopen previously used workspaces.
3. **Cross-Module Coordination** — Other modules can react to workspace and file changes via MediatR events.
4. **Reliable File Watching** — External edits (git checkout, npm install, IDE refactors) are detected reliably without event storms.
5. **File Management** — Users can create, rename, and delete files/folders without leaving the IDE.

> [!NOTE]
> For detailed technical changes, see [LCS-CL-012a.md](./LCS-CL-012a.md), [LCS-CL-012b.md](./LCS-CL-012b.md), [LCS-CL-012c.md](./LCS-CL-012c.md), and [LCS-CL-012d.md](./LCS-CL-012d.md).

---

## [v0.1.1] - 2026-01-28 (In Progress)

### The Layout Engine (Docking System)

This release establishes a flexible, IDE-like docking system using `Dock.Avalonia`. Modules can contribute documents and tool panes to resizable, draggable regions.

#### What's New

- **Dock Library Integration** — Integrated `Dock.Avalonia` to provide resizable splitters and draggable panels in the main window. Replaced static grid layout with a 4-region dock system (Left, Center, Right, Bottom).

- **Layout Abstractions** — Created `IDockFactory`, `IDocument`, `ITool`, and `DockRegionConfig` interfaces that decouple modules from the docking library implementation.

- **Region Injection Service** — Added `IRegionManager` service enabling modules to inject tool panes and documents into dock regions without direct Dock.Avalonia dependency. Includes MediatR integration for region change notifications.

- **Layout Serialization** — JSON-based layout persistence enables saving/loading workspace arrangements. Features named profiles, auto-save with debouncing, atomic file writes, and schema versioning for future migrations.

- **Tab Infrastructure** — Core abstractions for document tabs including `IDocumentTab`, `DocumentViewModelBase`, and `ITabService`. Features dirty state tracking with visual indicators, pinning with automatic tab reordering, save confirmation dialogs, and a standard IDE context menu. MediatR integration enables lifecycle notifications for close, pin, and dirty state changes.

- **MainWindowViewModel** — New ViewModel managing the dock layout lifecycle with proper DI integration.

#### Why This Matters

This work enables:

1. **Flexible Layouts** — Users can resize, drag, and rearrange panels to customize their workspace.
2. **Module Panel Registration** — Modules can contribute tool panes and documents without knowledge of the docking library.
3. **Document Management** — Documents can track unsaved changes, prompt for save on close, and respect pinned state.

> [!NOTE]
> For detailed technical changes, see [LCS-CL-011a.md](./LCS-CL-011a.md), [LCS-CL-011b.md](./LCS-CL-011b.md), [LCS-CL-011c.md](./LCS-CL-011c.md), and [LCS-CL-011d.md](./LCS-CL-011d.md).

---

## [v0.0.8] - 2026-01-29 (In Progress)

### The Hello World (Golden Skeleton)

This release establishes the first feature module as a reference implementation for all future Lexichord modules. The Status Bar module demonstrates the complete module lifecycle and introduces the Shell Region system for decoupled UI composition.

#### What's New

- **Shell Region Infrastructure** — Modules can now contribute UI to host window regions (Top, Left, Center, Right, Bottom) via `IShellRegionView` without direct host dependencies.

- **Status Bar Module** — The canonical "Golden Skeleton" reference implementation demonstrating proper module structure, service registration, and async initialization patterns.

- **Database Health Monitoring** — SQLite-based health tracking with `HealthRepository` for uptime tracking and `HeartbeatService` for 60-second heartbeat recording. Includes staleness detection and `SystemHealthChangedEvent` for cross-module health updates.

- **Vault Status Tracking** — Secure vault integration with `VaultStatusService` that verifies API key presence, handles platform support detection, and provides "Ready", "No Key", "Error", or "N/A" status display. Includes `ApiKeyDialog` for user key entry.

#### Why This Matters

This work enables:

1. **Module Development Pattern** — Future module developers have a complete working example to follow.
2. **Decoupled UI** — Modules contribute views without knowing about the host layout.
3. **End-to-End Validation** — Proves the module system works from discovery to rendering.

---

## [v0.0.7] - 2026-01-28 (In Progress)

### The Event Bus (Communication)

This release establishes MediatR-based in-process messaging that enables loose coupling between Lexichord modules. Commands, queries, and domain events now flow through a central mediator with pipeline behaviors for cross-cutting concerns.

#### What's New

- **MediatR Bootstrap** — Core messaging infrastructure with IMediator registration, assembly scanning for handler discovery, and CQRS marker interfaces (ICommand, IQuery, IDomainEvent).

- **Shared Domain Events** — DomainEventBase, ContentCreatedEvent, and SettingsChangedEvent enable modules to publish and subscribe to state changes without direct dependencies.

- **Logging Pipeline Behavior** — Automatic request/response logging with timing, slow request warnings, sensitive data redaction (`[SensitiveData]`, `[NoLog]`), and configurable thresholds.

- **Validation Pipeline Behavior** — FluentValidation integration with automatic request validation, structured error aggregation (`ValidationException`, `ValidationError`), and validator auto-discovery.

#### Why This Matters

This work enables:

1. **Loose Coupling** — Modules communicate via messages, not direct dependencies.
2. **CQRS Pattern** — Clear separation between commands (writes) and queries (reads).
3. **Extensibility** — Pipeline behaviors enable cross-cutting concerns (logging, validation).
4. **Input Validation** — Commands are validated before handlers execute, with structured error responses.

---

## [v0.0.6] - 2026-01-28 (In Progress)

### The Vault (Secure Secrets Storage)

This release establishes the secure secrets management infrastructure that protects sensitive credentials. Lexichord can now store API keys, connection strings, and OAuth tokens using OS-native encryption.

#### What's New

- **ISecureVault Interface** — Platform-agnostic contract for secure secret storage with CRUD operations, metadata access, and streaming key listing.

- **WindowsSecureVault (DPAPI)** — Windows-specific implementation using Data Protection API with user-scoped encryption, per-installation entropy, and secure file-based storage.

- **UnixSecureVault (libsecret/AES-256)** — Linux and macOS implementation with libsecret integration and robust AES-256-GCM file-based encryption fallback.

- **Integration Testing** — Comprehensive test suite verifying secrets survive restarts, CRUD lifecycle, metadata accuracy, and platform factory selection.

#### Why This Matters

This work enables:

1. **Security** — Credentials encrypted at rest using platform-native APIs.
2. **Cross-Platform** — Windows DPAPI, Linux/macOS libsecret support.
3. **Foundation** — Enables secure LLM API key storage (v0.3.x+).

---

## [v0.0.5] - 2026-01-28 (In Progress)

### The Memory (Data Layer)

This release establishes the persistent data layer that gives Lexichord its "memory." The application can now store and retrieve data across sessions using PostgreSQL.

#### What's New

- **Docker Orchestration** — A reproducible PostgreSQL 16 development environment via Docker Compose with health checks, data persistence, and optional pgAdmin administration UI.

- **Database Connector** — Npgsql-based connectivity with connection pooling and Polly resilience patterns (retry with exponential backoff, circuit breaker).

- **FluentMigrator Runner** — Database schema versioning with CLI commands (`--migrate`, `--migrate:down`, `--migrate:list`) and the initial `Migration_001_InitSystem` creating Users and SystemSettings tables.

- **Repository Base** — Generic repository pattern with Dapper for type-safe CRUD operations, including entity-specific repositories for Users and SystemSettings plus Unit of Work for transactions.

#### Why This Matters

This work enables:

1. **Data Persistence** — User data and settings survive application restarts.
2. **Schema Evolution** — Migrations enable safe database updates.
3. **Foundation** — Enables all data-dependent features (v0.0.6+).

---

## [v0.0.4] - 2026-01-28 (In Progress)

### The Module Protocol

This release establishes the module architecture that enables Lexichord's "modular monolith" — allowing features to be developed as independent modules while running in a single process.

#### What's New

- **Module Contract (`IModule`)** — A standardized interface defining how modules register services, initialize, and expose metadata.

- **Module Metadata (`ModuleInfo`)** — Immutable record containing module identity, version, author, and optional dependencies.

- **Module Loader (`IModuleLoader`)** — Discovers and loads modules from `./Modules/` at startup with two-phase loading (service registration before DI build, initialization after).

- **License Gate (Skeleton)** — Establishes license tier gating with `LicenseTier`, `ILicenseContext`, and `RequiresLicenseAttribute`. Stub implementation returns Core tier; v1.x will provide real validation.

- **Sandbox Module (Proof of Concept)** — First feature module validating the module architecture. Demonstrates discovery, service registration, and async initialization.

#### Why This Matters

This work enables:

1. **Extensibility** — Third-party and internal modules can be developed independently.
2. **Feature Isolation** — Each module encapsulates its own services and state.
3. **License Gating** — Future versions will enable per-module licensing tiers.

---

## [v0.0.3] - 2026-01-28 (In Progress)

### The Nervous System (Logging & DI)

This release establishes the runtime infrastructure that enables the application to "think" and report errors. This transforms the Avalonia shell into a properly instrumented, dependency-injectable application.

#### What's New

- **Dependency Injection Container** — Microsoft.Extensions.DependencyInjection is now the sole IoC container. All services are resolved from the DI container instead of manual instantiation.

- **Structured Logging** — Serilog provides comprehensive logging with colorized console output for development and rolling file logs for production diagnostics.

#### Why This Matters

This work enables:

1. **Testability** — Services can now be mocked and isolated for unit testing.
2. **Module Foundation** — Module registration (v0.0.4) will build on this DI container.
3. **Flexibility** — Services can be swapped or decorated without modifying consuming code.

---

## [v0.0.2] - 2026-01-28 (In Progress)

### The Host Shell & UI Foundation

This release introduces the visual foundation of Lexichord — the Avalonia-based desktop application that will host all writing features.

#### What's New

- **Avalonia UI Framework Bootstrapped** — The application now launches as a proper desktop window instead of a console application. This establishes the foundation for all future UI work.

- **Podium Layout Shell** — The main window now features a structured layout with a top bar, navigation rail, content host, and status bar — the "Podium" where all future writing tools will perform.

- **Runtime Theme Switching** — Users can toggle between Dark and Light themes via the StatusBar button, with the application detecting and respecting OS preferences on first launch.

- **Window State Persistence** — Window position, size, maximized state, and theme preference are now remembered between sessions.

#### Why This Matters

This work establishes:

1. **Visual Identity** — Lexichord now has a window, title, and theme infrastructure for building the interface.
2. **Cross-Platform Support** — Avalonia enables Windows, macOS, and Linux deployment from a single codebase.
3. **Modern UI Patterns** — The architecture supports compiled bindings, resource dictionaries, and theme switching.

---

## [v0.0.1] - 2026-01-28

### The Architecture Skeleton

This release establishes the foundation of the Lexichord application. While no visible features exist yet, this work creates the structural backbone that all future features will build upon.

#### What's New

- **Project Structure Established** — The codebase now follows a clean "Modular Monolith" architecture that separates core application code from future plugins and extensions.

- **Code Quality Safeguards Added** — Build-time checks ensure code consistency across the project, catching potential issues before they become problems.

- **Automated Testing Infrastructure** — A comprehensive test suite framework is in place, enabling developers to verify changes don't break existing functionality.

- **Continuous Integration Pipeline** — Every push and pull request now triggers automated builds and tests, ensuring code quality before it reaches the main branch.

#### Why This Matters

This foundational work ensures:

1. **Maintainability** — Clean separation of concerns prevents "spaghetti code" as the project grows.
2. **Extensibility** — The plugin architecture (coming in v0.0.4) will build directly on this work.
3. **Quality Assurance** — Automated tests catch regressions before they reach users.
4. **Developer Confidence** — Clear structure and testing make it easier for contributors to add new features safely.
5. **Continuous Integration** — Automated pipelines validate every change, preventing broken code from entering the main branch.

---

## Version History

| Version | Date       | Codename                          | Summary                                                    |
| :------ | :--------- | :-------------------------------- | :--------------------------------------------------------- |
| v0.1.5  | 2026-01-29 | The Conductor's Baton (Commands)  | Command Registry, command execution and discovery          |
| v0.1.2  | 2026-01-28 | The Explorer (Project Management) | Workspace service, file system watcher, project context    |
| v0.1.1  | 2026-01-28 | The Layout Engine (Docking)       | Dock.Avalonia integration, region injection, serialization |
| v0.0.8  | 2026-01-29 | The Hello World (Golden Skeleton) | Shell regions, Status Bar module reference implementation  |
| v0.0.7  | 2026-01-28 | The Event Bus (Communication)     | MediatR bootstrap, CQRS interfaces, handler discovery      |
| v0.0.6  | 2026-01-28 | The Vault (Secure Storage)        | Secure secrets interface, metadata, exception hierarchy    |
| v0.0.5  | 2026-01-28 | The Memory (Data Layer)           | Docker orchestration, database connector, migrations       |
| v0.0.4  | 2026-01-28 | The Module Protocol               | Module contract, metadata, loader, license gating          |
| v0.0.3  | 2026-01-28 | The Nervous System                | Dependency Injection, Logging, Crash Handling, Config      |
| v0.0.2  | 2026-01-28 | The Host Shell & UI Foundation    | Avalonia bootstrap, window stub, theme infrastructure      |
| v0.0.1  | 2026-01-28 | The Architecture Skeleton         | Modular Monolith foundation, test infrastructure, CI/CD    |

---

## Changelog Format

Each major version has detailed technical changelogs organized by sub-part:

### v0.1.5 Sub-Parts

| Document                               | Sub-Part | Title              |
| :------------------------------------- | :------- | :----------------- |
| [LCS-CL-015a](./LCS-CL-015a.md)        | v0.1.5a  | Command Registry   |
| [LCS-CL-015b](./LCS-CL-015b.md)        | v0.1.5b  | Command Palette UI |
| [LCS-CL-015c](./v0.1.x/LCS-CL-015c.md) | v0.1.5c  | File Jumper        |
| [LCS-CL-015d](./LCS-CL-015d.md)        | v0.1.5d  | Keybinding Service |

### v0.1.2 Sub-Parts

| Document                        | Sub-Part | Title               |
| :------------------------------ | :------- | :------------------ |
| [LCS-CL-012a](./LCS-CL-012a.md) | v0.1.2a  | Workspace Service   |
| [LCS-CL-012b](./LCS-CL-012b.md) | v0.1.2b  | File System Watcher |
| [LCS-CL-012c](./LCS-CL-012c.md) | v0.1.2c  | Tree View UI        |

### v0.1.1 Sub-Parts

| Document                        | Sub-Part | Title                    |
| :------------------------------ | :------- | :----------------------- |
| [LCS-CL-011a](./LCS-CL-011a.md) | v0.1.1a  | Dock Library Integration |
| [LCS-CL-011b](./LCS-CL-011b.md) | v0.1.1b  | Region Injection Service |
| [LCS-CL-011c](./LCS-CL-011c.md) | v0.1.1c  | Layout Serialization     |

### v0.0.8 Sub-Parts

| Document                        | Sub-Part | Title                   |
| :------------------------------ | :------- | :---------------------- |
| [LCS-CL-008a](./LCS-CL-008a.md) | v0.0.8a  | Status Bar Module       |
| [LCS-CL-008b](./LCS-CL-008b.md) | v0.0.8b  | Database Health Check   |
| [LCS-CL-008c](./LCS-CL-008c.md) | v0.0.8c  | Secure Vault Check      |
| [LCS-CL-008d](./LCS-CL-008d.md) | v0.0.8d  | Golden Skeleton Release |

### v0.0.7 Sub-Parts

| Document                        | Sub-Part | Title                        |
| :------------------------------ | :------- | :--------------------------- |
| [LCS-CL-007a](./LCS-CL-007a.md) | v0.0.7a  | MediatR Bootstrap            |
| [LCS-CL-007b](./LCS-CL-007b.md) | v0.0.7b  | Shared Domain Events         |
| [LCS-CL-007c](./LCS-CL-007c.md) | v0.0.7c  | Logging Pipeline Behavior    |
| [LCS-CL-007d](./LCS-CL-007d.md) | v0.0.7d  | Validation Pipeline Behavior |

### v0.0.6 Sub-Parts

| Document                        | Sub-Part | Title                               |
| :------------------------------ | :------- | :---------------------------------- |
| [LCS-CL-006a](./LCS-CL-006a.md) | v0.0.6a  | ISecureVault Interface              |
| [LCS-CL-006b](./LCS-CL-006b.md) | v0.0.6b  | WindowsSecureVault (DPAPI)          |
| [LCS-CL-006c](./LCS-CL-006c.md) | v0.0.6c  | UnixSecureVault (libsecret/AES-256) |
| [LCS-CL-006d](./LCS-CL-006d.md) | v0.0.6d  | Integration Testing                 |

### v0.0.5 Sub-Parts

| Document                        | Sub-Part | Title                 |
| :------------------------------ | :------- | :-------------------- |
| [LCS-CL-005a](./LCS-CL-005a.md) | v0.0.5a  | Docker Orchestration  |
| [LCS-CL-005b](./LCS-CL-005b.md) | v0.0.5b  | Database Connector    |
| [LCS-CL-005c](./LCS-CL-005c.md) | v0.0.5c  | FluentMigrator Runner |
| [LCS-CL-005d](./LCS-CL-005d.md) | v0.0.5d  | Repository Base       |

### v0.0.4 Sub-Parts

| Document                        | Sub-Part | Title             |
| :------------------------------ | :------- | :---------------- |
| [LCS-CL-004a](./LCS-CL-004a.md) | v0.0.4a  | IModule Interface |

### v0.0.3 Sub-Parts

| Document                        | Sub-Part | Title                     |
| :------------------------------ | :------- | :------------------------ |
| [LCS-CL-003a](./LCS-CL-003a.md) | v0.0.3a  | Dependency Injection Root |
| [LCS-CL-003b](./LCS-CL-003b.md) | v0.0.3b  | Serilog Pipeline          |
| [LCS-CL-003c](./LCS-CL-003c.md) | v0.0.3c  | Global Exception Trap     |
| [LCS-CL-003d](./LCS-CL-003d.md) | v0.0.3d  | Configuration Service     |

### v0.0.2 Sub-Parts

| Document                        | Sub-Part | Title                    |
| :------------------------------ | :------- | :----------------------- |
| [LCS-CL-002a](./LCS-CL-002a.md) | v0.0.2a  | Avalonia Bootstrap       |
| [LCS-CL-002b](./LCS-CL-002b.md) | v0.0.2b  | Podium Layout            |
| [LCS-CL-002c](./LCS-CL-002c.md) | v0.0.2c  | Runtime Theme Switching  |
| [LCS-CL-002d](./LCS-CL-002d.md) | v0.0.2d  | Window State Persistence |

### v0.0.1 Sub-Parts

| Document                        | Sub-Part | Title                           |
| :------------------------------ | :------- | :------------------------------ |
| [LCS-CL-001a](./LCS-CL-001a.md) | v0.0.1a  | Solution Scaffolding            |
| [LCS-CL-001b](./LCS-CL-001b.md) | v0.0.1b  | Dependency Graph Enforcement    |
| [LCS-CL-001c](./LCS-CL-001c.md) | v0.0.1c  | Test Suite Foundation           |
| [LCS-CL-001d](./LCS-CL-001d.md) | v0.0.1d  | Continuous Integration Pipeline |

Individual sub-part changelogs provide implementation-level detail for developers.
