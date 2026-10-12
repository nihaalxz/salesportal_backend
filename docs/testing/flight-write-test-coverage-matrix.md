# Flight Write Test Coverage Matrix

This matrix documents the integration and unit tests covering the Flight Booking vertical slice, matching required business behavior against specific test cases.

---

## 1. Test Coverage Matrix

| Scenario Category | Required Behavior | Integration Test Method (in [FlightBookingWriteTests.cs](file:///d:/freelanceprojects/TexasPortalsDotNet/texas_sales_portal_backend/texas_sales_portal_backend/tests/TexasPortal.IntegrationTests/FlightBookingWriteTests.cs)) | Status |
| :--- | :--- | :--- | :--- |
| **Authentication** | Anonymous request returns `401 Unauthorized` | `AnonymousRequest_Returns_401` | **PASS** |
| **Authorization** | Request from non-Salesman returns `403 Forbidden` | `NonSalesmanRole_Returns_403` | **PASS** |
| **Authorization** | Block booking under customer head owned by another salesman | `CrossSalesmanHead_Returns_Blocked` | **PASS** |
| **Input Validation** | Missing idempotency key header returns `400 Bad Request` | `MissingIdempotencyKey_Returns_400` | **PASS** |
| **Input Validation** | Negative amounts, missing fields, or empty passengers returns `400` | `CreateBooking_InvalidRequest_Returns_400` | **PASS** |
| **Input Validation** | Invalid/Non-existent Head ID returns validation error | `CreateBooking_HeadNotFound_ReturnsExpectedError` | **PASS** |
| **Cash Settlement** | Debit = amount, Credit = amount, increments CashInHand, increments Salesman Profit | `CreateBooking_CashMethod_EnforcesFinancialInvariants` | **PASS** |
| **Credit Settlement** | Debit = amount, Credit = 0, increments Head Balance & BalanceAmount, increments Salesman Profit | `CreateBooking_CreditMethod_IncrementsHeadBalance` | **PASS** |
| **Wallet Settlement** | Debit = amount, Credit = amount, decrements Head Wallet atomically, increments Salesman Profit | `CreateBooking_WalletMethod_DeductsHeadWallet` | **PASS** |
| **Wallet Validation** | Wallet booking with insufficient funds fails with `422 Unprocessable Entity` | `CreateBooking_InsufficientWallet_Returns_422` | **PASS** |
| **Aggregations** | Sums individual passenger amounts for total booking PayableAmount | `MultiplePassengers_AggregatesPayableAmount_And_ProfitCorrectly` | **PASS** |
| **In-House Costing** | Forces supplier cost and profit to 0 for passengers marked `isInhouse = true` | `InHousePassenger_ProfitBehaviorCorrect` | **PASS** |
| **Mixed Costing** | Calculates profit correctly on mixed bookings (some in-house, some external) | `MixedPassengerTypes_ProfitBehaviorCorrect` | **PASS** |
| **Idempotency** | Returns original cached response for exact replay | `IdempotentReplay_ReturnsOriginalResult` | **PASS** |
| **Idempotency** | Returns `409 Conflict` if same key is used with a different request payload | `IdempotencyConflict_Returns409` | **PASS** |
| **Concurrency** | Concurrent duplicate requests with same key execute once | `ConcurrentDuplicateRequest_CreatesOnce` | **PASS** |
| **Concurrency** | Concurrent wallet bookings targeting same Head do not overdraft balance | `ConcurrentWalletPayments_DoNotOverspend` | **PASS** |
| **Concurrency** | Concurrent profit updates for same salesman are applied without lost updates | `ConcurrentProfitUpdates_DoNotLoseUpdates` | **PASS** |
| **Rollback Safety** | Transaction failure rolls back all database mutations cleanly (bookings, ledger, cash, user profit) | `Failure_RollsBackStagedDatabaseState` | **PASS** |
| **Asynchronous I/O** | Gracefully aborts operations when cancellation token is canceled | `CancellationToken_PropagatesWherePractical` | **PASS** |
