using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
namespace GOZO;
public static class SecuritySetup
{
    public static void AddGozoSecurity(this WebApplicationBuilder builder, string dataDir) {
        var dev = builder.Environment.IsDevelopment();
        builder.Services.AddDbContext<AccountsDb>(o => o.UseSqlite( $"Data Source={Path.Combine(dataDir, "accounts.db")}"));
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<AccountService>();
        builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        builder.Services.Configure<PasswordHasherOptions>(o => o.IterationCount = 210000);
        builder.Services.AddDataProtection().SetApplicationName("GOZO").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
        builder.Services.AddAntiforgery(o => {
            o.HeaderName = "X-CSRF-TOKEN";
            o.Cookie.Name = dev ? "Gozo.Csrf" : "__Host-Gozo.Csrf";
            o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = dev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddAuthentication("GozoCookie").AddCookie("GozoCookie", o => {
            o.LoginPath = "/Cuenta/Ingresar"; o.AccessDeniedPath = "/Cuenta/Denegado";
            o.Cookie.Name = dev ? "Gozo.Session" : "__Host-Gozo.Session";
            o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict; o.Cookie.Path = "/";
            o.Cookie.SecurePolicy = dev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = false;
            o.Events = new CookieAuthenticationEvents {
                OnValidatePrincipal = async c => {
                    if (!await c.HttpContext.RequestServices.GetRequiredService<AccountService>().Valid(c.Principal)) c.RejectPrincipal();
                },
                OnRedirectToLogin = c => Redirect(c, 401), OnRedirectToAccessDenied = c => Redirect(c, 403)
            };
        });
        builder.Services.AddAuthorization(o => {
            o.FallbackPolicy = new AuthorizationPolicyBuilder("GozoCookie").RequireAuthenticatedUser().Build();
            o.AddPolicy("Dueno", p => p.RequireAuthenticatedUser().RequireRole("Dueno"));
        });
        builder.Services.AddRateLimiter(o => {
            o.RejectionStatusCode = 429;
            o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(c.User.Identity?.Name ?? c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 240, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            o.OnRejected = async (c, token) => { c.HttpContext.Response.Headers["Retry-After"] = "60"; await c.HttpContext.Response.WriteAsync("Demasiadas solicitudes. Espera unos minutos e intenta otra vez.", token); };
        });
    }
    private static Task Redirect(RedirectContext<CookieAuthenticationOptions> c, int status) {
        if (c.Request.Path.StartsWithSegments("/api")) { c.Response.StatusCode = status; return c.Response.WriteAsJsonAsync(new { error = status == 401 ? "La sesión venció. Inicia sesión de nuevo." : "No tienes permiso para esta operación." }); }
        c.Response.Redirect(c.RedirectUri); return Task.CompletedTask;
    }
}
