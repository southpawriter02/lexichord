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
