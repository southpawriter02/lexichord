# Lexichord Scope Breakdown Document
## v0.19.4-RES: Diagnostics & Logging

---

## Document Control

| Field | Value |
|:------|:------|
| **Document ID** | LCS-SBD-v0.19.4-RES |
| **Version** | 1.0.0 |
| **Status** | In Definition |
| **Author** | Lexichord Architecture Team |
| **Created** | 2025-01-31 |
| **Last Updated** | 2025-01-31 |
| **Target Release** | v0.19.4 |
| **Total Effort** | 54 hours |
| **Classification** | Technical Specification |

### Revision History

| Version | Date | Author | Changes |
|:--------|:-----|:-------|:--------|
| 1.0.0 | 2025-01-31 | Architecture Team | Initial comprehensive scope breakdown |

### Approval Signatures

| Role | Name | Date | Signature |
|:-----|:-----|:-----|:----------|
| Technical Lead | _________________ | _______ | _______ |
| Product Owner | _________________ | _______ | _______ |
| QA Lead | _________________ | _______ | _______ |
| Security Lead | _________________ | _______ | _______ |

---

## 1. Executive Summary

### 1.1 Vision Statement

v0.19.4-RES implements **Diagnostics & Logging** — a comprehensive observability infrastructure that provides deep insight into system behavior, enabling rapid issue detection, troubleshooting, and performance optimization. This version transforms Lexichord from a black-box application into a fully transparent, instrumentable platform.

### 1.2 Strategic Objectives

1. **Complete Observability**: Every significant operation is logged, measured, and traceable
2. **Rapid Debugging**: Reduce mean-time-to-diagnosis (MTTD) by 80% through structured logging
3. **Performance Visibility**: Track and optimize all critical paths with real-time metrics
4. **Developer Experience**: Intuitive debug modes and diagnostic tools for development and support
5. **Production Readiness**: Enterprise-grade logging infrastructure with retention, search, and export

### 1.3 Business Impact

| Metric | Current | Target | Improvement |
|:-------|:--------|:-------|:------------|
| Issue diagnosis time | 45 min | 9 min | 80% reduction |
| Log search latency | N/A | < 100ms | New capability |
| Performance visibility | 20% | 95% | 4.75x improvement |
| Debug session setup | 15 min | 30 sec | 30x faster |
| Support ticket resolution | 4 hours | 1.5 hours | 62% reduction |

### 1.4 Key Features

- **Structured Logging Engine**: Consistent, queryable log format with rich context
- **Telemetry Collection**: Events, metrics, dependencies, and request tracking
- **Performance Tracking**: Operation timing, percentiles, and bottleneck identification
- **Debug Mode System**: Component-specific debugging with minimal overhead
- **Log Viewer & Search**: In-app log exploration with powerful filtering
- **Diagnostic Export**: Support bundles for issue resolution

---

## 2. Sub-Parts Breakdown

### 2.1 v0.19.4a: Structured Logging Engine (10 hours)

#### 2.1.1 Overview

Implement a high-performance structured logging engine that captures rich contextual information in a consistent, machine-readable format while maintaining human readability.

#### 2.1.2 Deliverables

1. `IStructuredLogger` interface with scoped logging
2. Log entry schema with correlation tracking
3. Log enrichment pipeline (automatic context capture)
4. Log sink abstraction (console, file, database)
5. Log level configuration and filtering
6. Async log processing with backpressure

#### 2.1.3 Acceptance Criteria

- [ ] Log entries include timestamp, level, category, message, and properties
- [ ] Correlation IDs propagate across async boundaries
- [ ] Scoped logging captures hierarchical context
- [ ] Log level can be changed at runtime without restart
- [ ] Log writes complete in < 1ms (async, buffered)
- [ ] Memory usage stays constant under sustained logging
- [ ] Logs can be output in JSON or text format
- [ ] Sensitive data is automatically redacted
- [ ] Log entries are correctly partitioned by date
- [ ] Log rotation and retention work as configured

### 2.2 v0.19.4b: Telemetry Collection (10 hours)

#### 2.2.1 Overview

Build a comprehensive telemetry collection system that captures events, metrics, dependencies, and requests with minimal performance overhead and configurable sampling.

#### 2.2.2 Deliverables

1. `ITelemetryCollector` interface with all track methods
2. Event telemetry (custom events with properties)
3. Metric telemetry (counters, gauges, histograms)
4. Dependency telemetry (external service calls)
5. Request telemetry (operation tracking)
6. Telemetry sampling and filtering
7. Flush management and batching

#### 2.2.3 Acceptance Criteria

- [ ] Events capture custom properties and metrics
- [ ] Metrics support counters, gauges, and histograms
- [ ] Dependency tracking includes duration and success status
- [ ] Request tracking captures full operation lifecycle
- [ ] Sampling reduces volume while maintaining insight
- [ ] Flush completes within 5 seconds
- [ ] Telemetry overhead < 0.1% of request time
- [ ] Batching reduces I/O operations by 90%
- [ ] Failed telemetry doesn't affect application
- [ ] Telemetry can be disabled completely

### 2.3 v0.19.4c: Performance Tracking (10 hours)

#### 2.3.1 Overview

Implement detailed performance tracking with operation timing, percentile calculations, and bottleneck identification to enable data-driven optimization.

#### 2.3.2 Deliverables

1. `IPerformanceTracker` interface with scope-based tracking
2. `IPerformanceScope` for automatic timing
3. Histogram-based percentile calculation (p50, p95, p99)
4. Operation categorization and tagging
5. Performance summary aggregation
6. Checkpoint recording within operations
7. Resource correlation (CPU, memory per operation)

#### 2.3.3 Acceptance Criteria

- [ ] Scoped measurements auto-complete on dispose
- [ ] Percentiles calculated accurately from histograms
- [ ] Tags enable multi-dimensional analysis
- [ ] Checkpoints capture intermediate timings
- [ ] Summary queries return in < 500ms
- [ ] Performance tracking overhead < 0.5ms
- [ ] Historical data retained per configuration
- [ ] Slow operation alerts trigger correctly
- [ ] Counters and gauges update atomically
- [ ] Metrics aggregate correctly across instances

### 2.4 v0.19.4d: Debug Mode System (8 hours)

#### 2.4.1 Overview

Create a flexible debug mode system that enables verbose logging, performance tracing, and internal state inspection for specific components without affecting overall performance.

#### 2.4.2 Deliverables

1. `IDebugModeManager` interface
2. Global debug mode toggle
3. Component-specific debug configuration
4. Debug log level boosting
5. Performance tracing injection
6. Memory profiling hooks
7. SQL query logging
8. Agent internal state logging

#### 2.4.3 Acceptance Criteria

- [ ] Debug mode enables without restart
- [ ] Component-specific debugging isolates noise
- [ ] Auto-disable prevents forgotten debug sessions
- [ ] Debug overhead documented and acceptable
- [ ] SQL queries logged with parameters
- [ ] Memory profiling captures allocations
- [ ] Network inspection shows request/response
- [ ] Agent internals reveal decision process
- [ ] Debug mode indicated in UI
- [ ] Production safeguards prevent accidental enable

### 2.5 v0.19.4e: Log Viewer & Search (10 hours)

#### 2.5.1 Overview

Build an in-application log viewer with powerful search, filtering, and visualization capabilities that enables rapid issue investigation without external tools.

#### 2.5.2 Deliverables

1. `ILogViewer` interface with query capabilities
2. Log query DSL with multiple filter types
3. Full-text search across log messages
4. Real-time log streaming
5. Log entry detail view
6. Log statistics and aggregation
7. Export to various formats

#### 2.5.3 Acceptance Criteria

- [ ] Search by time range, level, category, and text
- [ ] Results paginate correctly with large datasets
- [ ] Real-time stream updates within 1 second
- [ ] Full-text search returns in < 100ms
- [ ] Filters combine with AND/OR logic
- [ ] Statistics show log distribution
- [ ] Export produces valid JSON, CSV, or text
- [ ] Correlation ID search finds related logs
- [ ] Query history persists between sessions
- [ ] Large result sets don't crash viewer

### 2.6 v0.19.4f: Diagnostic Export (6 hours)

#### 2.6.1 Overview

Implement a diagnostic export system that generates comprehensive support bundles containing logs, configuration, system info, and performance data for issue resolution.

#### 2.6.2 Deliverables

1. `IDiagnosticExporter` interface
2. Diagnostic bundle specification
3. Log collection with period selection
4. Configuration sanitization
5. System information gathering
6. Performance data inclusion
7. Compression and encryption
8. Support submission integration

#### 2.6.3 Acceptance Criteria

- [ ] Bundle includes all specified components
- [ ] Sensitive data automatically sanitized
- [ ] Bundle generation completes in < 60 seconds
- [ ] Bundle size minimized through compression
- [ ] System info includes OS, runtime, resources
- [ ] Performance data shows recent trends
- [ ] Export format documented for support team
- [ ] Submission to support succeeds with confirmation
- [ ] Progress indicator shows export status
- [ ] Failed exports provide actionable error

