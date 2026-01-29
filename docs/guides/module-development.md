# Module Development Guide

This guide explains how to create a Lexichord module using the StatusBar module as a reference implementation.

---

## Quick Start

1. Create a new Class Library project
2. Reference only `Lexichord.Abstractions`
3. Implement `IModule` interface
4. Configure output to `./Modules/` directory

---

## Project Structure

```
src/Lexichord.Modules.YourModule/
├── YourModuleModule.cs           # IModule implementation
├── Views/
│   ├── MainView.axaml
│   └── MainView.axaml.cs
├── ViewModels/
│   └── MainViewModel.cs
└── Services/
    └── YourService.cs
```

---

## Project File Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputPath>$(SolutionDir)Modules\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.3" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

---

## IModule Implementation

```csharp
public class YourModuleModule : IModule
{
    public ModuleInfo Info => new(
        Id: "yourmodule",
        Name: "Your Module",
        Version: new Version(1, 0, 0),
        Author: "Your Name",
        Description: "What your module does"
    );

    public void RegisterServices(IServiceCollection services)
    {
        // Register views, viewmodels, and services
        services.AddTransient<MainView>();
        services.AddTransient<MainViewModel>();
        services.AddSingleton<IYourService, YourService>();
    }

    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<YourModuleModule>>();
        logger.LogInformation("Initializing {Module}", Info.Name);

        // Initialize services, run migrations, etc.
    }
}
```

---

## Accessing Host Services

Modules can inject any Host service through the standard DI container:

```csharp
public class YourService(
    IDbConnectionFactory dbFactory,    // Database access
    ISecureVault vault,                 // Secure storage
    IMediator mediator,                 // Event bus
    IConfiguration config,              // Configuration
    ILogger<YourService> logger)        // Logging
{
    // Use injected services
}
```

---

## Shell Region Registration

To add UI to the Host:

```csharp
public class YourRegionView : IShellRegionView
{
    public ShellRegion TargetRegion => ShellRegion.Left;
    public int Order => 50;
    public object ViewContent => _view;
}

// Register in IModule.RegisterServices:
services.AddSingleton<IShellRegionView, YourRegionView>();
```

---

## License Restrictions

To restrict your module to certain license tiers:

```csharp
[RequiresLicense(LicenseTier.Teams)]
public class PremiumModule : IModule
{
    // Only loaded if user has Teams or higher license
}
```

---

## Testing

Write unit tests for your services:

```csharp
[Fact]
public async Task YourMethod_WorksCorrectly()
{
    // Arrange
    var mock = new Mock<IDbConnectionFactory>();
    var sut = new YourService(mock.Object, ...);

    // Act
    var result = await sut.YourMethodAsync();

    // Assert
    result.Should().NotBeNull();
}
```

---

## Reference Implementation

See `Lexichord.Modules.StatusBar` for a complete example of:

- Module lifecycle
- Database access
- Vault integration
- Shell region registration
- Event publishing
- ViewModel with DI
