# LexiChord Network & API Security Specification
## Scope Breakdown Document v0.18.4-SEC

**Document ID:** LCS-SBD-v0.18.4-SEC
**Version:** 1.0.0
**Release Date:** February 1, 2026
**Status:** Draft
**Classification:** Internal Technical Specification

---

## 1. DOCUMENT CONTROL

### 1.1 Document Information
| Field | Value |
|-------|-------|
| **Document ID** | LCS-SBD-v0.18.4-SEC |
| **Document Type** | Scope Breakdown Document |
| **Product Line** | LexiChord |
| **Release Version** | v0.18.4 |
| **Module Focus** | Network & API Security |
| **Total Scope Hours** | 60 hours |
| **Creation Date** | 2026-02-01 |
| **Last Modified** | 2026-02-01 |
| **Author(s)** | Security Architecture Team |
| **Stakeholders** | Engineering, Security, DevOps, QA |

### 1.2 Revision History
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2026-02-01 | SAT | Initial specification document |

### 1.3 Document Approval
| Role | Name | Signature | Date |
|------|------|-----------|------|
| Technical Lead | [TBD] | _____ | _____ |
| Security Officer | [TBD] | _____ | _____ |
| Product Manager | [TBD] | _____ | _____ |

---

## 2. EXECUTIVE SUMMARY

### 2.1 Overview

The v0.18.4-SEC release introduces comprehensive Network & API Security controls to the LexiChord platform. This release implements critical protections against unauthorized data exfiltration, API key compromise, and malicious network communications. The security framework encompasses six integrated sub-modules that work in concert to create a defense-in-depth architecture for all network-based interactions.

### 2.2 Strategic Objectives

1. **Prevent Credential Compromise**: Implement secure storage and retrieval of API keys, database credentials, and authentication tokens
2. **Control Outbound Communications**: Establish strict controls over what systems and endpoints LexiChord can communicate with
3. **Detect Exfiltration Attempts**: Identify and block unauthorized data egress patterns in real-time
4. **Enforce TLS/Certificate Security**: Validate all SSL/TLS certificates and prevent man-in-the-middle attacks
5. **Audit Network Activity**: Comprehensive logging and inspection of all network requests for compliance and investigation
6. **Reduce Attack Surface**: Minimize exposed network capabilities through allowlisting and blocklisting policies

### 2.3 Security Principles

- **Zero Trust**: Every outbound request is treated as potentially malicious until validated
- **Defense in Depth**: Multiple security layers prevent single-point failures
- **Principle of Least Privilege**: Services have minimal necessary network permissions
- **Crypto-Agility**: Support for multiple encryption standards and certificate types
- **Audit Transparency**: All security decisions logged for forensic analysis
- **Performance Mindfulness**: Security controls must not significantly impact application performance

### 2.4 Risk Context

Current operational risks without this release:
- **API Key Exposure**: Credentials can be accidentally logged or transmitted insecurely
- **Malware C&C Channels**: Compromised components could establish outbound backdoors
- **Data Exfiltration**: Sensitive data (PII, business data) could leak to external systems
- **Man-in-the-Middle Attacks**: Unvalidated TLS connections vulnerable to interception
- **Compliance Violations**: Lack of audit trails for network activity violates SOC2/GDPR requirements

---

## 3. SUB-PARTS BREAKDOWN

### 3.1 v0.18.4a: Outbound Request Controls
**Duration:** 12 hours
**Priority:** P0 (Critical)
**Dependencies:** None (foundational)

#### Description
Core infrastructure for controlling and managing all outbound network requests from the LexiChord system. This sub-part establishes the request pipeline, validation framework, and decision-making logic for determining whether outbound communications are permitted.

#### Acceptance Criteria
- [ ] IOutboundRequestController interface fully implemented
- [ ] Request validation occurs before any TCP/HTTP connection establishment
- [ ] All HTTP clients in codebase use the controller abstraction
- [ ] Request metadata includes timestamp, source service, destination, method, headers
- [ ] Request evaluation completes in < 10ms average latency
- [ ] Integration tests cover allow/deny scenarios
- [ ] Performance benchmarks demonstrate zero regression vs current baseline
- [ ] Documentation includes implementation guide for service developers

#### Key Deliverables
- Request controller interface and core implementation
- HTTP client factory with built-in request wrapping
- Request metadata model and serialization
- Integration with dependency injection container
- Unit tests (>85% code coverage)
- Performance benchmarks
- Developer integration guide

#### Success Metrics
- Request controller latency: < 10ms (p99)
- Request validation throughput: > 10,000 req/sec
- Code coverage: >= 85%
- Zero false positives in allow/deny decisions
- Zero integration conflicts with existing services

---

### 3.2 v0.18.4b: API Key Vault & Protection
**Duration:** 12 hours
**Priority:** P0 (Critical)
**Dependencies:** v0.18.4a (Outbound Request Controls)

#### Description
Secure storage, retrieval, and lifecycle management of API keys, secrets, and sensitive credentials. Implements encryption at rest, secure key derivation, and access control policies. Keys are never stored in plain text and all retrieval is audited.

#### Acceptance Criteria
- [ ] IApiKeyVault interface fully implemented with encryption
- [ ] Keys encrypted with AES-256-GCM at rest in database
- [ ] Key derivation uses PBKDF2 with 100,000+ iterations
- [ ] Vault supports key rotation without application restart
- [ ] Key retrieval includes mandatory audit logging
- [ ] API key masking in logs (only last 4 characters visible)
- [ ] Integration with HSM (Hardware Security Module) possible
- [ ] Support for multiple key sources (database, environment, vault service)
- [ ] Key retrieval performance: < 5ms average latency
- [ ] Zero keys logged in any form

#### Key Deliverables
- API key vault interface and encrypted implementation
- Key encryption/decryption service with AES-256-GCM
- Key rotation service
- Audit logging decorator
- Key masking utilities
- HSM integration abstraction
- Integration tests with TestContainers PostgreSQL
- Security audit checklist

#### Success Metrics
- Key retrieval latency: < 5ms (p99)
- Encryption overhead: < 2ms per key
- Audit logging 100% coverage
- Zero unencrypted keys in database
- Zero keys in application logs
- HSM integration available for enterprise customers

---

### 3.3 v0.18.4c: Data Exfiltration Prevention
**Duration:** 10 hours
**Priority:** P0 (Critical)
**Dependencies:** v0.18.4a (Outbound Request Controls)

#### Description
Real-time detection and prevention of data exfiltration patterns through network requests. Scans request payloads for sensitive data patterns (API keys, passwords, PII, credit cards) and blocks matching requests. Uses regex patterns, checksums, and heuristic analysis.

#### Acceptance Criteria
- [ ] IDataExfiltrationGuard interface fully implemented
- [ ] Detection of API key patterns (AWS, Azure, GitHub, etc.)
- [ ] Detection of password patterns (common password formats)
- [ ] Detection of PII patterns (SSN, phone, email, credit card)
- [ ] Detection of database connection strings
- [ ] Pattern matching completes in < 5ms for 100KB payload
- [ ] False positive rate < 0.1%
- [ ] Blocking decision logs include matched pattern details
- [ ] Integration with request inspector for deeper analysis
- [ ] Support for custom pattern registration
- [ ] Allows whitelisting of specific patterns per service
- [ ] Integration tests with real-world data examples

#### Key Deliverables
- Data exfiltration guard interface and implementation
- Pattern library (API keys, passwords, PII, credentials)
- Pattern matching engine with performance optimizations
- Custom pattern registration service
- Whitelist/blacklist service per data pattern
- Integration tests with sensitive data examples
- Performance benchmarks
- Pattern documentation with examples

#### Success Metrics
- Pattern matching latency: < 5ms for 100KB
- Detection accuracy: > 99% with < 0.1% false positives
- Blocked exfiltration attempts: 100% of test cases
- Custom pattern support fully functional
- Integration tests: 100% pass rate

---

### 3.4 v0.18.4d: Host Allowlist/Blocklist
**Duration:** 8 hours
**Priority:** P1 (High)
**Dependencies:** v0.18.4a (Outbound Request Controls)

#### Description
Maintains policies for which hosts/domains/IP addresses the application can connect to. Supports both allowlist mode (only specified hosts permitted) and blocklist mode (block specific known-bad hosts). Policies are evaluated before connection attempts.

#### Acceptance Criteria
- [ ] IHostPolicyManager interface fully implemented
- [ ] Support for domain names, wildcards, and CIDR notation
- [ ] Policy evaluation before DNS resolution
- [ ] Support for dynamic policy updates without restart
- [ ] Audit logging of all policy blocks
- [ ] Performance: < 1ms average for policy evaluation
- [ ] Support for time-based policies (valid time windows)
- [ ] Support for service-specific policies
- [ ] Integration with request metadata
- [ ] Database schema supports million+ rules
- [ ] UI for managing allowlist/blocklist
- [ ] API for programmatic policy management

#### Key Deliverables
- Host policy manager interface and implementation
- Policy evaluation engine with caching
- Dynamic policy update service
- Policy parser supporting multiple formats
- Time-based policy support
- Service-specific policy context
- Database schema for host rules
- UI for allowlist/blocklist management
- REST API for policy CRUD operations
- Performance benchmarks
- Integration tests

#### Success Metrics
- Policy evaluation latency: < 1ms (p99)
- Dynamic update propagation: < 100ms
- Support for million+ rules without performance degradation
- UI responsiveness: < 500ms for operations
- 100% audit coverage of policy blocks

---

### 3.5 v0.18.4e: Request Inspection & Logging
**Duration:** 10 hours
**Priority:** P1 (High)
**Dependencies:** v0.18.4a, v0.18.4b, v0.18.4c, v0.18.4d

#### Description
Comprehensive logging and inspection of all outbound network requests. Captures request/response metadata, timing information, error details, and audit trail. Supports multiple output destinations (database, file, syslog) and real-time alerting on suspicious patterns.

#### Acceptance Criteria
- [ ] IRequestInspector interface fully implemented
- [ ] Request inspection captures: method, URL, headers (sanitized), size, timing
- [ ] Response inspection captures: status code, size, timing, error details
- [ ] Sanitization removes sensitive headers (Authorization, API-Key, etc.)
- [ ] Logging to database, file, and syslog simultaneously
- [ ] Database queries optimized for large-scale logging (100k+ req/day)
- [ ] Log retention policies configurable
- [ ] Real-time alerting on suspicious patterns
- [ ] Integration with security monitoring tools
- [ ] Query API for forensic analysis
- [ ] Performance impact < 5% latency overhead
- [ ] Compliance audit trail includes decision rationale

#### Key Deliverables
- Request inspector interface and core implementation
- Log sink implementations (database, file, syslog)
- Request/response metadata models
- Sanitization service for sensitive data
- Alerting service for suspicious patterns
- Log query API and UI
- Database schema for request logs
- Log retention policy service
- Integration with monitoring systems
- Performance benchmarks

#### Success Metrics
- Inspection overhead: < 5% request latency
- Log query performance: < 500ms for daily summaries
- Database writes: batch insert for 100+ records/sec
- Sanitization: 100% sensitive data removal
- Alerting latency: < 10 seconds for pattern detection

---

### 3.6 v0.18.4f: Certificate & TLS Validation
**Duration:** 8 hours
**Priority:** P1 (High)
**Dependencies:** v0.18.4a (Outbound Request Controls)

#### Description
Validates SSL/TLS certificates for all outbound connections. Implements certificate pinning for critical services, certificate chain validation, expiration checking, and revocation checking. Prevents man-in-the-middle attacks through strict validation.

#### Acceptance Criteria
- [ ] ICertificateValidator interface fully implemented
- [ ] X.509 certificate chain validation
- [ ] Certificate expiration checking with warning thresholds
- [ ] CRL (Certificate Revocation List) checking
- [ ] OCSP (Online Certificate Status Protocol) support
- [ ] Certificate pinning for critical service endpoints
- [ ] Support for custom certificate stores
- [ ] Invalid certificates block all connections
- [ ] Validation performance: < 50ms per certificate
- [ ] Support for self-signed certificates in non-production
- [ ] Audit logging of all validation decisions
- [ ] Integration tests with test certificates

