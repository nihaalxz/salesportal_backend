using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Auth;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Auth;

public sealed class LoginHandler
{
    private readonly ISalesPortalDbContext _db;

    public LoginHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Validates credentials and returns the matching User entity, or null if invalid.
    /// JWT generation is delegated to the API layer (AuthController) which has access
    /// to configuration and the authentication infrastructure packages.
    /// </summary>
    public async Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        if (user is null)
            return null;

        return BCrypt.Net.BCrypt.Verify(password, user.Password) ? user : null;
    }
}
