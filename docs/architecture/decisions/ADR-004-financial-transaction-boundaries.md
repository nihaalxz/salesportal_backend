# ADR-004: Database Transaction Boundaries for Financial Consistency

## Status
Accepted

## Context
Financial mutations (bookings, payments, wallets, and ledger entries) must remain consistent, auditable, and resilient to failure. Swallowing errors or omitting database transactional rollbacks is unacceptable.

## Decision
All financial modifications within a capability boundary (e.g. payment allocations) must be wrapped in a database transaction (`BeginTransactionAsync`).
The local database state is saved and committed. Outbox records for external system changes are committed *inside the same transaction* to guarantee local consistency first.

## Alternatives Considered
- **Distributed Transactions (2PC)**: Rejected due to latency, lack of compatibility with external third-party API portals.

## Consequences
- **Positive**: Strict financial consistency locally; reliable rollback behavior.
- **Negative**: Increased transaction isolation complexity under concurrent allocations.

## Risks
- Concurrency locks blocking other salesman allocations (Mitigated by granular transaction boundaries).

## Revisit Conditions
None.
