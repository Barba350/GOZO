using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace GOZO;

public static class DatabaseSetup
{
    public static bool UsesPostgres(IConfiguration config) =>
        string.Equals(config["GOZO_DB_PROVIDER"], "PostgreSQL", StringComparison.OrdinalIgnoreCase);

    public static void Configure(DbContextOptionsBuilder options, IConfiguration config) {
        var provider = config["GOZO_DB_PROVIDER"] ?? "SQLServer";
        if(UsesPostgres(config)) {
            var connection=config["GOZO_POSTGRES_CONNECTION"]??throw new InvalidOperationException("Falta GOZO_POSTGRES_CONNECTION.");
            var settings=new NpgsqlConnectionStringBuilder(connection);
            if(settings.SslMode!=SslMode.VerifyFull && !(config["GOZO_ALLOW_TEST_DATABASE"]=="true"&&config["DOTNET_ENVIRONMENT"]=="Development")) throw new InvalidOperationException("PostgreSQL requiere SSL Mode=VerifyFull.");
            settings.IncludeErrorDetail=false;options.UseNpgsql(settings.ConnectionString);
        } else if(provider.Equals("SQLServer",StringComparison.OrdinalIgnoreCase)) {
            var connection=config["GOZO_SQLSERVER_CONNECTION"]??config.GetConnectionString("Gozo");
            if(string.IsNullOrWhiteSpace(connection))throw new InvalidOperationException("Configura SQL Server local o GOZO_DB_PROVIDER=PostgreSQL para Neon.");
            var settings=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
            var test=config["GOZO_ALLOW_TEST_DATABASE"]=="true"&&(config["ASPNETCORE_ENVIRONMENT"]=="Development"||config["DOTNET_ENVIRONMENT"]=="Development");
            if(!test&&(settings.TrustServerCertificate||settings.Encrypt==Microsoft.Data.SqlClient.SqlConnectionEncryptOption.Optional))throw new InvalidOperationException("Configura Encrypt=True;TrustServerCertificate=False.");
            options.UseSqlServer(settings.ConnectionString,s=>s.UseCompatibilityLevel(150));
        } else throw new InvalidOperationException("Solo se admiten SQLServer y PostgreSQL.");
    }

    public static async Task Initialize(GozoDb sales, AccountsDb accounts) {
        if (sales.Database.IsSqlServer()) {
            await SqlServerSetup.Initialize(sales,accounts);
            return;
        }
        // Dos contextos en una base: EnsureCreated por sí solo omite el segundo.
        // Un bloqueo de sesión evita inicializaciones simultáneas en despliegues.
        await using var connection = new NpgsqlConnection(sales.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var guard = new NpgsqlCommand("SELECT pg_advisory_lock(72401683)", connection);
        await guard.ExecuteNonQueryAsync();
        try {
            foreach (var item in new (DbContext Context, string Table)[] { (sales, "public.\"Categories\""), (accounts, "gozo_accounts.\"Accounts\"") }) {
                await using var check = new NpgsqlCommand("SELECT to_regclass(@table) IS NOT NULL", connection);
                check.Parameters.AddWithValue("table", item.Table);
                if (!(bool)(await check.ExecuteScalarAsync())!) {
                    await using var transaction = await connection.BeginTransactionAsync();
                    await using var create = new NpgsqlCommand(item.Context.Database.GenerateCreateScript(), connection, transaction);
                    await create.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
            }
            await UpgradeOrders(sales);
            await UpgradeSalePromotions(sales);
            await UpgradeProductImages(sales);
            await UpgradeOperations(sales);
            await UpgradeDefaultCustomer(sales);
        } finally {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(72401683)", connection);
            await release.ExecuteNonQueryAsync();
        }
    }
    private static async Task UpgradeOrders(GozoDb db) {
        var dialect = db.Database.IsNpgsql() ? "postgresql" : "sqlite";
        using var resource = typeof(DatabaseSetup).Assembly.GetManifestResourceStream($"GOZO.Data.Migrations.001_orders.{dialect}.sql")
            ?? throw new InvalidOperationException("Falta la migración de pedidos.");
        using var reader = new StreamReader(resource);
        var sql = await reader.ReadToEndAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(sql);
        await transaction.CommitAsync();
    }

    private static async Task UpgradeSalePromotions(GozoDb db) {
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed) await connection.OpenAsync();
        try {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM \"GozoSchemaVersions\" WHERE \"Version\"='002-sale-promotions'";
            if (Convert.ToInt64(await check.ExecuteScalarAsync()) != 0) return;
        } finally { if (wasClosed) await connection.CloseAsync(); }
        var dialect = db.Database.IsNpgsql() ? "postgresql" : "sqlite";
        using var resource = typeof(DatabaseSetup).Assembly.GetManifestResourceStream($"GOZO.Data.Migrations.002_sale_promotions.{dialect}.sql")
            ?? throw new InvalidOperationException("Falta la migración de promociones por venta.");
        using var reader = new StreamReader(resource);
        var sql = await reader.ReadToEndAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(sql);
        await transaction.CommitAsync();
    }

    private static async Task UpgradeProductImages(GozoDb db) {
        var dialect = db.Database.IsNpgsql() ? "postgresql" : "sqlite";
        using var resource = typeof(DatabaseSetup).Assembly.GetManifestResourceStream($"GOZO.Data.Migrations.003_product_images.{dialect}.sql")
            ?? throw new InvalidOperationException("Falta la migración de fotos de productos.");
        using var reader = new StreamReader(resource);
        var sql = await reader.ReadToEndAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(sql);
        await transaction.CommitAsync();
    }

    private static async Task UpgradeOperations(GozoDb db) {
        using var resource=typeof(DatabaseSetup).Assembly.GetManifestResourceStream("GOZO.Data.Migrations.004_operations.postgresql.sql")??throw new InvalidOperationException("Falta migración de sucursales y caja.");
        using var reader=new StreamReader(resource);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(await reader.ReadToEndAsync());
        await tx.CommitAsync();
    }

    private static async Task UpgradeDefaultCustomer(GozoDb db) {
        using var resource=typeof(DatabaseSetup).Assembly.GetManifestResourceStream("GOZO.Data.Migrations.005_default_customer.postgresql.sql")??throw new InvalidOperationException("Falta migración de cliente predeterminado.");
        using var reader=new StreamReader(resource);
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(await reader.ReadToEndAsync());await tx.CommitAsync();
    }

}
