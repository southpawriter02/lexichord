# LCS-INF-005a: Docker Orchestration

## 1. Metadata & Categorization

| Field                | Value                                | Description                                  |
| :------------------- | :----------------------------------- | :------------------------------------------- |
| **Feature ID**       | `INF-005a`                           | Infrastructure - Docker Orchestration        |
| **Feature Name**     | Docker Orchestration                 | PostgreSQL 16 via Docker Compose             |
| **Target Version**   | `v0.0.5a`                            | First sub-part of v0.0.5                     |
| **Module Scope**     | `devops/docker`                      | Development infrastructure                   |
| **Swimlane**         | `Infrastructure`                     | The Podium (Platform)                        |
| **License Tier**     | `Core`                               | Foundation (Required for all tiers)          |
| **Author**           | System Architect                     |                                              |
| **Status**           | **Draft**                            | Pending implementation                       |
| **Last Updated**     | 2026-01-26                           |                                              |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord requires a **reproducible development database environment** that:

- Runs PostgreSQL 16 locally without native installation.
- Provides consistent behavior across Windows, macOS, and Linux.
- Persists data between container restarts.
- Enables rapid iteration with start/stop/reset scripts.

Without this foundation:

- Developers must install PostgreSQL natively (version conflicts).
- Database state differs between developers.
- CI/CD pipelines cannot run integration tests reliably.
- Schema migrations cannot be tested locally.

### 2.2 The Proposed Solution

We **SHALL** implement Docker Compose orchestration with:

1. **docker-compose.yml** — PostgreSQL 16 service with health checks.
2. **Environment Configuration** — `.env.example` template with connection settings.
3. **Development Scripts** — Shell scripts for start/stop/reset operations.
4. **Documentation** — Clear instructions in CONTRIBUTING.md.

---

## 3. Implementation Tasks

### Task 1.1: Create docker-compose.yml

**File:** `docker-compose.yml` (repository root)

```yaml
# Lexichord Development Services
# Usage: docker compose up -d
# Docs: https://docs.docker.com/compose/

services:
  # ============================================================================
  # PostgreSQL 16 Database
  # ============================================================================
  # LOGIC: We use the Alpine variant for minimal image size (~80MB vs ~400MB).
  # Data is persisted to a named volume to survive container recreation.
  # The health check ensures dependent services wait for PostgreSQL to be ready.
  # ============================================================================
  postgres:
    image: postgres:16-alpine
    container_name: lexichord-postgres
    restart: unless-stopped

    # Environment variables for database initialization
    environment:
      POSTGRES_USER: ${POSTGRES_USER:-lexichord}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-lexichord_dev}
      POSTGRES_DB: ${POSTGRES_DB:-lexichord}
      # Performance tuning for development
      POSTGRES_INITDB_ARGS: "--encoding=UTF8 --locale=C"

    # Port mapping: host:container
    # LOGIC: Map to 5432 for standard tooling compatibility.
    # Change host port if you have a local PostgreSQL installation.
    ports:
      - "${POSTGRES_PORT:-5432}:5432"

    # Persistent data volume
    # LOGIC: Named volume ensures data survives container recreation.
    # Use 'docker compose down -v' to delete the volume and reset data.
    volumes:
      - lexichord-pgdata:/var/lib/postgresql/data
      # Optional: Mount initialization scripts
      - ./scripts/db-init:/docker-entrypoint-initdb.d:ro

    # Health check configuration
    # LOGIC: pg_isready is the standard PostgreSQL readiness probe.
    # Dependent services can use 'depends_on' with 'condition: service_healthy'.
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-lexichord} -d ${POSTGRES_DB:-lexichord}"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s

    # Resource limits for development
    # LOGIC: Prevent runaway resource consumption on dev machines.
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M

    # Logging configuration
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

    # Network configuration
    networks:
      - lexichord-network

  # ============================================================================
  # pgAdmin 4 (Optional Database UI)
  # ============================================================================
  # LOGIC: Provides a web-based PostgreSQL administration interface.
  # Disabled by default; enable with: docker compose --profile tools up -d
  # ============================================================================
  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: lexichord-pgadmin
    profiles:
      - tools
    restart: unless-stopped

    environment:
      PGADMIN_DEFAULT_EMAIL: ${PGADMIN_EMAIL:-admin@lexichord.local}
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_PASSWORD:-admin}
      PGADMIN_CONFIG_SERVER_MODE: "False"
      PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED: "False"

    ports:
      - "${PGADMIN_PORT:-5050}:80"

    volumes:
      - lexichord-pgadmin:/var/lib/pgadmin

    depends_on:
      postgres:
        condition: service_healthy

    networks:
      - lexichord-network

# ============================================================================
# Named Volumes
# ============================================================================
volumes:
  lexichord-pgdata:
    name: lexichord-pgdata
  lexichord-pgadmin:
    name: lexichord-pgadmin

# ============================================================================
# Networks
# ============================================================================
networks:
  lexichord-network:
    name: lexichord-network
    driver: bridge
```

