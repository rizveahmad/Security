-- =============================================================================
-- 0001_Identity.sql
-- Creates the standard ASP.NET Core Identity tables required by
-- IdentityDbContext<ApplicationUser> plus the custom audit columns on AspNetUsers.
-- This script is idempotent: every CREATE is wrapped in an existence check.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- AspNetUsers
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetUsers'
)
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id                   NVARCHAR(450)  NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName             NVARCHAR(256)  NULL,
        NormalizedUserName   NVARCHAR(256)  NULL,
        Email                NVARCHAR(256)  NULL,
        NormalizedEmail      NVARCHAR(256)  NULL,
        EmailConfirmed       BIT            NOT NULL,
        PasswordHash         NVARCHAR(MAX)  NULL,
        SecurityStamp        NVARCHAR(MAX)  NULL,
        ConcurrencyStamp     NVARCHAR(MAX)  NULL,
        PhoneNumber          NVARCHAR(MAX)  NULL,
        PhoneNumberConfirmed BIT            NOT NULL,
        TwoFactorEnabled     BIT            NOT NULL,
        LockoutEnd           DATETIMEOFFSET NULL,
        LockoutEnabled       BIT            NOT NULL,
        AccessFailedCount    INT            NOT NULL,
        -- ApplicationUser custom columns
        FirstName            NVARCHAR(MAX)  NULL,
        LastName             NVARCHAR(MAX)  NULL,
        CreatedBy            NVARCHAR(MAX)  NULL,
        CreatedDate          DATETIME2      NOT NULL CONSTRAINT DF_AspNetUsers_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedBy            NVARCHAR(MAX)  NULL,
        UpdatedDate          DATETIME2      NULL,
        IsActive             BIT            NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT 1
    );

    CREATE UNIQUE INDEX UserNameIndex
        ON dbo.AspNetUsers (NormalizedUserName)
        WHERE NormalizedUserName IS NOT NULL;

    CREATE INDEX EmailIndex
        ON dbo.AspNetUsers (NormalizedEmail);
END
GO

-- -----------------------------------------------------------------------------
-- AspNetRoles
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetRoles'
)
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id               NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name             NVARCHAR(256) NULL,
        NormalizedName   NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );

    CREATE UNIQUE INDEX RoleNameIndex
        ON dbo.AspNetRoles (NormalizedName)
        WHERE NormalizedName IS NOT NULL;
END
GO

-- -----------------------------------------------------------------------------
-- AspNetUserRoles
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetUserRoles'
)
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetUserRoles_RoleId
        ON dbo.AspNetUserRoles (RoleId);
END
GO

-- -----------------------------------------------------------------------------
-- AspNetUserClaims
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetUserClaims'
)
BEGIN
    CREATE TABLE dbo.AspNetUserClaims (
        Id         INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId     NVARCHAR(450) NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetUserClaims_UserId
        ON dbo.AspNetUserClaims (UserId);
END
GO

-- -----------------------------------------------------------------------------
-- AspNetRoleClaims
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetRoleClaims'
)
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims (
        Id         INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId     NVARCHAR(450) NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_Roles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetRoleClaims_RoleId
        ON dbo.AspNetRoleClaims (RoleId);
END
GO

-- -----------------------------------------------------------------------------
-- AspNetUserLogins
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetUserLogins'
)
BEGIN
    CREATE TABLE dbo.AspNetUserLogins (
        LoginProvider       NVARCHAR(128) NOT NULL,
        ProviderKey         NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId              NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_AspNetUserLogins_UserId
        ON dbo.AspNetUserLogins (UserId);
END
GO

-- -----------------------------------------------------------------------------
-- AspNetUserTokens
-- -----------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'dbo' AND t.name = 'AspNetUserTokens'
)
BEGIN
    CREATE TABLE dbo.AspNetUserTokens (
        UserId        NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(128) NOT NULL,
        Name          NVARCHAR(128) NOT NULL,
        Value         NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO
