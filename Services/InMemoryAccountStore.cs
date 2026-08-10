using System.Collections.Concurrent;
using FairyMU.Api.Models;

namespace FairyMU.Api.Services;

public sealed class InMemoryAccountStore
{
    private readonly ConcurrentDictionary<Guid, AccountRecord> _byId = new();

    private readonly ConcurrentDictionary<string, Guid> _usernameIndex =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, Guid> _emailIndex =
        new(StringComparer.OrdinalIgnoreCase);

    public bool TryCreate(AccountRecord account)
    {
        if (!_usernameIndex.TryAdd(account.Username, account.Id))
            return false;

        if (!_emailIndex.TryAdd(account.Email, account.Id))
        {
            _usernameIndex.TryRemove(account.Username, out _);
            return false;
        }

        if (!_byId.TryAdd(account.Id, account))
        {
            _usernameIndex.TryRemove(account.Username, out _);
            _emailIndex.TryRemove(account.Email, out _);
            return false;
        }

        return true;
    }

    public AccountRecord? FindByUsernameOrEmail(string value)
    {
        if (_usernameIndex.TryGetValue(value, out var usernameId) &&
            _byId.TryGetValue(usernameId, out var byUsername))
        {
            return byUsername;
        }

        if (_emailIndex.TryGetValue(value, out var emailId) &&
            _byId.TryGetValue(emailId, out var byEmail))
        {
            return byEmail;
        }

        return null;
    }

    public AccountRecord? FindById(Guid id)
    {
        return _byId.TryGetValue(id, out var account)
            ? account
            : null;
    }
}
