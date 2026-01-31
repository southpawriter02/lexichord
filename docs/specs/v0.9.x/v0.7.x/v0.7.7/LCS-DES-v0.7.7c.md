# LCS-DES-077c: Design Specification — Preset Workflows

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `AGT-077c` | Sub-part of AGT-077 |
| **Feature Name** | `Preset Workflows` | Built-in workflow templates |
| **Target Version** | `v0.7.7c` | Third sub-part of v0.7.7 |
| **Module Scope** | `Lexichord.Modules.Agents` | Agents module |
| **Swimlane** | `Ensemble` | Agent orchestration vertical |
| **License Tier** | `WriterPro` (execute) / `Teams` (edit) | Tiered access |
| **Feature Gate Key** | `FeatureFlags.Agents.PresetWorkflows` | |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-077-INDEX](./LCS-DES-077-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-077 Section 3.3](./LCS-SBD-077.md#33-v077c-preset-workflows) | |

---

## 2. Executive Summary

### 2.1 The Requirement

New users need immediate value from the Agent Workflows feature without designing workflows from scratch. They need:

- Ready-to-use workflows for common document processing tasks
- Examples demonstrating best practices for workflow composition
- Starting points for customization (Teams tier)
- Consistent, tested workflow implementations

> **Goal:** Provide 5 production-ready preset workflows that deliver immediate value and serve as templates for custom workflow creation.

### 2.2 The Proposed Solution

Implement pre-built workflows as embedded YAML resources:

1. **Technical Review** — 4-step technical documentation pipeline
2. **Marketing Polish** — 4-step marketing content enhancement
3. **Quick Edit** — 1-step fast grammar check
4. **Academic Review** — 3-step scholarly document review
5. **Executive Summary** — 3-step executive briefing generation

All presets are:
- Embedded in the assembly as resources
- Loaded at startup via `IPresetWorkflowRepository`
- Immutable (cannot be modified, but can be duplicated by Teams users)
- Optimized prompts tested against real documents

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Modules

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `EditorAgent` | v0.7.3b | Grammar and clarity steps |
| `SimplifierAgent` | v0.7.4b | Readability enhancement steps |
| `TuningAgent` | v0.7.5b | Style compliance steps |
| `SummarizerAgent` | v0.7.6b | Metadata generation steps |
| `IWorkflowEngine` | v0.7.7b | Workflow execution |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `YamlDotNet` | 15.x | YAML deserialization |

### 3.2 Licensing Behavior

| Tier | Can View | Can Execute | Can Duplicate | Can Edit |
| :--- | :--- | :--- | :--- | :--- |
| Core | Yes | No | No | No |
| WriterPro | Yes | Yes (3/day) | No | No |
| Teams | Yes | Yes | Yes | No (create copy) |
| Enterprise | Yes | Yes | Yes | No (create copy) |

Presets are immutable — Teams users can duplicate and edit the copy.

---

## 4. Data Contract (The API)

### 4.1 Repository Interface

```csharp
namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Repository for accessing preset (built-in) workflows.
/// Presets are loaded from embedded resources at startup.
/// </summary>
public interface IPresetWorkflowRepository
{
    /// <summary>
    /// Gets all available preset workflows.
    /// </summary>
    IReadOnlyList<WorkflowDefinition> GetAll();

    /// <summary>
    /// Gets a preset workflow by ID.
    /// </summary>
    /// <param name="workflowId">Preset workflow ID (e.g., "preset-technical-review").</param>
    /// <returns>Workflow definition or null if not found.</returns>
    WorkflowDefinition? GetById(string workflowId);

    /// <summary>
    /// Gets preset workflows by category.
    /// </summary>
    /// <param name="category">Workflow category.</param>
    /// <returns>List of workflows in the category.</returns>
    IReadOnlyList<WorkflowDefinition> GetByCategory(WorkflowCategory category);

    /// <summary>
    /// Checks if a workflow ID is a built-in preset.
    /// </summary>
    /// <param name="workflowId">Workflow ID to check.</param>
    /// <returns>True if the ID refers to a preset workflow.</returns>
    bool IsPreset(string workflowId);

    /// <summary>
    /// Gets all preset workflow summaries for display.
    /// </summary>
    IReadOnlyList<PresetWorkflowSummary> GetSummaries();
}

/// <summary>
/// Summary of a preset workflow for display purposes.
/// </summary>
public record PresetWorkflowSummary(
    string WorkflowId,
    string Name,
    string Description,
    string Icon,
    WorkflowCategory Category,
    int StepCount,
    IReadOnlyList<string> AgentIds,
    LicenseTier RequiredTier,
    IReadOnlyList<string> Tags
);
```

### 4.2 Preset Identifiers

| ID | Name | Category | Steps |
| :--- | :--- | :--- | :--- |
| `preset-technical-review` | Technical Review | Technical | 4 |
| `preset-marketing-polish` | Marketing Polish | Marketing | 4 |
| `preset-quick-edit` | Quick Edit | General | 1 |
| `preset-academic-review` | Academic Review | Academic | 3 |
| `preset-executive-summary` | Executive Summary | General | 3 |

---

## 5. Preset Workflow Definitions

### 5.1 Technical Review

**File:** `Resources/Workflows/technical-review.yaml`

```yaml
workflow_id: "preset-technical-review"
name: "Technical Review"
description: "Comprehensive technical document review: grammar, clarity, and style compliance"
icon: "file-code"
category: Technical

steps:
  - step_id: "edit"
    agent_id: "editor"
    persona_id: "strict"
    order: 1
    prompt_override: |
      Review this technical document with meticulous attention to detail.

      Focus on:
      1. Grammar and punctuation errors
      2. Technical accuracy and precision
      3. Unclear or ambiguous statements
      4. Inconsistent terminology
      5. Missing or incorrect technical references

      For each issue found, provide:
      - Location (paragraph/sentence identifier)
      - The problematic text
      - Your suggested correction
      - Severity rating (Critical/Major/Minor)

      Format your response as a structured list of findings.

  - step_id: "simplify"
    agent_id: "simplifier"
    order: 2
    prompt_override: |
      Analyze this technical document for readability and accessibility.

      Your task:
      1. Identify overly complex sentences (25+ words)
      2. Flag jargon that could be simplified for a broader audience
      3. Suggest paragraph breaks for dense content
      4. Recommend transition phrases where flow is choppy

      Target audience: Technical professionals who may not be domain experts
      Target reading level: 10th-12th grade (Flesch-Kincaid)

      Preserve all technical accuracy while improving clarity.
    condition:
      type: Expression
      expression: "settings.simplify_enabled != false"

  - step_id: "tune"
    agent_id: "tuning"
    order: 3
    prompt_override: |
      Review this document for style guide compliance.

      Check for:
      1. Consistent use of technical terms
      2. Proper capitalization of product names
      3. Correct formatting of code snippets
      4. Appropriate use of passive vs active voice
      5. Acronym definitions on first use

      Fix any style violations while preserving the technical content.
    condition:
      type: PreviousSuccess

  - step_id: "summarize"
    agent_id: "summarizer"
    order: 4
    prompt_override: |
      Generate comprehensive metadata for this technical document.

      Produce:
      1. Technical Abstract (150 words max)
         - Purpose of the document
         - Key technical concepts covered
         - Target audience

      2. Key Points (5-7 bullet points)
         - Most important technical takeaways

      3. Prerequisites (if applicable)
         - Required knowledge or setup

      4. Related Topics (3-5 suggestions)
         - Topics for further reading

      5. Suggested Tags (5-10 keywords)
         - For searchability and categorization

metadata:
  author: "Lexichord"
  version: "1.0.0"
  created_at: "2026-01-01T00:00:00Z"
  tags:
    - technical
    - documentation
    - review
    - comprehensive
  is_built_in: true
  required_tier: WriterPro
```

### 5.2 Marketing Polish

**File:** `Resources/Workflows/marketing-polish.yaml`

```yaml
workflow_id: "preset-marketing-polish"
name: "Marketing Polish"
description: "Transform content into compelling marketing copy with brand voice alignment"
icon: "megaphone"
category: Marketing

steps:
  - step_id: "simplify"
    agent_id: "simplifier"
    order: 1
    prompt_override: |
      Transform this content for maximum marketing impact.

      Apply these principles:
      1. Use active voice throughout
      2. Lead with benefits, not features
      3. Replace passive constructions with dynamic verbs
      4. Shorten sentences for punch (aim for 15-20 words max)
      5. Eliminate filler words and redundancies

      Target audience: General consumers
      Target reading level: 8th grade (accessible to all)

      Make every word earn its place. If it doesn't add value, cut it.

  - step_id: "edit"
    agent_id: "editor"
    persona_id: "friendly"
    order: 2
    prompt_override: |
      Polish this marketing content for maximum engagement.

      Enhance:
      1. Opening hooks - make them irresistible
      2. Calls-to-action - clear, urgent, benefit-driven
      3. Emotional resonance - connect with reader needs
      4. Power words - energize bland phrases
      5. Rhythm and flow - create momentum

      Avoid:
      - Marketing cliches ("best-in-class", "cutting-edge", "synergy")
      - Hyperbole without substance
      - Passive or weak constructions
      - Jargon that alienates

      The tone should be confident, conversational, and compelling.

  - step_id: "tune"
    agent_id: "tuning"
    order: 3
    prompt_override: |
      Ensure brand voice consistency throughout this marketing content.

      Verify:
      1. Tone matches brand personality (confident, approachable, expert)
      2. Terminology is consistent with brand lexicon
      3. Formatting follows marketing style guide
      4. No conflicting messages or mixed metaphors
      5. Legal/compliance language preserved where required

      Make subtle adjustments to strengthen brand alignment.
    condition:
      type: PreviousSuccess

  - step_id: "summarize"
    agent_id: "summarizer"
    order: 4
    prompt_override: |
      Generate marketing metadata for this content.

      Produce:
      1. Tagline (10 words max)
         - Catchy, memorable, benefit-focused

      2. Social Media Snippets
         - Twitter/X (280 chars)
         - LinkedIn (150 words)
         - Instagram caption (2200 chars with emojis)

      3. Key Selling Points (3 bullet points)
         - Each should stand alone

      4. Target Audience Definition
         - Demographics and psychographics

      5. Suggested A/B Test Variations (2 alternatives)
         - For the main headline

metadata:
  author: "Lexichord"
  version: "1.0.0"
  created_at: "2026-01-01T00:00:00Z"
  tags:
    - marketing
    - copywriting
    - brand
    - conversion
  is_built_in: true
  required_tier: WriterPro
```

### 5.3 Quick Edit

**File:** `Resources/Workflows/quick-edit.yaml`

```yaml
workflow_id: "preset-quick-edit"
name: "Quick Edit"
description: "Fast grammar and clarity check for any document"
icon: "zap"
category: General

steps:
  - step_id: "edit"
    agent_id: "editor"
    persona_id: "friendly"
    order: 1
    prompt_override: |
      Perform a quick, focused review of this text.

      Priority checks (in order):
      1. Grammar errors (subject-verb agreement, tense consistency)
      2. Spelling mistakes
      3. Punctuation problems
      4. Obviously unclear sentences

      Keep it fast:
      - Only flag significant issues
      - Skip minor style preferences
      - Focus on errors, not improvements
      - Limit to top 5 most impactful fixes

      Provide your response as a brief, actionable list.
      Format: "[Issue type] Line/sentence: [Problem] → [Fix]"

metadata:
  author: "Lexichord"
  version: "1.0.0"
  created_at: "2026-01-01T00:00:00Z"
  tags:
    - quick
    - edit
    - grammar
    - fast
  is_built_in: true
  required_tier: WriterPro
```

### 5.4 Academic Review

**File:** `Resources/Workflows/academic-review.yaml`

```yaml
workflow_id: "preset-academic-review"
name: "Academic Review"
description: "Scholarly document review with citation and formality checks"
icon: "graduation-cap"
category: Academic

steps:
  - step_id: "edit"
    agent_id: "editor"
    persona_id: "strict"
    order: 1
    prompt_override: |
      Review this academic document with scholarly rigor.

      Examine:
      1. Grammar and syntax (formal academic register)
      2. Logical argument structure and flow
      3. Claim-evidence alignment
      4. Hedging language appropriateness
      5. Citation placement and formatting
      6. Transition effectiveness between sections

      Academic standards:
      - Third person perspective (unless methodology requires first)
      - Formal vocabulary (no colloquialisms)
      - Precise, unambiguous statements
      - Proper qualification of claims

      For each issue, indicate its impact on scholarly credibility.

  - step_id: "tune"
    agent_id: "tuning"
    order: 2
    prompt_override: |
      Ensure this document meets academic writing standards.

      Verify compliance with:
      1. Formal academic tone throughout
      2. Consistent citation style (detect and maintain)
      3. Appropriate hedging ("suggests" vs "proves")
      4. Scholarly vocabulary usage
      5. Objective language (minimize subjective terms)
      6. Proper use of technical terminology

      Fix violations while preserving the author's argument.
    condition:
      type: PreviousSuccess

  - step_id: "summarize"
    agent_id: "summarizer"
    order: 3
    prompt_override: |
      Generate an academic abstract and metadata.

      Produce:
      1. Structured Abstract (250 words)
         - Background/Context (2-3 sentences)
         - Objective/Research Question
         - Methods/Approach (if applicable)
         - Results/Findings
         - Conclusions/Implications

      2. Keywords (5-7 academic terms)
         - For database indexing

      3. Discipline Classification
         - Primary and secondary fields

      4. Contribution Statement
         - How this advances the field (2-3 sentences)

      5. Suggested Venues
         - Appropriate journals or conferences (3-5)

metadata:
  author: "Lexichord"
  version: "1.0.0"
  created_at: "2026-01-01T00:00:00Z"
  tags:
    - academic
    - scholarly
    - research
    - formal
  is_built_in: true
  required_tier: Teams
```

### 5.5 Executive Summary

**File:** `Resources/Workflows/executive-summary.yaml`

```yaml
workflow_id: "preset-executive-summary"
name: "Executive Summary"
description: "Transform detailed content into executive-ready briefings"
icon: "briefcase"
category: General

steps:
  - step_id: "simplify"
    agent_id: "simplifier"
    order: 1
    prompt_override: |
      Condense this content for executive consumption.

      Apply these executive communication principles:
      1. Bottom Line Up Front (BLUF)
         - Lead with the key takeaway/recommendation
      2. Eliminate Technical Jargon
         - Replace with business impact language
      3. Focus on Business Outcomes
         - "What does this mean for the organization?"
      4. Highlight Decision Points
         - What actions are needed?
      5. Quantify Where Possible
         - Numbers, percentages, timelines

      Target reading level: 12th grade (educated but not specialist)
      Target length: Reduce to 20-30% of original length

  - step_id: "edit"
    agent_id: "editor"
    persona_id: "strict"
    order: 2
    prompt_override: |
      Refine this executive briefing for C-suite consumption.

      Ensure:
      1. Clear, direct statements (no hedging or qualifications)
      2. Action-oriented language ("Implement X" not "X could be implemented")
      3. Logical flow from situation to recommendation
      4. Professional, confident tone
      5. No redundancy or filler

      Structure check:
      - Does it answer "So what?" immediately?
      - Are recommendations specific and actionable?
      - Is the ask clear?

  - step_id: "summarize"
    agent_id: "summarizer"
    order: 3
    prompt_override: |
      Format this as a one-page executive summary.

      Structure:
      1. Situation (2-3 sentences)
         - Context the executive needs

      2. Key Findings (3-5 bullet points)
         - Most important insights
         - Each bullet self-contained

      3. Implications (2-3 sentences)
         - What this means for the business

      4. Recommended Actions (numbered list)
         - Specific, actionable steps
         - Include owners/timelines if available

      5. Next Steps / Timeline
         - Immediate actions required
         - Key milestones

      Total length: Must fit on one page (approximately 400-500 words)

metadata:
  author: "Lexichord"
  version: "1.0.0"
  created_at: "2026-01-01T00:00:00Z"
  tags:
    - executive
    - summary
    - business
    - briefing
  is_built_in: true
  required_tier: Teams
```

---

## 6. Implementation

### 6.1 Repository Implementation

```csharp
namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Repository for preset workflows loaded from embedded resources.
/// </summary>
public class PresetWorkflowRepository : IPresetWorkflowRepository
{
    private readonly IReadOnlyList<WorkflowDefinition> _presets;
    private readonly ILogger<PresetWorkflowRepository> _logger;

    public PresetWorkflowRepository(ILogger<PresetWorkflowRepository> logger)
    {
        _logger = logger;
        _presets = LoadPresets();
        _logger.LogDebug("Loaded {Count} preset workflows", _presets.Count);
    }

    public IReadOnlyList<WorkflowDefinition> GetAll() => _presets;

    public WorkflowDefinition? GetById(string workflowId) =>
        _presets.FirstOrDefault(p => p.WorkflowId == workflowId);

    public IReadOnlyList<WorkflowDefinition> GetByCategory(WorkflowCategory category) =>
        _presets.Where(p => p.Metadata.Category == category).ToList();

    public bool IsPreset(string workflowId) =>
        workflowId.StartsWith("preset-") && _presets.Any(p => p.WorkflowId == workflowId);

    public IReadOnlyList<PresetWorkflowSummary> GetSummaries() =>
        _presets.Select(p => new PresetWorkflowSummary(
            p.WorkflowId,
            p.Name,
            p.Description,
            p.IconName ?? "workflow",
            p.Metadata.Category,
            p.Steps.Count,
            p.Steps.Select(s => s.AgentId).Distinct().ToList(),
            p.Metadata.RequiredTier,
            p.Metadata.Tags.ToList()
        )).ToList();

    private IReadOnlyList<WorkflowDefinition> LoadPresets()
    {
        var presets = new List<WorkflowDefinition>();
        var assembly = typeof(PresetWorkflowRepository).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.Contains("Workflows") && n.EndsWith(".yaml"));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        foreach (var resourceName in resourceNames)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null) continue;

                using var reader = new StreamReader(stream);
                var yaml = reader.ReadToEnd();
                var workflow = DeserializeWorkflow(yaml, deserializer);
                presets.Add(workflow);

                _logger.LogDebug("Loaded preset workflow: {WorkflowId}", workflow.WorkflowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load preset workflow from {Resource}", resourceName);
            }
        }

        return presets.OrderBy(p => p.Name).ToList();
    }

    private static WorkflowDefinition DeserializeWorkflow(string yaml, IDeserializer deserializer)
    {
        var dto = deserializer.Deserialize<WorkflowYamlDto>(yaml);
        return dto.ToWorkflowDefinition();
    }
}

/// <summary>
/// DTO for YAML deserialization of workflow definitions.
/// </summary>
internal class WorkflowYamlDto
{
    public string WorkflowId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Icon { get; set; }
    public string Category { get; set; } = "General";
    public List<WorkflowStepYamlDto> Steps { get; set; } = new();
    public WorkflowMetadataYamlDto Metadata { get; set; } = new();

    public WorkflowDefinition ToWorkflowDefinition() =>
        new(
            WorkflowId,
            Name,
            Description,
            Icon,
            Steps.Select(s => s.ToStepDefinition()).ToList(),
            Metadata.ToMetadata(Category)
        );
}

internal class WorkflowStepYamlDto
{
    public string StepId { get; set; } = "";
    public string AgentId { get; set; } = "";
    public string? PersonaId { get; set; }
    public string? PromptOverride { get; set; }
    public int Order { get; set; }
    public WorkflowConditionYamlDto? Condition { get; set; }
    public Dictionary<string, string>? InputMappings { get; set; }
    public Dictionary<string, string>? OutputMappings { get; set; }

    public WorkflowStepDefinition ToStepDefinition() =>
        new(
            StepId,
            AgentId,
            PersonaId,
            PromptOverride,
            Order,
            Condition?.ToCondition(),
            InputMappings,
            OutputMappings
        );
}

internal class WorkflowConditionYamlDto
{
    public string Type { get; set; } = "Always";
    public string? Expression { get; set; }

    public WorkflowStepCondition ToCondition() =>
        new(
            Expression ?? "",
            Enum.TryParse<ConditionType>(Type, true, out var t) ? t : ConditionType.Always
        );
}

internal class WorkflowMetadataYamlDto
{
    public string Author { get; set; } = "Lexichord";
    public string? Version { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsBuiltIn { get; set; } = true;
    public string RequiredTier { get; set; } = "WriterPro";

    public WorkflowMetadata ToMetadata(string categoryStr) =>
        new(
            Author,
            CreatedAt ?? DateTime.UtcNow,
            CreatedAt ?? DateTime.UtcNow,
            Version,
            Tags,
            Enum.TryParse<WorkflowCategory>(categoryStr, true, out var c) ? c : WorkflowCategory.General,
            IsBuiltIn,
            Enum.TryParse<LicenseTier>(RequiredTier, true, out var t) ? t : LicenseTier.WriterPro
        );
}
```

---

## 7. UI/UX Specifications

### 7.1 Preset Workflow Cards

```text
+-----------------------------------------------+
| [icon] Technical Review                [Teams]|
|-----------------------------------------------|
| Comprehensive technical document review:      |
| grammar, clarity, and style compliance        |
|                                               |
| Steps: Editor -> Simplifier -> Tuning ->      |
|        Summarizer                             |
|                                               |
| [Run Workflow]                [View Details]  |
+-----------------------------------------------+

+-----------------------------------------------+
| [icon] Quick Edit                  [WriterPro]|
|-----------------------------------------------|
| Fast grammar and clarity check for any        |
| document                                      |
|                                               |
| Steps: Editor                                 |
|                                               |
| [Run Workflow]                [View Details]  |
+-----------------------------------------------+
```

### 7.2 Preset Details Modal

```text
+---------------------------------------------------------------+
|  Technical Review                                    [X Close] |
+---------------------------------------------------------------+
| Comprehensive technical document review: grammar, clarity,     |
| and style compliance                                           |
+---------------------------------------------------------------+
| WORKFLOW STEPS                                                 |
+---------------------------------------------------------------+
| 1. [icon] Editor (Strict)                                      |
|    Review technical document for grammar, accuracy,            |
|    and terminology consistency                                 |
|                                                                |
| 2. [icon] Simplifier                                           |
|    Simplify complex explanations for broader audience          |
|    Condition: If simplify_enabled setting is true              |
|                                                                |
| 3. [icon] Tuning                                               |
|    Fix remaining style guide violations                        |
|    Condition: If previous step succeeded                       |
|                                                                |
| 4. [icon] Summarizer                                           |
|    Generate technical abstract and key points                  |
+---------------------------------------------------------------+
| Tags: technical, documentation, review, comprehensive          |
+---------------------------------------------------------------+
| [Run Workflow]               [Duplicate to Edit (Teams)]       |
+---------------------------------------------------------------+
```

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Loaded {Count} preset workflows from embedded resources"` |
| Debug | `"Loaded preset workflow: {WorkflowId}"` |
| Info | `"Preset workflow {WorkflowId} executed by user"` |
| Warning | `"Failed to load preset workflow from {Resource}"` |
| Error | `"Preset workflow deserialization failed: {Error}"` |

---

## 9. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | App starts | Presets loaded | 5 preset workflows available |
| 2 | User views presets | GetAll() called | Returns all 5 presets |
| 3 | User requests by ID | GetById("preset-technical-review") | Returns Technical Review workflow |
| 4 | User filters by category | GetByCategory(Technical) | Returns Technical Review |
| 5 | Technical Review executed | All steps run | Document reviewed by 4 agents |
| 6 | Marketing Polish executed | All steps run | Content enhanced for marketing |
| 7 | Quick Edit executed | Single step runs | Fast grammar check completed |
| 8 | Academic Review executed | All steps run | Scholarly document reviewed |
| 9 | Executive Summary executed | All steps run | Executive briefing generated |
| 10 | WriterPro user | Runs preset | Execution succeeds |
| 11 | Teams user | Duplicates preset | Creates editable copy |
| 12 | Core user | Tries to run preset | Upgrade prompt shown |

---

## 10. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7c")]
public class PresetWorkflowRepositoryTests
{
    private readonly PresetWorkflowRepository _sut;

    public PresetWorkflowRepositoryTests()
    {
        _sut = new PresetWorkflowRepository(Mock.Of<ILogger<PresetWorkflowRepository>>());
    }

    [Fact]
    public void GetAll_ReturnsExpectedPresetCount()
    {
        var presets = _sut.GetAll();

        presets.Should().HaveCount(5);
    }

    [Theory]
    [InlineData("preset-technical-review", "Technical Review")]
    [InlineData("preset-marketing-polish", "Marketing Polish")]
    [InlineData("preset-quick-edit", "Quick Edit")]
    [InlineData("preset-academic-review", "Academic Review")]
    [InlineData("preset-executive-summary", "Executive Summary")]
    public void GetById_KnownPreset_ReturnsWorkflow(string id, string expectedName)
    {
        var preset = _sut.GetById(id);

        preset.Should().NotBeNull();
        preset!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var preset = _sut.GetById("unknown-workflow");

        preset.Should().BeNull();
    }

    [Fact]
    public void GetByCategory_Technical_ReturnsTechnicalReview()
    {
        var presets = _sut.GetByCategory(WorkflowCategory.Technical);

        presets.Should().HaveCount(1);
        presets[0].WorkflowId.Should().Be("preset-technical-review");
    }

    [Fact]
    public void GetByCategory_Marketing_ReturnsMarketingPolish()
    {
        var presets = _sut.GetByCategory(WorkflowCategory.Marketing);

        presets.Should().HaveCount(1);
        presets[0].WorkflowId.Should().Be("preset-marketing-polish");
    }

    [Fact]
    public void IsPreset_WithPresetId_ReturnsTrue()
    {
        var result = _sut.IsPreset("preset-technical-review");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsPreset_WithCustomId_ReturnsFalse()
    {
        var result = _sut.IsPreset("custom-workflow-123");

        result.Should().BeFalse();
    }

    [Fact]
    public void TechnicalReview_HasExpectedSteps()
    {
        var preset = _sut.GetById("preset-technical-review");

        preset!.Steps.Should().HaveCount(4);
        preset.Steps.Select(s => s.AgentId).Should().ContainInOrder(
            "editor", "simplifier", "tuning", "summarizer");
    }

    [Fact]
    public void QuickEdit_HasSingleStep()
    {
        var preset = _sut.GetById("preset-quick-edit");

        preset!.Steps.Should().HaveCount(1);
        preset.Steps[0].AgentId.Should().Be("editor");
    }

    [Fact]
    public void AllPresets_HaveIsBuiltInTrue()
    {
        var presets = _sut.GetAll();

        presets.Should().AllSatisfy(p => p.Metadata.IsBuiltIn.Should().BeTrue());
    }

    [Fact]
    public void AllPresets_HaveValidAgentIds()
    {
        var validAgentIds = new[] { "editor", "simplifier", "tuning", "summarizer", "copilot" };
        var presets = _sut.GetAll();

        foreach (var preset in presets)
        {
            foreach (var step in preset.Steps)
            {
                validAgentIds.Should().Contain(step.AgentId,
                    $"Preset {preset.WorkflowId} step {step.StepId} has invalid agent {step.AgentId}");
            }
        }
    }
}

[Trait("Category", "Integration")]
[Trait("Version", "v0.7.7c")]
public class PresetWorkflowExecutionTests
{
    private readonly IWorkflowEngine _engine;
    private readonly IPresetWorkflowRepository _presets;

    public PresetWorkflowExecutionTests()
    {
        var services = TestServiceProvider.CreateWithMockAgents();
        _engine = services.GetRequiredService<IWorkflowEngine>();
        _presets = services.GetRequiredService<IPresetWorkflowRepository>();
    }

    [Theory]
    [InlineData("preset-technical-review")]
    [InlineData("preset-marketing-polish")]
    [InlineData("preset-quick-edit")]
    [InlineData("preset-academic-review")]
    [InlineData("preset-executive-summary")]
    public async Task PresetWorkflow_ExecutesSuccessfully(string workflowId)
    {
        // Arrange
        var workflow = _presets.GetById(workflowId);
        var context = new WorkflowExecutionContext(
            null,
            "Sample text for testing workflow execution.",
            new Dictionary<string, object>(),
            new WorkflowExecutionOptions());

        // Act
        var result = await _engine.ExecuteAsync(workflow!, context);

        // Assert
        result.Success.Should().BeTrue($"Preset workflow {workflowId} should execute successfully");
        result.FinalOutput.Should().NotBeNullOrEmpty();
    }
}
```

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `IPresetWorkflowRepository.cs` interface | [ ] |
| 2 | `PresetWorkflowRepository.cs` implementation | [ ] |
| 3 | `PresetWorkflowSummary.cs` record | [ ] |
| 4 | `technical-review.yaml` | [ ] |
| 5 | `marketing-polish.yaml` | [ ] |
| 6 | `quick-edit.yaml` | [ ] |
| 7 | `academic-review.yaml` | [ ] |
| 8 | `executive-summary.yaml` | [ ] |
| 9 | Embedded resource configuration | [ ] |
| 10 | Unit tests for PresetWorkflowRepository | [ ] |
| 11 | Integration tests for preset execution | [ ] |

---

## 12. Verification Commands

```bash
# Run preset repository tests
dotnet test --filter "FullyQualifiedName~PresetWorkflow"

# Run all v0.7.7c tests
dotnet test --filter "Version=v0.7.7c"

# Verify embedded resources
dotnet build && unzip -l bin/Debug/net9.0/Lexichord.Modules.Agents.dll | grep yaml

# Manual verification:
# a) Open Agents panel -> Workflows tab
# b) Verify 5 preset workflows listed
# c) Click each preset to view details
# d) Run "Quick Edit" on sample text
# e) Run "Technical Review" on technical doc
# f) Verify all steps execute
# g) Test with different license tiers
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
