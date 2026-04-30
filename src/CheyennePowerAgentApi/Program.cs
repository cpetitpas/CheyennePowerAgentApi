using System.Text.Json;
using CheyennePowerAgentApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddHttpClient();
builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IGenerationTools, GenerationTools>();
builder.Services.AddSingleton<TelemetryChannel>();
builder.Services.AddHostedService<TelemetrySimulator>();

var app = builder.Build();

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = { "dashboard.html" }
});
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();