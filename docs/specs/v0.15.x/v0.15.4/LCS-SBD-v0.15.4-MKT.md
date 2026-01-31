# LCS-SBD-0154-MKT: Scope Breakdown — Integration Hub

## Document Control

| Field            | Value                                                      |
| :--------------- | :--------------------------------------------------------- |
| **Document ID**  | LCS-SBD-0154-MKT                                           |
| **Version**      | v0.15.4                                                    |
| **Codename**     | Integration Hub (MKT)                                      |
| **Status**       | Draft                                                      |
| **Last Updated** | 2026-01-31                                                 |
| **Owner**        | Lead Architect                                             |
| **Depends On**   | v0.15.1-MKT (Plugin Architecture), v0.11.1-SEC (Auth), v0.11.3-SEC (Encryption) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.15.4-MKT** delivers the **Integration Hub** — a comprehensive framework for building connectors to external services with enterprise-grade OAuth, webhooks, and API management. This release transforms Lexichord from an isolated application into a connected ecosystem that seamlessly integrates with GitHub, GitLab, Jira, Linear, Confluence, Notion, Slack, OpenAI, Anthropic, and custom enterprise connectors.

This enables:
- "Connect to GitHub" → OAuth flow → list issues, manage repos
- "Slack notifications" → webhook registration → real-time team updates
- "Custom CRM connector" → connector framework → build in-house integrations
- API gateway with rate limiting, request transformation, credential security

### 1.2 Business Value

- **Ecosystem Connectivity:** Direct integrations with 9 popular platforms + unlimited custom connectors.
- **Enterprise Ready:** OAuth 2.0, webhook validation, credential encryption, audit logging.
- **Developer Experience:** Connector SDK framework reduces custom connector development from weeks to days.
- **Revenue Unlock:** Unlimited integrations at Enterprise tier, API gateway access, custom connector development.
- **Workflow Automation:** Triggers and actions enable powerful cross-platform automation.
- **Security:** Credential vault, encrypted storage, token refresh, audit trails.

### 1.3 Success Criteria

1. 9 built-in connectors functional with OAuth, actions, and triggers.
2. Custom connector development achievable in <8 hours with SDK.
3. OAuth flows complete in <3 seconds (interactive).
4. Webhook registration and validation within <500ms.
5. Credential vault stores/retrieves secrets with AES-256 encryption.
6. API gateway routes 1000+ requests/second with <100ms latency P95.
7. 100% webhook delivery reliability with retry logic.
8. Support for 5+ concurrent OAuth flows per user.

---

## 2. Relationship to Existing v0.15

The existing v0.15 spec covers the Plugin Architecture (v0.15.1-MKT) which provides the foundation for loading and managing external services. The Integration Hub builds on this by:

- **Connector Protocol:** Implements plugin interface for service connectors.
- **Configuration Storage:** Extends settings service for connector credentials.
- **Event System:** Uses MediatR for integration events (triggers, actions).
- **Enhanced Search:** Integrations can index remote content into local search.

---

## 3. Key Deliverables

### 3.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.15.4e | Connector Framework | IConnector protocol, manifest system, action/trigger pipeline | 12 |
| v0.15.4f | OAuth Manager | OAuth 2.0 flow implementation, token management, refresh logic | 10 |
| v0.15.4g | Webhook System | Webhook registration, validation, delivery with retries | 10 |
| v0.15.4h | API Gateway | Request routing, rate limiting, transformation, monitoring | 10 |
| v0.15.4i | Credential Vault | Encrypted secret storage, rotation, audit logging | 4 |
| v0.15.4j | Integration Manager UI | Connection UI, dashboard, trigger/action builder | 4 |
| **Total** | | | **50 hours** |

### 3.2 Key Interfaces

