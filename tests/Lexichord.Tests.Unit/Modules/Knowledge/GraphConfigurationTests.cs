// =============================================================================
// File: GraphConfigurationTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the GraphConfiguration record.
// =============================================================================
// LOGIC: Validates default values, section name constant, and property
//   initialization of GraphConfiguration. Ensures configuration defaults
//   match the Docker Compose Neo4j container settings.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// =============================================================================

using Lexichord.Modules.Knowledge.Graph;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="GraphConfiguration"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5e")]
public sealed class GraphConfigurationTests
{
    #region Default Values

    [Fact]
    public void SectionName_IsKnowledgeGraph()
    {
        // Assert
        GraphConfiguration.SectionName.Should().Be("Knowledge:Graph");
    }

    [Fact]
    public void DefaultUri_IsLocalhostBolt()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.Uri.Should().Be("bolt://localhost:7687");
    }

    [Fact]
    public void DefaultDatabase_IsNeo4j()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.Database.Should().Be("neo4j");
    }

    [Fact]
    public void DefaultUsername_IsNeo4j()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.Username.Should().Be("neo4j");
    }

    [Fact]
    public void DefaultPassword_IsNull()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.Password.Should().BeNull();
    }

    [Fact]
    public void DefaultMaxConnectionPoolSize_Is100()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.MaxConnectionPoolSize.Should().Be(100);
    }

    [Fact]
    public void DefaultConnectionTimeoutSeconds_Is30()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.ConnectionTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void DefaultQueryTimeoutSeconds_Is60()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.QueryTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void DefaultEncrypted_IsFalse()
    {
        // Arrange & Act
        var config = new GraphConfiguration();

        // Assert
        config.Encrypted.Should().BeFalse();
    }

    #endregion

    #region Record Behavior

    [Fact]
    public void WithExpression_OverridesSpecifiedProperties()
    {
        // Arrange
        var config = new GraphConfiguration();

        // Act
        var modified = config with
        {
            Uri = "bolt://remote:7687",
            Encrypted = true,
            MaxConnectionPoolSize = 50
        };

        // Assert
        modified.Uri.Should().Be("bolt://remote:7687");
        modified.Encrypted.Should().BeTrue();
        modified.MaxConnectionPoolSize.Should().Be(50);
        // Unchanged properties retain defaults
        modified.Database.Should().Be("neo4j");
        modified.Username.Should().Be("neo4j");
        modified.Password.Should().BeNull();
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var config1 = new GraphConfiguration { Password = "test" };
        var config2 = new GraphConfiguration { Password = "test" };

        // Assert
        config1.Should().Be(config2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var config1 = new GraphConfiguration { Password = "test1" };
        var config2 = new GraphConfiguration { Password = "test2" };

        // Assert
        config1.Should().NotBe(config2);
    }

    #endregion
}