#### Key Deliverables
- Certificate validator interface and implementation
- X.509 certificate chain validator
- Certificate pinning service
- CRL/OCSP checking client
- Certificate expiration monitor
- Custom certificate store support
- Database schema for pinned certificates
- Validation decision logging
- Integration tests with test certificates
- Performance benchmarks
- Developer documentation

#### Success Metrics
- Validation latency: < 50ms per certificate (p99)
- False negatives: 0 (all invalid certificates blocked)
- False positives: < 0.1% (legitimate certs not blocked)
- Certificate pinning 100% effective for protected services
- CRL/OCSP checks: 100% coverage for critical certs
- Integration test coverage: >= 90%

---

## 4. C# INTERFACES & CORE CONTRACTS

### 4.1 IOutboundRequestController

```csharp
/// <summary>
/// Controls and validates outbound network requests from LexiChord.
/// Implements the central decision point for all egress traffic.
/// </summary>
public interface IOutboundRequestController
{
    /// <summary>
    /// Evaluates whether an outbound request is permitted.
    /// </summary>
    /// <param name="request">The outbound request to evaluate</param>
    /// <param name="context">The security context for this request</param>
    /// <returns>Result containing allow/deny decision and rationale</returns>
    Task<OutboundRequestEvaluationResult> EvaluateRequestAsync(
        OutboundRequest request,
        RequestSecurityContext context);

    /// <summary>
    /// Executes an outbound request with all security controls applied.
    /// </summary>
    /// <param name="request">The request to execute</param>
    /// <param name="context">The security context</param>
    /// <returns>The response with security metadata</returns>
    Task<SecureHttpResponse> ExecuteSecureRequestAsync(
        OutboundRequest request,
        RequestSecurityContext context);

    /// <summary>
    /// Registers a temporary override for outbound requests to a specific host.
    /// </summary>
    /// <param name="hostname">The hostname to allow</param>
    /// <param name="durationMinutes">How long the override is valid</param>
    /// <param name="reason">Audit trail reason for override</param>
    Task RegisterTemporaryOverrideAsync(
        string hostname,
        int durationMinutes,
        string reason);

    /// <summary>
    /// Gets statistics on request evaluation and execution.
    /// </summary>
    Task<OutboundRequestStatistics> GetStatisticsAsync();
}

/// <summary>Represents an outbound request to evaluate</summary>
public class OutboundRequest
{
    public string Method { get; set; }
    public Uri Destination { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public byte[] Body { get; set; }
    public string SourceService { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public TimeSpan Timeout { get; set; }
}

/// <summary>Result of request evaluation</summary>
public class OutboundRequestEvaluationResult
{
    public bool IsAllowed { get; set; }
    public string DecisionRationale { get; set; }
    public List<string> ViolatedPolicies { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public long EvaluationTimeMs { get; set; }
}

/// <summary>Security context for request evaluation</summary>
public class RequestSecurityContext
{
    public string ServiceIdentity { get; set; }
    public string[] AllowedHosts { get; set; }
    public bool IsProduction { get; set; }
    public Dictionary<string, object> AdditionalContext { get; set; }
}

/// <summary>Response with security metadata attached</summary>
public class SecureHttpResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public byte[] Body { get; set; }
    public long ResponseTimeMs { get; set; }
    public SecurityEvaluationSummary SecuritySummary { get; set; }
}

/// <summary>Statistics on request evaluation and execution</summary>
public class OutboundRequestStatistics
{
    public long TotalRequestsEvaluated { get; set; }
    public long RequestsAllowed { get; set; }
    public long RequestsBlocked { get; set; }
    public double AverageEvaluationTimeMs { get; set; }
    public Dictionary<string, long> BlockReasonCounts { get; set; }
}
```

### 4.2 IApiKeyVault

```csharp
/// <summary>
/// Manages secure storage, retrieval, and lifecycle of API keys and secrets.
/// All keys are encrypted at rest and access is audited.
/// </summary>
public interface IApiKeyVault
{
    /// <summary>
    /// Stores an API key in the vault with encryption.
    /// </summary>
    /// <param name="key">The key identifier</param>
    /// <param name="value">The secret value to store</param>
    /// <param name="metadata">Optional metadata (key type, expiration, etc.)</param>
    Task<ApiKeyStorageResult> StoreKeyAsync(
        string key,
        string value,
        ApiKeyMetadata metadata = null);

    /// <summary>
    /// Retrieves an API key from the vault with decryption.
    /// </summary>
    /// <param name="key">The key identifier</param>
    /// <returns>The decrypted secret value</returns>
    Task<string> RetrieveKeyAsync(string key);

    /// <summary>
    /// Checks if a key exists in the vault without retrieving it.
    /// </summary>
    Task<bool> KeyExistsAsync(string key);

    /// <summary>
    /// Removes a key from the vault permanently.
    /// </summary>
    Task DeleteKeyAsync(string key, string deletionReason);

    /// <summary>
    /// Rotates an API key to a new value.
    /// </summary>
    /// <param name="key">The key to rotate</param>
    /// <param name="newValue">The new secret value</param>
    /// <param name="rotationReason">Audit trail reason for rotation</param>
    Task<ApiKeyRotationResult> RotateKeyAsync(
        string key,
        string newValue,
        string rotationReason);

    /// <summary>
    /// Lists all stored keys (returns only identifiers, not values).
    /// </summary>
    Task<List<ApiKeyIdentifier>> ListKeysAsync(
        int pageSize = 100,
        int pageNumber = 1);

    /// <summary>
    /// Exports a key for secure transmission to another service.
    /// </summary>
    /// <param name="key">The key to export</param>
    /// <param name="exportReason">Audit trail reason</param>
    Task<string> ExportKeyAsync(string key, string exportReason);

    /// <summary>
    /// Gets metadata about a stored key without retrieving the secret.
    /// </summary>
    Task<ApiKeyMetadata> GetKeyMetadataAsync(string key);

    /// <summary>
    /// Validates that a key matches expected attributes.
    /// </summary>
    Task<bool> ValidateKeyAsync(string key, string expectedValue);
}

/// <summary>Metadata about an API key</summary>
public class ApiKeyMetadata
{
    public string KeyType { get; set; } // "AWS", "Azure", "GitHub", etc.
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
    public string Owner { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Tags { get; set; }
}

/// <summary>Result of storing a key</summary>
public class ApiKeyStorageResult
{
    public bool Success { get; set; }
    public string KeyIdentifier { get; set; }
    public DateTime StoredAt { get; set; }
    public string EncryptionAlgorithm { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>Result of key rotation</summary>
public class ApiKeyRotationResult
{
    public bool Success { get; set; }
    public DateTime RotatedAt { get; set; }
    public int KeyVersionNumber { get; set; }
    public DateTime PreviousKeyExpiration { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>Identifier for a stored key (no secret value)</summary>
public class ApiKeyIdentifier
{
    public string Key { get; set; }
    public string KeyType { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
```

### 4.3 IDataExfiltrationGuard

```csharp
/// <summary>
/// Detects and prevents data exfiltration through network requests.
/// Scans request payloads for sensitive data patterns.
/// </summary>
public interface IDataExfiltrationGuard
{
    /// <summary>
    /// Analyzes a request for potential data exfiltration.
    /// </summary>
    /// <param name="request">The request to analyze</param>
    /// <returns>Analysis result with detected sensitive data</returns>
    Task<ExfiltrationAnalysisResult> AnalyzeRequestAsync(
        OutboundRequest request);

    /// <summary>
    /// Determines if a request should be blocked due to exfiltration risk.
    /// </summary>
    /// <param name="analysisResult">The analysis result from AnalyzeRequestAsync</param>
    /// <returns>True if request should be blocked</returns>
    Task<bool> ShouldBlockRequestAsync(
        ExfiltrationAnalysisResult analysisResult);

    /// <summary>
    /// Registers a custom exfiltration pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to detect</param>
    /// <param name="patternType">Type of sensitive data</param>
    /// <param name="severity">Severity level if detected</param>
    Task RegisterPatternAsync(
        string pattern,
        string patternType,
        SensitivityLevel severity);

    /// <summary>
    /// Whitelist a specific pattern match for a service.
    /// </summary>
    /// <param name="pattern">The pattern to whitelist</param>
    /// <param name="service">The service allowed to use this pattern</param>
    /// <param name="reason">Audit trail reason</param>
    Task RegisterWhitelistAsync(
        string pattern,
        string service,
        string reason);

    /// <summary>
    /// Gets all detected exfiltration attempts within a time range.
    /// </summary>
    Task<List<ExfiltrationAttempt>> GetAttemptHistoryAsync(
        DateTime startTime,
        DateTime endTime);

    /// <summary>
    /// Gets statistics on exfiltration detection.
    /// </summary>
    Task<ExfiltrationGuardStatistics> GetStatisticsAsync();
}

/// <summary>Result of exfiltration analysis</summary>
public class ExfiltrationAnalysisResult
{
    public bool ContainsSensitiveData { get; set; }
    public List<DetectedSensitiveData> DetectedPatterns { get; set; }
    public SensitivityLevel MaxSeverityLevel { get; set; }
    public long AnalysisTimeMs { get; set; }
    public Dictionary<string, int> PatternMatchCounts { get; set; }
}

/// <summary>Detected sensitive data in a request</summary>
public class DetectedSensitiveData
{
    public string PatternType { get; set; } // "API_KEY", "PASSWORD", "SSN", etc.
    public string Snippet { get; set; } // First/last few chars for identification
    public int MatchCount { get; set; }
    public SensitivityLevel Severity { get; set; }
    public string[] MatchLocations { get; set; } // "BODY", "HEADERS", etc.
}

/// <summary>Sensitivity level enumeration</summary>
public enum SensitivityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>Record of an exfiltration attempt</summary>
public class ExfiltrationAttempt
{
    public string RequestId { get; set; }
    public string SourceService { get; set; }
    public Uri DestinationHost { get; set; }
    public string DetectedPattern { get; set; }
    public SensitivityLevel Severity { get; set; }
    public bool WasBlocked { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>Statistics on exfiltration detection</summary>
public class ExfiltrationGuardStatistics
{
    public long TotalAnalyzed { get; set; }
    public long AttemptDetected { get; set; }
    public long AttemptsBlocked { get; set; }
    public Dictionary<string, long> PatternDetectionCounts { get; set; }
    public Dictionary<SensitivityLevel, long> SeverityCounts { get; set; }
}
```

### 4.4 IHostPolicyManager

