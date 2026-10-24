using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/productos")]
public class ProductosApiController(IProductService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] bool todos = false) => Ok(await service.List(todos));
    [Authorize(Policy = "Dueno")]
    [HttpPost] public async Task<IActionResult> Create([FromBody] ProductInput input) { var product = await service.Create(input); return Created($"/api/productos/{product.Id}", product); }
    [Authorize(Policy = "Dueno")]
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] ProductInput input) => Ok(await service.Update(id, input));
    [Authorize(Policy = "Dueno")]
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { await service.Delete(id); return NoContent(); }
}
