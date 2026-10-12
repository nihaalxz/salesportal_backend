# ADR-005: Durable Outbox for Critical Integrations

## Status
Accepted

## Context
Outgoing integration calls to the Accounts Portal are business-critical. Laravel performs these synchronously inside local database transactions, causing performance bottlenecks and ledger mismatches when external API requests succeed but local DB commits fail.

## Decision
Implement a **Transactional Outbox**.
1. Local transactions save financial states and insert an `OutboxMessage` record.
2. A background hosted service (`BackgroundService`) polls the outbox table asynchronously.
3. The worker executes HTTP calls to the Accounts Portal and marks the message as processed upon success.
4. Retry logic with exponential backoff will be enforced, requiring the Accounts Portal to support idempotency checks.

## Alternatives Considered
- **In-Memory Channel<T>**: Rejected as the primary reliability mechanism due to risk of message loss on API crash or redeployments.
- **RabbitMQ/Message Broker**: Rejected to avoid infrastructure complexity. The relational database table acts as a reliable outbox storage.

## Consequences
- **Positive**: High reliability, connection isolation, eventual remote consistency, retry safety.
- **Negative**: Adds message table latency.

## Risks
- Outbox processing lag under high load (Mitigated by tuning polling frequency or using in-memory channels for worker wake-up signals).

## Revisit Conditions
Revisit if volume of transaction integration messages exceeds relational database processing capabilities.
