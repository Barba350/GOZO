using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
namespace GOZO;
// Base separada: permite actualizar una instalación existente sin alterar las ventas.
public class AccountsDb(DbContextOptions<AccountsDb> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<SecurityEvent> Events => Set<SecurityEvent>();
    protected override void OnModelCreating(ModelBuilder b) {
        if (Database.IsNpgsql()) b.HasDefaultSchema("gozo_accounts");
        else b.Ignore<DataProtectionKey>();
        b.Entity<Account>().HasIndex(x => x.Key).IsUnique();
        b.Entity<SecurityEvent>().HasIndex(x => x.Date);
    }
}
