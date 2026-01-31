# LCS-DES-004c: The License Gate (Skeleton)

## 1. Metadata & Categorization

| Field                | Value                                    | Description                                           |
| :------------------- | :--------------------------------------- | :---------------------------------------------------- |
| **Feature ID**       | `INF-004c`                               | Infrastructure - License Tier Gating                  |
| **Feature Name**     | The License Gate (Skeleton)              | License tier and feature gating skeleton              |
| **Target Version**   | `v0.0.4c`                                | Third sub-part of v0.0.4                              |
| **Module Scope**     | `Lexichord.Abstractions` / `Lexichord.Host` | Contracts and stub implementation                  |
| **Swimlane**         | `Infrastructure`                         | The Podium (Platform)                                 |
| **License Tier**     | `Core`                                   | Foundation (Required for all tiers)                   |
| **Author**           | System Architect                         |                                                       |
| **Status**           | **Draft**                                | Pending implementation                                |
| **Last Updated**     | 2026-01-26                               |                                                       |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord's business model depends on license-gated features:

| Tier         | Features                                                |
| :----------- | :------------------------------------------------------ |
| **Core**     | Basic editor, fundamental writing tools                 |
| **WriterPro**| Style engine, readability metrics, AI assistance        |
| **Teams**    | Collaboration, shared style guides, team agents         |
| **Enterprise**| SSO, audit logs, compliance features, priority support |

Without license gating:

- All features are available regardless of subscription level.
- No mechanism to enforce the tiered pricing model.
- Premium module DLLs can be loaded by anyone.

### 2.2 The Proposed Solution

We **SHALL** implement a skeleton license system in v0.0.4 that:

1. Defines `LicenseTier` enum for the four tiers.
2. Creates `[RequiresLicense]` attribute for declarative gating.
3. Defines `ILicenseContext` interface for tier/feature checks.
4. Implements `HardcodedLicenseContext` stub returning `Core` tier.

> [!NOTE]
> This is a **skeleton** implementation. Real license validation (encrypted files, online activation, token counting for BYOK) will be implemented in v1.x. The v0.0.4 system is designed to establish the API contracts that v1.x will implement.

---

## 3. Implementation Tasks

### Task 1.1: LicenseTier Enum

**File:** `src/Lexichord.Abstractions/Contracts/LicenseTier.cs`

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the license tiers for Lexichord features.
/// </summary>
/// <remarks>
/// LOGIC: License tiers are hierarchical. Higher tiers include all features of lower tiers.
/// The numeric values enable comparison: if RequiredTier &lt;= CurrentTier, access is granted.
///
/// Tier Hierarchy:
/// - Core (0): Free/Community edition with basic features
/// - WriterPro (1): Individual professional writer subscription
/// - Teams (2): Business/Agency team license
/// - Enterprise (3): Corporate with compliance features
///
/// Design Decisions:
/// - Enum values are explicitly numbered for stable serialization
/// - Values are sequential to allow simple greater-than/less-than checks
/// - Core = 0 ensures default(LicenseTier) is the lowest tier
/// </remarks>
/// <example>
/// <code>
/// // Tier comparison for access check
/// var required = LicenseTier.Teams;
/// var current = LicenseTier.WriterPro;
/// var hasAccess = required &lt;= current; // false, Teams > WriterPro
///
/// // Using with attribute
/// [RequiresLicense(LicenseTier.Teams)]
/// public class CollaborationModule : IModule { }
/// </code>
/// </example>
public enum LicenseTier
{
    /// <summary>
    /// Free/Community edition with basic features.
    /// </summary>
    /// <remarks>
    /// Features: Basic editor, file operations, minimal styling.
    /// All users start at this tier. Modules without [RequiresLicense] default to Core.
    /// </remarks>
    Core = 0,

    /// <summary>
    /// Individual professional writer license.
    /// </summary>
    /// <remarks>
    /// Features: Full style engine, readability metrics, basic AI assistance,
    /// voice analysis, personal style guides.
    /// </remarks>
    WriterPro = 1,

    /// <summary>
    /// Business/Agency team license.
    /// </summary>
    /// <remarks>
    /// Features: All WriterPro features plus collaboration, shared style guides,
    /// team management, release notes agent, multi-user editing.
    /// </remarks>
    Teams = 2,

