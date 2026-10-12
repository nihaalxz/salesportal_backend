# Flight Profit Concurrency Verification

This document analyzes and verifies the safety of concurrent salesman profit updates, ensuring that simultaneous booking creations do not lead to lost updates on the `User.ProfitGenerated` column.

---

## 1. Concurrency Race Condition Scenario

When multiple bookings are created simultaneously by or for the same salesman:
1. Booking A is submitted (Profit = $20.000\text{ BHD}$).
2. Booking B is submitted (Profit = $20.000\text{ BHD}$).
3. If the application reads the salesman's current `ProfitGenerated` (e.g., $100.000\text{ BHD}$) into memory:
   - Request A reads $100.000\text{ BHD}$, calculates $100 + 20 = 120$.
   - Request B reads $100.000\text{ BHD}$, calculates $100 + 20 = 120$.
4. Request A writes $120.000\text{ BHD}$ to the database.
5. Request B writes $120.000\text{ BHD}$ to the database.
6. **Result**: One of the profit updates is **lost**, and the balance is $120.000\text{ BHD}$ instead of the correct $140.000\text{ BHD}$.

---

## 2. Guard Design & Implementation

To prevent lost updates, the application performs an atomic database update statement with a mathematical expression evaluated directly on the database engine:

```csharp
if (totalProfit > 0m)
{
    var affectedUserRows = await _db.UsersDbSet
        .Where(u => u.Id == salesmanId)
        .ExecuteUpdateAsync(s => s.SetProperty(u => u.ProfitGenerated, u => u.ProfitGenerated + totalProfit), transactionCt);
        
    if (affectedUserRows == 0)
    {
        throw new InvalidOperationException($"Salesman with ID {salesmanId} not found.");
    }
}
```

### Database Execution:
This statement is translated by EF Core to:
```sql
UPDATE [Users] 
SET [ProfitGenerated] = [ProfitGenerated] + @totalProfit 
WHERE [Id] = @salesmanId;
```
SQL Server acquires an exclusive update lock on the salesman's row and performs the addition directly in-place. Other concurrent transactions block until this lock is released, preserving absolute serializability for this increment.

---

## 3. Automated Test Verification

The integration test `ConcurrentProfitUpdates_DoNotLoseUpdates` performs the following:
- **Salesman Initial Profit**: $0.000\text{ BHD}$.
- **Simultaneous Bookings**: Spawns 4 concurrent booking transactions yielding $20.000\text{ BHD}$ profit each.
- **Results**:
  - All 4 requests succeed with `201 Created`.
  - **Final Database Profit**: Verified to be exactly $80.000\text{ BHD}$. No lost updates detected.
