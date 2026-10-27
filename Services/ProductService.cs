using static GOZO.Rules;
namespace GOZO;
public class ProductService(ICatalogRepository repository, IUnitOfWork unit) : IProductService
{
    public Task<ProductImage?> Image(int id) => repository.Image(id);
    public Task<List<Product>> List(bool all) => repository.List(all);
    public async Task<Product> Create(ProductInput input) {
        await Validate(input, 0);
        var photo = ProductPhoto.Decode(input.ImageBase64);
        if (input.RemoveImage && photo != null) throw new RuleException("Elige subir o quitar la foto, no ambas opciones.");
        var p = new Product { Name = input.Name.Trim(), Key = Key(input.Name), Price = Cents(input.Price), CategoryId = input.CategoryId };
        if (photo != null) { p.Image = new ProductImage { Bytes = photo }; p.ImageVersion = p.Image.Version; }
        repository.Add(p); await unit.Save(); return p;
    }
    public async Task<Product> Update(int id, ProductInput input) {
        var p = await repository.Find(id) ?? throw new RuleException("Producto inexistente."); await Validate(input, id);
        var photo = ProductPhoto.Decode(input.ImageBase64);
        if (input.RemoveImage && photo != null) throw new RuleException("Elige subir o quitar la foto, no ambas opciones.");
        if (input.RemoveImage) p.Image = null;
        if (photo != null) {
            p.Image ??= new ProductImage(); p.Image.Bytes = photo; p.Image.Version = Guid.NewGuid().ToString("N");
        }
        p.ImageVersion = p.Image?.Version;
        p.Name = input.Name.Trim(); p.Key = Key(input.Name); p.Price = Cents(input.Price); p.CategoryId = input.CategoryId;
        await unit.Save(); return p;
    }
    public async Task Delete(int id) {
        var p = await repository.Find(id) ?? throw new RuleException("Producto inexistente."); p.Active = false; await unit.Save();
    }
    private async Task Validate( ProductInput i, int id) {
        Text(i.Name, "Nombre", 100);
        if (i.Price < 0.01m || i.Price > 1000000m || decimal.Round(i.Price, 2) != i.Price) throw new RuleException("Precio entre 0,01 y 1.000.000 Bs, máximo dos decimales.");
        if (!await repository.CategoryExists(i.CategoryId)) throw new RuleException("Selecciona una categoría válida.");
        var key = Key(i.Name);
        if (await repository.Duplicate(key, i.CategoryId, id)) throw new RuleException("Ya existe ese producto en la categoría, incluso si fue desactivado.");
    }
}