    /// <summary>
    /// Corporate license with compliance features.
    /// </summary>
    /// <remarks>
    /// Features: All Teams features plus SSO/SAML, audit logs, data residency options,
    /// compliance reporting, priority support, custom deployment.
    /// </remarks>
    Enterprise = 3
}
```

---

### Task 1.2: RequiresLicenseAttribute

**File:** `src/Lexichord.Abstractions/Contracts/RequiresLicenseAttribute.cs`

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Specifies the minimum license tier required to load a module or use a service.
/// </summary>
/// <remarks>
/// LOGIC: This attribute is checked by ModuleLoader during assembly discovery.
/// If the current license tier is lower than the required tier, the module is skipped.
///
/// Check Timing:
/// - Module Loading: Checked BEFORE module instantiation (prevents any code execution)
/// - Runtime Services: Checked by ILicenseService.Guard() at feature boundaries
///
/// Inheritance:
/// - Inherited = false: Each class must explicitly declare its requirement
/// - This prevents accidentally inheriting a lower tier from base class
///
/// AllowMultiple:
/// - AllowMultiple = false: A class has exactly one license requirement
/// - Use FeatureCode for additional granularity within a tier
///
/// Design Decisions:
/// - Class-level only (not method-level) to keep gating coarse-grained
/// - FeatureCode allows feature-level gating within a tier (future use)
/// </remarks>
/// <example>
/// <code>
/// // Module requiring Teams tier
/// [RequiresLicense(LicenseTier.Teams)]
/// public class ReleaseNotesAgentModule : IModule { }
///
/// // Service with feature code for granular gating
/// [RequiresLicense(LicenseTier.WriterPro, FeatureCode = "RAG-01")]
/// public class VectorMemoryService : IMemoryService { }
///
/// // Core tier (explicit, same as no attribute)
/// [RequiresLicense(LicenseTier.Core)]
/// public class BasicEditorModule : IModule { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequiresLicenseAttribute : Attribute
{
    /// <summary>
    /// Gets the minimum required license tier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Comparison uses Tier &lt;= CurrentTier for access check.
    /// If this value is greater than the user's current tier, access is denied.
    /// </remarks>
    public LicenseTier Tier { get; }

    /// <summary>
    /// Gets or sets the feature code for granular license checks.
    /// </summary>
    /// <remarks>
    /// LOGIC: Feature codes allow fine-grained gating within a tier.
    /// A user might have WriterPro tier but not all WriterPro features enabled.
    ///
    /// Use Cases:
    /// - Beta features: Only enabled for beta testers within a tier
    /// - Add-on features: Purchased separately within a tier
    /// - Usage limits: Feature disabled after quota exceeded
    ///
    /// Format: "{Category}-{Number}" (e.g., "RAG-01", "AGT-05", "STY-03")
    /// </remarks>
    /// <example>
    /// <code>
    /// [RequiresLicense(LicenseTier.WriterPro, FeatureCode = "RAG-01")]
    /// public class VectorMemoryService { }
    ///
    /// // Check at runtime:
    /// if (!licenseContext.IsFeatureEnabled("RAG-01"))
    ///     throw new FeatureNotEnabledException("RAG-01");
    /// </code>
    /// </example>
    public string? FeatureCode { get; init; }

    /// <summary>
    /// Creates a new RequiresLicenseAttribute.
    /// </summary>
    /// <param name="tier">The minimum required license tier.</param>
    public RequiresLicenseAttribute(LicenseTier tier)
    {
        Tier = tier;
    }
}
```

---

### Task 1.3: ILicenseContext Interface

