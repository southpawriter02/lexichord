namespace Lexichord.Tests.Unit;

/// <summary>
/// Infrastructure verification tests.
/// These tests validate that the test framework and build pipeline function correctly.
/// </summary>
public class InfrastructureTests
{
    /// <summary>
    /// Sanity check to verify the test runner executes correctly.
    /// </summary>
    /// <remarks>
    /// LOGIC: This test exists purely to prove the xUnit runner discovers and executes tests.
    /// If this test fails, it indicates a fundamental issue with the test infrastructure,
    /// not with any application logic.
    /// </remarks>
    [Fact]
    public void TestRunner_ExecutesSuccessfully_WhenInvoked()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected, because: "the test framework must execute assertions correctly");
    }

    /// <summary>
    /// Verifies that FluentAssertions is correctly configured.
    /// </summary>
    [Fact]
    public void FluentAssertions_IsConfigured_WhenPackageInstalled()
    {
        // Arrange
        var testString = "Lexichord";

        // Act & Assert
        testString.Should().NotBeNullOrEmpty()
            .And.StartWith("Lexi")
            .And.EndWith("chord")
            .And.HaveLength(9);
    }

    /// <summary>
    /// Verifies that Moq mocking framework is correctly configured.
    /// </summary>
    [Fact]
    public void Moq_CreatesMocks_WhenFrameworkInstalled()
    {
        // Arrange
        var mockService = new Mock<IDisposable>();
        mockService.Setup(s => s.Dispose());

        // Act
        mockService.Object.Dispose();

        // Assert
        mockService.Verify(s => s.Dispose(), Times.Once);
    }
}
