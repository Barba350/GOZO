namespace GOZO;
public class CategoryService(ICatalogRepository repository) : ICategoryService
{
    public Task<List<Category>> List() => repository.Categories();
}
