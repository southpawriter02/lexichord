# Lexichord Specification Breakdown Document (SBD)
## v0.19.1-RES: Exception Framework

**Document ID:** LCS-SBD-v0.19.1-RES
**Version:** 1.0.0
**Status:** DRAFT
**Date Created:** 2026-02-01
**Last Modified:** 2026-02-01
**Author:** Lexichord Development Team
**Classification:** Technical Specification

---

## 1. DOCUMENT CONTROL

### 1.1 Revision History

| Version | Date       | Author | Status | Changes                          |
|---------|------------|--------|--------|----------------------------------|
| 1.0.0   | 2026-02-01 | Team   | DRAFT  | Initial comprehensive breakdown  |

### 1.2 Document Metadata

- **Product:** Lexichord (LCS) Platform
- **Module:** Exception Framework (RES)
- **Scope Type:** Major Feature Implementation
- **Total Effort:** 52 hours
- **Target Release:** v0.19.1
- **Stakeholders:** Platform Team, DevOps, QA, Documentation
- **Approval Required:** CTO, Product Manager, Tech Lead

### 1.3 Approval Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| CTO | [Pending] | [ ] | [ ] |
| Product Manager | [Pending] | [ ] | [ ] |
| Tech Lead | [Pending] | [ ] | [ ] |

---

## 2. EXECUTIVE SUMMARY

### 2.1 Overview

Version 0.19.1 introduces a **comprehensive Exception Framework** that establishes structured error handling across the Lexichord platform. This framework provides a unified approach to error management, encompassing error code registry, exception hierarchy, user-friendly messaging, error context management, global exception handling, and error presentation UI.

### 2.2 Business Value

- **Improved User Experience:** Consistent, clear error messages in multiple languages
- **Operational Excellence:** Structured error tracking and monitoring capabilities
- **Development Efficiency:** Standardized exception handling reduces debugging time
- **Compliance:** Comprehensive error logging for audit trails and compliance requirements
- **Maintainability:** Centralized error management reduces code duplication

### 2.3 Key Objectives

1. Establish a hierarchical exception taxonomy
2. Create comprehensive error code registry with standardized codes
3. Implement user-friendly error messaging system
4. Build global exception handler pipeline
5. Develop error presentation UI components
6. Enable multi-language error message support
7. Create error context and metadata persistence
8. Establish performance targets for error handling

### 2.4 Scope & Boundaries

**Included:**
- Exception hierarchy and base classes
- Error code registry (100+ codes across 7 categories)
- Global exception handling middleware
- User message localization
- Error context and metadata tracking
- Error presentation UI components
- PostgreSQL schema for error tracking
- MediatR event integration

**Not Included (v0.20+):**
- AI-powered error suggestions
- Automated error recovery mechanisms
- Advanced analytics dashboards
- Integration with external error tracking (Sentry, etc.)
- Error prediction and prevention ML models

### 2.5 Success Criteria

- All exception types implement standardized hierarchy
- 100% of errors produce user-friendly messages
- Error handling overhead < 1ms per operation
- 95% error code coverage across all components
- Global exception handler catches all unhandled exceptions
- Error messages available in 5+ languages
- Error context persisted to database (configurable)
- Zero data loss in error logging pipeline

---

## 3. DETAILED SUB-PARTS BREAKDOWN

### 3.1 v0.19.1a: Error Code Registry (10 hours)

#### 3.1.1 Purpose

Establish a comprehensive, standardized error code system that uniquely identifies all error conditions across the Lexichord platform. Each error code is globally unique, categorized, versioned, and contains metadata for automated handling.

#### 3.1.2 Key Deliverables

1. **Error Code Specification Document**
   - Naming convention: `LCS-{CATEGORY}-{NUMBER}` (e.g., LCS-GEN-0001)
   - Code ranges per category (0001-0999)
   - Metadata schema (error code, category, severity, description)

2. **Error Code Registry Implementation**
   - IErrorCodeRegistry interface
   - In-memory registry with database backup
   - Lookup by code, category, severity
   - Validation and registration mechanisms

3. **Error Code Catalog** (100+ codes)
   - LCS-GEN-xxxx: General/Platform (0001-0099)
   - LCS-DB-xxxx: Database (0100-0199)
   - LCS-AGT-xxxx: Agent/AI (0200-0299)
   - LCS-DOC-xxxx: Document Processing (0300-0399)
   - LCS-PRM-xxxx: Permissions/Auth (0400-0499)
   - LCS-NET-xxxx: Network/Communication (0500-0599)
   - LCS-RES-xxxx: Resource Management (0600-0699)

4. **Registry Storage**
   - Configuration file (JSON/YAML)
   - Database table (error_definitions)
   - Cache layer for performance

#### 3.1.3 Acceptance Criteria

- [ ] Error code naming convention documented and enforced
- [ ] IErrorCodeRegistry interface fully implemented
- [ ] Registry supports lookup by code, category, and severity
- [ ] All 100+ error codes defined and documented
- [ ] Error codes have severity levels (Critical, Error, Warning, Info)
- [ ] Registry can be initialized from configuration and database
- [ ] Error codes are versioned and backward compatible
- [ ] Registry supports custom error codes for extensions
- [ ] Performance: Registry lookup < 1ms
- [ ] Unit tests: 95% code coverage

#### 3.1.4 Effort Estimate: 10 hours

- Design & Architecture: 2 hours
- Interface Implementation: 2 hours
- Error Code Catalog Creation: 3 hours
- Registry Implementation & Testing: 2 hours
- Documentation: 1 hour

#### 3.1.5 Dependencies

- .NET 6+ Framework
- PostgreSQL Schema (error_definitions table)
- Configuration Management System
- Logging Framework

---

### 3.2 v0.19.1b: Exception Hierarchy (10 hours)

#### 3.2.1 Purpose

Create a comprehensive exception hierarchy that organizes all exception types used in Lexichord. The hierarchy enables type-safe exception handling and allows catch blocks to target specific error categories.

#### 3.2.2 Key Deliverables

1. **Base Exception Class**
   - LexichordException: Base class for all LCS exceptions
   - Properties: ErrorCode, Severity, UserMessage, ErrorContext
   - Constructors for various initialization patterns

2. **Exception Hierarchy (15+ types)**
   - ValidationException (for validation failures)
   - DatabaseException (for DB operations)
   - AuthenticationException (for auth failures)
   - AuthorizationException (for permission failures)
   - ResourceNotFoundException (for missing resources)
   - ConflictException (for resource conflicts)
   - OperationTimeoutException (for timeout scenarios)
   - ExternalServiceException (for external service failures)
   - AgentException (for AI agent failures)
   - DocumentProcessingException (for document issues)
   - ConfigurationException (for config problems)
   - StateException (for invalid state transitions)
   - OperationCanceledException (for cancelled operations)
   - CircuitBreakerOpenException (for circuit breaker trips)
   - RateLimitedException (for rate limit violations)

3. **Exception Metadata**
   - Error code association
   - Stack trace capture
   - Inner exception chaining
   - Context data (request ID, user ID, timestamp)
   - Localized messages
   - Recovery suggestions

#### 3.2.3 Acceptance Criteria

- [ ] LexichordException base class fully implemented
- [ ] All exception types inherit from LexichordException
- [ ] Exception hierarchy follows SOLID principles
- [ ] Each exception type has clear, documented purpose
- [ ] Exceptions are serializable for logging/transmission
- [ ] Exception constructors support multiple initialization patterns
- [ ] Inner exception chaining works correctly
- [ ] Stack trace is preserved through exception hierarchy
- [ ] Exceptions work with dependency injection
- [ ] Unit tests: 100% coverage of exception types

#### 3.2.4 Effort Estimate: 10 hours

- Design & Architecture: 2 hours
- Base Class Implementation: 2 hours
- Exception Types Implementation: 3 hours
- Serialization & Integration: 2 hours
- Testing & Documentation: 1 hour

#### 3.2.5 Dependencies

- v0.19.1a: Error Code Registry
- .NET Exception Framework
- Dependency Injection Container
- Serialization Framework (Newtonsoft.JSON)

---

### 3.3 v0.19.1c: User-Friendly Messages (8 hours)

#### 3.3.1 Purpose

Implement a comprehensive user message localization system that transforms technical error information into clear, actionable messages users can understand. Messages are context-aware, multi-language, and include recovery suggestions.

#### 3.3.2 Key Deliverables

1. **User Message Service**
   - IUserErrorMessageService interface
   - Message template engine
   - Variable substitution (${variableName})
   - HTML/Plaintext support

2. **Message Catalog**
   - 100+ user-friendly error messages
   - Each maps to error code
   - Multiple variants by context
   - Rich text formatting support

3. **Localization System**
   - IErrorLocalizationService interface
   - Support for 5+ languages (EN, ES, FR, DE, JA, ZH)
   - Language detection and fallback
   - Culture-specific formatting

4. **Message Enrichment**
   - Recovery suggestions
   - Support contact information
   - Documentation links
   - Contextual help

#### 3.3.3 Acceptance Criteria

- [ ] IUserErrorMessageService fully functional
- [ ] IErrorLocalizationService supports 5+ languages
- [ ] Message templates support variable substitution
- [ ] Messages are culturally appropriate
- [ ] Recovery suggestions provided for 90%+ of errors
- [ ] Rich text formatting preserved in messages
- [ ] Fallback to English for missing translations
- [ ] Performance: Message retrieval < 100ms
- [ ] Unit tests: 95% coverage
- [ ] Translation quality reviewed by native speakers

#### 3.3.4 Effort Estimate: 8 hours

- Message Service Design: 1.5 hours
- Message Catalog Creation: 2.5 hours
- Localization System: 2 hours
- Translation & Testing: 1.5 hours
- Documentation: 0.5 hours

#### 3.3.5 Dependencies

- v0.19.1a: Error Code Registry
- v0.19.1b: Exception Hierarchy
- Localization Framework (i18next or similar)
- Database Message Storage

---

### 3.4 v0.19.1d: Error Context & Metadata (8 hours)

#### 3.4.1 Purpose

Establish a comprehensive system for capturing, storing, and retrieving error context and metadata. This enables detailed error investigation, pattern analysis, and automated error handling decisions.

#### 3.4.2 Key Deliverables

1. **Error Context Class**
   - IErrorContext interface
   - Automatic capture of:
     - Request ID (correlation tracking)
     - User ID (user context)
     - Timestamp (temporal tracking)
     - Component/Module information
     - Operation type
     - Stack trace
     - Environment info (server, version)
     - Custom context data

2. **Error Metadata Service**
   - Metadata collection and enrichment
   - Contextual information injection
   - Automatic field population
   - Custom data support

3. **Error Persistence**
   - Error occurrence database storage
   - error_occurrences table schema
   - Bulk insert support
   - Retention policies

4. **Error Analysis Service**
   - Error frequency analysis
   - Pattern detection
   - Trend analysis
   - Impact assessment

#### 3.4.3 Acceptance Criteria

- [ ] IErrorContext captures all required metadata
- [ ] Error context automatically populated from request
- [ ] Error occurrences persisted to database
- [ ] Retention policies configurable
- [ ] Context data searchable and queryable
- [ ] Custom context data supported
- [ ] Performance: Context capture < 0.5ms
- [ ] Database: Error persistence < 10ms
- [ ] Unit tests: 95% coverage
- [ ] Integration tests: Database persistence verified

#### 3.4.4 Effort Estimate: 8 hours

- Error Context Design: 1.5 hours
- Metadata Service Implementation: 2 hours
- Database Schema & Persistence: 2.5 hours
- Analysis Service Implementation: 1.5 hours
- Testing & Documentation: 0.5 hours

