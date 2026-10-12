# Flight Financial Input Boundary

This document outlines the security architecture and validation boundaries between client-submitted parameters and server-derived values for the Flight Booking vertical slice.

---

## 1. Input Mapping & Classification

To prevent security vulnerabilities such as **Overposting (Mass Assignment)** and data tampering, the API strictly segregates parameters into three classes:

### A. Client-Supplied Business Inputs (Allowed)
These fields are expected from the frontend UI and are bound directly via `CreateFlightBookingRequest`:
- `clientType` (Must be either `"customer"` or `"agent"`)
- `headId` (Required if clientType is customer; validated against Head table)
- `agent` / `agentId` (Required if clientType is agent)
- `careOf` (Optional)
- `fromLocation` / `toLocation` (Flight origin and destination strings)
- `paymentMethod` (Must be `"cash"`, `"wallet"`, `"credit"`, or `"online"`)
- `bankId` (Required if paymentMethod is `"online"`)
- `bankName` / `paymentProof` (Optional online transaction parameters)
- `passengers` (Array of passenger rows containing):
  - `prefix`
  - `passengerName`
  - `amount` (Validated $> 0.000\text{ BHD}$)
  - `expectedSupplierCost` (Must be $\ge 0.000\text{ BHD}$; ignored/overridden to $0.000\text{ BHD}$ if `isInhouse` is true)
  - `isInhouse` (Boolean)
  - `supplierId` (Optional; required if `isInhouse` is false)

### B. Server-Derived Financial Values (Enforced)
Clients **cannot** submit these values. The handler calculates them using secure server-side logic:
- **`PayableAmount`**: Computed as the sum of passenger `Amount` fields.
- **`Passenger.Profit`**: Computed as `Passenger.Amount - Passenger.ExpectedSupplierCost` if `isInhouse = false`. If `isInhouse = true`, profit is strictly $0.000\text{ BHD}$.
- **`Booking.Profit`**: The sum of all calculated passenger profits.
- **`Ledger.Debit` / `Ledger.Credit`**: Created based on the payment method and the total payable amount.
- **`SalesmanId` / `UserId`**: Derived directly from the secure `HttpContext.User` claims (access tokens), preventing identity impersonation (IDOR).
- **`IdempotencyRecord` status/payload**: Controlled by the server transaction flow.

### C. Forbidden / Injected Inputs
Any attempt to submit properties like `Profit`, `PayableAmount`, `UserId`, `CreatedAt`, `UpdatedAt`, or ledger transaction records inside the JSON request body will be ignored. The ASP.NET Core model binder deserializes into the record `CreateFlightBookingRequest`, which does not expose these properties.

---

## 2. Mass Assignment (Overposting) Protection

1. **Record-Based DTOs**: The API uses C# `record` types with specific parameters for binding. Unlike classes with open properties, records do not allow binding of undeclared parameters.
2. **Authoritative Calculation**: No financial values are read from client-controlled parameters. The database mutations are entirely calculated inside the transactional block of the MediatR handler.
3. **Audit Trail**: Any change to balances (credit/debit/wallet) creates a matching ledger entry linked to the created booking ID, ensuring a double-entry ledger guarantee.
