using C_3PO.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace C_3PO.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Onboarding> Onboardings { get; set; }
    }
}