```csharp
/// <summary>
/// Base interface for all service connectors.
/// </summary>
public interface IConnector
{
    /// <summary>
    /// Connector metadata and configuration.
    /// </summary>
    ConnectorManifest Manifest { get; }

    /// <summary>
    /// Initialize connector with configuration.
    /// </summary>
    Task InitializeAsync(ConnectorConfig config, CancellationToken ct = default);

    /// <summary>
    /// Test connectivity to external service.
    /// </summary>
    Task<ConnectorHealth> HealthCheckAsync(CancellationToken ct = default);

    /// <summary>
    /// Execute a connector action (e.g., create issue, post message).
    /// </summary>
    Task<ActionResult> ExecuteActionAsync(
        string actionId,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default);

    /// <summary>
    /// List available actions this connector can perform.
    /// </summary>
    IReadOnlyList<ConnectorAction> GetActions();

    /// <summary>
    /// List available triggers this connector can emit.
    /// </summary>
    IReadOnlyList<ConnectorTrigger> GetTriggers();
}

/// <summary>
/// Connector manifest with metadata and capabilities.
/// </summary>
public record ConnectorManifest
{
    /// <summary>Unique connector ID (e.g., 'github', 'slack').</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Connector category.</summary>
    public required ConnectorCategory Category { get; init; }

    /// <summary>Short description.</summary>
    public required string Description { get; init; }

    /// <summary>Logo URL or base64 data URI.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>Documentation URL.</summary>
    public string? DocsUrl { get; init; }

    /// <summary>Authentication method required.</summary>
    public required AuthenticationMethod AuthMethod { get; init; }

    /// <summary>OAuth configuration (if AuthMethod.OAuth).</summary>
    public OAuthConfig? OAuthConfig { get; init; }

    /// <summary>Supported actions (built-in).</summary>
    public IReadOnlyList<ConnectorAction> Actions { get; init; } = [];

    /// <summary>Supported triggers (built-in).</summary>
    public IReadOnlyList<ConnectorTrigger> Triggers { get; init; } = [];

    /// <summary>Connector version (semantic versioning).</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Minimum Lexichord version required.</summary>
    public string MinLexichordVersion { get; init; } = "0.15.4";
}

/// <summary>
/// Connector category classification.
/// </summary>
public enum ConnectorCategory
{
    Communication,      // Slack, Teams, Discord
    ProjectManagement,  // Jira, Linear, Azure DevOps
    Documentation,      // Confluence, Notion, Wiki
    SourceControl,      // GitHub, GitLab, Bitbucket
    AI,                 // OpenAI, Anthropic, Hugging Face
    CRM,                // Salesforce, HubSpot
    Analytics,          // Google Analytics, Mixpanel
    Database,           // MongoDB, PostgreSQL, Firebase
    Webhooks,           // Generic webhook receiver
    Custom              // User-defined integrations
}

/// <summary>
/// Authentication method for connector.
/// </summary>
public enum AuthenticationMethod
{
    None,               // No authentication
    ApiKey,             // Static API key
    BasicAuth,          // Username + password
    Bearer,             // Bearer token
    OAuth,              // OAuth 2.0 flow
    Custom              // Custom auth scheme
}

/// <summary>
/// OAuth 2.0 configuration.
/// </summary>
public record OAuthConfig
{
    /// <summary>Authorization endpoint URL.</summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>Token endpoint URL.</summary>
    public required string TokenUrl { get; init; }

    /// <summary>OAuth client ID (from service provider).</summary>
    public required string ClientId { get; init; }

    /// <summary>OAuth client secret (encrypted in vault).</summary>
    public required string ClientSecret { get; init; }

    /// <summary>Redirect URI (typically configured at service provider).</summary>
    public required string RedirectUri { get; init; }

    /// <summary>Scopes required (space-separated).</summary>
    public required string Scopes { get; init; }

    /// <summary>Whether to use PKCE flow (recommended).</summary>
    public bool UsePkce { get; init; } = true;

    /// <summary>Token refresh strategy.</summary>
    public OAuthTokenRefreshStrategy RefreshStrategy { get; init; } =
        OAuthTokenRefreshStrategy.Automatic;
}

/// <summary>
/// An action a connector can perform.
/// </summary>
public record ConnectorAction
{
    /// <summary>Unique action ID within connector.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Detailed description.</summary>
    public required string Description { get; init; }

    /// <summary>Input parameters.</summary>
    public IReadOnlyList<ActionParameter> Parameters { get; init; } = [];

    /// <summary>Output schema (JSON schema).</summary>
    public string? OutputSchema { get; init; }

    /// <summary>Required scopes for this action.</summary>
    public IReadOnlyList<string> RequiredScopes { get; init; } = [];
}

/// <summary>
/// A trigger a connector can emit.
/// </summary>
public record ConnectorTrigger
{
    /// <summary>Unique trigger ID within connector.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Trigger event type.</summary>
    public required TriggerType Type { get; init; }

    /// <summary>Description of when this trigger fires.</summary>
    public required string Description { get; init; }

    /// <summary>Webhook-based (vs polling).</summary>
    public bool IsWebhookBased { get; init; } = true;

    /// <summary>Output payload schema (JSON schema).</summary>
    public string? PayloadSchema { get; init; }
}

/// <summary>
/// Trigger type classification.
/// </summary>
public enum TriggerType
{
    Created,            // Item created
    Updated,            // Item modified
    Deleted,            // Item removed
    StatusChanged,      // Status/state transition
    Commented,          // Comment/note added
    Assigned,           // Assignment changed
    Custom              // Custom trigger
}

/// <summary>
/// OAuth flow manager.
/// </summary>
public interface IOAuthManager
{
    /// <summary>
    /// Start OAuth flow, returns authorization URL for user.
    /// </summary>
    Task<OAuthFlow> StartFlowAsync(
        string connectorId,
        OAuthFlowOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exchange authorization code for access token.
    /// </summary>
    Task<OAuthToken> ExchangeCodeAsync(
        Guid flowId,
        string authorizationCode,
        CancellationToken ct = default);

    /// <summary>
    /// Refresh an expired OAuth token.
    /// </summary>
    Task<OAuthToken> RefreshTokenAsync(
        Guid connectionId,
        CancellationToken ct = default);

    /// <summary>
    /// Revoke a token and remove connection.
    /// </summary>
    Task RevokeTokenAsync(Guid connectionId, CancellationToken ct = default);
}

/// <summary>
/// Active OAuth flow tracking.
/// </summary>
public record OAuthFlow
{
    /// <summary>Flow session ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Authorization URL user should visit.</summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>PKCE code verifier (if using PKCE).</summary>
    public string? CodeVerifier { get; init; }

    /// <summary>State token for CSRF protection.</summary>
    public required string StateToken { get; init; }

    /// <summary>When flow expires (default 10 minutes).</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>Connector being authenticated.</summary>
    public required string ConnectorId { get; init; }
}

/// <summary>
/// OAuth token with metadata.
/// </summary>
public record OAuthToken
{
    /// <summary>Access token (encrypted at rest).</summary>
    public required string AccessToken { get; init; }

    /// <summary>Token type (typically 'Bearer').</summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>Refresh token (if available).</summary>
    public string? RefreshToken { get; init; }

    /// <summary>Expiration timestamp.</summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Granted scopes.</summary>
    public IReadOnlyList<string> GrantedScopes { get; init; } = [];
}

/// <summary>
/// OAuth flow customization options.
/// </summary>
public record OAuthFlowOptions
{
    /// <summary>Requested scopes (space-separated).</summary>
    public string? Scopes { get; init; }

    /// <summary>Additional parameters to include in auth URL.</summary>
    public IReadOnlyDictionary<string, string>? AdditionalParameters { get; init; }
}

/// <summary>
/// Webhook manager.
/// </summary>
public interface IWebhookManager
{
    /// <summary>
    /// Register webhook with external service.
    /// </summary>
    Task<WebhookRegistration> RegisterWebhookAsync(
        Guid connectionId,
        WebhookDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Unregister webhook from external service.
    /// </summary>
    Task UnregisterWebhookAsync(Guid registrationId, CancellationToken ct = default);

    /// <summary>
    /// Handle incoming webhook payload.
    /// </summary>
    Task HandleWebhookAsync(
        string connectorId,
        WebhookPayload payload,
        CancellationToken ct = default);

    /// <summary>
    /// List registered webhooks for connection.
    /// </summary>
    Task<IReadOnlyList<WebhookRegistration>> ListWebhooksAsync(
        Guid connectionId,
        CancellationToken ct = default);
}

/// <summary>
/// Webhook registration at external service.
/// </summary>
public record WebhookRegistration
{
    /// <summary>Unique registration ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>External service webhook ID.</summary>
    public required string ExternalWebhookId { get; init; }

    /// <summary>Connection this webhook belongs to.</summary>
    public required Guid ConnectionId { get; init; }

    /// <summary>Webhook definition.</summary>
    public required WebhookDefinition Definition { get; init; }

    /// <summary>Webhook URL (Lexichord endpoint).</summary>
    public required string WebhookUrl { get; init; }

    /// <summary>Secret for validating webhook signatures.</summary>
    public required string Secret { get; init; }

    /// <summary>When registered.</summary>
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

    /// <summary>Last webhook received.</summary>
    public DateTime? LastFiredAt { get; init; }

    /// <summary>Status of webhook.</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Webhook definition and trigger mapping.
/// </summary>
public record WebhookDefinition
{
    /// <summary>Trigger ID from connector.</summary>
    public required string TriggerId { get; init; }

    /// <summary>Events to watch for (service-specific).</summary>
    public required IReadOnlyList<string> Events { get; init; }

    /// <summary>Optional filter (JSON query).</summary>
    public string? FilterQuery { get; init; }
}

/// <summary>
/// Incoming webhook payload.
/// </summary>
public record WebhookPayload
{
    /// <summary>Raw webhook body.</summary>
    public required string Body { get; init; }

    /// <summary>X-Webhook-Signature header value.</summary>
    public required string Signature { get; init; }

    /// <summary>Webhook headers.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>
/// Manages active integrations and connections.
/// </summary>
public interface IIntegrationManager
{
    /// <summary>
    /// Create new connection to external service.
    /// </summary>
    Task<ActiveConnection> CreateConnectionAsync(
        string connectorId,
        string connectionName,
        CancellationToken ct = default);

    /// <summary>
    /// Get active connection details.
    /// </summary>
    Task<ActiveConnection?> GetConnectionAsync(Guid connectionId, CancellationToken ct = default);

    /// <summary>
    /// List all connections for user.
    /// </summary>
    Task<IReadOnlyList<ActiveConnection>> ListConnectionsAsync(
        ConnectorCategory? category = null,
        CancellationToken ct = default);

    /// <summary>
    /// Test connection health.
    /// </summary>
    Task<ConnectionHealth> TestConnectionAsync(Guid connectionId, CancellationToken ct = default);

    /// <summary>
    /// Disconnect and remove integration.
    /// </summary>
    Task DisconnectAsync(Guid connectionId, CancellationToken ct = default);

    /// <summary>
    /// Check connection limit based on license.
    /// </summary>
    Task<int> GetRemainingConnectionsAsync(CancellationToken ct = default);
}

/// <summary>
/// Active connection to external service.
/// </summary>
public record ActiveConnection
{
    /// <summary>Connection ID (foreign key).</summary>
    public Guid Id { get; init; }

    /// <summary>Connector ID (e.g., 'github').</summary>
    public required string ConnectorId { get; init; }

    /// <summary>User-friendly name.</summary>
    public required string Name { get; init; }

    /// <summary>Connection status.</summary>
    public required ConnectionStatus Status { get; init; }

    /// <summary>Account identifier at external service.</summary>
    public required string ExternalAccountId { get; init; }

    /// <summary>Account display name/email.</summary>
    public string? ExternalAccountName { get; init; }

    /// <summary>When connected.</summary>
    public DateTime ConnectedAt { get; init; }

    /// <summary>Last health check.</summary>
    public DateTime? LastHealthCheckAt { get; init; }

    /// <summary>Number of active actions/triggers.</summary>
    public int ActiveTriggersCount { get; init; }

    /// <summary>Metadata (JSON-serialized).</summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// Connection lifecycle status.
/// </summary>
public enum ConnectionStatus
{
    Initializing,       // OAuth flow in progress
    Active,             // Connected and operational
    TokenExpired,       // Needs token refresh
    TokenInvalid,       // Permanent failure
    Disconnected,       // User-initiated disconnect
    Error               // Transient error
}

/// <summary>
/// Connection health status.
/// </summary>
public record ConnectionHealth
{
    /// <summary>Is connection currently healthy.</summary>
    public required bool IsHealthy { get; init; }

    /// <summary>Status message.</summary>
    public string? Message { get; init; }

    /// <summary>Health check latency (ms).</summary>
    public long LatencyMs { get; init; }

    /// <summary>Account info at service.</summary>
    public IReadOnlyDictionary<string, string>? AccountInfo { get; init; }

    /// <summary>When checked.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// API Gateway for routing and transforming requests.
/// </summary>
public interface IApiGateway
{
    /// <summary>
    /// Route request to connector API.
    /// </summary>
    Task<GatewayResponse> RouteRequestAsync(
        Guid connectionId,
        GatewayRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get rate limit status for connection.
    /// </summary>
    Task<RateLimitStatus> GetRateLimitStatusAsync(Guid connectionId, CancellationToken ct = default);
}

/// <summary>
/// Credential vault for encrypted storage.
/// </summary>
public interface ICredentialVault
{
    /// <summary>
    /// Store encrypted credential.
    /// </summary>
    Task StoreCredentialAsync(
        Guid connectionId,
        string credentialKey,
        string value,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieve and decrypt credential.
    /// </summary>
    Task<string?> GetCredentialAsync(
        Guid connectionId,
        string credentialKey,
        CancellationToken ct = default);

    /// <summary>
    /// Delete credential.
    /// </summary>
    Task DeleteCredentialAsync(
        Guid connectionId,
        string credentialKey,
        CancellationToken ct = default);
}
```

