# Business Rules and Workflows

This document outlines the core business workflows, rules, and mathematical invariants verified from the Laravel Sales Portal codebase.

---

## 1. Booking Creation & Ledger Entry

When a salesman creates a booking (e.g. Flight, Hotel, Visa, etc.):

- **Client Resolution**:
  - If client type is `agent` and an `agent_id` is supplied, the system pad-formats the ID to match a user code: `AGT-` followed by the 3-digit padded ID (e.g., `AGT-012`). It looks up a local `Head` record with `user_id` equal to this code.
  - If client type is `customer`, it links to the selected `head_id` or associates it with the name under `care_of`.
- **Calculation of Totals**:
  - The booking's total `payable_amount` is calculated by summing individual line items (e.g. passengers' amounts, guest costs, visa fees).
  - Profit is computed as: `payable_amount - expected_supplier_cost` (only for non-inhouse bookings; in-house bookings start with 0 cost and profit, which is synced later).
- **Immediate Payment Settle**:
  - If the payment method is `wallet`, `cash`, or `online`, the booking settles immediately. The ledger credit is set to the full payable amount.
  - If the payment method is `credit`, the advance paid credit is 0, and the booking remains in `pending` payment status.
- **Ledger Posting**:
  - A debit entry equal to `payable_amount` is created in `HeadLedger`.
  - A credit entry equal to the immediate payment (if any) is created in `HeadLedger`.
  - If payment method is `wallet`, the head's `wallet` field is decremented by the payable amount.
  - The head's `balance` and `balance_amount` are updated: `balance = balance + (debit - credit)`.

---

## 2. Payment Allocation Engine

Located in `BookingPaymentService::allocateReceipt(Receipt $receipt)`:

1. **Double Allocation Protection**: Checks if a ledger entry already exists for this receipt's ID. If so, skips processing to enforce idempotency.
2. **Status Enforcement**: Receipt must be in `'success'` status.
3. **Cash-in-Hand Increment**: If `payment_mode === 'cash'`, increments the creating salesman's physical cash ledger in `cash_in_hands` table.
4. **Ledger Credit**: Creates a ledger transaction crediting the Head's ledger for the receipt amount. (Wallet deposits bypass this and instead directly increment the Head's `wallet` field).
5. **Accounts Sync**:
  - If the receipt has an `agent_id`, makes a synchronous external API call to the Accounts Portal:
    - If `service === 'wallet_deposit'`, adds to the agent's remote wallet: `updateAgentWallet($agent_id, $amount)`.
    - Otherwise, reduces the agent's outstanding remote balance: `updateAgentBalance($agent_id, -$amount)`.
  - *Note*: Laravel suppresses exceptions here, leaving the local transaction committed even if remote sync fails.
6. **Booking Allocation**:
  - Retrieves all outstanding bookings (`payment_status != 'success'`) of the matching service type, sorted by oldest first (`created_at`).
  - Iterates over bookings, allocating the remaining receipt amount:
    - Settle amount is the minimum of remaining receipt funds and the booking's remaining outstanding amount.
    - If outstanding is fully cleared (remaining <= 0.001), `payment_status` is updated to `'success'`.
    - Saves updated bookings.

---

## 3. Staff Performance & Target Tracking

- **Salesman Profit Attribution**:
  - When non-inhouse bookings are created, the total profit (`payable_amount - expected_supplier_cost`) is calculated.
  - If profit is greater than 0, the salesman's `profit_generated` is incremented, and their remaining `profit_target` is decremented by the same amount.
  - For in-house bookings, profit is initially 0. When the Operations Portal syncs the actual cost via the `/services/sync-actual-cost` endpoint, the profit is computed as `payable_amount - sales_returns - actual_cost`. The difference between the new profit and the old profit is calculated as `totalAdjustment`, and the salesman's `profit_generated` and `profit_target` are adjusted accordingly.
- **Monthly Reset & Archiving**:
  - When `resetStats` is triggered on a user, the current month's performance data is archived into the `staff_monthly_records` table, capturing `profit_generated`, `incentive`, `profit_target`, and a boolean flag `target_achieved` (`profit_generated >= monthly_target`).
  - The user's `profit_generated` is reset to 0, and `profit_target` is reset to their default `monthly_target`.
