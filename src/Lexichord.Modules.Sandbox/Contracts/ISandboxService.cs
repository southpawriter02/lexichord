namespace Lexichord.Modules.Sandbox.Contracts;

/// <summary>
/// Simple service interface demonstrating module service registration.
/// </summary>
/// <remarks>
/// LOGIC: This interface exists to prove that modules can:
/// 1. Define their own service interfaces
/// 2. Register implementations in the Host's DI container
/// 3. Have those services resolved by other parts of the application
///
/// In a real module, this would be a meaningful service interface
/// (e.g., ITuningEngine, IMemoryService, IAgentOrchestrator).
/// </remarks>
public interface ISandboxService
{
    /// <summary>
    /// Gets the module name as reported by the service.
    /// </summary>
    /// <returns>The module name string.</returns>
    /// <remarks>
    /// LOGIC: Simple method to verify the service was resolved correctly.
    /// Returns a value that proves the Sandbox module's service is active.
    /// </remarks>
    string GetModuleName();

    /// <summary>
    /// Performs a simple operation to verify the service is functional.
    /// </summary>
    /// <param name="input">Input string to echo.</param>
    /// <returns>The input with module signature appended.</returns>
    /// <remarks>
    /// LOGIC: Demonstrates the service can perform actual work.
    /// Used in integration tests to verify end-to-end functionality.
    /// </remarks>
    string Echo(string input);

    /// <summary>
    /// Gets the timestamp when the module was initialized.
    /// </summary>
    /// <returns>The initialization timestamp.</returns>
    /// <remarks>
    /// LOGIC: Proves InitializeAsync was called and the service
    /// has access to state set during initialization.
    /// </remarks>
    DateTime GetInitializationTime();
}