---

## 4. Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          INTEGRATION HUB (v0.15.4)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      Integration Manager UI                          │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐        │  │
│  │  │Connection│ │ Trigger/ │ │  Health  │ │  Credential      │        │  │
│  │  │  Panel   │ │  Action  │ │  Status  │ │  Settings        │        │  │
│  │  │          │ │ Builder  │ │          │ │                  │        │  │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────────┘        │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    ▲                                        │
│                                    │                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Integration Manager (v0.15.4j)                    │  │
│  │  ┌────────────────────────────────────────────────────────────────┐  │  │
│  │  │ - Connection lifecycle management                             │  │  │
│  │  │ - License gating (connection limits)                          │  │  │
│  │  │ - Action/trigger registry                                    │  │  │
│  │  └────────────────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                   ▲           ▲              ▲           ▲                 │
│                   │           │              │           │                 │
│  ┌────────────────┴───┐  ┌───┴──────────┐  ┌┴───────┐  ┌┴─────────────┐  │
│  │   Connector        │  │  OAuth       │  │Webhook │  │  API         │  │
│  │   Framework        │  │  Manager     │  │System  │  │  Gateway     │  │
│  │ (v0.15.4e)        │  │ (v0.15.4f)   │  │(v0.15. │  │ (v0.15.4h)   │  │
│  │                    │  │              │  │4g)     │  │              │  │
│  │ ┌────────────────┐ │  │ ┌──────────┐ │  │┌──────┐│  │┌────────────┐│  │
│  │ │IConnector      │ │  │ │OAuth Flow│ │  ││Webhook││  ││Rate Limit  ││  │
│  │ │Protocol        │ │  │ │Handler   │ │  ││Mgr    ││  ││Enforcement ││  │
│  │ ├────────────────┤ │  │ ├──────────┤ │  │├──────┤│  │├────────────┤│  │
│  │ │Actions/        │ │  │ │Token     │ │  ││Webhook││  ││Request     ││  │
│  │ │Triggers        │ │  │ │Refresh   │ │  ││Event  ││  ││Transform   ││  │
│  │ │Registry        │ │  │ │          │ │  ││Queue  ││  ││            ││  │
│  │ └────────────────┘ │  │ └──────────┘ │  │└──────┘│  │└────────────┘│  │
│  └────────────────────┘  └──────────────┘  └────────┘  └──────────────┘  │
│           ▲                     ▲                ▲             ▲            │
│           │                     │                │             │            │
│  ┌────────┴──────────────────────┴────────────────┴─────────────┴──────┐  │
│  │              Credential Vault (v0.15.4i)                           │  │
│  │  ┌──────────────────────────────────────────────────────────────┐  │  │
│  │  │ - AES-256 Encryption at rest                                 │  │  │
│  │  │ - Token storage and rotation                                 │  │  │
│  │  │ - Audit logging                                              │  │  │
│  │  │ - Integration with v0.11.3-SEC (Encryption)                  │  │  │
│  │  └──────────────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                         EXTERNAL SERVICES                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  GitHub    GitLab    Jira    Linear    Confluence    Notion                │
│    │         │        │        │          │           │                    │
│    └─────────┴────────┴────────┴──────────┴───────────┘                    │
│                    (Built-in Connectors)                                    │
│                                                                             │
│    Slack     OpenAI    Anthropic    + Custom Connectors                    │
│      │         │          │               │                                │
│      └─────────┴──────────┴───────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. PostgreSQL Schema

