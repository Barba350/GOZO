using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
namespace GOZO;
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CuentaController(AccountService accounts) : Controller
{
    [AllowAnonymous, HttpGet, EnableRateLimiting("login")]
    public IActionResult Ingresar(string? returnUrl) => View(new LoginInput { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });
    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Ingresar(LoginInput input) {
        if (!ModelState.IsValid) { ModelState.AddModelError("", "Escribe tu usuario y contraseña."); return View(input); }
        try {
            var account = await accounts.Login(input.Username, input.Password, Ip());
            await HttpContext.SignInAsync("GozoCookie", AccountService.Principal(account), new AuthenticationProperties { IsPersistent = false });
            return LocalRedirect(account.MustChangePassword ? "/Cuenta/CambiarClave" : Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl! : "/Ventas");
        } catch (RuleException e) { ModelState.AddModelError("", e.Message); input.Password = ""; return View(input); }
    }
    [Authorize, HttpPost] public async Task<IActionResult> Salir() {
        await accounts.Audit(User.Identity?.Name ?? "", "Cierre de sesión", Ip()); await HttpContext.SignOutAsync("GozoCookie"); return RedirectToAction(nameof(Ingresar));
    }
    [Authorize, HttpGet] public IActionResult CambiarClave() => View(new PasswordInput());
    [Authorize, HttpPost, EnableRateLimiting("login")] public async Task<IActionResult> CambiarClave(PasswordInput input) {
        if (!ModelState.IsValid) return View(input);
        try { await accounts.ChangePassword(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), input, Ip()); await HttpContext.SignOutAsync("GozoCookie"); TempData["Message"] = "Contraseña actualizada. Inicia sesión de nuevo."; return RedirectToAction(nameof(Ingresar)); }
        catch (RuleException e) { ModelState.AddModelError("", e.Message); return View(input); }
    }
    [Authorize, HttpGet] public IActionResult Denegado() { Response.StatusCode = 403; return View(); }
    private string Ip() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
}
