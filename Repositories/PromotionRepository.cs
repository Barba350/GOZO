using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class PromotionRepository(GozoDb db) : IPromotionRepository
{
    public Task<List<Promotion>> List(bool active, DateTime now) => db.Promotions.AsNoTracking().Include(x => x.Rule).Include(x => x.Products).Include(x => x.Customers).Where(x => !active || (x.Active && x.Start <= now && x.End >= now)).OrderByDescending(x => x.Id).ToListAsync();
    public Task<Promotion?> Find(int id) => db.Promotions.Include(x => x.Rule).Include(x => x.Products).Include(x => x.Customers).FirstOrDefaultAsync(x => x.Id == id);
    public async Task<Dictionary<int, int>> Uses(int customerId) => await db.Set<PromotionUse>().AsNoTracking()
        .Where(x => x.CustomerId == customerId && (x.SaleId != null || (x.Order != null && x.Order.Status != "CANCELADO")))
        .GroupBy(x => x.PromotionId).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
    public void Use(int promotionId, int customerId, int? saleId, int? orderId) => db.Set<PromotionUse>().Add(new() { PromotionId = promotionId, CustomerId = customerId, SaleId = saleId, OrderId = orderId });
    public void Add(Promotion promotion) => db.Promotions.Add(promotion);
}