```sql
-- Active connections to external services
CREATE TABLE integrations_connections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    connector_id TEXT NOT NULL,          -- 'github', 'slack', etc.
    name TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'active',  -- 'active', 'error', 'token_expired'
    external_account_id TEXT NOT NULL,
    external_account_name TEXT,
    connected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_health_check_at TIMESTAMPTZ,
    last_health_check_status BOOLEAN,
    active_triggers_count INT DEFAULT 0,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_user FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_connector_name UNIQUE(user_id, connector_id, name)
);

CREATE INDEX idx_connections_user_id ON integrations_connections(user_id);
CREATE INDEX idx_connections_status ON integrations_connections(status);
CREATE INDEX idx_connections_connector_id ON integrations_connections(connector_id);

-- OAuth tokens (encrypted at rest)
CREATE TABLE integrations_oauth_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id UUID NOT NULL,
    access_token_encrypted BYTEA NOT NULL,       -- Encrypted with AES-256
    access_token_iv BYTEA NOT NULL,              -- IV for encryption
    token_type TEXT DEFAULT 'Bearer',
    refresh_token_encrypted BYTEA,
    refresh_token_iv BYTEA,
    expires_at TIMESTAMPTZ,
    granted_scopes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_connection FOREIGN KEY(connection_id)
        REFERENCES integrations_connections(id) ON DELETE CASCADE,
    CONSTRAINT uq_connection_token UNIQUE(connection_id)
);

CREATE INDEX idx_oauth_tokens_expires_at ON integrations_oauth_tokens(expires_at);

-- Webhook registrations
CREATE TABLE integrations_webhooks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id UUID NOT NULL,
    external_webhook_id TEXT NOT NULL,
    trigger_id TEXT NOT NULL,
    events TEXT[] NOT NULL,                  -- Array of event types
    filter_query JSONB,
    webhook_url TEXT NOT NULL,
    secret_encrypted BYTEA NOT NULL,
    secret_iv BYTEA NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_fired_at TIMESTAMPTZ,
    delivery_count INT DEFAULT 0,
    failure_count INT DEFAULT 0,
    last_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_connection FOREIGN KEY(connection_id)
        REFERENCES integrations_connections(id) ON DELETE CASCADE,
    CONSTRAINT uq_external_webhook UNIQUE(connection_id, external_webhook_id)
);

CREATE INDEX idx_webhooks_connection_id ON integrations_webhooks(connection_id);
CREATE INDEX idx_webhooks_active ON integrations_webhooks(is_active);
CREATE INDEX idx_webhooks_last_fired ON integrations_webhooks(last_fired_at);

-- Webhook delivery log (for retry and audit)
CREATE TABLE integrations_webhook_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    webhook_id UUID NOT NULL,
    payload JSONB NOT NULL,
    delivery_attempt INT DEFAULT 1,
    http_status_code INT,
    error_message TEXT,
    delivered_at TIMESTAMPTZ,
    next_retry_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_webhook FOREIGN KEY(webhook_id)
        REFERENCES integrations_webhooks(id) ON DELETE CASCADE
);

CREATE INDEX idx_deliveries_webhook_id ON integrations_webhook_deliveries(webhook_id);
CREATE INDEX idx_deliveries_next_retry ON integrations_webhook_deliveries(next_retry_at);
CREATE INDEX idx_deliveries_created ON integrations_webhook_deliveries(created_at DESC);

-- API gateway request log
CREATE TABLE integrations_api_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id UUID NOT NULL,
    method TEXT NOT NULL,
    endpoint TEXT NOT NULL,
    status_code INT,
    response_time_ms INT,
    rate_limit_remaining INT,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_connection FOREIGN KEY(connection_id)
        REFERENCES integrations_connections(id) ON DELETE CASCADE
);

CREATE INDEX idx_api_requests_connection_id ON integrations_api_requests(connection_id);
CREATE INDEX idx_api_requests_created ON integrations_api_requests(created_at DESC);
CREATE INDEX idx_api_requests_status ON integrations_api_requests(status_code);

-- Credential vault
CREATE TABLE integrations_credentials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id UUID NOT NULL,
    credential_key TEXT NOT NULL,
    value_encrypted BYTEA NOT NULL,         -- Encrypted with AES-256
    value_iv BYTEA NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_connection FOREIGN KEY(connection_id)
        REFERENCES integrations_connections(id) ON DELETE CASCADE,
    CONSTRAINT uq_credential UNIQUE(connection_id, credential_key)
);

-- Audit log for integration activity
CREATE TABLE integrations_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id UUID NOT NULL,
    event_type TEXT NOT NULL,               -- 'connect', 'disconnect', 'action_exec', etc.
    action_id TEXT,
    status TEXT,                            -- 'success', 'failure'
    details JSONB,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_connection FOREIGN KEY(connection_id)
        REFERENCES integrations_connections(id) ON DELETE CASCADE
);

CREATE INDEX idx_audit_connection_id ON integrations_audit_log(connection_id);
CREATE INDEX idx_audit_event_type ON integrations_audit_log(event_type);
CREATE INDEX idx_audit_created ON integrations_audit_log(created_at DESC);

-- Rate limit tracking per connection
CREATE TABLE integrations_rate_limits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    connection_id UUID NOT NULL,
    api_name TEXT NOT NULL,
    requests_remaining INT,
    requests_limit INT,
    reset_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_connection FOREIGN KEY(connection_id)
        REFERENCES integrations_connections(id) ON DELETE CASCADE,
    CONSTRAINT uq_rate_limit UNIQUE(connection_id, api_name)
);

COMMENT ON TABLE integrations_connections IS 'Tracks active integrations';
COMMENT ON TABLE integrations_oauth_tokens IS 'Encrypted OAuth tokens';
COMMENT ON TABLE integrations_webhooks IS 'Registered webhooks with delivery tracking';
COMMENT ON TABLE integrations_credentials IS 'Encrypted credential storage';
COMMENT ON TABLE integrations_audit_log IS 'Integration activity audit trail';
```

