# Flight Booking Security Review

This document audits the security controls implemented in the Flight Booking vertical slice, focusing on authorization, IDOR prevention, and error handling.

---

## 1. Authentication and Authorization Guard

All endpoints in the `BookingsController.cs` are protected by ASP.NET Core authorization policies.

### A. Endpoint-Level Controls
- **Attribute**: `[Authorize(Roles = "Salesman")]` is applied at the controller or endpoint level.
- **Verification**: The integration test `NonSalesmanRole_Returns_403` ensures that other roles (e.g. `Accountant`, `Admin`) receive a `403 Forbidden` response. `AnonymousRequest_Returns_401` ensures unauthenticated requests are blocked.

### B. Head Ownership Enforcement
A salesman can only create bookings for customers they own. 
- **Rule**: If the booking's `ClientType` is `"customer"`, the referenced `Head` entity must satisfy: `Head.CreatedBy == CurrentUserId`.
- **Implementation**: The handler executes:
  ```csharp
  if (request.ClientType == "customer" && head.CreatedBy != salesmanId)
  {
      return CreateFlightBookingResult.ValidationError(new Dictionary<string, string[]> {
          { "headId", ["This head is owned by another salesman. Booking is blocked."] }
      });
  }
  ```
- **Verification**: The test `CrossSalesmanHead_ReturnsBlocked` verifies this boundary is strictly enforced.

---

## 2. Insecure Direct Object Reference (IDOR) Prevention

An attacker could attempt to pass a target salesman's user ID in the request payload to spoof who created the booking or who receives the commission/profit.
- **Mitigation**: The API does not accept a `UserId` or `SalesmanId` parameter in the request payload (`CreateFlightBookingRequest`).
- **Enforcement**: The authenticated user ID is resolved authoritatively from the JWT claims inside the controller:
  ```csharp
  var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  ```
  And passed inside the MediatR command `CreateFlightBookingCommand(CurrentUserId, ...)`. It is impossible for a salesman to write bookings under another salesman's credentials.

---

## 3. Leak-Safe Error Handling

To prevent stack traces, SQL syntax leaks, or database structure details from being exposed to the client, the application utilizes a global exception middleware.

- **Standard**: All exceptions are caught and transformed into RFC 7807 `ProblemDetails` responses.
- **Database Exceptions**: DB constraint violations or timeout exceptions are returned as generic `500 Internal Server Error` responses, with details logged internally in application telemetry, preventing path or schema leaking.
