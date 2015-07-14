using System.Data.Entity;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public DbSet<Series> Series { get; set; }
        public DbSet<Show> Shows { get; set; }

        public AppDbContext() : base("DbConnection")
        {
            
        }
    }
}
