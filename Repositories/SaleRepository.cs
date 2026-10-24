using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class SaleRepository(GozoDb db) : ISaleRepository
{
    public Task<Sale?> FindByRequest(string requestId) => db.Sales.AsNoTracking().Include(x => x.Customer).Include(x => x.Lines).FirstOrDefaultAsync(x => x.RequestId == requestId);
    public Task<List<Sale>> Range(DateTime start, DateTime end, int? customer) => db.Sales.AsNoTracking().Include(x => x.Customer).Include(x => x.Lines).Where(x => x.Date >= start && x.Date < end && (customer == null || x.CustomerId == customer)).OrderByDescending(x => x.Date).ToListAsync();
    public void Add(Sale sale) => db.Sales.Add(sale);
}