---

## 6. Built-in Connectors

### Supported Connectors

| Connector | Category | Auth | Actions | Triggers | Status |
|:----------|:---------|:-----|:--------|:---------|:-------|
| GitHub | Source Control | OAuth | Create issue, add comment, merge PR, etc. | Issue opened, PR created, etc. | Built-in |
| GitLab | Source Control | OAuth | Create issue, merge request operations | Issue/MR events | Built-in |
| Jira | Project Management | OAuth | Create/update issue, add comment | Issue created, status changed | Built-in |
| Linear | Project Management | OAuth | Create issue, update status | Issue events | Built-in |
| Confluence | Documentation | OAuth | Create page, update content | Page published, updated | Built-in |
| Notion | Documentation | OAuth | Create/update page, add block | Page modified | Built-in |
| Slack | Communication | OAuth | Send message, react, thread reply | Message, mention, reaction | Built-in |
| OpenAI | AI | API Key | Complete, chat, embeddings | (Polling-based) | Built-in |
| Anthropic | AI | API Key | Message, completion | (Polling-based) | Built-in |

---

## 7. License Gating

| Tier | Connections | Webhooks | Custom Connectors | API Gateway | Audit Log |
|:-----|:-----------|:---------|:------------------|:------------|:----------|
| **Core** | 2 | No | No | No | No |
| **WriterPro** | 5 | Yes | No | Basic | 30 days |
| **Teams** | Unlimited | Yes | No | Full | 90 days |
| **Enterprise** | Unlimited | Yes | Yes | Full + Custom | Unlimited |

