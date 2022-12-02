using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => 
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen));
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();
app.Run();