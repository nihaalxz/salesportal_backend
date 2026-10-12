# Heads, Leads, and Receipts Migration Analysis

## 1. Laravel Source Audit & Behavior

### Heads
- **Model / DB:** `heads` table. Stores `user_id` (a unique customer code, e.g., `TX123` generated via `'TX' . rand(100, 999)`), name (`first_name`, `last_name`), contact info (`mobile_no`, `email`, `address`, `nationality`), type (`customer` or `agent`), balances (`wallet`, `balance_amount`), and status (`active`, `inactive`).
- **External Agents:** Laravel `HeadController@index` auto-syncs external agents from the Accounts Portal and creates local `Head` records for them (type = `agent`, `user_id` prefixed with `AGT-` and padded agent ID, e.g., `AGT-001`).
- **Authorization:** Only the salesman who created the Head (`created_by == auth()->id()`) can view or manage it.
- **Editing:** Allows editing general details (`first_name`, `last_name`, `mobile_no`, `email`, `nationality`, `address`). Financial totals (`wallet`, `balance_amount`) are locked from direct mutation.

### Leads
- **Model / DB:** `leads` table. Fields: `name`, `country_code`, `mobile_no`, `services` (stored as JSON array), `lead_source` (CRM, WhatsApp, Facebook, etc.), `description`, `status`, `reminder_date`, `reminder_active`.
- **Behavior:** Status defaults to `Pending` on creation. General users can only update the lead name. Salesmen can update the lead name and transition the status between `Pending`, `Followed Up`, and `Completed`.
- **Deleted/Soft Delete:** Supports soft delete with `deleted_at`.

### Receipts
- **Model / DB:** `receipts` table. Reference code `receipt_id` prefixed with `INV` + 4 random digits (e.g., `INV8923`).
- **Invariants & Statuses:**
  - `wallet`: Status is immediately `success`. Deducts the head's `wallet` atomically and triggers allocation.
  - `cash`: Status is initialized to `collection` (awaiting accountant collection confirmation).
  - `online`: Status is initialized to `pending` (awaiting bank/accountant verification). Requires bank information (`bank_id`, `bank_name`).
- **Allocation Rules (`BookingPaymentService`):**
  - If a receipt targets a specific `booking_id`, the amount is allocated directly to that booking.
  - Otherwise, funds are allocated using FIFO (first-in-first-out) across all outstanding (unpaid) bookings of the selected `service` for the customer `head_id` or agent.
  - When fully allocated (outstanding booking outstanding <= remaining amount), the booking's `payment_status` is updated to `success`.
  - For cash/online payments, CashInHand for the salesman is incremented when the status transitions to `success` (verified/collected by accountant).

---

## 2. Existing .NET Backend Support & Gaps

### Heads
- **Supported:** Get all heads (`GET /api/sales/heads`), get head by ID (`GET /api/sales/heads/{id}`), get head ledger entries (`GET /api/sales/heads/{id}/ledger`), and lookup heads (`GET /api/sales/heads/lookup`).
- **Gaps:** 
  - `POST /api/sales/heads` (Create Head command)
  - `PUT /api/sales/heads/{id}` (Update Head command)

### Leads
- **Supported:** Get all leads (`GET /api/sales/leads`), get lead by ID (`GET /api/sales/leads/{id}`).
- **Gaps:**
  - `POST /api/sales/leads` (Create Lead command)
  - `PUT /api/sales/leads/{id}` (Update Lead command)
  - `PATCH /api/sales/leads/{id}/archive` (Soft-delete/archive Lead command)

### Receipts
- **Supported:** None. Receipts are entirely missing from the .NET backend.
- **Gaps:**
  - `GET /api/sales/receipts` (List Receipts query)
  - `GET /api/sales/receipts/{id}` (Get Receipt Details query)
  - `GET /api/sales/receipts/booking-options` (Fetch outstanding bookings for a head and service)
  - `POST /api/sales/receipts` (Create Receipt command - with idempotency and atomic allocation)
  - `POST /api/sales/receipts/{id}/status` (Update Receipt Status command - used to confirm collections/deposits and allocate cash/online receipts)

---

## 3. Financial Invariants

- **Authority:** Authoritative balances (`Wallet`, `BalanceAmount`) must always be derived and validated by the backend. The browser should never compute or submit post-transaction balance totals.
- **Atomic Wallet Deductions:** Direct wallet receipts must execute in an atomic transaction:
  - Verify `Wallet >= Amount`.
  - Decrement `Wallet` by `Amount`.
  - Record the receipt and immediately allocate to outstanding bookings.
- **Ledger Invariance:** Every successful receipt allocation (except `wallet_deposit`) must record a corresponding `HeadLedger` credit entry linked to the `Receipt` (`ServiceType = "receipt"`, `ServiceId = ReceiptId`).
- **Cash Management:** Cash receipt collection must increment the salesman's `CashInHand` balance on the server, but only when verified (`status = "success"`).

---

## 4. Key Risks

- **Concurrency Risks:** Multiple concurrent booking/receipt submissions using the same customer head or wallet could cause over-drafts. Optimistic updates and pessimistic locking (`ExecuteUpdateAsync` or transaction isolation) must be used.
- **Security Risks:** Head and Lead ownership validation is critical. A salesman must only be allowed to view/write resources where `CreatedBy == CurrentUserId`.
- **Performance Risks:** FIFO allocation requires reading outstanding bookings. The query must only select unpaid/partially-paid bookings using indexes on `head_id` and `payment_status` to avoid scanning entire tables.

---

## 5. Functionality NOT to Migrate
- **Outbound HTTP / External Systems:** Direct sync with external portals (Accounts Portal) will be stubbed or simulated locally via database updates since no live third-party network APIs are configured in the C# backend.
- **Laravel Session Blending:** Blade-specific view logic, partial template scripts, and custom session messages are discarded in favor of SvelteKit's clean page/component division.
