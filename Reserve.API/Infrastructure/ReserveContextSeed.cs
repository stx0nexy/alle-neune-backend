using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Reserve.API.Model;

namespace Reserve.API.Infrastructure;

public partial class ReserveContextSeed(
    IWebHostEnvironment env,
    IOptions<ReserveOptions> settings,
    ILogger<ReserveContextSeed> logger) : IDbSeeder<ReserveContext>
{
    public async Task SeedAsync(ReserveContext context)
    {
        var useCustomizationData = settings.Value.UseCustomizationData;
        var contentRootPath = env.ContentRootPath;

        // Workaround from https://github.com/npgsql/efcore.pg/issues/292#issuecomment-388608426
        context.Database.OpenConnection();
        ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();

        if (!context.ReserveItems.Any())
        {
            var sourcePath = Path.Combine(contentRootPath, "Setup", "reserve.json");
            var sourceJson = File.ReadAllText(sourcePath);
            var sourceItems = JsonSerializer.Deserialize<ReserveSourceEntry[]>(sourceJson);


            await context.SaveChangesAsync();
            
            var reserveItems = sourceItems.Select(source => new ReserveItem()
            {
                Id = source.Id,
                Game = source.Game,
                DateReservation = source.DateReservation,
                TimeReservation = source.TimeReservation,
                EatAndPlay = source.EatAndPlay,
                Name = source.Name,
                Surname = source.Surname,
                PhoneNumber = source.PhoneNumber,
                CountPersons = source.CountPersons,
                Message = source.Message
                
                
            }).ToArray();

            await context.ReserveItems.AddRangeAsync(reserveItems);
            logger.LogInformation("Seeded reserve with {NumItems} items", context.ReserveItems.Count());
            await context.SaveChangesAsync();
        }
    }

    private class ReserveSourceEntry
    {
        public int Id { get; set; }
    
        public bool Game { get; set; }
    
        public DateTime DateReservation { get; set; }
    
        public TimeSpan TimeReservation { get; set; }
        
        public bool EatAndPlay { get; set; }

        public string Name { get; set; } = null!;
    
        public string Surname { get; set; } = null!;
    
        public string PhoneNumber { get; set; } = null!;
    
        public int CountPersons { get; set; }
    
        public string? Message { get; set; }
    }
}