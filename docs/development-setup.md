# Lexichord Development Setup

This guide covers setting up your local development environment for Lexichord.

## Prerequisites

- **.NET 9.0 SDK** or later
- **Docker Desktop** (or Docker Engine + Docker Compose)
- **Git**

## Quick Start

```bash
# Clone the repository
git clone https://github.com/your-org/lexichord.git
cd lexichord

# Copy environment template
cp .env.example .env

# Start the database
./scripts/db-start.sh

# Verify pgvector extension (v0.4.1a)
./scripts/test-pgvector.sh

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Database Setup

### PostgreSQL with pgvector

Lexichord uses PostgreSQL 16 with the [pgvector](https://github.com/pgvector/pgvector) extension for vector storage and semantic search capabilities.

**Starting the database:**

```bash
./scripts/db-start.sh
```

**Stopping the database:**

```bash
./scripts/db-stop.sh
```

**Viewing logs:**

```bash
./scripts/db-logs.sh
```

**Reset database (deletes all data):**

```bash
./scripts/db-reset.sh
```

### Verifying pgvector Installation

After starting the database, verify pgvector is working:

```bash
./scripts/test-pgvector.sh
```

Expected output:

```
==============================================
Lexichord pgvector Verification (v0.4.1a)
==============================================

Checking if PostgreSQL container is running... OK
Checking PostgreSQL readiness... OK
Checking pgvector extension is installed... OK
Getting pgvector version... v0.7.0
Testing vector type creation... OK
Testing vector insert operation... OK
Testing vector similarity search... OK
Cleaning up test table... OK

==============================================
All pgvector verification checks passed!
==============================================
```

You can also verify manually via psql:

```bash
docker exec -it lexichord-postgres psql -U lexichord -d lexichord -c "\dx vector"
```

### Connection Details

| Setting  | Default Value   |
| -------- | --------------- |
| Host     | `localhost`     |
| Port     | `5432`          |
| Database | `lexichord`     |
| Username | `lexichord`     |
| Password | `lexichord_dev` |

Connection string:

```
Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=lexichord_dev
```

### Optional: pgAdmin

For a web-based database UI:

```bash
docker compose --profile tools up -d
```

Access pgAdmin at http://localhost:5050 (default credentials in `.env.example`).

## Environment Variables

Copy `.env.example` to `.env` and customize as needed:

```bash
cp .env.example .env
```

See `.env.example` for all available configuration options.

## Troubleshooting

### Container fails to start

1. Ensure Docker is running
2. Check if port 5432 is already in use: `lsof -i :5432`
3. Review container logs: `./scripts/db-logs.sh`

### pgvector extension not found

1. Verify you're using the `pgvector/pgvector:pg16` image (check `docker-compose.yml`)
2. Reset the database to re-run init scripts: `./scripts/db-reset.sh`
3. Check init script output in container logs

### Health check failing

The health check verifies both PostgreSQL readiness AND pgvector availability:

```bash
docker inspect lexichord-postgres --format='{{.State.Health.Status}}'
```

Wait for status to be `healthy` before connecting.
