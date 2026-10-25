using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[Authorize(Policy = "Reportes")]
public class PedidosController(IWorkspaceService workspace) : Controller
{
    public async Task<IActionResult> Index() => View(await workspace.Build("pedidos", "Cada pedido, a tiempo.", "PEDIDOS POR WHATSAPP"));
}
