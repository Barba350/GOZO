using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[Authorize(Policy = "Reportes")]
public class ReportesController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("reportes", "Tu negocio, en números.", "RESULTADOS"));
}
