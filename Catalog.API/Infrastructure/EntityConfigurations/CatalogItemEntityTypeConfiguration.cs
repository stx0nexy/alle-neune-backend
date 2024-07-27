using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.API.Infrastructure.EntityConfigurations;

public class CatalogItemEntityTypeConfiguration: IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("Catalog");

        builder.Property(ci => ci.Title)
            .HasMaxLength(350);

        builder.HasOne(ci => ci.Category)
            .WithMany();

        builder.HasIndex(ci => ci.Title);
    }
}
