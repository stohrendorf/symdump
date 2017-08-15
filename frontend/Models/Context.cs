using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace frontend.Models
{
    public class Context : DbContext
    {
        public DbSet<Project> Projects => Set<Project>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var lf = new LoggerFactory();
            lf.AddNLog();
            optionsBuilder.UseLoggerFactory(lf);

            optionsBuilder.UseSqlite("Data Source=projects.db");
        }
    }
}