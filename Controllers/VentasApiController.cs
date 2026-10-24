using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/ventas")]
public class VentasApiController(ISaleService service) : ControllerBase
{
    [HttpPost("cotizar")] public async Task<IActionResult> Quote([FromBody] SaleInput input) => Ok(await service.Quote(input));
    [HttpPost] public async Task<IActionResult> Create([FromBody] SaleInput input) => Ok(await service.Create(input));
}
