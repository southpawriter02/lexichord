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
