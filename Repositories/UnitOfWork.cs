using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
namespace GOZO;
// Una misma instancia scoped de GozoDb coordina todos los repositorios por solicitud.
public class UnitOfWork(GozoDb db) : IUnitOfWork
{
    public async Task LockCustomer(int? customerId) {
        // SQLite ya serializa escritores. PostgreSQL bloquea por cliente dentro de la transacción.
        if (customerId.HasValue && db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(72401684, {customerId.Value})");
    }
    public async Task Save() { await db.SaveChangesAsync(); }
    public async Task<ITransaction> BeginTransaction() => new DatabaseTransaction(await db.Database.BeginTransactionAsync());
    private sealed class DatabaseTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task Commit() => transaction.CommitAsync();
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
