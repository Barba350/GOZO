namespace GOZO;
public interface IProductService
{
    Task<List<Product>> List(bool all);
    Task<Product> Create(ProductInput input);
    Task<Product> Update(int id, ProductInput input);
    Task Delete(int id);
    Task<ProductImage?> Image(int id);
}
public interface ICustomerService { Task<List<Customer>> Search(string? search); Task<Customer> Create(CustomerInput input); }
public interface ICategoryService { Task<List<Category>> List(); }
public interface IPromotionService
{
    Task<List<Promotion>> List(bool active);
    Task<Promotion> Create(PromotionInput input);
    Task<Promotion> Update(int id, PromotionInput input);
    Task Delete(int id);
}
public interface ISaleService { Task<Sale> Quote(SaleInput input); Task<object> Create(SaleInput input); }
public interface IReportService
{
    Task<object> Generate(string? from, string? to);
    Task<object> History(int id, string? from, string? to);
    Task<byte[]> Excel(string? from, string? to);
    Task<byte[]> Pdf(string? from, string? to);
}
