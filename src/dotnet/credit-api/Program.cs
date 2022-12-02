using Carter;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => 
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();
builder.Services.AddDaprClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.MapCarter();
app.Run();