using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reserve.API.Model;

namespace Reserve.API.Infrastructure.EntityConfigurations;

public class ReserveItemEntityTypeConfiguration: IEntityTypeConfiguration<ReserveItem>
{
    public void Configure(EntityTypeBuilder<ReserveItem> builder)
    {
        builder.ToTable("ReserveItem");
    }
}