using Asp.Versioning.Builder;
using Catalog.API.Apis;
using RestaurantApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors("AllowAll");

var catalogApi  = app.NewVersionedApi("Catalog");

catalogApi.MapCatalogApiV1()
    .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();