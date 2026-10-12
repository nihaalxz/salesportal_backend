# Flight Wallet Concurrency Verification

This document analyzes and verifies the safety of concurrent wallet booking payments targeting the same Head, ensuring that overdrafts are impossible.

---

## 1. Concurrency Vulnerability Scenario

If two concurrent requests attempt to deduct from a customer's wallet balance:
1. Request A reads current wallet balance (e.g. $100\text{ BHD}$).
2. Request B reads current wallet balance (e.g. $100\text{ BHD}$).
3. Request A validates that $100 \ge 60$ (payable amount for Booking A) and proceeds.
4. Request B validates that $100 \ge 60$ (payable amount for Booking B) and proceeds.
5. Request A writes updated wallet balance $40\text{ BHD}$ ($100 - 60$) to database.
6. Request B writes updated wallet balance $40\text{ BHD}$ ($100 - 60$) to database.
7. **Result**: Head got two bookings totaling $120\text{ BHD}$ but wallet balance was only deducted by $60\text{ BHD}$, resulting in a **double-spending/overdraft leak**.

---

## 2. Guard Design & Implementation

To prevent this, the .NET backend uses an atomic database update statement with a conditional check directly at the database level:

```csharp
var affectedRows = await _db.HeadsDbSet
    .Where(h => h.Id == request.HeadId && h.Wallet >= totalPayable)
    .ExecuteUpdateAsync(s => s.SetProperty(h => h.Wallet, h => h.Wallet - totalPayable), transactionCt);

if (affectedRows == 0)
{
    // Double check if head exists to return correct error
    var headExists = await _db.HeadsDbSet.AnyAsync(h => h.Id == request.HeadId, transactionCt);
    if (!headExists)
        return CreateFlightBookingResult.ValidationError(new Dictionary<string, string[]> { { "headId", ["Head not found."] } });

    return CreateFlightBookingResult.InsufficientWallet("Insufficient wallet balance.");
}
```

### Why this is safe:
1. **Atomic Update**: The execution runs as a single SQL query:
   ```sql
   UPDATE [Heads] SET [Wallet] = [Wallet] - @totalPayable 
   WHERE [Id] = @headId AND [Wallet] >= @totalPayable;
   ```
2. **Row-level Locks**: SQL Server locks the row during the update. If the wallet drops below `@totalPayable`, the filter condition `[Wallet] >= @totalPayable` fails, `affectedRows` returns `0`, and the transaction fails and rolls back cleanly.

---

## 3. Automated Test Verification

The test `ConcurrentWalletPayments_DoNotOverspend` simulates this exact scenario:
- **Head Initial Wallet**: $100.000\text{ BHD}$.
- **Simultaneous Bookings**: Three concurrent requests of $60.000\text{ BHD}$ each are sent.
- **Results**:
  - Request 1: `201 Created` (wallet decremented to $40.000\text{ BHD}$).
  - Request 2: `422 Unprocessable Entity` (Insufficient Wallet).
  - Request 3: `422 Unprocessable Entity` (Insufficient Wallet).
  - **Final Database Wallet**: $40.000\text{ BHD}$.
