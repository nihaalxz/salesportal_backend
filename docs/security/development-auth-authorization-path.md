# Development Authorization Path Audit

This document details the authentication and authorization pipeline of the Sales Read API in the development environment.

---

## 1. Pipeline Overview

When a client sends an HTTP request to the API:

```mermaid
graph TD
    Client[Browser / Client] -->|Authorization: Bearer token| AuthMiddleware[Authentication Middleware]
    AuthMiddleware -->|Resolves claims| ClaimsPrincipal[ClaimsPrincipal Built]
    ClaimsPrincipal -->|Checked against [Authorize(Roles = 'Salesman')] | AuthzMiddleware[Authorization Middleware]
    AuthzMiddleware -->|Pass / Fail| Controller[Sales Controller]
```

1. **Authentication Middleware**: Evaluates the `Authorization` header. Since `builder.Environment.IsDevelopment()` is true, the `DevMockScheme` is active and triggers the `DevMockAuthHandler`.
2. **Claims Resolution**: If the token matches one in `DevMockAuthHandler.TokenAllowlist`, a `ClaimsPrincipal` is created containing the mapped `NameIdentifier`, `Name`, and `Role` claims.
3. **Authorization Middleware**: Inspects the attributes on the target controller and action. Every Sales read controller is decorated with `[Authorize(Roles = "Salesman")]`. It checks if `User.IsInRole("Salesman")` returns true.
4. **ICurrentUser Evaluation**: Inside the controller, `ICurrentUser` (implemented by `CurrentUser`) retrieves the `NameIdentifier` claim using `HttpContextAccessor` and parses it to a `long?`.

---

## 2. Identity Mapping Matrix

| Token | Authentication Result | UserId Claim | Role Claims | Permission Claims (Frontend Only) | Authorization Policy | Expected HTTP Status | Actual HTTP Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| *Missing Token* | Fail | None | None | None | `[Authorize]` | `401 Unauthorized` | **401 Unauthorized** |
| *Unknown Token* | Fail | None | None | None | `[Authorize]` | `401 Unauthorized` | **401 Unauthorized** |
| `dev-token-salesman-a` | Success | `"1"` | `"Salesman"` | `read:dashboard`, `read:leads`, etc. | `Roles = "Salesman"` | `200 OK` | **200 OK** |
| `dev-token-salesman-b` | Success | `"2"` | `"Salesman"` | `read:dashboard`, `read:leads`, etc. | `Roles = "Salesman"` | `200 OK` | **200 OK** |
| `dev-token-no-permission` | Success | `"3"` | `"Guest"` | None | `Roles = "Salesman"` | `403 Forbidden` | **403 Forbidden** |

---

## 3. Policy Registration and Enforcement

In Program.cs:
- `builder.Services.AddAuthorization();` registers the authorization services.
- `app.UseAuthorization();` places the authorization middleware in the pipeline after `app.UseAuthentication()`.
- The controller decoration `[Authorize(Roles = "Salesman")]` ensures that any user lacking the `Salesman` role will receive a `403 Forbidden` from ASP.NET Core before any application logic or EF Core database context is invoked.