#### 3.4.5 Dependencies

- v0.19.1a: Error Code Registry
- v0.19.1b: Exception Hierarchy
- PostgreSQL Database
- Entity Framework Core
- MediatR Events

---

### 3.5 v0.19.1e: Global Exception Handlers (8 hours)

#### 3.5.1 Purpose

Implement a comprehensive global exception handling pipeline that captures all unhandled exceptions, processes them according to the exception framework, and initiates appropriate responses.

#### 3.5.2 Key Deliverables

1. **Global Exception Handler Middleware**
   - IExceptionHandler interface (handler pattern)
   - IGlobalExceptionHandler interface (pipeline)
   - Handler chain implementation
   - Error transformation pipeline

2. **Built-in Handlers**
   - ValidationExceptionHandler
   - DatabaseExceptionHandler
   - AuthenticationExceptionHandler
   - AuthorizationExceptionHandler
   - ResourceNotFoundExceptionHandler
   - ConflictExceptionHandler
   - TimeoutExceptionHandler
   - ExternalServiceExceptionHandler
   - GenericExceptionHandler (catch-all)

3. **ASP.NET Core Integration**
   - Middleware registration
   - Request context propagation
   - Response formatting
   - HTTP status code mapping

4. **Error Response Format**
   - Standardized error response structure
   - Error code, message, context
   - Nested error support
   - Timestamp and request ID

#### 3.5.3 Acceptance Criteria

- [ ] IExceptionHandler interface fully functional
- [ ] IGlobalExceptionHandler implements handler chain
- [ ] All built-in handlers implemented
- [ ] Middleware registers in ASP.NET Core pipeline
- [ ] Request context available to all handlers
- [ ] Error responses follow standard format
- [ ] HTTP status codes map correctly to exceptions
- [ ] Performance: Handler execution < 1ms
- [ ] Unhandled exceptions never exposed to client
- [ ] Unit tests: 95% coverage
- [ ] Integration tests: Full pipeline tested

#### 3.5.4 Effort Estimate: 8 hours

- Handler Pattern Design: 1.5 hours
- Handler Implementation: 2.5 hours
- Middleware Integration: 2 hours
- Status Code Mapping: 1 hour
- Testing & Documentation: 1 hour

#### 3.5.5 Dependencies

- v0.19.1a: Error Code Registry
- v0.19.1b: Exception Hierarchy
- v0.19.1c: User-Friendly Messages
- v0.19.1d: Error Context & Metadata
- ASP.NET Core Framework
- Dependency Injection

---

### 3.6 v0.19.1f: Error Presentation UI (8 hours)

#### 3.6.1 Purpose

Develop user-facing UI components for error presentation, including dialogs, toast notifications, error details panels, and error history views. These components work across web and mobile platforms.

#### 3.6.2 Key Deliverables

1. **Error Dialog Component**
   - Modal error dialog
   - Error icon, title, message
   - Recovery suggestion section
   - Action buttons (Retry, Cancel, Details)
   - Error details expansion

2. **Toast Notification Component**
   - Non-blocking toast notifications
   - Error severity levels (colors)
   - Auto-dismiss capability
   - Manual dismiss option
   - Action buttons support

3. **Error Details Panel**
   - Error code display
   - Full error message
   - Stack trace (dev mode only)
   - Context information
   - Copy-to-clipboard functionality

4. **Error History View**
   - Recent errors listing
   - Search and filter
   - Error frequency chart
   - Error trend analysis
   - Export functionality

5. **Error Recovery UI**
   - Retry button with exponential backoff
   - Fallback mode suggestions
   - Contact support integration
   - Documentation links

#### 3.6.3 Acceptance Criteria

- [ ] Error dialog renders correctly on all screen sizes
- [ ] Toast notifications don't overlap
- [ ] Error details accessible but not overwhelming
- [ ] Responsive design works on mobile/tablet/desktop
- [ ] Accessibility (WCAG 2.1 AA) compliance
- [ ] Error recovery options clearly visible
- [ ] Support contact information integrated
- [ ] Error history searchable and filterable
- [ ] Performance: UI render < 100ms
- [ ] Unit tests: 90% coverage
- [ ] E2E tests: User workflows verified

#### 3.6.4 Effort Estimate: 8 hours

- Component Design: 2 hours
- Component Implementation: 3 hours
- Styling & Responsive Design: 1.5 hours
- Integration & Testing: 1 hour
- Documentation: 0.5 hours

#### 3.6.5 Dependencies

- v0.19.1c: User-Friendly Messages
- React/Vue.js Framework
- UI Component Library
- CSS Framework (Tailwind/Bootstrap)
- Accessibility Tools

---

## 4. C# INTERFACE SPECIFICATIONS

### 4.1 IErrorCodeRegistry Interface

```csharp
/// <summary>
/// Centralized registry for all error codes used in the Lexichord platform.
/// Provides lookup, validation, and management of standardized error codes.
/// </summary>
public interface IErrorCodeRegistry
{
    /// <summary>
    /// Registers a new error code in the registry.
    /// </summary>
    /// <param name="errorCode">The error code to register (e.g., LCS-GEN-0001)</param>
    /// <param name="metadata">Metadata associated with the error code</param>
    /// <returns>True if registration succeeded; false if code already exists</returns>
    Task<bool> RegisterErrorCodeAsync(string errorCode, ErrorCodeMetadata metadata);

    /// <summary>
    /// Retrieves error code metadata by code.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., LCS-GEN-0001)</param>
    /// <returns>ErrorCodeMetadata or null if not found</returns>
    ErrorCodeMetadata GetErrorCode(string errorCode);

    /// <summary>
    /// Retrieves all error codes in a specific category.
    /// </summary>
    /// <param name="category">Category code (e.g., GEN, DB, AGT)</param>
    /// <returns>Collection of error codes in the category</returns>
    IEnumerable<ErrorCodeMetadata> GetErrorCodesByCategory(string category);

    /// <summary>
    /// Retrieves all error codes with a specific severity level.
    /// </summary>
    /// <param name="severity">Error severity level</param>
    /// <returns>Collection of error codes with specified severity</returns>
    IEnumerable<ErrorCodeMetadata> GetErrorCodesBySeverity(ErrorSeverity severity);

    /// <summary>
    /// Validates that an error code exists in the registry.
    /// </summary>
    /// <param name="errorCode">The error code to validate</param>
    /// <returns>True if error code is registered; false otherwise</returns>
    bool IsErrorCodeRegistered(string errorCode);

    /// <summary>
    /// Checks if an error code is valid according to naming convention.
    /// Format: LCS-{CATEGORY}-{NUMBER} (e.g., LCS-GEN-0001)
    /// </summary>
    /// <param name="errorCode">The error code to validate</param>
    /// <returns>True if code follows naming convention; false otherwise</returns>
    bool IsValidErrorCodeFormat(string errorCode);

    /// <summary>
    /// Retrieves all registered error codes.
    /// </summary>
    /// <returns>All registered error codes</returns>
    IEnumerable<ErrorCodeMetadata> GetAllErrorCodes();

    /// <summary>
    /// Gets the count of registered error codes.
    /// </summary>
    /// <returns>Total count of registered error codes</returns>
    int GetErrorCodeCount();

    /// <summary>
    /// Reloads the error code registry from data source.
    /// Useful for updating registry without restart.
    /// </summary>
    Task ReloadAsync();
}

/// <summary>
/// Metadata for an error code.
/// </summary>
public class ErrorCodeMetadata
{
    /// <summary>Error code (e.g., LCS-GEN-0001)</summary>
    public string Code { get; set; }

    /// <summary>Category (GEN, DB, AGT, DOC, PRM, NET, RES)</summary>
    public string Category { get; set; }

    /// <summary>Numeric code within category (0001-0999)</summary>
    public int NumericCode { get; set; }

    /// <summary>Human-readable name (e.g., "Invalid Configuration")</summary>
    public string Name { get; set; }

    /// <summary>Detailed description of the error condition</summary>
    public string Description { get; set; }

    /// <summary>Error severity level</summary>
    public ErrorSeverity Severity { get; set; }

    /// <summary>The exception type that corresponds to this error code</summary>
    public Type ExceptionType { get; set; }

    /// <summary>Whether this error is user-facing (should show to user)</summary>
    public bool IsUserFacing { get; set; }

    /// <summary>Whether this error should be logged</summary>
    public bool ShouldLog { get; set; }

    /// <summary>Whether this error indicates a retryable operation</summary>
    public bool IsRetryable { get; set; }

    /// <summary>Maximum retry attempts for this error</summary>
    public int MaxRetries { get; set; }

    /// <summary>HTTP status code to return for this error</summary>
    public int HttpStatusCode { get; set; }

    /// <summary>Related error codes (e.g., prerequisites or follow-up errors)</summary>
    public string[] RelatedErrorCodes { get; set; }

    /// <summary>Version when this error code was introduced</summary>
    public string Version { get; set; }

    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last modification timestamp</summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>Custom metadata key-value pairs</summary>
    public Dictionary<string, object> CustomMetadata { get; set; }
}

/// <summary>
/// Error severity levels.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Critical error - system failure, immediate attention required</summary>
    Critical = 0,

    /// <summary>Error - operation failed, user action required</summary>
    Error = 1,

    /// <summary>Warning - operation succeeded but with issues</summary>
    Warning = 2,

    /// <summary>Informational - operation succeeded, user should be aware</summary>
    Info = 3
}
```

### 4.2 LexichordException Base Class

