-- =============================================================================
-- 0002_CoreTables.sql
-- Creates the core security domain tables.
-- All tables include standard audit columns and soft-delete support.
-- This script is idempotent: every DDL statement is wrapped in an existence
-- check so re-running the script on an existing database is safe.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Company
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'Company'
)
BEGIN
    CREATE TABLE dbo.Company (
        Id          INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Company PRIMARY KEY,
        Name        NVARCHAR(200)  NOT NULL,
        Code        NVARCHAR(50)   NOT NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_Company_IsActive DEFAULT 1,
        CreatedBy   NVARCHAR(256)  NULL,
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_Company_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy   NVARCHAR(256)  NULL,
        UpdatedDate DATETIME2      NULL,
        DeletedBy   NVARCHAR(256)  NULL,
        DeletedDate DATETIME2      NULL
    );

    CREATE UNIQUE INDEX UX_Company_Code
        ON dbo.Company (Code)
        WHERE DeletedDate IS NULL;
END
GO

-- -----------------------------------------------------------------------------
-- Module
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'Module'
)
BEGIN
    CREATE TABLE dbo.Module (
        Id          INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Module PRIMARY KEY,
        Name        NVARCHAR(200)  NOT NULL,
        Code        NVARCHAR(100)  NOT NULL,
        DisplayOrder INT           NOT NULL CONSTRAINT DF_Module_DisplayOrder DEFAULT 0,
        IsActive    BIT            NOT NULL CONSTRAINT DF_Module_IsActive DEFAULT 1,
        CreatedBy   NVARCHAR(256)  NULL,
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_Module_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy   NVARCHAR(256)  NULL,
        UpdatedDate DATETIME2      NULL,
        DeletedBy   NVARCHAR(256)  NULL,
        DeletedDate DATETIME2      NULL
    );

    CREATE UNIQUE INDEX UX_Module_Code
        ON dbo.Module (Code)
        WHERE DeletedDate IS NULL;
END
GO

-- -----------------------------------------------------------------------------
-- Menu
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'Menu'
)
BEGIN
    CREATE TABLE dbo.Menu (
        Id           INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Menu PRIMARY KEY,
        ModuleId     INT            NOT NULL
            CONSTRAINT FK_Menu_Module REFERENCES dbo.Module (Id),
        ParentMenuId INT            NULL
            CONSTRAINT FK_Menu_ParentMenu REFERENCES dbo.Menu (Id),
        Name         NVARCHAR(200)  NOT NULL,
        Icon         NVARCHAR(100)  NULL,
        Url          NVARCHAR(500)  NULL,
        DisplayOrder INT            NOT NULL CONSTRAINT DF_Menu_DisplayOrder DEFAULT 0,
        IsActive     BIT            NOT NULL CONSTRAINT DF_Menu_IsActive DEFAULT 1,
        CreatedBy    NVARCHAR(256)  NULL,
        CreatedDate  DATETIME2      NOT NULL CONSTRAINT DF_Menu_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy    NVARCHAR(256)  NULL,
        UpdatedDate  DATETIME2      NULL,
        DeletedBy    NVARCHAR(256)  NULL,
        DeletedDate  DATETIME2      NULL
    );
END
GO

-- -----------------------------------------------------------------------------
-- Workstation
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'Workstation'
)
BEGIN
    CREATE TABLE dbo.Workstation (
        Id           INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Workstation PRIMARY KEY,
        CompanyId    INT            NOT NULL
            CONSTRAINT FK_Workstation_Company REFERENCES dbo.Company (Id),
        Name         NVARCHAR(200)  NOT NULL,
        MacAddress   NVARCHAR(50)   NULL,
        IpAddress    NVARCHAR(50)   NULL,
        IsActive     BIT            NOT NULL CONSTRAINT DF_Workstation_IsActive DEFAULT 1,
        CreatedBy    NVARCHAR(256)  NULL,
        CreatedDate  DATETIME2      NOT NULL CONSTRAINT DF_Workstation_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy    NVARCHAR(256)  NULL,
        UpdatedDate  DATETIME2      NULL,
        DeletedBy    NVARCHAR(256)  NULL,
        DeletedDate  DATETIME2      NULL
    );
