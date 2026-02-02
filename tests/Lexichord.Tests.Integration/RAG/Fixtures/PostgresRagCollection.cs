// =============================================================================
// File: PostgresRagCollection.cs
// Project: Lexichord.Tests.Integration
// Description: xUnit collection definition for RAG integration tests.
// =============================================================================
// v0.4.8b: Marker class for xUnit collection fixture sharing.
// =============================================================================

namespace Lexichord.Tests.Integration.RAG.Fixtures;

/// <summary>
/// xUnit collection definition for RAG integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This collection enables sharing a single <see cref="PostgresRagFixture"/>
/// instance across all test classes in the collection. The fixture is created
/// once before any tests run and disposed after all tests complete.
/// </para>
/// <para>
/// <b>Usage:</b> Add <c>[Collection("PostgresRag")]</c> attribute to test classes.
/// </para>
/// </remarks>
[CollectionDefinition("PostgresRag")]
public class PostgresRagCollection : ICollectionFixture<PostgresRagFixture>
{
    // This class has no code, it's just a marker for xUnit to share the fixture
}
