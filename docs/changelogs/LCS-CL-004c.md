# Changelog: v0.0.4c — The License Gate (Skeleton)

| Field    | Value            |
| -------- | ---------------- |
| Version  | v0.0.4c          |
| Codename | The License Gate |
| Date     | 2026-01-28       |
| Spec     | LCS-DES-004c     |
| Status   | Complete         |

---

## Summary

Implements the skeleton license system that enables tiered feature gating. This establishes the API contracts that v1.x will implement with real license validation.

---

## What's New

### Abstractions (Expanded)

- **`LicenseTier`** enum expanded with comprehensive XML documentation explaining tier hierarchy and comparison semantics
- **`RequiresLicenseAttribute`** expanded with `FeatureCode` as `init` property for granular gating within tiers
- **`ILicenseContext`** expanded with two new methods:
    - `GetExpirationDate()`: Returns license expiration (null for perpetual)
    - `GetLicenseeName()`: Returns licensee name for UI display

### Host Services (Expanded)

- **`HardcodedLicenseContext`** expanded with:
    - Constructor accepting optional `ILogger<HardcodedLicenseContext>`
    - Logging of license checks for diagnostics
    - Implementation of `GetExpirationDate()` (returns null)
    - Implementation of `GetLicenseeName()` (returns "Development License")

### DI Registration

- **`HostServices.cs`** updated to register `ILicenseContext` as singleton

---

## Files Modified

| File                                                 | Changes                                  |
| ---------------------------------------------------- | ---------------------------------------- |
| `Abstractions/Contracts/LicenseTier.cs`              | Expanded XML docs                        |
| `Abstractions/Contracts/RequiresLicenseAttribute.cs` | FeatureCode as init property             |
| `Abstractions/Contracts/ILicenseContext.cs`          | Added GetExpirationDate, GetLicenseeName |
| `Host/Services/HardcodedLicenseContext.cs`           | Added logging, implemented new methods   |
| `Host/HostServices.cs`                               | Added ILicenseContext DI registration    |

---

## Testing

| Test File                          | Tests  | Status |
| ---------------------------------- | ------ | ------ |
| `LicenseTierTests.cs`              | 7      | ✅     |
| `RequiresLicenseAttributeTests.cs` | 5      | ✅     |
| `ILicenseContextTests.cs`          | 4      | ✅     |
| `HardcodedLicenseContextTests.cs`  | 7      | ✅     |
| **Total**                          | **23** | ✅     |

---

## Security Note

> [!WARNING]
> **v0.0.4 License Gating is NOT Secure**
>
> The skeleton implementation provides **no real security**. Users can bypass license checks by modifying source code, removing attributes, or replacing DI registrations. Real security requires server-side validation (v1.x).

---

## Related Documents

- [LCS-DES-004c](../specs/v0.0.x/v0.0.4/LCS-DES-004c.md) — Design Specification
- [LCS-CL-004b](./LCS-CL-004b.md) — ModuleLoader changelog
- [LCS-DES-004d](../specs/v0.0.x/v0.0.4/LCS-DES-004d.md) — Sandbox Module (next)
