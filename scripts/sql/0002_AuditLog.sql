-- =============================================================================
-- 0002_AuditLog.sql
-- Creates the AuditLog table used for change-data capture across the system.
-- This script is idempotent.
-- =============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AuditLog'
)
BEGIN
    CREATE TABLE dbo.AuditLog (
        Id            BIGINT          IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AuditLog PRIMARY KEY,
        EntityName    NVARCHAR(200)   NOT NULL,
        EntityId      NVARCHAR(100)   NOT NULL,
        Action        NVARCHAR(50)    NOT NULL,   -- INSERT | UPDATE | DELETE
        OldValues     NVARCHAR(MAX)   NULL,       -- JSON snapshot before change
        NewValues     NVARCHAR(MAX)   NULL,       -- JSON snapshot after change
        AffectedColumns NVARCHAR(MAX) NULL,       -- comma-separated changed column names
        PerformedBy   NVARCHAR(256)   NULL,
        PerformedAt   DATETIME2       NOT NULL
            CONSTRAINT DF_AuditLog_PerformedAt DEFAULT SYSUTCDATETIME(),
        IpAddress     NVARCHAR(50)    NULL,
        UserAgent     NVARCHAR(500)   NULL
    );

    CREATE INDEX IX_AuditLog_EntityName_EntityId
        ON dbo.AuditLog (EntityName, EntityId);

    CREATE INDEX IX_AuditLog_PerformedAt
        ON dbo.AuditLog (PerformedAt DESC);

    CREATE INDEX IX_AuditLog_PerformedBy
        ON dbo.AuditLog (PerformedBy);
END
GO
