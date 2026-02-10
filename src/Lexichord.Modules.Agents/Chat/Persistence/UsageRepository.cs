// -----------------------------------------------------------------------
// <copyright file="UsageRepository.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using Lexichord.Modules.Agents.Chat.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.Persistence;

/// <summary>
/// Repository for usage record persistence and queries.
/// </summary>
/// <remarks>
/// <para>
/// Uses in-memory storage with thread-safe access for usage records.
/// This approach avoids introducing EF Core as a dependency in the
/// Agents module, consistent with the module's lightweight persistence
/// strategy (cf. v0.6.6c custom agent persistence via ISettingsService).
/// </para>
/// <para>
/// Records are maintained for the current application session. For
/// cross-session persistence, the monthly summaries can be exported
/// via <see cref="ExportAsync"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6d as part of the Usage Tracking feature.
/// </para>
/// </remarks>
public class UsageRepository
{
    private readonly List<UsageRecord> _records = new();
    private readonly object _lock = new();
    private readonly ILogger<UsageRepository> _logger;
    private long _nextId = 1;

    /// <summary>
    /// Initializes a new instance of <see cref="UsageRepository"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public UsageRepository(ILogger<UsageRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a usage entry.
    /// </summary>
    /// <param name="record">The usage record to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Assigns an auto-incrementing ID and adds the record
    /// to the in-memory store under lock for thread safety.
    /// </remarks>
    public virtual Task RecordAsync(UsageRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_lock)
        {
            record.Id = _nextId++;
            _records.Add(record);
        }

        _logger.LogTrace("Persisted usage record: {Id}", record.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets monthly summary for the specified month.
    /// </summary>
    /// <param name="month">The month to aggregate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated monthly usage summary.</returns>
    /// <remarks>
    /// LOGIC: Filters records by computed month key (Year*100+Month),
    /// then groups by AgentId and Model for breakdown summaries.
    /// </remarks>
    public virtual Task<MonthlyUsageSummary> GetMonthlySummaryAsync(
        DateOnly month,
        CancellationToken ct = default)
    {
        var monthKey = month.Month + (month.Year * 100);

        List<UsageRecord> records;
        lock (_lock)
        {
            records = _records.Where(r => r.Month == monthKey).ToList();
        }

        // LOGIC: Group by agent for per-agent breakdown.
        var byAgent = records
            .GroupBy(r => r.AgentId)
            .ToDictionary(
                g => g.Key,
                g => new AgentUsageSummary(
                    g.Key,
                    g.Count(),
                    g.Sum(r => r.PromptTokens + r.CompletionTokens),
                    g.Sum(r => r.EstimatedCost)));

        // LOGIC: Group by model for per-model breakdown.
        var byModel = records
            .GroupBy(r => r.Model)
            .ToDictionary(
                g => g.Key,
                g => new ModelUsageSummary(
                    g.Key,
                    g.Count(),
                    g.Sum(r => r.PromptTokens + r.CompletionTokens),
                    g.Sum(r => r.EstimatedCost)));

        var summary = new MonthlyUsageSummary(
            month,
            records.Count,
            records.Sum(r => r.PromptTokens),
            records.Sum(r => r.CompletionTokens),
            records.Sum(r => r.EstimatedCost),
            byAgent,
            byModel);

        _logger.LogDebug("Retrieved monthly summary for {Month}", month);

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Exports usage data for a date range.
    /// </summary>
    /// <param name="startDate">Start of export period.</param>
    /// <param name="endDate">End of export period.</param>
    /// <param name="format">Export format (CSV or JSON).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A stream containing the exported data.</returns>
    /// <remarks>
    /// LOGIC: Filters records by timestamp range, then serializes
    /// to the requested format. The returned stream is positioned
    /// at the beginning for immediate reading.
    /// </remarks>
    public virtual Task<Stream> ExportAsync(
        DateOnly startDate,
        DateOnly endDate,
        ExportFormat format,
        CancellationToken ct = default)
    {
        var start = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        List<UsageRecord> records;
        lock (_lock)
        {
            records = _records
                .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                .OrderBy(r => r.Timestamp)
                .ToList();
        }

        Stream stream = format switch
        {
            ExportFormat.Csv => ExportToCsv(records),
            ExportFormat.Json => ExportToJson(records),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        _logger.LogInformation(
            "Exported {Count} usage records in {Format} format",
            records.Count, format);

        return Task.FromResult(stream);
    }

    /// <summary>
    /// Exports records to CSV format.
    /// </summary>
    private static Stream ExportToCsv(List<UsageRecord> records)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, Encoding.UTF8);

        writer.WriteLine(
            "Timestamp,ConversationId,AgentId,Model,PromptTokens,CompletionTokens,Cost,Duration,Streamed");

        foreach (var r in records)
        {
            writer.WriteLine(
                $"{r.Timestamp:O},{r.ConversationId},{r.AgentId},{r.Model}," +
                $"{r.PromptTokens},{r.CompletionTokens},{r.EstimatedCost}," +
                $"{r.Duration.TotalSeconds},{r.Streamed}");
        }

        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Exports records to JSON format.
    /// </summary>
    private static Stream ExportToJson(List<UsageRecord> records)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, records, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        stream.Position = 0;
        return stream;
    }
}
