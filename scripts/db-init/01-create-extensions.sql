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
