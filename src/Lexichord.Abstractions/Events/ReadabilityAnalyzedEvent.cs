// <copyright file="ReadabilityAnalyzedEvent.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when readability analysis completes for a document.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3c - MediatR notification for UI updates after readability analysis.
/// Subscribers can update HUD widgets, status bars, or other UI components
/// without tight coupling to the readability service.
/// </remarks>
/// <param name="DocumentId">The unique identifier of the analyzed document.</param>
/// <param name="Metrics">The calculated readability metrics.</param>
/// <param name="AnalysisDuration">Time taken to perform the analysis.</param>
/// <example>
/// <code>
/// public class ReadabilityHudHandler : INotificationHandler&lt;ReadabilityAnalyzedEvent&gt;
/// {
///     public Task Handle(ReadabilityAnalyzedEvent notification, CancellationToken ct)
///     {
///         // Update HUD widget with new metrics
///         _viewModel.GradeLevel = notification.Metrics.FleschKincaidGradeLevel;
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record ReadabilityAnalyzedEvent(
    Guid DocumentId,
    ReadabilityMetrics Metrics,
    TimeSpan AnalysisDuration) : INotification;
