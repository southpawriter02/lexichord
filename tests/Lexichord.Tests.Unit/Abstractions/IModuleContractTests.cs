using Lexichord.Abstractions.Contracts;

using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Interface contract tests for <see cref="IModule"/>.
/// </summary>
public class IModuleContractTests
{
    [Fact]
    public void IModule_Interface_DefinesRequiredMembers()
    {
        // Arrange
        var interfaceType = typeof(IModule);

        // Act
        var properties = interfaceType.GetProperties();
        var methods = interfaceType.GetMethods();

        // Assert - Info property exists
        properties.Should().Contain(p => p.Name == "Info" && p.PropertyType == typeof(ModuleInfo),
            because: "IModule must define Info property of type ModuleInfo");

        // Assert - RegisterServices method exists
        methods.Should().Contain(m => m.Name == "RegisterServices" &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(IServiceCollection),
            because: "IModule must define RegisterServices(IServiceCollection)");

        // Assert - InitializeAsync method exists
        methods.Should().Contain(m => m.Name == "InitializeAsync" &&
            m.ReturnType == typeof(Task) &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(IServiceProvider),
            because: "IModule must define InitializeAsync(IServiceProvider)");
    }
}
