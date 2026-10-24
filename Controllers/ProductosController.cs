using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[Authorize(Policy = "Dueno")]
public class ProductosController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("productos", "Todo lo que hace sonreír.", "CATÁLOGO"));
}
