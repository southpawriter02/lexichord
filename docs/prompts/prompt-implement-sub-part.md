# Prompt: Implement Sub-Part from Design Specification

## Usage Instructions

Replace the placeholders below with the specific sub-part you want to implement:

- `{VERSION}` → e.g., `v0.5.6a`
- `{SUB_PART_TITLE}` → e.g., `Snippet Extraction Service`

---

## Prompt Template

```
Implement sub-part {VERSION}: {SUB_PART_TITLE} for the Lexichord project.

## Required Reading (in order)

Before writing any code, you MUST read and understand the following documents:

1. **Design Specification** (primary source of truth):
   @LCS-DES-{VERSION_NUMBER}{LETTER}.md

2. **Version Index** (full feature context):
   @LCS-DES-{VERSION_NUMBER}-INDEX.md

3. **Scope Breakdown** (implementation checklist and dependencies):
   @LCS-SBD-{VERSION_NUMBER}.md

4. **Dependency Matrix** (interface sources and ghost dependency prevention):
   @DEPENDENCY-MATRIX.md

## Implementation Rules

### ✅ DO:
- Implement ONLY what is explicitly defined in the design specification for this sub-part
- Use the exact interface names, method signatures, and record definitions from the spec
- Follow the code examples provided in the design specification
- Import dependencies from the versions specified in the Dependency Matrix
- Include all XML documentation comments as specified
- Implement all unit tests outlined in the design specification
- Register services in DI as specified in the scope breakdown
- Follow the decision trees and algorithm flows exactly as documented

### ❌ DO NOT:
- Add features, parameters, or methods not defined in the specification
- Modify interfaces or contracts from prior versions
- Implement functionality from other sub-parts (defer to their specifications)
- Create "ghost dependencies" (interfaces that don't exist in the matrix)
- Skip unit tests or logging requirements
- Change namespace conventions or file locations

### Dependency Verification Checklist:
Before using ANY dependency, verify it exists in DEPENDENCY-MATRIX.md:
- [ ] Interface exists in Section 1 (Type Registry) with correct version
- [ ] NuGet package exists in Section 3 (NuGet Package Registry)
- [ ] No entries in Section 4 (Ghost Dependencies) match what you're using

### Output Requirements:
1. Create all files in the correct locations per the specification
2. Follow the exact file structure outlined in the design spec
3. Include complete implementation with XML docs
4. Include all unit tests in the appropriate test project
5. Update DI registration in the module file if specified

## Verification After Implementation

After implementing, confirm:
- [ ] All interfaces match the design specification exactly
- [ ] All records/DTOs match the design specification exactly
- [ ] All unit tests from the spec are implemented
- [ ] Logging follows the specification
- [ ] License gating is implemented if specified
- [ ] No additional features were added beyond the spec
```

---

## Quick Reference: Document ID Patterns

| Document Type     | Pattern                    | Example                |
| :---------------- | :------------------------- | :--------------------- |
| Design Spec       | `LCS-DES-{VER}{LETTER}.md` | `LCS-DES-056a.md`      |
| Version Index     | `LCS-DES-{VER}-INDEX.md`   | `LCS-DES-056-INDEX.md` |
| Scope Breakdown   | `LCS-SBD-{VER}.md`         | `LCS-SBD-056.md`       |
| Dependency Matrix | `DEPENDENCY-MATRIX.md`     | (single file)          |

---

## Example Usage

### Example 1: Implementing v0.5.6a (Snippet Extraction Service)

```
Implement sub-part v0.5.6a: Snippet Extraction Service for the Lexichord project.

## Required Reading (in order)

Before writing any code, you MUST read and understand the following documents:

1. **Design Specification** (primary source of truth):
   @LCS-DES-056a.md

2. **Version Index** (full feature context):
   @LCS-DES-056-INDEX.md

3. **Scope Breakdown** (implementation checklist and dependencies):
   @LCS-SBD-056.md

4. **Dependency Matrix** (interface sources and ghost dependency prevention):
   @DEPENDENCY-MATRIX.md

[... rest of prompt ...]
```

### Example 2: Implementing v0.5.8d (Error Resilience)

```
Implement sub-part v0.5.8d: Graceful Degradation & Error Handling for the Lexichord project.

## Required Reading (in order)

Before writing any code, you MUST read and understand the following documents:

1. **Design Specification** (primary source of truth):
   @LCS-DES-058d.md

2. **Version Index** (full feature context):
   @LCS-DES-058-INDEX.md

3. **Scope Breakdown** (implementation checklist and dependencies):
   @LCS-SBD-058.md

4. **Dependency Matrix** (interface sources and ghost dependency prevention):
   @DEPENDENCY-MATRIX.md

[... rest of prompt ...]
```

---

## Troubleshooting Ghost Dependencies

If the AI references an interface or service that doesn't exist:

1. Check **Section 4: Ghost Dependencies to Avoid** in DEPENDENCY-MATRIX.md
2. Find the correct replacement in the "Correct Alternative" column
3. Verify the correct version number in Section 1

Common ghost dependencies to watch for:

- `IConfigurationService` → Use `IConfiguration` (v0.0.3d)
- `Serilog` direct usage → Use `ILogger<T>` (v0.0.3b)
- `ViewModelBase` from wrong source → Use CommunityToolkit.Mvvm

---

## Post-Implementation: Updating Prerequisites

After successful implementation, update **DEPENDENCY-MATRIX.md** Section 6 to check off completed items:

```markdown
### v0.5.6 Prerequisites for v0.5.7+

- [x] ISnippetService interface (v0.5.6a) ← Mark as complete
- [x] SnippetService implementation (v0.5.6a) ← Mark as complete
- [ ] IHighlightRenderer interface (v0.5.6b) ← Still pending
```
