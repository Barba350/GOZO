using Microsoft.EntityFrameworkCore;
namespace GOZO;

public class GozoDb(DbContextOptions<GozoDb> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<SaleCorrection> SaleCorrections => Set<SaleCorrection>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<GozoOrder> Orders => Set<GozoOrder>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Fechas comerciales en hora de Bolivia; auditoría en UTC.
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
        b.Entity<Promotion>().HasOne(x => x.Rule).WithOne().HasForeignKey<PromotionRule>(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PromotionRule>().ToTable("PromotionRules").HasKey(x => x.PromotionId);
        var use = b.Entity<PromotionUse>();
        use.ToTable("PromotionUses");
        use.HasOne<Promotion>().WithMany().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Restrict);
        use.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        use.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        use.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        use.HasIndex(x => new { x.PromotionId, x.SaleId }).IsUnique();
        use.HasIndex(x => new { x.PromotionId, x.OrderId }).IsUnique();
        use.HasIndex(x => new { x.CustomerId, x.PromotionId });
        b.Entity<ProductImage>().ToTable("ProductImages").HasKey(x => x.ProductId);
        b.Entity<Product>().HasOne(x => x.Image).WithOne().HasForeignKey<ProductImage>(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Product>().HasIndex(x => new { x.Key, x.CategoryId }).IsUnique();
        b.Entity<Product>().HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Customer>().HasIndex(x => x.Ci).IsUnique();
        b.Entity<Sale>().HasIndex(x => x.Date);
        b.Entity<Sale>().HasIndex(x => x.RequestId).IsUnique();
        b.Entity<Sale>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SaleLine>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PromotionProduct>().HasKey(x => new { x.PromotionId, x.ProductId });
        b.Entity<PromotionCustomer>().HasKey(x => new { x.PromotionId, x.CustomerId });
        b.Entity<Product>().Property(x => x.Key).HasMaxLength(100);
        b.Entity<Customer>().Property(x => x.Ci).HasMaxLength(25);
        b.Entity<Sale>().Property(x => x.RequestId).HasMaxLength(36);
        order.Property(x => x.RequestId).HasMaxLength(36);
        order.Property(x => x.SaleRequestId).HasMaxLength(36);
        b.Entity<Promotion>().Property(x => x.Value).HasPrecision(18, 2);
        b.Entity<Sale>().Property(x => x.CouponValue).HasPrecision(18, 2);
        order.Property(x => x.CouponValue).HasPrecision(18, 2);
        b.Entity<Coupon>().Property(x => x.Value).HasPrecision(18, 2);
        b.Entity<Branch>().Property(x => x.Name).HasMaxLength(100);
        b.Entity<Coupon>().Property(x => x.Code).HasMaxLength(40);
        b.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Coupon>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CashSession>().HasIndex(x => x.OpenedById).IsUnique().HasFilter(Database.IsNpgsql()?"\"ClosedAt\" IS NULL":"[ClosedAt] IS NULL");
        b.Entity<CashSession>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Sale>().HasOne(x=>x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Sale>().HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
        order.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Sale>().HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        order.HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Sale>().Property(x => x.Revision).IsConcurrencyToken();
        b.Entity<CashSession>().Property(x => x.Revision).IsConcurrencyToken();
        b.Entity<Coupon>().Property(x => x.Revision).IsConcurrencyToken();
        b.Entity<CouponRedemption>().HasIndex(x => x.CouponId).IsUnique().HasFilter(Database.IsNpgsql()?"NOT \"Released\"":"[Released] = 0");
        b.Entity<CouponRedemption>().HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CouponRedemption>().HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CouponRedemption>().HasOne<GozoOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SaleCorrection>().HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        if(Database.IsNpgsql()) {
            foreach(var entity in b.Model.GetEntityTypes())
                foreach(var prop in entity.GetProperties().Where(x=>x.ClrType==typeof(DateTime)||x.ClrType==typeof(DateTime?)))
                    prop.SetColumnType("timestamp without time zone");
        }
    }
}
