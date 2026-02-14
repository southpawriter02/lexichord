<div align="center">

<!-- Hero: Musical staff with notes forming a crescendo — the Lexichord signature motif -->
<img src="docs/assets/svg/hero-crescendo.svg" alt="Musical staff with ascending notes forming a crescendo" width="800"/>

# Lexichord

### The Agentic Writing Platform

*Where every document is a symphony, every writer is a conductor,*
*and every word lands with precision.*

---

**Lexichord** redefines the technical writer not as a solitary author, but as a **Conductor**. You define the theme. You set the key signature. Lexichord orchestrates an ensemble of AI agents, style sentinels, knowledge graphs, and semantic search to execute the performance — at scale, with governance, without compromise.

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](#tech-stack)
[![Avalonia UI](https://img.shields.io/badge/Avalonia-11.3-8B44F7)](#tech-stack)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)](#tech-stack)

</div>

---

## Why Lexichord?

Most writing tools bolt AI on as an afterthought — a chatbot in a sidebar, a grammar checker that ignores your style guide, a search bar that doesn't understand your domain.

Lexichord was built from the ground up around a different premise: **writing is orchestration**. A technical writer doesn't just type words. They navigate terminology databases, enforce style guides across hundreds of pages, search vast documentation sets for precedent, validate claims against knowledge bases, and maintain consistency across teams. That work deserves a platform that treats it as the complex, multi-system coordination problem it actually is.

Lexichord gives you:

- **An editor that understands structure**, not just characters — parsing Markdown AST to know when you're in a code block, a table, or a heading hierarchy.
- **A style engine that governs**, not just suggests — with YAML-driven rules, fuzzy matching, readability scoring, voice profiles, and real-time compliance dashboards.
- **A search system that reasons**, not just matches — combining vector similarity, BM25 full-text search, and reciprocal rank fusion to find exactly the documentation you need.
- **A knowledge graph that remembers**, not just stores — modeling entities, relationships, and axioms so your AI agents can ground their responses in verified domain truth.
- **An agent architecture that collaborates**, not just responds — with specialized personas, parallel context assembly, token budgeting, and transparent previews of exactly what context each agent receives.

---

<!-- Conductor's baton with motion arc — directing the ensemble -->
<div align="center">
<img src="docs/assets/svg/conductor-baton.svg" alt="Conductor's baton with sweeping motion arc" width="600"/>
</div>

## The Ensemble

Lexichord ships with four built-in AI agents, each with distinct personalities and switchable personas:

| Agent | Persona Variants | Specialty | Temperature |
|:------|:-----------------|:----------|:-----------:|
| **General Chat** | Balanced | Versatile writing companion for brainstorming and conversation | 0.7 |
| **The Editor** | Strict / Friendly | Grammar, clarity, and structure — catches errors while preserving your voice | 0.1 - 0.5 |
| **The Researcher** | Academic / Casual | Research synthesis, citation management, and information architecture | 0.1 - 0.4 |
| **The Storyteller** | Dramatic / Subtle | Plot development, character arcs, dialogue, and world-building | 0.6 - 0.9 |

Every agent is defined in YAML. Swap personas at runtime. Register your own agents with `[AgentDefinition]` attributes or drop a `.yaml` file into your workspace's `.lexichord/agents/` directory. The registry discovers, validates, and hot-reloads them automatically.

---

<!-- Open manuscript page with staff lines and scattered notes — the feature score -->
<div align="center">
<img src="docs/assets/svg/manuscript-page.svg" alt="Open manuscript page with musical notation" width="600"/>
</div>

## Feature Tour

### The Manuscript Editor

A high-performance Markdown editor built on AvaloniaEdit, designed for documents that span hundreds of pages without flinching.

- **Syntax highlighting** for Markdown, JSON, YAML, and XML with full theme awareness
- **Search & Replace** with live highlighting, regex support, and ReDoS protection
- **Real-time statistics** — lines, words, characters, updated as you type
- **Ctrl+Scroll zoom** with debounced persistence across sessions
- **Atomic saves** using a Write-Temp-Delete-Rename strategy — no half-written files, ever
- **Dirty state tracking** with visual indicators and safe-close workflows
- **IDE-style docking** — draggable, resizable panels with serialized layout profiles

### The Rulebook — Style Engine

YAML-driven style governance that enforces your writing standards with the precision of a linter and the nuance of a human editor.

- **Rule categories**: Terminology, Formatting, Syntax — each with configurable severities (Error, Warning, Info, Hint)
- **Pattern types**: Regex, Literal, Case-insensitive, StartsWith, EndsWith, Contains
- **26 default rules** covering common technical writing pitfalls
- **Hot-reload** — edit your `.lexichord/style.yaml` and see changes in milliseconds
- **Fuzzy matching** powered by Levenshtein distance with per-term configurable thresholds
- **5 inclusive language defaults** (e.g., `whitelist` &rarr; `allowlist`)
- **Code block awareness** — fenced blocks and inline backticks are automatically excluded from analysis
- **Frontmatter awareness** — YAML, TOML, and JSON frontmatter sections are respected and skipped

### The Critic — Linting & Diagnostics

A reactive analysis pipeline that underlines problems as you type, then helps you fix them.

- **Wavy underlines** with severity-based coloring (theme-aware for light and dark modes)
- **Hover tooltips** with rule details and context
- **Quick-fix context menu** (Ctrl+.) for one-click corrections
- **Problems Panel** with collapsible severity groups, double-click navigation, and a compliance scorecard (0-100%, letter grade A-F, trend indicator)
- **System.Reactive pipeline** — debounced at 300ms, parallel scanning, deduplication built in

### The Voice — Readability & Tone Analysis

Quantitative measurement of your writing's readability, voice, and stylistic consistency.

- **Readability metrics**: Flesch-Kincaid Grade Level, Gunning Fog Index, Flesch Reading Ease
- **Sentence tokenizer** with 50+ abbreviation handling for accurate scoring
- **Passive voice detector** with confidence scoring
- **Weak word scanner** — 40 adverbs, 30 weasel words, 15 fillers
- **5 Voice Profiles**: Technical, Marketing, Academic, Narrative, Casual — each with configurable constraints for grade level, sentence length, and passive voice tolerance
- **Resonance Dashboard** — a spider chart plotting 6 axes (Readability, Clarity, Precision, Accessibility, Density, Flow) with target overlay from your active Voice Profile

### The Archive — Semantic Search (RAG)

A retrieval-augmented generation pipeline that turns your documentation into a searchable knowledge base your AI agents can query in real time.

- **Vector storage**: PostgreSQL 16 + pgvector with HNSW indexing
- **Hybrid search**: Vector similarity + BM25 full-text search fused via Reciprocal Rank Fusion
- **Automatic ingestion**: File watcher detects changes, hashes for deduplication, chunks intelligently (fixed-size, paragraph, or Markdown-header-aware), embeds via OpenAI `text-embedding-3-small`, and stores — all in a priority queue with backpressure handling
- **Citation engine**: Full provenance tracking (path, heading, line, timestamp) with 3 citation styles (Inline, Footnote, Markdown) and stale citation detection
- **Context window**: Sibling chunk retrieval, heading hierarchy breadcrumbs, and LRU-cached context expansion
- **Relevance tuner**: Query analysis (keywords, entities, intent, specificity), synonym expansion, Porter stemming, and zero-result tracking
- **Filter system**: Path globs, file extensions, date ranges, heading filters, and saveable presets
- **Answer preview**: Snippet extraction with match density scoring, query term highlighting across 4 themes, and smart sentence-boundary truncation
- **Deduplication**: Similarity detection (threshold 0.95), relationship classification (Equivalent, Complementary, Contradictory, Superseding, Subset), and canonical record management

### The Graph — Knowledge Base (CKVS)

A Neo4j-backed knowledge graph that models your domain as entities, relationships, and axioms — giving your AI agents grounded, verifiable context instead of hallucinated guesses.

- **Schema registry**: YAML-driven with 6 built-in entity types (Product, Component, Endpoint, Parameter, Response, Concept) and 6 relationship types
- **Entity extraction**: Pluggable pipeline with regex-based extractors for Endpoints, Parameters, and Concepts — with mention deduplication and confidence filtering
- **Axiom system**: YAML-defined domain rules with 9 constraint types, 13 condition operators, severity levels, and hot-reload from workspace files
- **Claim extraction**: Subject-predicate-object triples parsed from natural language, stored with full-text search, diff tracking, and contradiction detection
- **Validation pipeline**: Schema validation, axiom compliance checking, consistency verification (claim contradictions), and a unified findings view integrated with the linting panel
- **Knowledge-aware prompting**: 3 built-in prompt templates with entity/axiom/relationship formatting and configurable grounding levels (Strict, Moderate, Flexible)
- **Pre/post-generation validation**: Check agent inputs for consistency *before* generation; detect hallucinations and validate claims *after*
- **Entity citations**: Rendered with type-specific icons in Compact, Detailed, or TreeView formats with verification status

### The Assembler — Intelligent Context

A strategy-based context assembly system that automatically gathers the right information from the right sources and delivers it to the right agent — in parallel, within budget, without duplicates.

Seven built-in strategies, each with its own priority, token budget, and license tier:

| Strategy | Priority | Tokens | What It Gathers |
|:---------|:--------:|:------:|:----------------|
| **Document** | 100 | 4,000 | Full document content with heading-aware smart truncation |
| **Selection** | 80 | 1,000 | User-selected text wrapped in markers with surrounding paragraph context |
| **Cursor** | 70 | 500 | Text window around cursor position with word-boundary expansion |
| **Heading** | 70 | 300 | Document heading tree as an indented outline with breadcrumb |
| **RAG** | 60 | 2,000 | Semantic search results from your documentation corpus |
| **Style** | 50 | 1,000 | Active style rules filtered by agent type |
| **Knowledge** | 30 | 4,000 | Knowledge graph entities, relationships, and axioms |

The **Context Orchestrator** runs all applicable strategies in parallel, deduplicates overlapping content (Jaccard similarity, 0.85 threshold), sorts by priority, trims to your token budget, and publishes the result as a MediatR event. The **Context Preview Panel** shows you exactly what your agent sees — every fragment, every token count, every strategy toggle — in real time.

### The Gateway — LLM Integration

Multi-provider LLM connectivity with enterprise-grade resilience.

- **Providers**: OpenAI (GPT-4o, GPT-4o-mini, GPT-4 Turbo, GPT-3.5 Turbo) and Anthropic (Claude 3.5 Sonnet, Claude 3 Opus/Sonnet/Haiku)
- **Streaming**: Server-Sent Events parsing for both OpenAI and Anthropic formats with 50ms throttled UI updates and typing indicators
- **Resilience**: Polly v8 pipelines with exponential backoff, circuit breakers, timeouts, bulkhead isolation, and Retry-After header support
- **Token management**: Exact GPT tokenization via Microsoft.ML.Tokenizers, approximate Claude estimation, and pre-flight budget enforcement
- **Prompt templates**: Mustache rendering with 5 built-in YAML templates and hot-reload for custom templates
- **Context injection**: 3 built-in providers (Document, StyleRules, RAG) executed in parallel with per-provider timeouts and graceful degradation
- **Usage tracking**: Per-conversation and session-level metrics with cost estimation and CSV/JSON export

---

<!-- Accent mark with lightning energy — quick action motif -->
<div align="center">
<img src="docs/assets/svg/sforzando-accents.svg" alt="Sforzando accent marks with staccato dots" width="600"/>
</div>

## Quick Actions

Eight built-in actions available from the editor context menu, each mapped to a specialized prompt template:

| Action | What It Does |
|:-------|:-------------|
| **Improve** | Enhance clarity, flow, and impact |
| **Simplify** | Reduce complexity while preserving meaning |
| **Expand** | Develop ideas with more detail and examples |
| **Summarize** | Distill to key points |
| **Fix** | Correct grammar, spelling, and punctuation |
| **Explain** | Break down complex passages |
| **Comment** | Add inline documentation |
| **Add Row** | Extend tables with contextually appropriate data |

Actions are context-aware — they appear only when applicable to your current selection and content type (prose, code, table, list, heading).

---

<!-- Interconnected module nodes — the modular monolith constellation -->
<div align="center">
<img src="docs/assets/svg/module-constellation.svg" alt="Interconnected module constellation with labeled nodes" width="600"/>
</div>

## Architecture

Lexichord is a **modular monolith** — each module is a self-contained unit with its own services, views, and domain logic, communicating through well-defined abstractions and a shared event bus.

```
Lexichord.Host                    The application shell (Avalonia UI)
  |
  +-- Lexichord.Abstractions      Shared contracts, interfaces, records
  |
  +-- Lexichord.Infrastructure    PostgreSQL, Neo4j, migrations, security
  |
  +-- Modules/
       +-- Editor                 AvaloniaEdit-based Markdown editor
       +-- Style                  YAML rules, linting, readability, voice
       +-- RAG                    Vector search, chunking, citations
       +-- Knowledge              Neo4j graph, axioms, claims, validation
       +-- Agents                 AI orchestration, prompts, context assembly
       +-- LLM                   Provider connectors (OpenAI, Anthropic)
       +-- Workspace              File system, project explorer
       +-- StatusBar              System health, module indicators
```

Modules are discovered at startup from `./Modules/*.dll` via reflection. Each implements `IModule` with `RegisterServices()` and `InitializeAsync()`. License-tier gating is enforced at the DI level through `[RequiresLicense]` attributes — modules cannot bypass the tier system even if they try.

### Event-Driven Communication

All cross-module communication flows through **MediatR**:
- **Commands** for write operations
- **Queries** for read operations
- **Domain Events** for notifications
- **Pipeline Behaviors** for cross-cutting concerns (logging with PII redaction, FluentValidation, timing with slow-query warnings)

### Data Layer

| Database | Purpose | Access |
|:---------|:--------|:-------|
| **PostgreSQL 16** | Style terms, RAG chunks, axioms, claims, settings | Dapper + FluentMigrator |
| **PostgreSQL + pgvector** | Vector embeddings for semantic search | HNSW index, cosine similarity |
| **Neo4j 5.x** | Knowledge graph (entities, relationships) | Cypher queries, license-gated sessions |
| **SQLite** | Embedding cache (LRU eviction) | Local-only, performance optimization |

---

## Licensing

Lexichord uses a four-tier model that unlocks capabilities progressively:

| | Core | WriterPro | Teams | Enterprise |
|:---|:---:|:---:|:---:|:---:|
| Markdown Editor | **Yes** | **Yes** | **Yes** | **Yes** |
| File Management | **Yes** | **Yes** | **Yes** | **Yes** |
| General Chat Agent | **Yes** | **Yes** | **Yes** | **Yes** |
| Style Engine (full) | | **Yes** | **Yes** | **Yes** |
| Readability & Voice | | **Yes** | **Yes** | **Yes** |
| Semantic Search (RAG) | | **Yes** | **Yes** | **Yes** |
| Specialist Agents | | **Yes** | **Yes** | **Yes** |
| Quick Actions | | **Yes** | **Yes** | **Yes** |
| Inline Suggestions | | **Yes** | **Yes** | **Yes** |
| Streaming Responses | | | **Yes** | **Yes** |
| Knowledge Graph | | | **Yes** | **Yes** |
| Custom Agents | | | **Yes** | **Yes** |
| Template Hot-Reload | | | **Yes** | **Yes** |
| Knowledge Context | | | **Yes** | **Yes** |
| SSO / SAML | | | | **Yes** |
| Audit & Compliance | | | | **Yes** |

---

## Tech Stack

| Layer | Technology |
|:------|:-----------|
| **Runtime** | .NET 9.0 |
| **UI Framework** | Avalonia UI 11.3.2 |
| **Text Editor** | AvaloniaEdit 11.1.0 |
| **Docking** | Dock.Avalonia |
| **Charts** | LiveCharts2 + SkiaSharp |
| **MVVM** | CommunityToolkit.Mvvm 8.4 |
| **Messaging** | MediatR 12.x |
| **Reactive** | System.Reactive |
| **Templates** | Stubble.Core (Mustache) |
| **YAML** | YamlDotNet 15.x |
| **Database** | PostgreSQL 16, Neo4j 5.x, SQLite |
| **ORM** | Dapper + FluentMigrator |
| **Vectors** | pgvector (HNSW) |
| **Tokenization** | Microsoft.ML.Tokenizers |
| **Resilience** | Polly v8 |
| **Caching** | Microsoft.Extensions.Caching.Memory |
| **Auto-Update** | Velopack |
| **Telemetry** | Sentry (opt-in, PII-scrubbed) |
| **Testing** | xUnit, NSubstitute, FluentAssertions, BenchmarkDotNet |

---

## Project Structure

```
lexichord/
  src/
    Lexichord.Host/                    Application shell
    Lexichord.Abstractions/            Shared contracts
    Lexichord.Infrastructure/          Database & security
    Lexichord.Modules.Editor/          Markdown editor
    Lexichord.Modules.Style/           Style governance
    Lexichord.Modules.RAG/             Semantic search
    Lexichord.Modules.Knowledge/       Knowledge graph
    Lexichord.Modules.Agents/          AI agents
    Lexichord.Modules.LLM/            LLM providers
    Lexichord.Modules.Workspace/       File management
    Lexichord.Modules.StatusBar/       System health
  tests/
    Lexichord.Tests.Unit/              Unit tests
    Lexichord.Tests.Integration/       Integration tests
  benchmarks/
    Lexichord.Benchmarks/              Performance benchmarks
  docs/
    changelogs/                        Version-specific changelogs
    specs/                             Design specifications
  Lexichord.sln
```

---

## Building

```bash
# Prerequisites: .NET 9.0 SDK, PostgreSQL 16, Neo4j 5.x (optional)

# Build
dotnet build Lexichord.sln

# Test
dotnet test Lexichord.sln

# Run
dotnet run --project src/Lexichord.Host
```

---

<!-- Stacked chord notation — three notes sounding together in harmony -->
<div align="center">
<img src="docs/assets/svg/chord-triad.svg" alt="Three stacked notes forming a chord with resonance rings" width="600"/>
</div>

## The Name

In music, a **chord** is multiple notes sounding together in harmony. In linguistics, a **lexicon** is the complete vocabulary of a language.

**Lexichord**: the harmonious sounding of language — multiple voices (agents, rules, knowledge, search) resonating together to produce writing that is greater than the sum of its parts.

The metaphor runs deep. Your **Style Sheet** is the key signature. Your **Voice Profile** is the tempo marking. Your **Agents** are the ensemble players. The **Context Assembler** is the score. And you — the writer — are the **Conductor**.

---

<!-- Fermata over a final whole note — the ending sustain -->
<div align="center">
<img src="docs/assets/svg/fermata-finale.svg" alt="Fermata over a whole note — the final sustain" width="600"/>
</div>

<div align="center">

*Built with conviction that the best writing tools don't replace writers — they amplify them.*

**MIT License** &mdash; Copyright (c) 2026 Lexichord

</div>
