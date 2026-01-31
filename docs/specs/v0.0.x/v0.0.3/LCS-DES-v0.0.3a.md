# LCS-DES-003a: Dependency Injection Root

## 1. Metadata & Categorization

| Field                | Value                                | Description                                  |
| :------------------- | :----------------------------------- | :------------------------------------------- |
| **Feature ID**       | `INF-003a`                           | Infrastructure - DI Container Setup          |
| **Feature Name**     | Dependency Injection Root            | Microsoft.Extensions.DependencyInjection IoC |
| **Target Version**   | `v0.0.3a`                            | First sub-part of v0.0.3                     |
| **Module Scope**     | `Lexichord.Host`                     | Primary application executable               |
| **Swimlane**         | `Infrastructure`                     | The Podium (Platform)                        |
| **License Tier**     | `Core`                               | Foundation (Required for all tiers)          |
| **Author**           | System Architect                     |                                              |
| **Status**           | **Draft**                            | Pending implementation                       |
| **Last Updated**     | 2026-01-26                           |                                              |

---

## 2. Executive Summary

### 2.1 The Requirement

The Lexichord Host currently instantiates services directly using `new` keywords. This approach:

- Makes unit testing difficult (cannot mock dependencies).
- Creates tight coupling between components.
- Prevents future module registration.
- Violates SOLID principles (Dependency Inversion).

### 2.2 The Proposed Solution

We **SHALL** implement `Microsoft.Extensions.DependencyInjection` as the application's IoC container, following the standard .NET Generic Host pattern adapted for Avalonia desktop applications.

---

## 3. Implementation Tasks

### Task 1.1: Install DI Packages

**NuGet Packages to Add:**

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="9.0.0" />
```

**Rationale:** These packages provide the standard .NET DI container and host abstractions without bringing in the full ASP.NET Core stack.

---

### Task 1.2: Create Service Collection Builder

**File:** `src/Lexichord.Host/HostServices.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

namespace Lexichord.Host;

/// <summary>
/// Static helper for configuring Host services and DI container.
/// </summary>
/// <remarks>
/// LOGIC: All service registration is centralized here to provide a single source of truth
/// for the application's dependency graph. This enables:
/// - Easy testing via service replacement
/// - Clear visibility of dependencies
/// - Module integration in v0.0.4
/// </remarks>
public static class HostServices
{
    /// <summary>
    /// Configures all Host services in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.ConfigureServices(configuration);
    /// var provider = services.BuildServiceProvider();
    /// </code>
    /// </example>
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LOGIC: Register core Host services as Singletons
        // These services maintain state across the application lifetime
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IWindowStateService, WindowStateService>();

        return services;
    }
}
```

---

### Task 1.3: Integrate with Avalonia Lifecycle

**File:** `src/Lexichord.Host/App.axaml.cs` (Modified)

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;
using Lexichord.Host.Views;
using System;

namespace Lexichord.Host;

/// <summary>
/// The main Avalonia application class with DI integration.
/// </summary>
/// <remarks>
/// LOGIC: The application class owns the DI container lifetime.
/// Services are built during OnFrameworkInitializationCompleted and
/// disposed when the application shuts down.
/// </remarks>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the application-wide service provider.
    /// </summary>
    /// <remarks>
    /// LOGIC: This static accessor enables service resolution in components
    /// that cannot receive services via constructor injection (e.g., XAML-instantiated Views).
    /// Prefer constructor injection where possible.
    /// </remarks>
    public static IServiceProvider Services =>
        ((App)Current!).GetServices();

    private IServiceProvider GetServices() =>
        _serviceProvider ?? throw new InvalidOperationException(
            "Services not initialized. Ensure OnFrameworkInitializationCompleted has been called.");

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This is called after the framework is fully initialized.
    /// We build the DI container here because:
    /// 1. Avalonia is ready to create windows
    /// 2. We can inject the Application instance if needed
    /// 3. Command-line arguments are available via ApplicationLifetime
    /// </remarks>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Build DI container
            var services = new ServiceCollection();

            // LOGIC: For now, use empty configuration. v0.0.3d will add proper config.
            var configuration = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Register all Host services
            services.ConfigureServices(configuration);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();

            // Create main window with injected services
            desktop.MainWindow = CreateMainWindow();

            // Apply persisted settings
            ApplyPersistedSettings();

            // Register shutdown handler to dispose services
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow CreateMainWindow()
    {
        // LOGIC: Resolve services and inject into MainWindow
        // This demonstrates the transition from direct instantiation to DI
        var themeManager = _serviceProvider!.GetRequiredService<IThemeManager>();
        var windowStateService = _serviceProvider.GetRequiredService<IWindowStateService>();

        return new MainWindow
        {
            ThemeManager = themeManager,
            WindowStateService = windowStateService
        };
    }

    private void ApplyPersistedSettings()
    {
        var windowStateService = _serviceProvider!.GetRequiredService<IWindowStateService>();
        var themeManager = _serviceProvider.GetRequiredService<IThemeManager>();

        var savedState = windowStateService.LoadAsync().GetAwaiter().GetResult();
        if (savedState is not null)
        {
            themeManager.SetTheme(savedState.Theme);
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // LOGIC: Dispose the service provider to release resources
        // This ensures proper cleanup of singleton services
        _serviceProvider?.Dispose();
    }
}
```

---

### Task 1.4: Service Locator Pattern (Transitional)

**File:** `src/Lexichord.Abstractions/Contracts/IServiceLocator.cs`

```csharp
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
```

**File:** `src/Lexichord.Host/Services/ServiceLocator.cs`

