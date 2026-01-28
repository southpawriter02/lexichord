using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Services;

/// <summary>
/// Implementation of IServiceLocator that wraps the DI container.
/// </summary>
/// <remarks>
/// LOGIC: This is a thin wrapper around IServiceProvider. It exists to:
/// 1. Provide a clear interface in Abstractions
/// 2. Allow mocking in tests
/// 3. Add diagnostics/logging if needed later
/// </remarks>
#pragma warning disable CS0618 // Implementing obsolete interface intentionally
public sealed class ServiceLocator(IServiceProvider serviceProvider) : IServiceLocator
{
    /// <inheritdoc/>
    public T GetRequiredService<T>() where T : notnull
    {
        return serviceProvider.GetRequiredService<T>();
    }

    /// <inheritdoc/>
    public T? GetService<T>() where T : class
    {
        return serviceProvider.GetService<T>();
    }
}
#pragma warning restore CS0618
