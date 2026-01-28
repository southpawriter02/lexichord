using FluentAssertions;

using Lexichord.Modules.Sandbox;

using Xunit;

namespace Lexichord.Tests.Unit.Modules.Sandbox;

/// <summary>
/// Architecture tests verifying Sandbox module follows module constraints.
/// </summary>
[Trait("Category", "Architecture")]
public class SandboxModuleArchitectureTests
{
    [Fact]
    public void SandboxModule_DoesNotReference_Host()
    {
        // Arrange
        var assembly = typeof(SandboxModule).Assembly;

        // Act
        var references = assembly.GetReferencedAssemblies()
            .Select(r => r.Name)
            .ToList();

        // Assert
        references.Should().NotContain("Lexichord.Host",
            "Sandbox module must not reference Lexichord.Host");
    }

    [Fact]
    public void SandboxModule_DoesNotReference_OtherModules()
    {
        // Arrange
        var assembly = typeof(SandboxModule).Assembly;
        var assemblyName = assembly.GetName().Name;

        // Act
        var moduleReferences = assembly.GetReferencedAssemblies()
            .Where(r => r.Name?.StartsWith("Lexichord.Modules") == true)
            .Where(r => r.Name != assemblyName)
            .Select(r => r.Name)
            .ToList();

        // Assert
        moduleReferences.Should().BeEmpty(
            "Sandbox module must not reference other modules");
    }

    [Fact]
    public void SandboxModule_References_Abstractions()
    {
        // Arrange
        var assembly = typeof(SandboxModule).Assembly;

        // Act
        var references = assembly.GetReferencedAssemblies()
            .Select(r => r.Name)
            .ToList();

        // Assert
        references.Should().Contain("Lexichord.Abstractions",
            "Sandbox module must reference Lexichord.Abstractions");
    }
}
