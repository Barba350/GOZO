using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[Authorize(Policy = "Dueno")]
public class PromocionesController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("promociones", "Más motivos para volver.", "PROMOCIONES"));
}
