#!/usr/bin/env bash
# ============================================================================
# Lexichord pgvector Verification Script
# ============================================================================
# v0.4.1a: Verifies that PostgreSQL is running with the pgvector extension.
# Usage: ./scripts/test-pgvector.sh
# ============================================================================

set -e

# Configuration
CONTAINER_NAME="${CONTAINER_NAME:-lexichord-postgres}"
POSTGRES_USER="${POSTGRES_USER:-lexichord}"
POSTGRES_DB="${POSTGRES_DB:-lexichord}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=============================================="
echo "Lexichord pgvector Verification (v0.4.1a)"
echo "=============================================="
echo ""

# Check if container is running
echo -n "Checking if PostgreSQL container is running... "
if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo -e "${RED}FAILED${NC}"
    echo "Error: Container '${CONTAINER_NAME}' is not running."
    echo "Run ./scripts/db-start.sh first."
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# Check PostgreSQL readiness
echo -n "Checking PostgreSQL readiness... "
if ! docker exec "${CONTAINER_NAME}" pg_isready -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" > /dev/null 2>&1; then
    echo -e "${RED}FAILED${NC}"
    echo "Error: PostgreSQL is not ready to accept connections."
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# Check pgvector extension
echo -n "Checking pgvector extension is installed... "
VECTOR_CHECK=$(docker exec "${CONTAINER_NAME}" psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -tAc \
    "SELECT extname FROM pg_extension WHERE extname = 'vector';" 2>/dev/null)
if [ "${VECTOR_CHECK}" != "vector" ]; then
    echo -e "${RED}FAILED${NC}"
    echo "Error: pgvector extension is not installed."
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# Get pgvector version
echo -n "Getting pgvector version... "
VECTOR_VERSION=$(docker exec "${CONTAINER_NAME}" psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -tAc \
    "SELECT extversion FROM pg_extension WHERE extname = 'vector';" 2>/dev/null)
echo -e "${GREEN}v${VECTOR_VERSION}${NC}"

# Test vector operations
echo -n "Testing vector type creation... "
docker exec "${CONTAINER_NAME}" psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -c \
    "DROP TABLE IF EXISTS _pgvector_test; CREATE TABLE _pgvector_test (id serial PRIMARY KEY, embedding vector(3));" \
    > /dev/null 2>&1
echo -e "${GREEN}OK${NC}"

echo -n "Testing vector insert operation... "
docker exec "${CONTAINER_NAME}" psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -c \
    "INSERT INTO _pgvector_test (embedding) VALUES ('[1,2,3]'), ('[4,5,6]');" \
    > /dev/null 2>&1
echo -e "${GREEN}OK${NC}"

echo -n "Testing vector similarity search... "
SIMILARITY_RESULT=$(docker exec "${CONTAINER_NAME}" psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -tAc \
    "SELECT id FROM _pgvector_test ORDER BY embedding <-> '[3,1,2]' LIMIT 1;" 2>/dev/null)
if [ "${SIMILARITY_RESULT}" != "1" ]; then
    echo -e "${RED}FAILED${NC}"
    echo "Error: Vector similarity search returned unexpected result: ${SIMILARITY_RESULT}"
    exit 1
fi
echo -e "${GREEN}OK${NC}"

# Cleanup test table
echo -n "Cleaning up test table... "
docker exec "${CONTAINER_NAME}" psql -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" -c \
    "DROP TABLE IF EXISTS _pgvector_test;" \
    > /dev/null 2>&1
echo -e "${GREEN}OK${NC}"

echo ""
echo "=============================================="
echo -e "${GREEN}All pgvector verification checks passed!${NC}"
echo "=============================================="
echo ""
echo "pgvector version: v${VECTOR_VERSION}"
echo "Container: ${CONTAINER_NAME}"
echo "Database: ${POSTGRES_DB}"
echo ""
