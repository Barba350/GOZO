using Microsoft.EntityFrameworkCore;
namespace GOZO;

public class GozoDb(DbContextOptions<GozoDb> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<GozoOrder> Orders => Set<GozoOrder>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Fechas comerciales en hora de Bolivia, sin zona; auditoría/cuentas usan UTC.
        if (Database.IsNpgsql()) {
            b.Entity<Sale>().Property(x => x.Date).HasColumnType("timestamp without time zone");
            b.Entity<Promotion>().Property(x => x.Start).HasColumnType("timestamp without time zone");
            b.Entity<Promotion>().Property(x => x.End).HasColumnType("timestamp without time zone");
        }
        var order = b.Entity<GozoOrder>();
        order.ToTable("Orders");
        order.HasIndex(x => x.RequestId).IsUnique();
        order.HasIndex(x => x.SaleRequestId).IsUnique();
        order.HasIndex(x => x.SaleId).IsUnique();
        order.HasIndex(x => new { x.Status, x.Id });
        order.Property(x => x.Revision).IsConcurrencyToken();
        order.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        order.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        order.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<OrderLine>().ToTable("OrderLines");
        b.Entity<OrderLine>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        if (Database.IsNpgsql()) {
            order.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
            order.Property(x => x.ClosedAt).HasColumnType("timestamp without time zone");
        }
        b.Entity<Product>().HasIndex(x => new { x.Key, x.CategoryId }).IsUnique();
        b.Entity<Product>().HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Customer>().HasIndex(x => x.Ci).IsUnique();
        b.Entity<Sale>().HasIndex(x => x.Date);
        b.Entity<Sale>().HasIndex(x => x.RequestId).IsUnique();
        b.Entity<Sale>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SaleLine>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PromotionProduct>().HasKey(x => new { x.PromotionId, x.ProductId });
        b.Entity<PromotionCustomer>().HasKey(x => new { x.PromotionId, x.CustomerId });
    }
}
