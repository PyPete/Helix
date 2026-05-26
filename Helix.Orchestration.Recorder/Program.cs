using System.Threading.Channels;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Helix.Orchestration.Recorder.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
    .CreateLogger();

builder.Services.AddSerilog();

// Bind RecorderOptions from configuration
builder.Services.Configure<RecorderOptions>(builder.Configuration.GetSection("Recorder"));

var recorderOptions = builder.Configuration.GetSection("Recorder").Get<RecorderOptions>()
    ?? new RecorderOptions();

// Register bounded Channel<RecordedEvent> as singleton
var channel = Channel.CreateBounded<RecordedEvent>(new BoundedChannelOptions(recorderOptions.ChannelCapacity)
{
    FullMode = BoundedChannelFullMode.DropWrite
});
builder.Services.AddSingleton(channel);

// Register MinIO storage
builder.Services.AddMinioStorage(builder.Configuration);

// Register background services
builder.Services.AddHostedService<MqttRecorderService>();
builder.Services.AddHostedService<RecorderWriterService>();

var host = builder.Build();

try
{
    Log.Information("Starting Helix Orchestration Recorder...");
    await host.RunAsync();
    Log.Information("Helix Orchestration Recorder stopped cleanly.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Helix Orchestration Recorder terminated unexpectedly.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