```csharp
/// <summary>
/// Manages policies for allowed and blocked hosts/domains.
/// Supports both allowlist and blocklist modes with dynamic updates.
/// </summary>
public interface IHostPolicyManager
{
    /// <summary>
    /// Adds a host to the allowlist.
    /// </summary>
    /// <param name="host">Hostname, domain, or CIDR notation</param>
    /// <param name="reason">Audit trail reason for addition</param>
    Task AddToAllowlistAsync(string host, string reason);

    /// <summary>
    /// Removes a host from the allowlist.
    /// </summary>
    Task RemoveFromAllowlistAsync(string host, string reason);

    /// <summary>
    /// Adds a host to the blocklist.
    /// </summary>
    Task AddToBlocklistAsync(string host, string reason);

    /// <summary>
    /// Removes a host from the blocklist.
    /// </summary>
    Task RemoveFromBlocklistAsync(string host, string reason);

    /// <summary>
    /// Evaluates if a host is allowed based on current policies.
    /// </summary>
    /// <param name="hostname">The hostname to evaluate</param>
    /// <param name="context">Optional context for service-specific policies</param>
    Task<HostPolicyEvaluationResult> EvaluateHostAsync(
        string hostname,
        HostEvaluationContext context = null);

    /// <summary>
    /// Retrieves all allowlist entries.
    /// </summary>
    Task<List<HostPolicyEntry>> GetAllowlistAsync(
        int pageSize = 100,
        int pageNumber = 1);

    /// <summary>
    /// Retrieves all blocklist entries.
    /// </summary>
    Task<List<HostPolicyEntry>> GetBlocklistAsync(
        int pageSize = 100,
        int pageNumber = 1);

    /// <summary>
    /// Registers a service-specific policy override.
    /// </summary>
    /// <param name="service">The service identifier</param>
    /// <param name="host">The host to allow for this service</param>
    /// <param name="reason">Audit trail reason</param>
    Task RegisterServicePolicyAsync(
        string service,
        string host,
        string reason);

    /// <summary>
    /// Sets the policy mode (Allowlist vs Blocklist).
    /// </summary>
    Task SetPolicyModeAsync(HostPolicyMode mode);

    /// <summary>
    /// Gets current policy statistics.
    /// </summary>
    Task<HostPolicyStatistics> GetStatisticsAsync();
}

/// <summary>Policy evaluation result for a host</summary>
public class HostPolicyEvaluationResult
{
    public bool IsAllowed { get; set; }
    public HostPolicyDecisionReason Reason { get; set; }
    public string MatchedRule { get; set; }
    public long EvaluationTimeMs { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

/// <summary>Reason for policy decision</summary>
public enum HostPolicyDecisionReason
{
    OnAllowlist,
    OnBlocklist,
    NoMatchInAllowlist,
    NotOnBlocklist,
    TimeBasedRestriction,
    ServicePolicyOverride
}

/// <summary>A host policy entry</summary>
public class HostPolicyEntry
{
    public string Host { get; set; }
    public string HostPattern { get; set; } // Domain, wildcard, or CIDR
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string CreatedBy { get; set; }
    public string Reason { get; set; }
}

/// <summary>Context for host evaluation</summary>
public class HostEvaluationContext
{
    public string Service { get; set; }
    public string Environment { get; set; } // "production", "staging"
    public Dictionary<string, object> Metadata { get; set; }
}

/// <summary>Host policy mode enumeration</summary>
public enum HostPolicyMode
{
    /// <summary>Only hosts on allowlist are permitted</summary>
    AllowlistMode = 1,
    /// <summary>Hosts on blocklist are prohibited</summary>
    BlocklistMode = 2
}

/// <summary>Statistics on host policies</summary>
public class HostPolicyStatistics
{
    public HostPolicyMode CurrentMode { get; set; }
    public int AllowlistCount { get; set; }
    public int BlocklistCount { get; set; }
    public long HostsEvaluated { get; set; }
    public long HostsBlocked { get; set; }
    public Dictionary<string, long> BlockReasonCounts { get; set; }
}
```

### 4.5 IRequestInspector

```csharp
/// <summary>
/// Inspects and logs all outbound network requests for auditing and analysis.
/// Captures request/response metadata while protecting sensitive information.
/// </summary>
public interface IRequestInspector
{
    /// <summary>
    /// Inspects an outbound request before execution.
    /// </summary>
    /// <param name="request">The request to inspect</param>
    Task InspectRequestAsync(OutboundRequest request);

    /// <summary>
    /// Inspects a response after execution.
    /// </summary>
    /// <param name="request">The original request</param>
    /// <param name="response">The response received</param>
    /// <param name="executionTime">Total execution time in milliseconds</param>
    Task InspectResponseAsync(
        OutboundRequest request,
        SecureHttpResponse response,
        long executionTime);

    /// <summary>
    /// Logs a security event related to request processing.
    /// </summary>
    /// <param name="securityEvent">The event to log</param>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);

    /// <summary>
    /// Queries historical request logs.
    /// </summary>
    Task<List<RequestLogEntry>> QueryLogsAsync(
        RequestLogQueryCriteria criteria);

    /// <summary>
    /// Gets summary statistics for requests.
    /// </summary>
    Task<RequestInspectorStatistics> GetStatisticsAsync(
        DateTime startTime,
        DateTime endTime);

    /// <summary>
    /// Registers an alert handler for suspicious patterns.
    /// </summary>
    /// <param name="handler">The alert handler to register</param>
    Task RegisterAlertHandlerAsync(IAlertHandler handler);

    /// <summary>
    /// Sets the log retention policy.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain logs</param>
    Task SetLogRetentionAsync(int retentionDays);
}

/// <summary>Log entry for an inspected request</summary>
public class RequestLogEntry
{
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
    public string SourceService { get; set; }
    public string Method { get; set; }
    public Uri Destination { get; set; }
    public Dictionary<string, string> SanitizedHeaders { get; set; }
    public int RequestBodySizeBytes { get; set; }
    public int ResponseStatusCode { get; set; }
    public int ResponseBodySizeBytes { get; set; }
    public long TotalExecutionMs { get; set; }
    public bool WasBlocked { get; set; }
    public string BlockReason { get; set; }
}

/// <summary>Query criteria for request logs</summary>
public class RequestLogQueryCriteria
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string SourceService { get; set; }
    public Uri DestinationHost { get; set; }
    public bool? BlockedOnly { get; set; }
    public int PageSize { get; set; } = 100;
    public int PageNumber { get; set; } = 1;
}

/// <summary>A security event in request processing</summary>
public class SecurityEvent
{
    public string EventType { get; set; }
    public SensitivityLevel Severity { get; set; }
    public string Description { get; set; }
    public string SourceService { get; set; }
    public string RequestId { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

/// <summary>Alert handler for suspicious request patterns</summary>
public interface IAlertHandler
{
    Task HandleAlertAsync(SecurityEvent securityEvent);
}

/// <summary>Statistics on request inspection</summary>
public class RequestInspectorStatistics
{
    public long TotalRequestsLogged { get; set; }
    public long TotalResponsesLogged { get; set; }
    public Dictionary<int, long> StatusCodeCounts { get; set; }
    public long TotalRequestBytesLogged { get; set; }
    public long TotalResponseBytesLogged { get; set; }
    public double AverageResponseTimeMs { get; set; }
}
```

### 4.6 ICertificateValidator

```csharp
/// <summary>
/// Validates SSL/TLS certificates for outbound connections.
/// Implements certificate chain validation, expiration checking, and pinning.
/// </summary>
public interface ICertificateValidator
{
    /// <summary>
    /// Validates an X.509 certificate chain.
    /// </summary>
    /// <param name="certificate">The server certificate</param>
    /// <param name="chain">The certificate chain</param>
    /// <returns>Validation result</returns>
    Task<CertificateValidationResult> ValidateCertificateAsync(
        X509Certificate2 certificate,
        X509Chain chain);

    /// <summary>
    /// Checks certificate revocation status via CRL or OCSP.
    /// </summary>
    /// <param name="certificate">The certificate to check</param>
    /// <returns>Revocation status</returns>
    Task<CertificateRevocationStatus> CheckRevocationAsync(
        X509Certificate2 certificate);

    /// <summary>
    /// Registers a certificate pin for a specific hostname.
    /// </summary>
    /// <param name="hostname">The hostname to pin</param>
    /// <param name="certificateHash">SHA256 hash of the certificate</param>
    /// <param name="reason">Audit trail reason</param>
    Task RegisterCertificatePinAsync(
        string hostname,
        string certificateHash,
        string reason);

    /// <summary>
    /// Validates a certificate against registered pins.
    /// </summary>
    /// <param name="hostname">The hostname being connected to</param>
    /// <param name="certificate">The server certificate</param>
    Task<CertificatePinValidationResult> ValidatePinAsync(
        string hostname,
        X509Certificate2 certificate);

    /// <summary>
    /// Gets all registered certificate pins.
    /// </summary>
    Task<List<CertificatePin>> GetRegisteredPinsAsync();

    /// <summary>
    /// Monitors for certificate expiration and issues warnings.
    /// </summary>
    Task<List<ExpiringCertificate>> GetExpiringCertificatesAsync(
        int daysUntilExpiration = 30);

    /// <summary>
    /// Registers a custom certificate store (e.g., for self-signed certs).
    /// </summary>
    /// <param name="store">The certificate store</param>
    /// <param name="environment">The environment this applies to</param>
    Task RegisterCustomStoreAsync(
        X509Store store,
        string environment);
}

/// <summary>Result of certificate validation</summary>
public class CertificateValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; }
    public List<string> ValidationWarnings { get; set; }
    public X509ChainStatus[] ChainStatuses { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public string SubjectName { get; set; }
    public string IssuerName { get; set; }
}

/// <summary>Certificate revocation status</summary>
public class CertificateRevocationStatus
{
    public bool IsRevoked { get; set; }
    public DateTime? RevocationDate { get; set; }
    public string RevocationReason { get; set; }
    public RevocationCheckMethod CheckMethod { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>Revocation check method enumeration</summary>
public enum RevocationCheckMethod
{
    None,
    CRL,
    OCSP,
    Both
}

/// <summary>Result of certificate pin validation</summary>
public class CertificatePinValidationResult
{
    public bool IsValid { get; set; }
    public bool IsPinned { get; set; }
    public string MatchedPin { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>A registered certificate pin</summary>
public class CertificatePin
{
    public string Hostname { get; set; }
    public string CertificateHash { get; set; }
    public string BackupHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string CreatedBy { get; set; }
}

/// <summary>Certificate expiring soon</summary>
public class ExpiringCertificate
{
    public string Hostname { get; set; }
    public X509Certificate2 Certificate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DaysUntilExpiration { get; set; }
}
```

---

## 5. ARCHITECTURE DIAGRAMS

### 5.1 Network Security Layers - Request Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         LexiChord Application                            │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ Service A / Service B / Service C                                │   │
│  │ (Database, Cache, API Clients, etc.)                            │   │
│  └────────────────────────────┬─────────────────────────────────────┘   │
│                               │                                           │
│                        OutboundRequest                                    │
│                               ▼                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │              IOutboundRequestController                          │   │
│  │           (Central Request Evaluation Hub)                       │   │
│  └────────────────────────────┬─────────────────────────────────────┘   │
│                               │                                           │
│                ┌──────────────┼──────────────┬──────────────┐             │
│                ▼              ▼              ▼              ▼             │
│         ┌─────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐   │
│         │   Host      │ │ Data         │ │  API Key     │ │ Request  │   │
│         │ Policy      │ │ Exfiltration │ │  Validation  │ │Inspection│   │
│         │ Evaluation  │ │  Guard       │ │              │ │ & Logging│   │
│         └─────────────┘ └──────────────┘ └──────────────┘ └──────────┘   │
│              │                │                │               │         │
│         Result: Allow/       Result: Safe/    Result:         Result:   │
│         Block by Host        Block by Pattern Allow/Block     Logged    │
│                                              Keys                        │
│                ▼              ▼              ▼              ▼             │
│         ┌─────────────────────────────────────────────────────────────┐ │
│         │         Combined Security Decision Point                    │ │
│         │  All Checks PASS = Proceed / ANY Fail = Block              │ │
│         └────────────────────────┬──────────────────────────────────┘ │
│                                  │                                     │
│                          Is Request Allowed?                          │
│                                  │                                     │
│                ┌─────────────────┴──────────────────┐                 │
│                ▼                                    ▼                 │
│           YES (PROCEED)                       NO (BLOCK)              │
│                │                                    │                 │
│                ▼                                    ▼                 │
│    ┌──────────────────────┐        ┌────────────────────────────┐    │
│    │ Certificate & TLS    │        │ Rejection Response         │    │
│    │ Validation           │        │ + Audit Log Entry          │    │
│    │                      │        │ + Alert (if critical)      │    │
│    └──────────────┬───────┘        └────────────────────────────┘    │
│                   │                                                    │
│           Is Cert Valid?                                              │
│                   │                                                    │
│       ┌───────────┴───────────┐                                       │
│       ▼                       ▼                                       │
│    YES (EXEC)           NO (BLOCK)                                   │
│       │                       │                                       │
│       ▼                       ▼                                       │
│  ┌──────────────────┐    ┌─────────────┐                            │
│  │ Execute HTTP     │    │Block Request │                            │
│  │ Request with     │    │+ Audit Log   │                            │
│  │ TLS Connection   │    └─────────────┘                            │
│  └────────┬─────────┘                                                │
│           │                                                           │
│           ▼                                                           │
│  ┌──────────────────────────┐                                        │
│  │ Receive Response         │                                        │
│  │ Inspect Response         │                                        │
│  │ Log Response Metadata    │                                        │
│  │ Return to Service        │                                        │
│  └──────────────────────────┘                                        │
│                                                                        │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      Persistent Storage Layer                             │
│                                                                            │
│  ┌──────────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │  api_keys table      │  │  outbound_requests │  │  host_rules      │   │
│  │  (encrypted secrets) │  │  (audit trail)   │  │  (allowlist/      │   │
│  │                      │  │                  │  │   blocklist)      │   │
│  └──────────────────────┘  └──────────────────┘  └──────────────────┘   │
│                                                                            │
│  ┌──────────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │exfiltration_attempts │  │certificate_pins  │  │security_events   │   │
│  │(blocked patterns)    │  │(pinned certs)    │  │(audit events)    │   │
│  └──────────────────────┘  └──────────────────┘  └──────────────────┘   │
│                                                                            │
└─────────────────────────────────────────────────────────────────────────┘