---

## 3. C# Interface Specifications

### 3.1 IStructuredLogger

```csharp
using System;
using System.Collections.Generic;

namespace Lexichord.Resilience.Logging
{
    /// <summary>
    /// Provides structured logging with rich context and scoping.
    /// </summary>
    public interface IStructuredLogger
    {
        /// <summary>
        /// Gets the logger category (usually type name).
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Logs a message at the specified level with structured properties.
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The log message (can contain placeholders).</param>
        /// <param name="properties">Structured properties to include.</param>
        /// <param name="exception">Optional exception to log.</param>
        void Log(
            LogLevel level,
            string message,
            IReadOnlyDictionary<string, object>? properties = null,
            Exception? exception = null);

        /// <summary>
        /// Logs a message using a message template with named parameters.
        /// </summary>
        void Log(
            LogLevel level,
            string messageTemplate,
            params object[] args);

        /// <summary>
        /// Creates a logging scope that adds properties to all logs within it.
        /// </summary>
        /// <param name="scopeName">Name of the scope.</param>
        /// <param name="properties">Properties to add to scope.</param>
        /// <returns>Disposable scope.</returns>
        IDisposable BeginScope(
            string scopeName,
            IReadOnlyDictionary<string, object>? properties = null);

        /// <summary>
        /// Creates a child logger with an extended category.
        /// </summary>
        /// <param name="childCategory">Child category to append.</param>
        /// <returns>Child logger instance.</returns>
        IStructuredLogger CreateChild(string childCategory);

        /// <summary>
        /// Adds a property that persists across all log entries.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <param name="value">Property value.</param>
        void AddProperty(string key, object value);

        /// <summary>
        /// Removes a persistent property.
        /// </summary>
        /// <param name="key">Property key to remove.</param>
        void RemoveProperty(string key);

        /// <summary>
        /// Checks if logging at the specified level is enabled.
        /// </summary>
        /// <param name="level">Level to check.</param>
        /// <returns>True if enabled.</returns>
        bool IsEnabled(LogLevel level);

        // Convenience methods
        void Trace(string message, IReadOnlyDictionary<string, object>? properties = null);
        void Debug(string message, IReadOnlyDictionary<string, object>? properties = null);
        void Info(string message, IReadOnlyDictionary<string, object>? properties = null);
        void Warn(string message, IReadOnlyDictionary<string, object>? properties = null);
        void Error(string message, Exception? exception = null, IReadOnlyDictionary<string, object>? properties = null);
        void Critical(string message, Exception? exception = null, IReadOnlyDictionary<string, object>? properties = null);
    }

    /// <summary>
    /// Standard log levels.
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6
    }

    /// <summary>
    /// Represents a structured log entry.
    /// </summary>
    public record LogEntry
    {
        public LogEntryId Id { get; init; }
        public DateTime Timestamp { get; init; }
        public LogLevel Level { get; init; }
        public string Category { get; init; } = "";
        public string Message { get; init; } = "";
        public string? MessageTemplate { get; init; }
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
        public ExceptionInfo? Exception { get; init; }
        public string? CorrelationId { get; init; }
        public string? SpanId { get; init; }
        public string? ParentSpanId { get; init; }
        public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
        public string? MachineName { get; init; }
        public int ThreadId { get; init; }
    }

    public readonly record struct LogEntryId(Guid Value)
    {
        public static LogEntryId New() => new(Guid.NewGuid());
    }

    /// <summary>
    /// Exception information for logging.
    /// </summary>
    public record ExceptionInfo
    {
        public string Type { get; init; } = "";
        public string Message { get; init; } = "";
        public string? StackTrace { get; init; }
        public ExceptionInfo? InnerException { get; init; }
        public IReadOnlyDictionary<string, object>? Data { get; init; }
    }

    /// <summary>
    /// Factory for creating structured loggers.
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Creates a logger for the specified category.
        /// </summary>
        IStructuredLogger CreateLogger(string category);

        /// <summary>
        /// Creates a logger for the specified type.
        /// </summary>
        IStructuredLogger CreateLogger<T>();

        /// <summary>
        /// Configures minimum log level for a category.
        /// </summary>
        void SetMinimumLevel(string categoryPrefix, LogLevel level);

        /// <summary>
        /// Adds a log sink.
        /// </summary>
        void AddSink(ILogSink sink);
    }

    /// <summary>
    /// Log output destination.
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Writes a log entry.
        /// </summary>
        Task WriteAsync(LogEntry entry, CancellationToken ct = default);

        /// <summary>
        /// Writes multiple log entries.
        /// </summary>
        Task WriteBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken ct = default);

        /// <summary>
        /// Flushes pending writes.
        /// </summary>
        Task FlushAsync(CancellationToken ct = default);
    }
}
```

