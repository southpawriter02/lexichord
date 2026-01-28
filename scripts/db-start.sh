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
