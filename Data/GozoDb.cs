using Microsoft.EntityFrameworkCore;
namespace GOZO;

public class GozoDb(DbContextOptions<GozoDb> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    protected override void OnModelCreating(ModelBuilder b)
    {
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
