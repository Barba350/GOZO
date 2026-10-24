using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/promociones")]
public class PromocionesApiController(IPromotionService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] bool activas = false) => Ok(await service.List(activas));
    [Authorize(Policy = "Dueno")]
    [HttpPost] public async Task<IActionResult> Create([FromBody] PromotionInput input) => Ok(await service.Create(input));
    [Authorize(Policy = "Dueno")]
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] PromotionInput input) => Ok(await service.Update(id, input));
    [Authorize(Policy = "Dueno")]
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { await service.Delete(id); return NoContent(); }
}
