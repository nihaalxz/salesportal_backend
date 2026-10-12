# Database Transaction Analysis

This document evaluates the transaction boundary behaviors in the Laravel codebase, specifically examining external HTTP API calls inside database transactions, and sets consistency standards for the C# implementation.

---

## 1. Inventory of External API Calls inside Transactions

Every booking service and payment service in the Laravel Sales Portal interacts with the external Accounts Portal API using synchronous HTTP requests inside database transaction scopes.

| Operation / Source | Method Involved | Transaction Starts Before HTTP Call? | HTTP Call Inside Transaction? | Exception Suppressed? | Local Transaction Commits After Remote Failure? | Remote Call Can Succeed Before Local Rollback? | Financial Impact | Recommended .NET Consistency Model |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Receipt Verification**<br>`BookingPaymentService` | `allocateReceipt`<br>`reverseReceipt` | **Yes** (`DB::transaction`) | **Yes** | **Yes** (try/catch `\Exception`) | **Yes** | **Yes** | **High Risk**: Receipt marked success locally but Agent balance unchanged remote. | **Transactional Outbox**: Commit locally first, execute HTTP from queue. |
| **Flight Booking**<br>`FlightBookingService` | `createBooking`<br>`processDeferredCreditLedger` | **Yes** (`DB::transaction`) | **Yes** | **No** (propagates) | **No** (rolls back) | **Yes** (if DB commits fail after HTTP succeeds) | **High Risk**: Agent debited on remote portal but no booking created locally. | **Transactional Outbox**: Commit locally first, execute HTTP from queue. |
| **Hotel Booking**<br>`HotelBookingService` | `createBooking`<br>`processDeferredCreditLedger` | **Yes** (`DB::transaction`) | **Yes** | **No** (propagates) | **No** (rolls back) | **Yes** | **High Risk**: Agent debited on remote portal but no booking created locally. | **Transactional Outbox**: Commit locally first, execute HTTP from queue. |
| **Transportation Booking**<br>`TransportationService` | `createBooking`<br>`processDeferredCreditLedger` | **Yes** (`DB::transaction`) | **Yes** | **No** (propagates) | **No** (rolls back) | **Yes** | **High Risk**: Agent debited on remote portal but no booking created locally. | **Transactional Outbox**: Commit locally first, execute HTTP from queue. |
| **Vfs Appointment Booking**<br>`VfsAppointmentService` | `createBooking`<br>`processDeferredCreditLedger` | **Yes** (`DB::transaction`) | **Yes** | **No** (propagates) | **No** (rolls back) | **Yes** | **High Risk**: Agent debited on remote portal but no booking created locally. | **Transactional Outbox**: Commit locally first, execute HTTP from queue. |
| **Holiday Package Booking**<br>`HolidayPackageService` | `createBooking`<br>`processDeferredCreditLedger` | **Yes** (`DB::transaction`) | **Yes** | **No** (propagates) | **No** (rolls back) | **Yes** | **High Risk**: Agent debited on remote portal but no booking created locally. | **Transactional Outbox**: Commit locally first, execute HTTP from queue. |

---

## 2. Invariants & Recommendations

> [!WARNING]
> Synchronous HTTP execution inside a database transaction violates the principle of transaction isolation and risks connection pool starvation.
>
> **Consistency Strategy for .NET**:
> 1. All mutations modifying Agent balances or wallets on the Accounts Portal must be processed asynchronously using a **Transactional Outbox**.
> 2. The local transaction will only write the local entities (bookings, ledger entries) and insert a pending task into the `OutboxMessages` table.
> 3. Once committed, a background worker will pick up the task and execute the HTTP request, marking it successful or retrying with exponential backoff.
