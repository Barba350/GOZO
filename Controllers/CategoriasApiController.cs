using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/categorias")]
public class CategoriasApiController(ICategoryService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List() => Ok(await service.List());
}