**File:** `src/Lexichord.Abstractions/Contracts/ILicenseContext.cs`

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides access to the current license context for tier and feature checks.
/// </summary>
/// <remarks>
/// LOGIC: This interface abstracts the license validation mechanism.
/// The v0.0.4 implementation is a stub returning Core tier.
/// Future v1.x implementations will provide:
/// - Encrypted license file validation
/// - Online license activation
/// - Feature-level gating
/// - Usage metering (for BYOK token counting)
/// - License expiration handling
///
/// Thread Safety:
/// - Implementations must be thread-safe
/// - License state may change during runtime (upgrade, expiration)
///
/// Dependency:
/// - Registered in DI as Singleton (license context is application-wide)
/// - Injected into ModuleLoader and services that need tier checks
/// </remarks>
/// <example>
/// <code>
/// public class MyService(ILicenseContext license)
/// {
///     public void DoSomethingPremium()
///     {
///         if (license.GetCurrentTier() &lt; LicenseTier.WriterPro)
///             throw new LicenseRequiredException(LicenseTier.WriterPro);
///
///         if (!license.IsFeatureEnabled("MY-FEATURE"))
///             throw new FeatureNotEnabledException("MY-FEATURE");
///
///         // Proceed with premium functionality
///     }
/// }
/// </code>
/// </example>
public interface ILicenseContext
{
    /// <summary>
    /// Gets the current license tier.
    /// </summary>
    /// <returns>The active license tier.</returns>
    /// <remarks>
    /// LOGIC: Returns the highest tier the current user is authorized for.
    /// In the stub implementation, always returns Core.
    /// </remarks>
    LicenseTier GetCurrentTier();

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="featureCode">The feature code (e.g., "RAG-01", "AGT-05").</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Feature codes allow granular gating beyond tiers.
    /// A user at WriterPro tier might not have all WriterPro features enabled if:
    /// - Feature is in beta (opt-in)
    /// - Feature is an add-on (separate purchase)
    /// - Feature has usage limits (quota exceeded)
    ///
    /// In the stub implementation, always returns true (all features enabled).
    /// </remarks>
    bool IsFeatureEnabled(string featureCode);

    /// <summary>
    /// Gets the license expiration date, if applicable.
    /// </summary>
    /// <returns>The expiration date, or null for perpetual licenses.</returns>
    /// <remarks>
    /// LOGIC: Subscription licenses have expiration dates.
    /// When expired, tier typically reverts to Core.
    /// UI should warn users before expiration.
    ///
    /// In the stub implementation, returns null (perpetual).
    /// </remarks>
    DateTime? GetExpirationDate();

    /// <summary>
    /// Gets the licensed user or organization name.
    /// </summary>
    /// <returns>The licensee name, or null if not licensed.</returns>
    /// <remarks>
    /// LOGIC: Used for display in UI ("Licensed to: Company Name").
    /// Also useful for support/diagnostics.
    ///
    /// In the stub implementation, returns "Development License".
    /// </remarks>
    string? GetLicenseeName();
}
```

---

### Task 1.4: HardcodedLicenseContext (Stub Implementation)

**File:** `src/Lexichord.Host/Services/HardcodedLicenseContext.cs`

```csharp
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Stub implementation of ILicenseContext that returns Core tier.
/// </summary>
/// <remarks>
/// LOGIC: This is a placeholder implementation for v0.0.4.
/// It provides the API surface without real license validation.
///
/// Purpose:
/// - Allows development and testing without license files
/// - Establishes the API contract for v1.x implementation
/// - Demonstrates the license check flow in ModuleLoader
///
/// Future v1.x Implementation Will:
/// - Read encrypted license files
/// - Validate signatures
/// - Check expiration dates
/// - Contact licensing server for activation
/// - Support BYOK (Bring Your Own Key) with token counting
///
/// Security Note:
/// - This stub provides NO actual security
/// - Users can trivially bypass by modifying code or removing attributes
/// - Real security requires server-side validation and obfuscation
/// </remarks>
public sealed class HardcodedLicenseContext : ILicenseContext
{
    private readonly ILogger<HardcodedLicenseContext>? _logger;

