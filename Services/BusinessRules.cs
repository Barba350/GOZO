using System.Globalization;
using System.Text.RegularExpressions;
namespace GOZO;

public static class Rules
{
    public static string Key(string value) => Regex.Replace(value.Trim(), @"\s+", " ").ToUpperInvariant();
    public static string Ci(string value) => Regex.Replace(value.ToUpperInvariant(), @"[\s.]", "");
    public static DateTime Now() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-4), DateTimeKind.Unspecified);
    public static long Cents(decimal value) => checked((long)decimal.Round(value * 100, 0, MidpointRounding.AwayFromZero));
    public static string Text(string? value, string label, int length) {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > length) throw new RuleException($"{label}: campo obligatorio, máximo {length} caracteres."); return value.Trim();
    }
    public static object View(Sale s) => new { s.Id, s.BranchId, s.CashSessionId, s.Revision, s.CouponCode, s.CreatedById, seller=string.IsNullOrWhiteSpace(s.CreatedBy)?"Sin registro (histórica)":s.CreatedBy, branch=s.Branch?.Name??"Sin registro (histórica)", s.Date, s.CustomerId, customer = s.Customer == null ? "Consumidor final" : $"{s.Customer.Name} · CI {s.Customer.Ci}", s.Payment, s.Subtotal, s.Discount, s.Total, lines = s.Lines.Select(x => new { x.ProductId, x.Name, x.UnitPrice, x.Quantity, x.Discount, x.Total, x.Promotion }) };
    public static DateTime Parse(string? value, DateTime fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw new RuleException("Fecha inválida; usa año-mes-día."); return date;
    }
    public static object Report(List<Sale> sales, List<CashSession>? boxes = null) => new {
        income = sales.Sum(x => x.Total), count = sales.Count, discount = sales.Sum(x => x.Discount),
        payments = new[] { "EFECTIVO", "QR", "CANJE" }.Select(p => new { payment = p, total = sales.Where(x => x.Payment == p).Sum(x => x.Total) }),
        ranking = sales.SelectMany(x => x.Lines).GroupBy(x => x.ProductId).Select(g => new { productId = g.Key, name = g.First().Name, quantity = g.Sum(x => x.Quantity), total = g.Sum(x => x.Total) }).OrderByDescending(x => x.quantity).ThenBy(x => x.name),
        bySeller=sales.GroupBy(x=>new{x.CreatedById,x.CreatedBy}).Select(g=>new{id=g.Key.CreatedById,name=string.IsNullOrWhiteSpace(g.Key.CreatedBy)?"Sin registro (histórica)":g.Key.CreatedBy,count=g.Count(),cash=g.Where(x=>x.Payment=="EFECTIVO").Sum(x=>x.Total),qr=g.Where(x=>x.Payment=="QR").Sum(x=>x.Total),total=g.Sum(x=>x.Total)}).OrderByDescending(x=>x.total),
        byBranch=sales.GroupBy(x=>new{x.BranchId,Name=x.Branch?.Name}).Select(g=>new{id=g.Key.BranchId,name=g.Key.Name??"Sin registro (histórica)",count=g.Count(),total=g.Sum(x=>x.Total)}).OrderByDescending(x=>x.total),
        boxes=boxes??new List<CashSession>(),
        sales = sales.Select(View)
    };
}
