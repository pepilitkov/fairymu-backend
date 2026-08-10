namespace FairyMU.Api.Models;

public sealed class AccountRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Username { get; init; }

    public required string Email { get; init; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }

    public int WCoins { get; set; } = 0;

    public int Credits { get; set; } = 0;
}
