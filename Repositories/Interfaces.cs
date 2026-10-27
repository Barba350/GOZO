namespace GOZO;
public interface ICatalogRepository
{
    Task<List<Product>> List(bool all);
    Task<Product?> Find(int id);
    Task<Product?> FindActive(int id);
    Task<bool> Duplicate(string key, int category, int excluded);
    Task<bool> CategoryExists(int id);
    Task<int> CountActive(List<int> ids);
    Task<List<Category>> Categories();
    void Add(Product product);
}
public interface ICustomerRepository
{
    Task<List<Customer>> List();
    Task<Customer?> Find(int id);
    Task<bool> Exists(int id);
    Task<bool> CiExists(string ci);
    Task<int> Count(List<int> ids);
    void Add(Customer customer);
}
public interface IPromotionRepository
{
    Task<List<Promotion>> List(bool active, DateTime now);
    Task<Promotion?> Find(int id);
    void Add(Promotion promotion);
    Task<Dictionary<int, int>> Uses(int customerId);
    void Use(int promotionId, int customerId, int? saleId, int? orderId);
}
public interface ISaleRepository
{
    Task<Sale?> FindByRequest(string requestId);
    Task<List<Sale>> Range(DateTime start, DateTime end, int? customer);
    void Add(Sale sale);
}
public interface ITransaction : IAsyncDisposable { Task Commit(); }
public interface IUnitOfWork { Task Save(); Task<ITransaction> BeginTransaction(); Task LockCustomer(int? customerId); }