```csharp
/// <summary>
/// Base exception class for all Lexichord platform exceptions.
/// All exceptions in Lexichord should inherit from this class.
/// </summary>
public class LexichordException : Exception
{
    /// <summary>
    /// Initializes a new instance of the LexichordException class.
    /// </summary>
    public LexichordException()
        : base()
    {
        ErrorCode = "LCS-GEN-9999"; // Unknown error
        Severity = ErrorSeverity.Error;
        Context = new ErrorContext();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance with an error code and message.
    /// </summary>
    public LexichordException(string errorCode, string message)
        : base(message)
    {
        ValidateErrorCode(errorCode);
        ErrorCode = errorCode;
        UserMessage = message;
        Severity = ErrorSeverity.Error;
        Context = new ErrorContext();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance with error code, message, and severity.
    /// </summary>
    public LexichordException(string errorCode, string message, ErrorSeverity severity)
        : base(message)
    {
        ValidateErrorCode(errorCode);
        ErrorCode = errorCode;
        UserMessage = message;
        Severity = severity;
        Context = new ErrorContext();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance with error code, message, and inner exception.
    /// </summary>
    public LexichordException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ValidateErrorCode(errorCode);
        ErrorCode = errorCode;
        UserMessage = message;
        Severity = ErrorSeverity.Error;
        Context = new ErrorContext();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance with all parameters.
    /// </summary>
    public LexichordException(
        string errorCode,
        string message,
        string userMessage,
        ErrorSeverity severity,
        Exception innerException = null)
        : base(message, innerException)
    {
        ValidateErrorCode(errorCode);
        ErrorCode = errorCode;
        UserMessage = userMessage ?? message;
        Severity = severity;
        Context = new ErrorContext();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Standardized error code (e.g., LCS-GEN-0001).
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// User-friendly error message (localized).
    /// </summary>
    public string UserMessage { get; set; }

    /// <summary>
    /// Error severity level.
    /// </summary>
    public ErrorSeverity Severity { get; set; }

    /// <summary>
    /// Detailed error context information.
    /// </summary>
    public IErrorContext Context { get; set; }

    /// <summary>
    /// Timestamp when the exception was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional recovery suggestion for users.
    /// </summary>
    public string RecoverySuggestion { get; set; }

    /// <summary>
    /// Optional reference to documentation or help.
    /// </summary>
    public string HelpLink { get; set; }

    /// <summary>
    /// Correlation ID for tracking across systems.
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// Whether the error is retryable.
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Custom error data dictionary.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>
    /// Sets the error context for this exception.
    /// </summary>
    public LexichordException WithContext(IErrorContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        return this;
    }

    /// <summary>
    /// Sets the recovery suggestion.
    /// </summary>
    public LexichordException WithRecoverySuggestion(string suggestion)
    {
        RecoverySuggestion = suggestion;
        return this;
    }

    /// <summary>
    /// Sets the help link.
    /// </summary>
    public LexichordException WithHelpLink(string link)
    {
        HelpLink = link;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    public LexichordException WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Adds custom data to the exception.
    /// </summary>
    public LexichordException WithCustomData(string key, object value)
    {
        CustomData[key] = value;
        return this;
    }

    /// <summary>
    /// Marks the exception as retryable.
    /// </summary>
    public LexichordException AsRetryable(bool retryable = true)
    {
        IsRetryable = retryable;
        return this;
    }

    /// <summary>
    /// Validates error code format.
    /// </summary>
    private static void ValidateErrorCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("Error code cannot be null or empty", nameof(errorCode));

        if (!System.Text.RegularExpressions.Regex.IsMatch(errorCode, @"^LCS-[A-Z]{3}-\d{4}$"))
            throw new ArgumentException(
                $"Error code '{errorCode}' does not match format LCS-XXX-0000",
                nameof(errorCode));
    }

    /// <summary>
    /// Returns a string representation of the exception.
    /// </summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"LexichordException: {ErrorCode}");
        sb.AppendLine($"Message: {Message}");
        sb.AppendLine($"UserMessage: {UserMessage}");
        sb.AppendLine($"Severity: {Severity}");
        if (!string.IsNullOrEmpty(CorrelationId))
            sb.AppendLine($"CorrelationId: {CorrelationId}");
        if (InnerException != null)
            sb.AppendLine($"InnerException: {InnerException.Message}");
        return sb.ToString();
    }
}
```

### 4.3 Exception Hierarchy

```csharp
/// <summary>Exception for validation errors.</summary>
public class ValidationException : LexichordException
{
    public ValidationException(string message, string userMessage = null)
        : base("LCS-GEN-0001", message, userMessage ?? message, ErrorSeverity.Error)
    {
        HttpStatusCode = 400;
    }

    public int HttpStatusCode { get; set; }
}

/// <summary>Exception for database operation errors.</summary>
public class DatabaseException : LexichordException
{
    public DatabaseException(string message, Exception innerException = null)
        : base("LCS-DB-0100", message, "Database operation failed", ErrorSeverity.Error, innerException)
    {
        IsRetryable = true;
        MaxRetries = 3;
    }

    public int MaxRetries { get; set; }
}

/// <summary>Exception for authentication failures.</summary>
public class AuthenticationException : LexichordException
{
    public AuthenticationException(string message)
        : base("LCS-PRM-0400", message, "Authentication failed. Please log in again.", ErrorSeverity.Error)
    {
    }
}

/// <summary>Exception for authorization failures.</summary>
public class AuthorizationException : LexichordException
{
    public AuthorizationException(string message)
        : base("LCS-PRM-0401", message, "You do not have permission to perform this action.", ErrorSeverity.Error)
    {
    }
}

/// <summary>Exception for missing resources.</summary>
public class ResourceNotFoundException : LexichordException
{
    public ResourceNotFoundException(string resourceType, string resourceId)
        : base("LCS-RES-0600", $"{resourceType} '{resourceId}' not found",
            $"The requested {resourceType} could not be found.", ErrorSeverity.Error)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public string ResourceType { get; set; }
    public string ResourceId { get; set; }
}

/// <summary>Exception for resource conflicts.</summary>
public class ConflictException : LexichordException
{
    public ConflictException(string message)
        : base("LCS-RES-0601", message, "This operation would create a conflict. Please try again.", ErrorSeverity.Error)
    {
    }
}

/// <summary>Exception for timeout operations.</summary>
public class OperationTimeoutException : LexichordException
{
    public OperationTimeoutException(string operationName, int timeoutMs)
        : base("LCS-NET-0500",
            $"Operation '{operationName}' exceeded timeout of {timeoutMs}ms",
            "The operation took too long. Please try again.",
            ErrorSeverity.Error)
    {
        IsRetryable = true;
        OperationName = operationName;
        TimeoutMs = timeoutMs;
    }

    public string OperationName { get; set; }
    public int TimeoutMs { get; set; }
}

/// <summary>Exception for external service failures.</summary>
public class ExternalServiceException : LexichordException
{
    public ExternalServiceException(string serviceName, string message, Exception innerException = null)
        : base("LCS-NET-0501", $"External service '{serviceName}' failed: {message}",
            $"An external service is temporarily unavailable. Please try again later.",
            ErrorSeverity.Error, innerException)
    {
        IsRetryable = true;
        ServiceName = serviceName;
    }

    public string ServiceName { get; set; }
}

/// <summary>Exception for AI agent failures.</summary>
public class AgentException : LexichordException
{
    public AgentException(string message, Exception innerException = null)
        : base("LCS-AGT-0200", message, "The AI agent encountered an error. Please try again.",
            ErrorSeverity.Error, innerException)
    {
        IsRetryable = true;
    }
}

/// <summary>Exception for document processing errors.</summary>
public class DocumentProcessingException : LexichordException
{
    public DocumentProcessingException(string message)
        : base("LCS-DOC-0300", message, "Document processing failed. Please verify and try again.",
            ErrorSeverity.Error)
    {
    }
}

/// <summary>Exception for configuration errors.</summary>
public class ConfigurationException : LexichordException
{
    public ConfigurationException(string setting, string message)
        : base("LCS-GEN-0010", message, "System configuration error. Please contact support.",
            ErrorSeverity.Critical)
    {
        Setting = setting;
    }

    public string Setting { get; set; }
}

/// <summary>Exception for invalid state transitions.</summary>
public class StateException : LexichordException
{
    public StateException(string currentState, string requestedAction)
        : base("LCS-GEN-0020",
            $"Cannot perform '{requestedAction}' in state '{currentState}'",
            "This operation is not available in the current state.",
            ErrorSeverity.Error)
    {
        CurrentState = currentState;
        RequestedAction = requestedAction;
    }

    public string CurrentState { get; set; }
    public string RequestedAction { get; set; }
}

/// <summary>Exception for cancelled operations.</summary>
public class OperationCanceledException : LexichordException
{
    public OperationCanceledException(string operationName)
        : base("LCS-GEN-0030", $"Operation '{operationName}' was cancelled",
            "The operation was cancelled.",
            ErrorSeverity.Info)
    {
        OperationName = operationName;
    }

    public string OperationName { get; set; }
}

/// <summary>Exception for circuit breaker open conditions.</summary>
public class CircuitBreakerOpenException : LexichordException
{
    public CircuitBreakerOpenException(string serviceName)
        : base("LCS-NET-0502", $"Circuit breaker open for service '{serviceName}'",
            "Service is temporarily unavailable. Please try again in a moment.",
            ErrorSeverity.Error)
    {
        IsRetryable = true;
        ServiceName = serviceName;
    }

    public string ServiceName { get; set; }
}

/// <summary>Exception for rate limit violations.</summary>
public class RateLimitedException : LexichordException
{
    public RateLimitedException(int remainingSeconds)
        : base("LCS-NET-0503", $"Rate limit exceeded. Retry after {remainingSeconds} seconds",
            $"You are making requests too quickly. Please wait {remainingSeconds} seconds and try again.",
            ErrorSeverity.Warning)
    {
        IsRetryable = true;
        RemainingSeconds = remainingSeconds;
    }

    public int RemainingSeconds { get; set; }
}
```

### 4.4 IExceptionHandler Interface

```csharp
/// <summary>
/// Handler for a specific exception type in the exception handling pipeline.
/// Implements the Chain of Responsibility pattern.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Determines if this handler can handle the given exception.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if this handler can handle the exception; false otherwise</returns>
    bool CanHandle(Exception exception);

    /// <summary>
    /// Handles the exception and produces a response.
    /// </summary>
    /// <param name="context">The exception handling context</param>
    /// <returns>True if handled successfully; false if passed to next handler</returns>
    Task<bool> HandleAsync(ExceptionHandlerContext context);

    /// <summary>
    /// Gets the priority of this handler (lower value = higher priority).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Context information for exception handlers.
/// </summary>
public class ExceptionHandlerContext
{
    /// <summary>The exception being handled</summary>
    public Exception Exception { get; set; }

    /// <summary>The HTTP context (if applicable)</summary>
    public HttpContext HttpContext { get; set; }

    /// <summary>The request that caused the exception</summary>
    public HttpRequest Request { get; set; }

    /// <summary>The response to send back to client</summary>
    public HttpResponse Response { get; set; }

    /// <summary>The error context with metadata</summary>
    public IErrorContext ErrorContext { get; set; }

    /// <summary>User-friendly error message</summary>
    public string UserMessage { get; set; }

    /// <summary>Error code</summary>
    public string ErrorCode { get; set; }

    /// <summary>Error severity</summary>
    public ErrorSeverity Severity { get; set; }

    /// <summary>Custom handler state</summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>Request ID for correlation</summary>
    public string CorrelationId { get; set; }
}

/// <summary>
/// Global exception handler that manages a chain of exception handlers.
/// </summary>
public interface IGlobalExceptionHandler
{
    /// <summary>
    /// Registers an exception handler in the pipeline.
    /// </summary>
    /// <param name="handler">The handler to register</param>
    void RegisterHandler(IExceptionHandler handler);

    /// <summary>
    /// Handles an exception using the handler chain.
    /// </summary>
    /// <param name="exception">The exception to handle</param>
    /// <param name="context">The handling context</param>
    /// <returns>True if exception was handled; false if no handler could handle it</returns>
    Task<bool> HandleExceptionAsync(Exception exception, ExceptionHandlerContext context);

    /// <summary>
    /// Removes a handler from the pipeline.
    /// </summary>
    /// <param name="handler">The handler to remove</param>
    void UnregisterHandler(IExceptionHandler handler);

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    IEnumerable<IExceptionHandler> GetHandlers();

    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    void ClearHandlers();
}
```

### 4.5 IErrorPresenter Interface

```csharp
/// <summary>
/// Responsible for presenting errors to users in appropriate formats.
/// </summary>
public interface IErrorPresenter
{
    /// <summary>
    /// Presents an error in dialog format.
    /// </summary>
    /// <param name="exception">The exception to present</param>
    /// <param name="userMessage">User-friendly message</param>
    /// <returns>User's choice (Retry, Cancel, Details, etc.)</returns>
    Task<ErrorDialogResult> PresentDialogAsync(LexichordException exception, string userMessage);

    /// <summary>
    /// Presents an error as a toast notification.
    /// </summary>
    /// <param name="exception">The exception to present</param>
    /// <param name="userMessage">User-friendly message</param>
    /// <param name="duration">Display duration in milliseconds</param>
    Task PresentToastAsync(LexichordException exception, string userMessage, int duration = 5000);

    /// <summary>
    /// Presents detailed error information.
    /// </summary>
    /// <param name="exception">The exception to present</param>
    Task PresentDetailsAsync(LexichordException exception);

    /// <summary>
    /// Presents an inline error message.
    /// </summary>
    /// <param name="exception">The exception to present</param>
    /// <param name="targetElement">The UI element to attach the error to</param>
    Task PresentInlineAsync(LexichordException exception, string targetElement);
}

/// <summary>
/// Result of error dialog presentation.
/// </summary>
public enum ErrorDialogResult
{
    /// <summary>User chose to retry the operation</summary>
    Retry,

    /// <summary>User chose to cancel the operation</summary>
    Cancel,

    /// <summary>User chose to view error details</summary>
    Details,

    /// <summary>User chose to contact support</summary>
    ContactSupport,

    /// <summary>User chose to view help documentation</summary>
    ViewHelp
}
```

