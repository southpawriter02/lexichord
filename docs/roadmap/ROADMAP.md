# Lexichord Version Roadmap (v0.0.1 - v1.0.0)

## Phase 1: The Podium (Infrastructure)
**Goal:** Build the "Stage" where the modules will perform. A stable, plugin-aware host application.
**Timeline:** v0.0.1 – v0.1.9

### v0.0.1: The Foundation
*   **[PLT-01] The Host Shell:** Initial release of the `Lexichord.Host` executable using **AvaloniaUI**. Includes the main window frame, chrome, and navigation rail.
*   **[PLT-02] Module Bootstrapper:** Implementation of the `IModule` reflection loader. The host scans the `/Modules` directory and initializes DLLs implementing the contract without hard dependencies.
*   **[PLT-03] Database Core:** Integration of **PostgreSQL** container connection logic and **FluentMigrator**. The Host runs the "System Schema" (Users, Settings) on startup.
*   **[PLT-04] Secure Vault:** Implementation of `Lexichord.Security`, a service wrapping the OS Keychain (Windows DPAPI / macOS Keychain) to store future API keys securely.

### v0.1.0: The Workspace
*   **[UI-01] Docking Layout System:** Integration of `Avalonia.Dock` to allow users to create custom workspaces (draggable panels, tabbed documents).
*   **[LOG-01] Centralized Telemetry:** A structured logging pipeline (Serilog) that aggregates logs from the Host and all distinct Plugins into a single rolling file and debug console.
*   **[EDT-01] The Manuscript Editor:** A raw text editor implementation using `AvalonEdit`. Supports loading/saving Markdown files to the local file system.
*   **[PLT-05] Theme Manager:** System-aware Dark/Light mode switching that propagates resource dictionaries to all loaded modules dynamically.

---

## Phase 2: The Tuning Fork (Governance & Style)
**Goal:** Implement the "Conscience" of the platform. This makes the tool useful for writers even without AI.
**Timeline:** v0.2.0 – v0.3.9

### v0.2.0: The Lexicon
*   **[STY-01] YAML Style Parser:** A serialization service that ingests `styleguide.yaml` (Google/Microsoft style variants) and converts them into C# Rule Objects.
*   **[STY-02] Terminology Database:** A Postgres schema extension to store `Terms` with metadata: *Preferred, Deprecated, Forbidden, CaseSensitive*.
*   **[STY-03] The Linter Engine:** A background `Task` that scans the `Manuscript Editor` content against the database using Regex.
*   **[UI-02] The "Squiggly" Renderer:** Custom drawing logic in the Editor to underline violations (Red = Error, Yellow = Warning) with tooltip explanations.

### v0.3.0: The Resonance
*   **[STY-04] Fuzzy Matching Engine:** Integration of Levenshtein distance algorithms to catch typos of forbidden terms (e.g., detecting "White list" when "Allowlist" is preferred).
*   **[STY-05] Dictionary Manager:** A CRUD UI panel allowing writers to add/edit terms in the Lexicon without touching the database directly.
*   **[STY-06] Voice Metrics Service:** Algorithms to calculate Flesch-Kincaid, Gunning Fog, and Sentence Length variance.
*   **[UI-03] Resonance Dashboard:** A real-time radar chart in the sidebar visualizing the document's "Tone Score" (e.g., Directness vs. Politeness) against the target profile.

---

## Phase 3: The Score (Memory & RAG)
**Goal:** Give the system "Memory."
**Timeline:** v0.4.0 – v0.5.9

### v0.4.0: The Archive
*   **[RAG-01] Vector Backend:** Integration of the `pgvector` extension into the Postgres service to enable high-dimensional vector storage.
*   **[RAG-02] Ingestion Pipeline (Text):** A file-watcher service that reads `.md`, `.txt`, and `.json` files from a target folder, hashes them for change detection, and queues them for processing.
*   **[RAG-03] Chunking Strategies:** Implementation of three splitting strategies: `FixedSize`, `ParagraphBased`, and `MarkdownHeaderBased`.
*   **[RAG-04] Embedding Connector:** An abstraction layer (`IEmbeddingService`) connecting to OpenAI (`text-embedding-3-small`) to vectorize chunks.

