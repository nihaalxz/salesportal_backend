using Microsoft.EntityFrameworkCore;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Auth;
using TexasPortal.Domain.Entities;

namespace TexasPortal.Application.Auth;

public sealed class CreateUserHandler
{
    private readonly ISalesPortalDbContext _db;

    public CreateUserHandler(ISalesPortalDbContext db)
    {
        _db = db;
    }

    public async Task<bool> AnyUsersExistAsync(CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(ct);
    }

    /// <summary>
    /// Creates a new user account. The caller must ensure this is only
    /// invoked when authorized (e.g. first user bootstrap or admin-only).
    /// </summary>
    public async Task<CreateUserResponse> HandleAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        // Check for duplicate email
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (exists)
            throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new User
        {
            Name      = request.Name,
            Email     = request.Email,
            Password  = hashedPassword,
            UserType  = request.UserType,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new CreateUserResponse(
            Id:       user.Id,
            Name:     user.Name,
            Email:    user.Email,
            UserType: user.UserType);
    }
}
