# LCS-CL-005d: Repository Base

**Version:** v0.0.5d  
**Category:** Infrastructure  
**Feature Name:** Repository Base  
**Date:** 2026-01-28

---

## Summary

Implements the generic repository pattern using Dapper for type-safe CRUD operations. Provides entity-specific repositories for User and SystemSettings, plus Unit of Work for transaction management.

---

## New Features

### Entity Classes

- **EntityBase** — Base record with audit fields (CreatedAt, UpdatedAt)
- **User** — User entity with Id, Email, DisplayName, PasswordHash, IsActive
- **SystemSetting** — Key-value settings entity with Key, Value, Description

### Generic Repository

- **IGenericRepository<T, TId>** — Full CRUD interface with:
    - `GetByIdAsync` / `GetAllAsync` / `GetPagedAsync`
    - `ExistsAsync` / `CountAsync`
    - `InsertAsync` / `InsertManyAsync` / `UpdateAsync` / `DeleteAsync`
    - `QueryAsync` / `QuerySingleOrDefaultAsync` / `ExecuteAsync` / `ExecuteScalarAsync`

- **PagedResult<T>** — Record for paginated results with:
    - `Items`, `PageNumber`, `PageSize`, `TotalCount`
    - Computed: `TotalPages`, `HasPreviousPage`, `HasNextPage`

- **GenericRepository<T, TId>** — Dapper implementation with:
    - Automatic table/column name discovery via attributes
    - Parameterized queries for SQL injection prevention
    - Debug logging for all operations

### User Repository

- **IUserRepository** — User-specific operations:
    - `GetByEmailAsync` (case-insensitive)
    - `GetActiveUsersAsync`
    - `EmailExistsAsync`
    - `SearchAsync` (by display name or email)
    - `DeactivateAsync` / `ReactivateAsync` (soft delete)

- **UserRepository** — Implementation using PostgreSQL ILIKE for search

### SystemSettings Repository

- **ISystemSettingsRepository** — Key-value operations:
    - `GetByKeyAsync` / `GetValueAsync` (with default)
    - `GetValueAsync<T>` (typed with default)
    - `SetValueAsync` / `SetValueAsync<T>` (UPSERT)
    - `GetAllAsync` / `GetByPrefixAsync`
    - `DeleteAsync`

- **SystemSettingsRepository** — Implementation with:
    - PostgreSQL UPSERT (ON CONFLICT)
    - Automatic type conversion (primitives and JSON)

### Unit of Work

- **IUnitOfWork** — Transaction management:
    - `Connection` / `Transaction` / `HasActiveTransaction`
    - `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`
    - `SaveChangesAsync` (EF-like alias)

- **UnitOfWork** — Implementation with:
    - Lazy connection initialization
    - Automatic rollback on dispose

---

## Files Added

| File                                                                | Description                              |
| :------------------------------------------------------------------ | :--------------------------------------- |
| `src/Lexichord.Abstractions/Entities/EntityBase.cs`                 | Base entity with audit fields            |
| `src/Lexichord.Abstractions/Entities/User.cs`                       | User entity record                       |
| `src/Lexichord.Abstractions/Entities/SystemSetting.cs`              | SystemSetting entity record              |
| `src/Lexichord.Abstractions/Contracts/IGenericRepository.cs`        | Generic repository interface             |
| `src/Lexichord.Abstractions/Contracts/IUserRepository.cs`           | User repository interface                |
| `src/Lexichord.Abstractions/Contracts/ISystemSettingsRepository.cs` | SystemSettings repository interface      |
| `src/Lexichord.Abstractions/Contracts/IUnitOfWork.cs`               | Unit of Work interface                   |
| `src/Lexichord.Infrastructure/Data/GenericRepository.cs`            | Generic repository Dapper implementation |
| `src/Lexichord.Infrastructure/Data/UserRepository.cs`               | User repository implementation           |
| `src/Lexichord.Infrastructure/Data/SystemSettingsRepository.cs`     | SystemSettings repository implementation |
| `src/Lexichord.Infrastructure/Data/UnitOfWork.cs`                   | Unit of Work implementation              |

## Files Modified

| File                                                           | Description                              |
| :------------------------------------------------------------- | :--------------------------------------- |
| `src/Lexichord.Abstractions/Lexichord.Abstractions.csproj`     | Added Dapper.Contrib package             |
| `src/Lexichord.Infrastructure/Lexichord.Infrastructure.csproj` | Added Dapper and Dapper.Contrib packages |
| `src/Lexichord.Infrastructure/InfrastructureServices.cs`       | Added AddRepositories() method           |

---

## NuGet Packages Added

| Package        | Version |
| :------------- | :------ |
| Dapper         | 2.1.35  |
| Dapper.Contrib | 2.0.78  |

---

## Usage

### Basic CRUD

```csharp
// Inject via DI
public class UserService(IUserRepository userRepo)
{
    public async Task<User> CreateUserAsync(string email, string displayName)
    {
        if (await userRepo.EmailExistsAsync(email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName
        };

        return await userRepo.InsertAsync(user);
    }

    public async Task<PagedResult<User>> GetUsersPagedAsync(int page, int size)
    {
        return await userRepo.GetPagedAsync(page, size);
    }
}
```

### Using Settings

```csharp
public class FeatureFlagService(ISystemSettingsRepository settings)
{
    public async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        return await settings.GetValueAsync<bool>($"feature:{featureName}", false);
    }

    public async Task SetFeatureAsync(string featureName, bool enabled)
    {
        await settings.SetValueAsync($"feature:{featureName}", enabled);
    }
}
```

### Transactions

```csharp
public class RegistrationService(
    IUserRepository userRepo,
    ISystemSettingsRepository settings,
    IUnitOfWork unitOfWork)
{
    public async Task RegisterUserAsync(string email, string displayName)
    {
        await unitOfWork.BeginTransactionAsync();

        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = displayName
            };
            await userRepo.InsertAsync(user);

            var count = await settings.GetValueAsync<int>("stats:user_count", 0);
            await settings.SetValueAsync("stats:user_count", count + 1);

            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
```

---

## Verification Commands

```bash
# 1. Start test database
./scripts/db-start.sh

# 2. Run migrations (creates tables)
dotnet run --project src/Lexichord.Host -- --migrate

# 3. Verify tables
docker exec -it lexichord-postgres psql -U lexichord -c '\dt'

# 4. Check Users table
docker exec -it lexichord-postgres psql -U lexichord -c '\d "Users"'

# 5. Check SystemSettings
docker exec -it lexichord-postgres psql -U lexichord -c 'SELECT * FROM "SystemSettings"'

# 6. Test CRUD manually with psql
docker exec -it lexichord-postgres psql -U lexichord -d lexichord

# In psql:
INSERT INTO "Users" ("Id", "Email", "DisplayName")
VALUES ('11111111-1111-1111-1111-111111111111', 'manual@test.local', 'Manual Test');

SELECT * FROM "Users";

\q

# 7. Cleanup
./scripts/db-reset.sh
```

---

## Dependencies

- **From v0.0.5b:** `IDbConnectionFactory`, `DatabaseOptions`
- **From v0.0.5c:** Migration_001_InitSystem (creates Users and SystemSettings tables)

## Enables

- **v0.0.6+:** Data-dependent features (authentication, user management, document storage)
