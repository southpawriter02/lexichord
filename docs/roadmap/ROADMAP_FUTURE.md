# Future Horizons Roadmap

Here is the **Future Horizons Roadmap** for Lexichord. These features represent the evolution from a "Writer’s Tool" (v1.0) to an "Enterprise Intelligence Platform" (v2.0+).

They are categorized into **Expansion Packs**, consistent with the modular architecture.

---

### Expansion 1: The Neural Core (Advanced AI Training)
*Focus: Turning the consumption of AI into the training of custom AI.*

1.  **[AI-10] The Academy (Fine-Tuning UI):** A drag-and-drop interface where admins can upload "Gold Standard" documents. Lexichord orchestrates the training of a LoRA (Low-Rank Adaptation) adapter on top of the base model, creating a custom model that natively understands the company's voice without massive prompting.
2.  **[AI-11] The Localist (Ollama Integration):** A fully offline mode connecting to a local Ollama instance. This enables secure, air-gapped document generation using Llama-3 or Mistral running on the user’s NPU/GPU.
3.  **[AI-12] Reinforcement Loop (RLHF Widget):** A system where every "Reject" or "Rewrite" clicked by a human writer is anonymously logged. This dataset is compiled to train a Reward Model, making the agents smarter about *why* their drafts were rejected.
4.  **[AI-13] The Economist (Model Routing):** A cost-optimization layer that analyzes the complexity of a prompt. Simple grammar checks are routed to cheap models (GPT-4o-mini/Haiku), while complex architectural reasoning is routed to reasoning models (o1/Claude Opus), optimizing the monthly API bill.
5.  **[AI-14] Prompt Battle Arena:** A testing sandbox where a prompt engineer can run the same prompt against 5 different models simultaneously to compare output quality before deploying it to the workforce.
6.  **[RAG-10] Graph RAG (Knowledge Graph):** Moving beyond vector similarity. This feature extracts entities (e.g., "User" relates to "Account") and builds a graph database, allowing the AI to answer multi-hop reasoning questions like "How does the legacy billing system impact the new auth flow?"

---

### Expansion 2: The Global Stage (Localization & Media)
*Focus: Going beyond English text.*

7.  **[LOC-01] The Babel Engine:** Integration with DeepL and Google Translate APIs. It doesn't just translate; it uses an LLM to post-edit the translation to ensure technical accuracy and tone preservation.
8.  **[LOC-02] Translation Memory (TM) Manager:** A module to import/export `.xliff` files and maintain a local database of previously translated segments to save costs on re-translating unchanged content.
9.  **[VIS-01] Text-to-Diagram:** An agent that reads a textual description of a process and generates editable code for **Mermaid.js** or **PlantUML** sequence diagrams.
10. **[VIS-02] The Illustrator:** Integration with DALL-E 3 or Midjourney to generate abstract header art or stylized icons for documentation pages based on the content context.
11. **[AUT-01] Screenshot Automator:** A puppeteer-driven agent that spins up a headless browser, navigates to the software's UI, takes screenshots of specific workflows, and automatically inserts them into the docs.

---

### Expansion 3: The Enterprise Tower (Management & Ops)
*Focus: Managing the humans and the business.*

12. **[ENT-01] Role-Based Access Control (RBAC):** Defining granular permissions (e.g., "Junior Writers can Draft, Senior Writers can Approve, Admins can Change Style Rules").
13. **[ENT-02] The Audit Log:** An immutable ledger recording every interaction: who changed a rule, which agent generated a specific paragraph, and what the original prompt was (for liability and compliance).
14. **[ANL-01] Coverage Heatmap:** A visualization of the codebase that highlights files or API endpoints that have zero associated documentation in the Knowledge Hub.
15. **[ANL-02] The ROI Dashboard:** Metrics calculating "Hours Saved." (e.g., "Agents generated 50,000 words this month; estimated human time saved: 120 hours").
16. **[INT-05] The Ticket Master (Jira/Linear Sync):** Bi-directional sync. When a Jira ticket moves to "Done," Lexichord creates a "To-Do" in the writer’s queue. When the doc is published, Lexichord posts the link back to the Jira ticket.
17. **[OPS-01] CI/CD Quality Gate:** A CLI version of Lexichord that runs in GitHub Actions. It fails the build if the documentation in the PR has a Style Compliance Score below 80%.
18. **[SEO-01] Search Optimizer Agent:** An agent that analyzes public-facing docs against SEO best practices, suggesting meta descriptions, keyword density adjustments, and slug optimizations.

---

### Expansion 4: The Developer Convergence (The "AIntern" Merger)
*Focus: Reintegrating the developer-centric features from the AIntern project.*

19. **[TRM-01] The Integrated Terminal:** Re-introduction of `Pty.Net` to provide a fully functional terminal inside Lexichord, allowing writers to run `npm start` to test the software they are documenting.
20. **[COD-01] The Scaffold Agent:** An agent capable of generating folder structures and boilerplate code. "Create a new Docusaurus site structure in this folder."
21. **[COD-02] Safe-Run Sandbox:** A security container allowing the AI to execute read-only commands (like `grep` or `ls`) to gather information without risking system stability.
22. **[VER-01] Docs-as-Code Manager:** A GUI for managing complex Git operations specific to docs: cherry-picking commits to a "release branch" or resolving merge conflicts in Markdown tables.

---

