using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Manages shell region views contributed by modules.
/// </summary>
/// <remarks>
/// LOGIC: The ShellRegionManager collects IShellRegionView registrations
/// from the DI container and provides them to MainWindow for rendering.
///
/// Initialization Flow:
/// 1. Modules register IShellRegionView implementations during RegisterServices()
/// 2. Host builds ServiceProvider
/// 3. Host calls Initialize() which collects all registered views
/// 4. MainWindow calls GetViews() for each region to populate containers
///
/// Thread Safety:
/// - Initialize() should be called once from the main thread
/// - GetViews() is safe to call from UI thread after initialization
/// </remarks>
public sealed class ShellRegionManager : IShellRegionManager
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<ShellRegionManager> _logger;
    private readonly Dictionary<ShellRegion, List<IShellRegionView>> _regionViews = new();
    private bool _initialized;

    public ShellRegionManager(
        IServiceProvider provider,
        ILogger<ShellRegionManager> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized)
        {
            _logger.LogWarning("ShellRegionManager already initialized");
            return;
        }

        _logger.LogDebug("Initializing ShellRegionManager");

        // LOGIC: Get all registered IShellRegionView implementations
        var views = _provider.GetServices<IShellRegionView>();
        var viewCount = 0;

        foreach (var view in views)
        {
            if (!_regionViews.ContainsKey(view.TargetRegion))
                _regionViews[view.TargetRegion] = new List<IShellRegionView>();

            _regionViews[view.TargetRegion].Add(view);
            viewCount++;

            _logger.LogDebug(
                "Registered view in {Region} region with order {Order}",
                view.TargetRegion, view.Order);
        }

        // LOGIC: Sort each region by order (lower values first)
        foreach (var region in _regionViews.Keys)
        {
            _regionViews[region] = _regionViews[region]
                .OrderBy(v => v.Order)
                .ToList();
        }

        _initialized = true;
        _logger.LogInformation(
            "ShellRegionManager initialized with {ViewCount} views across {RegionCount} regions",
            viewCount, _regionViews.Count);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IShellRegionView> GetViews(ShellRegion region)
    {
        if (!_initialized)
        {
            _logger.LogWarning(
                "GetViews called before Initialize; returning empty list for {Region}",
                region);
            return Array.Empty<IShellRegionView>();
        }

        return _regionViews.TryGetValue(region, out var views)
            ? views.AsReadOnly()
            : Array.Empty<IShellRegionView>();
    }
}
