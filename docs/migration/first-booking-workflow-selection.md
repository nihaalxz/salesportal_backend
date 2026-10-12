# First Booking Workflow Selection Analysis

To establish a production-grade C# booking write transaction slice, we evaluate and compare legacy Laravel service workflows.

---

## 1. Booking Workflows Matrix

| Workflow Type | Tables Written | Ledger Writes? | Immediate Payment? | Wallet Mutations? | Cash Mutation? | External API Dependency? | Complexity Class |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Other Service Booking** | `other_services`, `heads`, `head_ledgers` | **Yes** (Debit + Credit) | **Yes** (Cash, Wallet, Online) | **Yes** (`wallet` payment) | **Yes** (`cash` payment) | Optional (Only for Agent client-type) | **Low** |
| **Transport Booking** | `transportations`, `heads`, `head_ledgers` | **Yes** (Debit + Credit) | **Yes** (Cash, Online) | No | **Yes** | Yes (Supplier synced) | **Medium** |
| **Flight Booking** | `flight_bookings`, `flight_passengers`, `head_ledgers` | **Yes** | **Yes** | No | **Yes** | Yes (Sync GDS APIs) | **High** |
| **Hotel Booking** | `hotel_bookings`, `head_ledgers` | **Yes** | **Yes** | No | **Yes** | Yes (Inventory check) | **High** |

---

## 2. Selection Rationale

- **Selected Candidate**: **Other Service Booking**
- **Justification**:
  1. Exercises all transaction, wallet deduction, cash accumulation, ledger debit/credit, and profit addition invariants in one workflow.
  2. Offers a clean isolation path: when client type is `customer`, it does not require external HTTP syncs to the accounts portal, allowing us to build the transaction slice without immediate outbox complexity.
  3. Uses a simple flat structure (no passenger/applicant child tables), minimizing DDL and validation noise so we can focus on core transactional and concurrency safety patterns.