**License Gating Implementation:**
- Connection limit enforced via `IIntegrationManager.GetRemainingConnectionsAsync()`
- Features checked via `ILicenseContext.HasFeature()` at integration creation time
- Rate limit enforcement at API Gateway tier
- Audit retention configured per license tier

---

## 8. MediatR Events

```csharp
namespace Lexichord.Modules.Integrations.Events;

/// <summary>
/// Published when new connection is created and authenticated.
/// </summary>
public record IntegrationConnectedEvent(
    Guid ConnectionId,
    string ConnectorId,
    string ExternalAccountId,
    DateTime Timestamp) : INotification;

/// <summary>
/// Published when connection is disconnected/removed.
/// </summary>
public record IntegrationDisconnectedEvent(
    Guid ConnectionId,
    string ConnectorId,
    DateTime Timestamp) : INotification;

/// <summary>
/// Published when connector action is executed.
/// </summary>
public record IntegrationActionExecutedEvent(
    Guid ConnectionId,
    string ActionId,
    ActionResult Result,
    long DurationMs,
    DateTime Timestamp) : INotification;

/// <summary>
/// Published when webhook is delivered successfully.
/// </summary>
public record WebhookDeliveredEvent(
    Guid WebhookId,
    Guid ConnectionId,
    string TriggerId,
    object Payload,
    DateTime Timestamp) : INotification;

/// <summary>
/// Published when webhook delivery fails (before retry).
/// </summary>
public record WebhookDeliveryFailedEvent(
    Guid WebhookId,
    string ErrorMessage,
    int AttemptNumber,
    DateTime Timestamp) : INotification;

/// <summary>
/// Published for API gateway access (audit/metrics).
/// </summary>
public record IntegrationApiRequestEvent(
    Guid ConnectionId,
    string Method,
    string Endpoint,
    int StatusCode,
    long DurationMs,
    DateTime Timestamp) : INotification;
```

