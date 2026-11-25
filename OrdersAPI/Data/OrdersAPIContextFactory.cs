using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrdersAPI.Data
{
    /// <summary>
    /// Utilisé uniquement par les outils EF Core (dotnet ef / console du gestionnaire de packages)
    /// pour créer les migrations sans démarrer l'application complète.
    /// </summary>
    public class OrdersAPIContextFactory : IDesignTimeDbContextFactory<OrdersAPIContext>
    {
        public OrdersAPIContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<OrdersAPIContext>()
                .UseSqlServer("server=(localdb)\\MSSQLLocalDB;database=OrdersDB;Trusted_Connection=true;MultipleActiveResultSets=true")
                .Options;

            return new OrdersAPIContext(options);
        }
    }
}
