# Repository Structure and Environment Verification

This document verifies the repository structure and target environment parameters for the TexasPortal backend.

---

## 1. Repository Structure

The actual repository layout is structured as follows:

```
D:\freelanceprojects\TexasPortalsDotNet\texas_sales_portal_backend
├── texas_sales_portal_backend.sln  <-- [Legacy] Default parent solution (contains invalid reference to deleted template project)
└── texas_sales_portal_backend      <-- [Active Workspace] Root directory for the modernized backend
    ├── TexasPortal.sln              <-- [Active] Solution file representing the Modular Monolith
    ├── docs/
    │   ├── architecture/            <-- Architectural design strategies & ADRs
    │   └── migration/               <-- Migration assessment & debt analysis documents
    ├── src/
    │   ├── TexasPortal.Api          <-- HTTP Web API Host, endpoints, controllers, & middlewares
    │   ├── TexasPortal.Application  <-- Use cases, command/query handlers, business boundaries
    │   ├── TexasPortal.Domain       <-- Entities, value objects, exceptions, and core domain services
    │   ├── TexasPortal.Infrastructure <-- Data context (EF Core), external API clients, integrations
    │   └── TexasPortal.Contracts    <-- Data transfer objects (DTOs) and request/response contracts
    └── tests/
        ├── TexasPortal.UnitTests    <-- Core logic, entity calculations, and handler tests
        ├── TexasPortal.IntegrationTests <-- EF Core mapping, query verification, and API behaviors
        └── TexasPortal.ArchitectureTests <-- Architecture rule verification (NetArchTest/reflection)
```

---

## 2. Environment Verification

- **Installed SDK Versions**:
  - `9.0.102`
  - `9.0.200`
  - `9.0.308` (Selected by the CLI builder as the highest available SDK toolset)
- **Selected TargetFramework**: `net9.0`
  - *Reason for Selection*: Matches the project standard (.NET 9) which provides modern performance optimizations, native OpenAPI bindings, and compiled queries.
- **ASP.NET Core Version**: `9.0` (Mapped by web/API projects using the `Microsoft.NET.Sdk.Web` SDK).
- **C# Language Version**: `C# 13` (Compiler default for the `.NET 9` target).