### 3.2 ITelemetryCollector

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Resilience.Telemetry
{
    /// <summary>
    /// Collects telemetry data including events, metrics, dependencies, and requests.
    /// </summary>
    public interface ITelemetryCollector
    {
        /// <summary>
        /// Tracks a custom event.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">String properties for the event.</param>
        /// <param name="metrics">Numeric metrics for the event.</param>
        void TrackEvent(
            string eventName,
            IReadOnlyDictionary<string, string>? properties = null,
            IReadOnlyDictionary<string, double>? metrics = null);

        /// <summary>
        /// Tracks a metric value.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="dimensions">Dimensions for the metric.</param>
        void TrackMetric(
            string name,
            double value,
            IReadOnlyDictionary<string, string>? dimensions = null);

        /// <summary>
        /// Tracks a dependency call to an external service.
        /// </summary>
        /// <param name="type">Dependency type (e.g., "HTTP", "SQL").</param>
        /// <param name="name">Dependency name.</param>
        /// <param name="target">Target (e.g., URL, database name).</param>
        /// <param name="duration">Duration of the call.</param>
        /// <param name="success">Whether the call succeeded.</param>
        void TrackDependency(
            string type,
            string name,
            string target,
            TimeSpan duration,
            bool success);

        /// <summary>
        /// Tracks a dependency with detailed information.
        /// </summary>
        void TrackDependency(DependencyTelemetry telemetry);

        /// <summary>
        /// Tracks a request/operation.
        /// </summary>
        /// <param name="name">Request name.</param>
        /// <param name="startTime">When the request started.</param>
        /// <param name="duration">Duration of the request.</param>
        /// <param name="responseCode">Response code or status.</param>
        /// <param name="success">Whether the request succeeded.</param>
        void TrackRequest(
            string name,
            DateTime startTime,
            TimeSpan duration,
            string responseCode,
            bool success);

        /// <summary>
        /// Tracks a request with detailed information.
        /// </summary>
        void TrackRequest(RequestTelemetry telemetry);

        /// <summary>
        /// Tracks an exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="properties">Additional properties.</param>
        void TrackException(
            Exception exception,
            IReadOnlyDictionary<string, string>? properties = null);

        /// <summary>
        /// Tracks a trace message.
        /// </summary>
        /// <param name="message">Trace message.</param>
        /// <param name="severity">Severity level.</param>
        /// <param name="properties">Additional properties.</param>
        void TrackTrace(
            string message,
            TraceSeverity severity = TraceSeverity.Information,
            IReadOnlyDictionary<string, string>? properties = null);

        /// <summary>
        /// Flushes all pending telemetry.
        /// </summary>
        Task FlushAsync(CancellationToken ct = default);

        /// <summary>
        /// Configures the telemetry collector.
        /// </summary>
        void Configure(TelemetryConfiguration config);

        /// <summary>
        /// Gets telemetry statistics.
        /// </summary>
        TelemetryStatistics GetStatistics();

        /// <summary>
        /// Gets or sets the operation context.
        /// </summary>
        OperationContext CurrentOperation { get; set; }
    }

    /// <summary>
    /// Dependency telemetry data.
    /// </summary>
    public record DependencyTelemetry
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
        public string Target { get; init; } = "";
        public string? Data { get; init; }
        public DateTime StartTime { get; init; }
        public TimeSpan Duration { get; init; }
        public bool Success { get; init; }
        public string? ResultCode { get; init; }
        public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Request telemetry data.
    /// </summary>
    public record RequestTelemetry
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = "";
        public DateTime StartTime { get; init; }
        public TimeSpan Duration { get; init; }
        public string ResponseCode { get; init; } = "";
        public bool Success { get; init; }
        public string? Source { get; init; }
        public string? Url { get; init; }
        public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    }

    public enum TraceSeverity
    {
        Verbose,
        Information,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Telemetry configuration.
    /// </summary>
    public record TelemetryConfiguration
    {
        public bool Enabled { get; init; } = true;
        public bool CollectEvents { get; init; } = true;
        public bool CollectMetrics { get; init; } = true;
        public bool CollectDependencies { get; init; } = true;
        public bool CollectRequests { get; init; } = true;
        public bool CollectExceptions { get; init; } = true;
        public double SamplingPercentage { get; init; } = 100;
        public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(30);
        public int BatchSize { get; init; } = 100;
        public int MaxQueueSize { get; init; } = 10000;
        public IReadOnlyList<string> ExcludedCategories { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Operation context for correlation.
    /// </summary>
    public record OperationContext
    {
        public string OperationId { get; init; } = "";
        public string? ParentId { get; init; }
        public string OperationName { get; init; } = "";
        public DateTime StartTime { get; init; }
    }

    /// <summary>
    /// Telemetry collection statistics.
    /// </summary>
    public record TelemetryStatistics
    {
        public long EventsCollected { get; init; }
        public long MetricsCollected { get; init; }
        public long DependenciesCollected { get; init; }
        public long RequestsCollected { get; init; }
        public long ExceptionsCollected { get; init; }
        public long TelemetrySent { get; init; }
        public long TelemetryDropped { get; init; }
        public int QueueDepth { get; init; }
        public DateTime LastFlush { get; init; }
    }
}
```

### 3.3 IPerformanceTracker

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Resilience.Performance
{
    /// <summary>
    /// Tracks performance metrics across operations.
    /// </summary>
    public interface IPerformanceTracker
    {
        /// <summary>
        /// Starts a performance measurement scope.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        /// <param name="tags">Optional tags for categorization.</param>
        /// <returns>Disposable scope that records duration on dispose.</returns>
        IPerformanceScope StartMeasurement(
            string operation,
            IReadOnlyDictionary<string, string>? tags = null);

        /// <summary>
        /// Records a timing measurement directly.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        /// <param name="duration">Duration of the operation.</param>
        /// <param name="tags">Optional tags.</param>
        void RecordTiming(
            string operation,
            TimeSpan duration,
            IReadOnlyDictionary<string, string>? tags = null);

        /// <summary>
        /// Increments a counter.
        /// </summary>
        /// <param name="name">Counter name.</param>
        /// <param name="value">Value to add (default 1).</param>
        /// <param name="tags">Optional tags.</param>
        void IncrementCounter(
            string name,
            long value = 1,
            IReadOnlyDictionary<string, string>? tags = null);

        /// <summary>
        /// Records a gauge value.
        /// </summary>
        /// <param name="name">Gauge name.</param>
        /// <param name="value">Current value.</param>
        /// <param name="tags">Optional tags.</param>
        void RecordGauge(
            string name,
            double value,
            IReadOnlyDictionary<string, string>? tags = null);

        /// <summary>
        /// Records a histogram value.
        /// </summary>
        /// <param name="name">Histogram name.</param>
        /// <param name="value">Value to record.</param>
        /// <param name="tags">Optional tags.</param>
        void RecordHistogram(
            string name,
            double value,
            IReadOnlyDictionary<string, string>? tags = null);

        /// <summary>
        /// Gets a performance summary for a time period.
        /// </summary>
        /// <param name="period">Time period to summarize.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Performance summary.</returns>
        Task<PerformanceSummary> GetSummaryAsync(
            TimeSpan period,
            CancellationToken ct = default);

        /// <summary>
        /// Gets metrics for a specific operation.
        /// </summary>
        /// <param name="operation">Operation name.</param>
        /// <param name="period">Time period.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Operation metrics.</returns>
        Task<OperationMetrics> GetOperationMetricsAsync(
            string operation,
            TimeSpan period,
            CancellationToken ct = default);

        /// <summary>
        /// Gets slow operations above a threshold.
        /// </summary>
        /// <param name="threshold">Duration threshold.</param>
        /// <param name="limit">Maximum results.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of slow operations.</returns>
        Task<IReadOnlyList<SlowOperation>> GetSlowOperationsAsync(
            TimeSpan threshold,
            int limit = 100,
            CancellationToken ct = default);

        /// <summary>
        /// Observable stream of performance alerts.
        /// </summary>
        IObservable<PerformanceAlert> Alerts { get; }
    }

    /// <summary>
    /// Performance measurement scope with checkpoints.
    /// </summary>
    public interface IPerformanceScope : IDisposable
    {
        /// <summary>
        /// Gets the operation name.
        /// </summary>
        string Operation { get; }

        /// <summary>
        /// Gets the start time.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Gets the elapsed time.
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Adds a tag to the measurement.
        /// </summary>
        void AddTag(string key, string value);

        /// <summary>
        /// Records an intermediate checkpoint.
        /// </summary>
        /// <param name="name">Checkpoint name.</param>
        void RecordCheckpoint(string name);

        /// <summary>
        /// Sets the operation success status.
        /// </summary>
        void SetSuccess(bool success);

        /// <summary>
        /// Sets an error if the operation failed.
        /// </summary>
        void SetError(Exception? exception);

        /// <summary>
        /// Gets recorded checkpoints.
        /// </summary>
        IReadOnlyList<PerformanceCheckpoint> Checkpoints { get; }
    }

    /// <summary>
    /// Performance checkpoint within an operation.
    /// </summary>
    public record PerformanceCheckpoint
    {
        public string Name { get; init; } = "";
        public TimeSpan Elapsed { get; init; }
        public TimeSpan SincePrevious { get; init; }
    }

    /// <summary>
    /// Summary of performance over a period.
    /// </summary>
    public record PerformanceSummary
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public IReadOnlyList<OperationMetrics> Operations { get; init; } = Array.Empty<OperationMetrics>();
        public IReadOnlyList<CounterMetrics> Counters { get; init; } = Array.Empty<CounterMetrics>();
        public IReadOnlyList<GaugeMetrics> Gauges { get; init; } = Array.Empty<GaugeMetrics>();
        public ResourceMetrics Resources { get; init; } = new();
        public int TotalOperations { get; init; }
        public int SlowOperations { get; init; }
        public int FailedOperations { get; init; }
    }

    /// <summary>
    /// Metrics for a specific operation.
    /// </summary>
    public record OperationMetrics
    {
        public string Name { get; init; } = "";
        public long Count { get; init; }
        public TimeSpan MinDuration { get; init; }
        public TimeSpan MaxDuration { get; init; }
        public TimeSpan AvgDuration { get; init; }
        public TimeSpan P50Duration { get; init; }
        public TimeSpan P95Duration { get; init; }
        public TimeSpan P99Duration { get; init; }
        public double SuccessRate { get; init; }
        public long SuccessCount { get; init; }
        public long FailureCount { get; init; }
        public IReadOnlyDictionary<string, long> TagCounts { get; init; } = new Dictionary<string, long>();
    }

    /// <summary>
    /// Counter metrics.
    /// </summary>
    public record CounterMetrics
    {
        public string Name { get; init; } = "";
        public long Value { get; init; }
        public long Delta { get; init; }
        public double Rate { get; init; }
    }

    /// <summary>
    /// Gauge metrics.
    /// </summary>
    public record GaugeMetrics
    {
        public string Name { get; init; } = "";
        public double Current { get; init; }
        public double Min { get; init; }
        public double Max { get; init; }
        public double Avg { get; init; }
    }

    /// <summary>
    /// Resource utilization metrics.
    /// </summary>
    public record ResourceMetrics
    {
        public double CpuPercent { get; init; }
        public long MemoryUsedBytes { get; init; }
        public long MemoryTotalBytes { get; init; }
        public long DiskUsedBytes { get; init; }
        public long DiskTotalBytes { get; init; }
        public int ThreadCount { get; init; }
        public int HandleCount { get; init; }
        public double GcPausePercent { get; init; }
    }

    /// <summary>
    /// Slow operation record.
    /// </summary>
    public record SlowOperation
    {
        public string Operation { get; init; } = "";
        public TimeSpan Duration { get; init; }
        public DateTime Timestamp { get; init; }
        public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
        public IReadOnlyList<PerformanceCheckpoint> Checkpoints { get; init; } = Array.Empty<PerformanceCheckpoint>();
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Performance alert.
    /// </summary>
    public record PerformanceAlert
    {
        public PerformanceAlertType Type { get; init; }
        public string Operation { get; init; } = "";
        public string Message { get; init; } = "";
        public double Threshold { get; init; }
        public double ActualValue { get; init; }
        public DateTime Timestamp { get; init; }
    }

    public enum PerformanceAlertType
    {
        SlowOperation,
        HighErrorRate,
        ThroughputDrop,
        ResourceExhaustion
    }
}
```

### 3.4 IDebugModeManager

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Resilience.Diagnostics
{
    /// <summary>
    /// Manages debug modes and diagnostic settings.
    /// </summary>
    public interface IDebugModeManager
    {
        /// <summary>
        /// Enables global debug mode.
        /// </summary>
        /// <param name="options">Debug mode options.</param>
        /// <param name="ct">Cancellation token.</param>
        Task EnableAsync(
            DebugModeOptions options,
            CancellationToken ct = default);

        /// <summary>
        /// Disables global debug mode.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task DisableAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets whether debug mode is currently active.
        /// </summary>
        bool IsDebugModeActive { get; }

        /// <summary>
        /// Gets the current debug configuration.
        /// </summary>
        DebugModeOptions? CurrentConfiguration { get; }

        /// <summary>
        /// Enables debug mode for a specific component.
        /// </summary>
        /// <param name="component">Component identifier.</param>
        /// <param name="options">Component-specific options.</param>
        /// <param name="ct">Cancellation token.</param>
        Task EnableComponentDebugAsync(
            string component,
            ComponentDebugOptions options,
            CancellationToken ct = default);

        /// <summary>
        /// Disables debug mode for a specific component.
        /// </summary>
        /// <param name="component">Component identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DisableComponentDebugAsync(
            string component,
            CancellationToken ct = default);

        /// <summary>
        /// Gets components with debug enabled.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task<IReadOnlyList<ComponentDebugStatus>> GetDebugComponentsAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Gets available debug components.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task<IReadOnlyList<DebugComponent>> GetAvailableComponentsAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Creates a debug session with all settings.
        /// </summary>
        /// <param name="options">Session options.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<DebugSession> StartSessionAsync(
            DebugSessionOptions options,
            CancellationToken ct = default);

        /// <summary>
        /// Ends a debug session.
        /// </summary>
        /// <param name="sessionId">Session to end.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<DebugSessionSummary> EndSessionAsync(
            DebugSessionId sessionId,
            CancellationToken ct = default);

        /// <summary>
        /// Observable stream of debug events.
        /// </summary>
        IObservable<DebugEvent> DebugEvents { get; }
    }

    /// <summary>
    /// Global debug mode options.
    /// </summary>
    public record DebugModeOptions
    {
        public bool VerboseLogging { get; init; } = true;
        public bool PerformanceTracing { get; init; } = true;
        public bool NetworkInspection { get; init; }
        public bool MemoryProfiling { get; init; }
        public bool SqlQueryLogging { get; init; }
        public bool AgentInternals { get; init; }
        public bool RequestResponseLogging { get; init; }
        public TimeSpan? AutoDisableAfter { get; init; }
        public IReadOnlyList<string>? EnabledComponents { get; init; }
        public LogLevel MinimumLogLevel { get; init; } = LogLevel.Debug;
    }

    /// <summary>
    /// Component-specific debug options.
    /// </summary>
    public record ComponentDebugOptions
    {
        public LogLevel LogLevel { get; init; } = LogLevel.Debug;
        public bool TraceMethodCalls { get; init; }
        public bool LogParameters { get; init; }
        public bool LogReturnValues { get; init; }
        public bool CaptureStackTraces { get; init; }
        public TimeSpan? AutoDisableAfter { get; init; }
        public IReadOnlyList<string>? MethodFilters { get; init; }
    }

    /// <summary>
    /// Debug component definition.
    /// </summary>
    public record DebugComponent
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Category { get; init; } = "";
        public IReadOnlyList<string> AvailableFeatures { get; init; } = Array.Empty<string>();
        public bool IsEnabled { get; init; }
    }

    /// <summary>
    /// Status of a debug-enabled component.
    /// </summary>
    public record ComponentDebugStatus
    {
        public string Component { get; init; } = "";
        public ComponentDebugOptions Options { get; init; } = new();
        public DateTime EnabledAt { get; init; }
        public DateTime? DisablesAt { get; init; }
        public int LogEntriesGenerated { get; init; }
    }

    /// <summary>
    /// Debug session for grouped diagnostics.
    /// </summary>
    public record DebugSession
    {
        public DebugSessionId Id { get; init; }
        public string Name { get; init; } = "";
        public DateTime StartTime { get; init; }
        public DebugSessionOptions Options { get; init; } = new();
        public DebugSessionState State { get; init; }
    }

    public readonly record struct DebugSessionId(Guid Value)
    {
        public static DebugSessionId New() => new(Guid.NewGuid());
    }

    public record DebugSessionOptions
    {
        public string Name { get; init; } = "";
        public DebugModeOptions GlobalOptions { get; init; } = new();
        public IReadOnlyDictionary<string, ComponentDebugOptions> ComponentOptions { get; init; }
            = new Dictionary<string, ComponentDebugOptions>();
        public TimeSpan? MaxDuration { get; init; }
        public bool AutoExportOnEnd { get; init; }
    }

    public enum DebugSessionState
    {
        Active,
        Paused,
        Ended,
        Expired
    }

    /// <summary>
    /// Summary of a completed debug session.
    /// </summary>
    public record DebugSessionSummary
    {
        public DebugSessionId Id { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public TimeSpan Duration { get; init; }
        public int TotalLogEntries { get; init; }
        public int TotalEvents { get; init; }
        public IReadOnlyDictionary<string, int> EntriesByComponent { get; init; }
            = new Dictionary<string, int>();
        public string? ExportPath { get; init; }
    }

    /// <summary>
    /// Debug event for streaming.
    /// </summary>
    public record DebugEvent
    {
        public DebugEventType Type { get; init; }
        public string Component { get; init; } = "";
        public string Message { get; init; } = "";
        public IReadOnlyDictionary<string, object> Data { get; init; }
            = new Dictionary<string, object>();
        public DateTime Timestamp { get; init; }
    }

    public enum DebugEventType
    {
        MethodEntry,
        MethodExit,
        StateChange,
        SqlQuery,
        HttpRequest,
        HttpResponse,
        AgentDecision,
        MemorySnapshot,
        Custom
    }
}
```

### 3.5 ILogViewer

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Resilience.Logging
{
    /// <summary>
    /// Provides log viewing and search capabilities.
    /// </summary>
    public interface ILogViewer
    {
        /// <summary>
        /// Queries logs with filtering.
        /// </summary>
        /// <param name="query">Query parameters.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Query results.</returns>
        Task<LogQueryResult> QueryAsync(
            LogQuery query,
            CancellationToken ct = default);

        /// <summary>
        /// Gets a real-time log stream.
        /// </summary>
        /// <param name="filter">Optional filter for the stream.</param>
        /// <returns>Observable log stream.</returns>
        IObservable<LogEntry> GetLiveStream(LogFilter? filter = null);

        /// <summary>
        /// Exports logs to a file or stream.
        /// </summary>
        /// <param name="options">Export options.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Exported data stream.</returns>
        Task<Stream> ExportAsync(
            LogExportOptions options,
            CancellationToken ct = default);

        /// <summary>
        /// Gets log statistics for a period.
        /// </summary>
        /// <param name="period">Time period.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Log statistics.</returns>
        Task<LogStatistics> GetStatisticsAsync(
            TimeSpan period,
            CancellationToken ct = default);

        /// <summary>
        /// Gets log entry by ID.
        /// </summary>
        /// <param name="id">Entry ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Log entry or null.</returns>
        Task<LogEntry?> GetEntryAsync(
            LogEntryId id,
            CancellationToken ct = default);

        /// <summary>
        /// Gets related log entries by correlation ID.
        /// </summary>
        /// <param name="correlationId">Correlation ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Related log entries.</returns>
        Task<IReadOnlyList<LogEntry>> GetCorrelatedEntriesAsync(
            string correlationId,
            CancellationToken ct = default);

        /// <summary>
        /// Saves a query for reuse.
        /// </summary>
        /// <param name="name">Query name.</param>
        /// <param name="query">Query to save.</param>
        /// <param name="ct">Cancellation token.</param>
        Task SaveQueryAsync(
            string name,
            LogQuery query,
            CancellationToken ct = default);

        /// <summary>
        /// Gets saved queries.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task<IReadOnlyList<SavedQuery>> GetSavedQueriesAsync(
            CancellationToken ct = default);
    }

    /// <summary>
    /// Log query parameters.
    /// </summary>
    public record LogQuery
    {
        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }
        public IReadOnlyList<LogLevel>? Levels { get; init; }
        public IReadOnlyList<string>? Categories { get; init; }
        public string? SearchText { get; init; }
        public string? CorrelationId { get; init; }
        public string? SpanId { get; init; }
        public bool IncludeExceptions { get; init; }
        public IReadOnlyDictionary<string, string>? PropertyFilters { get; init; }
        public LogSortOrder SortOrder { get; init; } = LogSortOrder.TimestampDescending;
        public int MaxResults { get; init; } = 1000;
        public int Skip { get; init; }
    }

    public enum LogSortOrder
    {
        TimestampAscending,
        TimestampDescending,
        LevelAscending,
        LevelDescending
    }

    /// <summary>
    /// Log query result.
    /// </summary>
    public record LogQueryResult
    {
        public IReadOnlyList<LogEntry> Entries { get; init; } = Array.Empty<LogEntry>();
        public int TotalCount { get; init; }
        public int ReturnedCount { get; init; }
        public bool HasMore { get; init; }
        public TimeSpan QueryDuration { get; init; }
    }

    /// <summary>
    /// Log filter for streaming.
    /// </summary>
    public record LogFilter
    {
        public IReadOnlyList<LogLevel>? Levels { get; init; }
        public IReadOnlyList<string>? Categories { get; init; }
        public string? SearchPattern { get; init; }
    }

    /// <summary>
    /// Log export options.
    /// </summary>
    public record LogExportOptions
    {
        public LogQuery Query { get; init; } = new();
        public LogExportFormat Format { get; init; } = LogExportFormat.Json;
        public bool IncludeMetadata { get; init; } = true;
        public bool Compress { get; init; }
        public int? MaxEntries { get; init; }
    }

    public enum LogExportFormat
    {
        Json,
        JsonLines,
        Csv,
        Text,
        Html
    }

    /// <summary>
    /// Log statistics.
    /// </summary>
    public record LogStatistics
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public long TotalEntries { get; init; }
        public IReadOnlyDictionary<LogLevel, long> EntriesByLevel { get; init; }
            = new Dictionary<LogLevel, long>();
        public IReadOnlyDictionary<string, long> EntriesByCategory { get; init; }
            = new Dictionary<string, long>();
        public IReadOnlyList<TimeSeriesPoint> EntriesOverTime { get; init; }
            = Array.Empty<TimeSeriesPoint>();
        public int UniqueCorrelationIds { get; init; }
        public int UniqueExceptionTypes { get; init; }
        public IReadOnlyList<TopException> TopExceptions { get; init; }
            = Array.Empty<TopException>();
    }

    public record TimeSeriesPoint
    {
        public DateTime Timestamp { get; init; }
        public long Value { get; init; }
    }

    public record TopException
    {
        public string ExceptionType { get; init; } = "";
        public string Message { get; init; } = "";
        public long Count { get; init; }
        public DateTime LastOccurrence { get; init; }
    }

    /// <summary>
    /// Saved query definition.
    /// </summary>
    public record SavedQuery
    {
        public string Name { get; init; } = "";
        public LogQuery Query { get; init; } = new();
        public DateTime CreatedAt { get; init; }
        public DateTime LastUsed { get; init; }
        public int UseCount { get; init; }
    }
}
```

