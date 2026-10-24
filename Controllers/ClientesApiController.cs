using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/clientes")]
public class ClientesApiController(ICustomerService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] string? buscar) => Ok(await service.Search(buscar));
    [HttpPost] public async Task<IActionResult> Create([FromBody] CustomerInput input) { var customer = await service.Create(input); return Created($"/api/clientes/{customer.Id}", customer); }
}
