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

        public DbSet<BusinessDomain> BusinessDomains => Set<BusinessDomain>();

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<WebsiteTemplate> WebsiteTemplates => Set<WebsiteTemplate>();

        public DbSet<WebsiteTemplateRequest> WebsiteTemplateRequests => Set<WebsiteTemplateRequest>();

        public DbSet<ProductAttributeDefinition> ProductAttributeDefinitions
            => Set<ProductAttributeDefinition>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<ProductImage> ProductImages => Set<ProductImage>();

        public DbSet<ProductDraft> ProductDrafts => Set<ProductDraft>();

        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<Feature> Features => Set<Feature>();
        public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<FeatureCreditPackage> FeatureCreditPackages => Set<FeatureCreditPackage>();
        public DbSet<BusinessFeatureCredit> BusinessFeatureCredits => Set<BusinessFeatureCredit>();
        public DbSet<FeatureCreditTransaction> FeatureCreditTransactions => Set<FeatureCreditTransaction>();
        public DbSet<ImageEditJob> ImageEditJobs => Set<ImageEditJob>();

        public DbSet<Invitation> Invitations => Set<Invitation>();

        public DbSet<UserRole> SystemRoles => Set<UserRole>();
        public DbSet<BusinessUserRole> BusinessUserRoles => Set<BusinessUserRole>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(MerchForgeDbContext).Assembly);
        }

    }
}
