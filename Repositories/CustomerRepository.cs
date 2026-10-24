using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class CustomerRepository(GozoDb db) : ICustomerRepository
{
    public Task<List<Customer>> List() => db.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    public Task<Customer?> Find(int id) => db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    public Task<bool> Exists(int id) => db.Customers.AnyAsync(x => x.Id == id);
    public Task<bool> CiExists(string ci) => db.Customers.AnyAsync(x => x.Ci == ci);
    public Task<int> Count(List<int> ids) => db.Customers.CountAsync(x => ids.Contains(x.Id));
    public void Add(Customer customer) => db.Customers.Add(customer);
}
