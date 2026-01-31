-- ============================================================================
-- Lexichord PostgreSQL Performance Configuration
-- ============================================================================
-- v0.4.1a: Performance tuning for vector workloads.
-- This script runs after extension creation during container initialization.
-- ============================================================================

-- Note: These settings are optimized for development environments.
-- Production deployments should tune based on available resources.

-- Increase work memory for vector operations (sorting large arrays)
-- Default is 4MB, we increase for better vector index build performance
ALTER SYSTEM SET work_mem = '64MB';

-- Increase maintenance work memory for index creation
-- Important for building IVFFlat or HNSW indexes on large datasets
ALTER SYSTEM SET maintenance_work_mem = '256MB';

-- Increase effective cache size estimate
-- Helps planner make better decisions about index usage
ALTER SYSTEM SET effective_cache_size = '512MB';

-- Reload configuration
SELECT pg_reload_conf();

-- Log successful configuration
DO $$
BEGIN
    RAISE NOTICE 'Lexichord PostgreSQL performance configuration applied (v0.4.1a)';
END $$;
