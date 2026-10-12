namespace TexasPortal.Contracts.Auth;

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    LoginUserDto User);

public sealed record LoginUserDto(
    long Id,
    string Name,
    string Email,
    string Role,
    string? Avatar);

public sealed record MeResponse(
    long Id,
    string Name,
    string Email,
    string Role,
    string? Avatar);

public sealed record UpdateAvatarRequest(
    string AvatarData);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string UserType = "Salesman");

public sealed record CreateUserResponse(
    long Id,
    string Name,
    string Email,
    string UserType);
