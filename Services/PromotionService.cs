using static GOZO.Rules;
namespace GOZO;
public class PromotionService(IPromotionRepository repository, ICustomerRepository customersRepository, IUnitOfWork unit) : IPromotionService
{
    public Task<List<Promotion>> List(bool active) => repository.List(active, Now());
    public async Task<Promotion> Create(PromotionInput input) {
        var p = new Promotion(); await Apply(p, input); repository.Add(p); await unit.Save(); return p;
    }
    public async Task<Promotion> Update(int id, PromotionInput input) {
        var p = await repository.Find(id) ?? throw new RuleException("Promoción inexistente."); await Apply(p, input); await unit.Save(); return p;
    }
    public async Task Delete(int id) {
        var p = await repository.Find(id) ?? throw new RuleException("Promoción inexistente."); p.Active = false; await unit.Save();
    }
    private async Task Apply( Promotion p, PromotionInput i) {
        p.Name = Text(i.Name, "Nombre", 100); p.Description = Text(i.Description, "Descripción", 500);
        if (p.Description.Length > 500) throw new RuleException("Descripción: máximo 500 caracteres.");
        if (i.Type != "PORCENTAJE" && i.Type != "MONTO") throw new RuleException("Tipo de descuento inválido.");
        if (i.Value <= 0 || i.Value > (i.Type == "PORCENTAJE" ? 100 : 1000000) || decimal.Round(i.Value, 2) != i.Value) throw new RuleException("Valor de descuento inválido.");
        if (i.Start == default || i.End == default) throw new RuleException("Las fechas de inicio y fin son obligatorias.");
        if (i.Start > i.End) throw new RuleException("La fecha inicial debe ser anterior a la final.");
        var products = new List<int>(); var customers = (i.CustomerIds ?? new()).Distinct().ToList();
        if (i.MaxPurchasesPerCustomer is < 1 or > 999) throw new RuleException("Límite por cliente: entre 1 y 999 compras.");
        if (await customersRepository.Count(customers) != customers.Count) throw new RuleException("Clientes inválidos.");
        p.Rule ??= new PromotionRule(); p.Rule.MaxPurchasesPerCustomer = i.MaxPurchasesPerCustomer;
        p.Type = i.Type; p.Value = i.Value; p.Start = DateTime.SpecifyKind(i.Start, DateTimeKind.Unspecified); p.End = DateTime.SpecifyKind(i.End, DateTimeKind.Unspecified); p.Active = i.Active;
        // Mantener las entidades existentes evita conflictos de claves al editar.
        p.Products.RemoveAll(x => !products.Contains(x.ProductId));
        foreach (var id in products.Where(id => !p.Products.Any(x => x.ProductId == id))) p.Products.Add(new() { ProductId = id });
        p.Customers.RemoveAll(x => !customers.Contains(x.CustomerId));
        foreach (var id in customers.Where(id => !p.Customers.Any(x => x.CustomerId == id))) p.Customers.Add(new() { CustomerId = id });
    }
}
