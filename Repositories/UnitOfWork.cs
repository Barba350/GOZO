using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
namespace GOZO;
// Una misma instancia scoped de GozoDb coordina todos los repositorios por solicitud.
public class UnitOfWork(GozoDb db, BranchContext branch) : IUnitOfWork
{
    public async Task LockCustomer(int? customerId) {
        if(!customerId.HasValue)return;
        if(db.Database.IsNpgsql())await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(72401684, {customerId.Value})");
        else await Lock("customer:"+customerId.Value);
    }
    public async Task Lock(string resource) {
        if(db.Database.IsNpgsql())await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"GOZO:"+resource},0))");
        else await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sp_getapplock @Resource={"GOZO:"+resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51001, 'No se pudo bloquear la operación', 1;");
    }
    public async Task Save() { await db.SaveChangesAsync(); }
    public async Task<ITransaction> BeginTransaction() {
        var tx=await db.Database.BeginTransactionAsync();
        try { await Lock("branch:"+branch.Id);return new DatabaseTransaction(tx); }
        catch {await tx.DisposeAsync();throw;}
    }
    private sealed class DatabaseTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task Commit() => transaction.CommitAsync();
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
