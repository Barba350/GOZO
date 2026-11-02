using Microsoft.AspNetCore.Mvc;
namespace GOZO;
public class DashboardController(IWorkspaceService workspace):Controller {
    public async Task<IActionResult> Index()=>View(await workspace.Build("dashboard","Así se mueve tu sucursal","PANEL DEL EQUIPO"));
}