Timing Targets:
- Host Policy Evaluation: < 1ms (p99)
- Data Exfiltration Scan: < 5ms (p99) for 100KB payload
- API Key Retrieval: < 5ms (p99)
- Certificate Validation: < 50ms (p99)
- Request Controller Overall: < 10ms (p99)
```

### 5.2 API Key Vault Security Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          API Key Request                                   │
│                                                                             │
│           Service Code: var key = await _vault.RetrieveKeyAsync("aws-key")│
└───────────────────────────┬──────────────────────────────────────────────┘
                            │
                            ▼
                  ┌──────────────────────┐
                  │  IApiKeyVault        │
                  │  (Interface Request) │
                  └──────────┬───────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Authentication & Authorization      │
                  │  Verify caller identity/permissions  │
                  └──────────┬──────────────────────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Audit Logging (Pre-Retrieval)       │
                  │  Log: WHO requested, WHAT key,       │
                  │  WHEN, WHY (if provided)             │
                  └──────────┬──────────────────────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Query Encrypted Storage             │
                  │  SELECT encrypted_value FROM         │
                  │  api_keys WHERE key_id = ?           │
                  └──────────┬──────────────────────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Decryption Process                  │
                  │  AES-256-GCM Decryption:             │
                  │  1. Derive Key (PBKDF2)              │
                  │  2. Extract IV/Nonce                 │
                  │  3. Decrypt Ciphertext                │
                  │  4. Verify Authentication Tag        │
                  └──────────┬──────────────────────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Return Plaintext Secret             │
                  │  (Only to Verified Caller)           │
                  └──────────┬──────────────────────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Audit Logging (Post-Retrieval)      │
                  │  Log: Retrieval Success/Failure      │
                  │  No plaintext secret is logged       │
                  └──────────┬──────────────────────────┘
                             │
                             ▼
                  ┌──────────────────────────────────────┐
                  │  Return to Service                   │
                  │  (Memory only - never logged)        │
                  └──────────────────────────────────────┘

Storage Format (PostgreSQL api_keys table):
┌─────────────────────────────────────────────────────────────┐
│ key_id          │ "aws-prod-key"                             │
│ key_type        │ "AWS"                                       │
│ encrypted_value │ [binary AES-256-GCM ciphertext + IV + tag]│
│ hash_checksum   │ SHA256(plaintext) for validation           │
│ created_at      │ 2026-01-15 10:30:00                        │
│ last_rotated    │ 2026-01-10 14:22:00                        │
│ expires_at      │ 2027-01-15 10:30:00 (optional)             │
│ owner           │ "platform-team"                            │
│ is_active       │ true                                        │
│ encryption_key  │ Reference to KMS key for encryption        │
│ created_by      │ "system-admin"                             │
└─────────────────────────────────────────────────────────────┘

Security Guarantees:
✓ Plaintext key never stored at rest
✓ Encryption uses authenticated encryption (AES-256-GCM)
✓ Key derivation uses strong algorithm (PBKDF2, 100k+ iterations)
✓ All retrieval attempts logged for audit trail
✓ Support for Hardware Security Module (HSM) key wrapping
✓ Automatic key rotation with grace period for old key
✓ Masking in logs (only "...XXXX" visible)
```

### 5.3 Data Exfiltration Prevention - Pattern Detection Flow

```
┌────────────────────────────────────────────────────────────────────┐
│                    Outbound HTTP Request                            │
│                                                                      │
│  POST https://external-api.com/data                                │
│  Authorization: Bearer sk_live_XXXXXXXXXXXXXXXXXXXXXXXX             │
│  Content-Type: application/json                                    │
│                                                                      │
│  {                                                                   │
│    "user_ssn": "123-45-6789",                                      │
│    "api_key": "AKIA7EFKDJ2K3J4K5",                                 │
│    "credit_card": "4532-1111-2222-3333",                           │
│    "password": "SuperSecretPassword123!"                           │
│  }                                                                   │
└────────────────┬─────────────────────────────────────────────────┘
                 │
                 ▼
        ┌────────────────────┐
        │ Extract Components │
        │ • Headers          │
        │ • URL Parameters   │
        │ • Body (JSON/XML)  │
        │ • Cookies          │
        └────────┬───────────┘
                 │
     ┌───────────┴───────────┬────────────────┬──────────────────┐
     ▼                       ▼                ▼                  ▼
  HEADERS              URL PARAMS          JSON BODY        COOKIES
     │                       │                │                  │
     ▼                       ▼                ▼                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│         Pattern Matching Engine (Parallel Execution)                │
│                                                                       │
│ ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│ │ AWS Key Pattern  │  │ Password Pattern │  │ SSN Pattern      │   │
│ │ AKIA[0-9A-Z]{16} │  │ (?i)password[=:] │  │ \d{3}-\d{2}-\d{4}│   │
│ │                  │  │                  │  │                  │   │
│ │ Match: FOUND ✓   │  │ Match: FOUND ✓   │  │ Match: FOUND ✓   │   │
│ └──────────────────┘  └──────────────────┘  └──────────────────┘   │
│                                                                       │
│ ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│ │ Credit Card Pat  │  │ Database ConnStr │  │ API Key Pattern  │   │
│ │ \d{4}[-\s]?\d{4} │  │ (Server=|host=)  │  │ Multiple formats │   │
│ │                  │  │                  │  │                  │   │
│ │ Match: FOUND ✓   │  │ Match: NOT FOUND │  │ Match: FOUND ✓   │   │
│ └──────────────────┘  └──────────────────┘  └──────────────────┘   │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
                             │
                             ▼
                ┌────────────────────────────────┐
                │ Aggregate Results              │
                │                                │
                │ Patterns Found:                │
                │ • AWS Access Key (CRITICAL)    │
                │ • Password (HIGH)              │
                │ • SSN (CRITICAL)               │
                │ • Credit Card (CRITICAL)       │
                │ • API Key (HIGH)               │
                │                                │
                │ Max Severity: CRITICAL         │
                │ Total Matches: 5               │
                └────────────┬───────────────────┘
                             │
                             ▼
                ┌────────────────────────────────┐
                │ Check Whitelists               │
                │ (For this service/pattern)     │
                │                                │
                │ Result:                        │
                │ All patterns on blocklist      │
                │ No valid whitelist exception   │
                │ Status: BLOCK                  │
                └────────────┬───────────────────┘
                             │
                             ▼
                ┌────────────────────────────────┐
                │ Decision: BLOCK REQUEST        │
                │                                │
                │ Action:                        │
                │ 1. Reject HTTP request         │
                │ 2. Log blocked attempt         │
                │ 3. Alert security team        │
                │ 4. Increment attempt counter  │
                │ 5. Consider rate limit action  │
                └────────────────────────────────┘

Pattern Detection Patterns (Examples):

AWS Access Key:
  Pattern: AKIA[0-9A-Z]{16}
  Severity: CRITICAL
  Example: AKIAIOSFODNN7EXAMPLE

AWS Secret Key:
  Pattern: aws_secret_access_key[=\s:]*[A-Za-z0-9/+=]{40}
  Severity: CRITICAL

Azure Connection String:
  Pattern: DefaultEndpointsProtocol=https;AccountName=[^;]+;AccountKey=[^;]+
  Severity: CRITICAL

Generic API Key:
  Pattern: [Aa]pi[_-]?[Kk]ey[=\s:]*[A-Za-z0-9_\-]{32,}
  Severity: HIGH

Password Detection:
  Pattern: (?i)(password|passwd|pwd)[=\s:]*[^\s,}]+
  Severity: HIGH

Social Security Number:
  Pattern: \d{3}-\d{2}-\d{4}
  Severity: CRITICAL

Credit Card Number:
  Pattern: \d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}
  Severity: CRITICAL

Database Connection String:
  Pattern: (Server|Host|Password|User)[=:]
  Severity: CRITICAL

Performance Target: < 5ms for 100KB payload
Accuracy Target: > 99% detection, < 0.1% false positives
```

---

## 6. DATA EXFILTRATION PATTERNS REFERENCE

### 6.1 Credential Pattern Library

#### API Key Patterns

| Provider | Pattern | Severity | Example |
|----------|---------|----------|---------|
| AWS Access Key | `AKIA[0-9A-Z]{16}` | CRITICAL | AKIAIOSFODNN7EXAMPLE |
| AWS Secret | `aws_secret_access_key[=:]*[A-Za-z0-9/+=]{40}` | CRITICAL | wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY |
| GitHub Token | `gh[pousr]_[A-Za-z0-9_]{36,255}` | CRITICAL | ghp_aAbBcCdDeEfFgGhHiIjJkKlLmMnNoOpPqQrSt |
| Azure App Registration | `[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}` | HIGH | 12345678-1234-1234-1234-123456789012 |
| Stripe API Key | `(sk\|pk)_live_[a-zA-Z0-9]{24,}` | CRITICAL | sk_live_4eC39HqLyjWDarhtT1ZdV7dc |
| SendGrid API Key | `SG\.[a-zA-Z0-9_-]{22,}` | CRITICAL | SG.1234567890123456789012345678901234567890123456789012 |
| Twilio Account SID | `AC[a-z0-9]{32}` | HIGH | ACaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa |
| Slack Bot Token | `xoxb-[0-9]{10,13}-[0-9]{10,13}-[a-zA-Z0-9]{24,33}` | HIGH | xoxb-12345678901-1234567890123-AbCdEfGhIjKlMnOpQrStUvWx |

#### Password Patterns

| Pattern | Severity | Description |
|---------|----------|-------------|
| `(?i)(password\|passwd\|pwd)[=:\s]+[^\s,}]+` | HIGH | Generic password assignment |
| `(?i)(secret\|api_secret)[=:\s]+[^\s,}]+` | HIGH | Secret key assignment |
| `(?i)(token\|auth_token)[=:\s]+[^\s,}]+` | HIGH | Authentication token |
| `"password":"[^"]*"` | HIGH | JSON password field |
| `password=[^&]*` | HIGH | URL parameter password |

#### Database Connection Strings

| Pattern | Severity | Example |
|---------|----------|---------|
| `(mongodb\|mongo)://[^@]+@[^/]+/` | CRITICAL | mongodb://user:pass@host:27017/db |
| `Server=[^;]+;User[^;]*=[^;]+;Password[^;]*=[^;]+` | CRITICAL | SQL Server conn string |
| `host=[^;]+.*password=[^;]+` | CRITICAL | PostgreSQL conn string |
| `mysql://[^@]+@[^/]+/` | CRITICAL | MySQL connection URL |
| `(redis\|memcached)://:[^@]+@[^/]+` | HIGH | Cache connection string |

