### The Licensing Philosophy: "Managed" vs. "BYOK"

Since Lexichord is an AI-heavy desktop app, cost management is critical.
1.  **Subscription (SaaS):** The user pays a monthly fee. Lexichord provides the AI API keys (managed service).
2.  **Perpetual (BYOK):** The user pays a one-time fee for the software version. The user **Brings Your Own Key** (OpenAI/Anthropic API Key). This creates a sustainable business model where you don't go bankrupt paying for a perpetual user's tokens.

---

### The Lexichord Feature Matrix

| Module Code | Feature / Capability | **Core** (Community) | **Writer Pro** (Individual) | **Teams** (Business) | **Enterprise** (Corp) |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **INFRASTRUCTURE** | **Pricing Model** | **Free** | **$20/mo** (or $150 Lifetime BYOK) | **$50/seat/mo** | **Custom** |
| `[PLT-01]` | Host Shell & Plugin Loader | ✅ | ✅ | ✅ | ✅ |
| `[PLT-03]` | PostgreSQL Database (Local) | ✅ | ✅ | ✅ | ✅ |
| `[EDT-01]` | Markdown Editor | ✅ | ✅ | ✅ | ✅ |
| `[PLT-05]` | Theme Manager (Dark/Light) | ✅ | ✅ | ✅ | ✅ |
| `[EXP-01]` | Static Site Gen (MkDocs) | ❌ | ✅ | ✅ | ✅ |
| **GOVERNANCE** | *(The Tuning Fork)* | | | | |
| `[STY-01]` | YAML Style Parser | ✅ | ✅ | ✅ | ✅ |
| `[STY-02]` | Terminology DB (Basic) | ✅ | ✅ | ✅ | ✅ |
| `[STY-03]` | Linter (Regex) | ✅ | ✅ | ✅ | ✅ |
| `[STY-04]` | Fuzzy Matching (Typos) | ❌ | ✅ | ✅ | ✅ |
| `[STY-06]` | Voice/Tone Metrics (AI) | ❌ | ✅ | ✅ | ✅ |
| `[STY-05]` | Dictionary Manager GUI | ❌ | ✅ | ✅ | ✅ |
| **MEMORY (RAG)** | *(The Score)* | | | | |
| `[RAG-01]` | Vector Storage (pgvector) | ❌ | ✅ | ✅ | ✅ |
| `[RAG-02]` | Local Doc Ingestion | ❌ | ✅ | ✅ | ✅ |
| `[RAG-05]` | Semantic Search Panel | ❌ | ✅ | ✅ | ✅ |
| `[RAG-10]` | Knowledge Graph (Advanced) | ❌ | ❌ | ❌ | ✅ |
| **AGENTS** | *(The Ensemble)* | | | | |
| `[AGT-01]` | Co-pilot (Chat) | ❌ | ✅ | ✅ | ✅ |
| `[AGT-03]` | Tuning Agent (Auto-fix) | ❌ | ✅ | ✅ | ✅ |
| `[AGT-05]` | Release Notes Agent | ❌ | ❌ | ✅ | ✅ |
| `[AGT-02]` | Custom Agent Personas | ❌ | ❌ | ✅ | ✅ |
| `[WKF-01]` | Workflow Orchestration | ❌ | ❌ | ✅ | ✅ |
| **DEV TOOLS** | *(The Acoustics)* | | | | |
| `[GIT-01]` | Git Repo Reader | ❌ | ✅ | ✅ | ✅ |
| `[TRM-01]` | Integrated Terminal | ❌ | ❌ | ✅ | ✅ |
| `[COD-01]` | Scaffold/Code Agents | ❌ | ❌ | ✅ | ✅ |
| **MANAGEMENT** | *(The Tower)* | | | | |
| `[COL-02]` | Inline Comments/Review | ❌ | ❌ | ✅ | ✅ |
| `[ENT-01]` | RBAC (Role Management) | ❌ | ❌ | ❌ | ✅ |
| `[AI-11]` | Local LLM (Ollama) | ❌ | ✅ (BYO) | ✅ (BYO) | ✅ |
| `[ENT-02]` | Audit Logs | ❌ | ❌ | ❌ | ✅ |

---

### Licensing Models Explained

#### 1. Core (Community Edition)
*   **Target:** Students, Open Source Contributors.
*   **Mechanism:** Public download. No license key required.
*   **Limitation:** The `ModuleLoader` checks for the existence of specific "Pro" DLLs. If they are present (downloaded separately), they won't load without a signed license key file.
*   **Value:** A very good, strict Markdown editor that helps you follow a style guide manually.

#### 2. Writer Pro (The "Freelancer" License)
*   **Target:** Individual Technical Writers, Novelists.
*   **Option A: Subscription ($20/mo)**
    *   Includes a "Managed" API key for OpenAI/Anthropic with a generous monthly token cap (e.g., 2M tokens).
    *   If they hit the cap, they can add their own key.
*   **Option B: Perpetual License ($150 one-time)**
    *   **BYOK (Bring Your Own Key):** The user enters their own OpenAI API Key in the settings.
    *   They own the software version (e.g., v1.x) forever.
    *   Includes 1 year of software updates.
*   **Key Features:** Unlocks the **RAG Engine** (Memory) and **Basic Agents**. This transforms the tool from an editor to an assistant.

#### 3. Teams (The "Agency" License)
*   **Target:** Dev Shops, Documentation Agencies, Game Studios.
*   **Model:** Subscription only ($50/seat/mo).
*   **Why Subscription?** Teams features (Workflows, Git integration, Release Agents) require complex logic updates and consistent syncing that are hard to support on perpetual licenses.
*   **Key Features:**
    *   **Orchestration:** Access to the `Release Notes Agent` and `Workflow Engine`.
    *   **Shared Config:** Ability to host a `styleguide.yaml` on a shared network drive or Git repo that locks settings for all users.
    *   **Dev Tools:** Terminal and Scaffolding agents.

#### 4. Enterprise (The "Corp" License)
*   **Target:** Fortune 500, Banks, Defense.
*   **Model:** Annual Contract.
*   **Key Features:**
    *   **Local-Only Mode:** Ability to force the `AI-11` (Ollama) module to be the *only* active AI provider (no data leaves the laptop).
    *   **SSO/RBAC:** Integration with Active Directory.
    *   **Audit Logging:** Logs every prompt sent to an AI for compliance.

---

### Implementation Strategy: The "License Manager" Module

To make this matrix reality, you need to implement a **License Capability Check** in your code.

**The Interface:**
```csharp
public interface ILicenseService
{
    bool IsFeatureEnabled(string featureCode); // e.g., "RAG-01"
    int GetTokenLimit();
    bool IsByokRequired();
}
```

**The Logic (in `[PLT-07]`):**
```csharp
public class LicenseService : ILicenseService
{
    public bool IsFeatureEnabled(string featureCode)
    {
        var license = _currentLicense; // Loaded from encrypted file
        
        // Example: Only Teams/Ent can use Release Notes Agent
        if (featureCode == "AGT-05" && license.Tier < LicenseTier.Teams)
            return false;
            
        // Example: Pro and above can use Fuzzy Matching
        if (featureCode == "STY-04" && license.Tier < LicenseTier.Pro)
            return false;

        return true;
    }
}
```

**The UI Binding:**
Buttons and Menu Items in AvaloniaUI will bind their `IsVisible` property to this service.
*   *Result:* A "Free" user literally doesn't see the "Generate Release Notes" button, or sees it with a generic "Lock" icon depending on your UX preference.