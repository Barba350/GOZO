using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace GOZO;

public static class DatabaseSetup
{
    public static bool UsesPostgres(IConfiguration config) =>
        string.Equals(config["GOZO_DB_PROVIDER"], "PostgreSQL", StringComparison.OrdinalIgnoreCase);

    public static void Configure(DbContextOptionsBuilder options, IConfiguration config, string sqliteFile) {
        var provider = config["GOZO_DB_PROVIDER"] ?? "SQLite";
        if (UsesPostgres(config)) {
            var connection = config["GOZO_POSTGRES_CONNECTION"];
            if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Falta el secreto GOZO_POSTGRES_CONNECTION.");
            var settings = new NpgsqlConnectionStringBuilder(connection);
            if (settings.SslMode != SslMode.VerifyFull && !(config["GOZO_ALLOW_TEST_DATABASE"] == "true" && config["DOTNET_ENVIRONMENT"] == "Development"))
                throw new InvalidOperationException("PostgreSQL requiere SSL Mode=VerifyFull para verificar el servidor.");
            settings.IncludeErrorDetail = false;
            options.UseNpgsql(settings.ConnectionString);
        } else if (string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase)) {
            options.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = sqliteFile }.ConnectionString);
        } else throw new InvalidOperationException("GOZO_DB_PROVIDER debe ser SQLite o PostgreSQL.");
    }

    public static async Task Initialize(GozoDb sales, AccountsDb accounts) {
        if (!sales.Database.IsNpgsql()) {
            await sales.Database.EnsureCreatedAsync();
            await accounts.Database.EnsureCreatedAsync();
            await UpgradeOrders(sales);
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

}
