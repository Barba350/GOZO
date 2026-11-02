using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GOZO;
[ApiController, Route("api/operaciones")]
public class OperacionesApiController(OperationsService service) : ControllerBase {
    [HttpGet("dashboard")] public async Task<IActionResult> Dashboard()=>Ok(await service.Dashboard());
    [HttpGet("resumen"),Authorize(Policy="Dueno")] public async Task<IActionResult> Summary()=>Ok(await service.Summary());
    [HttpGet("sucursales")] public async Task<IActionResult> Branches()=>Ok(await service.Branches());
    [HttpPost("sucursales/{id:int}/seleccionar")] public async Task<IActionResult> Select(int id) {
        await service.SelectBranch(id);
        Response.Cookies.Append("Gozo.Branch",id.ToString(),new CookieOptions{HttpOnly=true,Secure=Request.IsHttps,SameSite=SameSiteMode.Strict,Path="/"});return Ok(new { id });
    }
    [HttpPut("sucursales/{id:int}"),Authorize(Policy="Dueno")] public async Task<IActionResult> Rename(int id,BranchNameInput input)=>Ok(await service.RenameBranch(id,input.Name));
    [HttpGet("caja/{id:int}")] public async Task<IActionResult> CashDetail(int id)=>Ok(await service.CashDetail(id));
    [HttpGet("caja")] public async Task<IActionResult> Cash()=>Ok(await service.Status());
    [HttpPost("caja/abrir")] public async Task<IActionResult> Open(OpenCashInput input)=>Ok(await service.Open(input));
    [HttpPost("caja/{id:int}/cerrar")] public async Task<IActionResult> Close(int id,CloseCashInput input)=>Ok(await service.Close(id,input));
    [HttpGet("cupones"),Authorize(Policy="Dueno")] public async Task<IActionResult> Coupons()=>Ok(await service.Coupons());
    [HttpPost("cupones"),Authorize(Policy="Dueno")] public async Task<IActionResult> CreateCoupon(CouponInput input)=>Ok(await service.CreateCoupon(input));
    [HttpPost("cupones/{id:int}/desactivar"),Authorize(Policy="Dueno")] public async Task<IActionResult> DisableCoupon(int id)=>Ok(await service.DisableCoupon(id));
    [HttpGet("ventas/{id:int}"),Authorize(Policy="Dueno")] public async Task<IActionResult> Sale(int id)=>Ok(await service.Sale(id));
    [HttpPut("ventas/{id:int}"),Authorize(Policy="Dueno")] public async Task<IActionResult> Correct(int id,CorrectSaleInput input)=>Ok(await service.Correct(id,input));
    [HttpGet("ventas/{id:int}/correcciones"),Authorize(Policy="Dueno")] public async Task<IActionResult> Corrections(int id)=>Ok(await service.Corrections(id));
}
public record BranchNameInput(string Name);
public class OperacionesController(IWorkspaceService workspace) : Controller {
    public async Task<IActionResult> Index()=>View(await workspace.Build("operaciones","Sucursales y caja","OPERACIÓN DEL NEGOCIO"));
}
