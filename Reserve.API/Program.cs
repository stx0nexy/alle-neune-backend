using Asp.Versioning.Builder;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Mailjet.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Reserve.API.Apis;
using Reserve.API.Services;
using RestaurantApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Configure services
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
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (string.IsNullOrEmpty(keyVaultUri))
{
    throw new ArgumentNullException(nameof(keyVaultUri), "Key Vault URI must be provided in configuration.");
}
var keyVaultEndpoint = new Uri(keyVaultUri);

builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

// Регистрация служб Key Vault
builder.Services.AddSingleton(new SecretClient(keyVaultEndpoint, new DefaultAzureCredential()));
builder.Services.AddSingleton(new KeyClient(keyVaultEndpoint, new DefaultAzureCredential()));
builder.Services.AddTransient<EncryptionService>();
builder.Services.AddTransient<EmailService>();

// Configure API versioning
var withApiVersioning = builder.Services.AddApiVersioning();

// Configure OpenAPI
builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

// Configure middleware
//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();

// Map default endpoints
app.MapDefaultEndpoints();

app.UseCors("AllowAll");

var reserveApi  = app.NewVersionedApi("Reserve");

reserveApi.MapReserveApiV1()
    .RequireAuthorization();

// Use OpenAPI
app.UseDefaultOpenApi();

app.Run();