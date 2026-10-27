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
        await unit.LockCustomer(input.CustomerId);
        var sale = await Quote(input);
        if (input.ExpectedTotal != null && input.ExpectedTotal != sale.Total) throw new RuleException("El precio o descuento cambió. Cierra esta confirmación y actualiza el carrito antes de cobrar.");
        sale.RequestId = input.RequestId!;
        repository.Add(sale); await unit.Save();
        if (sale.CustomerId.HasValue) {
            foreach (var promotionId in sale.AppliedPromotionIds.Distinct())
                promotions.Use(promotionId, sale.CustomerId.Value, sale.Id, null);
            await unit.Save();
        }
        await tx.Commit();
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
            sale.Lines.Add(new SaleLine { ProductId = p.Id, Name = p.Name, UnitPrice = p.Price, Quantity = qty, Total = p.Price * qty });
        }
        sale.Subtotal = sale.Lines.Sum(x => x.UnitPrice * x.Quantity);
        // Descuento por unidad en los productos elegibles; cuota por compra y cliente con CI.
        if (i.CustomerId.HasValue) {
            var uses = await promotions.Uses(i.CustomerId.Value);
            var customerPromotions = activePromotions.Where(x => x.Customers.Count == 0 || x.Customers.Any(z => z.CustomerId == i.CustomerId)).ToList();
            foreach (var line in sale.Lines) {
                Promotion? best = null; long discount = 0;
                foreach (var promo in customerPromotions.Where(x => (x.Products.Count == 0 || x.Products.Any(z => z.ProductId == line.ProductId)) && uses.GetValueOrDefault(x.Id) < x.MaxPurchasesPerCustomer)) {
                    var perUnit = promo.Type == "PORCENTAJE" ? (long)decimal.Round(line.UnitPrice * promo.Value / 100m, 0, MidpointRounding.AwayFromZero) : Cents(promo.Value);
                    var candidate = Math.Min(perUnit, line.UnitPrice) * line.Quantity;
                    if (candidate > discount) { discount = candidate; best = promo; }
                }
                if (best != null && discount > 0) {
                    line.Discount = discount; line.Total -= discount; line.Promotion = best.Name;
                    if (!sale.AppliedPromotionIds.Contains(best.Id)) sale.AppliedPromotionIds.Add(best.Id);
                }
            }
            if (sale.AppliedPromotionIds.Count > 0)
                sale.PromotionNotice = "Descuento por unidad. Cada promoción utilizada consume una compra de su límite por cliente, aunque incluya varias unidades.";
            else if (customerPromotions.Any(x => (x.Products.Count == 0 || x.Products.Any(z => sale.Lines.Any(l => l.ProductId == z.ProductId))) && uses.GetValueOrDefault(x.Id) >= x.MaxPurchasesPerCustomer))
                sale.PromotionNotice = "Este cliente ya alcanzó el límite de compras con descuento, o tiene usos reservados en pedidos pendientes.";
        }
        sale.Discount = sale.Lines.Sum(x => x.Discount); sale.Total = sale.Subtotal - sale.Discount; return sale;
    }
}
