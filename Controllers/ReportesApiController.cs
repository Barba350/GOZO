using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController]
[Route("api/reportes")]
[Authorize(Policy = "Reportes")]
public class ReportesApiController(IReportService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Generate([FromQuery] string? desde, [FromQuery] string? hasta) => Ok(await service.Generate(desde, hasta));
    [Authorize(Policy = "Dueno")]
    [HttpGet("/api/clientes/{id:int}/historial")] public async Task<IActionResult> History(int id, [FromQuery] string? desde, [FromQuery] string? hasta) => Ok(await service.History(id, desde, hasta));
    [HttpGet("excel")] public async Task<IActionResult> Excel([FromQuery] string? desde, [FromQuery] string? hasta) => File(await service.Excel(desde, hasta), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GOZO-ventas.xlsx");
    [HttpGet("pdf")] public async Task<IActionResult> Pdf([FromQuery] string? desde, [FromQuery] string? hasta) => File(await service.Pdf(desde, hasta), "application/pdf", "GOZO-ventas.pdf");
}