### 4.6 IErrorLocalizationService Interface

```csharp
/// <summary>
/// Manages localization of error messages across multiple languages.
/// </summary>
public interface IErrorLocalizationService
{
    /// <summary>
    /// Gets a localized error message for the given error code and culture.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., LCS-GEN-0001)</param>
    /// <param name="culture">Culture code (e.g., en-US, es-ES)</param>
    /// <returns>Localized error message</returns>
    string GetLocalizedMessage(string errorCode, CultureInfo culture);

    /// <summary>
    /// Gets a localized message with variable substitution.
    /// </summary>
    /// <param name="errorCode">The error code</param>
    /// <param name="culture">Culture code</param>
    /// <param name="variables">Variables for substitution {name} -> value</param>
    /// <returns>Localized message with variables substituted</returns>
    string GetLocalizedMessage(string errorCode, CultureInfo culture, Dictionary<string, object> variables);

    /// <summary>
    /// Gets the current user's preferred culture.
    /// </summary>
    /// <returns>Culture information</returns>
    CultureInfo GetUserCulture();

    /// <summary>
    /// Sets the user's preferred culture.
    /// </summary>
    /// <param name="culture">Culture to set</param>
    Task SetUserCultureAsync(CultureInfo culture);

    /// <summary>
    /// Registers a localized message.
    /// </summary>
    /// <param name="errorCode">The error code</param>
    /// <param name="culture">Culture code</param>
    /// <param name="message">Localized message</param>
    Task RegisterMessageAsync(string errorCode, CultureInfo culture, string message);

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    /// <returns>Collection of supported cultures</returns>
    IEnumerable<CultureInfo> GetSupportedCultures();

    /// <summary>
    /// Adds support for a new culture.
    /// </summary>
    /// <param name="culture">Culture to add support for</param>
    Task AddCultureAsync(CultureInfo culture);

    /// <summary>
    /// Reloads all localization data from source.
    /// </summary>
    Task ReloadAsync();
}

/// <summary>
/// User error preferences for error presentation.
/// </summary>
public class UserErrorPreferences
{
    /// <summary>Preferred culture for error messages</summary>
    public CultureInfo PreferredCulture { get; set; }

    /// <summary>Whether to show detailed error information</summary>
    public bool ShowDetailedErrors { get; set; }

    /// <summary>Whether to show recovery suggestions</summary>
    public bool ShowRecoverySuggestions { get; set; }

    /// <summary>Preferred error presentation mode (Dialog, Toast, Inline)</summary>
    public ErrorPresentationMode PreferredMode { get; set; }

    /// <summary>Whether to include stack traces (for developers)</summary>
    public bool IncludeStackTraces { get; set; }

    /// <summary>Whether to log all errors locally</summary>
    public bool LogErrorsLocally { get; set; }

    /// <summary>Whether to allow sending errors to support</summary>
    public bool AllowSendingToSupport { get; set; }
}

/// <summary>
/// Error presentation modes.
/// </summary>
public enum ErrorPresentationMode
{
    /// <summary>Show errors in modal dialogs</summary>
    Dialog = 0,

    /// <summary>Show errors as toast notifications</summary>
    Toast = 1,

    /// <summary>Show errors inline in the UI</summary>
    Inline = 2,

    /// <summary>Show errors in a sidebar panel</summary>
    Panel = 3
}
```

---

## 5. ASCII ARCHITECTURE DIAGRAMS

### 5.1 Exception Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        APPLICATION OPERATION                        │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │ TRY OPERATION   │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  Success?       │
                    └────┬────────┬───┘
            ╔═══════════════╝    ╚═══════════════╗
            ║                                    ║
            ▼                                    ▼
        RETURN RESULT                    EXCEPTION THROWN
            │                                    │
            │                        ┌──────────▼──────────┐
            │                        │ GET EXCEPTION TYPE  │
            │                        └──────────┬──────────┘
            │                                   │
            │                        ┌──────────▼──────────────────┐
            │                        │ GLOBAL EXCEPTION HANDLER    │
            │                        │ (ASP.NET Core Middleware)   │
            │                        └──────────┬──────────────────┘
            │                                   │
            │                        ┌──────────▼──────────────┐
            │                        │ HANDLER CHAIN LOOKUP   │
            │                        │ (Priority Ordered)     │
            │                        └──────────┬──────────────┘
            │                                   │
            │        ┌──────────────────────────┼──────────────────────┐
            │        │                          │                      │
            │        ▼                          ▼                      ▼
            │    ValidationHandler      DatabaseHandler        GenericHandler
            │        │                          │                      │
            │        └──────────────────────────┼──────────────────────┘
            │                                   │
            │                        ┌──────────▼───────────────┐
            │                        │ ERROR CODE LOOKUP       │
            │                        │ (IErrorCodeRegistry)    │
            │                        └──────────┬──────────────┘
            │                                   │
            │                        ┌──────────▼────────────────┐
            │                        │ GET USER MESSAGE         │
            │                        │ (ILocalizationService)   │
            │                        └──────────┬───────────────┘
            │                                   │
            │                        ┌──────────▼──────────────┐
            │                        │ CAPTURE ERROR CONTEXT  │
            │                        │ (IErrorContext)        │
            │                        └──────────┬──────────────┘
            │                                   │
            │                        ┌──────────▼────────────────┐
            │                        │ PERSIST ERROR TO DB     │
            │                        │ (error_occurrences)     │
            │                        └──────────┬───────────────┘
            │                                   │
            │                        ┌──────────▼─────────────────┐
            │                        │ PUBLISH MEDIATOR EVENT    │
            │                        │ (ErrorOccurredEvent)      │
            │                        └──────────┬────────────────┘
            │                                   │
            │                        ┌──────────▼────────────────┐
            │                        │ FORMAT ERROR RESPONSE   │
            │                        │ (StandardErrorResponse)  │
            │                        └──────────┬───────────────┘
            │                                   │
            │                        ┌──────────▼─────────────────┐
            │                        │ RETURN 400/500 RESPONSE   │
            │                        │ (With Error Details)      │
            │                        └──────────┬────────────────┘
            │                                   │
            └───────────────────────────────────┼──────────────────────┘
                                                │
                                                ▼
                                    ┌──────────────────────┐
                                    │   CLIENT RECEIVES   │
                                    │   ERROR RESPONSE    │
                                    └──────────────────────┘
```

### 5.2 Error Handling Pipeline

```
REQUEST RECEIVED
      │
      ▼
┌──────────────────────────────────┐
│ GLOBAL EXCEPTION MIDDLEWARE      │
│ (Intercepts all exceptions)      │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ EXTRACT EXCEPTION INFORMATION    │
│ - Type, Message, StackTrace      │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ LOOK UP ERROR CODE               │
│ (Exception Type -> Error Code)    │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ PRIORITY-ORDERED HANDLER CHAIN   │
│                                  │
│ [Priority 1] ValidationHandler   │
│   ├─ CanHandle?                  │
│   └─ Yes -> Delegate             │
│                                  │
│ [Priority 2] DatabaseHandler     │
│   ├─ CanHandle?                  │
│   └─ Yes -> Delegate             │
│                                  │
│ [Priority 3] GenericHandler      │
│   ├─ CanHandle?                  │
│   └─ Yes -> Delegate             │
│                                  │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ HANDLER PROCESSES EXCEPTION      │
│ - Enriches context               │
│ - Determines HTTP status code    │
│ - Prepares response format       │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ LOCALIZE USER MESSAGE            │
│ - Detect user culture            │
│ - Get translated message         │
│ - Substitute variables           │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ CAPTURE ERROR CONTEXT            │
│ - Request ID                     │
│ - User ID                        │
│ - Timestamp                      │
│ - Component information          │
│ - Custom data                    │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ PERSIST ERROR TO DATABASE        │
│ (async, non-blocking)            │
│ - error_occurrences table        │
│ - Search indexed                 │
│ - Retention policy applied       │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ PUBLISH MEDIATOR EVENTS          │
│ - ErrorOccurredEvent             │
│ - Triggers subscribers           │
│ - Analytics, Alerts, etc.        │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ FORMAT RESPONSE                  │
│ {                                │
│   "success": false,              │
│   "error": {                     │
│     "code": "LCS-GEN-0001",     │
│     "message": "...",            │
│     "userMessage": "...",        │
│     "details": { ... },          │
│     "timestamp": "2026-02-01..." │
│   }                              │
│ }                                │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│ SEND HTTP RESPONSE               │
│ (Status code based on error)     │
└────────────┬─────────────────────┘
             │
             ▼
        CLIENT RECEIVED
```

### 5.3 Error Code Lookup Flow

```
┌─────────────────────────┐
│ Exception Thrown        │
│ (Type: ValidationEx)    │
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│ Extract Exception Type  │
│ ValidationException     │
└────────────┬────────────┘
             │
             ▼
┌──────────────────────────────┐
│ IErrorCodeRegistry Lookup    │
│ Exception Type -> Error Code │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ Check in-memory cache        │
│ ├─ Hit? Return code          │
│ └─ Miss? Query database      │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ Load from error_definitions  │
│ SELECT * FROM error_defn...  │
│ WHERE exception_type = ?     │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ Validate Error Code Format   │
│ LCS-{CATEGORY}-{NUMBER}      │
│ LCS-GEN-0001 ✓               │
└────────────┬─────────────────┘
             │
             ▼
┌──────────────────────────────┐
│ Return Error Code            │
│ "LCS-GEN-0001"               │
└──────────────────────────────┘
```

---

## 6. COMPLETE ERROR CODE CATALOG

### 6.1 LCS-GEN-xxxx: General/Platform Errors (0001-0099)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-GEN-0001 | Validation Failed | Error | No | One or more validation rules failed | 400 |
| LCS-GEN-0002 | Invalid Input | Error | No | Input parameter is invalid or malformed | 400 |
| LCS-GEN-0003 | Missing Required Field | Error | No | Required field is missing from request | 400 |
| LCS-GEN-0004 | Invalid Data Type | Error | No | Data type of parameter is incorrect | 400 |
| LCS-GEN-0005 | Operation Not Supported | Error | No | The requested operation is not supported | 405 |
| LCS-GEN-0006 | Method Not Allowed | Error | No | HTTP method not allowed for this resource | 405 |
| LCS-GEN-0007 | Not Found | Error | No | Resource not found | 404 |
| LCS-GEN-0008 | Conflict | Error | No | Operation would create a conflict | 409 |
| LCS-GEN-0009 | Configuration Error | Critical | No | System configuration is invalid | 500 |
| LCS-GEN-0010 | Setting Not Found | Critical | No | Required configuration setting missing | 500 |
| LCS-GEN-0011 | Invalid Dependency | Critical | No | Required dependency not available | 500 |
| LCS-GEN-0020 | Invalid State | Error | No | Operation invalid in current state | 400 |
| LCS-GEN-0021 | State Transition Failed | Error | No | Cannot transition to requested state | 400 |
| LCS-GEN-0030 | Operation Cancelled | Info | No | Operation was cancelled by user | 200 |
| LCS-GEN-0031 | Operation Timeout | Error | Yes | Operation exceeded timeout limit | 408 |
| LCS-GEN-0040 | Serialization Error | Error | No | Failed to serialize/deserialize data | 500 |
| LCS-GEN-0041 | JSON Parsing Error | Error | No | Failed to parse JSON | 400 |
| LCS-GEN-0042 | XML Parsing Error | Error | No | Failed to parse XML | 400 |
| LCS-GEN-0050 | Feature Not Available | Error | No | Feature not available in this version | 501 |
| LCS-GEN-0051 | Feature Requires License | Error | No | Feature requires valid license | 403 |
| LCS-GEN-0060 | Internal Error | Error | Yes | Unexpected internal error occurred | 500 |
| LCS-GEN-0061 | Null Reference Error | Error | Yes | Null reference exception occurred | 500 |
| LCS-GEN-0062 | Index Out Of Range | Error | Yes | Array/List index out of bounds | 500 |
| LCS-GEN-0070 | Invalid Format | Error | No | Data format is invalid | 400 |
| LCS-GEN-0071 | Invalid Encoding | Error | No | Character encoding is invalid | 400 |
| LCS-GEN-0080 | Version Mismatch | Error | No | API version mismatch | 400 |
| LCS-GEN-0081 | Deprecated API | Warning | No | API endpoint is deprecated | 200 |
| LCS-GEN-0090 | Unknown Error | Error | Yes | Unknown error occurred | 500 |
| LCS-GEN-9999 | Unhandled Exception | Critical | Yes | Unhandled exception in system | 500 |

### 6.2 LCS-DB-xxxx: Database Errors (0100-0199)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-DB-0100 | Database Connection Failed | Error | Yes | Cannot connect to database | 503 |
| LCS-DB-0101 | Connection Timeout | Error | Yes | Database connection timed out | 503 |
| LCS-DB-0102 | Connection Pool Exhausted | Error | Yes | Database connection pool exhausted | 503 |
| LCS-DB-0110 | Query Execution Failed | Error | Yes | Database query execution failed | 500 |
| LCS-DB-0111 | Query Timeout | Error | Yes | Query execution exceeded timeout | 504 |
| LCS-DB-0112 | Invalid Query | Error | No | Query syntax is invalid | 400 |
| LCS-DB-0120 | Record Not Found | Error | No | Record does not exist | 404 |
| LCS-DB-0121 | Duplicate Key | Error | No | Unique key constraint violated | 409 |
| LCS-DB-0122 | Foreign Key Violation | Error | No | Foreign key constraint violated | 409 |
| LCS-DB-0123 | Check Constraint Violation | Error | No | Check constraint violated | 400 |
| LCS-DB-0130 | Transaction Failed | Error | Yes | Database transaction failed | 500 |
| LCS-DB-0131 | Deadlock Detected | Error | Yes | Database deadlock detected | 500 |
| LCS-DB-0132 | Lock Timeout | Error | Yes | Lock acquisition timed out | 503 |
| LCS-DB-0140 | Migration Failed | Critical | No | Database migration failed | 500 |
| LCS-DB-0141 | Schema Mismatch | Critical | No | Database schema mismatch | 500 |
| LCS-DB-0150 | Insufficient Permissions | Error | No | Insufficient permissions for operation | 403 |
| LCS-DB-0160 | Backup Failed | Error | Yes | Database backup failed | 500 |
| LCS-DB-0161 | Restore Failed | Critical | No | Database restore failed | 500 |
| LCS-DB-0170 | Disk Space Low | Critical | Yes | Insufficient disk space | 507 |
| LCS-DB-0180 | Connection String Invalid | Critical | No | Database connection string invalid | 500 |
| LCS-DB-0190 | Database Offline | Error | Yes | Database server is offline | 503 |
| LCS-DB-0191 | Database Maintenance | Error | Yes | Database under maintenance | 503 |
| LCS-DB-0199 | Database Error | Error | Yes | General database error | 500 |

### 6.3 LCS-AGT-xxxx: Agent/AI Errors (0200-0299)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-AGT-0200 | Agent Error | Error | Yes | AI agent encountered error | 500 |
| LCS-AGT-0201 | Agent Initialization Failed | Error | Yes | Failed to initialize AI agent | 500 |
| LCS-AGT-0202 | Agent Model Not Found | Error | No | AI model not found or loaded | 404 |
| LCS-AGT-0210 | Processing Error | Error | Yes | Error during AI processing | 500 |
| LCS-AGT-0211 | Processing Timeout | Error | Yes | AI processing exceeded timeout | 504 |
| LCS-AGT-0212 | Processing Cancelled | Info | No | AI processing was cancelled | 200 |
| LCS-AGT-0220 | Model Load Failed | Error | Yes | Failed to load AI model | 500 |
| LCS-AGT-0221 | Model Not Ready | Error | Yes | AI model not ready for inference | 503 |
| LCS-AGT-0222 | Model Version Mismatch | Error | No | AI model version mismatch | 400 |
| LCS-AGT-0230 | Inference Failed | Error | Yes | AI inference failed | 500 |
| LCS-AGT-0231 | Token Limit Exceeded | Error | No | Token limit exceeded for model | 400 |
| LCS-AGT-0232 | Context Too Large | Error | No | Context exceeds maximum size | 413 |
| LCS-AGT-0240 | External API Call Failed | Error | Yes | Call to external AI API failed | 502 |
| LCS-AGT-0241 | API Rate Limited | Warning | Yes | External API rate limit reached | 429 |
| LCS-AGT-0242 | API Key Invalid | Error | No | External API key is invalid | 401 |
| LCS-AGT-0250 | Memory Exhausted | Critical | Yes | Agent memory exhausted | 507 |
| LCS-AGT-0260 | Invalid Configuration | Error | No | Agent configuration is invalid | 400 |
| LCS-AGT-0270 | Validation Failed | Error | No | Agent input validation failed | 400 |
| LCS-AGT-0280 | No Response | Error | Yes | Agent failed to produce response | 500 |
| LCS-AGT-0299 | Agent Error | Error | Yes | General agent error | 500 |

### 6.4 LCS-DOC-xxxx: Document Processing Errors (0300-0399)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-DOC-0300 | Document Processing Error | Error | Yes | Error processing document | 500 |
| LCS-DOC-0301 | Document Upload Failed | Error | No | Failed to upload document | 400 |
| LCS-DOC-0302 | Document Invalid | Error | No | Document is invalid or corrupted | 400 |
| LCS-DOC-0310 | File Type Not Supported | Error | No | File type not supported | 400 |
| LCS-DOC-0311 | File Size Exceeded | Error | No | File size exceeds maximum limit | 413 |
| LCS-DOC-0312 | File Scan Failed | Error | Yes | Security scan of file failed | 500 |
| LCS-DOC-0320 | Parse Error | Error | No | Failed to parse document content | 400 |
| LCS-DOC-0321 | Extract Error | Error | Yes | Failed to extract data from document | 500 |
| LCS-DOC-0330 | OCR Failed | Error | Yes | Optical character recognition failed | 500 |
| LCS-DOC-0331 | OCR Confidence Low | Warning | No | OCR confidence below threshold | 200 |
| LCS-DOC-0340 | Conversion Failed | Error | No | Failed to convert document format | 400 |
| LCS-DOC-0341 | Rendering Failed | Error | Yes | Failed to render document | 500 |
| LCS-DOC-0350 | Indexing Failed | Error | Yes | Failed to index document | 500 |
| LCS-DOC-0360 | Storage Error | Error | Yes | Error storing document | 500 |
| LCS-DOC-0361 | Retrieval Error | Error | Yes | Error retrieving document | 500 |
| LCS-DOC-0370 | Version Conflict | Error | No | Document version conflict | 409 |
| LCS-DOC-0371 | Checkout Failed | Error | No | Failed to checkout document | 400 |
| LCS-DOC-0380 | Encoding Error | Error | No | Character encoding error in document | 400 |
| LCS-DOC-0390 | Validation Failed | Error | No | Document validation failed | 400 |
| LCS-DOC-0399 | Document Error | Error | Yes | General document processing error | 500 |

### 6.5 LCS-PRM-xxxx: Permissions/Auth Errors (0400-0499)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-PRM-0400 | Authentication Required | Error | No | Authentication required | 401 |
| LCS-PRM-0401 | Invalid Credentials | Error | No | Credentials are invalid | 401 |
| LCS-PRM-0402 | Token Expired | Error | No | Authentication token has expired | 401 |
| LCS-PRM-0403 | Token Invalid | Error | No | Authentication token is invalid | 401 |
| LCS-PRM-0410 | Authorization Failed | Error | No | Not authorized for this operation | 403 |
| LCS-PRM-0411 | Permission Denied | Error | No | Permission denied for resource | 403 |
| LCS-PRM-0412 | Insufficient Role | Error | No | User role insufficient for operation | 403 |
| LCS-PRM-0420 | Account Locked | Error | No | User account is locked | 403 |
| LCS-PRM-0421 | Account Disabled | Error | No | User account is disabled | 403 |
| LCS-PRM-0422 | Account Expired | Error | No | User account has expired | 403 |
| LCS-PRM-0430 | Password Expired | Error | No | User password has expired | 403 |
| LCS-PRM-0431 | Password Invalid | Error | No | Password does not meet requirements | 400 |
| LCS-PRM-0432 | Password Reuse | Error | No | Cannot reuse recent password | 400 |
| LCS-PRM-0440 | MFA Required | Error | No | Multi-factor authentication required | 403 |
| LCS-PRM-0441 | MFA Failed | Error | No | Multi-factor authentication failed | 403 |
| LCS-PRM-0450 | Session Expired | Error | No | User session has expired | 401 |
| LCS-PRM-0451 | Session Invalid | Error | No | User session is invalid | 401 |
| LCS-PRM-0460 | API Key Invalid | Error | No | API key is invalid or expired | 401 |
| LCS-PRM-0461 | API Key Revoked | Error | No | API key has been revoked | 401 |
| LCS-PRM-0470 | CORS Error | Error | No | Cross-origin request not allowed | 403 |
| LCS-PRM-0480 | Rate Limit Exceeded | Warning | Yes | Request rate limit exceeded | 429 |
| LCS-PRM-0490 | Audit Logged | Info | No | Action has been logged for audit | 200 |
| LCS-PRM-0499 | Permission Error | Error | No | General permission error | 403 |

### 6.6 LCS-NET-xxxx: Network/Communication Errors (0500-0599)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-NET-0500 | Timeout | Error | Yes | Operation exceeded timeout limit | 504 |
| LCS-NET-0501 | Connection Failed | Error | Yes | Connection to remote service failed | 502 |
| LCS-NET-0502 | Connection Timeout | Error | Yes | Connection timed out | 504 |
| LCS-NET-0503 | Connection Refused | Error | Yes | Connection refused by remote server | 502 |
| LCS-NET-0510 | DNS Resolution Failed | Error | Yes | DNS resolution failed | 502 |
| LCS-NET-0520 | Circuit Breaker Open | Error | Yes | Circuit breaker is open | 503 |
| LCS-NET-0530 | HTTP Error | Error | Yes | HTTP request failed | 502 |
| LCS-NET-0531 | HTTP 400 Bad Request | Error | No | Bad request to external service | 400 |
| LCS-NET-0532 | HTTP 401 Unauthorized | Error | No | Unauthorized access to external service | 401 |
| LCS-NET-0533 | HTTP 403 Forbidden | Error | No | Access forbidden to external service | 403 |
| LCS-NET-0534 | HTTP 404 Not Found | Error | No | Resource not found on external service | 404 |
| LCS-NET-0535 | HTTP 429 Too Many Requests | Warning | Yes | Too many requests to external service | 429 |
| LCS-NET-0536 | HTTP 500 Server Error | Error | Yes | External service server error | 502 |
| LCS-NET-0540 | SSL Certificate Error | Error | No | SSL certificate validation failed | 502 |
| LCS-NET-0541 | SSL Certificate Expired | Error | No | SSL certificate has expired | 502 |
| LCS-NET-0550 | Proxy Error | Error | Yes | Proxy connection failed | 502 |
| LCS-NET-0560 | Request Too Large | Error | No | Request payload exceeds maximum size | 413 |
| LCS-NET-0561 | Response Too Large | Error | Yes | Response payload exceeds maximum size | 502 |
| LCS-NET-0570 | Socket Error | Error | Yes | Socket communication error | 502 |
| LCS-NET-0580 | Serialization Error | Error | No | Failed to serialize HTTP request | 400 |
| LCS-NET-0581 | Deserialization Error | Error | Yes | Failed to deserialize HTTP response | 502 |
| LCS-NET-0590 | Service Unavailable | Error | Yes | Service is temporarily unavailable | 503 |
| LCS-NET-0591 | Service Degraded | Warning | Yes | Service is operating in degraded mode | 200 |
| LCS-NET-0599 | Network Error | Error | Yes | General network communication error | 502 |

### 6.7 LCS-RES-xxxx: Resource Management Errors (0600-0699)

| Code | Name | Severity | Retryable | Description | HTTP Status |
|------|------|----------|-----------|-------------|-------------|
| LCS-RES-0600 | Resource Not Found | Error | No | Resource does not exist | 404 |
| LCS-RES-0601 | Resource Conflict | Error | No | Resource operation conflict | 409 |
| LCS-RES-0602 | Resource Already Exists | Error | No | Resource already exists | 409 |
| LCS-RES-0603 | Resource Deleted | Error | No | Resource has been deleted | 410 |
| LCS-RES-0610 | Resource Locked | Error | No | Resource is locked by another user | 409 |
| LCS-RES-0611 | Resource In Use | Error | No | Resource is in use | 409 |
| LCS-RES-0620 | Memory Exhausted | Critical | Yes | System memory exhausted | 507 |
| LCS-RES-0621 | Disk Space Low | Critical | Yes | Disk space is critically low | 507 |
| LCS-RES-0622 | Quota Exceeded | Error | No | Resource quota exceeded | 429 |
| LCS-RES-0630 | Resource Allocation Failed | Error | Yes | Failed to allocate resource | 500 |
| LCS-RES-0631 | Resource Deallocation Failed | Error | Yes | Failed to deallocate resource | 500 |
| LCS-RES-0640 | Stream Error | Error | Yes | Error in data stream | 500 |
| LCS-RES-0641 | Buffer Overflow | Critical | No | Buffer overflow detected | 500 |
| LCS-RES-0650 | Thread Pool Exhausted | Critical | Yes | Thread pool exhausted | 503 |
| LCS-RES-0660 | Cache Error | Error | Yes | Cache operation failed | 500 |
| LCS-RES-0661 | Cache Miss | Info | No | Item not found in cache | 200 |
| LCS-RES-0670 | Queue Full | Error | Yes | Message queue is full | 503 |
| LCS-RES-0680 | Lock Acquisition Failed | Error | Yes | Failed to acquire lock | 503 |
| LCS-RES-0690 | Cleanup Failed | Warning | Yes | Resource cleanup failed | 200 |
| LCS-RES-0699 | Resource Error | Error | Yes | General resource management error | 500 |

---

## 7. POSTGRESQL SCHEMA

### 7.1 error_definitions Table

```sql
-- Error code definitions and metadata
CREATE TABLE IF NOT EXISTS error_definitions (
    id BIGSERIAL PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE, -- LCS-GEN-0001
    category VARCHAR(10) NOT NULL,   -- GEN, DB, AGT, DOC, PRM, NET, RES
    numeric_code INTEGER NOT NULL,   -- 0001-0999
    name VARCHAR(255) NOT NULL,      -- "Validation Failed"
    description TEXT,
    severity VARCHAR(20) NOT NULL,   -- CRITICAL, ERROR, WARNING, INFO
    exception_type VARCHAR(500),     -- Full type name
    is_user_facing BOOLEAN DEFAULT TRUE,
    should_log BOOLEAN DEFAULT TRUE,
    is_retryable BOOLEAN DEFAULT FALSE,
    max_retries INTEGER DEFAULT 0,
    http_status_code INTEGER DEFAULT 500,
    version VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    modified_by VARCHAR(255),
    deprecated BOOLEAN DEFAULT FALSE,
    deprecated_since VARCHAR(20),
    replacement_code VARCHAR(20),

    CONSTRAINT valid_code_format CHECK (code ~ '^LCS-[A-Z]{3}-[0-9]{4}$'),
    CONSTRAINT valid_severity CHECK (severity IN ('CRITICAL', 'ERROR', 'WARNING', 'INFO')),
    CONSTRAINT valid_http_status CHECK (http_status_code BETWEEN 100 AND 599)
);

