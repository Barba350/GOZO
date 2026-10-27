namespace GOZO;
public class PromotionRule
{
    public int PromotionId { get; set; }
    public int MaxPurchasesPerCustomer { get; set; } = 1;
}
public class PromotionUse
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public int CustomerId { get; set; }
    public int? SaleId { get; set; }
    public int? OrderId { get; set; }
    public GozoOrder? Order { get; set; }
}
