using Microsoft.EntityFrameworkCore;
namespace GOZO;

public static class CatalogDefaults
{
    // Conserva las categorías, IDs y productos de instalaciones existentes.
    public static readonly string[] Categories = ["Helados", "Hamburguesas", "Bebidas", "Sándwiches y croissants", "Salchipapas", "Platos de pollo", "Empanadas", "Cafetería", "Jugos naturales", "Postres"];
    public static async Task EnsureCategories(GozoDb db) {
        var existing = (await db.Categories.AsNoTracking().Select(x => x.Name).ToListAsync())
            .Select(Rules.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var name in Categories)
            if (existing.Add(Rules.Key(name))) db.Categories.Add(new Category { Name = name });
        await db.SaveChangesAsync();
    }
}
