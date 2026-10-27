namespace GOZO;

public record ProductInput(string Name, decimal Price, int CategoryId);
public record CustomerInput(string Name, string Ci, string Contact);
public record CartItem(int ProductId, int Quantity);
public record SaleInput(int? CustomerId, string Payment, List<CartItem> Items, string? RequestId, long? ExpectedTotal = null);
public record PromotionInput(string Name, string Description, string Type, decimal Value, DateTime Start, DateTime End, bool Active, List<int> ProductIds, List<int> CustomerIds, int MaxPurchasesPerCustomer = 1);
public class RuleException(string message) : Exception(message) { }
