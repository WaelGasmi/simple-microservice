using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductsAPI.Data
{
    /// <summary>
    /// Utilisé uniquement par les outils EF Core (dotnet ef / console du gestionnaire de packages)
    /// pour créer les migrations sans démarrer l'application complète.
    /// </summary>
    public class ProductsAPIContextFactory : IDesignTimeDbContextFactory<ProductsAPIContext>
    {
        public ProductsAPIContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ProductsAPIContext>()
                .UseSqlServer("server=(localdb)\\MSSQLLocalDB;database=ProductsDB;Trusted_Connection=true;MultipleActiveResultSets=true")
                .Options;

            return new ProductsAPIContext(options);
        }
    }
}