### Expansion 5: The Creative Suite (Worldbuilding)
*Focus: Expanding the user base to game designers and novelists.*

23. **[CW-01] The Story Bible:** A specialized variation of the Terminology Database designed for Lore. Fields include "Aliases," "Affiliations," "First Appearance," and "Status (Alive/Dead)."
24. **[CW-02] Timeline Manager:** A linear visualization of events found in the text. The AI extracts dates/times and places them on a timeline to ensure chronological consistency.
25. **[CW-03] Character Voice Profiles:** Advanced style settings where specific agents mimic specific personas. (e.g., "Rewrite this dialogue as a sarcastic 1920s detective.")
26. **[CW-04] The Showrunner:** A high-level orchestration agent that analyzes the plot arc across multiple files/chapters and flags continuity errors (e.g., "You mentioned this character lost their arm in Chapter 3, but they are clapping in Chapter 5").

---

### Expansion 6: The Collaborative Future
*Focus: Multiplayer functionality.*

27. **[COL-01] Real-Time Sync (CRDTs):** Implementation of Conflict-free Replicated Data Types to allow multiple writers (and agents) to edit the same document simultaneously, Google Docs style.
28. **[COL-02] Inline Commenting:** A review system where humans can tag specific sentences for discussion, and agents can reply to comments with suggestions.
29. **[MOB-01] Lexichord Companion App:** A mobile read-only view for stakeholders to review and approve documentation drafts on the go.
30. **[SDK-01] The Plugin SDK:** Public documentation and NuGet packages allowing third-party developers to write their own Lexichord Modules (e.g., a "WordPress Publisher" module or a "Trello" integration).

Core Platform & Architecture
 * Hierarchical Agent Orchestration: A central engine that analyzes user intent and coordinates specialized sub-agents to perform complex documentation tasks.
 * Modular Monolith Architecture: A system design that allows features to be developed, tested, and deployed as independent modules (e.g., Style Engine, RAG) while maintaining a cohesive application.
 * Local AI & Offline Mode: Support for running local LLMs (via Ollama/LlamaSharp) and local vector embeddings, enabling full functionality without internet access.
 * Extensible Plugin Architecture: A secure framework for adding third-party agents, integrations, and output formats via sandboxed plugins.
 * Cross-Platform Desktop App: A native-feeling application built on AvaloniaUI that runs consistently on Windows, macOS, and Linux.
Style & Voice Engine
 * Style Guide Management: A system to define, import (YAML), and manage organizational writing rules, including inheritance from base guides.
 * Terminology Database: A comprehensive engine for managing preferred terms, synonyms, and "avoid" terms with fuzzy matching detection.
 * Voice Analysis Metrics: NLP-based quantification of writing style using metrics like Formality, Directness, Complexity, and Passive Voice.
 * Real-Time Style Checker: An inline editor feature that flags terminology and voice violations as the user types, offering quick fixes.
 * Adaptive Style Learning: Machine learning capabilities that analyze existing approved documentation to automatically learn and suggest style patterns.
Knowledge Hub (RAG Pipeline)
 * Vector Storage Infrastructure: Integration with PostgreSQL and pgvector to store and query high-dimensional vector embeddings of documentation.
 * Intelligent Document Chunking: Advanced strategies (Semantic, Recursive, Structure-Aware) to split documents into optimal segments for retrieval.
 * Source Integration & Attribution: A pipeline that tracks the provenance of information, ensuring AI-generated content cites its original sources.
 * Knowledge Base Connectors: Built-in integrations to index and retrieve context from external platforms like Confluence and Notion.
 * Hybrid Search & Ranking: A retrieval engine that combines semantic vector search with keyword matching and re-ranks results for maximum relevance.
Specialized Writer Agents
 * Release Notes Agent: An agent that analyzes Git commit history, PRs, and issue trackers to auto-generate categorized release notes.
 * API Documentation Agent: An agent capable of parsing code and specifications (OpenAPI) to generate reference documentation.
 * Procedural Agent: An agent specialized in decomposing tasks into step-by-step tutorials and guides.
 * Conceptual Agent: An agent designed to write high-level explanations, overviews, and architecture documentation at varying depth levels.
 * Reference Agent: An agent that extracts entities from code to create cross-referenced indexes and glossaries.
Code Intelligence & Developer Tools
 * Integrated Code Editor: A multi-tab editor with syntax highlighting (TextMateSharp) for over 15 languages.
 * Smart Code Proposals: A feature that extracts code blocks from AI responses and allows users to apply them to files with diff previews.
 * Integrated Terminal: A built-in terminal emulator with shell detection, command extraction, and safety guardrails.
 * Git Integration: Deep connectivity with Git repositories for history analysis, branch management, and commit parsing.
 * Issue Tracker Integration: Connectors for Jira and Linear to pull context from tickets, sprints, and epics.
Workflow, Collaboration & Quality
 * Document Version Control: A custom versioning system optimized for documentation, supporting branching, merging, and semantic tagging.
 * Human-in-the-Loop Reviews: Workflows for approval gates, SME routing, and manual review queues to ensure content quality.
 * Feedback Loop: A system that captures user edits to AI content to continuously fine-tune agent prompts and behavior.
 * Content Freshness Monitoring: Analytics that detect stale documentation based on code changes, deprecated features, or age.
 * Multi-Language Localization: Infrastructure for managing translation workflows, detecting content language, and maintaining cross-language terminology consistency.
