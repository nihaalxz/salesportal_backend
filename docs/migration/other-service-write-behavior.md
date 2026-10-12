# Other Service Booking Write Behavior

This document details the behavioral analysis of the legacy Laravel Other Service Booking creation workflow.

---

## 1. Request Validation & Parameters

- **Laravel Route**: `POST /bookings/other`
- **Laravel Controller Method**: `OtherServiceController@store`
- **Request Fields**:
  - `prefix` (string, optional)
  - `client_type` (string: `customer`, `agent`)
  - `head_id` (integer, optional)
  - `name` (string, required)
  - `agent` (string, optional)
  - `agent_id` (integer, optional)
  - `is_inhouse` (boolean: `1` or `0`)
  - `supplier_id` (integer, optional)
  - `expected_supplier_cost` (numeric, optional)
  - `amount` (numeric, required)
  - `service_name` (string, required)
  - `narration` (string, optional)
  - `payment_method` (string: `credit`, `wallet`, `cash`, `online`)
  - `bank_id` (integer, optional)
  - `bank_name` (string, optional)
  - `status` (string: `pending`, `completed`)
  - `payment_proof` (string, optional)

---

## 2. Execution Flow

1. **Transaction Start**: Runs inside `DB::transaction(...)`.
2. **Resolve Head**:
   - Sourced from `head_id` if client type is customer.
   - Sourced from agent record if client type is agent.
3. **Wallet Check**: If payment method is `wallet`, checks if `$head->wallet >= $payableAmount` (`$isWalletSufficient = true`).
4. **Advance Paid Calculation**:
   - If `wallet`, `cash`, or `online`, `$advancePaid = $payableAmount`.
   - If `credit`, `$advancePaid = 0`.
5. **Profit Formula**:
   - If `is_inhouse == 1`, supplier cost is 0, supplier ID/name is null, and profit is 0.
   - If `is_inhouse == 0`, `profit = payableAmount - expectedSupplierCost`.
6. **Create OtherService Record**:
   - Sets payment status to `'success'` if wallet is sufficient, else `'pending'`.
7. **Ledger Posting**:
   - Invokes `ledgerService->recordTransaction` to log a debit transaction of `$payableAmount` and credit of `$advancePaid`.
   - If `wallet` payment, deducts `$advancePaid` from `head->wallet`.
   - Updates `head->balance_amount` and `head->balance` by adding `(debit - credit)`.
8. **Profit Logging**: If `profit > 0` and not in-house, increments the salesman's profit count (`User->addProfit($profit)`).
