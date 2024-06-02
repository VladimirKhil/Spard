using AspNetCoreRateLimit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using Spard.Service.BackgroundServices;
using Spard.Service.Configuration;
using Spard.Service.Contract;
using Spard.Service.Contracts;
using Spard.Service.EndpointDefinitions;
using Spard.Service.Metrics;
using Spard.Service.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(new Serilog.Formatting.Display.MessageTemplateTextFormatter(
        "[{Timestamp:yyyy/MM/dd HH:mm:ss} {Level}] {Message:lj} {Exception}{NewLine}"))
    .ReadFrom.Configuration(ctx.Configuration)
    .Filter.ByExcluding(logEvent =>
        logEvent.Exception is BadHttpRequestException
        || logEvent.Exception is OperationCanceledException));

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

Configure(app);

app.Run();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<SpardOptions>(configuration.GetSection(SpardOptions.ConfigurationSectionName));

    services.AddSingleton<IExamplesRepository, ExamplesRepository>();
    services.AddSingleton<ITransformManager, TransformManager>();

    services.AddHostedService<ExamplesLoader>();

    AddRateLimits(services, configuration);
    AddMetrics(services, configuration);

    services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, SpardSerializerContext.Default);
    });
}

static void AddRateLimits(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimit"));

    services.AddMemoryCache();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}

static void AddMetrics(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<OtelMetrics>();

    services.AddOpenTelemetry().WithMetrics(builder =>
        builder
            .ConfigureResource(rb => rb.AddService("Spard"))
            .AddMeter(OtelMetrics.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(options =>
            {
                var otelUri = configuration["OpenTelemetry:ServiceUri"];

                if (otelUri != null)
                {
                    options.Endpoint = new Uri(otelUri);
                }
            }));
}

static void Configure(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    ExamplesEndpointDefinitions.DefineExamplesEndpoint(app);
    SpardEndpointDefinitions.DefineExamplesEndpoint(app);
    TransformEndpointDefinitions.DefineExamplesEndpoint(app);

    app.UseIpRateLimiting();
}

[JsonSerializable(typeof(IEnumerable<SpardExampleBaseInfo>))]
[JsonSerializable(typeof(SpardExampleInfo))]
[JsonSerializable(typeof(TransformRequest))]
[JsonSerializable(typeof(TransformTableResult))]
[JsonSerializable(typeof(ProcessResult<string>))]
internal partial class SpardSerializerContext : JsonSerializerContext { }