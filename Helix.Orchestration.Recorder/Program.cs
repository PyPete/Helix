using System.Threading.Channels;
using Helix.Orchestration.Infrastructure.Storage;
using Helix.Orchestration.Recorder.Configuration;
using Helix.Orchestration.Recorder.Models;
using Helix.Orchestration.Recorder.Services;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.Configure<RecorderOptions>(
        builder.Configuration.GetSection(RecorderOptions.SectionName));

    builder.Services.Configure<MinioOptions>(
        builder.Configuration.GetSection("Minio"));

    var channel = Channel.CreateBounded<RecordedEvent>(new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleWriter = false,
        SingleReader = true
    });

    builder.Services.AddSingleton(channel);
    builder.Services.AddSingleton<IObjectStorageSink>(sp =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MinioOptions>>().Value;
        return new MinioObjectStorageSink(opts);
    });

    builder.Services.AddHostedService<MqttRecorderService>();
    builder.Services.AddHostedService<RecorderWriterService>();

    builder.Services.AddSerilog((_, lc) => lc
        .WriteTo.Console(new RenderedCompactJsonFormatter()));

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Recorder service terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
