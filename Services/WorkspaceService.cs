namespace GOZO;
public interface IWorkspaceService { Task<WorkspaceViewModel> Build(string page, string title, string eyebrow); }
public class WorkspaceService(IProductService products, ICustomerService customers, ICategoryService categories, IPromotionService promotions) : IWorkspaceService
{
    public async Task<WorkspaceViewModel> Build(string page, string title, string eyebrow) {
        // Secuencial: EF no permite consultas simultáneas sobre el mismo DbContext.
        var catalog = await products.List(false);
        var clients = await customers.Search(null);
        var groups = await categories.List();
        var offers = await promotions.List(false);
        return new(page, title, eyebrow, catalog, clients, groups, offers);
    }
}
