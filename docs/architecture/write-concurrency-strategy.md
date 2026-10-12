# Write Concurrency Strategy

This document details concurrency management strategies for modifying mutable financial balances in the sales portal.

---

## 1. Concurrency Risks

- **Dirty Wallet Deductions**: Two concurrent booking requests check head wallet balance simultaneously, find it sufficient, and both perform deductions, resulting in a negative wallet balance.
- **Lost Cash Accumulations**: Two concurrent cash bookings read the current salesman's cash balance, increment it, and overwrite each other's updates, losing cash records.

---

## 2. Chosen Concurrency Strategies

### A. Atomic Conditional SQL Update (Heads Wallet)
To deduct wallet balances without race conditions, we execute an atomic conditional database update:
```sql
UPDATE [heads]
SET [wallet] = [wallet] - @amount,
    [balance] = [balance] + @diff,
    [balance_amount] = [balance_amount] + @diff
WHERE [id] = @headId AND [wallet] >= @amount AND [deleted_at] IS NULL;
```
- **Verification**: If rows affected is `0`, we query the database to determine if the failure was due to insufficient funds versus a missing/unauthorized head record.

### B. Atomic SQL Update (Salesman Cash-In-Hand)
Similarly, salesman physical cash accumulation is updated atomically:
```sql
UPDATE [cash_in_hands]
SET [amount] = [amount] + @amount,
    [updated_at] = SYSUTCDATETIME()
WHERE [user_id] = @userId;
```
If the salesman cash record does not exist yet (first transaction), we handle it inside the transaction using a database insert try-catch block.
