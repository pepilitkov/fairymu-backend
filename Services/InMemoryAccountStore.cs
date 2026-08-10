using System.Collections.Concurrent;
using FairyMU.Api.Models;

namespace FairyMU.Api.Services;

public sealed class InMemoryAccountStore : IAccountStore
{
    private readonly ConcurrentDictionary<Guid, AccountRecord> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _usernameIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Guid> _emailIndex = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> TryCreateAsync(AccountRecord account, CancellationToken cancellationToken = default)
    {
        if (!_usernameIndex.TryAdd(account.Username, account.Id))
            return Task.FromResult(false);

        if (!_emailIndex.TryAdd(account.Email, account.Id))
        {
            _usernameIndex.TryRemove(account.Username, out _);
            return Task.FromResult(false);
        }

        if (!_byId.TryAdd(account.Id, account))
        {
            _usernameIndex.TryRemove(account.Username, out _);
            _emailIndex.TryRemove(account.Email, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<AccountRecord?> FindByUsernameOrEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        if (_usernameIndex.TryGetValue(value, out var usernameId) &&
            _byId.TryGetValue(usernameId, out var byUsername))
            return Task.FromResult<AccountRecord?>(byUsername);

        if (_emailIndex.TryGetValue(value, out var emailId) &&
            _byId.TryGetValue(emailId, out var byEmail))
            return Task.FromResult<AccountRecord?>(byEmail);

        return Task.FromResult<AccountRecord?>(null);
    }

    public Task<AccountRecord?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_byId.TryGetValue(id, out var account) ? account : null);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