---

## 9. Dependencies

| Component | Source | Usage |
|:----------|:-------|:------|
| `IPluginService` | v0.15.1-MKT | Plugin loading for custom connectors |
| `IAuthorizationService` | v0.11.1-SEC | OAuth scope validation |
| `IEncryptionService` | v0.11.3-SEC | Token and credential encryption |
| `ILicenseContext` | v0.0.4c | Connection limit enforcement |
| `IMediator` | v0.0.7a | Event publishing |
| `IHttpClientFactory` | System | HTTP requests to external APIs |
| PostgreSQL | Database | Integration storage and audit logs |

---

## 10. Performance Targets

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| OAuth flow completion | < 3 seconds | End-to-end timing (minus user interaction) |
| Webhook registration | < 500ms | API call latency |
| Webhook delivery | < 1 second | End-to-end including retries |
| API Gateway throughput | 1000+ req/sec | Load test measurement |
| API Gateway latency | < 100ms P95 | Request routing overhead |
| Credential vault access | < 10ms | Encrypt/decrypt operations |
| Connection health check | < 500ms | External service ping |

---

## 11. Testing Strategy

### 11.1 Unit Tests

```csharp
// OAuth Manager tests
[Fact]
public async Task StartFlowAsync_GeneratesValidAuthorizationUrl();

[Fact]
public async Task ExchangeCodeAsync_ReturnsValidToken();

[Fact]
public async Task RefreshTokenAsync_UpdatesExpiredToken();

// Webhook Manager tests
[Fact]
public async Task RegisterWebhookAsync_CreatesRegistration();

[Fact]
public async Task HandleWebhookAsync_ValidatesSignature();

[Fact]
public async Task HandleWebhookAsync_RejectsInvalidSignature();

// Connector Framework tests
[Fact]
public void GetActions_ReturnsValidActionList();

[Fact]
public async Task ExecuteActionAsync_RoutesToAction();

// API Gateway tests
[Fact]
public async Task RouteRequestAsync_EnforcesRateLimit();

[Fact]
public async Task RouteRequestAsync_TransformsRequest();

// Credential Vault tests
[Fact]
public async Task StoreCredentialAsync_EncryptsValue();

[Fact]
public async Task GetCredentialAsync_DecryptsValue();
```

### 11.2 Integration Tests

- OAuth flow with GitHub, Slack, Linear
- Webhook registration and delivery
- Action execution for each built-in connector
- Rate limit enforcement
- Credential encryption/decryption
- License gating enforcement
- Concurrent connection management

### 11.3 Test Coverage

| Component | Target |
|:----------|:-------|
| OAuth Manager | 95% |
| Webhook Manager | 95% |
| API Gateway | 90% |
| Credential Vault | 95% |
| Connector Framework | 85% |

---

## 12. Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|:-----|:-----------|:-------|:-----------|
| OAuth token revocation at service | Medium | High | Automatic refresh before expiry, handle revocation gracefully |
| Webhook signature validation bypass | Low | Critical | Use HMAC-SHA256, validate before processing |
| Rate limit exhaustion | Medium | Medium | Track limits per service, implement backoff |
| Token leakage in logs | Low | Critical | Never log tokens, mask in UI, audit encryption |
| Webhook delivery loss | Low | High | Retry logic with exponential backoff, delivery log |
| Connector SDK API stability | Medium | Medium | Semantic versioning, backward compatibility |
| Concurrent OAuth flow conflicts | Low | Medium | Session-based flow tracking with expiration |
| Custom connector execution safety | Medium | High | Sandbox execution, permission scoping, audit log |

---

## 13. Deliverables by Sub-Part

### 13.1 v0.15.4e: Connector Framework

**Deliverables:**
- `IConnector` interface with manifest system
- `ConnectorManifest`, `ConnectorAction`, `ConnectorTrigger` records
- Base `BaseConnector` abstract class for SDK
- Connector registry and discovery
- Action result structures
- 9 built-in connector implementations
- Unit tests (85%+ coverage)

**Hours:** 12

### 13.2 v0.15.4f: OAuth Manager

**Deliverables:**
- `IOAuthManager` interface
- OAuth 2.0 flow implementation with PKCE
- Token refresh with exponential backoff
- Token revocation handling
- `OAuthFlow`, `OAuthToken` records
- State token and CSRF protection
- Integration with credential vault
- Unit tests (95%+ coverage)

**Hours:** 10

### 13.3 v0.15.4g: Webhook System

**Deliverables:**
- `IWebhookManager` interface
- Webhook registration for each connector
- Signature validation (HMAC-SHA256)
- Webhook endpoint at `/webhooks/{connectorId}`
- Delivery retry logic with exponential backoff
- `WebhookRegistration`, `WebhookPayload` records
- Webhook delivery logging
- Unit tests (95%+ coverage)

**Hours:** 10

### 13.4 v0.15.4h: API Gateway

**Deliverables:**
- `IApiGateway` interface
- Request routing to connector API
- Rate limit enforcement
- Request/response transformation
- Authentication header injection
- `GatewayRequest`, `GatewayResponse` records
- API request logging and metrics
- Rate limit status tracking
- Unit tests (90%+ coverage)