### 3.6 IDiagnosticExporter

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Resilience.Diagnostics
{
    /// <summary>
    /// Exports diagnostic information for support and debugging.
    /// </summary>
    public interface IDiagnosticExporter
    {
        /// <summary>
        /// Generates a diagnostic bundle.
        /// </summary>
        /// <param name="options">Bundle options.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Generated bundle.</returns>
        Task<DiagnosticBundle> GenerateBundleAsync(
            DiagnosticBundleOptions options,
            IProgress<ExportProgress>? progress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Exports bundle to a file.
        /// </summary>
        /// <param name="bundle">Bundle to export.</param>
        /// <param name="outputPath">Output file path.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Final file path.</returns>
        Task<string> ExportToFileAsync(
            DiagnosticBundle bundle,
            string outputPath,
            CancellationToken ct = default);

        /// <summary>
        /// Submits bundle to support.
        /// </summary>
        /// <param name="bundle">Bundle to submit.</param>
        /// <param name="options">Submission options.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Support ticket ID.</returns>
        Task<SupportTicketId> SubmitToSupportAsync(
            DiagnosticBundle bundle,
            SupportSubmissionOptions options,
            CancellationToken ct = default);

        /// <summary>
        /// Gets previous diagnostic bundles.
        /// </summary>
        /// <param name="limit">Maximum results.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<IReadOnlyList<DiagnosticBundleSummary>> GetPreviousBundlesAsync(
            int limit = 10,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes a diagnostic bundle.
        /// </summary>
        /// <param name="bundleId">Bundle ID.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteBundleAsync(
            DiagnosticBundleId bundleId,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Options for diagnostic bundle generation.
    /// </summary>
    public record DiagnosticBundleOptions
    {
        public bool IncludeLogs { get; init; } = true;
        public TimeSpan LogPeriod { get; init; } = TimeSpan.FromHours(24);
        public IReadOnlyList<LogLevel>? LogLevels { get; init; }
        public bool IncludeConfiguration { get; init; } = true;
        public bool IncludeSystemInfo { get; init; } = true;
        public bool IncludePerformanceData { get; init; } = true;
        public bool IncludeErrorHistory { get; init; } = true;
        public bool IncludeHealthStatus { get; init; } = true;
        public bool IncludeNetworkInfo { get; init; }
        public bool IncludeDatabaseInfo { get; init; }
        public bool SanitizeSensitiveData { get; init; } = true;
        public IReadOnlyList<string>? ExcludeCategories { get; init; }
        public bool CompressOutput { get; init; } = true;
        public string? Description { get; init; }
    }

    /// <summary>
    /// Generated diagnostic bundle.
    /// </summary>
    public record DiagnosticBundle
    {
        public DiagnosticBundleId Id { get; init; }
        public DateTime GeneratedAt { get; init; }
        public SystemInfo SystemInfo { get; init; } = new();
        public IReadOnlyList<LogEntry> Logs { get; init; } = Array.Empty<LogEntry>();
        public IReadOnlyDictionary<string, object> Configuration { get; init; }
            = new Dictionary<string, object>();
        public PerformanceSummary? PerformanceData { get; init; }
        public IReadOnlyList<ErrorSummary> ErrorHistory { get; init; }
            = Array.Empty<ErrorSummary>();
        public HealthReport? HealthStatus { get; init; }
        public long SizeBytes { get; init; }
        public string? FilePath { get; init; }
        public string Checksum { get; init; } = "";
    }

    public readonly record struct DiagnosticBundleId(Guid Value)
    {
        public static DiagnosticBundleId New() => new(Guid.NewGuid());
    }

    /// <summary>
    /// System information.
    /// </summary>
    public record SystemInfo
    {
        public string ApplicationVersion { get; init; } = "";
        public string RuntimeVersion { get; init; } = "";
        public string OperatingSystem { get; init; } = "";
        public string OsVersion { get; init; } = "";
        public string Architecture { get; init; } = "";
        public int ProcessorCount { get; init; }
        public long TotalMemoryBytes { get; init; }
        public long AvailableMemoryBytes { get; init; }
        public string MachineName { get; init; } = "";
        public string Culture { get; init; } = "";
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Local;
        public DateTime SystemUptime { get; init; }
        public DateTime ApplicationStartTime { get; init; }
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }
            = new Dictionary<string, string>();
        public IReadOnlyList<InstalledComponent> InstalledComponents { get; init; }
            = Array.Empty<InstalledComponent>();
    }

    public record InstalledComponent
    {
        public string Name { get; init; } = "";
        public string Version { get; init; } = "";
        public string? Location { get; init; }
    }

    /// <summary>
    /// Error summary for history.
    /// </summary>
    public record ErrorSummary
    {
        public string ErrorCode { get; init; } = "";
        public string Message { get; init; } = "";
        public int Occurrences { get; init; }
        public DateTime FirstOccurrence { get; init; }
        public DateTime LastOccurrence { get; init; }
        public string? StackTraceSample { get; init; }
    }

    /// <summary>
    /// Export progress information.
    /// </summary>
    public record ExportProgress
    {
        public string Phase { get; init; } = "";
        public double PercentComplete { get; init; }
        public string? CurrentItem { get; init; }
        public int ItemsProcessed { get; init; }
        public int TotalItems { get; init; }
    }

    /// <summary>
    /// Support submission options.
    /// </summary>
    public record SupportSubmissionOptions
    {
        public string Subject { get; init; } = "";
        public string Description { get; init; } = "";
        public SupportPriority Priority { get; init; } = SupportPriority.Normal;
        public string? ContactEmail { get; init; }
        public bool IncludeBundle { get; init; } = true;
        public IReadOnlyList<string>? AdditionalFiles { get; init; }
    }

    public enum SupportPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public readonly record struct SupportTicketId(string Value);

    /// <summary>
    /// Summary of a previous bundle.
    /// </summary>
    public record DiagnosticBundleSummary
    {
        public DiagnosticBundleId Id { get; init; }
        public DateTime GeneratedAt { get; init; }
        public long SizeBytes { get; init; }
        public string? Description { get; init; }
        public string? FilePath { get; init; }
        public bool WasSubmitted { get; init; }
        public SupportTicketId? TicketId { get; init; }
    }
}
```

---

## 4. Architecture Diagrams

### 4.1 Logging Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Structured Logging Pipeline                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                    │
│  │ Application │────▶│   Logger    │────▶│  Enrichment │                    │
│  │    Code     │     │   Factory   │     │  Pipeline   │                    │
│  └─────────────┘     └─────────────┘     └──────┬──────┘                    │
│                                                  │                           │
│                                                  ▼                           │
│  ┌───────────────────────────────────────────────────────────────────┐      │
│  │                      Log Entry Builder                             │      │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────────────┐  │      │
│  │  │ Timestamp │ │   Level   │ │  Message  │ │    Properties     │  │      │
│  │  └───────────┘ └───────────┘ └───────────┘ └───────────────────┘  │      │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────────────┐  │      │
│  │  │ Category  │ │ Exception │ │CorrelId   │ │     Scopes        │  │      │
│  │  └───────────┘ └───────────┘ └───────────┘ └───────────────────┘  │      │
│  └───────────────────────────────────────────────────────────────────┘      │
│                                    │                                         │
│                                    ▼                                         │
│  ┌───────────────────────────────────────────────────────────────────┐      │
│  │                      Processing Pipeline                           │      │
│  │                                                                    │      │
│  │   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────────┐  │      │
│  │   │  Filter  │──▶│ Sanitize │──▶│  Buffer  │──▶│   Dispatch   │  │      │
│  │   │  (Level) │   │  (PII)   │   │  (Async) │   │   (Sinks)    │  │      │
│  │   └──────────┘   └──────────┘   └──────────┘   └──────────────┘  │      │
│  └───────────────────────────────────────────────────────────────────┘      │
│                                    │                                         │
│            ┌───────────────────────┼───────────────────────┐                 │
│            ▼                       ▼                       ▼                 │
│  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐            │
│  │   Console Sink  │   │   File Sink     │   │  Database Sink  │            │
│  │  (Development)  │   │  (Rolling Log)  │   │  (PostgreSQL)   │            │
│  └─────────────────┘   └─────────────────┘   └─────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Telemetry Collection Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Telemetry Collection Flow                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                        Application Layer                              │   │
│  │                                                                       │   │
│  │   ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐   │   │
│  │   │ Events  │  │ Metrics │  │  Deps   │  │Requests │  │ Traces  │   │   │
│  │   │  Track  │  │  Track  │  │  Track  │  │  Track  │  │  Track  │   │   │
│  │   └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘   │   │
│  └────────┼────────────┼────────────┼────────────┼────────────┼────────┘   │
│           │            │            │            │            │             │
│           └────────────┴────────────┼────────────┴────────────┘             │
│                                     ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                      Telemetry Collector                              │   │
│  │                                                                       │   │
│  │   ┌─────────────┐   ┌─────────────┐   ┌─────────────────────────┐   │   │
│  │   │   Context   │   │  Sampling   │   │      Buffering          │   │   │
│  │   │  Injection  │──▶│   Filter    │──▶│  (BatchSize: 100)       │   │   │
│  │   │ (CorrelId)  │   │  (Rate %)   │   │  (FlushInterval: 30s)   │   │   │
│  │   └─────────────┘   └─────────────┘   └───────────┬─────────────┘   │   │
│  └───────────────────────────────────────────────────┼──────────────────┘   │
│                                                       │                      │
│                          ┌────────────────────────────┘                      │
│                          ▼                                                   │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │                         Export Pipeline                               │   │
│  │                                                                       │   │
│  │   ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐   │   │
│  │   │    Database     │   │   Metrics API   │   │  External APM   │   │   │
│  │   │   (Primary)     │   │   (Optional)    │   │   (Optional)    │   │   │
│  │   └─────────────────┘   └─────────────────┘   └─────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.3 Performance Tracking Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Performance Tracking Architecture                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Instrumentation Points                            │    │
│  │                                                                      │    │
│  │  using (var scope = tracker.StartMeasurement("ProcessDocument"))    │    │
│  │  {                                                                   │    │
│  │      scope.RecordCheckpoint("ParseStart");                          │    │
│  │      // ... parse document ...                                       │    │
│  │      scope.RecordCheckpoint("ParseComplete");                       │    │
│  │      // ... process document ...                                     │    │
│  │      scope.SetSuccess(true);                                         │    │
│  │  } // Auto-records duration on dispose                               │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     Metrics Collection                               │    │
│  │                                                                      │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │    │
│  │  │   Timings    │  │   Counters   │  │    Gauges    │               │    │
│  │  │  (Histogram) │  │   (Atomic)   │  │  (Snapshot)  │               │    │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘               │    │
│  │         │                 │                 │                        │    │
│  │         └─────────────────┼─────────────────┘                        │    │
│  │                           ▼                                          │    │
│  │  ┌───────────────────────────────────────────────────────────────┐  │    │
│  │  │                   Percentile Calculator                        │  │    │
│  │  │   p50: 12ms  │  p95: 45ms  │  p99: 120ms  │  max: 250ms       │  │    │
│  │  └───────────────────────────────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                    │                                         │
│                                    ▼                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                      Storage & Analysis                              │    │
│  │                                                                      │    │
│  │  ┌────────────────┐   ┌────────────────┐   ┌────────────────────┐  │    │
│  │  │  Time-Series   │   │   Aggregation  │   │     Alerting       │  │    │
│  │  │   Database     │──▶│   (Rollups)    │──▶│  (Thresholds)      │  │    │
│  │  │ (performance_  │   │ 1m/5m/1h/1d    │   │                    │  │    │
│  │  │   metrics)     │   │                │   │                    │  │    │
│  │  └────────────────┘   └────────────────┘   └────────────────────┘  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. PostgreSQL Schema

```sql
-- ============================================================================
-- DIAGNOSTICS & LOGGING SCHEMA
-- v0.19.4-RES
-- ============================================================================

-- Create schema
CREATE SCHEMA IF NOT EXISTS diagnostics;

-- ============================================================================
-- STRUCTURED LOGS (with partitioning)
-- ============================================================================

CREATE TABLE diagnostics.structured_logs (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    level SMALLINT NOT NULL,
    category VARCHAR(256) NOT NULL,
    message TEXT NOT NULL,
    message_template TEXT,
    properties JSONB DEFAULT '{}',
    exception_type VARCHAR(256),
    exception_message TEXT,
    exception_stack TEXT,
    correlation_id VARCHAR(64),
    span_id VARCHAR(32),
    parent_span_id VARCHAR(32),
    scopes TEXT[],
    machine_name VARCHAR(64),
    thread_id INTEGER,
    PRIMARY KEY (id, timestamp)
) PARTITION BY RANGE (timestamp);

-- Create partitions for next 12 months
CREATE TABLE diagnostics.logs_2025_01 PARTITION OF diagnostics.structured_logs
    FOR VALUES FROM ('2025-01-01') TO ('2025-02-01');
CREATE TABLE diagnostics.logs_2025_02 PARTITION OF diagnostics.structured_logs
    FOR VALUES FROM ('2025-02-01') TO ('2025-03-01');
-- ... continue for remaining months

-- Indexes
CREATE INDEX idx_logs_timestamp ON diagnostics.structured_logs(timestamp DESC);
CREATE INDEX idx_logs_level ON diagnostics.structured_logs(level);
CREATE INDEX idx_logs_category ON diagnostics.structured_logs(category);
CREATE INDEX idx_logs_correlation ON diagnostics.structured_logs(correlation_id);
CREATE INDEX idx_logs_exception ON diagnostics.structured_logs(exception_type)
    WHERE exception_type IS NOT NULL;
CREATE INDEX idx_logs_properties ON diagnostics.structured_logs USING GIN (properties);
CREATE INDEX idx_logs_fulltext ON diagnostics.structured_logs
    USING GIN (to_tsvector('english', message));

-- ============================================================================
-- TELEMETRY EVENTS
-- ============================================================================

CREATE TABLE diagnostics.telemetry_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(20) NOT NULL, -- event, metric, dependency, request, trace
    name VARCHAR(256) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    duration_ms DOUBLE PRECISION,
    success BOOLEAN,
    result_code VARCHAR(32),
    target VARCHAR(512),
    properties JSONB DEFAULT '{}',
    metrics JSONB DEFAULT '{}',
    correlation_id VARCHAR(64),
    operation_id VARCHAR(64),
    operation_name VARCHAR(256),
    parent_id VARCHAR(64)
) PARTITION BY RANGE (timestamp);

