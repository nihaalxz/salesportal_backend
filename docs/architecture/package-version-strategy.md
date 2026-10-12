# Package Version Strategy

This document details the NuGet package version policy for the Texas Portal modernization solution.

---

## 1. Installed Package Versions Audit

The projects in the modular monolith currently reference:
- **`Microsoft.EntityFrameworkCore.SqlServer`**: `9.0.0`
- **`Microsoft.EntityFrameworkCore.Design`**: `9.0.0`
- **`Microsoft.Extensions.Configuration`**: `9.0.0`
- **`Microsoft.Extensions.Configuration.EnvironmentVariables`**: `9.0.0`
- **`Microsoft.Extensions.Configuration.Json`**: `9.0.0`

All secondary testing utilities use matching stable versions (`coverlet.collector` v6.0.2, `Microsoft.NET.Test.Sdk` v17.12.0, `xunit` v2.9.2).

---

## 2. Package Policy & Decisions

1. **Alignment with v9.0.0 release**:
   - We will **remain on version 9.0.0** for all Microsoft core dependencies.
   - **Reason**: All package references (`SqlServer`, `Design`, `Configuration`) are perfectly aligned at `9.0.0`. Upgrading one package (like EF Core) without upgrading the extensions packages is an anti-pattern that can lead to runtime binding errors or dependency injection conflicts.
   - **No Preview Releases**: To maintain production readiness, preview/alpha packages are strictly forbidden.
2. **Upgrade Workflow**:
   - If security advisories (CVEs) require upgrading EF Core packages, all `Microsoft.EntityFrameworkCore.*` and `Microsoft.Extensions.*` packages must be bumped to the same target patch version (e.g. `9.0.2`) simultaneously.
