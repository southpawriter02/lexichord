# LCS-CL-017b: Signing Infrastructure

**Version**: v0.1.7b  
**Category**: Distribution & Updates  
**Status**: ✅ Complete

---

## Overview

Implements code signing infrastructure for Windows (PFX/SignTool) and macOS (Developer ID/Notarization) releases. Signed binaries avoid security warnings from SmartScreen and Gatekeeper.

---

## Changes

### Build Infrastructure

#### New Files

| File                                                                                               | Purpose                                               |
| -------------------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| [entitlements.plist](file:///Users/ryan/Documents/GitHub/lexichord/build/macos/entitlements.plist) | macOS entitlements for .NET/Avalonia code signing     |
| [sign-windows.ps1](file:///Users/ryan/Documents/GitHub/lexichord/build/scripts/sign-windows.ps1)   | Windows code signing with SignTool + SHA256 timestamp |
| [sign-macos.sh](file:///Users/ryan/Documents/GitHub/lexichord/build/scripts/sign-macos.sh)         | macOS signing + Apple notarization                    |
| [release.yml](file:///Users/ryan/Documents/GitHub/lexichord/.github/workflows/release.yml)         | GitHub Actions release pipeline                       |

---

### macOS Entitlements

```xml
com.apple.security.cs.allow-jit                   <!-- .NET JIT -->
com.apple.security.cs.allow-unsigned-executable-memory
com.apple.security.cs.disable-library-validation  <!-- Assembly loading -->
com.apple.security.network.client                 <!-- Updates, API -->
com.apple.security.files.user-selected.read-write <!-- File dialogs -->
```

---

### Windows Signing Script

**Parameters:**

- `-ExePath` — Executable to sign
- `-CertificatePath` — PFX certificate path
- `-CertificatePassword` — PFX password
- `-TimestampUrl` — RFC 3161 server (default: DigiCert)

**Process:**

1. Locate SignTool.exe from Windows SDK
2. Sign with SHA256 + timestamp
3. Verify signature

---

### macOS Signing Script

**Environment Variables:**

- `APPLE_DEVELOPER_ID` — Certificate identity
- `APPLE_TEAM_ID` — 10-char team ID
- `APPLE_ID` — Apple ID email
- `APPLE_APP_SPECIFIC_PASSWORD` — App password

**Process:**

1. Sign nested Mach-O binaries with entitlements
2. Sign dylibs and frameworks
3. Sign main .app bundle
4. Verify signature with `codesign --verify --strict`
5. (Optional) Notarize with `notarytool` + staple ticket

---

### Release Workflow

**Trigger:** `v*` tags (e.g., `v0.1.7`)

**Jobs:**

| Job              | Runner           | Purpose                                       |
| ---------------- | ---------------- | --------------------------------------------- |
| `build-windows`  | `windows-latest` | Build, sign (optional), package with Velopack |
| `build-macos`    | `macos-latest`   | Build, sign/notarize (optional), create DMG   |
| `create-release` | `ubuntu-latest`  | Download artifacts, create GitHub Release     |

**Secrets Required:**

| Secret                         | Purpose                   |
| ------------------------------ | ------------------------- |
| `WINDOWS_CERTIFICATE_BASE64`   | Base64-encoded PFX        |
| `WINDOWS_CERTIFICATE_PASSWORD` | PFX password              |
| `APPLE_DEVELOPER_ID`           | Developer ID identity     |
| `APPLE_TEAM_ID`                | Apple Team ID             |
| `APPLE_ID`                     | Apple ID for notarization |
| `APPLE_APP_SPECIFIC_PASSWORD`  | App-specific password     |

> [!NOTE]
> Signing steps are conditionally skipped if certificates are not configured. Unsigned builds will still be produced.

---

## Dependencies

| Upstream                       | Purpose                                       |
| ------------------------------ | --------------------------------------------- |
| v0.1.7a (Velopack Integration) | Provides `vpk pack` for installer creation    |
| GitHub Actions Secrets         | Stores certificates securely                  |
| Windows SDK                    | SignTool.exe for Windows signing              |
| Xcode CLI Tools                | `codesign`, `notarytool`, `stapler` for macOS |

---

## Verification

| Check                                      | Status |
| ------------------------------------------ | ------ |
| `sign-windows.ps1` PowerShell syntax valid | ✅     |
| `sign-macos.sh` bash syntax valid          | ✅     |
| `release.yml` YAML syntax valid            | ✅     |
| `entitlements.plist` XML valid             | ✅     |
| All unit tests pass                        | ✅     |

---

## Technical Notes

### Graceful Degradation

The release workflow uses conditional steps (`if: ${{ secrets.X != '' }}`) so builds can proceed without certificates configured. This allows:

- Development builds without signing
- Gradual certificate setup
- Testing the pipeline before purchasing certificates

### Certificate Renewal

> [!IMPORTANT]
> Code signing certificates typically expire after 1-3 years. Set calendar reminders 60 days before expiration.

**Renewal Process:**

1. Purchase renewal from CA
2. Export as PFX with private key
3. Base64 encode: `cat cert.pfx | base64 > cert_b64.txt`
4. Update GitHub secret
5. Test with a new release

### SmartScreen Reputation

New certificates may still trigger SmartScreen warnings initially. Reputation builds over time as users install signed software.

---

## Related Documents

- [LCS-DES-017b](../specs/v0.1.x/v0.1.7/LCS-DES-017b.md) — Design Specification
- [LCS-SBD-017](../specs/v0.1.x/v0.1.7/LCS-SBD-017.md) — Scope Breakdown
- [LCS-CL-017a](LCS-CL-017a.md) — Velopack Integration (upstream)
