using Microsoft.AspNetCore.Mvc;
namespace GOZO;
public class VentasController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("venta", "Un nuevo momento de gozo.", "PUNTO DE VENTA"));
}
