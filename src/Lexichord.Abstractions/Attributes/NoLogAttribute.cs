namespace Lexichord.Abstractions.Attributes;

/// <summary>
/// Marks a property to be completely excluded from log output.
/// </summary>
/// <remarks>
/// LOGIC: Unlike SensitiveData which shows "[REDACTED]", NoLog completely
/// omits the property from the logged object. Use this for:
///
/// - Large binary data (file contents)
/// - Calculated/derived properties not useful for debugging
/// - Properties that would clutter logs without adding value
///
/// Example:
/// <code>
/// public record UploadFileCommand : ICommand
/// {
///     public string FileName { get; init; }
///
///     [NoLog]
///     public byte[] FileContents { get; init; }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NoLogAttribute : Attribute
{
}
