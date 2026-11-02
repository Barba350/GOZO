namespace GOZO;
public class Branch { public int Id { get; set; } public string Name { get; set; } = ""; public bool Active { get; set; } = true; }
public class CashSession {
    public int Id { get; set; } public int BranchId { get; set; }
    public int OpenedById { get; set; }
    public long? CountedQr { get; set; } public long? QrDifference { get; set; }
    public DateTime OpenedAt { get; set; } public DateTime? ClosedAt { get; set; }
    public string OpenedBy { get; set; } = ""; public string ClosedBy { get; set; } = "";
    public long OpeningAmount { get; set; } public long? CountedAmount { get; set; }
    public long? ExpectedAmount { get; set; } public long? Difference { get; set; } public long? QrTotal { get; set; }
    public string OpeningDenominations { get; set; } = ""; public string ClosingDenominations { get; set; } = "";
    public string Notes { get; set; } = ""; public string Revision { get; set; } = Guid.NewGuid().ToString();
}
public class Coupon {
    public int? GiftProductId { get; set; } public int GiftQuantity { get; set; } = 1;
    public int Id { get; set; } public string Code { get; set; } = ""; public string Description { get; set; } = "";
    public string Type { get; set; } = "MONTO"; public decimal Value { get; set; }
    public DateTime Start { get; set; } public DateTime End { get; set; } public bool Active { get; set; } = true;
    public int? CustomerId { get; set; } public string Revision { get; set; } = Guid.NewGuid().ToString();
}
public class CouponRedemption {
    public int Id { get; set; } public int CouponId { get; set; } public int? SaleId { get; set; } public int? OrderId { get; set; }
    public bool Released { get; set; } public DateTime CreatedAt { get; set; }
}
public class SaleCorrection {
    public int Id { get; set; } public int SaleId { get; set; } public DateTime CreatedUtc { get; set; }
    public string Actor { get; set; } = ""; public string Reason { get; set; } = "";
    public string BeforeJson { get; set; } = ""; public string AfterJson { get; set; } = "";
}
public record Denomination(int Cents, int Count);
public record OpenCashInput(decimal OpeningAmount, List<Denomination>? Denominations = null);
public record CloseCashInput(string Revision, decimal CountedCash, decimal CountedQr, string? Notes, List<Denomination>? Denominations = null);
public record CouponInput(string Code, string Description, string Type, decimal Value, DateTime Start, DateTime End, int? CustomerId, int? GiftProductId = null, int GiftQuantity = 1);
public record CorrectionItem(int ProductId, int Quantity);
public record CorrectSaleInput(string Revision, string Payment, List<CorrectionItem> Items, string Reason);