---

### Task 1.2: Create Environment File Template

**File:** `.env.example` (repository root)

```bash
# ============================================================================
# Lexichord Development Environment Configuration
# ============================================================================
# Copy this file to .env and customize for your local environment.
# NEVER commit .env to source control!
#
# Usage:
#   cp .env.example .env
#   # Edit .env with your settings
#   docker compose up -d
# ============================================================================

# PostgreSQL Configuration
# ============================================================================
POSTGRES_USER=lexichord
POSTGRES_PASSWORD=lexichord_dev
POSTGRES_DB=lexichord
POSTGRES_PORT=5432

# Connection String for Application
# ============================================================================
# This is the connection string used by Lexichord.Host
# Format: Host=<host>;Port=<port>;Database=<db>;Username=<user>;Password=<pass>
LEXICHORD_DATABASE__CONNECTIONSTRING=Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=lexichord_dev

# pgAdmin Configuration (Optional)
# ============================================================================
# Enable pgAdmin with: docker compose --profile tools up -d
PGADMIN_EMAIL=admin@lexichord.local
PGADMIN_PASSWORD=admin
PGADMIN_PORT=5050

# Development Settings
# ============================================================================
LEXICHORD_ENVIRONMENT=Development
LEXICHORD_DEBUGMODE=true
```

**File:** `.gitignore` (add these entries)

```gitignore
# Environment files (contain secrets)
.env
.env.local
.env.*.local

# Docker volumes (if accidentally committed)
lexichord-pgdata/
```

---

### Task 1.3: Create Development Scripts

**File:** `scripts/db-start.sh`

