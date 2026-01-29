using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Provides license management operations including validation, activation, and deactivation.
/// </summary>
/// <remarks>
/// LOGIC: Implements ILicenseService with:
/// - Format validation via regex
/// - Checksum validation via modular arithmetic
/// - Simulated server validation based on key prefix
/// - Secure storage via ISecureVault
/// - Event publishing via MediatR
/// 
/// Thread Safety:
/// - Uses lock for license state updates
/// - All public methods are thread-safe
/// 
/// Version: v0.1.6c
/// </remarks>
public sealed partial class LicenseService : ILicenseService
{
    private const string LicenseKeyVaultKey = "license:key";

    /// <summary>
    /// License key format: XXXX-XXXX-XXXX-XXXX where X is uppercase alphanumeric.
    /// </summary>
    [GeneratedRegex(@"^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", RegexOptions.Compiled)]
    private static partial Regex LicenseKeyFormat();

    private readonly ISecureVault _secureVault;
    private readonly IMediator _mediator;
    private readonly ILogger<LicenseService> _logger;
    private readonly object _lock = new();

    private LicenseInfo _currentLicense = LicenseInfo.CoreDefault;

    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseService"/> class.
    /// </summary>
    public LicenseService(
        ISecureVault secureVault,
        IMediator mediator,
        ILogger<LicenseService> logger)
    {
        _secureVault = secureVault ?? throw new ArgumentNullException(nameof(secureVault));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load cached license on startup (fire and forget)
        _ = LoadCachedLicenseAsync();
    }

    /// <inheritdoc/>
    public event EventHandler<LicenseChangedEventArgs>? LicenseChanged;

    #region ILicenseContext Implementation

    /// <inheritdoc/>
    public LicenseTier GetCurrentTier() => _currentLicense.Tier;

    /// <inheritdoc/>
    public bool IsFeatureEnabled(string featureCode)
    {
        var features = GetFeatureAvailability(_currentLicense.Tier);
        return features.FirstOrDefault(f => f.FeatureId == featureCode)?.IsAvailable ?? false;
    }

    /// <inheritdoc/>
    public DateTime? GetExpirationDate() => _currentLicense.ExpirationDate;

    /// <inheritdoc/>
    public string? GetLicenseeName() => _currentLicense.LicenseeName;

    #endregion

    #region ILicenseService Implementation

