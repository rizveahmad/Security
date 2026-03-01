# Developer Setup Guide

This guide explains how to run the Security application locally on a Windows or Linux machine.

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| .NET SDK 10 | [Download](https://dotnet.microsoft.com/download) |
| SQL Server | Local SQL Server Express, Developer Edition, or Docker (`mcr.microsoft.com/mssql/server`) |
| Visual Studio 2022 (or VS Code) | Optional but recommended |

> **SQL Server is required.** The application uses EF Core with the SQL Server provider. SQLite is not supported.

---

## 1. Configure the Database Connection

The application reads the connection string from `DefaultConnection`.

### Option A — `appsettings.Development.json` (not committed to git)

Create or edit `src/Security.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SecurityDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Adjust `Server=` as needed (e.g. `Server=.\\SQLEXPRESS` for a named instance).

### Option B — .NET User Secrets (recommended, keeps secrets out of files)

```bash
cd src/Security.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=SecurityDb;Trusted_Connection=True;TrustServerCertificate=True"
```

---

## 2. Set the SuperAdmin Seed Password

The first-run database seeder creates a `SuperAdmin` account. The password is read from config key `Seed:SuperAdminPassword`.

### User Secrets (recommended)

```bash
cd src/Security.Web
dotnet user-secrets set "Seed:SuperAdminPassword" "YourStrongPassword123!"
```

### Environment Variable

```bash
# PowerShell
$env:Seed__SuperAdminPassword = "YourStrongPassword123!"

# Bash
export Seed__SuperAdminPassword="YourStrongPassword123!"
```

> **Never commit a real password to `appsettings.json`.**  
> The `Seed:SuperAdminPassword` key in `appsettings.json` is intentionally empty; supply the value via user-secrets, an environment variable, or a secrets manager.

---

## 3. Run the Application

### From the command line

```bash
cd src/Security.Web
dotnet run
```

The app starts on `https://localhost:5001` (or the port shown in the terminal). It automatically creates the database, runs numbered SQL scripts from `scripts/`, and seeds Identity roles and the SuperAdmin account on first run.

### From Visual Studio

1. Open `Security.slnx` in Visual Studio 2022.
2. Set **Security.Web** as the startup project.
3. Press **F5** (debug) or **Ctrl+F5** (without debugger).

Visual Studio will restore NuGet packages, build, and launch the browser automatically.

---

## 4. Run Tests

```bash
dotnet test Security.slnx
```

---

## 5. Common Issues

| Symptom | Solution |
|---------|---------|
| `A network-related or instance-specific error` | SQL Server is not running, or the connection string is wrong. |
| `Login failed for user` | Use Windows auth (`Trusted_Connection=True`) or add SQL login credentials. |
| `No migrations applied` | The app uses SQL scripts (not EF migrations) for schema; check app logs for `SqlScriptRunner` errors. |
| `Invalid password for SuperAdmin` | Set `Seed:SuperAdminPassword` via user-secrets or env var (see step 2). |
