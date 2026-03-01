# Security Platform

A .NET 10 MVC web application that provides centralised **user management, role-based access control, dynamic permissions, multi-tenant company isolation, and audit logging** — the foundation layer for building secure, multi-tenant business applications.

---

## Purpose

The Security platform manages:

- **Identity & Authentication** — cookie-based login (username or email), ASP.NET Core Identity
- **Roles & Role Groups** — hierarchical role assignment per tenant
- **Dynamic Permissions** — per-module, per-menu, per-workstation permission types; resolved at runtime and cached
- **Multi-Tenancy** — each Company is an isolated tenant; global EF Core query filters enforce row-level isolation
- **User Management** — create/edit/import/export users with active-status filtering
- **Audit Logging** — all significant actions are recorded
- **JWT Token API** — `POST /api/auth/token` issues a short-lived bearer token for external integrations
- **SuperAdmin** — a built-in bootstrapped administrator that bypasses all dynamic permission checks

---

## Architecture

```
Security.Domain          ← entities, value objects (AppRole, AppModule, RoleGroup, Workstation, …)
Security.Application     ← CQRS (MediatR), interfaces, DTOs, validation
Security.Infrastructure  ← EF Core, Identity, authorization handlers, caching, services
Security.Web             ← ASP.NET Core MVC controllers + Razor views (Areas/Admin)
```

Dependency direction: **Domain ← Application ← Infrastructure ← Web**

All admin UI lives under `Areas/Admin`. There are no Razor Pages (`Pages/` folder) in this project.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, Developer, or Docker) reachable at the `DefaultConnection` string

### Run locally

```bash
# 1. Clone and build
dotnet build Security.slnx

# 2. Set the connection string (user-secrets recommended)
cd src/Security.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\\mssqllocaldb;Database=SecurityDb;Trusted_Connection=True;TrustServerCertificate=True"

# 3. Set the SuperAdmin seed password
dotnet user-secrets set "Seed:SuperAdminPassword" "YourStrongPassword123!"

# 4. Run — the app bootstraps the database automatically
dotnet run
```

No `dotnet ef database update` or manual SQL execution is required.

After startup, log in at `https://localhost:<port>` with:

- **Email:** `superadmin@security.local`
- **Password:** the value you set in `Seed:SuperAdminPassword`

> **Never commit a real password.** The `Seed:SuperAdminPassword` key in `appsettings.json` is intentionally empty; supply the value via user-secrets, an environment variable, or a secrets manager.

See **[docs/DEV_SETUP.md](docs/DEV_SETUP.md)** for full setup instructions.

---

## Automatic Database Bootstrap

On every startup the app runs three steps (before the first request is served):

| Step | Component | What it does |
|------|-----------|--------------|
| 1 | `DatabaseBootstrapper` | Connects to SQL Server `master` and issues `CREATE DATABASE` if the target DB is missing |
| 2 | `SqlScriptRunner` | Runs each `scripts/NNNN_*.sql` file in lexical order; skips scripts already recorded in `dbo.ScriptExecutionHistory` |
| 3 | `DbInitializer` | Seeds Identity roles (`SuperAdmin`, `Admin`, `User`) and the SuperAdmin user (idempotent — skips if already present) |

### Adding a schema change

Place a new file `scripts/NNNN_description.sql` in the repo-root `scripts/` folder.  
The `CopySqlScripts` build target in `Security.Web.csproj` copies it into the build-output directory automatically — no extra steps needed.  
Scripts must be idempotent (wrap DDL in `IF NOT EXISTS` guards).

---

## Configuration Reference

| Key | Required | Purpose |
|-----|----------|---------|
| `ConnectionStrings:DefaultConnection` | **Yes** | SQL Server connection string |
| `Seed:SuperAdminPassword` | Recommended | Password for the bootstrapped SuperAdmin account |
| `Jwt:Key` | Recommended | Secret key for signing JWT tokens (min 32 chars) |
| `Jwt:Issuer` / `Jwt:Audience` | No | JWT claims (default: `SecurityApp` / `SecurityApi`) |
| `Jwt:ExpiresInMinutes` | No | Token lifetime in minutes (default: `60`) |
| `ScriptRunner:ScriptFolder` | No | Override the default script folder (defaults to `scripts/` next to the executable) |

---

## Multi-Tenancy

The `Company` entity is the tenant root. Every resource that requires isolation (users, roles, role-groups, modules, permissions, workstations) is scoped to a `CompanyId`. EF Core global query filters on `AppModule`, `AppRole`, `RoleGroup`, and `Workstation` automatically restrict queries to the active tenant.

A `null` tenant context grants SuperAdmin-level bypass (no filter applied).

> **Status:** The tenant isolation infrastructure is implemented. A full self-service company-onboarding workflow is on the roadmap.

---

## Dynamic Permissions

The `DynamicPermissionHandler` resolves the required `PermissionType` for each policy at runtime via `IPermissionService`. The permission graph (User → RoleGroup → Roles → PermissionTypes) is cached in-memory with per-tenant invalidation (`IPermissionCache`). SuperAdmin users skip the handler entirely.

---

## JWT Token API *(implemented)*

External integrations authenticate via:

```
POST /api/auth/token
Content-Type: application/json

{ "username": "user@example.com", "password": "…" }
```

Returns a short-lived JWT bearer token. API controllers decorated with `[Authorize(AuthenticationSchemes="Bearer")]` accept bearer tokens; browser controllers continue to use cookie authentication.

---

## Roadmap *(planned, not yet implemented)*

- Self-service company onboarding (tenant registration UI)
- Password reset / email confirmation flow
- Delegated admin — company admins managing their own users without SuperAdmin access
- Fine-grained per-resource row-level permissions
- Refresh token support for the JWT API

---

## Running Tests

```bash
dotnet test Security.slnx
```