CREATE INDEX idx_error_definitions_code ON error_definitions(code);
CREATE INDEX idx_error_definitions_category ON error_definitions(category);
CREATE INDEX idx_error_definitions_severity ON error_definitions(severity);
CREATE INDEX idx_error_definitions_exception_type ON error_definitions(exception_type);
```

### 7.2 error_occurrences Table

```sql
-- Error occurrence tracking for monitoring and analysis
CREATE TABLE IF NOT EXISTS error_occurrences (
    id BIGSERIAL PRIMARY KEY,
    error_code VARCHAR(20) NOT NULL,
    error_type VARCHAR(500),
    message TEXT NOT NULL,
    user_message TEXT,
    severity VARCHAR(20),
    http_status_code INTEGER,
    stack_trace TEXT,
    inner_exception TEXT,

    -- Context information
    request_id UUID,
    correlation_id UUID,
    user_id VARCHAR(255),
    user_name VARCHAR(255),
    tenant_id VARCHAR(255),
    component VARCHAR(255),
    operation_name VARCHAR(255),
    environment VARCHAR(50), -- Development, Staging, Production
    server_name VARCHAR(255),
    version VARCHAR(20),

    -- Request details
    http_method VARCHAR(10),
    request_path VARCHAR(1000),
    query_string TEXT,

    -- Timestamps
    occurred_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    handled_at TIMESTAMP,
    logged_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMP,

    -- Resolution tracking
    is_resolved BOOLEAN DEFAULT FALSE,
    resolution_notes TEXT,
    assigned_to VARCHAR(255),

    -- Custom data
    custom_data JSONB,

    -- Metadata
    metadata JSONB,

    CONSTRAINT valid_error_code CHECK (error_code ~ '^LCS-[A-Z]{3}-[0-9]{4}$')
);

CREATE INDEX idx_error_occurrences_code ON error_occurrences(error_code);
CREATE INDEX idx_error_occurrences_user_id ON error_occurrences(user_id);
CREATE INDEX idx_error_occurrences_occurred_at ON error_occurrences(occurred_at DESC);
CREATE INDEX idx_error_occurrences_severity ON error_occurrences(severity);
CREATE INDEX idx_error_occurrences_request_id ON error_occurrences(request_id);
CREATE INDEX idx_error_occurrences_correlation_id ON error_occurrences(correlation_id);
CREATE INDEX idx_error_occurrences_component ON error_occurrences(component);
CREATE INDEX idx_error_occurrences_is_resolved ON error_occurrences(is_resolved);
CREATE INDEX idx_error_occurrences_environment ON error_occurrences(environment);

-- Partition by date for better query performance
SELECT create_partitions('error_occurrences', 'occurred_at', 'MONTHLY', 12);
```

### 7.3 error_translations Table

```sql
-- Localized error messages
CREATE TABLE IF NOT EXISTS error_translations (
    id BIGSERIAL PRIMARY KEY,
    error_code VARCHAR(20) NOT NULL,
    language_code VARCHAR(10) NOT NULL, -- en, es, fr, de, ja, zh
    country_code VARCHAR(2),            -- US, ES, FR, etc.
    message TEXT NOT NULL,
    description TEXT,
    recovery_suggestion TEXT,
    help_link VARCHAR(1000),

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    modified_by VARCHAR(255),

    is_reviewed BOOLEAN DEFAULT FALSE,
    reviewed_by VARCHAR(255),
    reviewed_at TIMESTAMP,

    CONSTRAINT fk_error_translations_code
        FOREIGN KEY (error_code)
        REFERENCES error_definitions(code),
    CONSTRAINT unique_translation
        UNIQUE (error_code, language_code, country_code)
);

CREATE INDEX idx_error_translations_code ON error_translations(error_code);
CREATE INDEX idx_error_translations_language ON error_translations(language_code);
CREATE INDEX idx_error_translations_locale ON error_translations(language_code, country_code);
```

### 7.4 user_error_preferences Table

```sql
-- User preferences for error handling and presentation
CREATE TABLE IF NOT EXISTS user_error_preferences (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL UNIQUE,

    -- Localization preferences
    preferred_language VARCHAR(10) DEFAULT 'en',
    preferred_country_code VARCHAR(2),

    -- Display preferences
    show_detailed_errors BOOLEAN DEFAULT FALSE,
    show_recovery_suggestions BOOLEAN DEFAULT TRUE,
    show_stack_traces BOOLEAN DEFAULT FALSE,

    -- Presentation mode: DIALOG, TOAST, INLINE, PANEL
    preferred_presentation_mode VARCHAR(20) DEFAULT 'TOAST',

    -- Notification preferences
    log_errors_locally BOOLEAN DEFAULT TRUE,
    allow_sending_to_support BOOLEAN DEFAULT FALSE,

    -- Advanced options
    include_custom_data BOOLEAN DEFAULT TRUE,
    include_system_info BOOLEAN DEFAULT FALSE,
    max_error_history INTEGER DEFAULT 100,

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT valid_presentation_mode
        CHECK (preferred_presentation_mode IN ('DIALOG', 'TOAST', 'INLINE', 'PANEL'))
);

CREATE INDEX idx_user_error_preferences_user_id ON user_error_preferences(user_id);
```

### 7.5 error_analytics Table

```sql
-- Aggregated error analytics
CREATE MATERIALIZED VIEW error_analytics AS
SELECT
    error_code,
    DATE(occurred_at) as error_date,
    COUNT(*) as total_occurrences,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT request_id) as unique_requests,
    COUNT(CASE WHEN is_resolved THEN 1 END) as resolved_count,
    AVG(EXTRACT(EPOCH FROM (handled_at - occurred_at))) as avg_resolution_time_sec,
    MIN(occurred_at) as first_occurrence,
    MAX(occurred_at) as last_occurrence
FROM error_occurrences
GROUP BY error_code, DATE(occurred_at);

CREATE INDEX idx_error_analytics_code ON error_analytics(error_code);
CREATE INDEX idx_error_analytics_date ON error_analytics(error_date DESC);
```

---

## 8. UI MOCKUPS & SPECIFICATIONS

### 8.1 Error Dialog Component

```
┌─────────────────────────────────────────┐
│   ⚠️  An Error Occurred                  │
├─────────────────────────────────────────┤
│                                         │
│   Validation Failed                     │
│                                         │
│   The form contains errors that must    │
│   be corrected before continuing.       │
│                                         │
│   • Email address is invalid            │
│   • Password must be at least 8 chars   │
│   • Terms must be accepted              │
│                                         │
│   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                         │
│   🔗 Learn More  [+] Details            │
│                                         │
├─────────────────────────────────────────┤
│  [Cancel]               [Try Again]     │
└─────────────────────────────────────────┘

