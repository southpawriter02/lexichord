using System.Reflection;
using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Contract tests for the <see cref="IChunkRepository"/> interface.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the interface's method signatures and return types
/// without requiring a concrete implementation. Special attention is given to
/// the SearchSimilarAsync method which has complex parameters.
/// </remarks>
public class IChunkRepositoryContractTests
{
    private readonly Type _interfaceType = typeof(IChunkRepository);

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
    public void GetByDocumentIdAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("GetByDocumentIdAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<Chunk>>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("documentId");
        parameters[1].ParameterType.Should().Be(typeof(CancellationToken));
        parameters[1].HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void SearchSimilarAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("SearchSimilarAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<ChunkSearchResult>>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(5);

        // Verify parameter types and names
        parameters[0].ParameterType.Should().Be(typeof(float[]));
        parameters[0].Name.Should().Be("queryEmbedding");

        parameters[1].ParameterType.Should().Be(typeof(int));
        parameters[1].Name.Should().Be("topK");
        parameters[1].HasDefaultValue.Should().BeTrue();

        parameters[2].ParameterType.Should().Be(typeof(double));
        parameters[2].Name.Should().Be("threshold");
        parameters[2].HasDefaultValue.Should().BeTrue();

        parameters[3].ParameterType.Should().Be(typeof(Guid?));
        parameters[3].Name.Should().Be("projectId");
        parameters[3].HasDefaultValue.Should().BeTrue();

        parameters[4].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void SearchSimilarAsync_HasDefaultTopKValue()
    {
        // Arrange
        var method = _interfaceType.GetMethod("SearchSimilarAsync");
        var topKParam = method!.GetParameters()[1];

        // Assert
        topKParam.DefaultValue.Should().Be(10,
            because: "default topK should be 10 results");
    }

    [Fact]
    public void SearchSimilarAsync_HasDefaultThresholdValue()
    {
        // Arrange
        var method = _interfaceType.GetMethod("SearchSimilarAsync");
        var thresholdParam = method!.GetParameters()[2];

        // Assert
        thresholdParam.DefaultValue.Should().Be(0.5,
            because: "default threshold should be 0.5");
    }

    #endregion

    #region Write Operation Methods

    [Fact]
    public void AddRangeAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("AddRangeAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(IEnumerable<Chunk>));
        parameters[0].Name.Should().Be("chunks");
    }

    [Fact]
    public void DeleteByDocumentIdAsync_HasCorrectSignature()
    {
        // Arrange
        var method = _interfaceType.GetMethod("DeleteByDocumentIdAsync");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<int>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(Guid));
        parameters[0].Name.Should().Be("documentId");
    }

    #endregion

    [Fact]
    public void Interface_HasExpectedMethodCount()
    {
        // Arrange
        var methods = _interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        // 2 read + 2 write = 4 methods
        methods.Should().HaveCount(4,
            because: "IChunkRepository should have exactly 4 methods");
    }

    [Theory]
    [InlineData("GetByDocumentIdAsync")]
    [InlineData("SearchSimilarAsync")]
    [InlineData("AddRangeAsync")]
    [InlineData("DeleteByDocumentIdAsync")]
    public void Interface_HasMethod(string methodName)
    {
        // Arrange
        var method = _interfaceType.GetMethod(methodName);

        // Assert
        method.Should().NotBeNull($"IChunkRepository should have method '{methodName}'");
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

    [Fact]
    public void SearchSimilarAsync_ReturnsChunkSearchResults()
    {
        // Arrange
        var method = _interfaceType.GetMethod("SearchSimilarAsync");
        var returnType = method!.ReturnType;

        // Assert
        returnType.IsGenericType.Should().BeTrue();
        var innerType = returnType.GetGenericArguments()[0];
        innerType.Should().Be(typeof(IEnumerable<ChunkSearchResult>));
    }

    [Fact]
    public void AddRangeAsync_AcceptsEnumerableOfChunks()
    {
        // Arrange
        var method = _interfaceType.GetMethod("AddRangeAsync");
        var chunksParam = method!.GetParameters()[0];

        // Assert
        chunksParam.ParameterType.Should().Be(typeof(IEnumerable<Chunk>),
            because: "batch insertion should accept an enumerable of chunks");
    }

    [Fact]
    public void DeleteByDocumentIdAsync_ReturnsDeletedCount()
    {
        // Arrange
        var method = _interfaceType.GetMethod("DeleteByDocumentIdAsync");

        // Assert
        method!.ReturnType.Should().Be(typeof(Task<int>),
            because: "delete should return the count of deleted records");
    }
}
