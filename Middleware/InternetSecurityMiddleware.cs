namespace GOZO;
public class InternetSecurityMiddleware(RequestDelegate next, IWebHostEnvironment env, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context) {
        var r = context.Response;
        r.Headers["X-Content-Type-Options"] = "nosniff";
        r.Headers["X-Frame-Options"] = "DENY";
        r.Headers["Referrer-Policy"] = "no-referrer";
        r.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        r.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";
        if (!context.Request.Path.StartsWithSegments("/images") && !context.Request.Path.Value!.EndsWith(".css") && !context.Request.Path.Value.EndsWith(".js")) r.Headers["Cache-Control"] = "no-store";
        if (!env.IsDevelopment() && context.Request.Path != "/salud") {
            var origin = new Uri(configuration["GOZO_PUBLIC_ORIGIN"]!);
            if (!string.Equals(context.Request.Host.Value, origin.Authority, StringComparison.OrdinalIgnoreCase)) { r.StatusCode = 400; await r.WriteAsync("Host no permitido."); return; }
            r.Headers["Strict-Transport-Security"] = "max-age=31536000";
        }
        await next(context);
    }
}
public class AccountAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AccountService accounts) {
        if (context.User.Identity?.IsAuthenticated == true && context.User.FindFirst("change_password")?.Value == "yes" && !context.Request.Path.StartsWithSegments("/Cuenta")) {
            if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = 403; await context.Response.WriteAsJsonAsync(new { error = "Cambia tu contraseña antes de continuar." }); }
            else context.Response.Redirect("/Cuenta/CambiarClave");
            return;
        }
        await next(context);
        if (context.User.Identity?.IsAuthenticated == true && context.Request.Path.StartsWithSegments("/api") && context.Request.Path != "/api/ventas/cotizar" && (context.Request.Method is "POST" or "PUT" or "DELETE") && context.Response.StatusCode < 400) {
            await accounts.Audit(context.User.Identity.Name ?? "", context.Request.Method + " " + context.Request.Path, context.Connection.RemoteIpAddress?.ToString() ?? "");
        }
    }
}