#### PII Patterns

| Pattern | Severity | Description | Example |
|---------|----------|-------------|---------|
| `\d{3}-\d{2}-\d{4}` | CRITICAL | US Social Security Number | 123-45-6789 |
| `\d{9}` (context aware) | HIGH | SSN without hyphens | 123456789 |
| `(?i)phone[=:\s]*(\d{3}[-.\s]?){2}\d{4}` | HIGH | Phone number | (123) 456-7890 |
| `\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}` | CRITICAL | Credit card number | 4532-1111-2222-3333 |
| `\d{3}[-\s]?\d{2}` (SSN partial) | MEDIUM | Partial SSN | 123-45 |
| `[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}` | MEDIUM | Email address | user@example.com |

### 6.2 Whitelist & Exception Handling

Services may whitelist specific patterns for legitimate use cases:

```csharp
// Example: Analytics service legitimately needs to send event data including emails
await _exfiltrationGuard.RegisterWhitelistAsync(
    pattern: @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
    service: "AnalyticsService",
    reason: "Event tracking requires user email for analytics pipeline"
);

// Example: Test data service allowed to send fake SSNs
await _exfiltrationGuard.RegisterWhitelistAsync(
    pattern: @"999-99-\d{4}",
    service: "TestDataGeneratorService",
    reason: "Test data generation uses dummy SSNs (999-99-XXXX pattern)"
);
```

---

## 7. POSTGRESQL SCHEMA DEFINITIONS

### 7.1 API Keys Storage

```sql
-- Table for encrypted API key storage
CREATE TABLE api_keys (
    key_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_name VARCHAR(255) NOT NULL,
    key_identifier VARCHAR(255) NOT NULL UNIQUE,
    key_type VARCHAR(50) NOT NULL,  -- 'AWS', 'Azure', 'GitHub', 'Stripe', etc.
    encrypted_value BYTEA NOT NULL,
    encryption_algorithm VARCHAR(50) NOT NULL DEFAULT 'AES-256-GCM',
    encryption_key_id UUID REFERENCES kms_keys(key_id) ON DELETE RESTRICT,
    iv_nonce BYTEA NOT NULL,  -- Initialization vector/nonce for AES-GCM
    authentication_tag BYTEA NOT NULL,  -- GCM authentication tag
    hash_checksum VARCHAR(64) NOT NULL,  -- SHA256 of plaintext for validation
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255) NOT NULL,
    last_retrieved_at TIMESTAMP WITH TIME ZONE,
    last_retrieved_by VARCHAR(255),
    last_rotated_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    owner_team VARCHAR(255),
    description TEXT,
    tags JSONB,  -- Additional metadata
    version INTEGER NOT NULL DEFAULT 1,  -- For key rotation tracking
    created_index BIGINT NOT NULL DEFAULT 0  -- For fast ordering
);

CREATE INDEX idx_api_keys_service_name ON api_keys(service_name);
CREATE INDEX idx_api_keys_key_type ON api_keys(key_type);
CREATE INDEX idx_api_keys_is_active ON api_keys(is_active);
CREATE INDEX idx_api_keys_created_at ON api_keys(created_at);
CREATE INDEX idx_api_keys_expires_at ON api_keys(expires_at) WHERE expires_at IS NOT NULL;

-- Audit log for all key access
CREATE TABLE api_key_access_audit (
    audit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_id UUID NOT NULL REFERENCES api_keys(key_id) ON DELETE CASCADE,
    accessor_identity VARCHAR(255) NOT NULL,
    accessor_service VARCHAR(255),
    access_type VARCHAR(20) NOT NULL,  -- 'RETRIEVE', 'STORE', 'ROTATE', 'DELETE'
    success BOOLEAN NOT NULL,
    error_message TEXT,
    reason_for_access TEXT,
    accessed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT
);

CREATE INDEX idx_api_key_access_audit_key_id ON api_key_access_audit(key_id);
CREATE INDEX idx_api_key_access_audit_accessed_at ON api_key_access_audit(accessed_at);

-- Historical versions of rotated keys (with expiration)
CREATE TABLE api_key_versions (
    version_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_id UUID NOT NULL REFERENCES api_keys(key_id) ON DELETE CASCADE,
    encrypted_value BYTEA NOT NULL,
    version INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,  -- When old key expires
    rotation_reason TEXT
);

CREATE INDEX idx_api_key_versions_key_id ON api_key_versions(key_id);
```

### 7.2 Outbound Requests Audit Log

```sql
-- Complete audit log of all outbound requests
CREATE TABLE outbound_requests (
    request_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_service VARCHAR(255) NOT NULL,
    destination_host VARCHAR(255) NOT NULL,
    destination_uri TEXT NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    request_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    was_allowed BOOLEAN NOT NULL,
    block_reason VARCHAR(255),
    request_body_size_bytes INTEGER,
    request_body_hash VARCHAR(64),  -- SHA256 of body (without logging actual body)
    response_status_code SMALLINT,
    response_body_size_bytes INTEGER,
    total_execution_time_ms INTEGER NOT NULL,
    dns_lookup_time_ms INTEGER,
    connection_time_ms INTEGER,
    tls_handshake_time_ms INTEGER,
    request_send_time_ms INTEGER,
    response_wait_time_ms INTEGER,
    response_read_time_ms INTEGER,
    ssl_certificate_common_name VARCHAR(255),
    ssl_certificate_valid BOOLEAN,
    ssl_certificate_issuer VARCHAR(255),
    error_details TEXT,
    sanitized_request_headers JSONB,  -- Headers with sensitive data masked
    sanitized_response_headers JSONB,
    exfiltration_check_result VARCHAR(50),  -- 'SAFE', 'BLOCKED_SUSPICIOUS', etc.
    host_policy_result VARCHAR(50),  -- 'ALLOWED', 'BLOCKED', etc.
    created_index BIGINT NOT NULL DEFAULT 0  -- For fast ordering
);

CREATE INDEX idx_outbound_requests_source_service ON outbound_requests(source_service);
CREATE INDEX idx_outbound_requests_destination_host ON outbound_requests(destination_host);
CREATE INDEX idx_outbound_requests_request_timestamp ON outbound_requests(request_timestamp DESC);
CREATE INDEX idx_outbound_requests_was_allowed ON outbound_requests(was_allowed);
CREATE INDEX idx_outbound_requests_response_status ON outbound_requests(response_status_code);
```

### 7.3 Host Allowlist/Blocklist

```sql
-- Host allowlist entries
CREATE TABLE host_allowlist (
    rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    host_pattern VARCHAR(255) NOT NULL,  -- domain, wildcard, or CIDR
    pattern_type VARCHAR(20) NOT NULL,  -- 'DOMAIN', 'WILDCARD', 'CIDR', 'REGEX'
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    applies_to_services JSONB,  -- NULL = all services, or specific service list
    reason_for_addition TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true
);

CREATE INDEX idx_host_allowlist_host_pattern ON host_allowlist(host_pattern);
CREATE INDEX idx_host_allowlist_pattern_type ON host_allowlist(pattern_type);
CREATE INDEX idx_host_allowlist_is_active ON host_allowlist(is_active);

-- Host blocklist entries
CREATE TABLE host_blocklist (
    rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    host_pattern VARCHAR(255) NOT NULL,
    pattern_type VARCHAR(20) NOT NULL,
    description TEXT,
    threat_level VARCHAR(20),  -- 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE,
    applies_to_services JSONB,
    reason_for_blocking TEXT NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    external_threat_source VARCHAR(255)  -- e.g., 'VirusTotal', 'internal-security-team'
);

CREATE INDEX idx_host_blocklist_host_pattern ON host_blocklist(host_pattern);
CREATE INDEX idx_host_blocklist_threat_level ON host_blocklist(threat_level);
CREATE INDEX idx_host_blocklist_is_active ON host_blocklist(is_active);

-- Policy mode (allowlist vs blocklist)
CREATE TABLE host_policy_config (
    config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_name VARCHAR(255) UNIQUE,  -- NULL for global default
    policy_mode VARCHAR(20) NOT NULL,  -- 'ALLOWLIST' or 'BLOCKLIST'
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by VARCHAR(255) NOT NULL
);
```

### 7.4 Exfiltration Attempts

```sql
-- Blocked exfiltration attempts
CREATE TABLE exfiltration_attempts (
    attempt_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID REFERENCES outbound_requests(request_id) ON DELETE CASCADE,
    source_service VARCHAR(255) NOT NULL,
    destination_host VARCHAR(255) NOT NULL,
    detected_pattern_type VARCHAR(100) NOT NULL,  -- 'API_KEY', 'SSN', 'CC', etc.
    detected_pattern_name VARCHAR(255),
    match_count INTEGER NOT NULL,
    match_locations JSONB,  -- Where detected: ['BODY', 'HEADERS']
    severity_level VARCHAR(20) NOT NULL,  -- 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
    was_blocked BOOLEAN NOT NULL DEFAULT true,
    detected_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    matched_snippet VARCHAR(50),  -- First/last chars for identification, never full value
    matched_snippet_hash VARCHAR(64),  -- SHA256 of matched value for correlation
    alert_sent BOOLEAN NOT NULL DEFAULT false,
    investigated_at TIMESTAMP WITH TIME ZONE,
    investigation_result VARCHAR(100)  -- 'FALSE_POSITIVE', 'WHITELISTED', 'MALICIOUS'
);

CREATE INDEX idx_exfiltration_attempts_source_service ON exfiltration_attempts(source_service);
CREATE INDEX idx_exfiltration_attempts_detected_at ON exfiltration_attempts(detected_at DESC);
CREATE INDEX idx_exfiltration_attempts_severity_level ON exfiltration_attempts(severity_level);
CREATE INDEX idx_exfiltration_attempts_was_blocked ON exfiltration_attempts(was_blocked);
```

### 7.5 Certificate Management

```sql
-- Pinned certificates for critical services
CREATE TABLE certificate_pins (
    pin_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    hostname VARCHAR(255) NOT NULL UNIQUE,
    public_key_sha256 VARCHAR(64) NOT NULL,  -- SHA256 hash of public key
    backup_public_key_sha256 VARCHAR(64),  -- Backup pin
    certificate_issuer VARCHAR(255),
    certificate_expiration TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE,  -- When pin expires
    is_active BOOLEAN NOT NULL DEFAULT true,
    description TEXT
);

CREATE INDEX idx_certificate_pins_hostname ON certificate_pins(hostname);
CREATE INDEX idx_certificate_pins_is_active ON certificate_pins(is_active);

-- Certificate validation audit log
CREATE TABLE certificate_validations (
    validation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    hostname VARCHAR(255) NOT NULL,
    certificate_common_name VARCHAR(255),
    validation_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_valid BOOLEAN NOT NULL,
    validation_errors TEXT,
    chain_status VARCHAR(255),
    revocation_checked BOOLEAN NOT NULL DEFAULT false,
    revocation_status VARCHAR(50),  -- 'UNKNOWN', 'GOOD', 'REVOKED'
    pin_validated BOOLEAN NOT NULL DEFAULT false,
    pin_match BOOLEAN
);

CREATE INDEX idx_certificate_validations_hostname ON certificate_validations(hostname);
CREATE INDEX idx_certificate_validations_validation_timestamp ON certificate_validations(validation_timestamp);

-- Key Management System keys for encrypting API key vault
CREATE TABLE kms_keys (
    key_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_name VARCHAR(255) NOT NULL UNIQUE,
    key_version INTEGER NOT NULL,
    algorithm VARCHAR(50) NOT NULL,  -- 'AES-256-GCM', etc.
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    rotated_at TIMESTAMP WITH TIME ZONE,
    external_key_reference TEXT  -- Reference to HSM or external KMS
);
```

### 7.6 Security Events & Alerts

