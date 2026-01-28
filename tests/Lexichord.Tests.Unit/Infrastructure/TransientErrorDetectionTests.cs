using System.Reflection;
using Npgsql;
using Lexichord.Infrastructure.Data;

namespace Lexichord.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for transient error detection logic.
/// </summary>
public class TransientErrorDetectionTests
{
    /// <summary>
    /// Helper to create NpgsqlException with a specific SQL state.
    /// Uses reflection since NpgsqlException constructor is internal.
    /// </summary>
    private static NpgsqlException CreateNpgsqlException(string sqlState)
    {
        // NpgsqlException doesn't have a public constructor with SqlState
        // We need to use PostgresException which is a subclass
        // Create via reflection or use a workaround
        var exceptionType = typeof(PostgresException);
        var constructor = exceptionType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(c => c.GetParameters().Length > 0);

        if (constructor != null)
        {
            try
            {
                // Try to create PostgresException with message builder
                return new PostgresException(
                    messageText: "Test error",
                    severity: "ERROR",
                    invariantSeverity: "ERROR",
                    sqlState: sqlState);
            }
            catch
            {
                // Fallback: just return a basic exception that will fail the test
                // This is acceptable because the test will indicate what needs fixing
                throw new InvalidOperationException($"Unable to create NpgsqlException with SqlState {sqlState}");
            }
        }

        throw new InvalidOperationException("Cannot find suitable NpgsqlException constructor");
    }

    [Theory]
    [InlineData("08000")] // connection_exception
    [InlineData("08003")] // connection_does_not_exist
    [InlineData("08006")] // connection_failure
    [InlineData("08001")] // sqlclient_unable_to_establish_sqlconnection
    [InlineData("08004")] // sqlserver_rejected_establishment_of_sqlconnection
    [InlineData("08007")] // transaction_resolution_unknown
    [InlineData("08P01")] // protocol_violation
    public void IsTransientError_ShouldReturnTrue_ForConnectionExceptions(string sqlState)
    {
        // Arrange
        var exception = CreateNpgsqlException(sqlState);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue($"SQL state {sqlState} should be transient");
    }

    [Theory]
    [InlineData("40001")] // serialization_failure
    [InlineData("40002")] // transaction_integrity_constraint_violation
    [InlineData("40003")] // statement_completion_unknown
    [InlineData("40P01")] // deadlock_detected
    public void IsTransientError_ShouldReturnTrue_ForTransactionRollback(string sqlState)
    {
        // Arrange
        var exception = CreateNpgsqlException(sqlState);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue($"SQL state {sqlState} should be transient");
    }

    [Theory]
    [InlineData("53000")] // insufficient_resources
    [InlineData("53100")] // disk_full
    [InlineData("53200")] // out_of_memory
    [InlineData("53300")] // too_many_connections
    public void IsTransientError_ShouldReturnTrue_ForResourceExhaustion(string sqlState)
    {
        // Arrange
        var exception = CreateNpgsqlException(sqlState);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue($"SQL state {sqlState} should be transient");
    }

    [Theory]
    [InlineData("57000")] // operator_intervention
    [InlineData("57014")] // query_canceled
    [InlineData("57P01")] // admin_shutdown
    [InlineData("57P02")] // crash_shutdown
    [InlineData("57P03")] // cannot_connect_now
    [InlineData("57P04")] // database_dropped
    public void IsTransientError_ShouldReturnTrue_ForOperatorIntervention(string sqlState)
    {
        // Arrange
        var exception = CreateNpgsqlException(sqlState);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue($"SQL state {sqlState} should be transient");
    }

    [Theory]
    [InlineData("58000")] // system_error
    [InlineData("58030")] // io_error
    public void IsTransientError_ShouldReturnTrue_ForSystemErrors(string sqlState)
    {
        // Arrange
        var exception = CreateNpgsqlException(sqlState);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeTrue($"SQL state {sqlState} should be transient");
    }

    [Theory]
    [InlineData("23505")] // unique_violation
    [InlineData("42P01")] // undefined_table
    [InlineData("28P01")] // invalid_password
    [InlineData("22001")] // string_data_right_truncation
    [InlineData("42601")] // syntax_error
    public void IsTransientError_ShouldReturnFalse_ForNonTransientErrors(string sqlState)
    {
        // Arrange
        var exception = CreateNpgsqlException(sqlState);

        // Act
        var result = InvokeIsTransientError(exception);

        // Assert
        result.Should().BeFalse($"SQL state {sqlState} should NOT be transient");
    }

    /// <summary>
    /// Invokes the internal IsTransientError method via reflection.
    /// </summary>
    private static bool InvokeIsTransientError(NpgsqlException exception)
    {
        var method = typeof(NpgsqlConnectionFactory).GetMethod(
            "IsTransientError",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("IsTransientError method not found");
        }

        return (bool)method.Invoke(null, [exception])!;
    }
}
