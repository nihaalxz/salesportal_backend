# Architectural Decision Record: Application Dependency on EF Core Querying

- **Status**: **ACCEPTED**
- **Date**: 2026-07-10
- **Deciders**: Principal .NET Software Architect

---

## Context

In our modernized C# backend, the `TexasPortal.Application` project defines use cases, query interfaces, and domain workflows. The query services require executing database-backed asynchronous operations (e.g. `ToListAsync()`, `CountAsync()`, `SumAsync()`).

To execute these:
1. The query handlers inject `ISalesPortalDbContext` which exposes `IQueryable<T>`.
2. The query handlers must call EF Core's async extension methods.
3. This introduces a dependency on `Microsoft.EntityFrameworkCore` package inside the `Application` project.

---

## Evaluation

- **Provider Independence**: `Microsoft.EntityFrameworkCore` contains the core abstractions and is database-provider agnostic (the concrete SQL Server provider package `Microsoft.EntityFrameworkCore.SqlServer` remains strictly inside the `Infrastructure` layer).
- **Testability**: Query handlers are highly testable. In-memory databases or real SQL Server integration testing hosts can inject test instances of `ISalesPortalDbContext` without issue.
- **Complexity**: Attempting to hide EF Core behind custom repository abstractions (e.g. `IHeadRepository.ListAsync()`) introduces heavy boilerplate, hinders flexible Linq projection mapping (`.Select()`), and leads to massive repository classes with dozens of single-use query methods.

---

## Decision

We accept the `TexasPortal.Application` project's direct dependency on the core `Microsoft.EntityFrameworkCore` package for asynchronous query terminal execution.

- **Constraint**: The Application layer must never reference specific provider libraries (like `Microsoft.EntityFrameworkCore.SqlServer`).
