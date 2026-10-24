using Microsoft.AspNetCore.Mvc;
namespace GOZO;
public class ClientesController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("clientes", "Cada cliente, su historia.", "CLIENTES"));
}
