# Status Value Inventory

This document inventories all string and enum status fields used in legacy Laravel MySQL schema and maps their C# representation and EF Core translation strategy.

---

## 1. Mapped Status Properties Inventory

| Table | Column | MySQL Type | Schema Values | Laravel Code Usage | Target SQL Server Type | EF Core & C# Representation | Validation Strategy |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `heads` | `status` | `enum` | `'active', 'inactive'` | `active`, `inactive` | `nvarchar(20)` | `string` properties with Constants class checks. | Restrict writes via C# domain verification. |
| `heads` | `type` | `varchar` | - | `customer`, `agent` | `nvarchar(255)` | `string` constants. | Allowed values checked in validator. |
| `receipts` | `status` | `enum` | `'pending', 'collection', 'collected', 'success'` | `pending`, `success` etc. | `nvarchar(20)` | `string` constants. | Database CHECK constraint `status IN ('pending', 'collection', 'collected', 'success')`. |
| `receipts` | `payment_mode` | `enum` | `'cash', 'online', 'wallet'` | `cash`, `online`, `wallet` | `nvarchar(20)` | `string` constants. | CHECK constraint `payment_mode IN ('cash', 'online', 'wallet')`. |
| Bookings | `payment_status` | `varchar` | - | `pending`, `success` | `nvarchar(255)` | `string` constants. | Validate in Application service. |
| Bookings | `processing_status`| `varchar` | - | `pending`, `processing`, `completed` | `nvarchar(255)` | `string` constants. | Validate in Application service. |
| `deposits` | `status` | `varchar` | - | `pending`, `success` | `nvarchar(255)` | `string` constants. | Checked in validator. |
| `disputes` | `status` | `enum` | `'open', 'resolved', 'rejected'` | `open`, `resolved`, `rejected` | `nvarchar(20)` | `string` constants. | CHECK constraint `status IN ('open', 'resolved', 'rejected')`. |

---

## 2. Design Rationale

> [!TIP]
> **Why Strings over Numeric Enums**:
> 1. Mapping database enums directly to C# integer enums is highly fragile under migration co-existence. If other Laravel portals insert a new string status value, the .NET materializer will crash.
> 2. Using `string` properties with explicit verification (e.g. `public static class ReceiptStatuses`) avoids materialization crashes while ensuring type safety.
