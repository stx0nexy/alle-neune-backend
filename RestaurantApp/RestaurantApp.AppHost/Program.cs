using RestaurantApp.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddForwardedHeaders();


var launchProfileName = ShouldUseHttpForEndpoints() ? "http" : "https";

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithImage("ankane/pgvector")
    .WithImageTag("latest");

var catalogDb = postgres.AddDatabase("catalogdb");
var identityDb = postgres.AddDatabase("identitydb");
var reserveDb = postgres.AddDatabase("reservedb");


var identityApi = builder.AddProject<Projects.Identity_API>("identity-api", launchProfileName)
        .WithReference(identityDb);

var catalogApi = builder.AddProject<Projects.Catalog_API>("catalog-api")
        .WithReference(catalogDb);

var reserveApi = builder.AddProject<Projects.Reserve_API>("reserve-api")
    .WithReference(reserveDb);

var apiService = builder.AddProject<Projects.RestaurantApp_ApiService>("apiservice");

builder.AddProject<Projects.RestaurantApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

identityApi.WithEnvironment("CatalogApiClient", catalogApi.GetEndpoint("http"))
            .WithEnvironment("ReserveApiClient", reserveApi.GetEndpoint("http"));
    
builder.Build().Run();

static bool ShouldUseHttpForEndpoints()
{
    const string EnvVarName = "ESHOP_USE_HTTP_ENDPOINTS";
    var envValue = Environment.GetEnvironmentVariable(EnvVarName);

    // Attempt to parse the environment variable value; return true if it's exactly "1".
    return int.TryParse(envValue, out int result) && result == 1;
}