    /// <inheritdoc/>
    public async Task<LicenseValidationResult> ValidateLicenseKeyAsync(
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating license key");

        // Normalize the key
        var normalizedKey = licenseKey.Trim().ToUpperInvariant();

        // Step 1: Format validation
        if (!LicenseKeyFormat().IsMatch(normalizedKey))
        {
            _logger.LogWarning("Invalid license key format");
            return LicenseValidationResult.Failure(
                LicenseErrorCode.InvalidFormat,
                "License key format is invalid. Expected: XXXX-XXXX-XXXX-XXXX");
        }

        // Step 2: Checksum validation
        if (!ValidateChecksum(normalizedKey))
        {
            _logger.LogWarning("Invalid license key checksum");
            return LicenseValidationResult.Failure(
                LicenseErrorCode.InvalidSignature,
                "License key checksum is invalid.");
        }

        // Step 3: Server-side validation (simulated)
        var result = await ValidateWithServerAsync(normalizedKey, cancellationToken);

        _logger.LogInformation(
            "License validation result: Valid={IsValid}, Tier={Tier}",
            result.IsValid,
            result.Tier);

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateLicenseAsync(
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Activating license");

        // Validate first
        var validationResult = await ValidateLicenseKeyAsync(licenseKey, cancellationToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Cannot activate invalid license: {Error}", validationResult.ErrorMessage);

            await _mediator.Publish(
                new LicenseValidationFailedEvent(validationResult.ErrorCode, validationResult.ErrorMessage!),
                cancellationToken);

            return false;
        }

        // Store license key securely
        var normalizedKey = licenseKey.Trim().ToUpperInvariant();
        await _secureVault.StoreSecretAsync(LicenseKeyVaultKey, normalizedKey, cancellationToken);

        // Update current license
        var oldTier = _currentLicense.Tier;
        var newLicense = new LicenseInfo(
            validationResult.Tier,
            validationResult.LicenseeName,
            validationResult.ExpirationDate,
            normalizedKey,
            IsActivated: true);

        lock (_lock)
        {
            _currentLicense = newLicense;
        }

        _logger.LogInformation(
            "License activated: Tier={Tier}, Licensee={Licensee}",
            validationResult.Tier,
            validationResult.LicenseeName);

        // Raise events
        OnLicenseChanged(oldTier, validationResult.Tier, newLicense);

        await _mediator.Publish(
            new LicenseActivatedEvent(validationResult.Tier, validationResult.LicenseeName!),
            cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateLicenseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating license");

        if (!_currentLicense.IsActivated)
        {
            _logger.LogDebug("No active license to deactivate");
            return false;
        }

        // Remove from secure storage
        await _secureVault.DeleteSecretAsync(LicenseKeyVaultKey, cancellationToken);

        var oldTier = _currentLicense.Tier;
        var newLicense = LicenseInfo.CoreDefault;

        lock (_lock)
        {
            _currentLicense = newLicense;
        }

        _logger.LogInformation("License deactivated, reverted to Core tier");

        // Raise events
        OnLicenseChanged(oldTier, LicenseTier.Core, newLicense);

        await _mediator.Publish(
            new LicenseDeactivatedEvent(oldTier),
            cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public LicenseInfo GetCurrentLicense() => _currentLicense;

    /// <inheritdoc/>
    public IReadOnlyList<FeatureAvailability> GetFeatureAvailability(LicenseTier tier)
    {
        // LOGIC: Feature availability matrix
        // Features are available if their RequiredTier <= current tier
        return new List<FeatureAvailability>
        {
            // Core features (available to all)
            new("basic-editor", "Basic Editor", "Full-featured text editor", true, LicenseTier.Core),
            new("file-operations", "File Operations", "Open, save, export files", true, LicenseTier.Core),
            new("syntax-highlighting", "Syntax Highlighting", "Language-aware code coloring", true, LicenseTier.Core),

            // WriterPro features
            new("ai-suggestions", "AI Suggestions",
                "AI-powered writing suggestions",
                tier >= LicenseTier.WriterPro, LicenseTier.WriterPro),
            new("style-analysis", "Style Analysis",
                "Advanced writing style checks",
                tier >= LicenseTier.WriterPro, LicenseTier.WriterPro),
            new("voice-detection", "Voice Detection",
                "Detect and maintain consistent voice",
                tier >= LicenseTier.WriterPro, LicenseTier.WriterPro),
            new("readability-metrics", "Readability Metrics",
                "Grade level and readability scores",
                tier >= LicenseTier.WriterPro, LicenseTier.WriterPro),

            // Teams features
            new("collaboration", "Real-time Collaboration",
                "Multi-user editing",
                tier >= LicenseTier.Teams, LicenseTier.Teams),
            new("shared-styles", "Shared Style Guides",
                "Team-wide style enforcement",
                tier >= LicenseTier.Teams, LicenseTier.Teams),

            // Enterprise features
            new("sso", "Single Sign-On",
                "SAML/OIDC authentication",
                tier >= LicenseTier.Enterprise, LicenseTier.Enterprise),
            new("audit-logs", "Audit Logs",
                "Compliance and security logging",
                tier >= LicenseTier.Enterprise, LicenseTier.Enterprise),
        };
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads cached license from secure storage on startup.
    /// </summary>
    private async Task LoadCachedLicenseAsync()
    {
        try
        {
            if (!await _secureVault.SecretExistsAsync(LicenseKeyVaultKey))
            {
                _logger.LogDebug("No cached license found");
                return;
            }

            var cachedKey = await _secureVault.GetSecretAsync(LicenseKeyVaultKey);
            var validationResult = await ValidateLicenseKeyAsync(cachedKey);

            if (validationResult.IsValid)
            {
                lock (_lock)
                {
                    _currentLicense = new LicenseInfo(
                        validationResult.Tier,
                        validationResult.LicenseeName,
                        validationResult.ExpirationDate,
                        cachedKey,
                        IsActivated: true);
                }

                _logger.LogInformation("Loaded cached license: Tier={Tier}", validationResult.Tier);
            }
            else
            {
                _logger.LogWarning("Cached license is no longer valid: {Error}", validationResult.ErrorMessage);

                // Remove invalid cached license
                await _secureVault.DeleteSecretAsync(LicenseKeyVaultKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cached license");
        }
    }

    /// <summary>
    /// Validates checksum using modular arithmetic.
    /// </summary>
    /// <remarks>
    /// LOGIC: Simple checksum - sum of all alphanumeric values mod 10 should equal the last digit.
    /// This is a placeholder; production would use cryptographic signatures.
    /// </remarks>
    private static bool ValidateChecksum(string licenseKey)
    {
        // Remove dashes for checksum calculation
        var chars = licenseKey.Replace("-", "");

        if (chars.Length < 2)
            return false;

        // Calculate checksum (sum of char values)
        var sum = 0;
        for (var i = 0; i < chars.Length - 1; i++)
        {
            var c = chars[i];
            sum += char.IsDigit(c) ? c - '0' : c - 'A' + 10;
        }

        // Last character should match checksum mod 36 (0-9, A-Z)
        var expectedChecksum = sum % 36;
        var lastChar = chars[^1];
        var actualChecksum = char.IsDigit(lastChar) ? lastChar - '0' : lastChar - 'A' + 10;

        return expectedChecksum == actualChecksum;
    }

    /// <summary>
    /// Simulates server-side validation.
    /// </summary>
    /// <remarks>
    /// LOGIC: In development, tier is determined by key prefix:
    /// - PRO1/PRO2: WriterPro
    /// - TEAM: Teams
    /// - ENT1/ENT2: Enterprise
    /// - Others: Invalid
    /// 
    /// Production would call an actual license server.
    /// </remarks>
    private Task<LicenseValidationResult> ValidateWithServerAsync(
        string licenseKey,
        CancellationToken cancellationToken)
    {
        // Simulate network delay
        // In production: await _httpClient.PostAsync(...)

        var prefix = licenseKey[..4];

        LicenseTier tier;
        if (prefix.StartsWith("PRO"))
        {
            tier = LicenseTier.WriterPro;
        }
        else if (prefix.StartsWith("TEAM"))
        {
            tier = LicenseTier.Teams;
        }
        else if (prefix.StartsWith("ENT"))
        {
            tier = LicenseTier.Enterprise;
        }
        else
        {
            return Task.FromResult(LicenseValidationResult.Failure(
                LicenseErrorCode.InvalidSignature,
                "License key is not recognized."));
        }

        return Task.FromResult(LicenseValidationResult.Success(
            tier,
            "Development User",
            DateTime.UtcNow.AddYears(1)));
    }

    /// <summary>
    /// Raises the LicenseChanged event.
    /// </summary>
    private void OnLicenseChanged(LicenseTier oldTier, LicenseTier newTier, LicenseInfo licenseInfo)
    {
        LicenseChanged?.Invoke(this, new LicenseChangedEventArgs
        {
            OldTier = oldTier,
            NewTier = newTier,
            LicenseInfo = licenseInfo
        });
    }

    #endregion
}
