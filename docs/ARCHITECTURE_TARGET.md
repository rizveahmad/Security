# Architecture Target

This document describes the intended architecture and platform decisions for the Security application.

## Overall Pattern

**MVC + Razor Views + AJAX**

The web layer uses ASP.NET Core MVC with Razor Views (`.cshtml`). Interactive UX enhancements (inline validation, dynamic table updates, etc.) are handled with AJAX calls to the same MVC controllers or dedicated API endpoints. There are no Razor Pages (`Pages/` folder) in this project.

## Data Layer

### Database Engine

SQL Server is the target database engine. The connection string is configured in `appsettings.json` under the key `DefaultConnection` (see `appsettings.Development.json` or user-secrets for local overrides).

### Auto Database Creation and Scripts Runner

On application startup `DbInitializer` (called from `Program.cs`) applies any pending EF Core migrations automatically and then runs seed scripts. This means the database schema and baseline data are always up-to-date without manual migration commands during development or deployment.

No direct SQL scripts are committed to the repo; all schema changes go through EF Core migrations.

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

## External API Authentication (Token-Based)

External applications (integrations, mobile clients, scripts) authenticate via a dedicated token endpoint rather than cookie sessions. Tokens are issued as short-lived bearer tokens. API controllers decorated with `[Authorize(AuthenticationSchemes = "Bearer")]` accept only bearer tokens; standard browser controllers continue to use cookie auth.

> **Note:** The bearer/token infrastructure is planned and the architecture slot is reserved here. Exact token format (JWT vs opaque) will be decided when the external-app integration feature is implemented.

## Project Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|---------------|
| Domain | `Security.Domain` | Entities, value objects, domain events |
| Application | `Security.Application` | CQRS (MediatR), interfaces, DTOs, validation |
| Infrastructure | `Security.Infrastructure` | EF Core, Identity, services, authorization handlers |
| Web | `Security.Web` | MVC controllers, Razor views, middleware wiring |

Dependency direction: **Domain ← Application ← Infrastructure ← Web**