```csharp
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Lexichord.Host.Services;

/// <summary>
/// Implementation of IServiceLocator that wraps the DI container.
/// </summary>
/// <remarks>
/// LOGIC: This is a thin wrapper around IServiceProvider. It exists to:
/// 1. Provide a clear interface in Abstractions
/// 2. Allow mocking in tests
/// 3. Add diagnostics/logging if needed later
/// </remarks>
#pragma warning disable CS0618 // Implementing obsolete interface intentionally
public sealed class ServiceLocator(IServiceProvider serviceProvider) : IServiceLocator
{
    /// <inheritdoc/>
    public T GetRequiredService<T>() where T : notnull
    {
        return serviceProvider.GetRequiredService<T>();
    }

    /// <inheritdoc/>
    public T? GetService<T>() where T : class
    {
        return serviceProvider.GetService<T>();
    }
}
#pragma warning restore CS0618
```

**Registration in HostServices.cs:**

```csharp
// Add to ConfigureServices method
#pragma warning disable CS0618 // Intentionally using obsolete interface
services.AddSingleton<IServiceLocator>(sp => new ServiceLocator(sp));
#pragma warning restore CS0618
```

---

## 4. Decision Tree: Service Resolution

```text
START: "How do I resolve a dependency?"
│
├── Is this a Service or ViewModel?
│   └── Use constructor injection (primary constructor pattern)
│       public class MyService(ILogger<MyService> logger, IRepository repo)
│       {
│           // Dependencies are automatically injected
│       }
│
├── Is this a View (Window/UserControl)?
│   ├── Can I pass services via DataContext (ViewModel)?
│   │   └── YES → Create ViewModel with services, bind to View
│   │       var vm = new MyViewModel(services...);
│   │       view.DataContext = vm;
│   │
│   └── View needs direct service access?
│       └── Use App.Services.GetRequiredService<T>() sparingly
│           // Mark with comment explaining why DI is not possible
│
├── Is this a static method?
│   └── NEVER resolve services inside static methods
│       Pass required services as method parameters
│       public static void Process(ILogger logger, IConfig config) { }
│
└── Is this during startup (before DI container built)?
    └── Use direct instantiation with comment
        // DI not available yet, direct instantiation required
        var tempService = new BootstrapService();
```

---

## 5. Unit Testing Requirements

### 5.1 Test: Services Are Registered

```csharp
[TestFixture]
[Category("Unit")]
public class HostServicesTests
{
    [Test]
    public void ConfigureServices_RegistersThemeManager()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();
        var result = provider.GetService<IThemeManager>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<ThemeManager>());
    }

    [Test]
    public void ConfigureServices_RegistersWindowStateService()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        // Act
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();
        var result = provider.GetService<IWindowStateService>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<WindowStateService>());
    }

    [Test]
    public void ConfigureServices_SingletonsReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.ConfigureServices(config);
        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetService<IThemeManager>();
        var instance2 = provider.GetService<IThemeManager>();

        // Assert
        Assert.That(instance1, Is.SameAs(instance2),
            "Singleton services should return the same instance");
    }
}
```

### 5.2 Test: Service Locator Works

```csharp
[TestFixture]
[Category("Unit")]
public class ServiceLocatorTests
{
    [Test]
    public void GetRequiredService_ReturnsRegisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IThemeManager, ThemeManager>();
        var provider = services.BuildServiceProvider();
        #pragma warning disable CS0618
        var sut = new ServiceLocator(provider);

        // Act
        var result = sut.GetRequiredService<IThemeManager>();

        // Assert
        Assert.That(result, Is.Not.Null);
        #pragma warning restore CS0618
    }

    [Test]
    public void GetRequiredService_ThrowsForUnregisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        #pragma warning disable CS0618
        var sut = new ServiceLocator(provider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            sut.GetRequiredService<IThemeManager>());
        #pragma warning restore CS0618
    }

    [Test]
    public void GetService_ReturnsNullForUnregisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        #pragma warning disable CS0618
        var sut = new ServiceLocator(provider);

        // Act
        var result = sut.GetService<IThemeManager>();

        // Assert
        Assert.That(result, Is.Null);
        #pragma warning restore CS0618
    }
}
```

---

## 6. Observability & Logging

| Level | Context      | Message Template                                       |
| :---- | :----------- | :----------------------------------------------------- |
| Debug | App          | `Building service provider with {ServiceCount} services` |
| Debug | App          | `Service provider built successfully`                  |
| Debug | App          | `Resolving {ServiceType} from DI container`            |
| Debug | App          | `Service provider disposed on shutdown`                |

---

## 7. Definition of Done

- [ ] `Microsoft.Extensions.DependencyInjection` package installed
- [ ] `Microsoft.Extensions.Hosting.Abstractions` package installed
- [ ] `HostServices.ConfigureServices()` method created
- [ ] `IThemeManager` registered as Singleton
- [ ] `IWindowStateService` registered as Singleton
- [ ] `App.axaml.cs` builds `ServiceProvider` on startup
- [ ] `App.Services` static property provides access to services
- [ ] Service provider disposed on application shutdown
- [ ] `IServiceLocator` interface defined in Abstractions (marked obsolete)
- [ ] `ServiceLocator` implementation in Host
- [ ] No direct `new` instantiation of registered services
- [ ] All unit tests passing

---

## 8. Verification Commands

```bash
# Build to verify packages and compilation
dotnet build src/Lexichord.Host

# Run unit tests
dotnet test --filter "FullyQualifiedName~HostServicesTests"
dotnet test --filter "FullyQualifiedName~ServiceLocatorTests"

# Run application and verify no runtime errors
dotnet run --project src/Lexichord.Host
```