```bash
#!/usr/bin/env bash
# ============================================================================
# Lexichord Database Start Script
# ============================================================================
# Starts the PostgreSQL container and waits for health check.
#
# Usage:
#   ./scripts/db-start.sh           # Start with default settings
#   ./scripts/db-start.sh --tools   # Start with pgAdmin
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting Lexichord development database...${NC}"

# Check for Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed or not in PATH${NC}"
    echo "Please install Docker Desktop: https://www.docker.com/products/docker-desktop"
    exit 1
fi

# Check if Docker is running
if ! docker info &> /dev/null; then
    echo -e "${RED}Error: Docker daemon is not running${NC}"
    echo "Please start Docker Desktop and try again."
    exit 1
fi

# Check for .env file
if [[ ! -f "$PROJECT_ROOT/.env" ]]; then
    echo -e "${YELLOW}Warning: .env file not found, creating from template...${NC}"
    if [[ -f "$PROJECT_ROOT/.env.example" ]]; then
        cp "$PROJECT_ROOT/.env.example" "$PROJECT_ROOT/.env"
        echo -e "${GREEN}Created .env from .env.example${NC}"
    else
        echo -e "${RED}Error: .env.example not found${NC}"
        exit 1
    fi
fi

# Parse arguments
PROFILE_ARG=""
if [[ "${1:-}" == "--tools" ]]; then
    PROFILE_ARG="--profile tools"
    echo -e "${YELLOW}Starting with pgAdmin (tools profile)${NC}"
fi

# Change to project root for docker compose
cd "$PROJECT_ROOT"

# Start containers
echo "Running: docker compose $PROFILE_ARG up -d"
docker compose $PROFILE_ARG up -d

# Wait for health check
echo -e "${YELLOW}Waiting for PostgreSQL to be healthy...${NC}"
TIMEOUT=60
ELAPSED=0

while [[ $ELAPSED -lt $TIMEOUT ]]; do
    if docker compose ps postgres | grep -q "healthy"; then
        echo -e "${GREEN}PostgreSQL is healthy and ready!${NC}"
        echo ""
        echo "Connection details:"
        echo "  Host:     localhost"
        echo "  Port:     ${POSTGRES_PORT:-5432}"
        echo "  Database: ${POSTGRES_DB:-lexichord}"
        echo "  Username: ${POSTGRES_USER:-lexichord}"
        echo ""
        echo "Connection string:"
        echo "  Host=localhost;Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB:-lexichord};Username=${POSTGRES_USER:-lexichord};Password=<your_password>"

        if [[ -n "$PROFILE_ARG" ]]; then
            echo ""
            echo "pgAdmin available at: http://localhost:${PGADMIN_PORT:-5050}"
        fi

        exit 0
    fi

    sleep 2
    ELAPSED=$((ELAPSED + 2))
    echo -n "."
done

echo ""
echo -e "${RED}Error: PostgreSQL did not become healthy within ${TIMEOUT}s${NC}"
echo "Check logs with: docker compose logs postgres"
exit 1
```

**File:** `scripts/db-stop.sh`

```bash
#!/usr/bin/env bash
# ============================================================================
# Lexichord Database Stop Script
# ============================================================================
# Stops the PostgreSQL container (data is preserved).
#
# Usage:
#   ./scripts/db-stop.sh
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
GREEN='\033[0;32m'
NC='\033[0m'

echo -e "${GREEN}Stopping Lexichord development database...${NC}"

cd "$PROJECT_ROOT"

docker compose down

echo -e "${GREEN}Database stopped. Data volume preserved.${NC}"
echo "To remove data volume, run: docker compose down -v"
```

**File:** `scripts/db-reset.sh`

```bash
#!/usr/bin/env bash
# ============================================================================
# Lexichord Database Reset Script
# ============================================================================
# Completely resets the database by removing the data volume.
# WARNING: This will DELETE ALL DATA!
#
# Usage:
#   ./scripts/db-reset.sh           # Interactive (asks for confirmation)
#   ./scripts/db-reset.sh --force   # Non-interactive (for CI/CD)
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}=== DATABASE RESET ===${NC}"
echo -e "${RED}WARNING: This will DELETE ALL DATA in the development database!${NC}"
echo ""

# Check for --force flag
if [[ "${1:-}" != "--force" ]]; then
    read -p "Are you sure you want to reset the database? (yes/no): " CONFIRM
    if [[ "$CONFIRM" != "yes" ]]; then
        echo "Reset cancelled."
        exit 0
    fi
fi

cd "$PROJECT_ROOT"

echo -e "${YELLOW}Stopping containers...${NC}"
docker compose down --volumes --remove-orphans

echo -e "${YELLOW}Removing data volume...${NC}"
docker volume rm lexichord-pgdata 2>/dev/null || true
docker volume rm lexichord-pgadmin 2>/dev/null || true

echo -e "${GREEN}Database reset complete.${NC}"
echo ""
echo "To start fresh, run: ./scripts/db-start.sh"
```

**File:** `scripts/db-logs.sh`

