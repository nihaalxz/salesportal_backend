# Password Migration Strategy

This document outlines the authentication compatibility strategy to ensure seamless login behavior during the gradual migration from Laravel to ASP.NET Core.

---

## 1. Hashing Scheme Analysis

- **Laravel Hashing Driver**: `bcrypt` (defined in `config/hashing.php`).
- **Bcrypt Cost Factor**: Defaults to `10` rounds (value of `BCRYPT_ROUNDS`).
- **Hash Prefix**: `$2y$` (standard PHP bcrypt identifier).
- **Simultaneous Portals Execution**:
  The Laravel application contains multiple portals (Accounts Portal, Master Accountant Portal, Operations Portal) that will remain live and connect to the same shared database while the Sales Portal is migrated to the new ASP.NET Core API.
  - **Critical Compatibility Risk**: If the .NET API automatically rehashes passwords to default ASP.NET Core formats (PBKDF2/RFC 2898), the legacy Laravel portals will fail to authenticate those users because PHP's native `password_verify()` does not support ASP.NET Core PBKDF2 format.

---

## 2. Authentication and Hashing Policy

To maintain bidirectional authentication compatibility across all portals:

1. **Active Hash Format Standard**: **BCrypt** with `$2y$` or `$2a$` prefix.
2. **Automatic Rehashing**: **PROHIBITED** during the co-existence phase. We will keep user password hashes in BCrypt format.
3. **C# Implementation**:
   - Install the standard NuGet package `BCrypt.Net-Next`.
   - Implement a custom `IPasswordHasher<User>` that delegates hashing and verification exclusively to `BCrypt.Net-Next`.
   - Ensure the generated BCrypt hash uses the standard prefix compatible with PHP's `password_verify()`.

### .NET Custom BCrypt Password Hasher Example

```csharp
using Microsoft.AspNetCore.Identity;
using BC = BCrypt.Net.BCrypt;

public class BcryptPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
{
    private const int WorkFactor = 10;

    public string HashPassword(TUser user, string password)
    {
        // Generate hash using standard salt format (compatible with PHP)
        return BC.HashPassword(password, WorkFactor);
    }

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        // Detect PHP standard prefix ($2y$) and translate to standard $2a$ format if needed for verification
        string formattedHash = hashedPassword;
        if (hashedPassword.StartsWith("$2y$"))
        {
            formattedHash = "$2a$" + hashedPassword.Substring(4);
        }

        try
        {
            bool isValid = BC.Verify(providedPassword, formattedHash);
            return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }
        catch
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
```

---

## 3. Deployment and Rollback Strategy
- **Co-existence Phase**: Both .NET API and Laravel portals read and write `$2y$` or `$2a$` BCrypt hashes.
- **Final Cutover**: Once all Laravel portals are retired, a decision can be made to rehash passwords to PBKDF2 or Argon2id upon user login.
