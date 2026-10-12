# Phase 8 Implementation Inventory

This document lists all files created or modified during the initial Phase 8 implementation.

---

| Path | Layer | Purpose | Public Types | Dependencies | DB? | HTTP? | Auth? |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `src/TexasPortal.Domain/Common/IUserOwned.cs` | Domain | Ownership contract | `IUserOwned` | None | No | No | No |
| `src/TexasPortal.Domain/Common/ICreatorOwned.cs` | Domain | Ownership contract | `ICreatorOwned` | None | No | No | No |
| `src/TexasPortal.Application/Common/Interfaces/ICurrentUser.cs` | Application | Current user identity abstraction | `ICurrentUser` | None | No | No | Yes |
| `src/TexasPortal.Infrastructure/Services/CurrentUser.cs` | Infrastructure | Resolves ICurrentUser via HttpContext | `CurrentUser` | `IHttpContextAccessor` | No | Yes | Yes |
| `src/TexasPortal.Application/Common/Extensions/QueryableExtensions.cs` | Application | Ownership filter extensions | `QueryableExtensions` | `IQueryable` | Yes | No | No |
| `src/TexasPortal.Contracts/Common/PagedResponse.cs` | Contracts | Pagination payload wrapper | `PagedResponse<T>` | None | No | Yes | No |
| `src/TexasPortal.Application/Sales/Queries/ISalesReadService.cs` | Application | Service interface (to be refactored) | `ISalesReadService` | None | Yes | No | No |
| `src/TexasPortal.Infrastructure/Services/SalesReadService.cs` | Infrastructure | Service implementation (to be refactored) | `SalesReadService` | `ISalesPortalDbContext` | Yes | No | No |
| `src/TexasPortal.Api/Controllers/Sales/DashboardController.cs` | Api | Dashboard summary endpoint | `DashboardController` | `ISalesReadService`, `ICurrentUser` | Yes | Yes | Yes |
| `src/TexasPortal.Api/Controllers/Sales/HeadsController.cs` | Api | Customer management endpoints | `HeadsController` | `ISalesReadService`, `ICurrentUser` | Yes | Yes | Yes |
| `src/TexasPortal.Api/Controllers/Sales/LeadsController.cs` | Api | Lead management endpoints | `LeadsController` | `ISalesReadService`, `ICurrentUser` | Yes | Yes | Yes |
| `src/TexasPortal.Api/Controllers/Sales/BookingsController.cs` | Api | Mapped booking lists endpoints | `BookingsController` | `ISalesReadService`, `ICurrentUser` | Yes | Yes | Yes |
| `src/TexasPortal.Api/Controllers/Sales/CashManagementController.cs` | Api | Cash-in-hand endpoints | `CashManagementController` | `ISalesReadService`, `ICurrentUser` | Yes | Yes | Yes |
| `src/TexasPortal.Api/Controllers/Sales/PerformanceController.cs` | Api | Monthly records endpoints | `PerformanceController` | `ISalesReadService`, `ICurrentUser` | Yes | Yes | Yes |
| `tests/TexasPortal.IntegrationTests/SalesReadSecurityTests.cs` | Test | Security isolation tests | `SalesReadSecurityTests` | `SalesPortalDbContext` | Yes | No | Yes |
