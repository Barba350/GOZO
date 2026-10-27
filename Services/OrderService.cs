using System.Text.RegularExpressions;
using static GOZO.Rules;
namespace GOZO;
public interface IOrderService
{
    Task<List<OrderView>> List(string? status);
    Task<OrderView> Create(OrderInput input);
    Task<OrderView> Deliver(int id, DeliverOrderInput input);
    Task<OrderView> Cancel(int id);
}
public class OrderService(IOrderRepository orders, ISaleService pricing, ISaleRepository sales, IUnitOfWork unit, IPromotionRepository promotions) : IOrderService
{
    private static OrderView View(GozoOrder o) => new(o.Id, o.Status, o.CreatedAt, o.ClosedAt, o.ContactName, o.ContactPhone, o.Notes, o.CustomerId, o.Subtotal, o.Discount, o.Total, o.SaleId, o.Sale?.Payment, o.Lines.OrderBy(x => x.Id).Select(x => new OrderLineView(x.ProductId, x.Name, x.UnitPrice, x.Quantity, x.Discount, x.Total, x.Promotion)).ToList());
    public async Task<List<OrderView>> List(string? status) {
        status = string.IsNullOrWhiteSpace(status) ? null : status.ToUpperInvariant();
        if (status != null && status is not ("PENDIENTE" or "ENTREGADO" or "CANCELADO")) throw new RuleException("Estado de pedido inválido.");
        return (await orders.List(status)).Select(View).ToList();
    }
    public async Task<OrderView> Create(OrderInput input) {
        if (!Guid.TryParse(input.RequestId, out var request)) throw new RuleException("Falta identificador del pedido. Actualiza la página.");
        var requestId = request.ToString();
        await using var tx = await unit.BeginTransaction();
        var existing = await orders.FindByRequest(requestId);
        if (existing != null) return View(existing);
        var name = Text(input.ContactName, "Nombre de quien recibe", 120);
        if (input.ContactPhone == null || !Regex.IsMatch(input.ContactPhone, @"^[0-9]{8}$")) throw new RuleException("Escribe un celular de 8 dígitos.");
        var notes = (input.Notes ?? "").Trim();
        if (notes.Length > 1000) throw new RuleException("Las notas admiten hasta 1000 caracteres.");
        await unit.LockCustomer(input.CustomerId);
        var quote = await pricing.Quote(new(input.CustomerId, "", input.Items, null));
        if (input.ExpectedTotal != null && input.ExpectedTotal != quote.Total) throw new RuleException("El precio cambió. Actualiza el resumen antes de guardar el pedido.");
        var order = new GozoOrder { RequestId = requestId, CreatedAt = Now(), ContactName = name, ContactPhone = input.ContactPhone, Notes = notes, CustomerId = input.CustomerId, Subtotal = quote.Subtotal, Discount = quote.Discount, Total = quote.Total,
            Lines = quote.Lines.Select(x => new OrderLine { ProductId = x.ProductId, Name = x.Name, UnitPrice = x.UnitPrice, Quantity = x.Quantity, Discount = x.Discount, Total = x.Total, Promotion = x.Promotion }).ToList() };
        orders.Add(order); await unit.Save();
        if (order.CustomerId.HasValue) {
            foreach (var promotionId in quote.AppliedPromotionIds.Distinct())
                promotions.Use(promotionId, order.CustomerId.Value, null, order.Id);
            await unit.Save();
        }
        await tx.Commit(); return View(order);
    }
    public async Task<OrderView> Deliver(int id, DeliverOrderInput input) {
        await using var tx = await unit.BeginTransaction();
        var order = await orders.Find(id) ?? throw new RuleException("Pedido inexistente.");
        if (order.Status == "ENTREGADO") return View(order);
        if (order.Status != "PENDIENTE") throw new RuleException("Un pedido cancelado no puede entregarse.");
        if (input.Payment is not ("EFECTIVO" or "QR")) throw new RuleException("Selecciona Efectivo o QR al entregar.");
        await unit.LockCustomer(order.CustomerId);
        // La revisión detecta entregas/cancelaciones concurrentes. Todo se confirma en una transacción.
        order.Status = "ENTREGADO"; order.ClosedAt = Now(); order.Revision = Guid.NewGuid().ToString();
        await unit.Save();
        var sale = new Sale { Date = order.ClosedAt.Value, RequestId = order.SaleRequestId, CustomerId = order.CustomerId, Payment = input.Payment, Subtotal = order.Subtotal, Discount = order.Discount, Total = order.Total,
            Lines = order.Lines.Select(x => new SaleLine { ProductId = x.ProductId, Name = x.Name, UnitPrice = x.UnitPrice, Quantity = x.Quantity, Discount = x.Discount, Total = x.Total, Promotion = x.Promotion }).ToList() };
        sales.Add(sale); order.Sale = sale;
        await unit.Save(); await tx.Commit(); return View(order);
    }
    public async Task<OrderView> Cancel(int id) {
        await using var tx = await unit.BeginTransaction();
        var order = await orders.Find(id) ?? throw new RuleException("Pedido inexistente.");
        if (order.Status == "CANCELADO") return View(order);
        if (order.Status != "PENDIENTE") throw new RuleException("Un pedido entregado ya tiene una venta y no puede cancelarse aquí.");
        await unit.LockCustomer(order.CustomerId);
        order.Status = "CANCELADO"; order.ClosedAt = Now(); order.Revision = Guid.NewGuid().ToString();
        await unit.Save(); await tx.Commit(); return View(order);
    }
}
