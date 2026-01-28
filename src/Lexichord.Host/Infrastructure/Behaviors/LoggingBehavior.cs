using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Lexichord.Abstractions.Attributes;
using Lexichord.Host.Infrastructure.Options;

namespace Lexichord.Host.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that automatically logs all MediatR requests.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// LOGIC: This behavior wraps all request handlers and provides:
///
/// 1. **Start Logging**: Logs when a request begins with type and properties.
/// 2. **Completion Logging**: Logs when a request completes with duration.
/// 3. **Slow Request Warning**: Warns when requests exceed configured threshold.
/// 4. **Exception Logging**: Logs failures with exception details.
/// 5. **Sensitive Data Redaction**: Hides properties marked with [SensitiveData].
///
/// Pipeline Position: OUTERMOST (first to see request, last to see response)
/// This ensures we capture the total request duration including all behaviors.
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly LoggingBehaviorOptions _options;

    private static readonly string RequestTypeName = typeof(TRequest).Name;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the LoggingBehavior.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Configuration options.</param>
    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IOptions<LoggingBehaviorOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new LoggingBehaviorOptions();
    }

    /// <summary>
    /// Handles the request by logging before and after handler execution.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // LOGIC: Check if this request type is excluded from logging
        if (_options.ExcludedRequestTypes.Contains(RequestTypeName))
        {
            return await next();
        }

        // Extract correlation ID if present
        var correlationId = ExtractCorrelationId(request);

        // Log request start
        LogRequestStart(request, correlationId);

        // Start timing
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Execute the pipeline (next behavior or handler)
            var response = await next();

            stopwatch.Stop();

            // Log successful completion
            LogRequestCompletion(correlationId, stopwatch.ElapsedMilliseconds, response);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log failure
            LogRequestFailure(correlationId, stopwatch.ElapsedMilliseconds, ex);

            throw; // Re-throw to let exception propagate
        }
    }

    /// <summary>
    /// Logs the start of request handling.
    /// </summary>
    private void LogRequestStart(TRequest request, string? correlationId)
    {
        if (correlationId is not null)
        {
            _logger.LogDebug(
                "Handling {RequestType} [CorrelationId: {CorrelationId}]",
                RequestTypeName,
                correlationId);
        }
        else
        {
            _logger.LogDebug("Handling {RequestType}", RequestTypeName);
        }

        // Log request properties if enabled
        if (_options.LogRequestProperties && _logger.IsEnabled(LogLevel.Debug))
        {
            var redactedProperties = GetRedactedProperties(request);
            _logger.LogDebug(
                "Request: {RequestProperties}",
                redactedProperties);
        }
    }

    /// <summary>
    /// Logs successful request completion.
    /// </summary>
    private void LogRequestCompletion(
        string? correlationId,
        long elapsedMs,
        TResponse? response)
    {
        // Check for slow request
        if (elapsedMs > _options.SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request: {RequestType} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                RequestTypeName,
                elapsedMs,
                _options.SlowRequestThresholdMs);
        }

        // Log completion
        _logger.LogInformation(
            "Handled {RequestType} in {ElapsedMs}ms",
            RequestTypeName,
            elapsedMs);

        // Log response if enabled
        if (_options.LogResponseProperties &&
            response is not null &&
            _logger.IsEnabled(LogLevel.Debug))
        {
            var redactedResponse = GetRedactedProperties(response);
            _logger.LogDebug(
                "Response: {ResponseProperties}",
                redactedResponse);
        }
    }

    /// <summary>
    /// Logs request failure.
    /// </summary>
    private void LogRequestFailure(
        string? correlationId,
        long elapsedMs,
        Exception exception)
    {
        if (_options.LogFullExceptions)
        {
            _logger.LogWarning(
                exception,
                "Request {RequestType} failed after {ElapsedMs}ms: {ExceptionMessage}",
                RequestTypeName,
                elapsedMs,
                exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "Request {RequestType} failed after {ElapsedMs}ms: {ExceptionType} - {ExceptionMessage}",
                RequestTypeName,
                elapsedMs,
                exception.GetType().Name,
                exception.Message);
        }
    }

    /// <summary>
    /// Extracts correlation ID from the request if present.
    /// </summary>
    private static string? ExtractCorrelationId(TRequest request)
    {
        // LOGIC: Look for common correlation ID property names
        var type = typeof(TRequest);
        var property = type.GetProperty("CorrelationId") ??
                       type.GetProperty("RequestId") ??
                       type.GetProperty("TraceId");

        return property?.GetValue(request)?.ToString();
    }

    /// <summary>
    /// Creates a dictionary of properties with sensitive values redacted.
    /// </summary>
    private static string GetRedactedProperties<T>(T obj)
    {
        if (obj is null)
            return "null";

        try
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new Dictionary<string, object?>();

            foreach (var property in properties)
            {
                // Skip properties marked with [NoLog]
                if (property.GetCustomAttribute<NoLogAttribute>() is not null)
                    continue;

                var value = property.GetValue(obj);

                // Redact properties marked with [SensitiveData]
                var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
                if (sensitiveAttr is not null)
                {
                    result[property.Name] = sensitiveAttr.RedactedText;
                    continue;
                }

                // Handle common sensitive property names even without attribute
                if (IsPotentiallySensitiveName(property.Name))
                {
                    result[property.Name] = "[REDACTED]";
                    continue;
                }

                // Truncate long strings
                if (value is string stringValue && stringValue.Length > 100)
                {
                    result[property.Name] = stringValue[..100] + "...";
                    continue;
                }

                result[property.Name] = value;
            }

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch
        {
            return "[Serialization Failed]";
        }
    }

    /// <summary>
    /// Checks if a property name suggests sensitive data.
    /// </summary>
    private static bool IsPotentiallySensitiveName(string propertyName)
    {
        var lowerName = propertyName.ToLowerInvariant();
        return lowerName.Contains("password") ||
               lowerName.Contains("secret") ||
               lowerName.Contains("token") ||
               lowerName.Contains("apikey") ||
               lowerName.Contains("api_key") ||
               lowerName.Contains("credential") ||
               lowerName.Contains("connectionstring") ||
               lowerName.Contains("privatekey");
    }
}
