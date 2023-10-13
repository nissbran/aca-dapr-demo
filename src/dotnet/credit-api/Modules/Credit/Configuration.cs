using OpenTelemetry.Metrics;

namespace CreditApi.Modules.Credit;

public static class CreditConfiguration
{
    internal static IServiceCollection AddCreditModule(this IServiceCollection services)
    {
        services.AddSingleton<CreditMetrics>();
        return services;
    }
    
    internal static MeterProviderBuilder AddCreditMetrics(this MeterProviderBuilder builder)
    {
        builder.AddMeter(CreditMetrics.InstrumentationName);
        return builder.AddInstrumentation(provider => provider.GetRequiredService<CreditMetrics>());
    }
}