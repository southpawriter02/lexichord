namespace Lexichord.Abstractions.Attributes;

/// <summary>
/// Marks a property as containing sensitive data that should be redacted from logs.
/// </summary>
/// <remarks>
/// LOGIC: When the LoggingBehavior serializes request properties for logging,
/// properties marked with this attribute will have their values replaced with
/// "[REDACTED]" to prevent sensitive data from appearing in log files.
///
/// Use this attribute on properties containing:
/// - Passwords or secrets
/// - API keys or tokens
/// - Personal identifiable information (PII)
/// - Financial data
/// - Health information
///
/// Example:
/// <code>
/// public record CreateUserCommand : ICommand&lt;UserId&gt;
/// {
///     public string Username { get; init; }
///
///     [SensitiveData]
///     public string Password { get; init; }
///
///     [SensitiveData]
///     public string Email { get; init; }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveDataAttribute : Attribute
{
    /// <summary>
    /// The text to display in logs instead of the actual value.
    /// </summary>
    /// <remarks>
    /// LOGIC: Defaults to "[REDACTED]" but can be customized for context.
    /// Example: "[API_KEY]", "[PASSWORD]", "[EMAIL]"
    /// </remarks>
    public string RedactedText { get; }

    /// <summary>
    /// Initializes a new instance of the SensitiveDataAttribute.
    /// </summary>
    /// <param name="redactedText">The text to show in logs instead of the value.</param>
    public SensitiveDataAttribute(string redactedText = "[REDACTED]")
    {
        RedactedText = redactedText;
    }
}
