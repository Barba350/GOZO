using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController, Route("api/pedidos"), Authorize(Policy = "Reportes")]
public class PedidosApiController(IOrderService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] string? estado) => Ok(await service.List(estado));
    [HttpPost] public async Task<IActionResult> Create(OrderInput input) => Ok(await service.Create(input));
    [HttpPost("{id:int}/entregar")] public async Task<IActionResult> Deliver(int id, DeliverOrderInput input) => Ok(await service.Deliver(id, input));
    [HttpPost("{id:int}/cancelar")] public async Task<IActionResult> Cancel(int id) => Ok(await service.Cancel(id));
}
