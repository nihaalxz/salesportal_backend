# Concurrency Risks Analysis

This document evaluates the concurrency behavior of financial mutations in the Laravel Sales Portal database, classifying each as either database-level atomic or memory-level read-modify-write.

---

## 1. Inventory of Financial Mutations and Concurrency Behavior

| Mutation Type | Laravel Source Location | Operation Behavior | Concurrency Safety | Under Transaction? | Remaining Race Risk |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **User Profit Increment** | `User::addProfit` (lines 89, 99) | `DB::table('users')->increment()` | **Atomic UPDATE Expression**:<br>`UPDATE users SET profit_generated = profit_generated + ? WHERE id = ?` | Yes | Low |
| **User Profit Sync Adjustment** | `InHouseCostSyncController::syncCost` (lines 127-129) | Read model, increment in memory, call `$user->save()` | **Read-Modify-Write**:<br>`UPDATE users SET profit_generated = ? WHERE id = ?` | Yes | **High**: Concurrent syncs for the same salesman will corrupt metrics. |
| **Head Wallet Credit/Debit** | `BookingPaymentService::allocateReceipt` (line 80) | `$head->increment('wallet')` | **Atomic UPDATE Expression**:<br>`UPDATE heads SET wallet = wallet + ? WHERE id = ?` | Yes | Low |
| **Head Balance Mutation** | `HeadLedgerService::recordTransaction` (lines 48-50) | `$head->balance_amount += $diff`, `$head->save()` | **Read-Modify-Write**:<br>`UPDATE heads SET balance_amount = ? WHERE id = ?` | Yes | **High**: Simultaneous ledger postings for the same customer will corrupt balance. |
| **Cash-In-Hand Increment** | `BookingPaymentService::allocateReceipt` (line 54) | `CashInHand::updateOrCreate` with `DB::raw('amount + ?')` | **Atomic UPDATE Expression** | Yes | Low |
| **Cash-In-Hand Decrement** | `BookingPaymentService::reverseReceipt` (line 118) | `->decrement('amount')` | **Atomic UPDATE Expression** | Yes | Low |
| **Payment Allocation Settle** | `BookingPaymentService::processSingleAllocation` (lines 187-189) | `$booking->payment_status = 'success'`, `$booking->save()` | **Overwrite state** | Yes | **High**: Concurrent requests validating different receipts can double-allocate to same booking. |

---

## 2. Concurrency Safety Standards for C#

1. **Optimistic Concurrency Control**:
   - Apply `[Timestamp]` or `[ConcurrencyCheck]` to `Head` and `User` entities. This ensures that any read-modify-write operations check row versions, throwing a `DbUpdateConcurrencyException` if modified since read.
2. **Atomic Increments/Decrements**:
   - Use EF Core's `ExecuteUpdateAsync` for atomic updates to avoid loading entities into memory:
     ```csharp
     await db.Heads
         .Where(h => h.Id == headId)
         .ExecuteUpdateAsync(s => s.SetProperty(h => h.Balance, h => h.Balance + diff), cancellationToken);
     ```
3. **Idempotency Keys**:
   - Enforce idempotency on payment allocations and booking creation requests using unique client-provided keys.
