using System.ComponentModel.DataAnnotations;

namespace FairyMU.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, RegularExpression("^[A-Za-z0-9_]{4,16}$")]
    public string Username { get; init; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; init; } = "";

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = "";
}

public sealed class LoginRequest
{
    [Required, MaxLength(200)]
    public string UsernameOrEmail { get; init; } = "";

    [Required, MinLength(1), MaxLength(128)]
    public string Password { get; init; } = "";
}
