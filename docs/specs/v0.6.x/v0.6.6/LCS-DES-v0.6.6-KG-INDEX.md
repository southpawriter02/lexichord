# LCS-DES-066-KG-INDEX: Knowledge-Aware Co-pilot — Design Specifications Index

## Document Control

| Field | Value |
| :--- | :--- |
| **Index ID** | LCS-DES-066-KG-INDEX |
| **System Breakdown** | LCS-SBD-066-KG |
| **Version** | v0.6.6 |
| **Codename** | Knowledge-Aware Co-pilot (CKVS Phase 3b) |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

This index catalogs all design specifications for the **Knowledge-Aware Co-pilot** component of CKVS Phase 3b. This enhancement transforms the Co-pilot from a general-purpose LLM assistant into a domain-aware documentation expert that generates content grounded in verified knowledge.

---

## 2. Sub-Part Specifications

| Spec ID | Title | Est. Hours | Description |
| :------ | :---- | :--------- | :---------- |
| [LCS-DES-066-KG-e](LCS-DES-066-KG-e.md) | Graph Context Provider | 6 | Queries KG for relevant entities and context |
| [LCS-DES-066-KG-f](LCS-DES-066-KG-f.md) | Pre-Generation Validator | 4 | Validates context before LLM call |
| [LCS-DES-066-KG-g](LCS-DES-066-KG-g.md) | Post-Generation Validator | 5 | Validates output, detects hallucinations |
| [LCS-DES-066-KG-h](LCS-DES-066-KG-h.md) | Entity Citation Renderer | 3 | Shows which entities informed response |
| [LCS-DES-066-KG-i](LCS-DES-066-KG-i.md) | Knowledge-Aware Prompts | 4 | Prompt templates with graph context |
| **Total** | | **22** | |

---

## 3. Architecture Diagram

```mermaid
graph TB
    subgraph "User Interface"
        UI[Co-pilot Panel]
        CP[Citation Panel<br/>LCS-DES-066-KG-h]
    end

    subgraph "Co-pilot Core"
        KAC[KnowledgeAwareCopilot]
        PB[PromptBuilder<br/>LCS-DES-066-KG-i]
    end

    subgraph "Context Layer"
        GCP[GraphContextProvider<br/>LCS-DES-066-KG-e]
        ERR[EntityRelevanceRanker]
        CF[ContextFormatter]
    end

    subgraph "Validation Layer"
        PGV[PreGenerationValidator<br/>LCS-DES-066-KG-f]
        POV[PostGenerationValidator<br/>LCS-DES-066-KG-g]
        HD[HallucinationDetector]
    end

    subgraph "Knowledge Sources"
        KG[(Knowledge Graph)]
        AX[(Axiom Store)]
        CR[(Claim Repository)]
    end

    subgraph "External"
        LLM[LLM Gateway]
    end

    UI -->|Request| KAC
    KAC --> GCP
    GCP --> ERR
    GCP --> CF
    GCP --> KG
    GCP --> AX

    KAC --> PGV
    PGV --> KG

    KAC --> PB
    PB -->|Prompt| LLM
    LLM -->|Response| KAC

    KAC --> POV
    POV --> HD
    POV --> CR

    KAC --> CP
    CP --> UI

    style GCP fill:#3b82f6
    style PGV fill:#f59e0b
    style POV fill:#f59e0b
    style CP fill:#8b5cf6
    style PB fill:#10b981
```

---

## 4. Co-pilot Flow with CKVS

```mermaid
sequenceDiagram
    participant User
    participant KAC as KnowledgeAwareCopilot
    participant GCP as GraphContextProvider
    participant PGV as PreGenValidator
    participant PB as PromptBuilder
    participant LLM as LLM Gateway
    participant POV as PostGenValidator
    participant CP as CitationPanel

    User->>KAC: GenerateWithValidationAsync(request)

    Note over KAC: 1. Context Assembly
    KAC->>GCP: GetContextAsync(query)
    GCP-->>KAC: KnowledgeContext

    Note over KAC: 2. Pre-Validation
    KAC->>PGV: ValidateAsync(request, context)
    alt Context Invalid
        PGV-->>KAC: CanProceed=false
        KAC-->>User: Error: [issues]
    else Context Valid
        PGV-->>KAC: CanProceed=true

        Note over KAC: 3. Prompt Building
        KAC->>PB: BuildPrompt(request, context)
        PB-->>KAC: KnowledgePrompt

        Note over KAC: 4. Generation
        KAC->>LLM: Generate(prompt)
        LLM-->>KAC: Generated content

        Note over KAC: 5. Post-Validation
        KAC->>POV: ValidateAsync(content, context)
        POV-->>KAC: PostValidationResult

        Note over KAC: 6. Citation Rendering
        KAC->>CP: GenerateCitations(result)
        CP-->>KAC: CitationMarkup

        KAC-->>User: ValidatedGenerationResult + Citations
    end
```

