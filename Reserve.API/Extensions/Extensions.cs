using Microsoft.EntityFrameworkCore;
using Reserve.API;
using Reserve.API.Infrastructure;
using RestaurantApp.ServiceDefaults;


public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddDefaultAuthentication();
        builder.AddNpgsqlDbContext<ReserveContext>("reservedb", configureDbContextOptions: dbContextOptionsBuilder =>
        {
            dbContextOptionsBuilder.UseNpgsql(builder =>
            {
                builder.UseVector();
            });
        });

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<ReserveContext, ReserveContextSeed>();

        // Add the integration services that consume the DbContext
        /*builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();

        builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
               .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();*/

        builder.Services.AddOptions<ReserveOptions>()
            .BindConfiguration(nameof(ReserveOptions));

        if (builder.Configuration["AI:Onnx:EmbeddingModelPath"] is string modelPath &&
            builder.Configuration["AI:Onnx:EmbeddingVocabPath"] is string vocabPath)
        {
            //builder.Services.AddBertOnnxTextEmbeddingGeneration(modelPath, vocabPath);
        }
        else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("openai")))
        {
            //builder.AddAzureOpenAIClient("openai");
            //builder.Services.AddOpenAITextEmbeddingGeneration(builder.Configuration["AIOptions:OpenAI:EmbeddingName"] ?? "text-embedding-3-small");
        }

        //builder.Services.AddSingleton<ICatalogAI, CatalogAI>();
    }
}
