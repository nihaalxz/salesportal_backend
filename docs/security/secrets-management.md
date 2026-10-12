# Secrets Management

This document defines security standards for database connection strings and API credentials in development, staging, and production environments.

---

## 1. Zero Committed Credentials Policy

> [!CAUTION]
> **Never Commit Connection Strings**:
> Hardcoding database user IDs and passwords in `appsettings.json` or `appsettings.Development.json` is a critical security vulnerability. 
> All committed configuration files must use placeholder values.

---

## 2. Local Development Configuration

### dotnet user-secrets (Recommended)
ASP.NET Core automatically loads configuration from user secrets during development.
1. **Initialize User Secrets** (run in the project root containing `.sln` or inside the `src/TexasPortal.Api` directory):
   ```bash
   dotnet user-secrets init --project src/TexasPortal.Api
   ```
2. **Set the Local Connection String**:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;" --project src/TexasPortal.Api
   ```

### Environment Variables
Alternatively, configure the connection string as a system environment variable:
- **Variable Name**: `ConnectionStrings__DefaultConnection` (or `TEXAS_PORTAL_CONNECTION_STRING` for integration test executions).
- **Value**: Mapped database connection string.