CREATE INDEX idx_telemetry_type ON diagnostics.telemetry_events(event_type);
CREATE INDEX idx_telemetry_name ON diagnostics.telemetry_events(name);
CREATE INDEX idx_telemetry_timestamp ON diagnostics.telemetry_events(timestamp DESC);
CREATE INDEX idx_telemetry_correlation ON diagnostics.telemetry_events(correlation_id);
CREATE INDEX idx_telemetry_operation ON diagnostics.telemetry_events(operation_id);

-- ============================================================================
-- PERFORMANCE METRICS
-- ============================================================================

CREATE TABLE diagnostics.performance_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_type VARCHAR(20) NOT NULL, -- timing, counter, gauge, histogram
    name VARCHAR(128) NOT NULL,
    value DOUBLE PRECISION NOT NULL,
    tags JSONB DEFAULT '{}',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (timestamp);

CREATE INDEX idx_perf_type ON diagnostics.performance_metrics(metric_type);
CREATE INDEX idx_perf_name ON diagnostics.performance_metrics(name);
CREATE INDEX idx_perf_timestamp ON diagnostics.performance_metrics(timestamp DESC);
CREATE INDEX idx_perf_tags ON diagnostics.performance_metrics USING GIN (tags);

-- Performance aggregations (materialized view)
CREATE MATERIALIZED VIEW diagnostics.performance_hourly AS
SELECT
    date_trunc('hour', timestamp) AS hour,
    name,
    metric_type,
    COUNT(*) AS count,
    AVG(value) AS avg_value,
    MIN(value) AS min_value,
    MAX(value) AS max_value,
    PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY value) AS p50,
    PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY value) AS p95,
    PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY value) AS p99
