namespace Catalog.API.Model;

public class CatalogItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? PictureFileName { get; set; }
    public int CategoryId { get; set; }
    public CatalogCategory Category { get; set; } = null!;
}