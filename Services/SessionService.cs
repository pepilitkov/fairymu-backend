using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace FairyMU.Api.Services;

public sealed class SessionService
{
    private sealed record Session(Guid AccountId, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public string Create(Guid accountId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = new Session(accountId, DateTimeOffset.UtcNow.Add(SessionLifetime));
        return token;
    }

    public Guid? Resolve(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorization["Bearer ".Length..].Trim();
        if (!_sessions.TryGetValue(token, out var session))
            return null;

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return session.AccountId;
    }

    public void Revoke(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return;

        var token = authorization["Bearer ".Length..].Trim();
        _sessions.TryRemove(token, out _);
    }
}
