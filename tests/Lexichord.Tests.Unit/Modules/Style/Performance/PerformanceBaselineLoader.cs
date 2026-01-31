// <copyright file="PerformanceBaselineLoader.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Loads performance baseline configurations from JSON files.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.8d - Manages baseline data storage and retrieval.
/// Falls back to default baselines if file is not found.
///
/// File Resolution:
/// 1. Searches for Baselines/baseline.json relative to test assembly
/// 2. Falls back to DefaultBaselines.Default if not found
///
/// Thread Safety:
/// - Thread-safe for read operations
/// - Write operations should be serialized externally
///
/// Version: v0.3.8d
/// </remarks>
public static class PerformanceBaselineLoader
{
    /// <summary>
    /// JSON serialization options for baseline files.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets the default baseline file path.
    /// </summary>
    /// <remarks>
    /// LOGIC: Searches for baseline.json in the Baselines directory
    /// relative to the test assembly location.
    /// </remarks>
    public static string DefaultBaselinePath
    {
        get
        {
            var assemblyPath = typeof(PerformanceBaselineLoader).Assembly.Location;
            var baseDir = Path.GetDirectoryName(assemblyPath) ?? Environment.CurrentDirectory;

            // LOGIC: Search upward for Baselines directory
            var current = baseDir;
            while (current != null)
            {
                var baselinesPath = Path.Combine(current, "Baselines", "baseline.json");
                if (File.Exists(baselinesPath))
                {
                    return baselinesPath;
                }

                // LOGIC: Check for test project root marker
                var projectFile = Directory.GetFiles(current, "*.csproj").FirstOrDefault();
                if (projectFile != null && projectFile.Contains("Tests"))
                {
                    var testBaselinesPath = Path.Combine(
                        Path.GetDirectoryName(projectFile)!,
                        "Modules", "Style", "Performance", "Baselines", "baseline.json");

                    if (File.Exists(testBaselinesPath))
                    {
                        return testBaselinesPath;
                    }
                }

                current = Path.GetDirectoryName(current);
            }

            // LOGIC: Default to expected location
            return Path.Combine(
                baseDir,
                "Modules", "Style", "Performance", "Baselines", "baseline.json");
        }
    }

    /// <summary>
    /// Loads performance baseline from the default location.
    /// </summary>
    /// <returns>Loaded baseline or defaults if file not found.</returns>
    public static PerformanceBaseline Load()
    {
        return Load(DefaultBaselinePath);
    }

    /// <summary>
    /// Loads performance baseline from the specified path.
    /// </summary>
    /// <param name="path">Path to the baseline JSON file.</param>
    /// <returns>Loaded baseline or defaults if file not found.</returns>
    /// <remarks>
    /// LOGIC: Returns DefaultBaselines.Default if:
    /// - File does not exist
    /// - File cannot be parsed
    /// - Any exception occurs during loading
    /// </remarks>
    public static PerformanceBaseline Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return DefaultBaselines.Default;
            }

            var json = File.ReadAllText(path);
            var baseline = JsonSerializer.Deserialize<PerformanceBaseline>(json, SerializerOptions);

            return baseline ?? DefaultBaselines.Default;
        }
        catch
        {
            return DefaultBaselines.Default;
        }
    }

    /// <summary>
    /// Saves performance baseline to the specified path.
    /// </summary>
    /// <param name="baseline">The baseline to save.</param>
    /// <param name="path">Target path for the JSON file.</param>
    /// <remarks>
    /// LOGIC: Creates the directory structure if it doesn't exist.
    /// Overwrites existing file content.
    /// </remarks>
    public static void Save(PerformanceBaseline baseline, string? path = null)
    {
        path ??= DefaultBaselinePath;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(baseline, SerializerOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Gets the threshold for a specific operation and word count.
    /// </summary>
    /// <param name="baseline">The baseline configuration.</param>
    /// <param name="operation">Operation name (Readability, FuzzyScanning, VoiceAnalysis, FullPipeline).</param>
    /// <param name="wordCount">Word count tier (1000, 10000, or 50000).</param>
    /// <returns>Threshold in milliseconds.</returns>
    /// <exception cref="ArgumentException">If operation is not recognized.</exception>
    public static int GetThreshold(PerformanceBaseline baseline, string operation, int wordCount)
    {
        var thresholds = operation.ToLowerInvariant() switch
        {
            "readability" => baseline.Readability,
            "fuzzyscanning" or "fuzzy" => baseline.FuzzyScanning,
            "voiceanalysis" or "voice" => baseline.VoiceAnalysis,
            "fullpipeline" or "pipeline" => baseline.FullPipeline,
            _ => throw new ArgumentException($"Unknown operation: {operation}", nameof(operation))
        };

        return wordCount switch
        {
            <= 1000 => thresholds.Words1K,
            <= 10000 => thresholds.Words10K,
            _ => thresholds.Words50K
        };
    }

    /// <summary>
    /// Checks if a measurement exceeds the regression threshold.
    /// </summary>
    /// <param name="baseline">The baseline configuration.</param>
    /// <param name="operation">Operation name.</param>
    /// <param name="wordCount">Word count tier.</param>
    /// <param name="actualMs">Actual measured time in milliseconds.</param>
    /// <returns>True if the measurement exceeds threshold + regression allowance.</returns>
    public static bool IsRegression(
        PerformanceBaseline baseline,
        string operation,
        int wordCount,
        double actualMs)
    {
        var threshold = GetThreshold(baseline, operation, wordCount);
        var allowedMax = threshold * (1 + baseline.RegressionThreshold);
        return actualMs > allowedMax;
    }
}
