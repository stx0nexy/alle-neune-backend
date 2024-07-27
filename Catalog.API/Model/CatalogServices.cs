using Catalog.API.Infrastructure;
using Microsoft.Extensions.Options;

namespace Catalog.API.Model;

public class CatalogServices(
    CatalogContext context,
    IOptions<CatalogOptions> options,
    ILogger<CatalogServices> logger
    //ICatalogIntegrationEventService eventService
    )
{
    public CatalogContext Context { get; } = context;
    public IOptions<CatalogOptions> Options { get; } = options;
    public ILogger<CatalogServices> Logger { get; } = logger;
    //public ICatalogIntegrationEventService EventService { get; } = eventService;
};