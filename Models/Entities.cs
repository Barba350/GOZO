namespace GOZO;

public class Category { public int Id { get; set; } public string Name { get; set; } = ""; }
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Key { get; set; } = "";
    public long Price { get; set; } // Centavos: evita errores de coma flotante.
    public int CategoryId { get; set; }
    public bool Active { get; set; } = true;
}
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Ci { get; set; } = "";
    public string Contact { get; set; } = "";
}
public class Sale
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string Payment { get; set; } = "";
    public string RequestId { get; set; } = "";
    public long Subtotal { get; set; }
    public long Discount { get; set; }
    public long Total { get; set; }
    public List<SaleLine> Lines { get; set; } = new();
}
public class SaleLine
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = ""; // Copia histórica.
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long Discount { get; set; }
    public long Total { get; set; }
    public string Promotion { get; set; } = "";
}
public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "PORCENTAJE";
    public decimal Value { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool Active { get; set; } = true;
    public List<PromotionProduct> Products { get; set; } = new();
    public List<PromotionCustomer> Customers { get; set; } = new();
}
public class PromotionProduct { public int PromotionId { get; set; } public int ProductId { get; set; } public Product? Product { get; set; } }
public class PromotionCustomer { public int PromotionId { get; set; } public int CustomerId { get; set; } public Customer? Customer { get; set; } }