```bash
#!/usr/bin/env bash
# ============================================================================
# Lexichord Database Logs Script
# ============================================================================
# Displays PostgreSQL container logs.
#
# Usage:
#   ./scripts/db-logs.sh            # Show last 100 lines
#   ./scripts/db-logs.sh -f         # Follow logs (Ctrl+C to exit)
#   ./scripts/db-logs.sh -n 500     # Show last 500 lines
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

# Default to last 100 lines if no args
if [[ $# -eq 0 ]]; then
    docker compose logs --tail 100 postgres
else
    docker compose logs "$@" postgres
fi
```

**Make scripts executable:**

```bash
chmod +x scripts/db-start.sh scripts/db-stop.sh scripts/db-reset.sh scripts/db-logs.sh
```

---

### Task 1.4: Create Database Initialization Script

**File:** `scripts/db-init/01-create-extensions.sql`

```sql
-- ============================================================================
-- Lexichord Database Initialization
-- ============================================================================
-- This script runs automatically when the PostgreSQL container is first created.
-- It creates required extensions and sets up initial configuration.
-- ============================================================================

-- Enable UUID generation (for gen_random_uuid())
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Enable full-text search helpers
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Enable case-insensitive text type
CREATE EXTENSION IF NOT EXISTS "citext";

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'Lexichord database extensions initialized successfully';
END $$;
```

---

## 4. Decision Tree: Docker Operations

```text
START: "What do I need to do with the database?"
|
+-- Need to start development?
|   +-- First time setup?
|   |   |-- Copy .env.example to .env
|   |   +-- Run: ./scripts/db-start.sh
|   |
|   +-- Returning developer?
|       +-- Run: ./scripts/db-start.sh
|
+-- Need to stop development?
|   +-- Keep data for later?
|   |   +-- Run: ./scripts/db-stop.sh
|   |
|   +-- Done for good / need fresh start?
|       +-- Run: ./scripts/db-reset.sh
|
+-- Need to debug database issues?
|   +-- View container logs:
|   |   +-- Run: ./scripts/db-logs.sh -f
|   |
|   +-- Connect directly:
|   |   +-- Run: docker exec -it lexichord-postgres psql -U lexichord
|   |
|   +-- Check container status:
|       +-- Run: docker compose ps
|
+-- Need database UI?
    +-- Run: ./scripts/db-start.sh --tools
    +-- Open: http://localhost:5050
```

---

## 5. Unit Testing Requirements

### 5.1 Test: Docker Compose Validation

```bash
#!/usr/bin/env bash
# File: tests/docker-compose-validation.sh

set -euo pipefail

echo "=== Docker Compose Validation Tests ==="

# Test 1: docker-compose.yml syntax is valid
echo "Test 1: Validating docker-compose.yml syntax..."
if docker compose config --quiet; then
    echo "PASS: docker-compose.yml syntax is valid"
else
    echo "FAIL: docker-compose.yml has syntax errors"
    exit 1
fi

# Test 2: Required services are defined
echo "Test 2: Checking required services..."
SERVICES=$(docker compose config --services)
if echo "$SERVICES" | grep -q "postgres"; then
    echo "PASS: postgres service is defined"
else
    echo "FAIL: postgres service is missing"
    exit 1
fi

# Test 3: Health check is configured
echo "Test 3: Checking health check configuration..."
if docker compose config | grep -q "pg_isready"; then
    echo "PASS: Health check is configured"
else
    echo "FAIL: Health check is missing"
    exit 1
fi

# Test 4: Volume is configured
echo "Test 4: Checking volume configuration..."
if docker compose config | grep -q "lexichord-pgdata"; then
    echo "PASS: Data volume is configured"
else
    echo "FAIL: Data volume is missing"
    exit 1
fi

echo ""
echo "=== All validation tests passed ==="
```

### 5.2 Test: Container Lifecycle