Properties:
- Icon: Severity-based icon (⚠️, 🔴, ℹ️)
- Title: Error code and human-readable name
- Message: User-friendly message
- Actions: Context-sensitive action buttons
- Details: Expandable error details section
- Help: Links to documentation or support
```

### 8.2 Toast Notification Component

```
┌──────────────────────────────────┐
│ ⚠️  Database connection failed    │  [×]
│                                  │
│ Please wait while we reconnect   │
│ or [Retry Now]                   │
└──────────────────────────────────┘

Properties:
- Position: Top-right corner
- Auto-dismiss: 5-8 seconds
- Action: Optional retry button
- Close: Manual dismiss button
- Color: Severity-based (red, orange, blue, green)
```

### 8.3 Inline Error Message

```
Form Layout with Inline Errors:

Email Address: [___________________]
                ❌ Invalid email format

Password:       [___________________]
                ❌ Minimum 8 characters required
                   Current: 5 characters

Terms & Conditions: [✗]
                ❌ You must accept the terms

                        [Cancel]  [Submit]
```

### 8.4 Error Details Panel

```
┌────────────────────────────────────────┐
│  Error Details                    [×]  │
├────────────────────────────────────────┤
│                                        │
│  Error Code: LCS-DB-0100              │
│  Error Type: DatabaseException         │
│  Severity: ERROR                       │
│  Timestamp: 2026-02-01T14:30:45Z      │
│  Request ID: a1b2c3d4-e5f6-7890      │
│                                        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│  Error Message:                        │
│  Cannot connect to database server     │
│                                        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│  Stack Trace: (collapsed)       [▼]   │
│                                        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│  Custom Data:                          │
│  {                                     │
│    "operation": "SELECT * FROM users"  │
│    "timeout_ms": 30000                 │
│  }                                     │
│                                        │
│  [Copy to Clipboard]  [Send to Support]│
└────────────────────────────────────────┘
```

### 8.5 Error History View

```
┌─────────────────────────────────────────────────┐
│  Recent Errors                      [Search]    │
├─────────────────────────────────────────────────┤
│ Filter: [Severity ▼] [Date ▼] [Component ▼]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Date & Time        │ Code          │ Message   │
│ ─────────────────────────────────────────────  │
│ 2/1 14:30:45      │ LCS-DB-0100   │ Connection
│ 2/1 14:25:12      │ LCS-GEN-0001  │ Validation
│ 2/1 14:20:33      │ LCS-PRM-0401  │ Auth failed
│ 2/1 14:15:00      │ LCS-DOC-0330  │ OCR failed
│ 2/1 14:10:22      │ LCS-NET-0503  │ Timeout
│                                                 │
├─────────────────────────────────────────────────┤
│  Error Frequency (Last 24 Hours)               │
│                                                 │
│  LCS-DB-0100     ████████████ (45 occurrences)│
│  LCS-GEN-0001    ████ (15 occurrences)        │
│  LCS-NET-0503    ██ (8 occurrences)           │
│                                                 │
│  [Export as CSV]  [View Trends]                │
└─────────────────────────────────────────────────┘
```

---

## 9. DEPENDENCY CHAIN

### 9.1 Internal Dependencies

```
v0.19.1f (Error Presentation UI)
    ↓ depends on
v0.19.1c (User-Friendly Messages)
    ↓ depends on
v0.19.1a (Error Code Registry)

v0.19.1e (Global Exception Handlers)
    ↓ depends on
v0.19.1c (User-Friendly Messages)
    ↓ depends on
v0.19.1a (Error Code Registry)

v0.19.1e (Global Exception Handlers)
    ↓ depends on
v0.19.1d (Error Context & Metadata)
    ↓ depends on
v0.19.1b (Exception Hierarchy)
    ↓ depends on
v0.19.1a (Error Code Registry)

All Sub-Parts
    ↓ depend on
v0.19.1b (Exception Hierarchy) - foundational

All Sub-Parts
    ↓ depend on
v0.19.1a (Error Code Registry) - foundational
```

### 9.2 External Dependencies

**Framework & Infrastructure:**
- .NET 6.0 or higher
- ASP.NET Core 6.0 or higher
- Entity Framework Core 6.0+
- MediatR 10.0+
- Newtonsoft.JSON 13.0+

**Database:**
- PostgreSQL 13+
- pgAdmin 4+ (development)

**Testing:**
- xUnit 2.4+
- Moq 4.16+
- FluentAssertions 6.0+

**Localization:**
- System.Globalization
- i18next or similar

**UI/Frontend:**
- React 18+ or Vue 3+
- TypeScript 4.0+
- Axios or Fetch API
- TailwindCSS or Bootstrap 5+

**Development Tools:**
- Visual Studio 2022 or VS Code
- SQL Server Management Studio or pgAdmin
- Postman or Insomnia (API testing)
- Git 2.30+

---

## 10. LICENSE GATING TABLE

| Feature | Gating | Required License | Details |
|---------|--------|-----------------|---------|
| Error Code Registry | Community | FREE | Basic error registry |
| Exception Hierarchy | Community | FREE | Standard exception types |
| User-Friendly Messages | Professional | LITE | Localization for 2 languages |
| Multi-Language Support | Professional | PRO | 5+ language support |
| Error Context Capture | Professional | LITE | Error metadata tracking |
| Error Persistence | Professional | PRO | Database storage (30-day retention) |
| Error History | Professional | PRO | Access to error history |
| Global Exception Handlers | Community | FREE | Basic handler pipeline |
| Advanced Handlers | Professional | PRO | Specialized exception handlers |
| Error Presentation UI | Professional | LITE | Basic UI components |
| Advanced UI Themes | Professional | PRO | Themed error dialogs |
| Error Analytics | Enterprise | ENTERPRISE | Analytics and reporting |
| SLA Monitoring | Enterprise | ENTERPRISE | Error SLA tracking |
| Custom Error Codes | Enterprise | ENTERPRISE | Custom error code creation |
| Error Recovery Automation | Enterprise | ENTERPRISE | Automated recovery mechanisms |
| Integration with Monitoring | Enterprise | ENTERPRISE | Sentry, DataDog, etc. |

---

## 11. PERFORMANCE TARGETS

### 11.1 Error Handling Performance

| Operation | Target | Constraint |
|-----------|--------|-----------|
| Error Code Lookup | < 1 ms | In-memory registry with cache |
| Error Context Capture | < 0.5 ms | Async where possible |
| Exception Handler Execution | < 1 ms | Optimized handler chain |
| User Message Retrieval | < 100 ms | Include localization |
| Error Persistence to DB | < 10 ms | Async with queue |
| Global Exception Handler | < 1 ms | Per exception |
| Total Error Response Time | < 50 ms | From exception to HTTP response |

### 11.2 Database Performance

| Operation | Target | Details |
|-----------|--------|---------|
| Error Insertion | < 50 ms | Bulk insert batching |
| Error Query (by code) | < 100 ms | 1M+ records |
| Error Query (by date range) | < 500 ms | Partitioned table |
| Analytics Materialization | < 5 sec | Nightly refresh |
| Error Cleanup (30-day retention) | < 1 min | Automated maintenance |

### 11.3 UI Performance

| Component | Target | Details |
|-----------|--------|---------|
| Dialog Render | < 100 ms | CSS animation included |
| Toast Notification | < 50 ms | Lightweight DOM |
| Error Details Expansion | < 200 ms | Lazy load stack trace |
| Error History Load | < 500 ms | First 50 records |
| Localization Switch | < 100 ms | Cached translations |

### 11.4 Memory Targets

| Resource | Target | Details |
|----------|--------|---------|
| Error Code Registry | < 10 MB | All 100+ codes |
| Handler Chain | < 1 MB | All handlers loaded |
| Exception Instance | < 50 KB | Including stack trace |
| Error Context | < 10 KB | Metadata only |

---

## 12. TESTING STRATEGY

### 12.1 Unit Testing

**Error Code Registry (v0.19.1a)**
- Test code validation (format, uniqueness)
- Test registry lookup (by code, category, severity)
- Test cache hit/miss scenarios
- Test concurrent access
- Test edge cases (null, empty, invalid)
- Coverage: 95%+

**Exception Hierarchy (v0.19.1b)**
- Test exception creation (all constructors)
- Test exception inheritance
- Test property assignment
- Test serialization/deserialization
- Test chaining methods (fluent API)
- Coverage: 100%

**User-Friendly Messages (v0.19.1c)**
- Test message retrieval
- Test variable substitution
- Test localization
- Test fallback to default language
- Test missing message handling
- Test HTML/plaintext formats
- Coverage: 95%+

**Error Context & Metadata (v0.19.1d)**
- Test context capture (all fields)
- Test metadata enrichment
- Test timestamp accuracy
- Test correlation ID tracking
- Test custom data storage
- Coverage: 90%+

**Global Exception Handlers (v0.19.1e)**
- Test handler chain execution
- Test handler prioritization
- Test context passing
- Test error transformation
- Test HTTP status code mapping
- Test response formatting
- Coverage: 95%+

**Error Presentation UI (v0.19.1f)**
- Test component rendering
- Test button click handlers
- Test message display
- Test auto-dismiss behavior
- Test accessibility (a11y)
- Test responsive design
- Coverage: 85%+

### 12.2 Integration Testing

- Full error pipeline from exception to response
- Database persistence and retrieval
- MediatR event publishing and handling
- Localization system end-to-end
- HTTP status code mapping validation
- Handler chain ordering

### 12.3 Performance Testing

- Error handling overhead < 1ms
- Database insert performance < 50ms
- UI rendering performance < 100ms
- Memory leak detection
- Concurrent error handling
- Cache hit/miss scenarios

### 12.4 User Acceptance Testing

- Error messages clarity and helpfulness
- UI usability and accessibility
- Localization quality
- Recovery suggestion effectiveness
- Error history and analytics accuracy

---

## 13. RISKS & MITIGATIONS

### 13.1 Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Error code collisions | Low | High | Format validation, uniqueness constraints, code review |
| Performance degradation in error handling | Medium | High | Performance testing, async operations, caching |
| Database performance with high volume | Medium | High | Partitioning, indexing, retention policies |
| Incorrect error messages | Medium | Medium | Translation review, testing, fallback messages |
| Missing error codes | Low | Medium | Code catalog review, automated validation |
| Unhandled exception types | Low | Critical | Generic exception handler, comprehensive testing |
| Localization coverage gaps | Medium | Low | Native speaker review, multiple language support |
| Memory leaks in exception handling | Low | High | Memory profiling, careful resource disposal |
| User confusion with error messages | Medium | Medium | User testing, A/B testing, feedback collection |
| Database connection during error | Low | Critical | Connection pooling, fallback logging, graceful degradation |

### 13.2 Mitigation Strategies

**Code Collisions:**
- Enforce LCS-{CATEGORY}-{NUMBER} format strictly
- Database uniqueness constraint
- Automated validation in CI/CD
- Code review checklist

**Performance:**
- In-memory caching of error codes
- Async error persistence
- Batch database inserts
- Index optimization
- Load testing before release

**Database:**
- Table partitioning by date
- Retention policies (30-day default)
- Automated cleanup jobs
- Connection pooling
- Read replicas for analytics

**Localization:**
- Native speaker review for each language
- Comprehensive test coverage
- Fallback to English
- Regular audit of translations

**Exception Handling:**
- Generic catch-all handler
- Comprehensive exception mapping
- Unit test all exception types
- Integration tests for handler chain

**User Communication:**
- A/B testing of error messages
- User feedback mechanism
- Regular improvement cycles
- Analytics on error message clarity

---

## 14. MEDIATOR EVENTS

### 14.1 ErrorOccurredEvent

```csharp
/// <summary>
/// Published when an error occurs in the system.
/// Allows subscribers to react to errors (logging, alerting, etc.)
/// </summary>
public class ErrorOccurredEvent : INotification
{
    /// <summary>
    /// The error code for this event.
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// The exception that occurred.
    /// </summary>
    public LexichordException Exception { get; set; }

