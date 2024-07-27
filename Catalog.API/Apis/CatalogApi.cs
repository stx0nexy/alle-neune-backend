using Catalog.API.Infrastructure;
using Catalog.API.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Apis;

public static class CatalogApi
{
    public static RouteGroupBuilder MapCatalogApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/catalog").HasApiVersion(1.0);

        // Routes for querying catalog items.
        api.MapGet("/items", GetAllItems).AllowAnonymous();
        api.MapGet("/items/by", GetItemsByIds).AllowAnonymous();
        api.MapGet("/items/{id:int}", GetItemById).AllowAnonymous();
        api.MapGet("/items/by/{name:minlength(1)}", GetItemsByName).AllowAnonymous();
        api.MapGet("/items/{catalogItemId:int}/pic", GetItemPictureById).AllowAnonymous();

        // Routes for resolving catalog items using AI.

        // Routes for resolving catalog items by type and brand.
        api.MapGet("/items/type/all/category/{categoryId:int?}", GetItemsByCategoryId).AllowAnonymous();
        api.MapGet("/categories", async (CatalogContext context) => await context.CatalogCategories.OrderBy(x => x.Title).ToListAsync()).AllowAnonymous();

        // Routes for modifying catalog items.
        api.MapPut("/items", UpdateItem);
        api.MapPost("/items", CreateItem);
        api.MapPost("/category", CreateCategory);
        api.MapDelete("/items/{id:int}", DeleteItemById);
        api.MapDelete("/category/{id:int}", DeleteCategoryById);


        return api;
    }
    
    public static async Task<Results<Ok<PaginatedItems<CatalogItem>>, BadRequest<string>>> GetAllItems(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        var totalItems = await services.Context.CatalogItems
            .LongCountAsync();

        var itemsOnPage = await services.Context.CatalogItems
            .OrderBy(c => c.Title)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }
    
    public static async Task<Ok<List<CatalogItem>>> GetItemsByIds(
        [AsParameters] CatalogServices services,
        int[] ids)
    {
        var items = await services.Context.CatalogItems.Where(item => ids.Contains(item.Id)).ToListAsync();
        return TypedResults.Ok(items);
    }

    public static async Task<Results<Ok<CatalogItem>, NotFound, BadRequest<string>>> GetItemById(
        [AsParameters] CatalogServices services,
        int id)
    {
        if (id <= 0)
        {
            return TypedResults.BadRequest("Id is not valid.");
        }

        var item = await services.Context.CatalogItems.Include(ci => ci.Category).SingleOrDefaultAsync(ci => ci.Id == id);

        if (item == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(item);
    }

    public static async Task<Ok<PaginatedItems<CatalogItem>>> GetItemsByName(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        string name)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        var totalItems = await services.Context.CatalogItems
            .Where(c => c.Title.StartsWith(name))
            .LongCountAsync();

        var itemsOnPage = await services.Context.CatalogItems
            .Where(c => c.Title.StartsWith(name))
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }

    public static async Task<Results<NotFound, PhysicalFileHttpResult>> GetItemPictureById(CatalogContext context, IWebHostEnvironment environment, int catalogItemId)
    {
        var item = await context.CatalogItems.FindAsync(catalogItemId);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        var path = GetFullPath(environment.ContentRootPath, item.PictureFileName);

        string imageFileExtension = Path.GetExtension(item.PictureFileName);
        string mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);
        DateTime lastModified = File.GetLastWriteTimeUtc(path);

        return TypedResults.PhysicalFile(path, mimetype, lastModified: lastModified);
    }
    
    public static async Task<Ok<PaginatedItems<CatalogItem>>> GetItemsByCategoryId(
        [AsParameters] PaginationRequest paginationRequest,
        [AsParameters] CatalogServices services,
        int? categoryId)
    {
        var pageSize = paginationRequest.PageSize;
        var pageIndex = paginationRequest.PageIndex;

        var root = (IQueryable<CatalogItem>)services.Context.CatalogItems;

        if (categoryId is not null)
        {
            root = root.Where(ci => ci.CategoryId == categoryId);
        }

        var totalItems = await root
            .LongCountAsync();

        var itemsOnPage = await root
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new PaginatedItems<CatalogItem>(pageIndex, pageSize, totalItems, itemsOnPage));
    }
    
    public static async Task<Results<Created, NotFound<string>>> UpdateItem(
        [AsParameters] CatalogServices services,
        CatalogItem productToUpdate)
    {
        var catalogItem = await services.Context.CatalogItems.SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

        if (catalogItem == null)
        {
            return TypedResults.NotFound($"Item with id {productToUpdate.Id} not found.");
        }

        // Update current product
        var catalogEntry = services.Context.Entry(catalogItem);
        catalogEntry.CurrentValues.SetValues(productToUpdate);

        //catalogItem.Embedding = await services.CatalogAI.GetEmbeddingAsync(catalogItem);

        var priceEntry = catalogEntry.Property(i => i.Price);

        
        await services.Context.SaveChangesAsync();
        
        return TypedResults.Created($"/api/catalog/items/{productToUpdate.Id}");
    }

    public static async Task<Created> CreateCategory(
        [AsParameters] CatalogServices services,
        CatalogCategory product)
    {
        var item = new CatalogCategory()
        {
            Description = product.Description,
            Title = product.Title,
        };

        services.Context.CatalogCategories.Add(item);
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/catalog/category/items/{item.Id}");
    }
    
    public static async Task<Created> CreateItem(
        [AsParameters] CatalogServices services,
        CatalogItem product)
    {
        var item = new CatalogItem
        {
            CategoryId = product.CategoryId,
            Description = product.Description,
            Title = product.Title,
            Subtitle = product.Subtitle,
            Price = product.Price
        };

        services.Context.CatalogItems.Add(item);
        await services.Context.SaveChangesAsync();

        return TypedResults.Created($"/api/catalog/items/{item.Id}");
    }

    public static async Task<Results<NoContent, NotFound>> DeleteItemById(
        [AsParameters] CatalogServices services,
        int id)
    {
        var item = services.Context.CatalogItems.SingleOrDefault(x => x.Id == id);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        services.Context.CatalogItems.Remove(item);
        await services.Context.SaveChangesAsync();
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, NotFound>> DeleteCategoryById(
            [AsParameters] CatalogServices services,
            int id)
        {
            var item = services.Context.CatalogCategories.SingleOrDefault(x => x.Id == id);
    
            if (item is null)
            {
                return TypedResults.NotFound();
            }
    
            services.Context.CatalogCategories.Remove(item);
            await services.Context.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        
    
    
    
    
    
    private static string GetImageMimeTypeFromImageFileExtension(string extension) => extension switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".bmp" => "image/bmp",
        ".tiff" => "image/tiff",
        ".wmf" => "image/wmf",
        ".jp2" => "image/jp2",
        ".svg" => "image/svg+xml",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };
     public static string GetFullPath(string contentRootPath, string pictureFileName) =>
            Path.Combine(contentRootPath, "Pics", pictureFileName);
}