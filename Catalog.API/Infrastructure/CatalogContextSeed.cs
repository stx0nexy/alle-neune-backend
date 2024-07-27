using Microsoft.Extensions.Options;
using System.Text.Json;
using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;

//using Npgsql;

namespace Catalog.API.Infrastructure;
public partial class CatalogContextSeed(
    IWebHostEnvironment env,
    IOptions<CatalogOptions> settings,
    ILogger<CatalogContextSeed> logger) : IDbSeeder<CatalogContext>
{
    public async Task SeedAsync(CatalogContext context)
    {
        var useCustomizationData = settings.Value.UseCustomizationData;
        var contentRootPath = env.ContentRootPath;
        var picturePath = env.WebRootPath;

        // Workaround from https://github.com/npgsql/efcore.pg/issues/292#issuecomment-388608426
        context.Database.OpenConnection();
        ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();        

        if (!context.CatalogItems.Any())
        {
            var sourcePath = Path.Combine(contentRootPath, "Setup", "catalog.json");
            var sourceJson = File.ReadAllText(sourcePath);
            var sourceItems = JsonSerializer.Deserialize<CatalogSourceEntry[]>(sourceJson);

            context.CatalogCategories.RemoveRange(context.CatalogCategories);
            await context.CatalogCategories.AddRangeAsync(sourceItems.Select(x => x.Category).Distinct()
                .Select(catalogName => new CatalogCategory { Title = catalogName }));
            logger.LogInformation("Seeded catalog with {NumCategories} categories", context.CatalogCategories.Count());


            await context.SaveChangesAsync();

            var categoryIdsByName = await context.CatalogCategories.ToDictionaryAsync(x => x.Title, x => x.Id);

            var catalogItems = sourceItems.Select(source => new CatalogItem
            {
                Title = source.Title,
                Description = source.Description,
                Price = source.Price,
                CategoryId = categoryIdsByName[source.Category],
            }).ToArray();

            await context.CatalogItems.AddRangeAsync(catalogItems);
            logger.LogInformation("Seeded catalog with {NumItems} items", context.CatalogItems.Count());
            await context.SaveChangesAsync();
        }
    }

    private class CatalogSourceEntry
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }
}