using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class PromotionRepository(GozoDb db) : IPromotionRepository
{
    public Task<List<Promotion>> List(bool active, DateTime now) => db.Promotions.AsNoTracking().Include(x => x.Products).Include(x => x.Customers).Where(x => !active || (x.Active && x.Start <= now && x.End >= now)).OrderByDescending(x => x.Id).ToListAsync();
    public Task<Promotion?> Find(int id) => db.Promotions.Include(x => x.Products).Include(x => x.Customers).FirstOrDefaultAsync(x => x.Id == id);
    public void Add(Promotion promotion) => db.Promotions.Add(promotion);
}
