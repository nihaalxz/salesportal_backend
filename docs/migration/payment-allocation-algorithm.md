# Payment Allocation Algorithm Specification

This document provides a detailed specification of the payment allocation algorithm implemented in `app/Services/BookingPaymentService.php`.

---

## 1. Core Entry Points and Interface

### `allocateReceipt(Receipt $receipt)`

#### Pre-Conditions
1. **Idempotency Guard**: Queries `HeadLedger` to check if a ledger record exists with `service_type = 'receipt'` and `service_id = $receipt->id`. If true, exits immediately to prevent double-posting.
2. **Status Guard**: The receipt status must be `'success'`. If not, exits.

#### Execution Sequence (within `DB::transaction`)
1. **Physical Cash Update**:
   - If `receipt->payment_mode === 'cash'`, executes `CashInHand::updateOrCreate()` incrementing the `amount` of the user who created the receipt.
2. **Local Ledger Crediting**:
   - If `receipt->service !== 'wallet_deposit'`:
     - Calls `HeadLedgerService::recordTransaction` to record a ledger credit matching the receipt amount for the specified `head_id`.
   - If `receipt->service === 'wallet_deposit'`:
     - Increments the Head's `wallet` field directly by `$receipt->amount`.
3. **External Sync**:
   - If `receipt->agent_id` is present:
     - If `service === 'wallet_deposit'`: Calls `AccountsApiService::updateAgentWallet(agent_id, amount)`.
     - Otherwise: Calls `AccountsApiService::updateAgentBalance(agent_id, -amount)`.
     - *Note*: Any exceptions thrown during this integration HTTP call are caught and logged, allowing the local transaction to commit successfully regardless.
4. **Booking Allocation**:
   - If `receipt->service !== 'wallet_deposit'`:
     - Calls `applyAllocation($receipt)`.

---

## 2. Booking Allocation Logic (`applyAllocation`)

### Priority 1: Direct Linking
If `$receipt->booking_id` is specified, the system looks up the individual booking matching the service type and ID.
- If found, executes `processSingleAllocation(booking, service, receipt->amount)`.
- Returns immediately (no search of other bookings occurs).

### Priority 2: FIFO Ledger Clearing
If no direct booking is linked:
1. Queries the database for outstanding bookings of the matching service type where `payment_status != 'success'` (and `payment_status != 'collected'`).
2. **Filtering Rules**:
   - If `head_id` is provided:
     - For `holidays` or `vfs`: Filters by `head_id = $head->id`.
     - For other services: Filters by `head_id = $head->id` OR `care_of = $head->name` (text name match).
   - If `agent_id` is provided:
     - For `visas`: Filters applicants where parent `application->agent_id = agent_id`.
     - For other services: Filters by `agent_id = agent_id`.
3. **Sorting**: Orders bookings oldest first (`oldest()` -> ascending order of creation date/id).
4. **Allocation Loop**:
   - Iterates through the list of outstanding bookings.
   - Calculates the booking's outstanding balance:
     - Outstanding = `payable_amount` (only if status is not success/collected).
   - Allocates the amount: `pay = min(remaining_receipt_amount, outstanding)`.
   - If `pay > 0.001`:
     - If `pay >= outstanding - 0.001` (within rounding threshold):
       - Sets `booking->payment_status = 'success'`.
     - Saves booking state: `$booking->save()`.
     - Decrements remaining receipt funds: `remaining = remaining - pay`.
   - Terminates loop if `remaining <= 0.001` BHD.

---

## 3. Reversal Logic (`reverseReceipt`)

1. **Revert Cash**: Decrements `cash_in_hands.amount` by receipt amount.
2. **Revert Ledger/Wallet**:
   - If `service !== 'wallet_deposit'`: Increments `heads.balance` (legacy column).
   - If `service === 'wallet_deposit'`: Decrements `heads.wallet`.
3. **Revert External Sync**:
   - If `agent_id` is present:
     - If `wallet_deposit`: `updateAgentWallet(agent_id, -amount)`.
     - Otherwise: `updateAgentBalance(agent_id, amount)`.
     - (HTTP exceptions caught and suppressed).
4. **Revert Booking Status**:
   - If `booking_id` is linked, sets the booking's `payment_status` back to `'pending'`.
