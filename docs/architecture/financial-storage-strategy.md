# Financial Storage Strategy

This document details the financial storage precision, rounding behaviors, and representation policies during modernization to SQL Server.

---

## 1. Monetary Precision Strategy

### Legacy Standard
Laravel migrations use `decimal(10,3)` for standard monetary amounts (payable amounts, profits, agent balances, wallets) and `decimal(15,3)` for large head ledger debit/credit postings.
- **Scale**: `3` decimal places (required for Bahraini Dinar (BHD) fractional values, e.g. 1.250 BHD).
- **Precision**: `10` digits (maximum amount of 9,999,999.999 BHD).

### SQL Server Standard
To prevent arithmetic overflows under high-value corporate ledgers, we map target database decimal properties to:
- **`decimal(15,3)`** for all financial columns (balances, ledgers, payable amounts, profit calculation parameters).
- **Scale**: Mapped strictly to `3` to guarantee exact representability of BHD values.
- **Precision**: Increased to `15` digits (maximum amount of 999,999,999,999.999 BHD).

### Verification
Increasing the precision to 15 while keeping the scale at 3 does not affect decimal fractional exactness. 
All legacy MySQL values (e.g. `1234.567`) are exactly representable as `decimal(15,3)` in SQL Server without rounding.

---

## 2. API Contract & Coexistence Policy

During the coexistence phase where both Laravel and C# backends run concurrently:
1. **API Mappings**: The C# API contracts will format decimals as string representations with 3 decimal places (`"123.450"`) to prevent parsing errors in Svelte frontends.
2. **Database Constraints**: C# writes will restrict values to 10-digit limits at the application level if they are writing to tables mapped as `decimal(10,3)` on shared legacy databases to avoid causing Laravel insert warnings.
3. **Rounding Rules**: Standardized to `MidpointRounding.AwayFromZero` when doing tax, target, and profit calculations to align with PHP's `round()` output.
