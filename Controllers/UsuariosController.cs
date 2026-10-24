using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace GOZO;
[Authorize(Policy = "Dueno")]
public class UsuariosController(AccountService accounts) : Controller
{
    [HttpGet] public async Task<IActionResult> Index() => View(await accounts.List());
    [HttpPost] public async Task<IActionResult> Crear(CreateAccountInput input) {
        try { if (!ModelState.IsValid) throw new RuleException("Revisa el usuario y la contraseña (mínimo 14 caracteres)."); await accounts.Create(input, User.Identity!.Name!, Ip()); TempData["Message"] = "Cuenta creada. Deberá cambiar la contraseña al entrar."; }
        catch (RuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Estado(int id) {
        try { await accounts.Toggle(id, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!, Ip()); }
        catch (RuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Restablecer(int id, string password) {
        try { if (string.IsNullOrEmpty(password)) throw new RuleException("Escribe una contraseña temporal."); await accounts.ResetPassword(id, password, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!, Ip()); TempData["Message"] = "Contraseña temporal actualizada. Las sesiones anteriores quedaron cerradas."; }
        catch (RuleException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
    private string Ip() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
}