### v0.5.0: The Retrieval
*   **[RAG-05] Semantic Search Panel:** A "Reference" sidebar where users can type natural language queries ("How does Auth work?") and receive ranked text snippets from the database.
*   **[RAG-06] Source Citation:** UI logic to link a retrieved chunk back to the original file path, allowing "Click to Open" functionality.
*   **[RAG-07] Re-ranking Logic:** Implementation of a lightweight localized re-ranking algorithm (BM25) to prioritize exact keyword matches over purely semantic matches.

---

## Phase 4: The Ensemble (Agents & Orchestration)
**Goal:** Introduce the AI workforce. This is the "Magic" phase.
**Timeline:** v0.6.0 – v0.7.9

### v0.6.0: The Conductors
*   **[AI-01] LLM Gateway:** A unified `IChatCompletionService` handling API keys, retry policies (Polly), and token counting for OpenAI and Anthropic.
*   **[AI-02] Prompt Template Engine:** A Mustache-based rendering system that injects Style Rules and RAG Context into system prompts dynamically.
*   **[AGT-01] The "Co-pilot" Agent:** A generic chat interface allowing conversation with the currently open document.
*   **[UI-04] Streaming Typography:** Implementation of Server-Sent Events (SSE) handling to "type" the AI response character-by-character into the UI for a natural feel.

### v0.7.0: The Specialists
*   **[AGT-02] Agent Registry:** A configuration system to define specialized personas (e.g., "The Editor," "The Simplifier") with distinct system prompts and temperature settings.
*   **[WKF-01] Context Assembler:** The logic bridge that automatically queries the [RAG] module based on the user's cursor position to fetch relevant context before the agent speaks.
*   **[AGT-03] The "Tuning" Agent:** A specialized agent pipeline that takes the Linter's output and automatically rewrites the paragraph to resolve all style violations.
*   **[AGT-04] The Summarizer:** A background agent that generates metadata descriptions and tag suggestions for the open document.

---

## Phase 5: The Publisher (Tech Writer Specifics)
**Goal:** Domain-specific features for software documentation.
**Timeline:** v0.8.0 – v0.8.9

### v0.8.0: The Historian
*   **[GIT-01] Repository Reader:** Integration of `LibGit2Sharp` to read local git history (commits, tags, branches) without executing CLI commands.
*   **[AGT-05] Release Notes Agent:** A workflow that accepts two Git Tags, retrieves all commit messages between them, and drafts a structured Changelog.
*   **[UI-05] Comparison View (Diff):** A side-by-side visualizer (Monaco Diff Editor style) showing "Current Draft" vs "AI Proposal" with Accept/Reject buttons.

### v0.8.5: The Exporter
*   **[EXP-01] Static Site Bridge:** Configuration generators for **MkDocs** and **Docusaurus**. Lexichord can now scaffold the `mkdocs.yml` based on the file structure.
*   **[EXP-02] PDF Proofing:** A rendering engine to export the current document (with Style comments as annotations) to PDF for management review.

---

## Phase 6: The Premiere (Release Hardening)
**Goal:** Security, Licensing, and User Experience polish.
**Timeline:** v0.9.0 – v1.0.0

### v0.9.0: The Gatekeeper
*   **[PLT-06] User Profiles:** Support for multiple local user profiles, allowing different API keys and Style Guides for different clients/projects.
*   **[PLT-07] Licensing Module:** Implementation of cryptographic license key validation to unlock "Pro" features (Enabling the RAG module).
*   **[PLT-08] Update Engine:** Integration of **Velopack** or **Squirrel** for background downloading and applying of updates.

### v0.9.5: The Polish
*   **[UI-06] "Zen Mode":** A distraction-free writing UI that collapses all docks and focuses solely on the Editor.
*   **[PERF-01] Startup Optimization:** Lazy-loading of non-critical modules (like the Git Service) to ensure the app opens in < 2 seconds.
*   **[SEC-01] PII Scrubber:** A safety filter that uses Regex to detect credit card numbers or IP addresses in the prompt *before* sending them to the LLM provider.

### v1.0.0: Lexichord Gold
*   **[MKT-01] Plugin Marketplace UI:** A browser interface to view available (but not installed) modules—preparing for the post-launch release of the "Creative Writing" and "Developer" suites.
*   **[DOC-01] "The Lexicon":** Full internal documentation, written and orchestrated by Lexichord v0.9, embedded into the Help menu.