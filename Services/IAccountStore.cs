using FairyMU.Api.Models;

namespace FairyMU.Api.Services;

public interface IAccountStore
{
    Task<bool> TryCreateAsync(AccountRecord account, CancellationToken cancellationToken = default);
    Task<AccountRecord?> FindByUsernameOrEmailAsync(string value, CancellationToken cancellationToken = default);
    Task<AccountRecord?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
