// <copyright file="ResonanceUpdateService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Manages the reactive update pipeline for the Resonance Dashboard.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5c - Coordinates chart updates with debouncing and license gating.</para>
/// <list type="bullet">
///   <item>Subscribes to <see cref="ReadabilityAnalyzedEvent"/> (debounced 300ms)</item>
///   <item>Subscribes to <see cref="ProfileChangedEvent"/> (immediate)</item>
///   <item>Provides <see cref="ForceUpdateAsync"/> for manual refresh</item>
/// </list>
/// <para>Thread-safe: All state mutations are synchronized via lock.</para>
/// </remarks>
public sealed partial class ResonanceUpdateService :
    IResonanceUpdateService,
    INotificationHandler<ReadabilityAnalyzedEvent>,
    INotificationHandler<ProfileChangedEvent>
{
    /// <summary>
    /// Debounce delay for analysis events in milliseconds.
    /// </summary>
    private const int DebounceDelayMs = 300;

    private readonly IChartDataService _chartDataService;
    private readonly ILicenseService _licenseService;
    private readonly IMediator _mediator;
    private readonly ILogger<ResonanceUpdateService> _logger;

    /// <summary>
    /// Subject for queuing debounced update triggers.
    /// </summary>
    private readonly Subject<UpdateTrigger> _updateSubject = new();

    /// <summary>
    /// Subject for emitting update events to UI subscribers.
    /// </summary>
    private readonly Subject<ChartUpdateEventArgs> _updateRequestedSubject = new();

    /// <summary>
    /// Subscription to the debounced update stream.
    /// </summary>
    private IDisposable? _debounceSubscription;

    /// <summary>
    /// Lock for thread-safe state management.
    /// </summary>
    private readonly object _stateLock = new();

    /// <summary>
    /// Tracks whether the service is currently listening.
    /// </summary>
    private bool _isListening;

    /// <summary>
    /// Tracks whether the service has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResonanceUpdateService"/> class.
    /// </summary>
    /// <param name="chartDataService">Service for chart data cache management.</param>
    /// <param name="licenseService">Service for license validation.</param>
    /// <param name="mediator">MediatR mediator for publishing events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ResonanceUpdateService(
        IChartDataService chartDataService,
        ILicenseService licenseService,
        IMediator mediator,
        ILogger<ResonanceUpdateService> logger)
    {
        _chartDataService = chartDataService ?? throw new ArgumentNullException(nameof(chartDataService));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LogServiceCreated();
    }

    /// <inheritdoc/>
    public IObservable<ChartUpdateEventArgs> UpdateRequested => _updateRequestedSubject.AsObservable();

    /// <inheritdoc/>
    public bool IsListening
    {
        get
        {
            lock (_stateLock)
            {
                return _isListening;
            }
        }
    }

    /// <inheritdoc/>
    public void StartListening()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                LogAlreadyDisposed();
                return;
            }

            if (_isListening)
            {
                LogAlreadyListening();
                return;
            }

            // LOGIC: Set up Rx debounce pipeline
            _debounceSubscription = _updateSubject
                .Throttle(TimeSpan.FromMilliseconds(DebounceDelayMs))
                .Subscribe(
                    trigger => DispatchUpdate(trigger, wasImmediate: false),
                    ex => LogDebounceError(ex));

            _isListening = true;
            LogStartedListening();
        }
    }

    /// <inheritdoc/>
    public void StopListening()
    {
        lock (_stateLock)
        {
            if (!_isListening)
            {
                return;
            }

            _debounceSubscription?.Dispose();
            _debounceSubscription = null;
            _isListening = false;
            LogStoppedListening();
        }
    }

    /// <inheritdoc/>
    public Task ForceUpdateAsync(CancellationToken ct = default)
    {
        if (!CheckCanProcess())
        {
            return Task.CompletedTask;
        }

        LogForceUpdate();
        return DispatchUpdateAsync(UpdateTrigger.ForceUpdate, wasImmediate: true, ct);
    }

    /// <summary>
    /// Handles readability analysis completion events.
    /// </summary>
    /// <param name="notification">The analysis event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>LOGIC: Debounced - queued via Rx Throttle.</remarks>
    public Task Handle(ReadabilityAnalyzedEvent notification, CancellationToken cancellationToken)
    {
        if (!CheckCanProcess())
        {
            return Task.CompletedTask;
        }

        LogEventReceived(nameof(ReadabilityAnalyzedEvent));
        _updateSubject.OnNext(UpdateTrigger.ReadabilityAnalyzed);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles profile change events.
    /// </summary>
    /// <param name="notification">The profile change event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>LOGIC: Immediate dispatch - bypasses debounce.</remarks>
    public Task Handle(ProfileChangedEvent notification, CancellationToken cancellationToken)
    {
        if (!CheckCanProcess())
        {
            return Task.CompletedTask;
        }

        LogEventReceived(nameof(ProfileChangedEvent));
        LogImmediateDispatch(nameof(ProfileChangedEvent));
        return DispatchUpdateAsync(UpdateTrigger.ProfileChanged, wasImmediate: true, cancellationToken);
    }

    /// <summary>
    /// Checks whether the service can process events.
    /// </summary>
    /// <returns>True if licensed and listening; otherwise false.</returns>
    private bool CheckCanProcess()
    {
        // LOGIC: License gate - only process if user has Resonance Dashboard feature
        if (!_licenseService.IsFeatureEnabled(FeatureCodes.ResonanceDashboard))
        {
            LogNotLicensed();
            return false;
        }

        lock (_stateLock)
        {
            if (!_isListening)
            {
                LogNotListening();
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Dispatches an update synchronously (for Rx callback).
    /// </summary>
    private void DispatchUpdate(UpdateTrigger trigger, bool wasImmediate)
    {
        _ = DispatchUpdateAsync(trigger, wasImmediate, CancellationToken.None);
    }

    /// <summary>
    /// Dispatches an update asynchronously.
    /// </summary>
    private async Task DispatchUpdateAsync(
        UpdateTrigger trigger,
        bool wasImmediate,
        CancellationToken ct)
    {
        var eventArgs = new ChartUpdateEventArgs
        {
            Trigger = trigger,
            WasImmediate = wasImmediate,
            EventReceivedAt = DateTimeOffset.UtcNow,
            DispatchedAt = DateTimeOffset.UtcNow
        };

        LogDispatchingUpdate(trigger, wasImmediate);

        // LOGIC: Invalidate cache for debounced updates to ensure fresh data
        if (!wasImmediate)
        {
            _chartDataService.InvalidateCache();
        }

        // LOGIC: Notify via MediatR for cross-module consumers
        await _mediator.Publish(new ChartUpdateEvent(trigger), ct).ConfigureAwait(false);

        // LOGIC: Notify via observable for ViewModel subscription
        _updateRequestedSubject.OnNext(eventArgs);

        LogUpdateDispatched(trigger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            StopListening();
            _updateSubject.Dispose();
            _updateRequestedSubject.Dispose();
            _disposed = true;
            LogDisposed();
        }
    }

    // LOGIC: Source-generated logging for performance
    [LoggerMessage(Level = LogLevel.Debug, Message = "ResonanceUpdateService created")]
    private partial void LogServiceCreated();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Started listening for update events")]
    private partial void LogStartedListening();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopped listening for update events")]
    private partial void LogStoppedListening();

    [LoggerMessage(Level = LogLevel.Warning, Message = "StartListening called but already listening")]
    private partial void LogAlreadyListening();

    [LoggerMessage(Level = LogLevel.Warning, Message = "StartListening called but service is disposed")]
    private partial void LogAlreadyDisposed();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Received {EventType} event")]
    private partial void LogEventReceived(string eventType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Immediate dispatch for {EventType}")]
    private partial void LogImmediateDispatch(string eventType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ForceUpdateAsync called")]
    private partial void LogForceUpdate();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching update: trigger={Trigger}, immediate={WasImmediate}")]
    private partial void LogDispatchingUpdate(UpdateTrigger trigger, bool wasImmediate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Update dispatched: trigger={Trigger}")]
    private partial void LogUpdateDispatched(UpdateTrigger trigger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping event - Resonance Dashboard not licensed")]
    private partial void LogNotLicensed();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping event - service not listening")]
    private partial void LogNotListening();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in debounce pipeline")]
    private partial void LogDebounceError(Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ResonanceUpdateService disposed")]
    private partial void LogDisposed();
}
