-- Initial schema for Order Event Tracking & Audit Trail System
-- This script creates the base tables and indexes for the event store

-- Create extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create extension for JSONB operations
CREATE EXTENSION IF NOT EXISTS "btree_gin";

-- Create events table
CREATE TABLE IF NOT EXISTS events (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL UNIQUE DEFAULT uuid_generate_v4(),
    event_type VARCHAR(255) NOT NULL,
    aggregate_id VARCHAR(255) NOT NULL,
    aggregate_type VARCHAR(255) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    version INTEGER NOT NULL,
    payload JSONB NOT NULL,
    metadata JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Ensure event ordering per aggregate
    CONSTRAINT events_aggregate_version_unique UNIQUE (aggregate_id, aggregate_type, version)
);

-- Create indexes for common query patterns
CREATE INDEX IF NOT EXISTS idx_events_aggregate ON events(aggregate_type, aggregate_id);
CREATE INDEX IF NOT EXISTS idx_events_type ON events(event_type);
CREATE INDEX IF NOT EXISTS idx_events_timestamp ON events(timestamp);
CREATE INDEX IF NOT EXISTS idx_events_created_at ON events(created_at);
CREATE INDEX IF NOT EXISTS idx_events_payload ON events USING GIN(payload);
CREATE INDEX IF NOT EXISTS idx_events_metadata ON events USING GIN(metadata);

-- Create composite indexes for common filtering patterns
CREATE INDEX IF NOT EXISTS idx_events_type_timestamp ON events(event_type, timestamp);
CREATE INDEX IF NOT EXISTS idx_events_aggregate_timestamp ON events(aggregate_type, aggregate_id, timestamp);

-- Create table for storing replay operations
CREATE TABLE IF NOT EXISTS replay_operations (
    id BIGSERIAL PRIMARY KEY,
    replay_id UUID NOT NULL UNIQUE DEFAULT uuid_generate_v4(),
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    filters JSONB NOT NULL,
    destination JSONB NOT NULL,
    progress JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    error_message TEXT,
    
    CONSTRAINT replay_operations_status_check CHECK (status IN ('pending', 'running', 'completed', 'failed', 'cancelled'))
);

-- Create indexes for replay operations
CREATE INDEX IF NOT EXISTS idx_replay_operations_status ON replay_operations(status);
CREATE INDEX IF NOT EXISTS idx_replay_operations_created_at ON replay_operations(created_at);

-- Create table for dead letter queue events
CREATE TABLE IF NOT EXISTS dead_letter_events (
    id BIGSERIAL PRIMARY KEY,
    original_topic VARCHAR(255) NOT NULL,
    original_partition INTEGER,
    original_offset BIGINT,
    original_key TEXT,
    original_value TEXT,
    original_headers JSONB,
    error_message TEXT NOT NULL,
    error_stack_trace TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_retry_at TIMESTAMPTZ,
    resolved_at TIMESTAMPTZ,
    resolved_by VARCHAR(255)
);

-- Create indexes for dead letter events
CREATE INDEX IF NOT EXISTS idx_dead_letter_events_topic ON dead_letter_events(original_topic);
CREATE INDEX IF NOT EXISTS idx_dead_letter_events_created_at ON dead_letter_events(created_at);
CREATE INDEX IF NOT EXISTS idx_dead_letter_events_resolved ON dead_letter_events(resolved_at) WHERE resolved_at IS NULL;

-- Create table for system metrics and health checks
CREATE TABLE IF NOT EXISTS system_metrics (
    id BIGSERIAL PRIMARY KEY,
    metric_name VARCHAR(255) NOT NULL,
    metric_value JSONB NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Partition constraint for time-based partitioning
    CONSTRAINT system_metrics_timestamp_check CHECK (timestamp >= '2025-01-01'::timestamptz)
);

-- Create indexes for system metrics
CREATE INDEX IF NOT EXISTS idx_system_metrics_name_timestamp ON system_metrics(metric_name, timestamp);
CREATE INDEX IF NOT EXISTS idx_system_metrics_timestamp ON system_metrics(timestamp);

