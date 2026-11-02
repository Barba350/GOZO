namespace GOZO;
// El personal autorizado puede operar en ambas sucursales; seleccionar una no otorga roles.
public class BranchContext(IHttpContextAccessor accessor) {
    public int Id {
        get {
            var request=accessor.HttpContext?.Request;
            var raw=request?.Headers["X-Gozo-Branch"].FirstOrDefault() ?? request?.Query["sucursal"].FirstOrDefault() ?? request?.Cookies["Gozo.Branch"];
            return int.TryParse(raw,out var id)&&id>0?id:1;
        }
    }
    public int ActorId => int.TryParse(accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id)?id:0;
    public int? ReportBranchId => int.TryParse(accessor.HttpContext?.Request.Query["sucursal"].FirstOrDefault(),out var id)&&id>0?id:null;
    public int? ReportSellerId => int.TryParse(accessor.HttpContext?.Request.Query["cajero"].FirstOrDefault(),out var id)&&id>0?id:null;
    public string Actor => accessor.HttpContext?.User.Identity?.Name ?? "Sistema";
    public bool Owner => accessor.HttpContext?.User.IsInRole("Dueno") == true;
}
