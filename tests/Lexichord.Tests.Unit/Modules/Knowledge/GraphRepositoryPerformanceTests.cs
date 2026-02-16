using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Lexichord.Modules.Knowledge.Graph;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge
{
    public class GraphRepositoryPerformanceTests
    {
        private readonly Mock<IGraphConnectionFactory> _mockFactory;
        private readonly Mock<IGraphSession> _mockSession;
        private readonly Mock<ILogger<GraphRepository>> _mockLogger;
        private readonly GraphRepository _repository;

        public GraphRepositoryPerformanceTests()
        {
            _mockFactory = new Mock<IGraphConnectionFactory>();
            _mockSession = new Mock<IGraphSession>();
            _mockLogger = new Mock<ILogger<GraphRepository>>();

            _mockFactory.Setup(f => f.CreateSessionAsync(It.IsAny<GraphAccessMode>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockSession.Object);

            _repository = new GraphRepository(_mockFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SearchEntities_FetchesOnlyMatchingEntities_WithCorrectCypher()
        {
            // Arrange
            var query = new EntitySearchQuery
            {
                Query = "test query",
                MaxResults = 10,
                EntityTypes = new HashSet<string> { "TestType" }
            };

            var entities = new List<KnowledgeEntity>
            {
                new KnowledgeEntity { Type = "TestType", Name = "Test Entity" }
            };

            _mockSession.Setup(s => s.QueryAsync<KnowledgeEntity>(
                    It.Is<string>(sql => sql.Contains("toLower") && sql.Contains("IN $types")),
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(entities);

            // Act
            var result = await _repository.SearchEntitiesAsync(query);

            // Assert
            _mockSession.Verify(s => s.QueryAsync<KnowledgeEntity>(
                It.Is<string>(sql =>
                    sql.Contains("MATCH (n:Entity)") &&
                    sql.Contains("WHERE n.id IS NOT NULL") &&
                    sql.Contains("AND n.type IN $types") &&
                    sql.Contains("toLower(n.name) CONTAINS") &&
                    sql.Contains("toLower(n.type) CONTAINS") &&
                    sql.Contains("toLower(n.properties) CONTAINS") &&
                    sql.Contains("LIMIT 50") // 10 * 5
                ),
                It.Is<Dictionary<string, object?>>(p =>
                    p.ContainsKey("types") &&
                    (p["types"] as List<string>) != null &&
                    ((List<string>)p["types"]!).Contains("TestType") &&
                    p.ContainsKey("t0") && "test".Equals(p["t0"]) &&
                    p.ContainsKey("t1") && "query".Equals(p["t1"])
                ),
                It.IsAny<CancellationToken>()), Times.Once);

            // Should not call GetAllEntities
            _mockSession.Verify(s => s.QueryAsync<KnowledgeEntity>(
                It.Is<string>(sql => sql.Contains("MATCH (n)") && !sql.Contains("toLower")),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SearchEntities_FiltersFalsePositivesInMemory()
        {
            // Arrange
            var query = new EntitySearchQuery
            {
                Query = "key", // Search for a term that might be a JSON key
                MaxResults = 10
            };

            // Entity 1: Has "key" in properties as a KEY only (e.g. {"key": "value"}). Should be filtered out.
            var entityFalsePositive = new KnowledgeEntity
            {
                Type = "Type1",
                Name = "Entity1",
                Properties = new Dictionary<string, object> { { "key", "value" } }
            };

            // Entity 2: Has "key" in properties as a VALUE (e.g. {"prop": "key"}). Should be kept.
            var entityTruePositive = new KnowledgeEntity
            {
                Type = "Type2",
                Name = "Entity2",
                Properties = new Dictionary<string, object> { { "prop", "key" } }
            };

            // Entity 3: Has "key" in Name. Should be kept.
            var entityNameMatch = new KnowledgeEntity
            {
                Type = "Type3",
                Name = "key entity",
                Properties = new Dictionary<string, object>()
            };

            var entities = new List<KnowledgeEntity> { entityFalsePositive, entityTruePositive, entityNameMatch };

            _mockSession.Setup(s => s.QueryAsync<KnowledgeEntity>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(entities);

            // Act
            var result = await _repository.SearchEntitiesAsync(query);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(entityTruePositive, result);
            Assert.Contains(entityNameMatch, result);
            Assert.DoesNotContain(entityFalsePositive, result);
        }
    }
}