```sql
-- All security-relevant events
CREATE TABLE security_events (
    event_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(100) NOT NULL,  -- 'API_KEY_RETRIEVED', 'EXFILTRATION_BLOCKED', etc.
    severity_level VARCHAR(20) NOT NULL,
    source_service VARCHAR(255),
    source_request_id UUID,
    related_host VARCHAR(255),
    description TEXT NOT NULL,
    event_data JSONB,  -- Additional structured data
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    alert_sent BOOLEAN NOT NULL DEFAULT false,
    alert_destination VARCHAR(255)  -- Email, Slack, PagerDuty, etc.
);

CREATE INDEX idx_security_events_event_type ON security_events(event_type);
CREATE INDEX idx_security_events_severity_level ON security_events(severity_level);
CREATE INDEX idx_security_events_created_at ON security_events(created_at DESC);
CREATE INDEX idx_security_events_alert_sent ON security_events(alert_sent);
```

---

## 8. UI MOCKUPS & DESIGNS

### 8.1 API Key Management Dashboard

```
┌──────────────────────────────────────────────────────────────────────────┐
│ LexiChord Platform Administration                                         │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  API Keys Management                           [+ New Key] [  Search  ]   │
│                                                                            │
│ Filter: [Service ▼] [Type ▼] [Status ▼] [Owner ▼]                       │
│                                                                            │
│┌────────────────────────────────────────────────────────────────────────┐│
││ Key Identifier      │ Type      │ Owner          │ Created    │ Expires  ││
│├────────────────────────────────────────────────────────────────────────┤│
││ aws-prod-key       │ AWS       │ platform-team  │ Jan 15    │ Jan 15   ││
││ ██████████XXXX     │           │                │ 2026      │ 2027     ││
││                                                                          ││
││ [View] [Rotate] [Delete] [Audit Log]                                   ││
││                                                                          ││
│├────────────────────────────────────────────────────────────────────────┤│
││ azure-storage-key  │ Azure     │ backend-team   │ Feb 1     │ Feb 1    ││
││ ████████XXXX       │           │                │ 2026      │ 2027     ││
││                                                                          ││
││ [View] [Rotate] [Delete] [Audit Log]                                   ││
││                                                                          ││
│├────────────────────────────────────────────────────────────────────────┤│
││ github-token       │ GitHub    │ devops-team    │ Jan 10    │ Expires  ││
││ ██████XXXX         │           │                │ 2026      │ Soon ⚠   ││
││                                                                          ││
││ [View] [Rotate] [Delete] [Audit Log]                                   ││
││                                                                          ││
│└────────────────────────────────────────────────────────────────────────┘│
│                                                                            │
│ Showing 3 of 12 keys                    [< Previous] [1 2 3] [Next >]   │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│ Audit Log for: aws-prod-key                                [← Back]       │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Timestamp              │ Action      │ User/Service       │ Status        │
│──────────────────────────────────────────────────────────────────────────│
│ 2026-02-01 10:30 UTC   │ RETRIEVE    │ billing-service    │ SUCCESS       │
│ 2026-02-01 09:15 UTC   │ RETRIEVE    │ analytics-service  │ SUCCESS       │
│ 2026-01-28 14:22 UTC   │ ROTATE      │ system-admin       │ SUCCESS       │
│ 2026-01-28 14:21 UTC   │ RETRIEVE    │ deployment-job     │ SUCCESS       │
│ 2026-01-25 08:00 UTC   │ RETRIEVE    │ backup-service     │ SUCCESS       │
│ 2026-01-20 16:45 UTC   │ DELETE      │ system-admin       │ DENIED        │
│   (Reason: Key still in use by services)                                  │
│                                                                            │
│ Showing 6 of 248 entries                 [← Previous] [Next →]           │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│ Rotate API Key: aws-prod-key                              [× Close]       │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  Current Key Expiration: 2027-01-15                                      │
│  Last Rotated: 2026-01-28                                                │
│                                                                            │
│  New Secret Value:                                                       │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ [Paste new secret value here or generate]  [Generate Random]       │  │
│  │                                                                     │  │
│  │                                                                     │  │
│  │ ⓘ New secret will be encrypted and stored immediately            │  │
│  │   Old key will expire in 30 days (grace period)                  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│  Rotation Reason (Audit Trail):                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ [Select reason ▼]                                                  │  │
│  │  • Scheduled rotation                                              │  │
│  │  • Security incident                                               │  │
│  │  • Key compromise suspected                                        │  │
│  │  • Policy requirement                                              │  │
│  │  • Other: _____________________                                    │  │
│  │                                                                     │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│  [Cancel]                                                 [Rotate Key]    │
│                                                                            │
│  ⚠ This action is IRREVERSIBLE and will be logged in the audit trail    │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Host Allowlist/Blocklist Management

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Network Security - Host Policies                                          │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Policy Mode: [Allowlist ▼]                                              │
│              ℹ Only hosts on allowlist are permitted                     │
│                                                                            │
│ ┌──────────────────────────────────────────────────────────────────────┐ │
│ │ ALLOWLIST                        [+ Add Host] [Import] [Export]      │ │
│ ├──────────────────────────────────────────────────────────────────────┤ │
│ │ Filter: [All Services] [Expires Soon]              [Search...]       │ │
│ │                                                                       │ │
│ │ Host Pattern          │ Type    │ Service(s)     │ Expires    │ Action│ │
│ │───────────────────────────────────────────────────────────────────│ │
│ │ api.stripe.com        │ Domain  │ (all)          │ Never      │ [×]   │ │
│ │ *.googleapis.com       │ Wildcard│ (all)          │ Never      │ [×]   │ │
│ │ 10.0.0.0/8            │ CIDR    │ internal-svc   │ Never      │ [×]   │ │
│ │ 192.168.1.0/24        │ CIDR    │ db-svc         │ 2026-03-01 │ [×]   │ │
│ │ analytics.example.com  │ Domain  │ metrics-svc    │ 2026-02-15 │ [×]   │ │
│ │                                                                       │ │
│ └──────────────────────────────────────────────────────────────────────┘ │
│                                                                            │
│ ┌──────────────────────────────────────────────────────────────────────┐ │
│ │ BLOCKLIST                        [+ Add Host] [Import] [Export]      │ │
│ ├──────────────────────────────────────────────────────────────────────┤ │
│ │ Filter: [All Threats] [High/Critical]           [Search...]          │ │
│ │                                                                       │ │
│ │ Host Pattern          │ Threat │ Service(s) │ Expires │ Source │ [×]  │ │
│ │───────────────────────────────────────────────────────────────────│ │
│ │ malware.exe          │ CRIT   │ (all)      │ Never   │ VT     │     │ │
│ │ evil-c2.net          │ HIGH   │ (all)      │ Never   │ Internal      │ │
│ │ phishing.example.com  │ CRIT   │ (all)      │ Never   │ VT     │     │ │
│ │                                                                       │ │
│ └──────────────────────────────────────────────────────────────────────┘ │
│                                                                            │
│ Legend: VT = VirusTotal, Internal = Security Team                       │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│ Add Host to Allowlist                                      [× Close]       │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  Host / Domain / CIDR:                                                   │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ example.com (or *.example.com or 10.0.0.0/8)                       │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│  Pattern Type: [Domain ▼]                                               │
│                                                                            │
│  Services (leave empty for all):                                         │
│  ☐ AnalyticsService                                                     │
│  ☐ BillingService                                                       │
│  ☐ NotificationService                                                  │
│  ☐ ReportingService                                                     │
│  ☐ (all others)                                                          │
│                                                                            │
│  Expiration (optional):                                                  │
│  ☐ Never expires                                                         │
│  ☐ Expires at: [2026-03-01 ▼]                                            │
│                                                                            │
│  Reason for Addition:                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ Third-party API required by payment processing service            │  │
│  │                                                                     │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│  [Cancel]                                                   [Add Host]    │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘
```

### 8.3 Request Inspector & Security Dashboard

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Network Security Monitor - Real-time Request Inspector                    │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Time Range: [Last 24 Hours ▼]                      [Refresh] [Export]   │
│                                                                            │
│ SUMMARY METRICS                                                          │
│ ┌────────────────────┐ ┌────────────────────┐ ┌─────────────────────┐   │
│ │ Total Requests     │ │ Blocked Requests   │ │ Exfiltration Events │   │
│ │      8,247         │ │       23 (0.3%)    │ │         3 CRITICAL  │   │
│ └────────────────────┘ └────────────────────┘ └─────────────────────┘   │
│                                                                            │
│ BLOCKED REQUESTS (Last 24H)                                              │
│ ┌────────────────────────────────────────────────────────────────────┐   │
│ │ Time              │ Service       │ Destination        │ Reason     │   │
│ ├────────────────────────────────────────────────────────────────────┤   │
│ │ 14:32 UTC         │ webhook-svc   │ evil-c2.net:8080   │ Blocklist  │   │
│ │ 13:15 UTC         │ sync-service  │ unauthorized.org   │ Blocklist  │   │
│ │ 12:47 UTC         │ export-svc    │ api.stripe.com     │ Exfil:CC   │   │
│ │ [...]                                                              │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│ REQUEST TIMELINE                                                         │
│ ┌────────────────────────────────────────────────────────────────────┐   │
│ │ [████████████████████████████████████████░░░░░░░░░░░░░░░░░░░░░░░░] │   │
│ │ Requests per hour: ▔▔▔▔▔▁▁▂▂▃▃▄▄▅▅▆▆▇▇█▇▇▆▆▅▅▄▄▃▃▂▂▁▁▔▔▔▔▔      │   │
│ │                                                                     │   │
│ │ Blocked per hour:  ░░░░░░░░░░░░░░░░░░░▓▓░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│ TOP BLOCKED DESTINATIONS                                                 │
│ ┌────────────────────────────────────────────────────────────────────┐   │
│ │ Host                  │ Blocks │ Reason              │ Threat Level  │   │
│ ├────────────────────────────────────────────────────────────────────┤   │
│ │ malicious.ru          │   8    │ Blocklist           │ CRITICAL      │   │
│ │ phishing.example      │   5    │ Exfiltration (SSN)  │ CRITICAL      │   │
│ │ unknown-api.net       │   4    │ Not on allowlist    │ HIGH          │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│ ALERTS                                                                   │
│ ┌────────────────────────────────────────────────────────────────────┐   │
│ │ ⚠ 3 CRITICAL ALERTS                                                │   │
│ ├────────────────────────────────────────────────────────────────────┤   │
│ │ ● High exfiltration attempt rate (5 in 1 hour) [Details]          │   │
│ │ ● Unusual outbound traffic to new destination [Details]           │   │
│ │ ● Certificate validation failure on stripe API [Details]          │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│                                                                            │
│ FILTERS: [Service ▼] [Destination ▼] [Status ▼] [Reason ▼]              │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│ Request Details - ID: 550e8400-e29b-41d4-a716-446655440000               │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ REQUEST INFORMATION                                                      │
│ ┌──────────────────────────────────────────────────────────────────────┐ │
│ │ Timestamp:        2026-02-01 14:32:15 UTC                            │ │
│ │ Source Service:   webhook-processor                                  │ │
│ │ Destination:      https://evil-c2.net:8080/phone-home                │ │
│ │ HTTP Method:      POST                                               │ │
│ │ Status:           ✗ BLOCKED                                          │ │
│ │ Reason:           Host on blocklist (CRITICAL threat)                │ │
│ │ Request Size:     2.4 KB                                             │ │
│ │ Response Size:    N/A (blocked before connection)                    │ │
│ │ Total Time:       0 ms (evaluation only)                             │ │
│ └──────────────────────────────────────────────────────────────────────┘ │
│                                                                            │
│ REQUEST HEADERS (Sanitized)                                              │
│ ┌──────────────────────────────────────────────────────────────────────┐ │
│ │ Host: evil-c2.net:8080                                               │ │
│ │ User-Agent: LexiChord/1.0.0                                          │ │
│ │ Content-Type: application/json                                       │ │
│ │ Authorization: [REDACTED]                                            │ │
│ │ Content-Length: 2048                                                 │ │
│ └──────────────────────────────────────────────────────────────────────┘ │
│                                                                            │
│ SECURITY EVALUATION                                                      │
│ ┌──────────────────────────────────────────────────────────────────────┐ │
│ │ ✗ Host Policy:            BLOCKED (on blocklist)                     │ │
│ │ ✗ Exfiltration Guard:     SUSPICIOUS (detected API key pattern)     │ │
│ │ ? Certificate Validation: N/A (connection blocked earlier)           │ │
│ │ ? API Key Validation:     N/A (connection blocked earlier)           │ │
│ │                                                                       │ │
│ │ Overall Decision: BLOCK REQUEST                                      │ │
│ │ First violation:  Host Blocklist                                     │ │
│ └──────────────────────────────────────────────────────────────────────┘ │
│                                                                            │
│ AUDIT LOG ENTRY                                                          │
│ [Download as JSON] [View Full Details] [Investigate]                    │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 9. DEPENDENCY CHAIN

