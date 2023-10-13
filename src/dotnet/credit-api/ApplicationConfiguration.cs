using CreditApi.Modules.Credit;
using Serilog;

namespace CreditApi;

internal static class ApplicationConfiguration
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDaprClient();
        builder.Services.AddHealthChecks();

        builder.Services.AddCreditModule();
        
        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHealthChecks("/healthz");
        app.UseSerilogRequestLogging();
        
        CreditModule.MapRoutes(app);
        
        return app;
    }
}