    /// <summary>
    /// Creates a new HardcodedLicenseContext.
    /// </summary>
    /// <param name="logger">Optional logger for license check events.</param>
    public HardcodedLicenseContext(ILogger<HardcodedLicenseContext>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation(
            "Using hardcoded license context (development mode). Tier: {Tier}",
            LicenseTier.Core);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub always returns Core tier.
    /// To test higher tiers during development, modify this return value.
    /// </remarks>
    public LicenseTier GetCurrentTier()
    {
        _logger?.LogDebug("License tier check: returning {Tier}", LicenseTier.Core);
        return LicenseTier.Core;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub returns true for all features.
    /// This allows testing any feature without feature gating.
    /// </remarks>
    public bool IsFeatureEnabled(string featureCode)
    {
        _logger?.LogDebug(
            "Feature check for {FeatureCode}: returning enabled (stub)",
            featureCode);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub returns null (perpetual license).
    /// No expiration warnings in development mode.
    /// </remarks>
    public DateTime? GetExpirationDate() => null;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Returns development indicator for UI display.
    /// </remarks>
    public string? GetLicenseeName() => "Development License";
}
```

---

### Task 1.5: DI Registration

**Updates to `HostServices.cs`:**

```csharp
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing registrations ...

    // LOGIC: Register license context as singleton
    // License state is application-wide and should not change per-request
    services.AddSingleton<ILicenseContext, HardcodedLicenseContext>();

    return services;
}
```

---

## 4. Decision Tree: License Check Flow

```text
START: "Should this feature be accessible?"
│
├── Is this a Module Load check?
│   │
│   ├── Does the module have [RequiresLicense]?
│   │   ├── NO --> Treat as Core tier (always accessible)
│   │   └── YES --> Continue
│   │
│   ├── Is RequiredTier <= GetCurrentTier()?
│   │   ├── NO --> DENY: Skip module, log "license restrictions"
│   │   └── YES --> Continue
│   │
│   ├── Does attribute have FeatureCode?
│   │   ├── NO --> ALLOW: Load the module
│   │   └── YES --> Check IsFeatureEnabled(FeatureCode)
│   │       ├── NO --> DENY: Skip module, log "feature restriction"
│   │       └── YES --> ALLOW: Load the module
│   │
│   └── END
│
├── Is this a Runtime check?
│   │
│   ├── Get current tier: licenseContext.GetCurrentTier()
│   │
│   ├── Is RequiredTier <= CurrentTier?
│   │   ├── NO --> Throw LicenseRequiredException
│   │   └── YES --> Continue
│   │
│   ├── Is there a FeatureCode requirement?
│   │   ├── NO --> ALLOW: Proceed with operation
│   │   └── YES --> Check licenseContext.IsFeatureEnabled(code)
│   │       ├── NO --> Throw FeatureNotEnabledException
│   │       └── YES --> ALLOW: Proceed with operation
│   │
│   └── END
│
└── END
```

---

## 5. Unit Testing Requirements

### 5.1 Test: LicenseTier Enum Values

```csharp
[TestFixture]
[Category("Unit")]
public class LicenseTierTests
{
    [Test]
    public void LicenseTier_HierarchyIsCorrect()
    {
        // Assert - Higher tiers have higher numeric values
        Assert.Multiple(() =>
        {
            Assert.That(LicenseTier.Core, Is.LessThan(LicenseTier.WriterPro));
            Assert.That(LicenseTier.WriterPro, Is.LessThan(LicenseTier.Teams));
            Assert.That(LicenseTier.Teams, Is.LessThan(LicenseTier.Enterprise));
        });
    }

    [Test]
    public void LicenseTier_CoreIsDefault()
    {
        // Arrange & Act
        var defaultTier = default(LicenseTier);

        // Assert
        Assert.That(defaultTier, Is.EqualTo(LicenseTier.Core));
    }

    [Test]
    public void LicenseTier_ExplicitValues()
    {
        // Assert - Values are explicitly defined for stable serialization
        Assert.Multiple(() =>
        {
            Assert.That((int)LicenseTier.Core, Is.EqualTo(0));
            Assert.That((int)LicenseTier.WriterPro, Is.EqualTo(1));
            Assert.That((int)LicenseTier.Teams, Is.EqualTo(2));
            Assert.That((int)LicenseTier.Enterprise, Is.EqualTo(3));
        });
    }

    [TestCase(LicenseTier.Core, LicenseTier.Core, true)]
    [TestCase(LicenseTier.Core, LicenseTier.WriterPro, true)]
    [TestCase(LicenseTier.WriterPro, LicenseTier.Core, false)]
    [TestCase(LicenseTier.Teams, LicenseTier.Enterprise, true)]
    [TestCase(LicenseTier.Enterprise, LicenseTier.Teams, false)]
    public void LicenseTier_ComparisonWorks(
        LicenseTier required,
        LicenseTier current,
        bool expectedAccess)
    {
        // Act
        var hasAccess = required <= current;

        // Assert
        Assert.That(hasAccess, Is.EqualTo(expectedAccess));
    }
}
```

### 5.2 Test: RequiresLicenseAttribute

```csharp
[TestFixture]
[Category("Unit")]
public class RequiresLicenseAttributeTests
{
    [Test]
    public void RequiresLicense_TierIsSet()
    {
        // Arrange & Act
        var attr = new RequiresLicenseAttribute(LicenseTier.Teams);

        // Assert
        Assert.That(attr.Tier, Is.EqualTo(LicenseTier.Teams));
    }

    [Test]
    public void RequiresLicense_FeatureCodeIsNull_ByDefault()
    {
        // Arrange & Act
        var attr = new RequiresLicenseAttribute(LicenseTier.WriterPro);

        // Assert
        Assert.That(attr.FeatureCode, Is.Null);
    }

    [Test]
    public void RequiresLicense_FeatureCodeCanBeSet()
    {
        // Arrange & Act
        var attr = new RequiresLicenseAttribute(LicenseTier.WriterPro)
        {
            FeatureCode = "RAG-01"
        };

        // Assert
        Assert.That(attr.FeatureCode, Is.EqualTo("RAG-01"));
    }

    [Test]
    public void RequiresLicense_AttributeUsageIsCorrect()
    {
        // Arrange
        var attributeUsage = typeof(RequiresLicenseAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.That(attributeUsage, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(attributeUsage!.ValidOn, Is.EqualTo(AttributeTargets.Class));
            Assert.That(attributeUsage.AllowMultiple, Is.False);
            Assert.That(attributeUsage.Inherited, Is.False);
        });
    }

    [Test]
    public void RequiresLicense_CanBeAppliedToClass()
    {
        // Arrange
        [RequiresLicense(LicenseTier.Enterprise)]
        class TestClass { }

        // Act
        var attr = typeof(TestClass)
            .GetCustomAttributes(typeof(RequiresLicenseAttribute), false)
            .Cast<RequiresLicenseAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Tier, Is.EqualTo(LicenseTier.Enterprise));
    }
}
```

### 5.3 Test: HardcodedLicenseContext

```csharp
[TestFixture]
[Category("Unit")]
public class HardcodedLicenseContextTests
{
    [Test]
    public void GetCurrentTier_ReturnsCore()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act
        var result = sut.GetCurrentTier();

