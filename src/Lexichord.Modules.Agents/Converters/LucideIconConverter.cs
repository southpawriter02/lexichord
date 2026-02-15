// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Lexichord.Modules.Agents.Converters;

/// <summary>
/// Converts Lucide icon names (e.g. "edit-3", "file-text") to StreamGeometry resources.
/// </summary>
public sealed class LucideIconConverter : IValueConverter
{
    /// <summary>
    /// Gets the singleton instance of the converter.
    /// </summary>
    public static LucideIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string iconName || string.IsNullOrWhiteSpace(iconName))
        {
            return GetDefaultIcon();
        }

        // Convert kebab-case to PascalCase and append "Icon"
        // e.g. "file-text" -> "FileTextIcon"
        string resourceKey = KebabToPascalCase(iconName) + "Icon";

        // Try exact match
        if (TryGetIconResource(resourceKey, out var geometry))
        {
            return geometry;
        }

        // Try stripping numeric suffix if present (e.g. "Edit3Icon" -> "EditIcon")
        // Check if the base name ends with a digit
        // resourceKey ends with "Icon" (4 chars). Check char before "Icon".
        if (resourceKey.Length > 4 && char.IsDigit(resourceKey[^5]))
        {
            // Simple heuristic: remove trailing digits from the name part
            // "Edit3Icon" -> "EditIcon"
            // Find where digits start from the end of the name part
            int iconSuffixLen = 4; // "Icon"
            int nameLen = resourceKey.Length - iconSuffixLen;
            int i = nameLen - 1;

            // Scan backwards to find the start of the numeric suffix
            while (i >= 0 && char.IsDigit(resourceKey[i]))
            {
                i--;
            }

            // If we found digits and there is still a name prefix
            if (i < nameLen - 1)
            {
                string baseKey = resourceKey.Substring(0, i + 1) + "Icon";
                if (TryGetIconResource(baseKey, out var baseGeometry))
                {
                    return baseGeometry;
                }
            }
        }

        return GetDefaultIcon();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static bool TryGetIconResource(string key, out object? resource)
    {
        resource = null;
        if (Application.Current == null) return false;

        // Try application resources first
        return Application.Current.TryGetResource(key, null, out resource);
    }

    private static object? GetDefaultIcon()
    {
        // Fallback to BotIcon or null
        if (TryGetIconResource("BotIcon", out var defaultIcon))
        {
            return defaultIcon;
        }
        return null;
    }

    private static string KebabToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new System.Text.StringBuilder();
        bool capitalizeNext = true;

        foreach (char c in input)
        {
            if (c == '-')
            {
                capitalizeNext = true;
            }
            else
            {
                if (capitalizeNext)
                {
                    sb.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                    capitalizeNext = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        return sb.ToString();
    }
}
