using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[Authorize(Policy = "Dueno")]
public class HistorialController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("historial", "El gozo de volver.", "FIDELIZACIÓN"));
}
