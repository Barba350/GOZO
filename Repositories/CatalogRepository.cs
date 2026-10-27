using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class CatalogRepository(GozoDb db) : ICatalogRepository
{
    public Task<List<Product>> List(bool all) => db.Products.AsNoTracking().Where(x => all || x.Active).OrderBy(x => x.Name).Select(x => new Product { Id = x.Id, Name = x.Name, Key = x.Key, Price = x.Price, CategoryId = x.CategoryId, Active = x.Active, ImageVersion = x.Image == null ? null : x.Image.Version }).ToListAsync();
    public Task<Product?> Find(int id) => db.Products.Include(x => x.Image).FirstOrDefaultAsync(x => x.Id == id);
    public Task<Product?> FindActive(int id) => db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Active);
    public Task<bool> Duplicate(string key, int category, int excluded) => db.Products.AnyAsync(x => x.Key == key && x.CategoryId == category && x.Id != excluded);
    public Task<bool> CategoryExists(int id) => db.Categories.AnyAsync(x => x.Id == id);
    public Task<int> CountActive(List<int> ids) => db.Products.CountAsync(x => ids.Contains(x.Id) && x.Active);
    public Task<List<Category>> Categories() => db.Categories.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    public Task<ProductImage?> Image(int id) => db.Set<ProductImage>().AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id && db.Products.Any(p => p.Id == id && p.Active));
    public void Add(Product product) => db.Products.Add(product);
}
