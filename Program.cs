using GOZO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
// Evita depender del registro de eventos de Windows (EventLog/RPC).
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var dataDir = builder.Configuration["GOZO_DATA_DIR"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
if (!Path.IsPathFullyQualified(dataDir)) throw new InvalidOperationException("GOZO_DATA_DIR debe ser una ruta absoluta.");
Directory.CreateDirectory(dataDir);
var production = !builder.Environment.IsDevelopment();
if (production) {
    if (!DatabaseSetup.UsesPostgres(builder.Configuration) && builder.Configuration["GOZO_PERSISTENT_SQLITE"] != "true") throw new InvalidOperationException("Esta versión usa SQLite. No la publiques en un disco temporal. El alojamiento gratuito requiere adaptar la versión de internet a una base externa; la versión local sigue funcionando sin este ajuste.");
    if (!Uri.TryCreate(builder.Configuration["GOZO_PUBLIC_ORIGIN"], UriKind.Absolute, out var origin) || origin.Scheme != "https" || origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment) || !string.IsNullOrEmpty(origin.UserInfo)) throw new InvalidOperationException("Configura GOZO_PUBLIC_ORIGIN con la URL pública HTTPS, sin rutas, usuario ni contraseña.");
    if (builder.Configuration["GOZO_TLS_TERMINATION"] != "Edge") throw new InvalidOperationException("El despliegue requiere un proxy HTTPS administrado. Configura GOZO_TLS_TERMINATION=Edge solo detrás de ese proxy, sin acceso público directo al puerto interno.");
}
builder.WebHost.ConfigureKestrel(o => { o.AddServerHeader = false; o.Limits.MaxRequestBodySize = 65536; });
builder.Services.AddDbContext<GozoDb>(o => DatabaseSetup.Configure(o, builder.Configuration, Path.Combine(dataDir, "gozo.db")));
builder.AddGozoSecurity(dataDir);
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
// Repositorios: acceso a datos. Todos comparten el DbContext de la solicitud.
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// Servicios: reglas de negocio, validación y coordinación.
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
var app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<InternetSecurityMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<AccountAccessMiddleware>();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<GozoDb>();
    await DatabaseSetup.Initialize(db, scope.ServiceProvider.GetRequiredService<AccountsDb>());
    if (!db.Categories.Any()) {
        db.Categories.AddRange(new Category { Name = "Helados" }, new Category { Name = "Hamburguesas" }, new Category { Name = "Bebidas" });
        db.SaveChanges();
    }
}
await OwnerBootstrap.EnsureOwner(app.Services, builder.Configuration, args.Contains("--crear-dueno"));
if (args.Contains("--crear-dueno")) return;
app.MapGet("/salud", () => Results.Text("OK")).AllowAnonymous();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Ventas}/{action=Index}/{id?}");
app.Run();