    /// <summary>
    /// The error context with metadata.
    /// </summary>
    public IErrorContext Context { get; set; }

    /// <summary>
    /// User-friendly message for the error.
    /// </summary>
    public string UserMessage { get; set; }

    /// <summary>
    /// Error severity level.
    /// </summary>
    public ErrorSeverity Severity { get; set; }

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Request ID for correlation.
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// Additional custom data associated with the error.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; }
}

/// <summary>
/// Handler for ErrorOccurredEvent - logs errors to database.
/// </summary>
public class ErrorOccurredEventHandler : INotificationHandler<ErrorOccurredEvent>
{
    private readonly IErrorOccurrenceService _errorService;
    private readonly ILogger<ErrorOccurredEventHandler> _logger;

    public ErrorOccurredEventHandler(
        IErrorOccurrenceService errorService,
        ILogger<ErrorOccurredEventHandler> logger)
    {
        _errorService = errorService;
        _logger = logger;
    }

    public async Task Handle(ErrorOccurredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Persist error occurrence to database
            await _errorService.RecordErrorAsync(notification, cancellationToken);

            // Log to application logs
            _logger.LogError(
                "Error occurred: {ErrorCode} - {Message}",
                notification.ErrorCode,
                notification.UserMessage);

            // Check for critical errors and alert
            if (notification.Severity == ErrorSeverity.Critical)
            {
                // Trigger alert notification
                _logger.LogCritical(
                    "Critical error: {ErrorCode}",
                    notification.ErrorCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling ErrorOccurredEvent");
        }
    }
}
```

### 14.2 ErrorHandledEvent

```csharp
/// <summary>
/// Published when an error has been handled by the exception handler.
/// </summary>
public class ErrorHandledEvent : INotification
{
    /// <summary>
    /// The error code.
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// The exception that was handled.
    /// </summary>
    public LexichordException Exception { get; set; }

    /// <summary>
    /// The handler that processed the error.
    /// </summary>
    public string HandlerName { get; set; }

    /// <summary>
    /// HTTP status code that will be returned.
    /// </summary>
    public int HttpStatusCode { get; set; }

    /// <summary>
    /// User-friendly message.
    /// </summary>
    public string UserMessage { get; set; }

    /// <summary>
    /// Time taken to handle the error (milliseconds).
    /// </summary>
    public long HandlingTimeMs { get; set; }

    /// <summary>
    /// Timestamp when handling completed.
    /// </summary>
    public DateTime HandledAt { get; set; }

    /// <summary>
    /// Request ID for correlation.
    /// </summary>
    public string CorrelationId { get; set; }
}

/// <summary>
/// Handler for ErrorHandledEvent - publishes metrics.
/// </summary>
public class ErrorHandledEventHandler : INotificationHandler<ErrorHandledEvent>
{
    private readonly IMetricsService _metricsService;

    public ErrorHandledEventHandler(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public async Task Handle(ErrorHandledEvent notification, CancellationToken cancellationToken)
    {
        // Record error handling metrics
        _metricsService.RecordErrorHandled(
            notification.ErrorCode,
            notification.HandlingTimeMs,
            notification.HttpStatusCode);

        // Update error statistics
        await _metricsService.UpdateErrorCountAsync(
            notification.ErrorCode,
            notification.HandledAt);
    }
}
```

### 14.3 ErrorDismissedEvent

```csharp
/// <summary>
/// Published when a user dismisses an error message.
/// </summary>
public class ErrorDismissedEvent : INotification
{
    /// <summary>
    /// The error code that was dismissed.
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// How the user dismissed the error.
    /// </summary>
    public ErrorDismissalAction Action { get; set; }

    /// <summary>
    /// Time the error was visible (milliseconds).
    /// </summary>
    public long VisibleDurationMs { get; set; }

    /// <summary>
    /// User ID if available.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Request ID for correlation.
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    /// Timestamp when dismissed.
    /// </summary>
    public DateTime DismissedAt { get; set; }
}

/// <summary>
/// User's action when dismissing error.
/// </summary>
public enum ErrorDismissalAction
{
    /// <summary>User clicked dismiss/close button</summary>
    Dismissed = 0,

    /// <summary>User clicked retry button</summary>
    Retried = 1,

    /// <summary>User clicked cancel/cancel button</summary>
    Cancelled = 2,

    /// <summary>User viewed details</summary>
    ViewedDetails = 3,

    /// <summary>User contacted support</summary>
    ContactedSupport = 4,

    /// <summary>Toast auto-dismissed after timeout</summary>
    AutoDismissed = 5
}

/// <summary>
/// Handler for ErrorDismissedEvent - tracks user interaction.
/// </summary>
public class ErrorDismissedEventHandler : INotificationHandler<ErrorDismissedEvent>
{
    private readonly IAnalyticsService _analyticsService;

    public ErrorDismissedEventHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task Handle(ErrorDismissedEvent notification, CancellationToken cancellationToken)
    {
        // Track user interaction with errors
        await _analyticsService.TrackErrorInteractionAsync(
            notification.ErrorCode,
            notification.Action,
            notification.VisibleDurationMs,
            notification.UserId);
    }
}

/// <summary>
/// Handler for error recovery when user clicks retry.
/// </summary>
public class ErrorRetryHandler : INotificationHandler<ErrorDismissedEvent>
{
    private readonly IRetryPolicyService _retryService;

    public ErrorRetryHandler(IRetryPolicyService retryService)
    {
        _retryService = retryService;
    }

    public async Task Handle(ErrorDismissedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Action == ErrorDismissalAction.Retried)
        {
            // Initiate retry with exponential backoff
            await _retryService.RetryOperationAsync(
                notification.CorrelationId,
                notification.ErrorCode);
        }
    }
}
```

### 14.4 Event Registration in DI Container

```csharp
public static class ErrorFrameworkExtensions
{
    public static IServiceCollection AddErrorFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register error code registry
        services.AddSingleton<IErrorCodeRegistry, ErrorCodeRegistry>();

        // Register error context factory
        services.AddScoped<IErrorContext, ErrorContext>();

        // Register localization service
        services.AddScoped<IErrorLocalizationService, ErrorLocalizationService>();

        // Register global exception handler
        services.AddScoped<IGlobalExceptionHandler, GlobalExceptionHandler>();

        // Register exception handlers
        services.AddScoped<IExceptionHandler, ValidationExceptionHandler>();
        services.AddScoped<IExceptionHandler, DatabaseExceptionHandler>();
        services.AddScoped<IExceptionHandler, AuthenticationExceptionHandler>();
        services.AddScoped<IExceptionHandler, AuthorizationExceptionHandler>();
        services.AddScoped<IExceptionHandler, ResourceNotFoundExceptionHandler>();
        services.AddScoped<IExceptionHandler, ConflictExceptionHandler>();
        services.AddScoped<IExceptionHandler, TimeoutExceptionHandler>();
        services.AddScoped<IExceptionHandler, ExternalServiceExceptionHandler>();
        services.AddScoped<IExceptionHandler, GenericExceptionHandler>();

        // Register error presenters
        services.AddScoped<IErrorPresenter, ErrorPresenter>();

        // Register error occurrence service
        services.AddScoped<IErrorOccurrenceService, ErrorOccurrenceService>();

        // Register MediatR notification handlers
        services.AddScoped<INotificationHandler<ErrorOccurredEvent>, ErrorOccurredEventHandler>();
        services.AddScoped<INotificationHandler<ErrorHandledEvent>, ErrorHandledEventHandler>();
        services.AddScoped<INotificationHandler<ErrorDismissedEvent>, ErrorDismissedEventHandler>();
        services.AddScoped<INotificationHandler<ErrorDismissedEvent>, ErrorRetryHandler>();

        return services;
    }
}
```

---

## 15. IMPLEMENTATION ROADMAP

### Phase 1: Foundation (Week 1)
- v0.19.1a: Error Code Registry (10 hours)
- v0.19.1b: Exception Hierarchy (10 hours)
- Database schema creation
- Base infrastructure setup

### Phase 2: Functionality (Week 2)
- v0.19.1c: User-Friendly Messages (8 hours)
- v0.19.1d: Error Context & Metadata (8 hours)
- Localization system setup
- MediatR integration

### Phase 3: Integration (Week 3)
- v0.19.1e: Global Exception Handlers (8 hours)
- ASP.NET Core middleware integration
- Handler chain testing

### Phase 4: UI & Polish (Week 4)
- v0.19.1f: Error Presentation UI (8 hours)
- Component styling and accessibility
- Responsive design verification
- User acceptance testing

### Week 5: Testing & Release
- Comprehensive testing (unit, integration, performance)
- Documentation completion
- Release preparation
- Deployment

---

## 16. ACCEPTANCE CRITERIA SUMMARY

### Functional Requirements Met

- [ ] Error code registry with 100+ codes defined and registered
- [ ] Exception hierarchy with 15+ exception types implemented
- [ ] User-friendly messages for all error codes (5+ languages)
- [ ] Error context capture with all required metadata
- [ ] Global exception handler catching all unhandled exceptions
- [ ] Error presentation UI with dialog, toast, and details components
- [ ] PostgreSQL schema for error tracking
- [ ] MediatR events for error lifecycle management

### Non-Functional Requirements Met

- [ ] Error handling overhead < 1ms per operation
- [ ] Database operations < 50ms (insert), < 100ms (query)
- [ ] UI rendering < 100ms
- [ ] Error code lookup < 1ms (with caching)
- [ ] Message retrieval < 100ms (with localization)
- [ ] 95%+ code coverage for core components
- [ ] All exceptions handled gracefully
- [ ] Zero data loss in error logging

### Documentation Requirements Met

- [ ] All interfaces documented with XML comments
- [ ] Error code catalog with 100+ codes
- [ ] Architecture diagrams showing exception flow
- [ ] UI mockups for all error presentation modes
- [ ] Database schema with comprehensive comments
- [ ] Integration guide for developers
- [ ] User guide for end-users
- [ ] Operations guide for DevOps team

---

## APPENDIX A: GLOSSARY

| Term | Definition |
|------|-----------|
| Error Code | Standardized identifier (LCS-CATEGORY-NUMBER) |
| Error Context | Metadata about error occurrence (request ID, user, timestamp, etc.) |
| Error Handler | Component that processes exceptions and produces responses |
| Exception Hierarchy | Organized structure of exception types |
| Localization | Translation of messages to different languages |
| Correlation ID | Unique ID to track related events across systems |
| HTTP Status Code | Numeric code indicating HTTP response status |
| Severity Level | Error importance (Critical, Error, Warning, Info) |
| User Message | Human-friendly error message shown to end users |
| Recovery Suggestion | Actionable guidance for users to resolve error |
| Stack Trace | Sequence of function calls when exception occurred |
| Circuit Breaker | Pattern to prevent cascading failures |
| Retryable | Error condition where operation may succeed on retry |
| Serialization | Converting objects to storable/transmissible format |

---

**Document End**

**Total Line Count: 2,847 lines**

---

