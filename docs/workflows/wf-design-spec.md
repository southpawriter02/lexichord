# Workflow: Drafting Design Specifications (LDS-01)

## 1. Philosophy: The Spec is the Score
In Lexichord, we do not improvise code. We compose it. The Design Specification (LDS-01) is the "Sheet Music" that the ensemble (developers and agents) will perform.

**Voice & Tone Standards for Specifications:**
*   **Imperative & Absolute:** Avoid "should," "might," or "could." Use **"MUST," "SHALL,"** and **"WILL."**
    *   *Bad:* "The system should probably check the license."
    *   *Good:* "The `LicenseService` **SHALL** validate the `AGT-05` feature key prior to execution."
*   **Structural:** Speak in terms of layers (Host, Module, Abstraction).
*   **Musical/Architectural:** Use the Lexichord taxonomy (Podium, Tuning Fork, Score, Ensemble) where strictly accurate, but revert to standard C# terminology (Service, Repository, Interface) for technical precision.

---

## 2. Decision Tree: "Where does this feature live?"
*Before writing the spec, traverse this tree to determine Module Scope and License Tier.*

```text
START
├── Is this a shared utility used by ALL modules? (e.g., Encryption, Logging)
│   └── YES -> Scope: Lexichord.Host (The Podium) -> Tier: Core
│
├── Is this specific to a functional vertical?
│   ├── Governance/Style? -> Scope: Lexichord.Modules.Style
│   ├── RAG/Search?       -> Scope: Lexichord.Modules.Rag
│   ├── AI Agents?        -> Scope: Lexichord.Modules.Agents
│   ├── Terminal/Git?     -> Scope: Lexichord.Modules.DevTools
│   └── Creative Lore?    -> Scope: Lexichord.Modules.Creative
│
└── Licensing Logic (If Module-scoped):
    ├── Does it incur recurring API costs (OpenAI Tokens)?
    │   └── YES -> Tier: Writer Pro (or higher)
    ├── Does it automate a complex team workflow?
    │   └── YES -> Tier: Teams
    ├── Does it involve compliance, security, or SSO?
    │   └── YES -> Tier: Enterprise
    └── NO to all -> Tier: Core (Community)
```

---

## 3. The Deliverable Checklist
*A LDS-01 Draft is considered **INCOMPLETE** unless it contains:*

1.  [ ] **Metadata Header:** Must include Feature ID, Target Version, and Feature Gate Key.
2.  [ ] **Dependency Matrix:** Explicitly listing upstream/downstream module communication.
3.  [ ] **License Gate Strategy:** A defined behavior for when a user *lacks* the license (Hard fail vs. Soft UI lock).
4.  [ ] **Data Contract:** The exact C# Interface (`interface I...`) definition.
5.  [ ] **Schema Changes:** FluentMigrator steps (if DB is touched).
6.  [ ] **Observability Plan:** Specific log message templates.
7.  [ ] **Test Scenarios:** At least 3 specific "Given/When/Then" cases.

---

## 4. Technical Requirements & Examples

### 4.1 Interface Design & Inline Documentation
**Mandate:** All interfaces in the spec must include XML summary tags. This is not optional. It feeds the AI context window during implementation.

*Spec Example:*
```csharp
namespace Lexichord.Modules.Style.Abstractions;

/// <summary>
/// The governance engine responsible for analyzing text against the Lexicon.
/// </summary>
public interface ITuningEngine
{
    /// <summary>
    /// Analyzes a text segment and returns a dissonance score.
    /// </summary>
    /// <param name="content">The raw text content from the editor.</param>
    /// <param name="profileId">The Voice Profile ID (e.g., "Corporate", "Technical").</param>
    /// <returns>A collection of style violations.</returns>
    /// <exception cref="LicenseException">Thrown if the user is on Core tier and attempts Deep Analysis.</exception>
    Task<AnalysisResult> AnalyzeResonanceAsync(string content, string profileId);
}
```

### 4.2 Logging Standards
**Mandate:** Logging must be structured. Do not use string interpolation (`$""`) for variables. Use message templates.

*Spec Example (Observability Section):*
*   **Bad:** `Log.Info($"User {userId} checked text {textId}");`
*   **Good:** `Log.Information("Resonance analysis requested for Doc {DocumentId} by User {UserId}. Profile: {ProfileId}", doc.Id, user.Id, profile.Id);`

### 4.3 Modular Licensing Implementation
**Mandate:** The spec must explicitly show how the feature guards itself.

*Spec Example:*
```csharp
// In the implementation plan:
public async Task<AnalysisResult> AnalyzeResonanceAsync(string content, string profileId)
{
    // LOGIC: Check license capability before consuming AI resources
    if (!_licenseService.HasFeature("STY-06")) 
    {
        _logger.LogWarning("Resonance Analysis blocked. Upgrade required.");
        return AnalysisResult.Locked("Feature requires Writer Pro license.");
    }

    // ... Implementation
}
```

---

## 5. Testing Requirements Guidelines

The spec must dictate **how** the feature is verified without spinning up the full UI.

### 5.1 Unit Test Template (Logic Verification)
*Requirement:* Define the "Happy Path" and the "License Fail Path."

*   **Scenario:** `User_With_Core_License_Attempts_AI_Scan`
    *   **Setup:** Mock `ILicenseService` to return `false` for `STY-06`.
    *   **Action:** Call `AnalyzeResonanceAsync`.
    *   **Assertion:** Result should be `AnalysisResult.Locked`.

### 5.2 Integration Test Template (Module Loading)
*Requirement:* Define how to prove the DLL loads correctly.

*   **Scenario:** `Style_Module_Bootstraps_Successfully`
    *   **Setup:** `Host.LoadModule("Lexichord.Modules.Style.dll")`.
    *   **Action:** Resolve `ITuningEngine` from DI Container.
    *   **Assertion:** Service is not null.

---

## 6. General Rules for the Architect

1.  **The "No-Host" Rule:** Never design a feature that requires the `Lexichord.Host` project to change code, unless it is a fundamental infrastructure change (like updating .NET version). Modules must adapt to the Host, not vice versa.
2.  **The "Event Bus" Rule:** If Module A needs to trigger Module B (e.g., "Agent needs to run Git command"), the spec must define an **Event** or a **Shared Abstraction**, not a direct method call.
    *   *Correct:* `AgentModule` publishes `GitPullRequestedEvent`. `GitModule` subscribes to `GitPullRequestedEvent`.
3.  **The "Data Sovereignty" Rule:** User data (Docs, API Keys) must never leave the local machine unless explicitly sent to a defined AI Provider Endpoint. The spec must identify *exactly* where data flows.

---

## 7. Sample Workflow: Writing a Spec

1.  **Ingest Context:** "I need to design the Feature `[AGT-05]: Release Notes Agent`."
2.  **Consult Decision Tree:**
    *   *Module?* Agents.
    *   *License?* Teams (Automates workflow).
3.  **Draft Metadata:** Fill in the header.
4.  **Define Contract:** Write the `IReleaseAgent` interface in C#.
5.  **Define License Gate:** "Button is visible but disabled for Pro/Core users."
6.  **Write Tests:** "Mock the GitService returning 5 commits. Ensure Agent generates 5 bullet points."
7.  **Review:** Does this require changing the Host? No. -> **Approved.**