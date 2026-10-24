using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace GOZO;
public interface IAccountRepository
{
    Task<Account?> Find(string key);
    Task<Account?> Find(int id);
    Task<List<Account>> List();
    Task<List<SecurityEvent>> Events();
    Task<bool> Any();
    Task<int> ActiveOwners();
    Task<ITransaction> Transaction();
    void Add(Account account);
    void Audit(string actor, string action, string address);
    Task Save();
}
public class AccountRepository(AccountsDb db) : IAccountRepository
{
    public Task<Account?> Find(string key) => db.Accounts.FirstOrDefaultAsync(x => x.Key == key);
    public Task<Account?> Find(int id) => db.Accounts.FirstOrDefaultAsync(x => x.Id == id);
    public Task<List<Account>> List() => db.Accounts.AsNoTracking().OrderBy(x => x.Username).ToListAsync();
    public Task<List<SecurityEvent>> Events() => db.Events.AsNoTracking().OrderByDescending(x => x.Id).Take(200).ToListAsync();
    public Task<bool> Any() => db.Accounts.AnyAsync();
    public Task<int> ActiveOwners() => db.Accounts.CountAsync(x => x.Active && x.Role == "Dueno");
    public async Task<ITransaction> Transaction() => new AccountTransaction(await db.Database.BeginTransactionAsync());
    private sealed class AccountTransaction(IDbContextTransaction transaction) : ITransaction {
        public Task Commit() => transaction.CommitAsync();
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
    public void Add(Account account) => db.Accounts.Add(account);
    public void Audit(string actor, string action, string address) => db.Events.Add(new SecurityEvent { Actor = actor[..Math.Min(actor.Length,40)], Action = action[..Math.Min(action.Length,200)], Address = address[..Math.Min(address.Length,64)] });
    public async Task Save() { await db.SaveChangesAsync(); }
}
