# Phase 10B Deterministic Read Data Strategy

This document details the deterministic database records, visibility rules, and seeding strategy used to verify ownership isolation across Salesman A and Salesman B.

---

## 1. Deterministic Seeding Mapping

The database schema is seeded with three users and their associated records:

| Record Purpose | Owner | Entity Type | Mapped ID | Details | Expected Visibility | Expected Cross-Owner Visibility |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Salesman A User** | System | `User` | `1` | John Salesman (`john.sales@texastravelsbh.com`) | Visible to self | None |
| **Salesman B User** | System | `User` | `2` | Sarah Salesman (`sarah.sales@texastravelsbh.com`) | Visible to self | None |
| **Guest User** | System | `User` | `3` | Guest User (`guest@texastravelsbh.com`) | Visible to self | None |
| **Salesman A Head** | Salesman A | `Head` | `1` | Alice A_Head (Manama) | John only | Sarah cannot see |
| **Salesman B Head** | Salesman B | `Head` | `2` | Bob B_Head (Khobar) | Sarah only | John cannot see |
| **Salesman A Ledger 1** | Salesman A | `HeadLedger` | `1` | Debit 250.750 BHD (Flight Booking) | John only | Sarah cannot see |
| **Salesman A Ledger 2** | Salesman A | `HeadLedger` | `2` | Credit 100.000 BHD (Receipt Deposit) | John only | Sarah cannot see |
| **Salesman B Ledger 1** | Salesman B | `HeadLedger` | `3` | Debit 480.000 BHD (Hotel Booking) | Sarah only | John cannot see |
| **Salesman A Bookings** | Salesman A | `FlightBooking`, `HotelBooking` | `1` | 1 Flight, 1 Hotel (Payable: 350.750, Profit: 25.000) | John's Dashboard | Sarah cannot see |
| **Salesman B Bookings** | Salesman B | `HotelBooking` | `2` | 1 Hotel (Payable: 480.000, Profit: 80.000) | Sarah's Dashboard | John cannot see |
| **Salesman A Cash** | Salesman A | `CashInHand` | `1` | Cash: 350.250 BHD | John's Dashboard | Sarah cannot see |
| **Salesman B Cash** | Salesman B | `CashInHand` | `2` | Cash: 120.500 BHD | Sarah's Dashboard | John cannot see |
| **Salesman A Leads** | Salesman A | `Lead` | `1` | Active Leads Count: 1 | John's Dashboard | Sarah cannot see |
| **Salesman B Leads** | Salesman B | `Lead` | `2` | Active Leads Count: 1 | Sarah's Dashboard | John cannot see |

---

## 2. Seeding Mechanism

The seeding process is fully automated via the `DevSeeder` class in `Program.cs`. 

- **Trigger**: Setting the environment variable `SEED_DETERMINISTIC_DATA=true` when launching the ASP.NET Core process in the `Development` environment.
- **Safety Gate**: The `DevSeeder` checks the connection string using `db.Database.GetConnectionString()`. It will refuse to run if the host does not indicate local execution (e.g. `localhost`, `127.0.0.1`, `SQLEXPRESS`, or `(localdb)`), preventing any accidental data loss in shared remote development or production catalogs.

---

## 3. Data Cleanup & Resettability Strategy

For every test run or seed invocation:
1. All dependent child tables (Ledgers, Bookings, Cash, Leads) are cleared.
2. Parent tables (Heads, Users) are cleared.
3. Identity seed counters are reset to 0 using `DBCC CHECKIDENT ('table', RESEED, 0)`.
4. The migration schema is applied, and the tables are re-populated from scratch, ensuring a clean, reproducible state.
