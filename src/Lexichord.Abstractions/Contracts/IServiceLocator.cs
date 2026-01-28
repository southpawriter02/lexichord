namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides access to application services for components that cannot use constructor injection.
/// </summary>
/// <remarks>
/// LOGIC: This interface exists for XAML-instantiated components that Avalonia creates
/// without constructor parameters. It is an anti-pattern and should be used sparingly.
///
/// Prefer constructor injection for:
/// - Services
/// - ViewModels
/// - Any class you instantiate yourself
///
/// Use IServiceLocator only for:
/// - Views created via XAML (DataTemplates, UserControls)
/// - Design-time scenarios
/// </remarks>
/// <example>
/// <code>
/// // In a XAML-instantiated UserControl
/// public partial class MyView : UserControl
/// {
///     public MyView()
///     {
///         InitializeComponent();
///
///         // Only use when constructor injection is impossible
///         #pragma warning disable CS0618
///         var locator = App.Services.GetRequiredService&lt;IServiceLocator&gt;();
///         var service = locator.GetRequiredService&lt;IMyService&gt;();
///         #pragma warning restore CS0618
///     }
/// }
/// </code>
/// </example>
[Obsolete("Use constructor injection where possible. This is for XAML-instantiated components only.")]
public interface IServiceLocator
{
    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service is not registered in the DI container.
    /// </exception>
    T GetRequiredService<T>() where T : notnull;

    /// <summary>
    /// Gets a service of the specified type, or null if not registered.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance, or null if not registered.</returns>
    T? GetService<T>() where T : class;
}
