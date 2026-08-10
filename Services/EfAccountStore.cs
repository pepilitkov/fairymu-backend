using FairyMU.Api.Models;
using FairyMU.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FairyMU.Api.Services;

public sealed class EfAccountStore(FairyMuDbContext db) : IAccountStore
{
    public async Task<bool> TryCreateAsync(AccountRecord account, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = account.Username.ToLower();
        var normalizedEmail = account.Email.ToLower();

        var exists = await db.Accounts.AnyAsync(
            x => x.Username.ToLower() == normalizedUsername || x.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (exists)
            return false;

        db.Accounts.Add(account);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(account).State = EntityState.Detached;
            return false;
        }
    }

    public Task<AccountRecord?> FindByUsernameOrEmailAsync(string value, CancellationToken cancellationToken = default)
    {
        var normalized = value.ToLower();

        return db.Accounts.FirstOrDefaultAsync(
            x => x.Username.ToLower() == normalized || x.Email.ToLower() == normalized,
            cancellationToken);
    }

    public Task<AccountRecord?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
