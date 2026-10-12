# ADR-001: Modular Monolith Solution Structure

## Status
Accepted

## Context
The platform contains multiple portals: Sales Portal, Accounts Portal, Master Accountant Portal, and Operations Portal. Building them as isolated services introduces distributed network complexity, data consistency challenges, and deployment overhead prematurely.

## Decision
Create a single deployable C# solution (`TexasPortal.sln`) following clean architecture within a Modular Monolith.
Logical separation will be maintained at the project level to support future module extraction (e.g. Sales, Identity, Accounting) if scaling demands justify split deployments.

## Alternatives Considered
- **Microservices**: Rejected due to high overhead, network latency, and complexity of distributed transactions across financial updates.
- **Single Spaghetti Project**: Rejected due to risk of tight coupling between portals, making future feature isolation impossible.

## Consequences
- **Positive**: Simplified deployment, unified transaction scope, clean separation of concerns.
- **Negative**: Increased initial project ceremony.

## Risks
- Direct database references between modules bypassing boundary contracts (Mitigated via custom Architecture Tests).

## Revisit Conditions
Revisit if operational scaling or organization size warrants independent project teams and deployments.
