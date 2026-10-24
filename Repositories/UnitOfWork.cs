using Microsoft.EntityFrameworkCore.Storage;
namespace GOZO;
// Una misma instancia scoped de GozoDb coordina todos los repositorios por solicitud.
public class UnitOfWork(GozoDb db) : IUnitOfWork
{
    public async Task Save() { await db.SaveChangesAsync(); }
    public async Task<ITransaction> BeginTransaction() => new DatabaseTransaction(await db.Database.BeginTransactionAsync());
    private sealed class DatabaseTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task Commit() => transaction.CommitAsync();
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
