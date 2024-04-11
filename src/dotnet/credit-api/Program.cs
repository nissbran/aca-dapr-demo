using CreditApi;
using CreditApi.Telemetry;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Events;

const string appName = "credit-api";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", theme: AnsiConsoleTheme.Sixteen)
    .CreateBootstrapLogger();

Log.Information("Starting up {Application}", appName);

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.local.json", true);

    var app = builder
        .ConfigureTelemetry("credits", appName, "team 1")
        .ConfigureServices()
        .ConfigurePipeline();
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception when starting {Application}", appName);
}
finally
{
    Log.Information("Shut down complete for {Application}", appName);
    Log.CloseAndFlush();
}
