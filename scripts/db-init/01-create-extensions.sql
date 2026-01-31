-- ============================================================================
-- Lexichord Database Initialization
-- ============================================================================
-- This script runs automatically when the PostgreSQL container is first created.
-- It creates required extensions and sets up initial configuration.
-- v0.4.1a: Added pgvector extension for semantic search vector storage.
-- ============================================================================

-- Enable UUID generation (for gen_random_uuid())
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Enable full-text search helpers
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Enable case-insensitive text type
CREATE EXTENSION IF NOT EXISTS "citext";

-- v0.4.1a: Enable vector storage for semantic search
-- The pgvector extension provides vector similarity search capabilities
CREATE EXTENSION IF NOT EXISTS "vector";

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'Lexichord database extensions initialized successfully (including pgvector)';
END $$;

-- v0.4.1a: Verify pgvector extension is loaded correctly
DO $$
DECLARE
    ext_version TEXT;
BEGIN
    SELECT extversion INTO ext_version FROM pg_extension WHERE extname = 'vector';
    IF ext_version IS NULL THEN
        RAISE EXCEPTION 'pgvector extension failed to load';
    ELSE
        RAISE NOTICE 'pgvector extension version % loaded successfully', ext_version;
    END IF;
END $$;