-- Create table for audit log of API access
CREATE TABLE IF NOT EXISTS audit_log (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(255),
    action VARCHAR(255) NOT NULL,
    resource VARCHAR(255) NOT NULL,
    request_data JSONB,
    response_status INTEGER,
    ip_address INET,
    user_agent TEXT,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for audit log
CREATE INDEX IF NOT EXISTS idx_audit_log_user_id ON audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_action ON audit_log(action);
CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON audit_log(timestamp);

-- Create views for common queries
CREATE OR REPLACE VIEW event_summary AS
SELECT 
    event_type,
    aggregate_type,
    COUNT(*) as event_count,
    MIN(timestamp) as first_event,
    MAX(timestamp) as last_event,
    DATE_TRUNC('hour', timestamp) as hour_bucket
FROM events
GROUP BY event_type, aggregate_type, DATE_TRUNC('hour', timestamp);

CREATE OR REPLACE VIEW aggregate_stream AS
SELECT 
    aggregate_type,
    aggregate_id,
    COUNT(*) as event_count,
    MAX(version) as latest_version,
    MIN(timestamp) as first_event,
    MAX(timestamp) as last_event
FROM events
GROUP BY aggregate_type, aggregate_id;

-- Create function for event statistics
CREATE OR REPLACE FUNCTION get_event_statistics(
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_event_type VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    event_type VARCHAR(255),
    event_count BIGINT,
    first_event TIMESTAMPTZ,
    last_event TIMESTAMPTZ,
    avg_events_per_hour NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.event_type,
        COUNT(*) as event_count,
        MIN(e.timestamp) as first_event,
        MAX(e.timestamp) as last_event,
        ROUND(
            COUNT(*)::NUMERIC / 
            GREATEST(1, EXTRACT(EPOCH FROM (MAX(e.timestamp) - MIN(e.timestamp))) / 3600),
            2
        ) as avg_events_per_hour
    FROM events e
    WHERE 
        (p_start_date IS NULL OR e.timestamp >= p_start_date) AND
        (p_end_date IS NULL OR e.timestamp <= p_end_date) AND
        (p_event_type IS NULL OR e.event_type = p_event_type)
    GROUP BY e.event_type
    ORDER BY event_count DESC;
END;
$$ LANGUAGE plpgsql;

-- Create function for aggregate event history
CREATE OR REPLACE FUNCTION get_aggregate_events(
    p_aggregate_type VARCHAR(255),
    p_aggregate_id VARCHAR(255),
    p_from_version INTEGER DEFAULT 1,
    p_to_version INTEGER DEFAULT NULL
)
RETURNS TABLE (
    event_id UUID,
    event_type VARCHAR(255),
    version INTEGER,
    timestamp TIMESTAMPTZ,
    payload JSONB,
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.event_id,
        e.event_type,
        e.version,
        e.timestamp,
        e.payload,
        e.metadata
    FROM events e
    WHERE 
        e.aggregate_type = p_aggregate_type AND
        e.aggregate_id = p_aggregate_id AND
        e.version >= p_from_version AND
        (p_to_version IS NULL OR e.version <= p_to_version)
    ORDER BY e.version ASC;
END;
$$ LANGUAGE plpgsql;

-- Create trigger function to prevent updates/deletes on events table
CREATE OR REPLACE FUNCTION prevent_event_modifications()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'UPDATE' THEN
        RAISE EXCEPTION 'Updates are not allowed on events table - append-only store';
    END IF;
    
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Deletes are not allowed on events table - immutable store';
    END IF;
    
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create triggers to enforce immutability
CREATE TRIGGER prevent_event_updates
    BEFORE UPDATE ON events
    FOR EACH ROW
    EXECUTE FUNCTION prevent_event_modifications();

CREATE TRIGGER prevent_event_deletes
    BEFORE DELETE ON events
    FOR EACH ROW
    EXECUTE FUNCTION prevent_event_modifications();

-- Create partitioning function for time-based partitioning (future enhancement)
CREATE OR REPLACE FUNCTION create_monthly_partition(table_name TEXT, start_date DATE)
RETURNS VOID AS $$
DECLARE
    partition_name TEXT;
    end_date DATE;
BEGIN
    end_date := start_date + INTERVAL '1 month';
    partition_name := table_name || '_' || TO_CHAR(start_date, 'YYYY_MM');
    
    EXECUTE FORMAT('CREATE TABLE IF NOT EXISTS %I PARTITION OF %I
                   FOR VALUES FROM (%L) TO (%L)',
                   partition_name, table_name, start_date, end_date);
                   
    EXECUTE FORMAT('CREATE INDEX IF NOT EXISTS idx_%I_timestamp ON %I (timestamp)',
                   partition_name, partition_name);
END;
$$ LANGUAGE plpgsql;

-- Insert initial system metrics
INSERT INTO system_metrics (metric_name, metric_value) VALUES
('schema_version', '{"version": "1.0.0", "created_at": "2025-01-08T10:00:00Z"}'),
('database_initialized', '{"status": "completed", "timestamp": "2025-01-08T10:00:00Z"}');

-- Create user for application access (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_user WHERE usename = 'audit_user') THEN
        CREATE USER audit_user WITH PASSWORD 'your_secure_password';
    END IF;
END
$$;

-- Grant permissions to application user
GRANT SELECT, INSERT ON events TO audit_user;
GRANT SELECT, INSERT, UPDATE ON replay_operations TO audit_user;
GRANT SELECT, INSERT, UPDATE ON dead_letter_events TO audit_user;
GRANT SELECT, INSERT ON system_metrics TO audit_user;
GRANT SELECT, INSERT ON audit_log TO audit_user;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO audit_user;

-- Create notification function for new events (optional for real-time features)
CREATE OR REPLACE FUNCTION notify_new_event()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('new_event', 
        json_build_object(
            'event_id', NEW.event_id,
            'event_type', NEW.event_type,
            'aggregate_type', NEW.aggregate_type,
            'aggregate_id', NEW.aggregate_id,
            'timestamp', NEW.timestamp
        )::text
    );
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for event notifications
CREATE TRIGGER notify_event_inserted
    AFTER INSERT ON events
    FOR EACH ROW
    EXECUTE FUNCTION notify_new_event();

-- Comments for documentation
COMMENT ON TABLE events IS 'Immutable event store containing all business events';
COMMENT ON COLUMN events.event_id IS 'Unique identifier for each event';
COMMENT ON COLUMN events.aggregate_id IS 'Identifier of the aggregate that generated the event';
COMMENT ON COLUMN events.version IS 'Version number of the event within the aggregate stream';
COMMENT ON COLUMN events.payload IS 'Event-specific data in JSON format';
COMMENT ON COLUMN events.metadata IS 'Event metadata including correlation IDs and source information';

COMMENT ON TABLE replay_operations IS 'Tracks event replay operations for audit and monitoring';
COMMENT ON TABLE dead_letter_events IS 'Events that failed processing and require manual intervention';
COMMENT ON TABLE system_metrics IS 'System performance and health metrics';
COMMENT ON TABLE audit_log IS 'Audit trail of API access and operations';

-- Set up row-level security (RLS) for future multi-tenancy
-- ALTER TABLE events ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE audit_log ENABLE ROW LEVEL SECURITY;

-- Analyze tables for query optimization
ANALYZE events;
ANALYZE replay_operations;
ANALYZE dead_letter_events;
ANALYZE system_metrics;
ANALYZE audit_log;
