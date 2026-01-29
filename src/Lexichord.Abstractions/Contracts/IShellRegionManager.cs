namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Manages shell region views contributed by modules.
/// </summary>
/// <remarks>
/// LOGIC: The ShellRegionManager is the bridge between modules and the Host.
/// 
/// Responsibilities:
/// 1. Collect all IShellRegionView registrations from the DI container
/// 2. Group views by their TargetRegion
/// 3. Sort views within each region by Order
/// 4. Provide views to MainWindow for rendering
///
/// Initialization Flow:
/// 1. Modules call RegisterServices(), registering IShellRegionView implementations
/// 2. Host builds ServiceProvider
/// 3. Modules call InitializeAsync()
/// 4. Host calls ShellRegionManager.Initialize()
/// 5. MainWindow calls GetViews() for each region
/// </remarks>
public interface IShellRegionManager
{
    /// <summary>
    /// Initializes the region manager by collecting all registered views.
    /// </summary>
    /// <remarks>
    /// LOGIC: This must be called after the ServiceProvider is built
    /// and all modules have registered their services.
    /// </remarks>
    void Initialize();

    /// <summary>
    /// Gets all views for a specific region, ordered by priority.
    /// </summary>
    /// <param name="region">The region to get views for.</param>
    /// <returns>Views ordered by their Order property.</returns>
    IReadOnlyList<IShellRegionView> GetViews(ShellRegion region);
}
