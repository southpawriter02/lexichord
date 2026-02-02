using System.Reflection;
using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Contract tests for the <see cref="IDocumentRepository"/> interface.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the interface's method signatures and return types
/// without requiring a concrete implementation. They ensure the contract is
/// stable and follows expected async patterns.
/// </remarks>
public class IDocumentRepositoryContractTests
{
    private readonly Type _interfaceType = typeof(IDocumentRepository);

    [Fact]
    public void Interface_ExistsInCorrectNamespace()
    {
        // Assert
        _interfaceType.Namespace.Should().Be("Lexichord.Abstractions.Contracts.RAG");
    }

    [Fact]
    public void Interface_IsPublic()
    {
        // Assert
        _interfaceType.IsPublic.Should().BeTrue(
            because: "repository interfaces must be publicly accessible");
    }

    [Fact]
    public void Interface_IsInterface()
    {
        // Assert
        _interfaceType.IsInterface.Should().BeTrue();
    }

    #region Read Operation Methods

    [Fact]
    public void GetByIdAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("GetByIdAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Document?>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("id");
        parameters[1].ParameterType.Should().Be(typeof(CancellationToken));
        parameters[1].HasDefaultValue.Should().BeTrue(because: "CancellationToken should be optional");
    }

    [Fact]
    public void GetByProjectAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("GetByProjectAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<Document>>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("projectId");
    }

    [Fact]
    public void GetByFilePathAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("GetByFilePathAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Document?>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(3);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("projectId");
        parameters[1].ParameterType.Should().Be(typeof(string));
        parameters[1].Name.Should().Be("filePath");
    }

    [Fact]
    public void GetByStatusAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("GetByStatusAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<Document>>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(DocumentStatus));
        parameters[0].Name.Should().Be("status");
    }

    #endregion

    #region Write Operation Methods

    [Fact]
    public void AddAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("AddAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Document>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Document));
        parameters[0].Name.Should().Be("document");
    }

    [Fact]
    public void UpdateAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("UpdateAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Document));
        parameters[0].Name.Should().Be("document");
    }

    [Fact]
    public void UpdateStatusAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("UpdateStatusAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(4);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("id");
        parameters[1].ParameterType.Should().Be(typeof(DocumentStatus));
        parameters[1].Name.Should().Be("status");
        parameters[2].ParameterType.Should().Be(typeof(string));
        parameters[2].Name.Should().Be("failureReason");
        parameters[2].HasDefaultValue.Should().BeTrue(because: "failureReason should be optional");
    }

    [Fact]
    public void DeleteAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("DeleteAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("id");
    }

    #endregion

    [Fact]
    public void Interface_HasExpectedMethodCount()
    {
        // Arrange
        var methods = _interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        // 5 read + 4 write = 9 methods (v0.4.7d added GetFailedDocumentsAsync)
        methods.Should().HaveCount(9,
            because: "IDocumentRepository should have exactly 9 methods (5 read, 4 write)");
    }

    [Theory]
    [InlineData("GetByIdAsync")]
    [InlineData("GetByProjectAsync")]
    [InlineData("GetByFilePathAsync")]
    [InlineData("GetByStatusAsync")]
    [InlineData("GetFailedDocumentsAsync")]
    [InlineData("AddAsync")]
    [InlineData("UpdateAsync")]
    [InlineData("UpdateStatusAsync")]
    [InlineData("DeleteAsync")]
    public void Interface_HasMethod(string methodName)
    {
        // Arrange
        var method = _interfaceType.GetMethod(methodName);

        // Assert
        method.Should().NotBeNull($"IDocumentRepository should have method '{methodName}'");
    }

    [Fact]
    public void AllMethods_ReturnTask()
    {
        // Arrange
        var methods = _interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        foreach (var method in methods)
        {
            var isTaskBased = method.ReturnType == typeof(Task) ||
                              (method.ReturnType.IsGenericType &&
                               method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

            isTaskBased.Should().BeTrue(
                because: $"method '{method.Name}' should be async (return Task or Task<T>)");
        }
    }

    [Fact]
    public void AllMethods_AcceptCancellationToken()
    {
        // Arrange
        var methods = _interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var hasCancellationToken = parameters.Any(p => p.ParameterType == typeof(CancellationToken));

            hasCancellationToken.Should().BeTrue(
                because: $"method '{method.Name}' should accept CancellationToken for graceful cancellation");
        }
    }
}