FROM diagnostics.performance_metrics
WHERE metric_type = 'timing'
GROUP BY date_trunc('hour', timestamp), name, metric_type;

CREATE UNIQUE INDEX idx_perf_hourly_pk
    ON diagnostics.performance_hourly(hour, name, metric_type);

-- ============================================================================
-- DEBUG SESSIONS
-- ============================================================================

CREATE TABLE diagnostics.debug_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(256) NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'active',
    global_options JSONB NOT NULL DEFAULT '{}',
    component_options JSONB NOT NULL DEFAULT '{}',
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    max_duration_minutes INTEGER,
    log_entries_count INTEGER DEFAULT 0,
    events_count INTEGER DEFAULT 0,
    export_path TEXT,
    created_by UUID
);

CREATE INDEX idx_debug_sessions_state ON diagnostics.debug_sessions(state);
CREATE INDEX idx_debug_sessions_started ON diagnostics.debug_sessions(started_at DESC);

-- ============================================================================
-- DIAGNOSTIC BUNDLES
-- ============================================================================

CREATE TABLE diagnostics.bundles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    generated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    options JSONB NOT NULL DEFAULT '{}',
    system_info JSONB NOT NULL DEFAULT '{}',
    size_bytes BIGINT NOT NULL,
    file_path TEXT,
    checksum VARCHAR(64) NOT NULL,
    description TEXT,
    was_submitted BOOLEAN DEFAULT FALSE,
    ticket_id VARCHAR(64),
    submitted_at TIMESTAMPTZ,
    created_by UUID
);

