using MerchForge.api.Models;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Data
{
    public class MerchForgeDbContext :DbContext
    {
        public MerchForgeDbContext(
            DbContextOptions<MerchForgeDbContext> options)
        :base (options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Business> Businesses => Set<Business>();

        public DbSet<BusinessUser> BusinessUsers => Set<BusinessUser>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<ProductDraft> ProductDrafts => Set<ProductDraft>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(MerchForgeDbContext).Assembly);
        }

    }
}