---

## 5. Key Interfaces

### 5.1 IKnowledgeAwareCopilot

```csharp
public interface IKnowledgeAwareCopilot : ICopilotAgent
{
    Task<ValidatedGenerationResult> GenerateWithValidationAsync(
        CopilotRequest request,
        CancellationToken ct = default);
}
```

### 5.2 IKnowledgeContextProvider

```csharp
public interface IKnowledgeContextProvider
{
    Task<KnowledgeContext> GetContextAsync(
        string query,
        KnowledgeContextOptions options,
        CancellationToken ct = default);

    Task<KnowledgeContext> GetContextForEntitiesAsync(
        IReadOnlyList<Guid> entityIds,
        KnowledgeContextOptions options,
        CancellationToken ct = default);
}
```

### 5.3 IPreGenerationValidator / IPostGenerationValidator

```csharp
public interface IPreGenerationValidator
{
    Task<PreValidationResult> ValidateAsync(
        CopilotRequest request,
        KnowledgeContext context,
        CancellationToken ct = default);
}

public interface IPostGenerationValidator
{
    Task<PostValidationResult> ValidateAsync(
        string generatedContent,
        KnowledgeContext context,
        CopilotRequest originalRequest,
        CancellationToken ct = default);
}
```

---

## 6. Grounding Levels

| Level | Behavior |
| :---- | :------- |
| **Strict** | Only facts from context, no inferences |
| **Moderate** | Prefer context, allow marked inferences |
| **Flexible** | Use context as guidance, supplement allowed |

---

## 7. Validation Status Flow

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Pre-Validate  │────▶│    Generate     │────▶│  Post-Validate  │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                                               │
        ▼                                               ▼
   CanProceed?                                  IsValid?
   ├── Yes ──▶ Continue                         ├── Valid ──▶ ✓ Show
   └── No ──▶ ✗ Block                           ├── Warnings ──▶ ⚠ Show + Warn
                                                └── Invalid ──▶ ✗ Show + Fixes
```

---

## 8. Citation Display Example

```
┌────────────────────────────────────────────────────────────────┐
│ Co-pilot Response                                              │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ The `/orders` endpoint accepts the following parameters:       │
│                                                                │
│ - `userId` (required): The ID of the user placing the order   │
│ - `items` (required): Array of order items                    │
│ - `coupon` (optional): Discount coupon code                   │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│ 📚 Based on:                                                   │
│ ├── 🔗 Endpoint: POST /orders ✓                               │
│ ├── 📝 Parameter: userId (path) ✓                             │
│ ├── 📝 Parameter: items (body) ✓                              │
│ └── 📝 Parameter: coupon (body) ✓                             │
│                                                                │
│ ✓ Validation passed                                           │
└────────────────────────────────────────────────────────────────┘
```

---

## 9. Performance Targets

| Metric | Target | Measurement |
| :----- | :----- | :---------- |
| Context retrieval | <200ms | P95 timing |
| Pre-validation | <100ms | P95 timing |
| Post-validation | <200ms | P95 timing |
| Total overhead | <500ms | P95 timing |
| Validation pass rate | >90% | Telemetry |

---

## 10. Dependencies

| Component | From | Description |
| :-------- | :--- | :---------- |
| `ICopilotAgent` | v0.6.6 | Base Co-pilot functionality |
| `IValidationEngine` | v0.6.5-KG | Pre/post validation |
| `IGraphRepository` | v0.4.5e | Query entities |
| `IAxiomStore` | v0.4.6-KG | Axiom constraints |
| `IClaimExtractionService` | v0.5.6-KG | Extract claims from output |
| `IEntityLinkingService` | v0.5.5-KG | Link entities in output |

---

## 11. License Gating Summary

| Component | Core | WriterPro | Teams | Enterprise |
| :-------- | :--- | :-------- | :---- | :--------- |
| GraphContextProvider | ❌ | Entity only | Full | Full |
| PreGenerationValidator | ❌ | Basic | Full | Full + custom |
| PostGenerationValidator | ❌ | Basic | Full + hallucination | Full + auto-fix |
| EntityCitationRenderer | ❌ | Basic | Full + details | Full + custom |
| KnowledgeAwarePrompts | ❌ | Default | All | Custom templates |

---

## 12. What This Enables

- **v0.7.5 Unified Validation:** Combined Co-pilot + Editor validation
- **v0.7.6 Sync Service:** Generated content updates graph
- **Expert Co-pilot:** Deep domain knowledge assistance
- **Publication Quality:** AI-assisted content meets standards

---

## 13. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
