using FairyMU.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FairyMU.Api.Persistence;

public sealed class FairyMuDbContext(DbContextOptions<FairyMuDbContext> options) : DbContext(options)
{
    public DbSet<AccountRecord> Accounts => Set<AccountRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var accounts = modelBuilder.Entity<AccountRecord>();

        accounts.ToTable("portal_accounts", "fairymu_portal");
        accounts.HasKey(x => x.Id);

        accounts.Property(x => x.Username).HasMaxLength(16).IsRequired();
        accounts.Property(x => x.Email).HasMaxLength(200).IsRequired();
        accounts.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

        accounts.HasIndex(x => x.Username).IsUnique();
        accounts.HasIndex(x => x.Email).IsUnique();
    }
}
