# Legacy Database Baseline Strategy

This document defines the strategy for introducing Entity Framework Core migrations to manage the existing database schema without causing destructive changes or data loss.

---

## 1. Migration Management Process

### Phase 1: Baseline Creation
1. **Generating the Baseline Migration**:
   We will create an initial migration that represents the legacy schema as of today:
   ```bash
   dotnet ef migrations add InitialCreate --project src/TexasPortal.Infrastructure --startup-project src/TexasPortal.Api
   ```
2. **Marking the Baseline as Applied**:
   To prevent EF Core from trying to create tables that already exist in the target database, we execute one of two options:
   - **Option A (EF Migrations History Mock)**:
     Manually insert the record into the `__EFMigrationsHistory` table:
     ```sql
     INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
     VALUES ('20260710000000_InitialCreate', '9.0.0');
     ```
   - **Option B (Empty Up Migration)**:
     Comment out the content of the `Up` method in the generated `InitialCreate` migration file, leaving only the schema metadata in the snapshot. This allows EF Core to understand the schema state without attempting to apply it.

### Phase 2: Future Migrations
1. **Making Schema Modifications**:
   - Apply EF Core configurations or code modifications to the entities.
   - Run:
     ```bash
     dotnet ef migrations add AddNewFeature --project src/TexasPortal.Infrastructure --startup-project src/TexasPortal.Api
     ```
2. **Reviewing generated SQL**:
   Before applying any migration to staging or production, generate the raw SQL script:
   ```bash
   dotnet ef migrations script --project src/TexasPortal.Infrastructure --startup-project src/TexasPortal.Api
   ```
   Audit the generated script to verify that no destructive drop operations (`DROP TABLE`, `DROP COLUMN`) exist unless explicitly planned.

---

## 2. Safety Guidelines

> [!CAUTION]
> **No Automatic Startup Migrations**:
> Never call `dbContext.Database.Migrate()` or `Database.EnsureCreated()` on application startup. In a production environment, migrations must be run explicitly as part of the CD pipeline or manually by database administrators.
>
> **Backup and Recovery**:
> A full database backup must be taken immediately before running any schema migration scripts.
