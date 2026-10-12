# Sales Read Performance Dataset Strategy

To execute deterministic baselines and integration tests, we populate the SQL Server target database with the following scale:

- **Salesmen Accounts**: 2 isolated records.
- **Customer Heads**: 3 records.
- **Leads**: 1 record.
- **Bookings**: 1 flight booking record.
- **Cash Records**: 1 cash record.
- **Deposits**: 1 deposit record.
- **Monthly Records**: 1 Monthly records count.

All test suites verify isolation bounds (Salesman A cannot view Salesman B's data).
