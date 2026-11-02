using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class OperationsRepository(GozoDb db, BranchContext branch) {
    public Task<List<Branch>> Branches() => db.Branches.AsNoTracking().Where(x=>x.Active).ToListAsync();
    public Task<bool> BranchExists(int id) => db.Branches.AnyAsync(x=>x.Id==id&&x.Active);
    public Task<Branch?> Branch(int id) => db.Branches.FirstOrDefaultAsync(x=>x.Id==id);
    public Task<bool> ActiveProductExists(int id) => db.Products.AnyAsync(x=>x.Id==id&&x.Active);
    public Task<bool> CustomerExists(int id) => db.Customers.AnyAsync(x=>x.Id==id);
    public Task<List<CashSession>> CashHistory() => db.CashSessions.AsNoTracking().Where(x=>x.BranchId==branch.Id&&x.ClosedAt!=null&&(branch.Owner||x.OpenedById==branch.ActorId)).OrderByDescending(x=>x.Id).Take(50).ToListAsync();
    public Task<Coupon?> CouponById(int id) => db.Coupons.FirstOrDefaultAsync(x=>x.Id==id);
    public void AddCash(CashSession cash) => db.CashSessions.Add(cash);
    public void AddCoupon(Coupon coupon) => db.Coupons.Add(coupon);
    public void AddRedemption(CouponRedemption use) => db.CouponRedemptions.Add(use);
    public void AddCorrection(SaleCorrection change) => db.SaleCorrections.Add(change);
    public void RemoveLine(SaleLine line) => db.Remove(line);
    public void ReplaceOrderLines(GozoOrder order,List<OrderLine> lines) { db.RemoveRange(order.Lines);order.Lines=lines; }
    public Task<GozoOrder?> OrderForSale(int id) => db.Orders.Include(x=>x.Lines).FirstOrDefaultAsync(x=>x.SaleId==id);
    public Task<List<CouponRedemption>> RedemptionsForOrder(int id) => db.CouponRedemptions.Where(x=>x.OrderId==id&&!x.Released).ToListAsync();
    public Task<List<SaleCorrection>> Corrections(int id) => db.SaleCorrections.AsNoTracking().Where(x=>x.SaleId==id).OrderByDescending(x=>x.Id).ToListAsync();
    public async Task<object> Summary(DateTime start,DateTime end) => await db.Sales.AsNoTracking().Where(x=>x.Date>=start&&x.Date<end).GroupBy(x=>x.BranchId).Select(g=>new {branchId=g.Key,total=g.Sum(x=>x.Total),count=g.Count()}).ToListAsync();
    public async Task<object> Coupons() => await db.Coupons.AsNoTracking().OrderByDescending(x=>x.Id).Take(200).Select(x=>new {x.Id,x.Code,x.Description,x.Type,x.Value,x.Start,x.End,x.Active,x.CustomerId,x.GiftProductId,x.GiftQuantity,reservedOrUsed=db.CouponRedemptions.Any(r=>r.CouponId==x.Id&&!r.Released)}).ToListAsync();
    public Task<bool> ValidBranch() => db.Branches.AnyAsync(x => x.Id == branch.Id && x.Active);
    public Task<bool> HasActiveCash() => db.CashSessions.AnyAsync(x=>x.OpenedById==branch.ActorId&&x.ClosedAt==null);
    public Task<CashSession?> Cash(int id) => db.CashSessions.FirstOrDefaultAsync(x=>x.Id==id&&x.BranchId==branch.Id&&(branch.Owner||x.OpenedById==branch.ActorId));
    public Task<List<CashSession>> OpenBoxes() => db.CashSessions.AsNoTracking().Where(x=>x.BranchId==branch.Id&&x.ClosedAt==null&&branch.Owner).ToListAsync();
    public Task<List<CashSession>> ReportClosures(DateTime start,DateTime end) => db.CashSessions.AsNoTracking().Where(x=>(x.OpenedAt>=start&&x.OpenedAt<end||x.ClosedAt>=start&&x.ClosedAt<end)&&(branch.ReportBranchId==null||x.BranchId==branch.ReportBranchId)&&(branch.ReportSellerId==null||x.OpenedById==branch.ReportSellerId)).OrderByDescending(x=>x.Id).Take(300).ToListAsync();
    public async Task<object> Dashboard() {
        var start=Rules.Now().Date.AddDays(-29);var end=Rules.Now().Date.AddDays(1);
        var sales=db.Sales.AsNoTracking().Where(x=>x.BranchId==branch.Id&&x.Date>=start&&x.Date<end);
        var products=await sales.SelectMany(x=>x.Lines).GroupBy(x=>new{x.ProductId,x.Name}).Select(g=>new{productId=g.Key.ProductId,name=g.Key.Name,quantity=g.Sum(x=>x.Quantity)}).OrderByDescending(x=>x.quantity).ThenBy(x=>x.name).Take(5).ToListAsync();
        var customers=await sales.Where(x=>x.CustomerId!=null).GroupBy(x=>new{x.CustomerId,Name=x.Customer!.Name}).Select(g=>new{customerId=g.Key.CustomerId,name=g.Key.Name,purchases=g.Count()}).OrderByDescending(x=>x.purchases).ThenBy(x=>x.name).Take(5).ToListAsync();
        return new{from=start,to=end.AddDays(-1),branchId=branch.Id,products,customers};
    }
    public Task<CashSession?> OpenCash() => db.CashSessions.FirstOrDefaultAsync(x => x.BranchId == branch.Id && x.ClosedAt == null && x.OpenedById == branch.ActorId);
    public Task<List<Sale>> CashSales(int id) => db.Sales.Where(x => x.CashSessionId == id).ToListAsync();
    public Task<Sale?> Sale(int id) => db.Sales.Include(x=>x.Branch).Include(x => x.Lines).Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branch.Id);
    public Task<Coupon?> Coupon(string code) => db.Coupons.FirstOrDefaultAsync(x => x.Code == code);
    public Task<bool> Redeemed(int id) => db.CouponRedemptions.AnyAsync(x => x.CouponId == id && !x.Released);
}
