using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TexasPortal.Application.Auth;
using TexasPortal.Application.Persistence;
using TexasPortal.Contracts.Auth;

using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace TexasPortal.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginHandler         _loginHandler;
    private readonly CreateUserHandler    _createUserHandler;
    private readonly IConfiguration       _config;
    private readonly IWebHostEnvironment  _env;

    private ISalesPortalDbContext _db => HttpContext.RequestServices.GetRequiredService<ISalesPortalDbContext>();

    public AuthController(
        LoginHandler          loginHandler,
        CreateUserHandler     createUserHandler,
        IConfiguration        config,
        IWebHostEnvironment   env)
    {
        _loginHandler      = loginHandler;
        _createUserHandler = createUserHandler;
        _config            = config;
        _env               = env;
    }

    /// <summary>Authenticates a user and returns a JWT access token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { detail = "Email and password are required." });

        var user = await _loginHandler.ValidateCredentialsAsync(request.Email, request.Password, ct);
        if (user is null)
            return Unauthorized(new { detail = "Invalid email or password." });

        var roleLower = user.UserType?.ToLower() ?? "";
        bool isSalesRole = roleLower.Contains("sales") || roleLower.Contains("salesman");
        if (!isSalesRole)
        {
            return StatusCode(403, new { detail = "Access Denied: This user account is not authorized to access the Sales Portal." });
        }

        // Build JWT claims
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
            new Claim(ClaimTypes.Name,               user.Name),
            new Claim(ClaimTypes.Role,               user.UserType),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:ExpiryHours", 8));

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new LoginResponse(
            Token:     tokenString,
            ExpiresAt: expiry,
            User: new LoginUserDto(
                Id:    user.Id,
                Name:  user.Name,
                Email: user.Email,
                Role:  user.UserType,
                Avatar: user.Avatar));

        return Ok(response);
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(idClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound();

        var response = new MeResponse(
            Id:    userId,
            Name:  user.Name,
            Email: user.Email,
            Role:  user.UserType,
            Avatar: user.Avatar);

        return Ok(response);
    }

    /// <summary>Uploads and updates the profile picture of the authenticated user.</summary>
    [HttpPost("me/avatar")]
    [Authorize]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request, CancellationToken ct)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.AvatarData))
            return BadRequest(new { detail = "Avatar data is required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound();

        // Decode base64 and save to wwwroot/uploads/avatars
        string base64Data = request.AvatarData;
        string extension = ".png";

        if (base64Data.Contains(","))
        {
            var parts = base64Data.Split(',');
            var meta = parts[0];
            base64Data = parts[1];

            if (meta.Contains("image/jpeg") || meta.Contains("image/jpg"))
                extension = ".jpg";
            else if (meta.Contains("image/gif"))
                extension = ".gif";
            else if (meta.Contains("image/webp"))
                extension = ".webp";
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            return BadRequest(new { detail = "Invalid image base64 format." });
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRoot, "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, bytes, ct);

        // Delete old local avatar if exists
        if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar.StartsWith("/uploads/avatars/"))
        {
            var oldFileName = Path.GetFileName(user.Avatar);
            var oldFilePath = Path.Combine(uploadsFolder, oldFileName);
            if (System.IO.File.Exists(oldFilePath))
            {
                try
                {
                    System.IO.File.Delete(oldFilePath);
                }
                catch
                {
                    // Ignore file delete errors
                }
            }
        }

        user.Avatar = $"/uploads/avatars/{fileName}";
        await _db.SaveChangesAsync(ct);

        return Ok(new { avatar = user.Avatar });
    }

    /// <summary>Changes the password of the currently authenticated user.</summary>
    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(idClaim, out var userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { detail = "Current password and new password are required." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { detail = "New password must be at least 6 characters long." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return NotFound();

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
        {
            return BadRequest(new { detail = "Current password is incorrect." });
        }

        // Hash and update new password
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Password updated successfully." });
    }

    /// <summary>
    /// Creates a new user. Open ONLY when no users exist yet (initial bootstrap).
    /// Once any user exists this endpoint returns 403.
    /// </summary>
    [HttpPost("users")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var hasUsers = await _createUserHandler.AnyUsersExistAsync(ct);
        if (hasUsers)
            return StatusCode(403, new { detail = "User creation is restricted. Contact your system administrator." });

        try
        {
            var result = await _createUserHandler.HandleAsync(request, ct);
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { detail = ex.Message });
        }
    }
}
