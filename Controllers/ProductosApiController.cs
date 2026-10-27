using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/productos")]
public class ProductosApiController(IProductService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] bool todos = false) => Ok(await service.List(todos));
    [HttpGet("{id:int}/imagen")]
    public async Task<IActionResult> Image(int id) {
        var image = await service.Image(id);
        if (image == null) return NotFound();
        Response.Headers["Cache-Control"] = "private, max-age=86400";
        return new FileContentResult(image.Bytes, "image/jpeg") { EntityTag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"" + image.Version + "\"") };
    }
    [Authorize(Policy = "Dueno")]
    [HttpPost] public async Task<IActionResult> Create([FromBody] ProductInput input) { var product = await service.Create(input); return Created($"/api/productos/{product.Id}", product); }
    [Authorize(Policy = "Dueno")]
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] ProductInput input) => Ok(await service.Update(id, input));
    [Authorize(Policy = "Dueno")]
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { await service.Delete(id); return NoContent(); }
}
