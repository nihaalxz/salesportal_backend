# Flight Booking Acceptance Gap Audit

This document logs the requirement-by-requirement audit of the Flight Booking vertical slice, listing implementation verification and test execution evidence.

---

## 1. Acceptance Requirements Audit Log

| ID | Requirement | Status | Verification Evidence / Code Reference |
| :--- | :--- | :--- | :--- |
| **REQ-1** | Flight booking creation endpoint maps to controller / MediatR pipeline. | **PASS** | [`BookingsController.cs`](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Api/Controllers/Sales/BookingsController.cs) maps `POST /api/sales/bookings/flights`. |
| **REQ-2** | Authenticated user is used as the booking creator (`UserId`). | **PASS** | `BookingsController.cs` extracts user ID from NameIdentifier claim. IDOR is prevented. |
| **REQ-3** | Customer Head ownership is validated if ClientType is customer. | **PASS** | [`CreateFlightBookingHandler.cs`](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/src/TexasPortal.Application/Sales/Bookings/Flights/CreateFlightBooking/CreateFlightBookingHandler.cs) checks `head.CreatedBy == salesmanId`. |
| **REQ-4** | Client overposting of derived financial values is blocked. | **PASS** | Strongly-typed DTO `CreateFlightBookingRequest` lacks derived properties, which prevents mass assignment. |
| **REQ-5** | Cash payment method: Credit & Debit ledger entries equal booking cost. | **PASS** | Ledger records verified in `CreateBooking_CashMethod_EnforcesFinancialInvariants`. |
| **REQ-6** | Cash payment method: Increments current salesman's CashInHand. | **PASS** | Verified in database state check in `CreateBooking_CashMethod_EnforcesFinancialInvariants`. |
| **REQ-7** | Cash payment method: Increments current salesman's total ProfitGenerated. | **PASS** | Verified in database state check in `CreateBooking_CashMethod_EnforcesFinancialInvariants`. |
| **REQ-8** | Credit payment method: Debit equals cost, Credit equals 0. | **PASS** | Verified in ledger assertions in `CreateBooking_CreditMethod_IncrementsHeadBalance`. |
| **REQ-9** | Credit payment method: Increments Head Balance and BalanceAmount. | **PASS** | Verified in database state check in `CreateBooking_CreditMethod_IncrementsHeadBalance`. |
| **REQ-10**| Wallet payment method: Debit and Credit equal cost. | **PASS** | Verified in ledger assertions in `CreateBooking_WalletMethod_DeductsHeadWallet`. |
| **REQ-11**| Wallet payment method: Decrements Head Wallet balance atomically. | **PASS** | Verified via atomic `ExecuteUpdateAsync` updates in code and `WalletPayment_WalletCorrect` test. |
| **REQ-12**| Wallet payment method: Rejects creation if wallet balance is insufficient. | **PASS** | Returns `422 Unprocessable Entity` verified in `CreateBooking_InsufficientWallet_Returns_422`. |
| **REQ-13**| Booking PayableAmount is calculated as the sum of passenger amounts. | **PASS** | Verified in passenger iteration inside handler and `MultiplePassengers_AggregatesPayableAmount_And_ProfitCorrectly` test. |
| **REQ-14**| Passenger Profit is calculated as Amount - ExpectedSupplierCost. | **PASS** | Verified in `FlightBookingWriteTests.cs`. |
| **REQ-15**| In-house passengers override expected supplier cost and profit to 0. | **PASS** | Checked in `InHousePassenger_ProfitBehaviorCorrect` test. |
| **REQ-16**| Mixed passenger types (in-house & external) aggregate profit correctly. | **PASS** | Verified in `MixedPassengerTypes_ProfitBehaviorCorrect` test. |
| **REQ-17**| Idempotency replay returns identical response payload. | **PASS** | Checked in `IdempotentReplay_ReturnsOriginalResult` test. |
| **REQ-18**| Idempotency key reuse with different request body returns 409 Conflict. | **PASS** | Checked in `IdempotencyConflict_Returns409` test. |
| **REQ-19**| Concurrent same-key requests result in exactly one database record. | **PASS** | Checked in `ConcurrentDuplicateRequest_CreatesOnce` test. |
| **REQ-20**| Concurrent wallet bookings targeting same Head prevent overspending. | **PASS** | Checked in `ConcurrentWalletPayments_DoNotOverspend` test. |
| **REQ-21**| Concurrent profit increments for salesman prevent lost updates. | **PASS** | Checked in `ConcurrentProfitUpdates_DoNotLoseUpdates` test. |
| **REQ-22**| Transaction failures rollback all staged database mutations. | **PASS** | Verified in `Failure_RollsBackStagedDatabaseState` test. |
| **REQ-23**| Operations support CancellationToken propagation. | **PASS** | Checked in `CancellationToken_PropagatesWherePractical` test. |
| **REQ-24**| Frontend list page supports status filtering and paging. | **PASS** | Verified in [`+page.svelte`](file:///d:/freelanceprojects/TexasPortalsDotNet/salesportal_frontend/src/routes/bookings/flights/+page.svelte). |
| **REQ-25**| Frontend create form supports dynamic passenger rows and client calc. | **PASS** | Verified in [`new/+page.svelte`](file:///d:/freelanceprojects/TexasPortalsDotNet/salesportal_frontend/src/routes/bookings/flights/new/+page.svelte). |

---

## 2. Test Execution & Compilation Evidence

### A. Backend Unit & Integration Tests
All tests compile and pass successfully:
- **Command**: `dotnet test --no-build`
- **Result**:
  - Total passed: **114**
  - Total failed: **0**
  - Total skipped: **0**
  - Exit code: `0`

### B. Frontend Unit Tests
All frontend API and formatter unit tests pass:
- **Command**: `npm run test:unit`
- **Result**:
  - Total passed: **51**
  - Total failed: **0**
  - Exit code: `0`

### C. Frontend Type & Static Checks
Svelte check reports zero compile errors:
- **Command**: `npx svelte-check`
- **Result**: `svelte-check found 0 errors and 0 warnings`
