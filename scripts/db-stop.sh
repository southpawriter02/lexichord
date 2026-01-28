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
