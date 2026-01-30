using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Expands message templates with context values from scan matches.
/// </summary>
/// <remarks>
/// LOGIC: Supports flexible message templates with placeholders:
/// - {0} or {text}: The matched text
/// - {suggestion}: The suggested replacement
/// - {rule}: The rule name
/// - {severity}: The severity level
/// - Named capture groups from regex patterns
///
/// Example: "Avoid '{text}', consider using '{suggestion}' instead."
///
/// Version: v0.2.3d
/// </remarks>
internal static class MessageTemplateEngine
{
    private static readonly Regex PlaceholderPattern = new(
        @"\{(\w+)\}",
        RegexOptions.Compiled);

    /// <summary>
    /// Expands a message template with context values.
    /// </summary>
    /// <param name="template">The message template with placeholders.</param>
    /// <param name="match">The scan match providing context values.</param>
    /// <param name="additionalValues">Optional additional values to substitute.</param>
    /// <returns>The expanded message with placeholders replaced.</returns>
    /// <remarks>
    /// LOGIC: Priority for placeholder resolution:
    /// 1. Additional values (passed explicitly)
    /// 2. Capture groups from regex match
    /// 3. Built-in context (text, suggestion, rule, severity)
    ///
    /// Unknown placeholders are preserved unchanged.
    /// </remarks>
    public static string Expand(
        string template,
        ScanMatch match,
        IReadOnlyDictionary<string, string>? additionalValues = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        // LOGIC: Build context dictionary with built-in values
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = match.MatchedText,
            ["text"] = match.MatchedText,
            ["suggestion"] = match.Rule.Suggestion ?? string.Empty,
            ["rule"] = match.Rule.Name,
            ["severity"] = match.Rule.DefaultSeverity.ToString()
        };

        // LOGIC: Add capture groups from regex match
        if (match.CaptureGroups is not null)
        {
            foreach (var (key, value) in match.CaptureGroups)
            {
                context[key] = value;
            }
        }

        // LOGIC: Add caller-provided values (highest priority)
        if (additionalValues is not null)
        {
            foreach (var (key, value) in additionalValues)
            {
                context[key] = value;
            }
        }

        // LOGIC: Replace placeholders with context values
        return PlaceholderPattern.Replace(template, m =>
        {
            var placeholder = m.Groups[1].Value;
            return context.TryGetValue(placeholder, out var value) ? value : m.Value;
        });
    }

    /// <summary>
    /// Checks if a template contains any placeholders.
    /// </summary>
    /// <param name="template">The template to check.</param>
    /// <returns>True if the template has placeholders.</returns>
    public static bool HasPlaceholders(string template)
    {
        return !string.IsNullOrEmpty(template) && PlaceholderPattern.IsMatch(template);
    }
}