        // Assert
        Assert.That(result, Is.EqualTo(LicenseTier.Core));
    }

    [Test]
    public void IsFeatureEnabled_AlwaysReturnsTrue()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(sut.IsFeatureEnabled("RAG-01"), Is.True);
            Assert.That(sut.IsFeatureEnabled("AGT-05"), Is.True);
            Assert.That(sut.IsFeatureEnabled("NONEXISTENT"), Is.True);
            Assert.That(sut.IsFeatureEnabled(""), Is.True);
        });
    }

    [Test]
    public void GetExpirationDate_ReturnsNull()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act
        var result = sut.GetExpirationDate();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetLicenseeName_ReturnsDevelopmentLicense()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act
        var result = sut.GetLicenseeName();

        // Assert
        Assert.That(result, Is.EqualTo("Development License"));
    }

    [Test]
    public void Constructor_WithLogger_LogsInitialization()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<HardcodedLicenseContext>>();

        // Act
        var sut = new HardcodedLicenseContext(mockLogger.Object);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("hardcoded license context")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void Constructor_WithoutLogger_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new HardcodedLicenseContext());
    }
}
```

### 5.4 Test: ILicenseContext Interface

```csharp
[TestFixture]
[Category("Unit")]
public class ILicenseContextTests
{
    [Test]
    public void ILicenseContext_DefinesRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(ILicenseContext);

        // Act
        var methods = interfaceType.GetMethods();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(methods.Any(m => m.Name == "GetCurrentTier" &&
                m.ReturnType == typeof(LicenseTier) &&
                m.GetParameters().Length == 0),
                Is.True, "ILicenseContext must define GetCurrentTier()");

