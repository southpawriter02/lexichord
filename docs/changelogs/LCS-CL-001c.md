# LCS-CL-001c: Changelog — Test Suite Foundation

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-001c                                            |
| **Version**      | v0.0.1c                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-001c](../specs/v0.0.x/v0.0.1/LCS-DES-001c.md) |

---

## Summary

Established the test suite infrastructure for the Lexichord project, including unit tests and Docker-based integration tests.

---

## Changes

### Files Created

| File                                                                   | Purpose                                |
| :--------------------------------------------------------------------- | :------------------------------------- |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj`               | Unit test project configuration        |
| `tests/Lexichord.Tests.Unit/InfrastructureTests.cs`                    | Sanity tests for test framework        |
| `tests/Lexichord.Tests.Integration/Lexichord.Tests.Integration.csproj` | Integration test project configuration |
| `tests/Lexichord.Tests.Integration/DockerConnectivityTests.cs`         | Docker/PostgreSQL connectivity tests   |

### Files Modified

| File            | Change                                            |
| :-------------- | :------------------------------------------------ |
| `Lexichord.sln` | Added test projects under `tests` solution folder |

---

## NuGet Packages Installed

### Lexichord.Tests.Unit

| Package                     | Version | Purpose                    |
| :-------------------------- | :------ | :------------------------- |
| `Microsoft.NET.Test.Sdk`    | 17.12.0 | Required for `dotnet test` |
| `xunit`                     | 2.9.2   | Test framework             |
| `xunit.runner.visualstudio` | 2.8.2   | IDE test discovery         |
| `FluentAssertions`          | 6.12.0  | Readable assertion syntax  |
| `Moq`                       | 4.20.70 | Mocking framework          |
| `coverlet.collector`        | 6.0.0   | Code coverage reporting    |

### Lexichord.Tests.Integration

| Package                     | Version | Purpose                      |
| :-------------------------- | :------ | :--------------------------- |
| `Microsoft.NET.Test.Sdk`    | 17.12.0 | Required for `dotnet test`   |
| `xunit`                     | 2.9.2   | Test framework               |
| `xunit.runner.visualstudio` | 2.8.2   | IDE test discovery           |
| `FluentAssertions`          | 6.12.0  | Readable assertion syntax    |
| `Testcontainers`            | 3.7.0   | Docker container management  |
| `Testcontainers.PostgreSql` | 3.7.0   | PostgreSQL container builder |
| `Npgsql`                    | 9.0.3   | PostgreSQL client            |
| `coverlet.collector`        | 6.0.0   | Code coverage reporting      |

---

## Tests Implemented

### Unit Tests (InfrastructureTests.cs)

```csharp
namespace Lexichord.Tests.Unit;

public class InfrastructureTests
{
    [Fact]
    public void TestRunner_ExecutesSuccessfully_WhenInvoked()
    // Verifies xUnit test runner works correctly

    [Fact]
    public void FluentAssertions_IsConfigured_WhenPackageInstalled()
    // Verifies FluentAssertions is properly configured

    [Fact]
    public void Moq_CreatesMocks_WhenFrameworkInstalled()
    // Verifies Moq mocking framework works correctly
}
```

### Integration Tests (DockerConnectivityTests.cs)

```csharp
namespace Lexichord.Tests.Integration;

[Trait("Category", "Integration")]
public class DockerConnectivityTests : IAsyncLifetime
{
    [Fact]
    public async Task Docker_StartsPostgreSqlContainer_WhenDockerDesktopIsRunning()
    // Verifies Docker Desktop can spin up a PostgreSQL container

    [Fact]
    public async Task PostgreSql_AcceptsConnections_WhenContainerIsRunning()
    // Verifies database connection and query execution

    [Fact]
    public async Task Docker_StopsContainer_WhenDisposed()
    // Verifies container cleanup on disposal
}
```

---

## Test Execution Commands

```bash
# Run all tests (requires Docker Desktop)
dotnet test

# Run unit tests only (no Docker required)
dotnet test --filter Category!=Integration

# List all discovered tests
dotnet test --list-tests

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

---

## Test Discovery Output

```
The following Tests are available:
    Lexichord.Tests.Unit.InfrastructureTests.TestRunner_ExecutesSuccessfully_WhenInvoked
    Lexichord.Tests.Unit.InfrastructureTests.FluentAssertions_IsConfigured_WhenPackageInstalled
    Lexichord.Tests.Unit.InfrastructureTests.Moq_CreatesMocks_WhenFrameworkInstalled
    Lexichord.Tests.Integration.DockerConnectivityTests.Docker_StartsPostgreSqlContainer_WhenDockerDesktopIsRunning
    Lexichord.Tests.Integration.DockerConnectivityTests.PostgreSql_AcceptsConnections_WhenContainerIsRunning
    Lexichord.Tests.Integration.DockerConnectivityTests.Docker_StopsContainer_WhenDisposed
```

---

## Verification Results

```
Test summary: total: 3, failed: 0, succeeded: 3, skipped: 0, duration: 27 ms
```

_(Unit tests only — integration tests require Docker Desktop)_

---

## Acceptance Criteria Verification

| Criterion                                                     | Status             |
| :------------------------------------------------------------ | :----------------- |
| `dotnet test --list-tests` shows tests from both projects     | ✅ Pass (6 tests)  |
| All unit tests pass with `--filter Category!=Integration`     | ✅ Pass (3/3)      |
| Integration tests pass when Docker Desktop is running         | ⏳ Requires Docker |
| `--filter Category!=Integration` completes without Docker ops | ✅ Pass            |
| Both test projects appear under `tests` solution folder       | ✅ Pass            |
| No orphan containers after test execution                     | ✅ N/A             |

---

## Project Configuration

### Lexichord.Tests.Unit.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test Framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>

    <!-- Assertions & Mocking -->
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.70" />

    <!-- Code Coverage -->
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <!-- Project References (Production Code) -->
    <ProjectReference Include="..\..\src\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />
    <ProjectReference Include="..\..\src\Lexichord.Host\Lexichord.Host.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
    <Using Include="Moq" />
  </ItemGroup>

</Project>
```

### Lexichord.Tests.Integration.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test Framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>

    <!-- Assertions -->
    <PackageReference Include="FluentAssertions" Version="6.12.0" />

    <!-- Testcontainers -->
    <PackageReference Include="Testcontainers" Version="3.7.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.7.0" />

    <!-- PostgreSQL Client (for connection verification) -->
    <PackageReference Include="Npgsql" Version="9.0.3" />

    <!-- Code Coverage -->
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <!-- Project References (Production Code) -->
    <ProjectReference Include="..\..\src\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />
    <ProjectReference Include="..\..\src\Lexichord.Host\Lexichord.Host.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
  </ItemGroup>

</Project>
```

---

## Notes

- Integration tests are marked with `[Trait("Category", "Integration")]` for selective execution.
- Developers without Docker can skip integration tests using `--filter Category!=Integration`.
- Npgsql was updated to 9.0.3 to resolve a known security vulnerability in earlier versions.
- Test naming follows the pattern: `MethodUnderTest_ExpectedBehavior_WhenCondition`.
