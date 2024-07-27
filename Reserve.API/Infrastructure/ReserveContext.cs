using Microsoft.EntityFrameworkCore;
using Reserve.API.Infrastructure.EntityConfigurations;
using Reserve.API.Model;

namespace Reserve.API.Infrastructure;

public class ReserveContext: DbContext
{
    public ReserveContext(DbContextOptions<ReserveContext> options, IConfiguration configuration) : base(options)
    {
    }

    public DbSet<ReserveItem> ReserveItems { get; set; }
    public DbSet<DateTimeNotAvailable> DateTimeNotAvailables { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("vector");
        builder.ApplyConfiguration(new ReserveItemEntityTypeConfiguration());
        builder.ApplyConfiguration(new DateTimeNotAvailableEntityTypeConfiguration());
        builder.Entity<ReserveItem>()
            .Property(r => r.DateReservation)
            .HasConversion(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.Entity<DateTimeNotAvailable>()
            .Property(d => d.Date)
            .HasConversion(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
        builder.Entity<ReserveItem>()
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Entity<DateTimeNotAvailable>()
            .Property(d => d.Id)
            .ValueGeneratedOnAdd();
        // Add the outbox table to this context
        //builder.UseIntegrationEventLogs();
    }
}