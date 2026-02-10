# v0.6.6i — Knowledge-Aware Prompts

**Released:** 2026-02-10  
**Spec:** LCS-DES-v0.6.6-KG-i  
**Component:** Co-pilot Agent (CKVS Phase 3b — Knowledge-Aware Prompts)

---

## Overview

Adds the Knowledge-Aware Prompts module — the prompt construction layer between the Knowledge Graph context and the LLM. Provides template-based prompt building with entity, axiom, and relationship context injection, configurable grounding levels, and custom template registration.

## What's New

### Abstractions (`Lexichord.Abstractions`)

| Type | File | Description |
|------|------|-------------|
| `IKnowledgePromptBuilder` | `Contracts/Knowledge/Copilot/IKnowledgePromptBuilder.cs` | Interface: `BuildPrompt`, `GetTemplates`, `RegisterTemplate` |
| `KnowledgePrompt` | `Contracts/Knowledge/Copilot/KnowledgePrompt.cs` | Output record: `SystemPrompt`, `UserPrompt`, `EstimatedTokens`, `TemplateId`, `IncludedEntityIds`, `IncludedAxiomIds` |
| `KnowledgePromptTemplate` | `Contracts/Knowledge/Copilot/KnowledgePromptTemplate.cs` | Template record: `Id`, `Name`, `Description`, `SystemTemplate`, `UserTemplate`, `DefaultOptions`, `Requirements` |
| `PromptRequirements` | `Contracts/Knowledge/Copilot/PromptRequirements.cs` | Requirements: `RequiresEntities`, `RequiresRelationships`, `RequiresAxioms`, `RequiresClaims`, `MinEntities` |
| `PromptOptions` | `Contracts/Knowledge/Copilot/PromptOptions.cs` | Options: `TemplateId`, `MaxContextTokens`, `IncludeAxioms`, `IncludeRelationships`, `ContextFormat`, `GroundingLevel`, `AdditionalInstructions` |
| `GroundingLevel` | `Contracts/Knowledge/Copilot/GroundingLevel.cs` | Enum: `Strict`, `Moderate`, `Flexible` |

### Implementation (`Lexichord.Modules.Knowledge`)

| Type | File | Description |
|------|------|-------------|
| `KnowledgePromptBuilder` | `Copilot/Prompts/KnowledgePromptBuilder.cs` | Implements `IKnowledgePromptBuilder` with 3 built-in templates, entity/axiom/relationship formatting, grounding instructions, and Mustache rendering via `IPromptRenderer` |

### Unit Tests

| Test Class | Count | File |
|-----------|-------|------|
| `KnowledgePromptBuilderTests` | 28 | `Abstractions/Knowledge/Copilot/KnowledgePromptBuilderTests.cs` |

Test coverage includes: entity inclusion, axiom inclusion, grounding levels (Strict/Moderate/Flexible), token estimation, default/specified/unknown template selection, relationship inclusion/exclusion, axiom exclusion, additional instructions, entity ID tracking, axiom ID tracking, user query passthrough, null argument guards, axioms without descriptions, relationship entity name resolution with fallback, default templates listing, custom template registration/override, constructor guards, and data record defaults.

## Spec Deviations

| Deviation | Reason |
|-----------|--------|
| `AgentRequest` instead of `CopilotRequest` | v0.6.6a established `AgentRequest` as the canonical input type |
| `IKnowledgeContextFormatter` instead of `IContextFormatter` | Renamed in v0.6.6e to avoid collision with `Lexichord.Modules.Agents.IContextFormatter` |
| `IPromptRenderer` (Mustache) instead of `IHandlebars` | Project uses Mustache renderer (v0.6.3b), not Handlebars. Context formatted as pre-rendered strings, not iterated via `{{#each}}` |
| `KnowledgePromptTemplate` instead of `PromptTemplate` | Avoids collision with existing `Lexichord.Abstractions.Contracts.LLM.PromptTemplate` (v0.6.3a) |
| Namespace `Lexichord.Modules.Knowledge.Copilot.Prompts` | Spec used `Lexichord.KnowledgeGraph.Copilot.Prompts`; adapted to match project convention |
| Entity name resolution from entity list | `KnowledgeRelationship` only has `FromEntityId`/`ToEntityId`, not `FromEntityName`/`ToEntityName` |
| Inline C# templates instead of YAML files | No YAML template loader infrastructure for knowledge prompts; matches built-in template approach |
| `IReadOnlyList<string>` for `IncludedAxiomIds` | Axiom IDs are strings (not `Guid`) in the `Axiom` record |

## Dependencies

| Dependency | Version | Usage |
|-----------|---------|-------|
| `AgentRequest` | v0.6.6a | User request input |
| `KnowledgeContext` | v0.6.6e | Knowledge graph context |
| `IKnowledgeContextFormatter` | v0.6.6e | Entity formatting |
| `IPromptRenderer` | v0.6.3a | Mustache template rendering |
| `KnowledgeEntity` | v0.4.5e | Entity data |
| `KnowledgeRelationship` | v0.4.5e | Relationship data |
| `Axiom` | v0.4.6e | Domain rules |
| `ContextFormat` / `ContextFormatOptions` | v0.6.6e | Format selection |
