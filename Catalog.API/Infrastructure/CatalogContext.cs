using Catalog.API.Infrastructure.EntityConfigurations;
using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Infrastructure;

/// <remarks>
/// Add migrations using the following command inside the 'Catalog.API' project directory:
///
/// dotnet ef migrations add --context CatalogContext [migration-name]
/// </remarks>
public class CatalogContext : DbContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options, IConfiguration configuration) : base(options)
    {
    }

    public DbSet<CatalogItem> CatalogItems { get; set; }
    public DbSet<CatalogCategory> CatalogCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new CatalogItemEntityTypeConfiguration());
        builder.ApplyConfiguration(new CatalogCategoryEntityTypeConfiguration());
        builder.Entity<CatalogCategory>()
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Entity<CatalogItem>()
            .Property(d => d.Id)
            .ValueGeneratedOnAdd();

        // Add the outbox table to this context
        //builder.UseIntegrationEventLogs();
    }
}