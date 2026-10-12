# ADR-003: Async/Await Usage and Cancellation Token Propagation

## Status
Accepted

## Context
Laravel operations run synchronously, blocking PHP workers. The C# API must exploit non-blocking asynchronous execution to maximize resource utilization and handle HTTP pipeline cancellations gracefully.

## Decision
All I/O operations (database queries, network HTTP requests, file reads/writes) must use asynchronous APIs end-to-end.
`CancellationToken` parameters must be accepted at the API controller/endpoint layer and propagated fully through application handlers down to the EF Core and HTTP client layers.

## Alternatives Considered
- **Selective Async**: Rejected. Sync-over-async causes thread pool starvation.

## Consequences
- **Positive**: High throughput, connection release, thread pool optimization, query cancellations on network drop.
- **Negative**: Async signature propagation required across all call chains.

## Risks
- Dropping cancellation tokens in nested service method calls.

## Revisit Conditions
None.
