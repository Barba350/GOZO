using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace GOZO;
public static class SqlServerSetup {
    public static async Task Initialize(GozoDb sales, AccountsDb accounts, bool createDatabase = false) {
        // En desarrollo se crea la base vacía si el usuario SQL tiene permisos.
        // En producción la base se prepara antes en el alojamiento.
        if(createDatabase) {
            var creator=sales.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
            if(!await creator.ExistsAsync()) await creator.CreateAsync();
        }
        await sales.Database.OpenConnectionAsync();
        try {
            await sales.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sp_getapplock @Resource=N'GOZO:initialize', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=30000; IF @r<0 THROW 51000, 'No se pudo bloquear la inicialización', 1;");
            foreach (var pair in new (DbContext Context, string Table)[] { (sales, "dbo.Categories"), (accounts, "gozo_accounts.Accounts") }) {
                using var cmd = sales.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(@table, 'U') IS NULL THEN 0 ELSE 1 END";
                var par = cmd.CreateParameter(); par.ParameterName="@table"; par.Value=pair.Table; cmd.Parameters.Add(par);
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 0) {
                    await using var tx = await sales.Database.BeginTransactionAsync();
                    foreach (var batch in System.Text.RegularExpressions.Regex.Split(pair.Context.Database.GenerateCreateScript(), @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        if (!string.IsNullOrWhiteSpace(batch)) await sales.Database.ExecuteSqlRawAsync(batch);
                    await tx.CommitAsync();
                }
            }
            foreach(var column in new[] {
                ("Sales","CreatedById","int NULL"),("Sales","CreatedBy","nvarchar(max) NOT NULL DEFAULT N''"),
                ("CashSessions","OpenedById","int NOT NULL DEFAULT 0"),("CashSessions","CountedQr","bigint NULL"),("CashSessions","QrDifference","bigint NULL"),
                ("Coupons","GiftProductId","int NULL"), ("Coupons","GiftQuantity","int NOT NULL DEFAULT 1"),
                ("Sales","CouponProductId","int NULL"), ("Sales","CouponQuantity","int NOT NULL DEFAULT 0"),
                ("Orders","CouponProductId","int NULL"), ("Orders","CouponQuantity","int NOT NULL DEFAULT 0")
            }) {
                // Identificadores constantes de esta lista; no se incorporan datos del usuario.
                var ddl=$"IF COL_LENGTH(N'dbo.{column.Item1}',N'{column.Item2}') IS NULL ALTER TABLE [dbo].[{column.Item1}] ADD [{column.Item2}] {column.Item3};";
                await sales.Database.ExecuteSqlRawAsync(ddl);
            }
            await sales.Database.ExecuteSqlRawAsync("IF EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_CashSessions_BranchId' AND object_id=OBJECT_ID(N'dbo.CashSessions') AND is_unique=1) DROP INDEX [IX_CashSessions_BranchId] ON [dbo].[CashSessions]; IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_CashSessions_OpenedById' AND object_id=OBJECT_ID(N'dbo.CashSessions')) CREATE UNIQUE INDEX [IX_CashSessions_OpenedById] ON [dbo].[CashSessions] ([OpenedById]) WHERE [ClosedAt] IS NULL AND [OpenedById]>0;");
            if (!await sales.Branches.AnyAsync()) {
                sales.Branches.AddRange(new Branch { Name="Sucursal 1" }, new Branch { Name="Sucursal 2" });
                await sales.SaveChangesAsync();
            }
            await CatalogDefaults.EnsureCategories(sales);
        } finally {
            try { await sales.Database.ExecuteSqlRawAsync("EXEC sp_releaseapplock @Resource=N'GOZO:initialize', @LockOwner='Session';"); }
            finally { await sales.Database.CloseConnectionAsync(); }
        }
    }
}
