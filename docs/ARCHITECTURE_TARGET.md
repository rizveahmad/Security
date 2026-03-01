# Architecture Target

This document describes the intended architecture and platform decisions for the Security application.

## Overall Pattern

**MVC + Razor Views + AJAX**

The web layer uses ASP.NET Core MVC with Razor Views (`.cshtml`). Interactive UX enhancements (inline validation, dynamic table updates, etc.) are handled with AJAX calls to the same MVC controllers or dedicated API endpoints. There are no Razor Pages (`Pages/` folder) in this project.

## Data Layer

### Database Engine

SQL Server is the target database engine. The connection string is configured in `appsettings.json` under the key `DefaultConnection` (see `appsettings.Development.json` or user-secrets for local overrides).

### Auto Database Creation and Scripts Runner

On application startup `DatabaseBootstrapper` connects to SQL Server `master` and creates the target database if it does not exist. `SqlScriptRunner` then executes any numbered `scripts/NNNN_*.sql` files that have not yet been recorded in `dbo.ScriptExecutionHistory`. Finally `DbInitializer` seeds Identity roles and the SuperAdmin account idempotently.

No EF Core migrations are used; all schema changes go through numbered SQL scripts in the repo-root `scripts/` folder.

## Authentication & Sessions

### Identity Auth

ASP.NET Core Identity (`Microsoft.AspNetCore.Identity`) is used for authentication. The `ApplicationUser` class (`Security.Infrastructure.Identity.ApplicationUser`) is the Identity user type.

### Cookie Session Behavior

Authentication state is stored in an encrypted cookie (ASP.NET Core cookie authentication middleware). Session timeout and sliding-expiry are configured in `Program.cs`. There is no JWT/bearer token requirement for browser-based access.

### Username OR Email Login

Users may sign in with either their **username** or their **email address**. The login logic normalises the input and queries both fields before challenging the password.

## SuperAdmin Bootstrap

A `SuperAdmin` account is seeded automatically at startup by `DbInitializer` when no users exist. The password is read from configuration key `Seed:SuperAdminPassword` (see DEV_SETUP.md for how to supply this locally). The `SuperAdmin` role bypasses all dynamic permission checks.

## Multi-Tenancy

### Company as Tenant

The `Company` entity is the tenant root. Every resource that requires isolation (users, roles, role-groups, permissions, form definitions, etc.) is scoped to a `CompanyId`. The current user's `CompanyId` is resolved by `ICurrentUserService` and injected automatically into queries and commands.

### Tenant Isolation

EF Core query filters ensure rows from other tenants are never returned. All `DbSet` queries automatically apply a `CompanyId == currentCompanyId` filter, enforced in `ApplicationDbContext.OnModelCreating`.

## Dynamic Authorization & Caching

Authorization is policy-based. `DynamicPermissionHandler` resolves the required `PermissionType` for each policy at runtime via `IPermissionService`. The permission graph (User → RoleGroup → Roles → PermissionTypes) is cached in-memory (`IMemoryCache`) and invalidated when roles or assignments change. `SuperAdmin` users skip the handler entirely.

### Cache keys

`IPermissionCache` (Application layer) / `InMemoryPermissionCache` (Infrastructure layer) keys entries as:

```
perm:{tenantId}:{userId}
```

Where `tenantId` is the active tenant identifier (resolved by `ITenantContext`) or `"null"` for SuperAdmin / no-tenant contexts. Entries expire after **15 minutes** (absolute expiry) or sooner if explicitly invalidated.

### Invalidation triggers

| Event | Invalidation scope | Method called |
|-------|--------------------|---------------|
| Role updated (`UpdateRoleCommand`) | All users in affected tenant | `InvalidateTenant(companyId)` |
| Role deleted (`DeleteRoleCommand`) | All users in affected tenant | `InvalidateTenant(companyId)` |
| RoleGroup updated (`UpdateRoleGroupCommand`) | All users in affected tenant | `InvalidateTenant(companyId)` |
| RoleGroup deleted (`DeleteRoleGroupCommand`) | All users in affected tenant | `InvalidateTenant(companyId)` |
| User role-group assignment (`AssignRoleGroupCommand`) | The specific user only | `InvalidateUser(tenantId, userId)` |

Tenant-level invalidation is implemented via a `CancellationTokenSource` per tenant: cancelling the token causes `IMemoryCache` to evict all entries registered with that token without affecting other tenants' entries.

### Cache usage

| Component | Layer | How it uses the cache |
|-----------|-------|-----------------------|
| `PermissionService.GetUserPermissionsAsync` | Infrastructure | Cache-aside: returns cached set on hit; queries DB and writes to cache on miss |
| `DynamicPermissionHandler` | Infrastructure | Calls `PermissionService.HasPermissionAsync`, which uses the cache indirectly |
| `GetUserMenuTreeQuery` | Application | Calls `IPermissionService.GetUserPermissionsAsync`; shares the same cached result as auth checks |

## External API Authentication (Token-Based)

External applications (integrations, mobile clients, scripts) authenticate via `POST /api/auth/token`, which issues a short-lived JWT bearer token. API controllers decorated with `[Authorize(AuthenticationSchemes = "Bearer")]` accept only bearer tokens; standard browser controllers continue to use cookie auth.

## Project Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|---------------|
| Domain | `Security.Domain` | Entities, value objects, domain events |
| Application | `Security.Application` | CQRS (MediatR), interfaces, DTOs, validation |
| Infrastructure | `Security.Infrastructure` | EF Core, Identity, services, authorization handlers |
| Web | `Security.Web` | MVC controllers, Razor views, middleware wiring |

Dependency direction: **Domain ← Application ← Infrastructure ← Web**