### 9.1 Module Dependencies

```
v0.18.4a: Outbound Request Controls
├── Status: FOUNDATIONAL (no dependencies)
├── Provides: IOutboundRequestController, OutboundRequest model
└── Used by: All other v0.18.4 sub-modules

v0.18.4b: API Key Vault & Protection
├── Depends on: v0.18.4a (for request context)
├── Provides: IApiKeyVault, encrypted key storage
└── Used by: Services requiring API credentials

v0.18.4c: Data Exfiltration Prevention
├── Depends on: v0.18.4a (for request inspection)
├── Provides: IDataExfiltrationGuard, pattern detection
└── Used by: IOutboundRequestController for request validation

v0.18.4d: Host Allowlist/Blocklist
├── Depends on: v0.18.4a (for request evaluation)
├── Provides: IHostPolicyManager, host evaluation
└── Used by: IOutboundRequestController for host validation

v0.18.4e: Request Inspection & Logging
├── Depends on: v0.18.4a, v0.18.4b, v0.18.4c, v0.18.4d
├── Provides: IRequestInspector, audit logging
└── Used by: IOutboundRequestController for audit trail

v0.18.4f: Certificate & TLS Validation
├── Depends on: v0.18.4a (for request execution)
├── Provides: ICertificateValidator, certificate handling
└── Used by: IOutboundRequestController before connection

Execution Order Recommendation:
1. v0.18.4a (2 days)     - Core request controller framework
2. v0.18.4b (2 days)     - API key vault (used by multiple services)
3. v0.18.4d (1 day)      - Host policies (independent validation)
4. v0.18.4c (2 days)     - Exfiltration guard (uses request inspection)
5. v0.18.4f (1 day)      - Certificate validation (independent)
6. v0.18.4e (2 days)     - Request inspection (aggregates all security decisions)
7. Integration (2 days)   - End-to-end testing and deployment
```

### 9.2 External Dependencies

| Dependency | Version | Purpose | License |
|------------|---------|---------|---------|
| .NET | 8.0+ | Framework | MIT |
| PostgreSQL | 14.0+ | Data persistence | PostgreSQL License |
| Polly | 8.0+ | Resilience/retry | BSD-3-Clause |
| MediatR | 12.0+ | Event publishing | Apache 2.0 |
| CryptographyExtensions | 1.0+ | Encryption utilities | MIT |
| System.Net.Http | Latest | HTTP client | MIT |
| System.Security.Cryptography | Latest | Crypto primitives | MIT |

---

## 10. LICENSE GATING TABLE

Features and modules gated by license tier:

| Feature | Community | Professional | Enterprise |
|---------|-----------|--------------|------------|
| Basic Outbound Request Controls | ✓ | ✓ | ✓ |
| Request Host Allowlist/Blocklist | ✓ Limited (10) | ✓ (1000) | ✓ Unlimited |
| API Key Vault | ✓ Limited (5 keys) | ✓ (100 keys) | ✓ Unlimited |
| Data Exfiltration Guard | ✗ | ✓ | ✓ |
| Custom Pattern Registration | ✗ | ✓ Limited (10) | ✓ Unlimited |
| Request Inspection & Logging | ✗ | ✓ (90 days) | ✓ Unlimited |
| Certificate Pinning | ✗ | ✓ (10 pins) | ✓ Unlimited |
| Real-time Alerting | ✗ | ✗ | ✓ |
| HSM Integration | ✗ | ✗ | ✓ |
| Advanced Audit Reports | ✗ | ✗ | ✓ |
| Slack/Email Notifications | ✗ | ✓ | ✓ |
| Security Dashboard | ✗ | ✓ | ✓ |
| API Access to Security APIs | ✗ | Limited | ✓ |
| Support SLA | Community | 24h | 2h |

---

## 11. PERFORMANCE TARGETS

### 11.1 Latency Targets

| Component | Target | Percentile | Notes |
|-----------|--------|-----------|-------|
| Host Policy Evaluation | < 1 ms | p99 | Cached rule evaluation |
| Data Exfiltration Pattern Scan | < 5 ms | p99 | For 100 KB payload |
| API Key Retrieval | < 5 ms | p99 | Including decryption |
| Certificate Validation | < 50 ms | p99 | Per certificate chain |
| Request Controller Overall | < 10 ms | p99 | All checks combined |
| Request Inspection Overhead | < 5% | p99 | Of total request time |

### 11.2 Throughput Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Request Evaluation Throughput | > 10,000 req/sec | Per node |
| API Key Retrieval Throughput | > 5,000 op/sec | Per node |
| Policy Evaluation Throughput | > 50,000 host/sec | Per node |
| Exfiltration Pattern Matches | > 20,000 scan/sec | Per node |
| Request Logging Rate | > 100,000 req/day | Distributed |

### 11.3 Storage Targets

| Data | Retention | Growth Rate |
|------|-----------|-------------|
| Outbound Request Logs | 90 days (Enterprise: unlimited) | ~2.5 GB/day per 1M req |
| Exfiltration Attempts | 1 year | ~10 MB/day |
| API Key Audit Logs | Unlimited | ~5 MB/month |
| Security Events | 1 year | ~20 MB/day |
| Certificate Validations | 90 days | ~30 MB/day |

### 11.4 Availability Targets

| Metric | Target | Notes |
|--------|--------|-------|
| API Key Vault Availability | 99.99% | Cross-region failover |
| Request Controller Availability | 99.9% | Graceful degradation |
| Host Policy Manager Availability | 99.99% | In-memory cache fallback |
| Certificate Validator Availability | 99.95% | Cached validations |

---

## 12. TESTING STRATEGY

### 12.1 Unit Testing

```gherkin
Feature: Outbound Request Controls
  Scenario: Request to allowed host is approved
    Given a service requests connection to "api.example.com"
    When the request controller evaluates the request
    Then the request should be allowed
    And audit log should record the evaluation

  Scenario: Request to blocked host is rejected
    Given a service requests connection to "malicious.net"
    And "malicious.net" is on the blocklist
    When the request controller evaluates the request
    Then the request should be blocked
    And the block reason should be "Host on blocklist"

Feature: API Key Vault
  Scenario: Retrieve encrypted key successfully
    Given an API key "aws-prod" is stored encrypted
    When the vault retrieves the key
    Then the plaintext value is returned
    And audit log records the retrieval with accessor identity

  Scenario: Key rotation creates grace period
    Given an API key with expiration of 30 days
    When the key is rotated to a new value
    Then the old key remains active for 30 days
    And new key is immediately available
    And new key version is incremented

Feature: Data Exfiltration Guard
  Scenario: API key pattern is detected and blocked
    Given a request body contains "AKIA123456789ABCDEF"
    When exfiltration guard scans the request
    Then the pattern should be detected as "AWS_KEY"
    And the request should be blocked
    And severity should be marked as CRITICAL

  Scenario: Whitelisted pattern is allowed
    Given pattern "999-99-9999" is whitelisted for "TestDataService"
    And a request from "TestDataService" contains "999-99-9999"
    When exfiltration guard scans the request
    Then the pattern should not block the request
```

### 12.2 Integration Testing

```csharp
[TestClass]
public class OutboundRequestControllerIntegrationTests
{
    private IOutboundRequestController _controller;
    private IApiKeyVault _vault;
    private IHostPolicyManager _policyManager;
    private IDataExfiltrationGuard _exfilGuard;

    [TestInitialize]
    public async Task SetupAsync()
    {
        // Use TestContainers for PostgreSQL
        _dbContainer = await PostgreSqlContainer.Start();
        // Initialize all components
    }

    [TestMethod]
    public async Task FullRequestFlow_AllChecksPassed_RequestAllowedAsync()
    {
        // Arrange
        var request = new OutboundRequest
        {
            Method = "POST",
            Destination = new Uri("https://api.stripe.com/v1/charges"),
            SourceService = "BillingService"
        };
        await _policyManager.AddToAllowlistAsync("api.stripe.com", "Required for payments");

        // Act
        var result = await _controller.ExecuteSecureRequestAsync(request, new RequestSecurityContext());

        // Assert
        Assert.IsTrue(result.SecuritySummary.WasAllowed);
        Assert.IsNull(result.SecuritySummary.BlockReason);
    }

    [TestMethod]
    public async Task FullRequestFlow_HostBlocked_RequestDeniedAsync()
    {
        // Arrange
        var request = new OutboundRequest
        {
            Destination = new Uri("https://malicious.net/phone-home")
        };
        await _policyManager.AddToBlocklistAsync("malicious.net", "Known malware C&C");

        // Act
        var result = await _controller.ExecuteSecureRequestAsync(request, new RequestSecurityContext());

        // Assert
        Assert.IsFalse(result.SecuritySummary.WasAllowed);
        Assert.AreEqual("Host on blocklist", result.SecuritySummary.BlockReason);
    }

    [TestMethod]
    public async Task FullRequestFlow_ExfiltrationDetected_RequestBlockedAsync()
    {
        // Arrange
        var request = new OutboundRequest
        {
            Method = "POST",
            Destination = new Uri("https://external-api.com/data"),
            Body = Encoding.UTF8.GetBytes("{ 'api_key': 'AKIA1234567890ABCDEF' }")
        };

        // Act
        var result = await _controller.ExecuteSecureRequestAsync(request, new RequestSecurityContext());

        // Assert
        Assert.IsFalse(result.SecuritySummary.WasAllowed);
        StringAssert.Contains(result.SecuritySummary.BlockReason, "exfiltration");
    }
}
```

### 12.3 Penetration Testing

| Test Case | Objective | Method | Expected Result |
|-----------|-----------|--------|-----------------|
| Credential Exfiltration Attempt | Verify API key detection | Send requests with known API key patterns | All patterns detected and blocked |
| Blocklist Bypass (CIDR evasion) | Test policy enforcement | Attempt to connect using IP instead of hostname | Blocked if IP matches CIDR rule |
| Certificate MITM Attack | Test TLS validation | Use self-signed cert for normally valid host | Connection rejected |
| Malicious DNS Rebinding | Test race condition | Resolve to allowed IP, then rebind to malicious IP | Pinning/validation prevents exploitation |
| Timing Attack on Key Retrieval | Constant-time comparison | Try to extract key bits via timing | Constant-time decryption used |
| Replay Attack on Audit Log | Test immutability | Attempt to modify logged security events | Tamper-evident logging in place |
| Sidecar Communication | Test service isolation | Attempt unauthorized inter-service requests | Only whitelisted services allowed |

### 12.4 Performance Testing

```csharp
[TestClass]
public class PerformanceBenchmarks
{
    [TestMethod]
    [DataRow(1000)]
    [DataRow(10000)]
    [DataRow(100000)]
    public async Task RequestControllerEvaluation_Latency_UnderTargetAsync(int iterationCount)
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterationCount; i++)
        {
            var request = GenerateTestRequest();
            await _controller.EvaluateRequestAsync(request, new RequestSecurityContext());
        }
        stopwatch.Stop();

        // Assert
        var avgLatency = stopwatch.ElapsedMilliseconds / (double)iterationCount;
        Assert.IsTrue(avgLatency < 10, $"Average latency {avgLatency}ms exceeds 10ms target");
    }

    [TestMethod]
    public async Task HostPolicyEvaluation_CachedRules_UnderTargetAsync()
    {
        // Arrange
        await _policyManager.AddToAllowlistAsync("api.example.com", "test");
        var stopwatch = Stopwatch.StartNew();

        // Act - Second call should use cache
        await _policyManager.EvaluateHostAsync("api.example.com");
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1, "Cached evaluation exceeds 1ms");
    }
}
```

---

## 13. RISKS & MITIGATIONS

### 13.1 Security Risks

| Risk | Severity | Impact | Mitigation |
|------|----------|--------|-----------|
| Key Extraction via Memory Dump | HIGH | API keys exposed to attacker with system access | Use pinned memory, zero memory after use |
| Timing Attack on Key Comparison | MEDIUM | Attacker could extract key bits via response timing | Use constant-time comparison functions |
| Pattern FP Causing DDoS | MEDIUM | Legitimate requests blocked, service unavailable | Rate limit false positive handling, whitelisting |
| Hostname Spoofing | MEDIUM | Service could bypass host policies | Always validate certificates, implement pinning |
| Database Injection (Host Rules) | MEDIUM | Attacker could modify allowlist/blocklist | Parameterized queries, input validation |
| Reverse Proxy Cache Poisoning | LOW | Cached security decisions could be wrong | Include request signatures in cache key |

### 13.2 Operational Risks

| Risk | Severity | Impact | Mitigation |
|------|----------|--------|-----------|
| Configuration Data Loss (Host Rules) | HIGH | Loss of security policies | Automated backups, replication to warm standby |
| Audit Log Deletion/Tampering | HIGH | No forensic evidence of attacks | Immutable append-only log, cryptographic signatures |
| Key Expiration Without Rotation | MEDIUM | Service outage when key expires | Automated alerts 30 days before expiration |
| Certificate Validation Failure (e.g., OCSP timeout) | MEDIUM | Connections could fail or be allowed unsafely | Graceful degradation with caching, configurable behavior |
| Performance Regression | MEDIUM | Security overhead exceeds 10ms target | Continuous benchmarking, regression detection |

### 13.3 Mitigation Controls

```
┌────────────────────────────────────────────────────────────┐
│            RISK MITIGATION CONTROL FRAMEWORK               │
├────────────────────────────────────────────────────────────┤
│                                                             │
│ PREVENTIVE CONTROLS                                        │
│ • Parameterized database queries (SQL injection)           │
│ • Input validation for all policy rules                    │
│ • TLS certificate pinning for critical services           │
│ • Rate limiting on suspicious pattern detection           │
│ • Encryption of sensitive data at rest                    │
│                                                             │
│ DETECTIVE CONTROLS                                         │
│ • Comprehensive audit logging of all security events      │
│ • Real-time alerting on policy violations                 │
│ • Anomaly detection for unusual access patterns           │
│ • Periodic security audits of policies/rules              │
│ • Log integrity verification via checksums                │
│                                                             │
│ CORRECTIVE CONTROLS                                        │
│ • Automated rollback of invalid policy changes            │
│ • Key rotation on suspected compromise                    │
│ • Automatic blocking of repeatedly violating services     │
│ • Manual override capability for false positives          │
│ • Incident response runbooks                              │
│                                                             │
│ DETECTIVE MONITORING                                       │
│ • Daily reconciliation of policies vs. actual usage       │
│ • Trending analysis of blocked requests                   │
│ • Certificate expiration alerts (30, 14, 7, 1 days)      │
│ • API key usage patterns analysis                         │
│ • Storage quota monitoring                                │
│                                                             │
└────────────────────────────────────────────────────────────┘
```

---

## 14. MEDIATRS EVENTS & INTEGRATION

### 14.1 Event Definitions

```csharp
/// <summary>
/// Published when an outbound request is evaluated.
/// </summary>
public class OutboundRequestEvaluatedEvent : INotification
{
    public string RequestId { get; set; }
    public string SourceService { get; set; }
    public Uri Destination { get; set; }
    public bool WasAllowed { get; set; }
    public List<string> ViolatedPolicies { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

/// <summary>
/// Published when an API key is retrieved from the vault.
/// </summary>
public class ApiKeyRetrievedEvent : INotification
{
    public string KeyIdentifier { get; set; }
    public string AccessorIdentity { get; set; }
    public string AccessorService { get; set; }
    public bool RetrievalSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Published when an API key is rotated.
/// </summary>
public class ApiKeyRotatedEvent : INotification
{
    public string KeyIdentifier { get; set; }
    public int NewKeyVersion { get; set; }
    public DateTime PreviousKeyExpiration { get; set; }
    public string RotationReason { get; set; }
    public DateTime RotatedAt { get; set; }
}

/// <summary>
/// Published when data exfiltration is detected.
/// </summary>
public class ExfiltrationAttemptDetectedEvent : INotification
{
    public string RequestId { get; set; }
    public string SourceService { get; set; }
    public Uri DestinationHost { get; set; }
    public string DetectedPattern { get; set; }
    public SensitivityLevel Severity { get; set; }
    public bool WasBlocked { get; set; }
    public List<string> MatchLocations { get; set; }
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Published when a host is blocked due to policy.
/// </summary>
public class HostBlockedEvent : INotification
{
    public string RequestId { get; set; }
    public string SourceService { get; set; }
    public string BlockedHost { get; set; }
    public string BlockingRule { get; set; }
    public string BlockReason { get; set; }  // 'ALLOWLIST_NOT_FOUND', 'BLOCKLIST_MATCH'
    public DateTime BlockedAt { get; set; }
}

/// <summary>
/// Published when certificate validation fails.
/// </summary>
public class CertificateValidationFailedEvent : INotification
{
    public string RequestId { get; set; }
    public Uri Destination { get; set; }
    public string CertificateCommonName { get; set; }
    public List<string> ValidationErrors { get; set; }
    public DateTime FailedAt { get; set; }
}

/// <summary>
/// Published when a request is blocked and needs investigation.
/// </summary>
public class SecurityIncidentDetectedEvent : INotification
{
    public string IncidentId { get; set; }
    public string RequestId { get; set; }
    public string SourceService { get; set; }
    public string IncidentType { get; set; }  // 'EXFILTRATION', 'HOST_BLOCKED', 'CERT_INVALID'
    public string Severity { get; set; }
    public Dictionary<string, object> EventData { get; set; }
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Published when suspicious patterns are detected at scale.
/// </summary>
public class AnomalolyDetectedEvent : INotification
{
    public string AnomalyId { get; set; }
    public string AnomalyType { get; set; }  // 'HIGH_BLOCK_RATE', 'CREDENTIAL_LEAK_PATTERN'
    public double CurrentRate { get; set; }
    public double ExpectedRate { get; set; }
    public DateTime DetectedAt { get; set; }
}
```

### 14.2 Event Handlers

```csharp
/// <summary>
/// Handles exfiltration attempt events and sends alerts.
/// </summary>
public class ExfiltrationAttemptAlertHandler
    : INotificationHandler<ExfiltrationAttemptDetectedEvent>
{
    private readonly IAlertService _alertService;
    private readonly ILogger<ExfiltrationAttemptAlertHandler> _logger;

    public async Task Handle(
        ExfiltrationAttemptDetectedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Exfiltration attempt detected: {Pattern} from {Service} to {Host}",
            notification.DetectedPattern,
            notification.SourceService,
            notification.DestinationHost);

        if (notification.Severity == SensitivityLevel.Critical)
        {
            await _alertService.SendCriticalAlertAsync(
                title: "CRITICAL: Data Exfiltration Attempt",
                description: $"Detected {notification.DetectedPattern} in request from " +
                           $"{notification.SourceService} to {notification.DestinationHost}",
                severity: AlertSeverity.Critical,
                cancellationToken: cancellationToken);
        }
    }
}

/// <summary>
/// Handles security incidents and logs them for investigation.
/// </summary>
public class SecurityIncidentLogger : INotificationHandler<SecurityIncidentDetectedEvent>
{
    private readonly ISecurityIncidentRepository _repository;
    private readonly ILogger<SecurityIncidentLogger> _logger;

    public async Task Handle(
        SecurityIncidentDetectedEvent notification,
        CancellationToken cancellationToken)
    {
        var incident = new SecurityIncident
        {
            IncidentId = notification.IncidentId,
            RequestId = notification.RequestId,
            SourceService = notification.SourceService,
            IncidentType = notification.IncidentType,
            Severity = notification.Severity,
            EventData = notification.EventData,
            DetectedAt = notification.DetectedAt,
            InvestigationStatus = "PENDING"
        };

        await _repository.CreateAsync(incident, cancellationToken);

        _logger.LogError(
            "Security incident {IncidentId} ({Type}) detected from {Service}",
            notification.IncidentId,
            notification.IncidentType,
            notification.SourceService);
    }
}

/// <summary>
/// Handles anomaly detection and escalates if threshold exceeded.
/// </summary>
public class AnomalyEscalationHandler : INotificationHandler<AnomalyDetectedEvent>
{
    private readonly IEscalationService _escalationService;

    public async Task Handle(
        AnomalyDetectedEvent notification,
        CancellationToken cancellationToken)
    {
        var rateDifference = notification.CurrentRate / notification.ExpectedRate;

        if (rateDifference > 5.0)  // 5x above normal
        {
            await _escalationService.EscalateToSecurityTeamAsync(
                anomalyId: notification.AnomalyId,
                anomalyType: notification.AnomalyType,
                currentRate: notification.CurrentRate,
                severity: EscalationSeverity.Critical,
                cancellationToken: cancellationToken);
        }
    }
}
```

### 14.3 Event Subscribers

```csharp
// In Startup/DependencyInjection Configuration

services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<OutboundRequestController>();
});

// Register all event handlers
services
    .AddScoped<INotificationHandler<OutboundRequestEvaluatedEvent>,
        OutboundRequestAuditHandler>()
    .AddScoped<INotificationHandler<ApiKeyRetrievedEvent>,
        ApiKeyAccessAuditHandler>()
    .AddScoped<INotificationHandler<ExfiltrationAttemptDetectedEvent>,
        ExfiltrationAttemptAlertHandler>()
    .AddScoped<INotificationHandler<ExfiltrationAttemptDetectedEvent>,
        ExfiltrationAttemptAuditHandler>()
    .AddScoped<INotificationHandler<HostBlockedEvent>,
        HostBlockedAuditHandler>()
    .AddScoped<INotificationHandler<CertificateValidationFailedEvent>,
        CertificateValidationAlertHandler>()
    .AddScoped<INotificationHandler<SecurityIncidentDetectedEvent>,
        SecurityIncidentLogger>()
    .AddScoped<INotificationHandler<AnomalolyDetectedEvent>,
        AnomalyEscalationHandler>();
```

---

## 15. IMPLEMENTATION ROADMAP

### Phase 1: Foundation (Week 1-2)
- Implement IOutboundRequestController and core request pipeline
- Set up PostgreSQL schema and migrations
- Create unit test framework
- Implement MediatR event infrastructure

### Phase 2: Core Security Controls (Week 3-4)
- Implement IApiKeyVault with AES-256-GCM encryption
- Implement IHostPolicyManager with evaluation logic
- Implement IDataExfiltrationGuard with pattern matching
- Create integration tests

### Phase 3: Operational Features (Week 5-6)
- Implement IRequestInspector and logging infrastructure
- Implement ICertificateValidator
- Build UI dashboards (API key management, host policies, request inspector)
- Create REST APIs for policy management

### Phase 4: Hardening & Deployment (Week 7-8)
- Penetration testing and security review
- Performance benchmarking and optimization
- Documentation and runbooks
- Staged rollout (dev → staging → production)

---

**END OF DOCUMENT**

Document ID: LCS-SBD-v0.18.4-SEC | Status: Draft | Total Lines: 1800+

