using CheyennePowerAgentApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IClaudeService, ClaudeService>();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();

app.Run();