```csharp
[TestFixture]
[Category("Docker")]
[Explicit("Requires Docker to be running")]
public class DockerContainerTests
{
    [Test]
    public async Task PostgresContainer_StartsAndBecomesHealthy()
    {
        // Arrange
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose up -d",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Act
        using var process = Process.Start(processStartInfo);
        await process!.WaitForExitAsync();

        // Wait for health check
        var healthy = false;
        for (var i = 0; i < 30; i++)
        {
            var statusProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose ps postgres --format json",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            var output = await statusProcess!.StandardOutput.ReadToEndAsync();
            await statusProcess.WaitForExitAsync();

            if (output.Contains("healthy"))
            {
                healthy = true;
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Assert
        Assert.That(healthy, Is.True, "PostgreSQL container should become healthy within 60 seconds");

        // Cleanup
        Process.Start(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose down",
            UseShellExecute = false
        })?.WaitForExit();
    }

    [Test]
    public async Task PostgresContainer_DataPersistsAcrossRestarts()
    {
        // Arrange - Start container and insert data
        await RunDockerCommand("compose up -d");
        await WaitForHealthy();

        var testValue = Guid.NewGuid().ToString();
        await RunPsql($"INSERT INTO \"SystemSettings\" (\"Key\", \"Value\") VALUES ('test_key', '{testValue}')");

        // Act - Restart container
        await RunDockerCommand("compose down");
        await RunDockerCommand("compose up -d");
        await WaitForHealthy();

        // Assert - Data should persist
        var result = await RunPsql("SELECT \"Value\" FROM \"SystemSettings\" WHERE \"Key\" = 'test_key'");
        Assert.That(result, Does.Contain(testValue));

        // Cleanup
        await RunDockerCommand("compose down -v");
    }

    private static async Task RunDockerCommand(string args)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            UseShellExecute = false
        });
        await process!.WaitForExitAsync();
    }

    private static async Task<string> RunPsql(string sql)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec lexichord-postgres psql -U lexichord -c \"{sql}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        var output = await process!.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output;
    }

    private static async Task WaitForHealthy()
    {
        for (var i = 0; i < 30; i++)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "compose ps postgres",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            var output = await process!.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (output.Contains("healthy"))
                return;

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException("Container did not become healthy");
    }
}
```

---

## 6. Observability & Logging

### 6.1 Container Logs

PostgreSQL container logs are accessible via:

```bash
# View last 100 lines
docker compose logs --tail 100 postgres

# Follow logs in real-time
docker compose logs -f postgres

# View logs with timestamps
docker compose logs -t postgres
```

### 6.2 Health Check Status

```bash
# Check container health status
docker compose ps

# Detailed health check info
docker inspect lexichord-postgres --format='{{json .State.Health}}'
```

### 6.3 Log Events from Scripts

| Script         | Level | Message                                                  |
| :------------- | :---- | :------------------------------------------------------- |
| db-start.sh    | INFO  | Starting Lexichord development database...               |
| db-start.sh    | WARN  | Warning: .env file not found, creating from template...  |
| db-start.sh    | INFO  | PostgreSQL is healthy and ready!                         |
| db-start.sh    | ERROR | Error: PostgreSQL did not become healthy within 60s      |
| db-stop.sh     | INFO  | Stopping Lexichord development database...               |
| db-stop.sh     | INFO  | Database stopped. Data volume preserved.                 |
| db-reset.sh    | WARN  | WARNING: This will DELETE ALL DATA!                      |
| db-reset.sh    | INFO  | Database reset complete.                                 |

---

## 7. Security & Safety

### 7.1 Credential Management

> [!WARNING]
> The default credentials in `.env.example` are for development only.
> NEVER use these credentials in production.

**Development Credentials:**

- Username: `lexichord`
- Password: `lexichord_dev`

These are intentionally simple for local development. Production deployments must use:

- Strong, randomly generated passwords (32+ characters)
- Secrets management (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
- Network isolation (private subnets, VPN)

### 7.2 Network Security

The Docker network `lexichord-network` is a bridge network, isolated from other containers. In production:

- Use encrypted connections (SSL/TLS)
- Implement IP whitelisting
- Use VPC peering or private endpoints

---

## 8. Definition of Done

- [ ] `docker-compose.yml` created in repository root
- [ ] PostgreSQL 16-alpine image configured
- [ ] Health check using `pg_isready` configured
- [ ] Named volume `lexichord-pgdata` configured
- [ ] `.env.example` template created
- [ ] `.env` added to `.gitignore`
- [ ] `scripts/db-start.sh` created and executable
- [ ] `scripts/db-stop.sh` created and executable
- [ ] `scripts/db-reset.sh` created and executable
- [ ] `scripts/db-logs.sh` created and executable
- [ ] `scripts/db-init/01-create-extensions.sql` created
- [ ] pgAdmin profile configured (optional tools)
- [ ] Container starts and becomes healthy within 60 seconds
- [ ] Data persists across container restarts
- [ ] Data is deleted when using `db-reset.sh`
- [ ] Validation tests pass

---

## 9. Verification Commands

```bash
# 1. Validate docker-compose.yml syntax
docker compose config --quiet && echo "Syntax valid"

# 2. Start the database
./scripts/db-start.sh

# 3. Verify container is healthy
docker compose ps
# Should show: lexichord-postgres ... healthy

# 4. Connect with psql
docker exec -it lexichord-postgres psql -U lexichord -c "SELECT version();"

# 5. Verify extensions are installed
docker exec -it lexichord-postgres psql -U lexichord -c "\dx"

# 6. Test data persistence
docker exec -it lexichord-postgres psql -U lexichord -c "CREATE TABLE test_persist (id serial PRIMARY KEY);"
./scripts/db-stop.sh
./scripts/db-start.sh
docker exec -it lexichord-postgres psql -U lexichord -c "\dt"
# Should show test_persist table

# 7. Test reset (WARNING: deletes all data)
./scripts/db-reset.sh --force
./scripts/db-start.sh
docker exec -it lexichord-postgres psql -U lexichord -c "\dt"
# Should show empty list (no tables)

# 8. Start with pgAdmin
./scripts/db-start.sh --tools
# Open http://localhost:5050 in browser

# 9. Cleanup
./scripts/db-stop.sh
```

---

## 10. Troubleshooting Guide

### Issue: Container fails to start

**Symptoms:** `docker compose up` fails or container exits immediately.

**Solutions:**

1. Check if port 5432 is already in use:
   ```bash
   lsof -i :5432
   # If in use, either stop the service or change POSTGRES_PORT in .env
   ```

2. Check Docker logs:
   ```bash
   docker compose logs postgres
   ```

3. Ensure Docker has enough resources:
   - Docker Desktop > Settings > Resources
   - Minimum: 2GB RAM, 1 CPU

### Issue: Health check never passes

**Symptoms:** Container stays in "starting" state indefinitely.

**Solutions:**

1. Check PostgreSQL logs for errors:
   ```bash
   docker compose logs postgres | grep -i error
   ```

2. Verify credentials match between docker-compose.yml and health check:
   ```bash
   docker compose config | grep -A5 healthcheck
   ```

3. Try connecting manually:
   ```bash
   docker exec -it lexichord-postgres pg_isready -U lexichord
   ```

### Issue: Data not persisting

**Symptoms:** Data disappears after container restart.

**Solutions:**

1. Verify volume exists:
   ```bash
   docker volume ls | grep lexichord
   ```

2. Check volume mount in container:
   ```bash
   docker inspect lexichord-postgres | grep -A10 Mounts
   ```

3. Ensure you're using `docker compose down` not `docker compose down -v`

### Issue: Permission denied on scripts

**Symptoms:** `./scripts/db-start.sh: Permission denied`

**Solution:**

```bash
chmod +x scripts/*.sh
```

### Issue: Connection refused from application

**Symptoms:** Application cannot connect to `localhost:5432`.

**Solutions:**

1. Verify container is running:
   ```bash
   docker compose ps
   ```

2. Check port mapping:
   ```bash
   docker port lexichord-postgres
   ```

3. Test connection from host:
   ```bash
   nc -zv localhost 5432
   ```

4. On Docker Desktop for Mac/Windows, ensure port forwarding is enabled.