CREATE INDEX idx_bundles_generated ON diagnostics.bundles(generated_at DESC);
CREATE INDEX idx_bundles_submitted ON diagnostics.bundles(was_submitted);

-- ============================================================================
-- SAVED QUERIES
-- ============================================================================

CREATE TABLE diagnostics.saved_queries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(128) NOT NULL,
    query_definition JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_used_at TIMESTAMPTZ,
    use_count INTEGER DEFAULT 0,
    user_id UUID,
    is_shared BOOLEAN DEFAULT FALSE,
    UNIQUE(name, user_id)
);

CREATE INDEX idx_saved_queries_user ON diagnostics.saved_queries(user_id);
CREATE INDEX idx_saved_queries_shared ON diagnostics.saved_queries(is_shared);

-- ============================================================================
-- LOG RETENTION CONFIGURATION
-- ============================================================================

CREATE TABLE diagnostics.retention_config (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    log_level SMALLINT,
    category_pattern VARCHAR(256),
    retention_days INTEGER NOT NULL DEFAULT 30,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Default retention rules
INSERT INTO diagnostics.retention_config (log_level, retention_days) VALUES
    (5, 365),  -- Critical: 1 year
    (4, 180),  -- Error: 6 months
    (3, 90),   -- Warning: 3 months
    (2, 30),   -- Information: 1 month
    (1, 7),    -- Debug: 1 week
    (0, 1);    -- Trace: 1 day
```

---

## 6. UI Mockups

### 6.1 Log Viewer

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Log Viewer                                                        [_][□][X] │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─── Filters ───────────────────────────────────────────────────────────┐  │
│ │ Time: [Last 24 hours    ▼] Level: [≥ Information ▼] Category: [All ▼] │  │
│ │ Search: [                                                         🔍] │  │
│ │ Correlation ID: [                    ]  [Apply Filters] [Clear] [Save]│  │
│ └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─── Results (12,456 entries) ──────────────────────────────────────────┐  │
│ │ Timestamp            │ Level │ Category            │ Message          │  │
│ ├──────────────────────┼───────┼─────────────────────┼──────────────────┤  │
│ │ 2025-01-31 14:32:01  │ ⚠ WRN │ Lexichord.Agent     │ Agent timeout... │  │
│ │ 2025-01-31 14:32:00  │ ℹ INF │ Lexichord.Document  │ Saved document...│  │
│ │ 2025-01-31 14:31:58  │ 🔴ERR │ Lexichord.Network   │ Connection fail..│  │
│ │ 2025-01-31 14:31:55  │ ℹ INF │ Lexichord.Editor    │ Format applied...│  │
│ │ 2025-01-31 14:31:52  │ 🐛DBG │ Lexichord.Cache     │ Cache hit for ...│  │
│ │ 2025-01-31 14:31:50  │ ℹ INF │ Lexichord.Auth      │ User login suc...│  │
│ │                      │       │                     │                  │  │
│ └──────────────────────┴───────┴─────────────────────┴──────────────────┘  │
│  ◀ 1 2 3 4 5 ... 125 ▶                                    [Export ▼] [↻]   │
│                                                                             │
│ ┌─── Entry Details ─────────────────────────────────────────────────────┐  │
│ │ Timestamp: 2025-01-31 14:31:58.234 UTC                                │  │
│ │ Level: Error           Category: Lexichord.Network.HttpClient         │  │
│ │ Correlation ID: abc-123-def-456                                       │  │
│ │                                                                       │  │
│ │ Message:                                                              │  │
│ │ Connection failed to api.example.com: Connection timed out after 30s  │  │
│ │                                                                       │  │
│ │ Properties:                                                           │  │
│ │   Host: api.example.com                                               │  │
│ │   Port: 443                                                           │  │
│ │   Timeout: 30000                                                      │  │
│ │   RetryAttempt: 3                                                     │  │
│ │                                                                       │  │
│ │ Exception: System.Net.Sockets.SocketException                         │  │
│ │ [Show Stack Trace]                                                    │  │
│ │                                                                       │  │
│ │ [Find Related Logs]  [Copy Entry]  [Open in New Tab]                  │  │
│ └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 Performance Dashboard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Performance Dashboard                                             [_][□][X] │
├─────────────────────────────────────────────────────────────────────────────┤
│ Period: [Last Hour ▼]  Auto-refresh: [●] 30s    Last updated: 14:32:01 UTC │
│                                                                             │
│ ┌─── Overview ──────────────────────────────────────────────────────────┐  │
│ │  Operations      Avg Response     P95 Response      Error Rate        │  │
│ │  ┌─────────┐    ┌─────────┐      ┌─────────┐       ┌─────────┐       │  │
│ │  │  1,234  │    │  45ms   │      │  120ms  │       │  0.3%   │       │  │
│ │  │  /min   │    │  ↓ 5%   │      │  ↑ 2%   │       │  ↓ 0.1% │       │  │
│ │  └─────────┘    └─────────┘      └─────────┘       └─────────┘       │  │
│ └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─── Response Time Trend ───────────────────────────────────────────────┐  │
│ │    ms                                                                  │  │
│ │   200├──────────────────────────────────────────────────────────────  │  │
│ │      │         ╱╲                                                      │  │
│ │   150├────────╱──╲────────╱╲──────────────────────────────────────── │  │
│ │      │   ╱╲  ╱    ╲  ╱╲  ╱  ╲                                         │  │
│ │   100├──╱──╲╱──────╲╱──╲╱────╲─────╱╲────────────────────────────── │  │
│ │      │ ╱                      ╲╱╲╱  ╲╱╲╱╲                              │  │
│ │    50├╱─────────────────────────────────╲╱╲──────────────────────── │  │
│ │      └────────────────────────────────────────────────────────────▶  │  │
│ │      13:30    13:40    13:50    14:00    14:10    14:20    14:30     │  │
│ │                                                                        │  │
│ │      ── p50   ── p95   ── p99                                         │  │
│ └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─── Top Operations ────────────────────────────────────────────────────┐  │
│ │ Operation              │ Count │ Avg   │ P95   │ Errors │ Trend      │  │
│ ├────────────────────────┼───────┼───────┼───────┼────────┼────────────┤  │
│ │ ProcessDocument        │ 456   │ 123ms │ 250ms │ 2      │ ▃▅▇▅▃▁    │  │
│ │ QueryAgents            │ 892   │ 45ms  │ 89ms  │ 0      │ ▁▃▅▇▅▃    │  │
│ │ SaveDocument           │ 234   │ 67ms  │ 120ms │ 1      │ ▅▃▁▃▅▇    │  │
│ │ AuthenticateUser       │ 156   │ 34ms  │ 56ms  │ 0      │ ▁▁▃▃▅▅    │  │
│ │ SyncChanges            │ 78    │ 234ms │ 450ms │ 3      │ ▇▇▅▃▁▁    │  │
│ └────────────────────────┴───────┴───────┴───────┴────────┴────────────┘  │
│                                                                             │
│ ┌─── Resource Usage ────────────────────────────────────────────────────┐  │
│ │ CPU                    Memory                 Disk I/O                │  │
│ │ ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐      │  │
│ │ │ ████████░░  78% │   │ ██████████ 1.2G │   │ R: 45 MB/s      │      │  │
│ │ │                 │   │ / 2.0 GB        │   │ W: 12 MB/s      │      │  │
│ │ └─────────────────┘   └─────────────────┘   └─────────────────┘      │  │
│ └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.3 Diagnostic Export Wizard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Generate Diagnostic Bundle                                        [_][□][X] │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Step 2 of 4: Select Content                                               │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━                          │
│                                                                             │
│  ┌─── Include in Bundle ────────────────────────────────────────────────┐  │
│  │                                                                       │  │
│  │  [✓] Logs                                                            │  │
│  │      Period: [Last 24 hours ▼]                                       │  │
│  │      Levels: [✓] Critical [✓] Error [✓] Warning [✓] Info [ ] Debug   │  │
│  │                                                                       │  │
│  │  [✓] System Information                                              │  │
│  │      [✓] OS & Runtime  [✓] Hardware  [✓] Network  [ ] Environment   │  │
│  │                                                                       │  │
│  │  [✓] Configuration (sanitized)                                       │  │
│  │      [✓] Application  [✓] Plugins  [ ] User preferences             │  │
│  │                                                                       │  │
│  │  [✓] Performance Data                                                │  │
│  │      [✓] Metrics  [✓] Slow operations  [✓] Resource usage           │  │
│  │                                                                       │  │
│  │  [✓] Error History                                                   │  │
│  │      Last [50 ▼] errors with stack traces                           │  │
│  │                                                                       │  │
│  │  [✓] Health Status                                                   │  │
│  │      Current health check results                                    │  │
│  │                                                                       │  │
│  │  [ ] Database Information (may contain sensitive data)               │  │
│  │                                                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌─── Security ─────────────────────────────────────────────────────────┐  │
│  │  [✓] Automatically sanitize sensitive data                          │  │
│  │      - API keys, passwords, tokens will be redacted                  │  │
│  │      - Personal identifiers will be masked                           │  │
│  │                                                                       │  │
│  │  [✓] Compress bundle (reduces size by ~70%)                         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Estimated bundle size: ~15 MB                                             │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │                                                                      │  │
│  │         [◀ Back]                              [Next: Review ▶]       │  │
│  │                                                                      │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Dependency Chain

```
v0.19.4a (Structured Logging Engine)
    └── Core Platform Logging Abstractions

v0.19.4b (Telemetry Collection)
    └── v0.19.4a (Structured Logging Engine)

v0.19.4c (Performance Tracking)
    ├── v0.19.4a (Structured Logging Engine)
    └── v0.19.4b (Telemetry Collection)

v0.19.4d (Debug Mode System)
    ├── v0.19.4a (Structured Logging Engine)
    └── v0.19.4c (Performance Tracking)

v0.19.4e (Log Viewer & Search)
    └── v0.19.4a (Structured Logging Engine)

v0.19.4f (Diagnostic Export)
    ├── v0.19.4a (Structured Logging Engine)
    ├── v0.19.4b (Telemetry Collection)
    ├── v0.19.4c (Performance Tracking)
    └── v0.19.4e (Log Viewer & Search)
```

---

## 8. License Gating

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:----:|:---------:|:-----:|:----------:|
| Structured Logging | ✓ | ✓ | ✓ | ✓ |
| Console Log Sink | ✓ | ✓ | ✓ | ✓ |
| File Log Sink | ✓ | ✓ | ✓ | ✓ |
| Database Log Sink | - | ✓ | ✓ | ✓ |
| Telemetry Collection | Basic | Full | Full | Full |
| Performance Tracking | Basic | Full | Full | Full |
| Debug Mode | - | ✓ | ✓ | ✓ |
| Log Viewer | 1 day | 7 days | 30 days | Unlimited |
| Log Search | Basic | Full | Full | Full |
| Diagnostic Export | - | ✓ | ✓ | ✓ |
| Support Submission | - | - | ✓ | ✓ |
| Log Retention | 7 days | 30 days | 90 days | Custom |
| External APM Integration | - | - | - | ✓ |
| Custom Log Sinks | - | - | - | ✓ |

---

## 9. Performance Targets

| Metric | Target | Critical Threshold |
|:-------|:-------|:-------------------|
| Log write latency (async) | < 1ms | < 5ms |
| Log query latency (1000 results) | < 100ms | < 500ms |
| Full-text search latency | < 200ms | < 1s |
| Telemetry flush latency | < 500ms | < 2s |
| Performance scope overhead | < 0.5ms | < 2ms |
| Debug mode overhead | < 5% CPU | < 15% CPU |
| Diagnostic export (24h logs) | < 60s | < 180s |
| Log viewer initial load | < 500ms | < 2s |
| Memory per 10K log entries | < 50MB | < 100MB |
| Telemetry batch processing | < 100ms | < 500ms |

---

## 10. Testing Strategy

### 10.1 Unit Testing

| Component | Test Count | Coverage Target |
|:----------|:-----------|:----------------|
| Structured Logging | 45 | 95% |
| Telemetry Collection | 40 | 90% |
| Performance Tracking | 35 | 90% |
| Debug Mode | 30 | 85% |
| Log Viewer | 35 | 90% |
| Diagnostic Export | 25 | 85% |
| **Total** | **210** | **90%** |

### 10.2 Integration Testing

- Log pipeline end-to-end (write → store → query)
- Telemetry batching and flush
- Performance metric aggregation
- Debug session lifecycle
- Export bundle generation
- Log retention enforcement

### 10.3 Performance Testing

- Sustained logging throughput (10K entries/sec)
- Query performance under load
- Memory stability during extended logging
- Debug mode CPU impact measurement

---

## 11. Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|:-----|:------------|:-------|:-----------|
| Logging performance impact | Medium | High | Async processing, sampling, buffering |
| Log storage growth | High | Medium | Retention policies, compression, partitioning |
| Sensitive data exposure | Medium | Critical | Auto-sanitization, audit, encryption |
| Debug mode left enabled | Medium | Medium | Auto-disable timer, UI warnings |
| Query timeout on large data | Medium | Low | Query limits, pagination, indexes |
| Telemetry data loss | Low | Medium | Retry logic, local buffer, acknowledgment |

---

## 12. MediatR Events

```csharp
// Log Events
public record LogEntryWrittenEvent(
    LogEntry Entry,
    string SinkName
) : INotification;

public record LogLevelChangedEvent(
    string Category,
    LogLevel OldLevel,
    LogLevel NewLevel
) : INotification;

// Telemetry Events
public record TelemetryFlushedEvent(
    int EventCount,
    int MetricCount,
    int DependencyCount,
    int RequestCount,
    TimeSpan FlushDuration
) : INotification;

// Performance Events
public record SlowOperationDetectedEvent(
    SlowOperation Operation,
    TimeSpan Threshold
) : INotification;

public record PerformanceAlertRaisedEvent(
    PerformanceAlert Alert
) : INotification;

// Debug Events
public record DebugModeToggledEvent(
    bool Enabled,
    DebugModeOptions? Options,
    UserId? ToggledBy
) : INotification;

public record DebugSessionStartedEvent(
    DebugSession Session
) : INotification;

public record DebugSessionEndedEvent(
    DebugSessionSummary Summary
) : INotification;

// Diagnostic Events
public record DiagnosticBundleCreatedEvent(
    DiagnosticBundle Bundle
) : INotification;

public record DiagnosticBundleSubmittedEvent(
    DiagnosticBundleId BundleId,
    SupportTicketId TicketId
) : INotification;
```

---

## 13. Acceptance Criteria Summary

### Functional Requirements

- [ ] Structured logs capture timestamp, level, category, message, and properties
- [ ] Correlation IDs propagate across all async operations
- [ ] Telemetry collects events, metrics, dependencies, and requests
- [ ] Performance tracking provides p50, p95, p99 percentiles
- [ ] Debug mode enables verbose logging per component
- [ ] Log viewer supports filtering, search, and real-time streaming
- [ ] Diagnostic export generates comprehensive support bundles

### Non-Functional Requirements

- [ ] Log write latency < 1ms (async)
- [ ] Log query latency < 100ms for 1000 results
- [ ] Debug mode overhead < 5% CPU
- [ ] 90%+ unit test coverage
- [ ] All sensitive data automatically sanitized
- [ ] Log retention enforced per configuration

---

## Document Revision

| Version | Date | Author | Changes |
|:--------|:-----|:-------|:--------|
| 1.0.0 | 2025-01-31 | Architecture Team | Initial comprehensive scope breakdown |
