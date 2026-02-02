// =============================================================================
// File: StringEqualsConverter.cs
// Project: Lexichord.Modules.RAG
// Description: Value converter for comparing string equality (used in style classes).
// =============================================================================
// LOGIC: Compares bound value to ConverterParameter and returns true if equal.
//   Used with Avalonia's Classes.ClassName binding to apply dynamic styles based
//   on string property values (e.g., ScoreColor -> badge styling).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.6b: Initial implementation for score badge styling
// =============================================================================

using System.Globalization;
using Avalonia.Data.Converters;

namespace Lexichord.Modules.RAG.Converters;

/// <summary>
/// Compares a bound string value to a converter parameter for equality.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StringEqualsConverter"/> enables dynamic style class application
/// based on string property values. Returns <c>true</c> if the bound value
/// equals the <c>ConverterParameter</c> (case-sensitive).
/// </para>
/// <para>
/// <b>Usage in XAML:</b>
/// <code>
/// Classes.HighRelevance="{Binding ScoreColor, 
///     Converter={StaticResource StringEqualsConverter}, 
///     ConverterParameter=HighRelevance}"
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6b for score badge color styling.
/// </para>
/// </remarks>
public class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// Converts a value to a boolean indicating equality with the parameter.
    /// </summary>
    /// <param name="value">The bound value to compare.</param>
    /// <param name="targetType">The target type (should be bool).</param>
    /// <param name="parameter">The string to compare against.</param>
    /// <param name="culture">Culture info (not used).</param>
    /// <returns>
    /// <c>true</c> if <paramref name="value"/> equals <paramref name="parameter"/>
    /// as strings (case-sensitive); otherwise, <c>false</c>.
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var valueString = value.ToString();
        var parameterString = parameter.ToString();

        return string.Equals(valueString, parameterString, StringComparison.Ordinal);
    }

    /// <summary>
    /// Not implemented. Reverse conversion is not supported.
    /// </summary>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
