# ADR-006: Legacy Authentication Compatibility

## Status
Accepted

## Context
Laravel uses BCrypt with a `$2y$` prefix for user password hashing. Other portals (Operations, Accounts, Master Accountant) continue running concurrently against the same user database.

## Decision
Configure the .NET backend to use BCrypt hashing for all credentials.
- Do NOT rehash passwords to PBKDF2/RFC 2898.
- Use a custom `IPasswordHasher<User>` that delegates verification and hashing to `BCrypt.Net-Next`.
- Configure the hasher to produce hashes compatible with PHP's `password_verify` standards.

## Alternatives Considered
- **Automatic password rehashing to PBKDF2**: Rejected because it would break logins on the live Laravel portals.

## Consequences
- **Positive**: Complete bidirectional login compatibility between the new C# API and existing Laravel portals.
- **Negative**: Domain depends on BCrypt libraries.

## Risks
- Incorrect prefix formatting during migration checks.

## Revisit Conditions
Revisit once all legacy Laravel portals are retired and the database schema is fully controlled by the .NET core.
