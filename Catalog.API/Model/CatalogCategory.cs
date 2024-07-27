namespace Catalog.API.Model;

public class CatalogCategory
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? PictureFileName { get; set; }
}