# Current Implementation Audit: Other Service Booking

This document details the audit of the accidentally advanced .NET implementation of the **Other Service Booking** workflow against the original Laravel implementation.

**Audit Date**: 2026-07-11

---

## 1. Audit Matrix

| Component / Rule | Laravel Behavior | Current .NET Behavior | Equivalent? | Improved? | Missing / Gaps | Incorrect | Test Evidence | Required Action |
|---|---|---|---|---|---|---|---|---|
| **Data Schema** | `other_services` table columns include: `created_by`, `head_id`, `client_type`, `prefix`, `name`, `care_of`, `agent`, `agent_id`, `is_inhouse`, `supplier_id`, `supplier_name`, `expected_supplier_cost`, `profit`, `service_name`, `narration`, `payable_amount`, `payment_method`, `payment_status`, `processing_status`, `payment_proof`. | Maps almost identical columns via EF Core configurations to `other_services` table. | Yes | No | None | None | `DatabaseMappingTests` pass. | None |
| **Validation Rules** | Validates string lengths, presence of dependent fields (e.g. `head_id` if client is `customer`, `agent` if client is `agent`, `supplier_id` if not in-house, `bank_id` if payment is `online`), non-negative decimals for amounts. | Basic checks in Handler: `Name`, `ServiceName` not empty; `Amount`, `ExpectedSupplierCost >= 0`. | No | No | Gaps: missing conditional validations for `head_id`, `agent`, `supplier_id`, and `online` payments. | None | `NegativeAmount_Returns_400_ValidationDetails` in `OtherServiceBookingWriteTests` | Extend `CreateOtherServiceBookingHandler` with robust conditional validation (Phase 5). |
| **Authorization** | `auth()->id()` limits bookings to the logged-in salesman. | Checks `ICurrentUser` claims in `BookingsController` using `[Authorize(Roles = "Salesman")]`. | Yes | Yes (strongly typed claims) | None | None | `NonSalesmanRole_Returns_403` | None |
| **Direct Head Access / Ownership** | Web show/list endpoints restrict query to `created_by == auth()->id()`. | Checked. Scopes list and detail queries by current salesman ID. | Yes | Yes | None | None | `SalesmanA_And_SalesmanB_Get_Distinct_Empty_Dashboards` | None |
| **Deducting Wallet** | Deducts from `head->wallet` immediately on ledger creation. Checks sufficiency for status. | Atomically updates wallet balance via EF Core `ExecuteUpdateAsync` to prevent concurrency race conditions. | Yes | Yes (Concurrency-safe atomic update) | None | None | `CreateBooking_WalletMethod_DeductsWalletCorrectly` | None |
| **Credit Balance** | Adds to `head->balance` / `head->balance_amount`. | Atomically increments balance via EF Core `ExecuteUpdateAsync` if `payableAmount > advancePaid`. | Yes | Yes (concurrency-safe) | None | None | `CreateBooking_CreditMethod_UpdatesCustomerBalance` | None |
| **Cash In Hand** | Increments salesman's cash-in-hand balance. | Atomically updates or inserts cash record for current salesman. | Yes | Yes | None | None | `CreateBooking_CashMethod_EnforcesFinancialInvariants` | None |
| **Salesman Profit** | Increments `User->profit_generated`. | Atomically increments `User->ProfitGenerated` via EF Core. | Yes | Yes | None | None | `CreateBooking_CashMethod_EnforcesFinancialInvariants` | None |
| **Head Ledger Entry** | Creates a `HeadLedger` debit/credit entry. | Inserts `HeadLedger` record in same transaction. | Yes | Yes | None | None | Verified in test database checks. | None |
| **Idempotency** | In Laravel, no formal idempotency keys. | Unique database key constraint in `idempotency_records` table, replaying cached response payload on match. | No | Yes (durable, DB-backed idempotency) | None | None | `CreateBooking_IdempotencyReplay_Returns_CachedResponse` | None |
| **Atomicity** | DB transaction block. | Explicit transaction scoped with EF Core retry strategy via `TransactionalExecutor`. | Yes | Yes | None | None | `OtherServiceBookingWriteTests` verify rollbacks on failure. | None |
| **Details Endpoint** | Not audited. | Missing `GET /api/sales/bookings/other/{bookingId}` endpoint in backend controller/service. | No | No | Missing details endpoint. | None | None | Implement Details endpoint (Phase 10). |

---

## 2. Test Evidence

The backend test suite has 10 dedicated HTTP integration tests in `OtherServiceBookingWriteTests.cs` validating:
- Auth bounds (401, 403)
- Validations (negative amounts)
- Creation and ledger side-effects for `cash`, `credit`, and `wallet` methods
- Insufficient wallet limits (422)
- Idempotency key play/replay and different payload conflicts (409)

All tests compile and pass.

---

## 3. Required Actions

1. **Define the authoritative contract** and payment behavior documents (Phase 2 & 3).
2. **Harden validation logic** in `CreateOtherServiceBookingHandler` to cover conditional rules (Phase 5).
3. **Implement Details read API endpoint**: `GET /api/sales/bookings/other/{bookingId}` (Phase 10).
4. **Regenerate OpenAPI types** and build the frontend vertical slice (list, form, details, and E2E tests).
