# Security Analysis

This analysis evaluates security enforcement in the Laravel codebase and details security modernization requirements for the ASP.NET Core API.

---

## 1. Authentication and Integration Keys

The Sales Portal interacts with three authentication domains:
1. **Salesman Frontend Login**: Uses Laravel Session Cookies (`web` middleware).
   - *.NET Modernization*: Replaced with JWT Bearer Token authentication.
2. **Operations Portal Calls**: Authenticated via `X-OPS-KEY` header inside `ValidateApiKey` middleware.
   - *.NET Modernization*: Policy-based authorization middleware verifying the API key header against secure configuration.
3. **Staff Synchronization**: Authenticated via `X-SYNC-KEY` header inside `StaffSyncController`.
   - *.NET Modernization*: Implemented as a specific authorization policy checking the `X-SYNC-KEY` header for staff sync endpoints.

---

## 2. Authorization and IDOR Protection

- **Laravel Weakness**:
  In many standard controllers, the system queries data by user ID (`where('user_id', $userId)`) or checks ownership via helper methods. However, API endpoints must guarantee that a salesman cannot query, edit, or delete another salesman's data.
- **.NET Enforcement**:
  - Implement a central `ICurrentUser` service:
    ```csharp
    public interface ICurrentUser
    {
        long Id { get; }
        string Email { get; }
        string Role { get; } // Salesman, Admin, etc.
    }
    ```
  - Use authorization handlers to enforce resource-based authorization.
  - Automatically filter all booking query lists by `ICurrentUser.Id` unless the current user is an Administrator or Operations service account.

---

## 3. Mass Assignment & Input Validation

- **Laravel Risk**: Eloquent's `$fillable` array guards mass assignment, but it is easy to accidentally allow fields like `balance` or `wallet` if request data is passed directly to model creation.
- **.NET Enforcement**:
  - **Zero Entity Exposure**: Never expose EF Core domain entities directly in API endpoints.
  - **Explicit DTOs**: Create dedicated strongly typed Request/Response DTO contracts with `required` properties and appropriate annotations.
  - **FluentValidation**: Implement request pipeline validation using FluentValidation to sanitize string bounds, positive decimal values, and proper format matches before use cases execute.
