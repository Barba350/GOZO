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
    public static object View(Sale s) => new { s.Id, s.Date, s.CustomerId, customer = s.Customer == null ? "Consumidor final" : $"{s.Customer.Name} · CI {s.Customer.Ci}", s.Payment, s.Subtotal, s.Discount, s.Total, lines = s.Lines.Select(x => new { x.ProductId, x.Name, x.UnitPrice, x.Quantity, x.Discount, x.Total, x.Promotion }) };
    public static DateTime Parse(string? value, DateTime fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) throw new RuleException("Fecha inválida; usa año-mes-día."); return date;
    }
    public static object Report(List<Sale> sales) => new {
        income = sales.Sum(x => x.Total), count = sales.Count, discount = sales.Sum(x => x.Discount),
        payments = new[] { "EFECTIVO", "QR" }.Select(p => new { payment = p, total = sales.Where(x => x.Payment == p).Sum(x => x.Total) }),
        ranking = sales.SelectMany(x => x.Lines).GroupBy(x => x.ProductId).Select(g => new { productId = g.Key, name = g.First().Name, quantity = g.Sum(x => x.Quantity), total = g.Sum(x => x.Total) }).OrderByDescending(x => x.quantity).ThenBy(x => x.name),
        sales = sales.Select(View)
    };
}