            Assert.That(methods.Any(m => m.Name == "IsFeatureEnabled" &&
                m.ReturnType == typeof(bool) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string)),
                Is.True, "ILicenseContext must define IsFeatureEnabled(string)");

            Assert.That(methods.Any(m => m.Name == "GetExpirationDate" &&
                m.ReturnType == typeof(DateTime?)),
                Is.True, "ILicenseContext must define GetExpirationDate()");

            Assert.That(methods.Any(m => m.Name == "GetLicenseeName" &&
                m.ReturnType == typeof(string)),
                Is.True, "ILicenseContext must define GetLicenseeName()");
        });
    }

    [Test]
    public void HardcodedLicenseContext_ImplementsILicenseContext()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Assert
        Assert.That(sut, Is.InstanceOf<ILicenseContext>());
    }
}
```

---

## 6. Observability & Logging

| Level       | Context                  | Message Template                                                          |
| :---------- | :----------------------- | :------------------------------------------------------------------------ |
| Information | HardcodedLicenseContext  | `Using hardcoded license context (development mode). Tier: {Tier}`        |
| Debug       | HardcodedLicenseContext  | `License tier check: returning {Tier}`                                    |
| Debug       | HardcodedLicenseContext  | `Feature check for {FeatureCode}: returning enabled (stub)`               |
| Warning     | ModuleLoader             | `Skipping module {ModuleName} due to license restrictions...`             |
| Warning     | ModuleLoader             | `Skipping module {ModuleName} due to feature restriction...`              |

---

## 7. Security Considerations

> [!WARNING]
> **v0.0.4 License Gating is NOT Secure**
>
> The skeleton implementation provides **no real security**. Users can bypass license checks by:
> - Modifying the `HardcodedLicenseContext` source code
> - Removing `[RequiresLicense]` attributes from modules
> - Replacing `ILicenseContext` registration in DI
> - Using reflection to access restricted services
>
> This is **acceptable for v0.0.4** because:
> - The goal is establishing API contracts, not security
> - Real security requires server-side validation
> - Premium modules will only be distributed to licensed users
> - The actual enforcement will be in module distribution, not runtime checks

### v1.x Security Requirements (Future)

The production license system will require:

1. **Encrypted License Files**: RSA-signed JSON with hardware binding
2. **Online Activation**: Server validation of license keys
3. **Obfuscation**: Code obfuscation to prevent trivial bypass
4. **Module Distribution**: Premium modules only available via authenticated download
5. **Telemetry**: Usage tracking for BYOK token counting

---

## 8. Definition of Done

- [ ] `LicenseTier` enum exists in `Lexichord.Abstractions.Contracts`
- [ ] `LicenseTier` has values: Core=0, WriterPro=1, Teams=2, Enterprise=3
- [ ] `default(LicenseTier)` equals `LicenseTier.Core`
- [ ] `RequiresLicenseAttribute` exists with `Tier` property
- [ ] `RequiresLicenseAttribute` has optional `FeatureCode` property
- [ ] `RequiresLicenseAttribute` targets Class only (not inherited)
- [ ] `ILicenseContext` interface exists with all methods
- [ ] `ILicenseContext.GetCurrentTier()` returns `LicenseTier`
- [ ] `ILicenseContext.IsFeatureEnabled(string)` returns `bool`
- [ ] `ILicenseContext.GetExpirationDate()` returns `DateTime?`
- [ ] `ILicenseContext.GetLicenseeName()` returns `string?`
- [ ] `HardcodedLicenseContext` implements `ILicenseContext`
- [ ] `HardcodedLicenseContext.GetCurrentTier()` returns `Core`
- [ ] `HardcodedLicenseContext.IsFeatureEnabled()` returns `true`
- [ ] `HardcodedLicenseContext.GetExpirationDate()` returns `null`
- [ ] `HardcodedLicenseContext.GetLicenseeName()` returns "Development License"
- [ ] `ILicenseContext` registered in DI as Singleton
- [ ] All unit tests passing
- [ ] XML documentation complete for all public members

---

## 9. Verification Commands

```bash
# Build to verify compilation
dotnet build src/Lexichord.Abstractions
dotnet build src/Lexichord.Host

# Verify LicenseTier enum exists
grep -r "public enum LicenseTier" src/Lexichord.Abstractions/

# Verify RequiresLicenseAttribute exists
grep -r "public sealed class RequiresLicenseAttribute" src/Lexichord.Abstractions/

# Verify ILicenseContext interface exists
grep -r "public interface ILicenseContext" src/Lexichord.Abstractions/

# Verify HardcodedLicenseContext exists
grep -r "public sealed class HardcodedLicenseContext" src/Lexichord.Host/

# Run unit tests
dotnet test --filter "FullyQualifiedName~LicenseTierTests"
dotnet test --filter "FullyQualifiedName~RequiresLicenseAttributeTests"
dotnet test --filter "FullyQualifiedName~HardcodedLicenseContextTests"
dotnet test --filter "FullyQualifiedName~ILicenseContextTests"

# Run application and check logs
dotnet run --project src/Lexichord.Host
# Should see: "Using hardcoded license context (development mode). Tier: Core"
```
