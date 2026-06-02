using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.Simulation;
using Helix.Orchestration.SiteServer.Agent;
using Helix.Orchestration.SiteServer.Campaigns;
using Helix.Orchestration.SiteServer.Endpoints;
using Helix.Orchestration.SiteServer.Mqtt;
using Helix.Orchestration.SiteServer.WebSockets;
using Microsoft.AspNetCore.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Helix Site Server API",
        Version = "v1",
        Description = "REST API for Helix lab automation control plane + MQTT-over-WebSocket real-time trends"
    });
});

// Register simulation layer (includes SimulatedTrendProvider, sim devices, SimCapabilityInvoker)
builder.Services.AddHelixSimulation();

// Register switchable agent proxy (sim or gRPC)
builder.Services.AddHelixAgentProxy(builder.Configuration);

// Register campaign services
builder.Services.AddSingleton<ICampaignIterationStrategy, SequentialStrategy>();
builder.Services.AddSingleton<ICampaignManager, CampaignManager>();

// Register MQTT trend bridge as hosted service
builder.Services.Configure<MqttBridgeOptions>(builder.Configuration.GetSection("MqttBridge"));
builder.Services.AddHostedService<MqttTrendBridge>();

// Register WebSocket services for live trend streaming (E5)
builder.Services.AddSingleton<TrendWebSocketManager>();
builder.Services.AddWebSockets(options =>
{
    options.AllowedOrigins.Add("http://localhost:5174");
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
});

// Register PostgreSQL repositories for campaigns and experiments
    builder.Services.AddHelixInfrastructure(builder.Configuration);

    var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// UseWebSockets MUST come before any app.Map* calls
app.UseWebSockets();

// Map endpoint groups
app.MapExperimentEndpoints();
app.MapDeviceEndpoints();
app.MapTrendEndpoints();
app.MapCampaignEndpoints();

// WebSocket endpoint for live trend streaming (E5 / D-02)
var trendWsManager = app.Services.GetRequiredService<TrendWebSocketManager>();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/trends/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await trendWsManager.HandleClientAsync(ws, context.RequestAborted);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else { await next(context); }
});

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
