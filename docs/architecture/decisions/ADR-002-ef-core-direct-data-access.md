# ADR-002: EF Core Direct Data Access and Projection Policies

## Status
Accepted

## Context
Generic Repository patterns or custom Unit of Work wrappers around Entity Framework Core DbContext add unnecessary layers of abstraction, restrict access to full LINQ features, and complicate complex projections.

## Decision
Use EF Core `DbContext` directly inside application use cases for data access.
- Projections (`.Select()`) are preferred over eager loading (`.Include()`) for read-only API contracts.
- Change tracking will be disabled (`AsNoTracking()`) by default for all read-only operations.

## Alternatives Considered
- **Repository Pattern**: Rejected as DbContext already implements the Repository and Unit of Work patterns natively.

## Consequences
- **Positive**: Native LINQ capabilities, cleaner code, direct optimization of generated SQL.
- **Negative**: Domain layer has direct dependencies on EF Core types for queries.

## Risks
- Developers bypassing boundary models or using in-memory filtering (Mitigated by explicit team conventions and query translation analyzer rules).

## Revisit Conditions
None.
