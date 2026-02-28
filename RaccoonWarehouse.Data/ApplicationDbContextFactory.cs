using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaccoonWarehouse.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var connectionString = "Data Source=DESKTOP-H7DBJRA;Initial Catalog=db_abc5d4_raccoon;User Id=sa;Password=anymouse786;TrustServerCertificate=True;";

            /*            var connectionString = "Server=.;Database=newFinalRaccoonWarehouseDb;Trusted_Connection=True;TrustServerCertificate=True;";
            */
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
