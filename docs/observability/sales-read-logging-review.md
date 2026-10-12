# Sales Read Logging Review

This audit reviews message templates, structured properties, and sensitive data checks across our logging pipeline.

---

## 1. Structured Logging Principles

- **Message Templates**: We require structured templates (e.g. `logger.LogInformation("Querying heads for user {UserId}", userId);`) instead of string interpolation (`$"Querying heads for user {userId}"`). This ensures properties are indexed correctly in structural log analyzers.
- **Sensitivity Checks**: Sourced data like Customer Names, emails, phone numbers, passport credentials, or passwords must never be stored inside structured log properties to prevent GDPR/HIPAA/PCI leakage.
- **Exception Traces**: EF Core exceptions and network failures are captured automatically in the API global exception handler middleware and serialized as safe ProblemDetails, preserving detailed stack traces in secure log stores only.
