using Microsoft.EntityFrameworkCore;
namespace GOZO;
public interface IOrderRepository
{
    Task<List<GozoOrder>> List(string? status);
    Task<GozoOrder?> Find(int id);
    Task<GozoOrder?> FindByRequest(string requestId);
    void Add(GozoOrder order);
}
public class OrderRepository(GozoDb db) : IOrderRepository
{
    public Task<List<GozoOrder>> List(string? status) => db.Orders.AsNoTracking().Include(x => x.Lines).Include(x => x.Sale).Where(x => status == null || x.Status == status).OrderByDescending(x => x.Id).Take(200).ToListAsync();
    public Task<GozoOrder?> Find(int id) => db.Orders.Include(x => x.Lines).Include(x => x.Sale).FirstOrDefaultAsync(x => x.Id == id);
    public Task<GozoOrder?> FindByRequest(string requestId) => db.Orders.Include(x => x.Lines).Include(x => x.Sale).FirstOrDefaultAsync(x => x.RequestId == requestId);
    public void Add(GozoOrder order) => db.Orders.Add(order);
}
