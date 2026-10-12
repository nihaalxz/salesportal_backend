# Financial Precision and Rounding Strategy

This document details the precision rules, calculations, and rounding behavior discovered in the Laravel project, mapping them to the target .NET Core implementation.

---

## 1. Database Decimal Precisions

All monetary columns are standardized to **3 decimal places** via Laravel migrations (specifically `2026_02_19_084727_standardize_decimals_to_3_places.php`).

| Table Name | Column Name | Database Precision | Mapped C# Type |
| :--- | :--- | :--- | :--- |
| `flight_passengers` | `amount`, `supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `hotel_bookings` | `payable_amount`, `expected_supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `transportations` | `payable_amount`, `expected_supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `other_services` | `payable_amount`, `expected_supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `sales_visa_applicants`| `amount`, `unpaid_amount`, `supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `holiday_packages` | `payable_amount`, `expected_supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `vfs_appointments` | `payable_amount`, `expected_supplier_cost`, `profit` | `decimal(10,3)` | `decimal` |
| `heads` | `balance`, `balance_amount`, `wallet` | `decimal(10,3)` | `decimal` |
| `receipts` | `amount` | `decimal(10,3)` | `decimal` |
| `users` | `profit_generated`, `incentive`, `profit_target`, `monthly_target` | `decimal(10,3)` | `decimal` |

---

## 2. Intermediate Calculations and Formatting

- **Arithmetic Operations**:
  - Profit is calculated in-memory during sync or booking checkout: `payable_amount - expected_supplier_cost` (and minus `sales_returns` during sync).
  - The Laravel code performs basic floating-point arithmetic (e.g. `(float)$val`) without BC Math (`bcadd`, `bcmul`) or explicit calls to PHP `round()`.
- **Database Serialization**:
  - The calculated floats are stored directly in `decimal(10,3)` fields. MySQL handles the rounding to 3 decimal places implicitly during SQL execution.
- **Display Formatting**:
  - Standard Blade views and models format BHD currencies using `number_format($value, 3)`, which rounds half-up (`MidpointRounding.AwayFromZero` equivalent).

---

## 3. Recommended .NET Policy

1. **Intermediate Calculations**:
   Perform all calculations using C# `decimal` to eliminate floating-point precision issues inherent to PHP floats.
2. **Explicit Rounding**:
   When intermediate rounding is needed (or when formatting for string display), use `MidpointRounding.AwayFromZero` to match standard `number_format` behavior:
   ```csharp
   decimal roundedValue = Math.Round(rawDecimal, 3, MidpointRounding.AwayFromZero);
   ```
3. **EF Core Configuration**:
   Explicitly configure EF Core database column types:
   ```csharp
   builder.Property(x => x.Amount)
          .HasPrecision(15, 3);
   ```
