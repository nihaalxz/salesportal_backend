# Financial Reconciliation Query Specifications

This document outlines the core SQL queries and equations required to reconcile financial balances and double-entry ledgers between the legacy Laravel system and the new ASP.NET Core backend.

---

## 1. Reconciliation Principles

1. **Balance Equivalence**: The balance of a customer account (`heads.balance_amount`) must equal the sum of their ledger entries:
   $$\text{balance} = \sum \text{credit} - \sum \text{debit}$$
2. **Salesman Target Parity**: The sum of salesman earnings/incentives must equal monthly records aggregations.
3. **Wallet Balance Consistency**: Wallet mutations must tie back to cash/bank deposit records.

---

## 2. SQL Server Reconciliation Queries

### Query A: Customer Ledger Balance Parity Check
Checks if any customer's recorded balance deviates from their total ledger entry sum.

```sql
SELECT 
    h.id AS HeadId,
    h.first_name,
    h.last_name,
    h.balance_amount AS RecordedBalance,
    COALESCE(SUM(l.credit - l.debit), 0) AS CalculatedBalance,
    (h.balance_amount - COALESCE(SUM(l.credit - l.debit), 0)) AS BalanceDiscrepancy
FROM heads h
LEFT JOIN head_ledgers l ON h.id = l.head_id
GROUP BY h.id, h.first_name, h.last_name, h.balance_amount
HAVING (h.balance_amount - COALESCE(SUM(l.credit - l.debit), 0)) <> 0;
```

### Query B: Total Ledger Cash Flow Balance
Verifies that all debits and credits across the system tie out correctly (for double-entry integrity checks).

```sql
SELECT 
    SUM(debit) AS TotalDebits,
    SUM(credit) AS TotalCredits,
    (SUM(debit) - SUM(credit)) AS SystemImbalance
FROM head_ledgers;
```

### Query C: Cash-in-Hand vs Deposits Allocation
Ensures that a user's cash-in-hand matches their logged transactions minus approved bank deposits.

```sql
SELECT 
    u.id AS UserId,
    u.name AS SalesmanName,
    cih.amount AS CurrentCashInHand,
    COALESCE(SUM(r.amount), 0) AS TotalCollectedReceipts,
    COALESCE(SUM(d.amount), 0) AS TotalDeposited
FROM users u
LEFT JOIN cash_in_hands cih ON u.id = cih.user_id
LEFT JOIN receipts r ON u.id = r.agent_id AND r.status = 'approved' AND r.payment_mode = 'cash'
LEFT JOIN deposits d ON u.id = d.user_id AND d.status = 'approved' AND d.type = 'cash'
GROUP BY u.id, u.name, cih.amount;
```