**Hours:** 10

### 13.5 v0.15.4i: Credential Vault

**Deliverables:**
- `ICredentialVault` interface
- AES-256 encryption at rest
- IV generation and storage
- Token rotation logic
- Credential storage migration
- Integration with v0.11.3-SEC (Encryption)
- Unit tests (95%+ coverage)

**Hours:** 4

### 13.6 v0.15.4j: Integration Manager UI

**Deliverables:**
- Connection creation and management UI
- OAuth flow UI with redirect handling
- Trigger/action builder component
- Health status display
- Credential settings panel
- Connection removal confirmation
- Recent activity log view
- License tier messaging

**Hours:** 4

---

## 14. Implementation Checklist

### Phase 1: Foundation (15 hours)

- [ ] Define `IConnector`, `ConnectorManifest` interfaces
- [ ] Define `IOAuthManager` and token classes
- [ ] Create database migrations
- [ ] Implement credential vault with encryption
- [ ] Set up webhook endpoint infrastructure

### Phase 2: Core Services (20 hours)

- [ ] Implement OAuth Manager with token refresh
- [ ] Implement Webhook Manager with delivery
- [ ] Implement API Gateway with rate limiting
- [ ] Implement Integration Manager
- [ ] Write integration tests

### Phase 3: Connectors & UI (15 hours)

- [ ] Implement 9 built-in connectors
- [ ] Create Connection Manager UI
- [ ] Create OAuth flow UI
- [ ] Create Trigger/Action builder
- [ ] Write UI tests

---

## 15. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| OAuth success rate | >99% | Telemetry tracking |
| Webhook delivery rate | >99.9% | Log analysis |
| API Gateway latency | <100ms P95 | APM monitoring |
| Connector health | >99% | Periodic health checks |
| User adoption | 50%+ of WriterPro | Usage analytics |

---

## 16. Acceptance Criteria

| # | Criterion | Verification |
|:--|:----------|:------------|
| 1 | OAuth flows complete with all 9 connectors | Integration test |
| 2 | Webhook registration succeeds with signature validation | Integration test |
| 3 | API Gateway routes requests transparently | Load test |
| 4 | Credentials stored encrypted with AES-256 | Code review + test |
| 5 | License gating enforced for connection limits | Integration test |
| 6 | Connection UI allows CRUD operations | Manual test |
| 7 | Trigger/action builder shows available options | Manual test |
| 8 | Health checks detect disconnected credentials | Integration test |
| 9 | Audit log tracks all integration activity | Log analysis |
| 10 | Performance targets achieved (latency, throughput) | Performance test |

---

## 17. Deferred Features

| Feature | Reason | Target Version |
|:--------|:-------|:---------------|
| AI-powered action suggestions | UX enhancement, not critical | v0.15.5 |
| Webhook payload transformation | DSL complexity | v0.15.6 |
| Connector marketplace | Requires additional infrastructure | v0.16.x |
| Native mobile OAuth | Platform-specific | v0.16.x |
| Webhook signing with RS256 | Advanced feature | v0.16.x |

---

## 18. Changelog Entry

```markdown
## [0.15.4] - Integration Hub

### Added

- **Connector Framework** (v0.15.4e): Plugin-based architecture for external service integrations
    - IConnector protocol with manifest system
    - Built-in connectors: GitHub, GitLab, Jira, Linear, Confluence, Notion, Slack, OpenAI, Anthropic
    - Action and trigger registry for automation
    - Connector SDK for custom integrations

- **OAuth Manager** (v0.15.4f): Enterprise OAuth 2.0 implementation
    - Full OAuth 2.0 flow with PKCE support
    - Automatic token refresh with expiry handling
    - Token revocation and cleanup
    - Multi-flow session management

- **Webhook System** (v0.15.4g): Reliable webhook delivery
    - Webhook registration for supported connectors
    - HMAC-SHA256 signature validation
    - Automatic retry with exponential backoff
    - Delivery tracking and failure logging

- **API Gateway** (v0.15.4h): Request routing and management
    - Transparent request routing to connector APIs
    - Rate limit enforcement and tracking
    - Request/response transformation
    - API request logging and metrics

- **Credential Vault** (v0.15.4i): Secure credential storage
    - AES-256 encryption at rest
    - Automatic token rotation
    - Integration with v0.11.3-SEC encryption
    - Audit trail for credential access

- **Integration Manager UI** (v0.15.4j): User interface for integrations
    - Connection management panel
    - OAuth flow UI with redirect handling
    - Trigger and action builder
    - Health status monitoring

### Changed

- Lexichord now supports 9 major platform integrations
- Settings service extended to support integration credentials
- Search can index remote content from integrations
- MediatR event system includes integration events

### License Gating

- **Core**: 2 connections (no webhooks)
- **WriterPro**: 5 connections (with webhooks)
- **Teams**: Unlimited connections (with webhooks)
- **Enterprise**: Unlimited + custom connectors + API gateway
```

---

## 19. Document History

| Version | Date       | Author         | Changes |
|:--------|:-----------|:---------------|:--------|
| 1.0     | 2026-01-31 | Lead Architect | Initial draft |

