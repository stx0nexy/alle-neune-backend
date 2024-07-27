using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reserve.API.Model;

namespace Reserve.API.Infrastructure.EntityConfigurations;

public class DateTimeNotAvailableEntityTypeConfiguration : IEntityTypeConfiguration<DateTimeNotAvailable>
{
    public void Configure(EntityTypeBuilder<DateTimeNotAvailable> builder)
    {
        builder.ToTable("DateTimeNotAvailable");
    }
}