END
GO

-- -----------------------------------------------------------------------------
-- PermissionType
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'PermissionType'
)
BEGIN
    CREATE TABLE dbo.PermissionType (
        Id          INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PermissionType PRIMARY KEY,
        Name        NVARCHAR(100)  NOT NULL,
        Code        NVARCHAR(50)   NOT NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_PermissionType_IsActive DEFAULT 1,
        CreatedBy   NVARCHAR(256)  NULL,
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_PermissionType_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy   NVARCHAR(256)  NULL,
        UpdatedDate DATETIME2      NULL,
        DeletedBy   NVARCHAR(256)  NULL,
        DeletedDate DATETIME2      NULL
    );

    CREATE UNIQUE INDEX UX_PermissionType_Code
        ON dbo.PermissionType (Code)
        WHERE DeletedDate IS NULL;
END
GO

-- -----------------------------------------------------------------------------
-- Role  (application-level role; complements ASP.NET Identity roles)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'Role'
)
BEGIN
    CREATE TABLE dbo.Role (
        Id          INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Role PRIMARY KEY,
        Name        NVARCHAR(200)  NOT NULL,
        Code        NVARCHAR(100)  NOT NULL,
        Description NVARCHAR(500)  NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_Role_IsActive DEFAULT 1,
        CreatedBy   NVARCHAR(256)  NULL,
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_Role_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy   NVARCHAR(256)  NULL,
        UpdatedDate DATETIME2      NULL,
        DeletedBy   NVARCHAR(256)  NULL,
        DeletedDate DATETIME2      NULL
    );

    CREATE UNIQUE INDEX UX_Role_Code
        ON dbo.Role (Code)
        WHERE DeletedDate IS NULL;
END
GO

-- -----------------------------------------------------------------------------
-- RoleGroup
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'RoleGroup'
)
BEGIN
    CREATE TABLE dbo.RoleGroup (
        Id          INT            IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_RoleGroup PRIMARY KEY,
        Name        NVARCHAR(200)  NOT NULL,
        Code        NVARCHAR(100)  NOT NULL,
        Description NVARCHAR(500)  NULL,
        IsActive    BIT            NOT NULL CONSTRAINT DF_RoleGroup_IsActive DEFAULT 1,
        CreatedBy   NVARCHAR(256)  NULL,
        CreatedDate DATETIME2      NOT NULL CONSTRAINT DF_RoleGroup_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy   NVARCHAR(256)  NULL,
        UpdatedDate DATETIME2      NULL,
        DeletedBy   NVARCHAR(256)  NULL,
        DeletedDate DATETIME2      NULL
    );

    CREATE UNIQUE INDEX UX_RoleGroup_Code
        ON dbo.RoleGroup (Code)
        WHERE DeletedDate IS NULL;
END
GO

-- -----------------------------------------------------------------------------
-- RoleGroupRole  (many-to-many: RoleGroup ↔ Role)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'RoleGroupRole'
)
BEGIN
    CREATE TABLE dbo.RoleGroupRole (
        Id          INT  IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_RoleGroupRole PRIMARY KEY,
        RoleGroupId INT  NOT NULL
            CONSTRAINT FK_RoleGroupRole_RoleGroup REFERENCES dbo.RoleGroup (Id),
        RoleId      INT  NOT NULL
            CONSTRAINT FK_RoleGroupRole_Role REFERENCES dbo.Role (Id),
        CreatedBy   NVARCHAR(256) NULL,
        CreatedDate DATETIME2     NOT NULL CONSTRAINT DF_RoleGroupRole_CreatedDate DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UX_RoleGroupRole_Pair
        ON dbo.RoleGroupRole (RoleGroupId, RoleId);
END
GO
