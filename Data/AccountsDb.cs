using Microsoft.EntityFrameworkCore;
namespace GOZO;
// Base separada: permite actualizar una instalación existente sin alterar las ventas.
public class AccountsDb(DbContextOptions<AccountsDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<SecurityEvent> Events => Set<SecurityEvent>();
    protected override void OnModelCreating(ModelBuilder b) {
        b.Entity<Account>().HasIndex(x => x.Key).IsUnique();
        b.Entity<SecurityEvent>().HasIndex(x => x.Date);
    }
}
