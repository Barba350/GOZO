using static GOZO.Rules;
namespace GOZO;
public class SaleService(ISaleRepository repository, ICatalogRepository catalog, ICustomerRepository customers, IPromotionRepository promotions, IUnitOfWork unit) : ISaleService
{
    public async Task<object> Create(SaleInput input) {
        if (!Guid.TryParse(input.RequestId, out _)) throw new RuleException("Falta identificador de operación. Actualiza la página.");
        await using var tx = await unit.BeginTransaction();
        var existing = await repository.FindByRequest(input.RequestId!);
        if (existing != null) return View(existing);
        if (input.Payment != "EFECTIVO" && input.Payment != "QR") throw new RuleException("Selecciona Efectivo o QR.");
        var sale = await Quote(input);
        if (input.ExpectedTotal != null && input.ExpectedTotal != sale.Total) throw new RuleException("El precio o descuento cambió. Cierra esta confirmación y actualiza el carrito antes de cobrar.");
        sale.RequestId = input.RequestId!;
        repository.Add(sale); await unit.Save(); await tx.Commit();
        if (sale.CustomerId.HasValue) sale.Customer = await customers.Find(sale.CustomerId.Value);
        return View(sale);
    }
    public async Task<Sale> Quote( SaleInput i) {
        if (i.Items == null || i.Items.Count == 0 || i.Items.Count > 200) throw new RuleException("Agrega entre 1 y 200 productos.");
        if (i.CustomerId != null && !await customers.Exists(i.CustomerId.Value)) throw new RuleException("Cliente inexistente.");
        if (i.Items.Any(x => x.Quantity < 1 || x.Quantity > 999)) throw new RuleException("Cantidad entre 1 y 999.");
        var now = Now(); var activePromotions = await promotions.List(true, now);
        var sale = new Sale { Date = now, CustomerId = i.CustomerId, Payment = i.Payment ?? "" };
        foreach (var group in i.Items.GroupBy(x => x.ProductId)) {
            var qty = group.Sum(x => x.Quantity); if (qty > 999) throw new RuleException("Máximo 999 unidades por producto.");
            var p = await catalog.FindActive(group.Key) ?? throw new RuleException("Un producto ya no está disponible. Actualiza el catálogo.");
            long best = 0; string name = "";
            foreach (var promo in activePromotions.Where(x => x.Products.Any(z => z.ProductId == p.Id) && (x.Customers.Count == 0 || x.Customers.Any(z => z.CustomerId == i.CustomerId)))) {
                // MONTO es descuento por unidad. Se usa solo la mejor promoción, sin acumular.
                long off = promo.Type == "PORCENTAJE" ? (long)decimal.Round(p.Price * qty * promo.Value / 100m, 0, MidpointRounding.AwayFromZero) : Cents(promo.Value) * qty;
                off = Math.Min(off, p.Price * qty);
                if (off > best) { best = off; name = promo.Name; }
            }
            sale.Lines.Add(new SaleLine { ProductId = p.Id, Name = p.Name, UnitPrice = p.Price, Quantity = qty, Discount = best, Total = p.Price * qty - best, Promotion = name });
        }
        sale.Subtotal = sale.Lines.Sum(x => x.UnitPrice * x.Quantity); sale.Discount = sale.Lines.Sum(x => x.Discount); sale.Total = sale.Subtotal - sale.Discount; return sale;
    }
}
