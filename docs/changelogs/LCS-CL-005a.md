# LCS-CL-005a: Docker Orchestration

## Version Information

| Field        | Value                |
| :----------- | :------------------- |
| Version      | v0.0.5a              |
| Feature Name | Docker Orchestration |
| Release Date | 2026-01-28           |
| Status       | ✅ Complete          |

---

## Summary

Implemented Docker Compose orchestration for a reproducible PostgreSQL 16 development environment.
This provides a consistent database setup across all developer machines with health checks,
data persistence, and optional pgAdmin administration interface.

---

## What's New

### Docker Compose Configuration

- **PostgreSQL 16 Service** — Alpine-based image for minimal footprint (~80MB)
    - Named volume `lexichord-pgdata` for data persistence
    - Health check using `pg_isready` command
    - Resource limits (512MB memory) for development safety
    - Logging configuration with rotation

- **pgAdmin 4 (Optional)** — Web-based database administration
    - Enabled via `--profile tools` flag
    - Pre-configured for single-user mode

### Environment Configuration

- **`.env.example` Template** — Complete configuration template including:
    - PostgreSQL credentials and port
    - Application connection string
    - pgAdmin settings

### Development Scripts

| Script        | Purpose                                    |
| :------------ | :----------------------------------------- |
| `db-start.sh` | Starts containers, waits for health check  |
| `db-stop.sh`  | Stops containers, preserves data volume    |
| `db-reset.sh` | Deletes all data and volumes (interactive) |
| `db-logs.sh`  | Views PostgreSQL container logs            |

### Database Extensions

- **`pgcrypto`** — UUID generation via `gen_random_uuid()`
- **`pg_trgm`** — Full-text search helpers
- **`citext`** — Case-insensitive text type

---

## Files Created

| File                                       | Purpose                          |
| :----------------------------------------- | :------------------------------- |
| `docker-compose.yml`                       | Service orchestration            |
| `.env.example`                             | Environment template             |
| `scripts/db-start.sh`                      | Start database with health wait  |
| `scripts/db-stop.sh`                       | Stop database, preserve data     |
| `scripts/db-reset.sh`                      | Reset database (delete all data) |
| `scripts/db-logs.sh`                       | View container logs              |
| `scripts/db-init/01-create-extensions.sql` | Extension initialization         |
| `tests/docker-compose-validation.sh`       | Compose file validation          |

---

## Usage

### First-Time Setup

```bash
# Copy environment template
cp .env.example .env

# Start the database
./scripts/db-start.sh
```

### Daily Development

```bash
# Start database
./scripts/db-start.sh

# Stop database (preserves data)
./scripts/db-stop.sh
```

### With pgAdmin

```bash
# Start with admin UI
./scripts/db-start.sh --tools

# Access at http://localhost:5050
```

### Reset Database

```bash
# Interactive (asks confirmation)
./scripts/db-reset.sh

# Non-interactive (for CI)
./scripts/db-reset.sh --force
```

---

## Verification

```bash
# Validate docker-compose.yml
./tests/docker-compose-validation.sh

# Check container health
docker compose ps

# Connect with psql
docker exec -it lexichord-postgres psql -U lexichord -c "SELECT version();"

# Verify extensions
docker exec -it lexichord-postgres psql -U lexichord -c "\dx"
```

---

## Related Documents

- **Design Specification**: [LCS-DES-005a.md](../specs/v0.0.x/v0.0.5/LCS-DES-005a.md)
- **Parent Version**: [LCS-DES-005-INDEX.md](../specs/v0.0.x/v0.0.5/LCS-DES-005-INDEX.md)
- **Scope Breakdown**: [LCS-SBD-005.md](../specs/v0.0.x/v0.0.5/LCS-SBD-005.md)
