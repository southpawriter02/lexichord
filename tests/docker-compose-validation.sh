#!/usr/bin/env bash
# ============================================================================
# Docker Compose Validation Tests
# ============================================================================
# Validates docker-compose.yml syntax and configuration.
#
# Usage:
#   ./tests/docker-compose-validation.sh
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

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
