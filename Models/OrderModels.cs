namespace GOZO;
public class GozoOrder
{
    public int Id { get; set; }
    public string RequestId { get; set; } = "";
    public string SaleRequestId { get; set; } = Guid.NewGuid().ToString();
    public string Revision { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = "PENDIENTE";
    public string ContactName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string Notes { get; set; } = "";
    public int? CustomerId { get; set; }
    public long Subtotal { get; set; }
    public long Discount { get; set; }
    public long Total { get; set; }
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}
public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long Discount { get; set; }
    public long Total { get; set; }
    public string Promotion { get; set; } = "";
}
public record OrderInput(string ContactName, string ContactPhone, string? Notes, int? CustomerId, List<CartItem> Items, string RequestId, long? ExpectedTotal = null);
public record DeliverOrderInput(string Payment);
public record OrderLineView(int ProductId, string Name, long UnitPrice, int Quantity, long Discount, long Total, string Promotion);
public record OrderView(int Id, string Status, DateTime CreatedAt, DateTime? ClosedAt, string ContactName, string ContactPhone, string Notes, int? CustomerId, long Subtotal, long Discount, long Total, int? SaleId, string? Payment, List<OrderLineView> Lines);
