# Flight Financial Model

This document maps the financial formulas, rules, and invariants governing the Flight Booking vertical slice, comparing the legacy Laravel implementation against the .NET Clean Architecture implementation.

---

## 1. Core Financial Rules

| Rule / Invariant | Legacy Laravel Model | .NET Implementation |
| :--- | :--- | :--- |
| **Booking-Level Cost / Amount** | No `amount` column in `flight_bookings`. It is derived as the sum of passenger amounts (`debit` in ledger, `payable_amount` in response). | Preserved. The booking-level `PayableAmount` is calculated as: $$\sum_{i=1}^{N} \text{Passenger.Amount}_i$$ |
| **Passenger-Level Profit** | Calculated on `flight_passengers` table as: $$\text{Profit} = \text{Amount} - \text{ExpectedSupplierCost}$$ (for non-inhouse). | Preserved. Calculated inside `CreateFlightBookingHandler` and persisted on `FlightPassenger.Profit`. |
| **In-House Bookings** | Set `is_inhouse = true`. Supplier ID is null. Expected supplier cost is set to `0.000m`. Profit is hardcoded to `0.000m`. | Preserved. The `CreateFlightBookingHandler` overrides supplier costs and profits to `0.000m` for all in-house passengers. |
| **Salesman Profit** | Incremented by the booking-level aggregate profit (if $> 0$ and non-inhouse). | Preserved. Salesman's `ProfitGenerated` is updated atomically during the booking creation transaction. |

---

## 2. Payment Method Actions & Balance Formulas

The payment method determines the credit/debit ledger entries and the balance mutations on other entities (Heads, Cash-In-Hand, and Wallets). All decimal operations are enforced with **3-decimal precision** (Bahraini Dinar - BHD).

Let $A_{\text{total}}$ be the total payable amount for the flight booking ($A_{\text{total}} = \sum \text{Passenger.Amount}$), and $P_{\text{total}}$ be the sum of passenger profits.

### A. Cash Payment
- **Ledger Entries**:
  - Debit: $A_{\text{total}}$
  - Credit: $A_{\text{total}}$
- **Entity Mutations**:
  - `CashInHand.Amount` (for current Salesman) is incremented by $A_{\text{total}}$.
  - `User.ProfitGenerated` (for current Salesman) is incremented by $P_{\text{total}}$ (if non-inhouse).
- **Head Balance/Wallet**: No effect.

### B. Wallet Payment
- **Ledger Entries**:
  - Debit: $A_{\text{total}}$
  - Credit: $A_{\text{total}}$
- **Entity Mutations**:
  - `Head.Wallet` is decremented by $A_{\text{total}}$ atomically.
  - `User.ProfitGenerated` is incremented by $P_{\text{total}}$ (if non-inhouse).
- **Validation**: Enforce $Head.Wallet \ge A_{\text{total}}$, otherwise return `422 Unprocessable Entity` (Insufficient Wallet).
- **CashInHand**: No effect.

### C. Credit Payment
- **Ledger Entries**:
  - Debit: $A_{\text{total}}$
  - Credit: $0.000\text{ BHD}$
- **Entity Mutations**:
  - `Head.Balance` is incremented by $A_{\text{total}}$.
  - `Head.BalanceAmount` is incremented by $A_{\text{total}}$.
  - `User.ProfitGenerated` is incremented by $P_{\text{total}}$ (if non-inhouse).
- **CashInHand/Wallet**: No effect.

### D. Online Payment
- **Ledger Entries**:
  - Debit: $A_{\text{total}}$
  - Credit: $A_{\text{total}}$
- **Entity Mutations**:
  - `User.ProfitGenerated` is incremented by $P_{\text{total}}$ (if non-inhouse).
- **Validation**: Enforce valid active `BankId`.
- **CashInHand/Wallet/Head Balance**: No effect.

---

## 3. Database Precision Invariant
- **Currency**: Bahraini Dinar (BHD).
- **EF Core Database Mappings**: All decimal columns in SQL Server use `decimal(18, 3)` configurations to prevent rounding discrepancies.
