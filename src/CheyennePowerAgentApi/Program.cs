using CheyennePowerAgentApi.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IInvestigateService, InvestigateService>();
builder.Services.AddScoped<IMultiNodeInvestigateService, MultiNodeInvestigateService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGenerationTools, GenerationTools>();
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddSingleton<TelemetryChannel>();
builder.Services.AddHostedService<TelemetrySimulator>();

builder.Services.AddHttpClient<InvestigateService>();
builder.Services.AddHttpClient<MultiNodeInvestigateService>();
builder.Services.AddHttpClient<ChatService>();

var app = builder.Build();

var staticFileOptions = new StaticFileOptions();
var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("dashboard.html");

app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();
app.MapControllers();

app.Run();