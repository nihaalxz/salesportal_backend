# Financial Reconciliation Strategy

This document details the reconciliation audit rules used to verify data integrity and correct type transformations after executing the database engine migration.

---

## 1. Zero-Tolerance Policy

Because the system manages money (ledger credits, customer balances, physical cash-in-hand), **no rounding tolerances are permitted**. All calculated totals between the legacy MySQL database and target SQL Server database must match exactly to **3 decimal places** (`0.000 BHD`). Any discrepancy represents a migration failure.

---

## 2. Audit Reconciliation Queries

### Audit 1: Customer Ledger Balance Integrity
Verify that the sum of debits and credits in the head ledger exactly equals the sum of the ledger records.
- **Source MySQL**:
  ```sql
  SELECT head_id, SUM(debit) as total_debit, SUM(credit) as total_credit 
  FROM head_ledgers 
  GROUP BY head_id;
  ```
- **Target SQL Server**:
  ```sql
  SELECT head_id, SUM(debit) as total_debit, SUM(credit) as total_credit 
  FROM head_ledgers 
  GROUP BY head_id;
  ```
- **Constraint**: `total_debit (MySQL) == total_debit (SQL Server)` and `total_credit (MySQL) == total_credit (SQL Server)` for every individual `head_id`.

### Audit 2: Derived vs. Stored Head Balance
Verify that the customer's stored balance equals the sum of ledger debits minus ledger credits.
- **Query (Both)**:
  ```sql
  SELECT h.id, h.balance, 
         (SELECT COALESCE(SUM(hl.debit) - SUM(hl.credit), 0) FROM head_ledgers hl WHERE hl.head_id = h.id) as derived_balance
  FROM heads h;
  ```
- **Constraint**: `derived_balance == balance` for every head.

### Audit 3: Cash In Hand Totals
Verify that the total physical cash tracked for all salesmen matches exactly.
- **Query (Both)**:
  ```sql
  SELECT SUM(amount) FROM cash_in_hands;
  ```

### Audit 4: Total Booking Payments
- **Query (Both)**:
  ```sql
  SELECT SUM(payable_amount) FROM flight_bookings;
  SELECT SUM(payable_amount) FROM hotel_bookings;
  ```
