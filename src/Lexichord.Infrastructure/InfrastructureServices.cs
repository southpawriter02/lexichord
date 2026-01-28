using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Data;

namespace Lexichord.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services.
/// </summary>
public static class InfrastructureServices
{
    /// <summary>
    /// Adds database connectivity services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LOGIC: Bind configuration options from appsettings.json
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        // LOGIC: Register connection factory as singleton
        // The factory maintains the connection pool which should live for app lifetime
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }

    /// <summary>
    /// Adds repository services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// LOGIC: Repositories are registered as Scoped to align with typical request lifetime.
    /// The generic repository is registered as an open generic to support any entity type.
    /// </remarks>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // LOGIC: Register entity-specific repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();

        // LOGIC: Register the generic repository for any entity type
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        // LOGIC: Register the Unit of Work for transaction management
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
